using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using NetEZ.Core.Event;
using NetEZ.Core.Client;

namespace NetEZ.Core.Server
{
    public class TcpClientManager : IDisposable,IClientManager
    {
        private static long _ClientIdSeed = 0;

        protected bool _Disposed = false;
        protected IService _TcpService = null;
        protected long _ClientId = 0;
        protected Socket _Socket = null;
        protected volatile ClientStatus _Status = ClientStatus.Reset;

        protected byte[] _PartialBuffer = null;
        protected int _PartialBufferOffset = 0;
        protected int _MessageMaxLength = Defines.MAX_MESSAGE_LENGTH;

        protected DateTime _LastRecvTime = DateTime.MinValue;

        protected int _TotalRecvMsgs = 0;
        protected long _TotalRecvBytes = 0;
        protected int _TotalSendMsgs = 0;
        protected long _TotalSendBytes = 0;

        protected SocketAsyncEventArgs _ReadSaea = null;

        public virtual long ClientId { get { return _ClientId; } }

        public int HostId { get; set; }

        public Socket ClientSocket { get { return _Socket; } }

        public DateTime LastRecvTime { get { return _LastRecvTime; } }

        public ClientStatus Status { get { return _Status; } }

        public SocketAsyncEventArgs ReadSaea { get { return _ReadSaea; } }

        public TcpClientManager(Socket socket, SocketAsyncEventArgs saea, IService svc,TcpClientParams clientParams = null)
        {
            _ClientId = Interlocked.Increment(ref _ClientIdSeed);

            _Socket = socket;
            _TcpService = svc;

            _ReadSaea = saea;

            SetClientConfigure(clientParams);

            SetStatus(ClientStatus.Ready);
        }

        ~TcpClientManager()
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

        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
                return;

            if (disposing)
            {
                // 清理资源
                FinalClient();
            }

            _Disposed = true;
        }

        protected virtual void FinalClient()
        {
            _ReadSaea = null;

            if (_Socket == null)
                return;

            _PartialBuffer = null;

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
        }

        private void SetStatus(ClientStatus status)
        {
            _Status = status;
        }

        private bool IsMessageLengthValid(int len)
        {
            if (0 < len && len <= _MessageMaxLength)
                return true;

            return false;
        }

