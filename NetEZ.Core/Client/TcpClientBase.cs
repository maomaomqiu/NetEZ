using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NetEZ.Core.IO;
using NetEZ.Utility.Logger;
using NetEZ.Core.Event;

namespace NetEZ.Core.Client
{
    public class TcpClientBase : IDisposable, IClient
    {
        private BinaryIOBuffer _BinIOBuffer = null;
        private TcpClientParams _ClientParams = null;

        protected bool _Disposed = false;
        protected long _ClientId = 0;
        protected Socket _Socket;
        protected byte[] _RecvBuffer = null;
        protected SocketAsyncEventArgs _ReadSaea = null;
        protected SocketAsyncEventArgs _WriteSaea = null;
        protected SocketAsyncEventArgsPool _SaeaPool = null;
        protected AutoResetEvent _WriteSaeaEvent = new AutoResetEvent(false);

        //protected SocketAsyncEventArgs _WriteSaea = null;
        protected Logger _Logger = null;
        protected ClientStatus _Status = ClientStatus.Reset;

        public event OnConnected OnConnectedCallback = null;
        public event OnDisConnected OnDisConnectedCallback = null;
        public event OnRecvServerData OnRecvServerDataCallback = null;
        public event OnSendCompleted OnSendCompletedCallback = null;

        public ClientStatus Status { get { return _Status; } }

        public TcpClientBase(long clientId, Logger logger, TcpClientParams clientParams = null,SocketAsyncEventArgsPool saeaPool = null)
        {
            _ClientId = clientId;
            InitClient(clientParams, saeaPool);
            _Logger = logger;
        }


        ~TcpClientBase()
        {
            Dispose();
        }

        public void Dispose()
        {
            //  释放所有的资源
            Dispose(true);
            //不需要再调用本对象的Finalize方法
            GC.SuppressFinalize(this);
        }

        public long ClientId { get { return _ClientId; } }

        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
                return;

            if (disposing)
            {
                // 关闭socket
                Close(false);
                // 清理资源
                FinalClient();

            }

            _Disposed = true;
        }

        private void InitClient(TcpClientParams clientParams = null,SocketAsyncEventArgsPool saeaPool = null)
        {
            if (clientParams == null)
                clientParams = new TcpClientParams();

            _ClientParams = clientParams;
            //  初始化接收数据缓冲区，以及上下文对象
            _RecvBuffer = new byte[_ClientParams.ReceiveBufferSize];

            _ReadSaea = new SocketAsyncEventArgs();
            _ReadSaea.SetBuffer(_RecvBuffer, 0, _RecvBuffer.Length);
            _ReadSaea.Completed += new EventHandler<SocketAsyncEventArgs>(Read_IO_Completed);

            if (saeaPool == null)
            {
                //  如果不使用对象池则创建默认Saea对象
                _WriteSaea = new SocketAsyncEventArgs();
                _WriteSaea.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            }
            else
                _SaeaPool = saeaPool;

            
            
            _BinIOBuffer = new BinaryIOBuffer(_ClientParams.MessageMinLength, _ClientParams.MessageMaxLength);
        }

        private void FinalClient()
        {
            try
            {
                _ReadSaea.Dispose();
            }
            catch { }
            finally
            {
                _ReadSaea = null;
            }

            _RecvBuffer = null;

            _BinIOBuffer = null;

        }

        

        protected void LogInfo(string log)
        {
            if (!_ClientParams.EnableLogger)
                return;

            if (_Logger != null && !string.IsNullOrEmpty(log))
                _Logger.Info(log);
        }

        protected void LogDebug(string log)
        {
            if (!_ClientParams.EnableLogger)
                return;

            if (_Logger != null && !string.IsNullOrEmpty(log))
                _Logger.Debug(log);
        }

        protected void LogError(string log)
        {
            if (!_ClientParams.EnableLogger)
                return;

            if (_Logger != null && !string.IsNullOrEmpty(log))
                _Logger.Error(log);
        }

