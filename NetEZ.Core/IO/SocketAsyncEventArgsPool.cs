using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace NetEZ.Core.IO
{
    public class SocketAsyncEventArgsPool
    {
        const uint BUFFER_READWRITE_MIN_SIZE = 16;
        const uint BUFFER_READWRITE_DEFAULT_SIZE = 4096;
        const uint MAX_SAEA_COUNT = 50000;

        private int _TotalSaeas = 0;
        private uint _MaxSaeas = MAX_SAEA_COUNT;

        /// <summary>
        /// SocketAsyncEventArgs栈
        /// </summary>
        private ConcurrentQueue<SocketAsyncEventArgs> _Pool;
        //private ConcurrentStack<SocketAsyncEventArgs> _Pool;
        private uint _BufferSize = BUFFER_READWRITE_DEFAULT_SIZE;

        public SocketAsyncEventArgsPool(uint buffSize = BUFFER_READWRITE_DEFAULT_SIZE, uint maxItems = MAX_SAEA_COUNT)
        {
            _BufferSize = buffSize < BUFFER_READWRITE_MIN_SIZE ? BUFFER_READWRITE_DEFAULT_SIZE : buffSize;
            _MaxSaeas = maxItems > 0 ? maxItems : MAX_SAEA_COUNT;
            _Pool = new ConcurrentQueue<SocketAsyncEventArgs>();
        }

        /// <summary>
        /// 返回SocketAsyncEventArgs池中的 数量
        /// </summary>
        public int Count { get { return _Pool.Count; } }

        /// <summary>
        /// 累计创建的对象数
        /// </summary>
        public int TotalSaeas
        {
            get { return _TotalSaeas; }
        }

        /// <summary>
        /// 弹出一个SocketAsyncEventArgs
        /// </summary>
        /// <returns></returns>
        public SocketAsyncEventArgs Pop(bool buffEnabled, out bool inited)
        {
            inited = false;
            SocketAsyncEventArgs result = null;

            if (!_Pool.TryDequeue(out result))
            {
                
                if (_TotalSaeas >= _MaxSaeas)
                {
                    //  队列没有元素，而且累计创建的对象数达到上限，此时必须等待可用对象
                    int retries = 0;
                    while (!_Pool.TryDequeue(out result))
                    {
                        if (retries ++ < 100)
                            Thread.Sleep(1);
                        else
                            Thread.Sleep(3);
                    }

                    inited = true;

                    //Console.WriteLine("\r[{0}] Pop Retry({1}) success.\r", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), retries);
                }
                else
                {
                    //  队列没有元素，而且累计创建的对象数未到上限，此时创建新的对象实例
                    result = new SocketAsyncEventArgs();
                    Interlocked.Increment(ref _TotalSaeas);

                    if (buffEnabled)
                    {
                        //  recv saea需要指定buffer,发送saea不需要
                        byte[] buf = new byte[_BufferSize];
                        result.SetBuffer(buf, 0, buf.Length);
                    }
                }
                
            }
            else
                inited = true;

            
            return result;
        }

        /// <summary>
        /// 添加一个 SocketAsyncEventArgs
        /// </summary>
        /// <param name="item">SocketAsyncEventArgs instance to add to the pool.</param>
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                Interlocked.Decrement(ref _TotalSaeas);
                return;
            }
                

            //if (_Pool.Count >= _MaxSaeas / 2)
            //{ 
            //    //  可用对象足够多，温和回收
            //    try
            //    {
            //        item.Dispose();
            //    }
            //    catch { }
            //    finally { item = null; }

            //    //  总数-1
            //    Interlocked.Decrement(ref _TotalSaeas);

            //    return;
            //}

            _Pool.Enqueue(item);
        }
    }
}
