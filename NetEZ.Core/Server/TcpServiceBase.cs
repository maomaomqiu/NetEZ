using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using NetEZ.Core.Protocol;
using NetEZ.Core.Event;
using NetEZ.Core.IO;
using NetEZ.Core.Client;
using NetEZ.Utility.Logger;

namespace NetEZ.Core.Server
{
    public class TcpServiceBase : IService
    {
        public const int DEFAULT_CONCURRENCYLEVEL = 10;
        public const int DEFAULT_MESSAGE_PROC_THREADS = 5;
        public const int DEFAULT_CLIENT_TABLE_CAPACITY = 1024 * 10;
        public const int DEFAULT_MESSAGE_MAX_LENGTH = Defines.MAX_MESSAGE_LENGTH;


        private SocketAsyncEventArgsPool _SaeaReadPool = null;
        private SocketAsyncEventArgsPool _SaeaWritePool = null;


        private ConcurrentQueue<ClientMessageContext>[] _ClientMessageQueues = null;
        private AutoResetEvent[] _ClientMessageQueueEvents = null;
        private Thread[] _MessageProcThreadPool = null;


        private volatile bool _IsServerRunning = false;

        private Thread _ClosingClientThread = null;
        private ConcurrentQueue<long> _ClosingClientIdQueue = new ConcurrentQueue<long>();
        private AutoResetEvent _ClosingClientSignal = new AutoResetEvent(false);

        private Thread _ObservingClientAliveThread = null;
        private ConcurrentQueue<long> _ObservingClientAliveQueue = new ConcurrentQueue<long>();

        protected string _ServiceName = string.Empty;
        protected int _ClientReceivingBufferSize = 0;
        protected int _MessageProcThreads = DEFAULT_MESSAGE_PROC_THREADS;

        protected event OnClientConnectedEvent _OnClientConnectedHandler = null;
        protected event OnClientClosingEvent _OnClientClosingHandler = null;
        protected event OnClientDisconnectedEvent _OnClientDisconnectedHandler = null;
        protected event OnClientReceivedEvent _OnClientReceivedHandler = null;
        protected event OnClientSentEvent _OnClientSentHandler = null;

        protected IProtocolParser _MessageParser = null;

        protected Logger _Logger = null;
        protected bool _LoggerEnabled = true;

        protected ConcurrentDictionary<long, NetEZ.Core.Server.TcpClientManager> _Clients = null;
        //protected ConcurrentQueue<byte[]> _MessageQueue = new ConcurrentQueue<byte[]>();

        protected string _Root = string.Empty;
        protected TcpServiceConfigure _Config = null;

        protected TcpClientParams _DefaultClientConfigure = null;

        protected List<Socket> _ServerSockets = new List<Socket>();
        protected ServerStat _Stat = new ServerStat();

        //protected volatile bool _HeartMonitorWorking = false;
        //protected Thread _HeartMonitor = null;



        public string ServiceName { get { return _ServiceName; } }
        public int ClientReceivingBufferSize { get { return _ClientReceivingBufferSize; } }
        public TcpServiceConfigure Config { get { return _Config; } }