        public void Close(bool launchEvent = true)
        {
            if (_BinIOBuffer != null)
                _BinIOBuffer.Reset();

            //  尝试关闭socket
            if (_Socket == null)
                return;

            ClientStatus status = _Status;

            _Status = ClientStatus.Reset;
            try
            {
                //  Disconnect首先等同于Shutdown(both);其次会以阻塞方式完成2项工作：尝试发完数据；发送0字节消息以通知远端
                _Socket.Disconnect(false);

            }
            catch (Exception ex) { }
            finally
            {
                try
                {
                    //  Dispose等效于无timeout参数的Close,
                    _Socket.Dispose();
                }
                catch { }
                finally { _Socket = null; }
            }

            if (status != ClientStatus.Reset && launchEvent)
                LaunchDisConnectedEvent(0);

        }

        private SocketAsyncEventArgs GetSaea()
        {
            if (_WriteSaea != null)
                return _WriteSaea;

            bool inited = false;
            SocketAsyncEventArgs saea = _SaeaPool.Pop(false, out inited);
            saea.Completed += IO_Completed;

            return saea;
        }

        private void Release(SocketAsyncEventArgs saea)
        {
            if (saea != null && _SaeaPool != null)
            {
                saea.SetBuffer(null, 0, 0);
                saea.Completed -= IO_Completed;
                _SaeaPool.Push(saea);
            }
        }

        private bool OpenSocket(string host, int port)
        {
            _WriteSaeaEvent.Reset();
            _Status = ClientStatus.Reset;
            bool openSucc = false;

            try
            {
                IPAddress ipAddress = IPAddress.Parse(host);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                SocketAsyncEventArgs saea = GetSaea();
                saea.RemoteEndPoint = remoteEP;

                if (_Socket != null)
                {
                    try { _Socket.Shutdown(SocketShutdown.Both); _Socket.Disconnect(true); _Socket.Close(); }
                    catch { }
                    finally { }
                }
                else
                {
                    _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _Socket.SendTimeout = _ClientParams.SendTimeout;
                }

                try
                {
                    _Socket.ConnectAsync(saea);

                    openSucc = true;
                }
                catch {  }
            }
            catch 
            {
                
            }
            finally { }

            if (!openSucc)
            { 
                LaunchConnectedEvent((int)SocketError.SocketError);
            }

            return openSucc;
        }
        
