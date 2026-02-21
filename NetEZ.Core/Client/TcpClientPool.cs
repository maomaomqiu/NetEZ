using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEZ.Core.Client;
using NetEZ.Core.IO;
using NetEZ.Core.Event;
using NetEZ.Core.Protocol;
using NetEZ.Core.Protocol.JMP;
using NetEZ.Utility.Logger;

namespace NetEZ.Core.Client
{
    public class TcpClientPool
    {
        const int MAX_CLIENTS = 10;

        private ConcurrentStack<TcpClientBase> _ClientStack = new ConcurrentStack<TcpClientBase>();
        private Logger _Logger = null;
        private long _ClientIdSeed = 0;
        private int _MaxClients = MAX_CLIENTS;
        private int _CurrClients = 0;
        private string _Host = string.Empty;
        private int _Port = 0;
        private TcpClientParams _Params = null;
        private SocketAsyncEventArgsPool _SaeaPool = null;
        public event OnConnected OnConnectedCallback = null;
        public event OnDisConnected OnDisConnectedCallback = null;
        public event OnRecvServerData OnRecvServerDataCallback = null;
        public event OnSendCompleted OnSendCompletedCallback = null;

        public int Totals { get { return _CurrClients; } }
        public int Count { get { return _ClientStack.Count; } }

        public TcpClientPool(int maxClients = MAX_CLIENTS, Logger logger = null,TcpClientParams clientParams = null)
        {
            _Logger = logger;

            if (maxClients > 0)
                _MaxClients = maxClients;

            _Params = clientParams != null ? clientParams : new TcpClientParams();

            _SaeaPool = new SocketAsyncEventArgsPool();
        }

        private void Info(string msg)
        {
            if (_Logger != null && !string.IsNullOrEmpty(msg))
                _Logger.Info(msg);
        }

        private void Debug(string msg)
        {
            if (_Logger != null && !string.IsNullOrEmpty(msg))
                _Logger.Debug(msg);
        }

        private void Error(string msg)
        {
            if (_Logger != null && !string.IsNullOrEmpty(msg))
                _Logger.Error(msg);
        }

        public bool Start(string host, int port)
        {
            if (string.IsNullOrEmpty(host) || port < 1)
                return false;

            _Host = host;
            _Port = port;

            return true;
        }

        public void ReleaseClient(TcpClientBase client)
        {
            if (client == null || client.Status != ClientStatus.Ready)
            {
                Interlocked.Decrement(ref _CurrClients);
                if (client != null)
                {
                    try { client.Dispose(); }
                    catch { }
                    finally { client = null; }
                }
                return;
            }

            _ClientStack.Push(client);
        }

        public TcpClientBase GetClient(int timeOutMs = 0)
        {
            TcpClientBase client = null;

            while (true)
            {
                if (!_ClientStack.TryPop(out client))
                    break;          //  没有可用客户端

                if (client.Status != ClientStatus.Ready)
                {
                    //  client已经失效
                    try { client.Dispose(); }
                    catch { }
                    finally { client = null; Interlocked.Decrement(ref _CurrClients); }
                    continue;
                }

                return client;
            }
            

            if (timeOutMs < 1)
                timeOutMs = _Params.ConnectTimeOut;

            //  如果累计实例数已达上限,只能循环等待可用对象
            int cnt = 0;
            DateTime timeOutTime = DateTime.Now.AddMilliseconds(timeOutMs);
            while (_CurrClients >= _MaxClients && !_ClientStack.TryPop(out client) && DateTime.Now < timeOutTime)
            {
                if (cnt++ < 100)
                    Thread.Sleep(3);
                else
                    Thread.Sleep(6);
            }

            if (client != null)
                return client;

            //  如果超时则返回null
            if (DateTime.Now >= timeOutTime)
                return null;                

            long clientId = Interlocked.Increment(ref _ClientIdSeed);
            client = new TcpClientBase(clientId, _Logger, _Params, _SaeaPool);
            client.OnConnectedCallback += OnConnected;
            client.OnDisConnectedCallback += OnDisConnected;
            client.OnRecvServerDataCallback += OnRecvServerData;
            client.OnSendCompletedCallback += OnSendCompleted;
            if (client.Connect(_Host, _Port, timeOutMs))
            {
                Interlocked.Increment(ref _CurrClients);
                return client;
            }

            return null;
        }

        private void OnConnected(IClient client, int code,string msg)
        {
            if (OnConnectedCallback != null)
                OnConnectedCallback(client, code, msg);
        }

        private void OnDisConnected(IClient client, int code, string msg)
        {
            if (OnDisConnectedCallback != null)
            {
                OnDisConnectedCallback(client, code, msg);
            }

            if (client != null)
            {
                TcpClientBase tcpClient = (TcpClientBase)client;
                tcpClient.Close(false);
            }
            
            //Interlocked.Decrement(ref _CurrClients);
        }

        private void OnRecvServerData(IClient client, byte[] rawMsgBytesList)
        {
            if (OnRecvServerDataCallback != null)
                OnRecvServerDataCallback(client, rawMsgBytesList);
        }

        private void OnSendCompleted(IClient client, object state, int code)
        {
            if (OnSendCompletedCallback != null)
                OnSendCompletedCallback(client, state, code);
        }
    }
}