        protected virtual bool InitService()
        {
            int concurrencylevel = DEFAULT_CONCURRENCYLEVEL;
            int capacity = DEFAULT_CLIENT_TABLE_CAPACITY;
            int maxMessageLength = DEFAULT_MESSAGE_MAX_LENGTH;

            if (!_Config.GetItemInt32Value("Socket", "ConcurrencyLevel", DEFAULT_CONCURRENCYLEVEL, out concurrencylevel))
                concurrencylevel = DEFAULT_CONCURRENCYLEVEL;

            if (!_Config.GetItemInt32Value("Socket", "Capacity", DEFAULT_CLIENT_TABLE_CAPACITY, out capacity))
                capacity = DEFAULT_CLIENT_TABLE_CAPACITY;

            if (!_Config.GetItemInt32Value("Socket", "MaxMessageLength", DEFAULT_MESSAGE_MAX_LENGTH, out maxMessageLength))
                maxMessageLength = DEFAULT_MESSAGE_MAX_LENGTH;

            if (concurrencylevel < 1)
                concurrencylevel = DEFAULT_CONCURRENCYLEVEL;
            if (capacity < 10)
                capacity = DEFAULT_CLIENT_TABLE_CAPACITY;
            if (maxMessageLength < 32)
                maxMessageLength = DEFAULT_MESSAGE_MAX_LENGTH;

            _Clients = new ConcurrentDictionary<long, Server.TcpClientManager>(concurrencylevel, capacity);

            //  初始化消息处理机线程
            //  必须放在StartAccept()之前
            InitServiceThreads();

            //  server socket初始化
            foreach (HostInfo host in _Config.ListenHosts)
            {
                Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                serverSocket.Bind(new IPEndPoint(IPAddress.Parse(host.Host), host.Port));
                serverSocket.Listen((int)SocketOptionName.MaxConnections);

                _ServerSockets.Add(serverSocket);
            }

            //_ServerSockets = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //_ServerSockets.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //_ServerSockets.Bind(new IPEndPoint(IPAddress.Parse(_Config.ListenHosts[0]), _Config.Port));        //  暂时只监听第一个ip
            //_ServerSockets.Listen((int)SocketOptionName.MaxConnections);

            _SaeaReadPool = new SocketAsyncEventArgsPool(_Config.ClientBuffSize);
            _SaeaWritePool = new SocketAsyncEventArgsPool(_Config.ClientBuffSize);

            _DefaultClientConfigure = new TcpClientParams();
            _DefaultClientConfigure.MessageMaxLength = maxMessageLength;

            for (int i = 0; i < _Config.ListenHosts.Count; i++)
            {
                StartAccept(null, i);
            }

            return true;
        }

        protected virtual bool InitLogger()
        {
            string logFolder = _Config.GetItemValue("Logger", "Folder");
            string templateName = _Config.GetItemValue("Logger", "TemplateName");
            int logLevel = 7;

            if (string.Compare(_Config.GetItemValue("Logger", "Enable"), "false", true) == 0)
            {
                //  关闭log需要显示指定为false
                _LoggerEnabled = false;
            }
            else
                _LoggerEnabled = true;

            //  log level
            //  bit0 = info; bit1 = debug; bit2 = error
            if (!_Config.GetItemInt32Value("Logger", "Level", 7, out logLevel))
                logLevel = 7;

            if (string.IsNullOrEmpty(logFolder) || string.IsNullOrEmpty(templateName))
                return false;

            string logPath = string.Format("{0}\\{1}", _Root, logFolder);

            _Logger = new Logger(logPath, templateName);

            if ((logLevel & 1) > 0)
                _Logger.EnableLogLevel(LogLevel.Info);
            if ((logLevel & 2) > 0)
                _Logger.EnableLogLevel(LogLevel.Debug);
            if ((logLevel & 4) > 0)
                _Logger.EnableLogLevel(LogLevel.Error);

            return true;
        }

        /// <summary>
        /// 加载配置文件;继承类可以重写以实现自定义配置
        /// </summary>
        /// <param name="cfgFile"></param>
        /// <returns></returns>
        protected virtual bool LoadConfigure(string cfgFile = "")
        {
            if (_Config != null)
                return true;

            try
            {
                _Root = System.Environment.CurrentDirectory;
                if (string.IsNullOrEmpty(cfgFile))
                    cfgFile = string.Format("{0}\\{1}", _Root, "config.xml");
                _Config = new TcpServiceConfigure(cfgFile);
                return true;
            }
            catch { }

            return false;
        }

        private SocketAsyncEventArgs PopReadSaea()
        {
            bool inited = false;
            SocketAsyncEventArgs saea = _SaeaReadPool.Pop(true, out inited);
            if (!inited)
                saea.Completed += ReadWrite_IO_Completed;
            return saea;
        }

        private void ReleaseReadSaea(SocketAsyncEventArgs saea)
        {
            if (saea != null)
            {
                saea.UserToken = null;
                saea.AcceptSocket = null;
            }

            _SaeaReadPool.Push(saea);
        }

        private SocketAsyncEventArgs PopWriteSaea()
        {
            bool inited = false;
            SocketAsyncEventArgs saea = _SaeaWritePool.Pop(false, out inited);
            if (!inited)
                saea.Completed += ReadWrite_IO_Completed;
            return saea;
        }

        private void ReleaseWriteSaea(SocketAsyncEventArgs saea)
        {
            if (saea != null)
            {
                saea.UserToken = null;
                saea.SetBuffer(null, 0, 0);
            }

            _SaeaWritePool.Push(saea);
        }

        public void SetLogger(Logger logger)
        {
            _Logger = logger;
        }