        /// <summary>
        /// 同步Connect
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool Connect(string host, int port,int timeOutMs = 0)
        {
            if (timeOutMs < 1)
                timeOutMs = 9000;

            ConnectAsync(host, port);
            //_WriteSaeaEvent.WaitOne(_ClientParams.ConnectTimeOut);
            _WriteSaeaEvent.WaitOne(timeOutMs);
            if (_Status != ClientStatus.Ready)
            {
                Close(false);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 异步Connect
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void ConnectAsync(string host, int port)
        {
            OpenSocket(host, port);
        }

        private void ProcessConnected(SocketAsyncEventArgs saea)
        {
            SocketError code = saea.SocketError;
            Release(saea);

            LaunchConnectedEvent((int)code);

            if (code == SocketError.Success)
            {
                _WriteSaeaEvent.Set();
                StartReceive();
            }
        }

        private void StartReceive()
        {
            try
            {
                if (!_Socket.ReceiveAsync(_ReadSaea))
                {
                    ProcessServerDataArrived();
                }

                return;
            }
            catch (Exception ex)
            {
                //  记录错误
                LogError(string.Format("StartReceive err:{0}; line:{1}", ex.Message, ex.StackTrace));
            }
            finally { }

            //  关闭连接
            Close();
        }

        protected virtual void LaunchConnectedEvent(int code)
        {
            if (code == 0)
                _Status = ClientStatus.Ready;
            else
                _Status = ClientStatus.Reset;

            //_WriteSaeaEvent.Set();

            if (OnConnectedCallback != null)
                OnConnectedCallback(this, code, "");
        }

        protected virtual void LaunchDisConnectedEvent(int code)
        {
            if (OnDisConnectedCallback != null)
                OnDisConnectedCallback(this, code, "");
        }

        protected virtual void LaunchRecvServerDataEvent(List<byte[]> rawMsgBytesList)
        {
            if (OnRecvServerDataCallback != null && rawMsgBytesList != null && rawMsgBytesList.Count > 0)
            {
                foreach (byte[] rawmsgBytes in rawMsgBytesList)
                {
                    OnRecvServerDataCallback(this, rawmsgBytes);
                }
            }
                
        }

        protected virtual void LaunchSendCompletedEvent(object state, int code)
        {
            if (OnSendCompletedCallback != null)
                OnSendCompletedCallback(this, state, code);
        }

        private void ProcessServerDataArrived()
        {
            int bytes = 0;
            bool recvRet = false;

            try
            {
                if (_ReadSaea.BytesTransferred > 0 && _ReadSaea.SocketError == SocketError.Success)
                {

                    List<byte[]> rawMsgBytesList = new List<byte[]>();

                    bytes = _ReadSaea.BytesTransferred;
                    recvRet = _BinIOBuffer.ReadMessage(_ReadSaea.Buffer, bytes, ref rawMsgBytesList);

                    if (recvRet)
                    {
                        //  通知上层业务，该客户端收到了新消息
                        //  一般来说，将由protocol层负责解析这些消息，并且建议解析过程不占用当前线程
                        LaunchRecvServerDataEvent(rawMsgBytesList);

                        StartReceive();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                //  记录错误
                LogError(string.Format("ProcessServerDataArrived err:{0}; line:{1}", ex.Message, ex.StackTrace));
            }

            //  关闭连接
            Close();
        }

        private void ProcessSendCompleted(SocketAsyncEventArgs saea)
        {
            bool sendSucc = false;
            ClientMessageContext cmc = null;

            cmc = (ClientMessageContext)saea.UserToken;
            sendSucc = saea.SocketError == SocketError.Success ? true : false;

            Release(saea);

            //  sent完成后触发事件
            LaunchSendCompletedEvent(cmc != null ? cmc.Arg : null, (int)saea.SocketError);

            if (!sendSucc)
            {
                Close();
            }
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs saea)
        {
            switch (saea.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnected(saea);
                    break;
                //case SocketAsyncOperation.Receive:
                //    ProcessServerDataArrived();
                //    break;
                case SocketAsyncOperation.Send:
                    ProcessSendCompleted(saea);
                    break;
                default:
                    break;
            }
        }

        private void Read_IO_Completed(object sender, SocketAsyncEventArgs saea)
        {
            switch (saea.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessServerDataArrived();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="arg"></param>
        public bool SendBytesAsync(byte[] buffer, int offset, int count,object arg)
        {
            if (_Status != ClientStatus.Ready)
            {
                LogError(string.Format("[SendBytesAsync] failed: _Status is not ready"));
                return false;
            }

            if (buffer == null || buffer.Length < 1 || buffer.Length > _ClientParams.MessageMaxLength)
            {
                LogError(string.Format("[SendBytesAsync] failed: buffer bytes={0}", buffer != null ? buffer.Length.ToString() : "null"));
                return false;
            }
                
            SocketAsyncEventArgs saea = GetSaea();

            try
            {
                saea.SetBuffer(buffer, offset, count);
                ClientMessageContext cmc = new ClientMessageContext(this, null, arg);
                saea.UserToken = cmc;
                if (!_Socket.SendAsync(saea))
                {
                    ProcessSendCompleted(saea);
                }

                return true;
            }
            catch (Exception ex )
            {
                LogError(string.Format("[SendBytesAsync] failed:{0}", ex.Message));
            }
            finally { }

            return false;
        }


        /// <summary>
        /// 同步发送
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="bytes"></param>
        /// <param name="arg"></param>
        public bool SendBytes(byte[] buffer, int offset, int bytes)
        {
            //_Socket.Send(buffer, offset, count, SocketFlags.None);
            return SendBytesAsync(buffer, offset, bytes, null);
        }
    }
}