        private bool ReadMessage(byte[] buffer, int bytes, ref byte[] partialBytes,ref int partialOffset, ref List<byte[]> rawMsgBytesList)
        {
            if (buffer == null || buffer.Length < 1 || bytes < 1 || bytes > buffer.Length)
                return false;

            int readBufferOffset = 0;
            
            //  解析数据包的情景
            //  1. 新数据包, 读出N（N >= 1）条消息，剩余M（M >= 0）字节是另一条消息的开头部分
            //  2. 上次剩余的字节(partialBytes)，结合本次buffer，读出N条消息，剩余M字节

            if (partialBytes != null)
            {
                //  partialBytes 定义：接收的字节数不足以构成一条完整的消息块
                //  处理partialBytes的几种结果
                //  1. partialBytes 本身不完整（长度小于4），没有在构建时固定长度，因此需要在收到后续字节后重构partialBytes
                //  2. partialBytes 本次从接收缓冲区填补完成
                //  3. partialBytes 本次从接收缓冲区填补字节，但仍未完成(整个消息字节块)

                if (partialBytes.Length < 4)
                {
                    //  上次剩余的字节不足4个，因此partialBytes的长度没有预先固定，本次需要合并partialBytes与buffer
                    byte[] tmpBuf = new byte[partialBytes.Length + bytes];
                    Buffer.BlockCopy(partialBytes, 0, tmpBuf, 0, partialBytes.Length);
                    Buffer.BlockCopy(buffer, 0, tmpBuf, partialBytes.Length, bytes);

                    partialBytes = null;            //  重置
                    partialOffset = 0;              //  重置
                    //  由于合并了 partialBytes+buffer，将合并后缓冲字节以无partial的方式递归调用
                    if (ReadMessage(tmpBuf, tmpBuf.Length, ref partialBytes, ref partialOffset, ref rawMsgBytesList))
                    {
                        return true;
                    }
                    else
                        return false;
                }
                else
                {
                    //  上次的partialBytes预留了长度，本次从partialOffset处开始填补收到的字节
                    if (partialBytes.Length - partialOffset <= bytes)
                    {
                        //  本次收到的字节填补了上次的partialBytes，而且填完
                        Buffer.BlockCopy(buffer, 0, partialBytes, partialOffset, partialBytes.Length - partialOffset);
                        //  将当前完成的消息字节块加入List
                        if (rawMsgBytesList == null)
                            rawMsgBytesList = new List<byte[]>();

                        rawMsgBytesList.Add(partialBytes);

                        //  read偏移量增加
                        //  设置readBuffer，表示后面还要从剩余的buffer里尝试读出其他消息字节块
                        readBufferOffset += partialBytes.Length - partialOffset;

                        partialBytes = null;            //  重置
                        partialOffset = 0;              //  重置
                    }
                    else
                    {
                        //  本次收到的字节填补了上次的partialBytes，但本次没有填完
                        Buffer.BlockCopy(buffer, 0, partialBytes, partialOffset, bytes);
                        partialOffset += bytes;

                        return true;
                    }
                }
            }

            int msgLen = 0;
            while (readBufferOffset < bytes)
            {
                if (readBufferOffset + 4 <= bytes)
                {
                    //  每个消息块的前面都应有4字节，表示消息块的长度
                    msgLen = BitConverter.ToInt32(buffer, readBufferOffset);

                    readBufferOffset += 4;
                    //  消息块的长度必须合法
                    if (!IsMessageLengthValid(msgLen))
                        return false;

                    byte[] rawMsgBytes = new byte[msgLen];

                    if (msgLen <= bytes - readBufferOffset)
                    {
                        //  如果后面的接收区有足够的字节可以读进来
                        Buffer.BlockCopy(buffer, readBufferOffset, rawMsgBytes, 0, msgLen);
                        readBufferOffset += msgLen;

                        if (rawMsgBytesList == null)
                            rawMsgBytesList = new List<byte[]>();

                        rawMsgBytesList.Add(rawMsgBytes);
                    }
                    else
                    {
                        //  后面的接收区没有足够的字节
                        //  先复制目前接收的字节
                        Buffer.BlockCopy(buffer, readBufferOffset, rawMsgBytes, 0, bytes - readBufferOffset);

                        //  当前未完成的消息字节块暂存到partialBytes,并记录偏移量
                        partialBytes = rawMsgBytes;
                        partialOffset = bytes - readBufferOffset;

                        readBufferOffset = bytes;
                    }
                }
                else
                { 
                    //  剩余的接收缓冲区还有不到4个字节
                    partialBytes = new byte[bytes - readBufferOffset];
                    partialOffset = 0;

                    Buffer.BlockCopy(buffer, readBufferOffset, partialBytes, 0, partialBytes.Length);
                    readBufferOffset = bytes;
                }

            }

            return true;
        }

        public void SetClientConfigure(TcpClientParams clientParams = null)
        {
            if (clientParams == null)
            {
                //  设置默认值
                _MessageMaxLength = Defines.MAX_MESSAGE_LENGTH;
            }
            else
            {
                if (clientParams.MessageMaxLength >= 1024)
                    _MessageMaxLength = clientParams.MessageMaxLength;
                else
                    _MessageMaxLength = Defines.MAX_MESSAGE_LENGTH;
            }
        }

        public virtual bool OnReceivedData(byte[] buffer, int bytes, ref List<byte[]> rawMsgBytesList)
        {
            //if (_OnReceivedHandler != null)
            //    _OnReceivedHandler(this, buffer, bytes);
            lock (this)
            {
                if (ReadMessage(buffer, bytes, ref _PartialBuffer, ref _PartialBufferOffset, ref rawMsgBytesList))
                {
                    return true;
                }

                return false;
            }
        }


        public int GetTotalRecvMsgs() { return _TotalRecvMsgs; }
        public long GetTotalRecvBytes() { return _TotalRecvBytes; }
        public int GetTotalSendMsgs() { return _TotalSendMsgs; }
        public long GetTotalSendBytes() { return _TotalSendBytes; }

        public void PlusRecvMsgs(int plus = 1)
        {
            if (plus < 1)
                return;

            Interlocked.Increment(ref _TotalRecvMsgs);
        }

        public void PlusRecvBytes(int bytes)
        {
            if (bytes < 1)
                return;

            Interlocked.Increment(ref _TotalRecvBytes);
        }

        public void PlusSendMsgs(int plus = 1)
        {
            if (plus < 1)
                return;

            Interlocked.Increment(ref _TotalSendMsgs);
        }

        public void PlusSendBytes(int bytes)
        {
            if (bytes < 1)
                return;

            Interlocked.Increment(ref _TotalSendBytes);
        }

        public void SetClosed() 
        {
            SetStatus(ClientStatus.Closed);
        }

        public void RefreshLastRecvTime() { _LastRecvTime = DateTime.Now; }
    }
}