        public void LogInfo(string log)
        {
            if (!_LoggerEnabled)
                return;

            if (_Logger != null && !string.IsNullOrEmpty(log))
                _Logger.Info(log);
        }

        public void LogDebug(string log)
        {
            if (!_LoggerEnabled)
                return;

            if (_Logger != null && !string.IsNullOrEmpty(log))
                _Logger.Debug(log);
        }

        public void LogError(string log)
        {
            if (!_LoggerEnabled)
                return;

            if (_Logger != null && !string.IsNullOrEmpty(log))
                _Logger.Error(log);
        }


        public void RegisterMessageParser(IProtocolParser parser)
        {
            if (parser == null)
                throw new Exception("invalid parser.");

            _MessageParser = parser;
        }

        public void RegisterOnClientConnectedCallback(OnClientConnectedEvent handler)
        {
            if (handler != null)
                _OnClientConnectedHandler += handler;
        }

        public void RegisterOnClientDisconnectedCallback(OnClientDisconnectedEvent handler)
        {
            if (handler != null)
                _OnClientDisconnectedHandler += handler;
        }

        public void RegisterOnClientClosingCallback(OnClientClosingEvent handler)
        {
            if (handler != null)
                _OnClientClosingHandler += handler;
        }


        public void RegisterOnClientReceivedCallback(OnClientReceivedEvent handler)
        {
            if (handler != null)
                _OnClientReceivedHandler += handler;
        }

        public void RegisterOnClientSentCallback(OnClientSentEvent handler)
        {
            if (handler != null)
                _OnClientSentHandler += handler;
        }

        private void InitServiceThreads()
        {
            _Config.GetItemInt32Value("Socket", "MessageThreads", DEFAULT_MESSAGE_PROC_THREADS, out _MessageProcThreads);

            if (_MessageProcThreads < 1)
                _MessageProcThreads = DEFAULT_MESSAGE_PROC_THREADS;

            //  初始化消息处理机(程池、消息队列、事件对象)


            //  消息处理机：ConcurrentQueue版
            _MessageProcThreadPool = new Thread[_MessageProcThreads];
            _ClientMessageQueueEvents = new AutoResetEvent[_MessageProcThreads];
            _ClientMessageQueues = new ConcurrentQueue<ClientMessageContext>[_MessageProcThreads];

            for (int i = 0; i < _MessageProcThreads; i++)
            {
                _ClientMessageQueueEvents[i] = new AutoResetEvent(false);
                _ClientMessageQueues[i] = new ConcurrentQueue<ClientMessageContext>();

                _MessageProcThreadPool[i] = new Thread(new ParameterizedThreadStart(MessageProcingLoop));
                _MessageProcThreadPool[i].IsBackground = true;
                _MessageProcThreadPool[i].Start(i);
                while (!_MessageProcThreadPool[i].IsAlive)
                    Thread.Sleep(3);
            }

            //  启动异步关闭客户端线程
            _ClosingClientThread = new Thread(ClosingClientLoop);
            _ClosingClientThread.IsBackground = true;
            _ClosingClientThread.Start();
            while (!_ClosingClientThread.IsAlive)
                Thread.Sleep(3);

            //  如果指定了心跳监测间隔时间
            if (_Config.HeartbeatIdle > 0)
            {
                //  启动客户管观察线程（用于监测心跳）
                _ObservingClientAliveThread = new Thread(ObservingClientAliveLoop);
                _ObservingClientAliveThread.IsBackground = true;
                _ObservingClientAliveThread.Start();
                while (!_ObservingClientAliveThread.IsAlive)
                    Thread.Sleep(3);
            }

        }

        /// <summary>
        /// 输出控制台方法，子类可以重写
        /// </summary>
        /// <param name="paraName"></param>
        /// <param name="msg"></param>
        public virtual void PrintToConsole(string paraName, string msg)
        {
            Console.WriteLine(msg);
        }

