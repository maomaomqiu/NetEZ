using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using NetEZ.Core.IO;

namespace NetEZ.Core.Client
{
    /*
    internal class WriteSaeaPool
    {
        private static SocketAsyncEventArgsPool _Pool = new SocketAsyncEventArgsPool();

        /// <summary>
        /// 返回SocketAsyncEventArgs池中的 数量
        /// </summary>
        public static int Count
        {
            get
            {
                return _Pool.Count;
            }
        }

        /// <summary>
        /// 弹出一个SocketAsyncEventArgs
        /// </summary>
        /// <returns></returns>
        public static SocketAsyncEventArgs Pop(EventHandler<SocketAsyncEventArgs> eventHandler)
        {
            bool inited = false;
            SocketAsyncEventArgs saea = _Pool.Pop(false, out inited);
            if (!inited)
                saea.Completed += eventHandler;

            return saea;
        }

        /// <summary>
        /// 添加一个 SocketAsyncEventArgs
        /// </summary>
        /// <param name="item">SocketAsyncEventArgs instance to add to the pool.</param>
        public static void Push(SocketAsyncEventArgs saea)
        {
            if (saea == null)
                return;
            saea.UserToken = null;
            _Pool.Push(saea);
        }
    }*/
}
