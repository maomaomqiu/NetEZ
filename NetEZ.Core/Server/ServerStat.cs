using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Core.Server
{
    public class ServerStat
    {
        /// <summary>
        /// 固有变量
        /// </summary>
        protected DateTime _StartTime = DateTime.MinValue;
        protected int _CurrConnections = 0;
        protected int _CurrUsers = 0;
        protected long _CntRecvMsg = 0;
        protected long _CntRecvErrMsg = 0;
        protected long _CntSendMsg = 0;
        protected long _CntPing = 0;

        /// <summary>
        /// 自定义变量表
        /// </summary>
        protected ConcurrentDictionary<string, long> _Variables = new ConcurrentDictionary<string, long>();

        /// <summary>
        /// 启动时间
        /// </summary>
        public DateTime StartTime { get { return _StartTime; } }

        /// <summary>
        /// 运行时间
        /// </summary>
        public int Duration { get { return _StartTime > DateTime.MinValue ? (int)DateTime.Now.Subtract(_StartTime).TotalSeconds : 0; } }

        public void SetStart() { _StartTime = DateTime.Now; }

        public long CntRecvMsg { get { return _CntRecvMsg; } }
        public long CntSendMsg { get { return _CntSendMsg; } }
        public long CntPing { get { return _CntPing; } }

        private long _SnapCntRecvMsg;
        private long _SnapCntSendMsg;
        private DateTime _SnapTime = DateTime.MinValue;

        public void GetSnap(out double avgRecvMsgPS, out double avgSendMsgPS)
        {
            if (_SnapTime == DateTime.MinValue)
            {
                avgRecvMsgPS = avgSendMsgPS = 0;
                _SnapTime = DateTime.Now;
                return;
            }

            
            long cntRecvMsg = _CntRecvMsg;
            long cntSendMsg = _CntSendMsg;
            double totalMs = DateTime.Now.Subtract(_SnapTime).TotalMilliseconds;
            
            _SnapTime = DateTime.Now;

            avgRecvMsgPS = (cntRecvMsg - _SnapCntRecvMsg) * 1000.0 / totalMs;
            avgSendMsgPS = (cntSendMsg - _SnapCntSendMsg) * 1000.0 / totalMs;

            _SnapCntRecvMsg = cntRecvMsg;
            _SnapCntSendMsg = cntSendMsg;
        }

        public void PlusCurrConnection()
        {
            Interlocked.Increment(ref _CurrConnections);
        }
        public void DecreCurrConnection()
        {
            Interlocked.Decrement(ref _CurrConnections);
        }

        public void ExchangeCurrConnection(int conns)
        {
            if (conns < 0)
                conns = 0;

            Interlocked.Exchange(ref _CurrConnections, conns);
        }

        public void PlusCurrUsers()
        {
            Interlocked.Increment(ref _CurrUsers);
        }

        public void DecreCurrUsers()
        {
            Interlocked.Decrement(ref _CurrUsers);
        }

        public void ExchangeCurrUsers(int users)
        {
            if (users < 0)
                users = 0;

            Interlocked.Exchange(ref _CurrUsers, users);
        }

        public void PlusCntRecvMsg(int plus = 1)
        {
            Interlocked.Add(ref _CntRecvMsg, plus);
        }

        public void PlusCntRecvErrMsg(int plus = 1)
        {
            Interlocked.Add(ref _CntRecvErrMsg, plus);
        }

        public void PlusCntSendMsg(int plus = 1)
        {
            Interlocked.Add(ref _CntSendMsg, plus);
        }

        public void PlusCntPing(int plus)
        {
            Interlocked.Add(ref _CntPing, plus);
        }

        public void SetVariable(string varName, long val)
        {
            varName = !string.IsNullOrEmpty(varName) ? varName.Trim().ToLower() : "";
            if (varName.Length < 1)
                return;

            _Variables.AddOrUpdate(varName, val, (k, v) => val);
        }

        public bool GetVariable(string varName,out long val)
        {
            val = -1;

            varName = !string.IsNullOrEmpty(varName) ? varName.Trim().ToLower() : "";
            if (varName.Length < 1)
                return false;

            return _Variables.TryGetValue(varName, out val);
        }
    }
}