        public virtual bool Start()
        {
            if (!LoadConfigure())
            {
                Console.WriteLine("*** [{0}] Starting failed:{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "LoadConfigure");
                return false;
            }

            //if (!InitLogger())
            //{
            //    Console.WriteLine("*** [{0}] Starting failed:{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "InitLogger");
            //    return false;
            //}

            _IsServerRunning = true;

            //NetEZ.Core.Protocol.PureText.PureTextParser parser = new Protocol.PureText.PureTextParser(_Logger);
            //RegisterMessageParser(parser);

            //  初始化socket服务
            if (!InitService())
            {
                LogError(string.Format("Starting failed:{0}", "InitService"));
                Console.WriteLine("*** [{0}] Starting failed:{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "InitService");
                Stop();
                return false;
            }

            //  统计对象启动
            _Stat.SetStart();

            LogInfo(string.Format("Started. MessageProcThreads={0}", _MessageProcThreads));

            PrintToConsole("Started", string.Format("[{0}] Started. MPT:{1}; ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), _MessageProcThreads));

            return true;
        }

        public virtual void Stop()
        {
            _IsServerRunning = false;

            //if (_MessageProcThreadPool != null)
            //{
            //    for (int i = 0; i < _MessageProcThreads; i++)
            //    {
            //        try
            //        {
            //            _MessageProcThreadPool[i].Join(500);
            //        }
            //        catch { }
            //        finally { _MessageProcThreadPool[i] = null; }
            //    }

            //    _MessageProcThreadPool = null;
            //}
        }

        private void StartAccept(SocketAsyncEventArgs sae, int idx)
        {
            try
            {
                if (sae == null)
                {
                    sae = new SocketAsyncEventArgs();
                    sae.UserToken = idx;
                    sae.Completed += new EventHandler<SocketAsyncEventArgs>(Accept_IO_Completed);
                }
                else
                {
                    sae.AcceptSocket = null;
                }

                bool willRaiseEvent = _ServerSockets[idx].AcceptAsync(sae);
                if (!willRaiseEvent)
                {
                    ProcessAccept(sae);
                }
            }
            catch (Exception ex)
            {
                LogError(string.Format("StartAccept err:{0};line:{1}", ex.Message, ex.StackTrace));
                //ReleaseReadAsyncEventArgs(sae); 
            }
        }

        private void StartReceive(SocketAsyncEventArgs saea)
        {
            TcpClientManager client = null;

            try
            {
                client = (TcpClientManager)saea.UserToken;
                if (!client.ClientSocket.ReceiveAsync(saea))
                {
                    ProcessReceive(saea);
                }
            }
            catch (Exception ex)
            {
                //  记录错误
                LogError(string.Format("StartReceive err:{0}; line:{1}", ex.Message, ex.StackTrace));

                //  关闭连接
                ShutdownClient(client);
            }
            finally { }

        }

        private void CloseClientNow(TcpClientManager client)
        {
            if (client == null)
                return;

            SocketAsyncEventArgs saea = client.ReadSaea;
            //  先dispose()
            client.Dispose();
            //  再回收saea
            ReleaseReadSaea(saea);
            //  触发客户端断开事件
            OnClientDisconnected(client);
        }

        /// <summary>
        /// 注册客户端实例
        /// </summary>
        /// <param name="client"></param>
        private void RegisterClient(TcpClientManager client)
        {
            client.RefreshLastRecvTime();

            _Clients.AddOrUpdate(client.ClientId, client, (k, v) => client);

            //  加入心跳监测队列
            if (_Config.HeartbeatIdle > 0)
                _ObservingClientAliveQueue.Enqueue(client.ClientId);

            OnClientConnected(client);
        }

        /// <summary>
        /// 客户端连接事件
        /// 注意：执行本方法占用IO线程
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnClientConnected(TcpClientManager client)
        {
            try
            {
                if (_OnClientConnectedHandler != null)
                    _OnClientConnectedHandler(client);
            }
            catch { }

        }

        //  保留
        //private void ClientReceivedMessagePreProc(ClientMessageContext cmc)
        //{
        //    if (cmc == null || cmc.Client == null || cmc.MsgBytes == null)
        //        return;

        //    TcpClientManager client = (TcpClientManager)cmc.Client;

        //    if (!_Clients.TryGetValue(client.ClientId, out client))
        //        return;

        //    IMessage msg = null;
        //    if (!_MessageParser.ParseMessageFromBytes(cmc.MsgBytes, out msg))
        //        return;

        //    //  刷新最近接收数据时间
        //    client.RefreshLastRecvTime();

        //    try
        //    {
        //        //  触发事件，通知上层业务客户端收到解析后的消息
        //        OnClientReceivedMessage(client, msg);
        //    }
        //    catch { }
        //}

        /// <summary>
        /// 客户端收到消息后的首要处理函数
        /// [Private]这里目前只用于刷新客户端活跃时间
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        private void ClientReceivedMessagePreProc(TcpClientManager client, IMessage msg)
        {
            if (client == null)
            {
                LogError("[TcpServiceBase][ClientReceivedMessagePreProc] client is null.");
                return;
            }


            if (!_Clients.TryGetValue(client.ClientId, out client))
                return;
            //  刷新最近接收数据时间
            client.RefreshLastRecvTime();

            try
            {
                //  触发事件，通知上层业务客户端收到解析后的消息
                OnClientReceivedMessage(client, msg);
            }
            catch { }
        }

        private void ClientReceivedMessageMultiThreadProc(object obj)
        {
            if (obj == null)
            {
                LogError(string.Format("[TcpServiceBase][ClientReceivedMessageMultiThreadProc] Error. obj is null"));
                return;
            }
            //  [ClientReceivedMessagePreProcMultiThreadProc] Error. arg=object; client=null
            try
            {
                ClientMessageContext cmc = (ClientMessageContext)obj;
                if (cmc.Arg == null || cmc.Client == null)
                {
                    LogError(string.Format("[TcpServiceBase][ClientReceivedMessageMultiThreadProc] Error. arg={0}; client={1}", cmc.Arg == null ? "null" : "object", cmc.Client == null ? "null" : "object"));
                    return;
                }

                ClientReceivedMessagePreProc((TcpClientManager)cmc.Client, (IMessage)cmc.Arg);
            }
            catch (Exception ex)
            {
                LogError(string.Format("[TcpServiceBase][ClientReceivedMessageMultiThreadProc] Exception: Ex={0}-{1}", ex.Message, ex.StackTrace));
            }
            finally { }
        }

        //private void ParseAndDeliveryMessageAsync(object state)
        //{
        //    if (state == null)
        //        return;

        //    ClientMessageContext cmc = (ClientMessageContext)state;
        //    IMessage msg = null;
        //    if (_MessageParser.ParseMessageFromBytes(cmc.MsgBytes, out msg))
        //    {
        //        _Stat.PlusCntRecvMsg();
        //        //  客户端接收数据事件
        //        ClientReceivedMessagePreProc((TcpClientManager)cmc.Client, msg);

        //        //  测试用
        //        //Console.WriteLine("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss"), msg.ToString());
        //    }
        //    else
        //    {
        //        LogError(string.Format("[ParseMessageAndProcingAsync] failed. bytes:{0}", cmc.MsgBytes != null ? cmc.MsgBytes.Length : 0));
        //    }

        //}

        /// <summary>
        /// 通知上层业务收到了客户端发来的消息; 业务处理入口
        /// 注意:对于单个client而言确保消息是顺序的，不同client之间无法保证顺序
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        protected virtual void OnClientReceivedMessage(TcpClientManager client, IMessage msg)
        {
            if (_OnClientReceivedHandler != null)
                _OnClientReceivedHandler(client, msg);
        }

        /// <summary>
        /// 向客户端发完消息后的事件
        /// 注意：执行本方法占用IO线程
        /// </summary>
        /// <param name="client"></param>
        /// <param name="state"></param>
        protected virtual void OnClientSentMessage(TcpClientManager client, object state)
        {
            if (_OnClientSentHandler != null)
                _OnClientSentHandler(client, state);
        }

        /// <summary>
        /// 客户端正在关闭的事件
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnClientClosing(TcpClientManager client)
        {
            try
            {
                if (_OnClientClosingHandler != null)
                    _OnClientClosingHandler(client);
            }
            catch { }

        }

        /// <summary>
        /// 客户端断开后的事件
        /// 备注：触发执行本方法的线程目前只有异步closing线程
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnClientDisconnected(TcpClientManager client)
        {
            try
            {
                if (_OnClientDisconnectedHandler != null)
                    _OnClientDisconnectedHandler(client);
            }
            catch { }

        }

        protected virtual void PrintServerStatus()
        {
            int conn = _Clients.Count();
            long cntRecvMsg = _Stat.CntRecvMsg;
            string strRecvMsg = cntRecvMsg >= 1024 * 1024 ? string.Format("{0}M", cntRecvMsg / (1024 * 1024)) : cntRecvMsg >= 1024 ? string.Format("{0}K", cntRecvMsg / 1024) : string.Format("{0}", cntRecvMsg);
            long cntSendMsg = _Stat.CntSendMsg;
            string strSendMsg = cntSendMsg >= 1024 * 1024 ? string.Format("{0}M", cntSendMsg / (1024 * 1024)) : cntSendMsg >= 1024 ? string.Format("{0}K", cntSendMsg / 1024) : string.Format("{0}", cntSendMsg);

            long cntPing = _Stat.CntPing;

            long queueMsgs = 0;
            double avgRecvMsg = 0;
            double avgSendMsg = 0;

            _Stat.GetSnap(out avgRecvMsg, out avgSendMsg);


            foreach (ConcurrentQueue<ClientMessageContext> queue in _ClientMessageQueues)
            {
                queueMsgs += queue.Count;
            }
            LogInfo(string.Format("[TcpServiceBase] TotalWriteSaea={0}", _SaeaWritePool.TotalSaeas));
            Console.Write("\rCon:{0} | Ping:{1} | SM/RM:{2}/{3} | Qe:{4} | Sa-R/T:{5}/{6}    \r", conn, cntPing, Math.Round(avgSendMsg, 2), Math.Round(avgRecvMsg, 2), queueMsgs, _SaeaWritePool.Count, _SaeaWritePool.TotalSaeas);
        }

        /// <summary>
        /// 心跳监视
        /// </summary>
        /// <param name="state"></param>
        private void ObservingClientAliveLoop(object state)
        {
            DateTime lastPrintTime = DateTime.MinValue;
            int batch = 5000;

            while (_IsServerRunning)
            {
                //  每3秒执行一次
                Thread.Sleep(3000);

                if (lastPrintTime.AddMilliseconds(500) < DateTime.Now)
                {
                    PrintServerStatus();
                    lastPrintTime = DateTime.Now;
                }

                long clientId = 0;
                TcpClientManager client = null;
                int cnt = 0;

                //int queueLength = _ObservingClientAliveQueue.Count;

                //if (queueLength < 1)
                //    continue;

                // 由于心跳正常的clientId会重新放回queue中，所以判断cnt < queueLength 是必要的
                batch = _ObservingClientAliveQueue.Count;
                if (batch > 5000)
                    batch = 5000;

                while (cnt < batch && _ObservingClientAliveQueue.TryDequeue(out clientId))
                {
                    if (!_Clients.TryGetValue(clientId, out client))
                    {
                        //  client对象不存在，抛弃
                        continue;
                    }

                    cnt++;

                    if (client.LastRecvTime.AddSeconds(_Config.HeartbeatIdle) > DateTime.Now)
                    {
                        //  该客户端心跳正常，需要重新放回队列里
                        _ObservingClientAliveQueue.Enqueue(clientId);
                        continue;
                    }

                    //  客户端心跳停止,加入关闭队列
                    ShutdownClient(client);

                }
            }
        }

        private void ClosingClientLoop(object state)
        {
            while (_IsServerRunning)
            {
                _ClosingClientSignal.WaitOne(2000);

                long clientId = 0;
                TcpClientManager client = null;
                while (_ClosingClientIdQueue.TryDequeue(out clientId))
                {
                    if (_Clients.TryGetValue(clientId, out client))
                    {
                        if (client.Status != ClientStatus.Closed)
                            continue;

                        CloseClientNow(client);

                        _Clients.TryRemove(clientId, out client);
                        client = null;
                    }
                }
            }
        }

        private void MessageProcingLoop(object state)
        {
            int idx = (int)state;
            AutoResetEvent signal = _ClientMessageQueueEvents[idx];
            ConcurrentQueue<ClientMessageContext> queue = _ClientMessageQueues[idx];
            ClientMessageContext cmc = null;
            IMessage msg = null;

            while (_IsServerRunning)
            {
                signal.WaitOne();

                //  消息解析主循环，不允许出现异常未被捕获的情况
                while (queue.TryDequeue(out cmc))
                {
                    if (_MessageParser.ParseMessageFromBytes(cmc.MsgBytes, out msg))
                    {
                        //  客户端接收数据事件
                        //ClientReceivedMessagePreProc((TcpClientManager)cmc.Client, msg);

                        //  使用线程池调用上层业务方法
                        if (msg != null && cmc.Client != null)
                        {
                            cmc.Arg = msg;
                            if (!ThreadPool.QueueUserWorkItem(ClientReceivedMessageMultiThreadProc, cmc))
                            {
                                LogError("[MessageProcingLoop] QueueUserWorkItem fail.");
                            }
                        }
                        else
                        {
                            LogError(string.Format("[MessageProcingLoop] failed. arg={0}; client={1}", msg == null ? "null" : "object", cmc.Client == null ? "null" : "object"));
                        }

                        //  测试用
                        //Console.WriteLine("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss"), msg.ToString());
                    }
                    else
                    {
                        _Stat.PlusCntRecvErrMsg();

                        LogError(string.Format("[ParseMessageFromBytes] failed. bytes:{0}", cmc.MsgBytes != null ? cmc.MsgBytes.Length : 0));
                    }
                }
            }
        }



        /// <summary>
        /// 异步处理消息解析
        /// </summary>
        /// <param name="client"></param>
        /// <param name="rawMsgBytesList"></param>
        private void ParseMessageBytesAsync(IClientManager client, List<byte[]> rawMsgBytesList)
        {
            if (client == null || rawMsgBytesList == null || rawMsgBytesList.Count < 1)
            {
                //LogError(string.Format("client or rawMsgBytesList is NullOrEmpty; client={0}; bytesList={1}", client == null ? "null" : "object", rawMsgBytesList != null ? rawMsgBytesList.Count.ToString() : "null"));
                return;
            }

            //  将新消息原始数据放入队列

            //  ConcurrentQueue版
            long idx = client.ClientId % _MessageProcThreads;

            foreach (byte[] msgBytes in rawMsgBytesList)
            {
                if (msgBytes == null || msgBytes.Length < 1)
                {
                    LogError(string.Format("rawMsgBytes is NullOrEmpty"));
                    continue;
                }

                ClientMessageContext cmc = new ClientMessageContext(client, msgBytes);
                //  方式A:自定义消息队列
                _ClientMessageQueues[idx].Enqueue(cmc);

                //  方式B:使用线程池
                //ThreadPool.QueueUserWorkItem(ParseAndDeliveryMessageAsync, cmc);

                _Stat.PlusCntRecvMsg();
            }
            _ClientMessageQueueEvents[idx].Set();
        }

        private void ProcessReceive(SocketAsyncEventArgs saea)
        {
            TcpClientManager client = null;
            int bytes = 0;
            bool recvRet = false;

            try
            {
                client = (TcpClientManager)saea.UserToken;

                if (client == null || client.Status != ClientStatus.Ready)
                {
                    //LogError(string.Format("ProcessReceive failed,client status is not Ready."));
                    if (client != null)
                        ShutdownClient(client);
                    return;                     //  客户端已经关闭了
                }


                if (saea.BytesTransferred > 0 && saea.BytesTransferred <= Defines.MAX_TRANSFER_LENGTH && saea.SocketError == SocketError.Success)
                {
                    //  512K以上属于较大的消息
                    if (saea.BytesTransferred >= 524288)
                    {
                        _Logger.Info(string.Format("[ProcessReceive] Receiving big data:{0}", saea.BytesTransferred));
                    }

                    List<byte[]> rawMsgBytesList = new List<byte[]>();

                    bytes = saea.BytesTransferred;
                    recvRet = client.OnReceivedData(saea.Buffer, bytes, ref rawMsgBytesList);

                    if (recvRet)
                    {
                        //  通知上层业务，该客户端收到了新消息
                        //  一般来说，将由protocol层负责解析这些消息，并且建议解析过程不占用当前线程
                        ParseMessageBytesAsync(client, rawMsgBytesList);
                    }
                }

                if (recvRet)
                {
                    StartReceive(saea);
                    return;
                }
                else
                {
                    //LogError(string.Format("ProcessReceive failed,recvRet is false."));
                }
            }
            catch (Exception ex)
            {
                //  记录错误
                LogError(string.Format("[ProcessReceive] Exception:{0}-{1}", ex.Message, ex.StackTrace));
            }

            //  关闭连接
            ShutdownClient(client);
        }

        private void ProcessSendCompleted(SocketAsyncEventArgs saea)
        {
            TcpClientManager client = null;
            bool sendSucc = false;
            ClientMessageContext cmc = null;
            try
            {
                cmc = (ClientMessageContext)saea.UserToken;
                client = (TcpClientManager)cmc.Client;

                sendSucc = saea.SocketError == SocketError.Success ? true : false;

            }
            catch (Exception ex)
            {
                sendSucc = false;
                LogError(string.Format("[ProcessSendCompleted] Exception:{0}-{1}", ex.Message, ex.StackTrace));
            }
            finally
            {
                ReleaseWriteSaea(saea);
            }

            if (sendSucc)
            {
                client.PlusSendMsgs();
                //client.PlusSendBytes(saea.Count);

                _Stat.PlusCntSendMsg();
                //  sent完成后触发事件
                OnClientSentMessage((TcpClientManager)cmc.Client, cmc.Arg);
            }
            else
            {
                ShutdownClient(client);
            }
        }

        protected void Accept_IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
                    break;
            }
        }

        protected void ReadWrite_IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSendCompleted(e);
                    break;
            }
        }

        protected void ProcessAccept(SocketAsyncEventArgs sae)
        {
            TcpClientManager client = null;
            if (sae == null)
                return;

            int idx = (int)sae.UserToken;
            // Get the socket for the accepted client connection
            if (sae.SocketError == SocketError.Success)
            {
                Socket acceptSocket = sae.AcceptSocket;
                StartAccept(sae, idx);

                try
                {
                    SocketAsyncEventArgs clientSaea = PopReadSaea();
                    client = new TcpClientManager(acceptSocket, clientSaea, this, _DefaultClientConfigure);
                    client.HostId = idx;
                    clientSaea.UserToken = client;

                    //  注册新客户端实例
                    RegisterClient(client);

                    StartReceive(clientSaea);
                }
                catch (Exception ex)
                {
                    //ShutdownClient(client);
                    LogError(string.Format("ProcessAccept err:{0};{1}", ex.Message, ex.StackTrace));
                }
            }
            else
                StartAccept(sae, idx);
        }


        /// <summary>
        /// 供各业务调用的关闭客户端方法
        /// 注意：本方法为异步执行，业务观察者需通过关闭事件来获知关闭客户端真正完成
        /// </summary>
        /// <param name="client"></param>
        public virtual void ShutdownClient(TcpClientManager client)
        {
            if (client == null)
                return;

            //  设置
            client.SetClosed();

            _ClosingClientIdQueue.Enqueue(client.ClientId);
            _ClosingClientSignal.Set();

            OnClientClosing(client);
        }

        public bool SendMessageToClient(TcpClientManager client, IMessage msg)
        {
            //long elapse = 0;

            int bytes = 0;
            byte[] buf = _MessageParser.GetMessageBytes(msg, out bytes);

            if (buf == null || buf.Length < 1 || bytes < 1)
                return false;

            //  发送的ClientMessageContext里不对MsgBytes赋值
            ClientMessageContext cmc = new ClientMessageContext(client, null, msg);
            bool bRet = SendBytesToClient(client, buf, 0, bytes, cmc);

            buf = null;

            return bRet;
        }

        public bool SendBytesToClient(TcpClientManager client, byte[] bytes, ClientMessageContext cmc)
        {
            if (bytes == null || bytes.Length < 1)
                return false;

            return SendBytesToClient(client, bytes, 0, bytes.Length, cmc);
        }

        public bool SendBytesToClient(TcpClientManager client, byte[] bytes, int offset, int count, ClientMessageContext cmc)
        {
            if (client == null || bytes == null || bytes.Length < 1 || offset < 0 || count < 1 || offset + count > bytes.Length || count > DEFAULT_MESSAGE_MAX_LENGTH)
            {
                LogError(string.Format("[SendBytesToClient] failed:client or bytes is NullOrEmpty. bufferBytes={0}; offset={1}; count={2}", bytes != null ? bytes.Length : -1, offset, count));
                return false;
            }


            if (client.Status != ClientStatus.Ready)
            {
                LogError(string.Format("[SendBytesToClient] failed:client status is not ready."));
                return false;
            }

            if (bytes.Length >= 262144)
            {
                _Logger.Info(string.Format("[SendBytesToClient] Sending big data:{0}", bytes.Length));
            }

            SocketAsyncEventArgs saea = null;
            try
            {
                saea = PopWriteSaea();

                saea.SetBuffer(bytes, offset, count);
                //  发送的ClientMessageContext里不对MsgBytes赋值
                saea.UserToken = cmc;
                if (!client.ClientSocket.SendAsync(saea))
                {
                    ProcessSendCompleted(saea);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError(string.Format("[SendBytesToClient] failed:{0}; {1}", ex.Message, ex.StackTrace));
                ReleaseWriteSaea(saea);
            }

            return false;
        }
    }
}
