using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NetEZ.Core.Protocol.JMP
{
    public class JMPMessaeBase:IJMPMessage
    {
        protected string _Signal = string.Empty;

        public string GetSignal() { return _Signal; } 

        public virtual string GetInstanceJson() { return JsonConvert.SerializeObject(this); }

        /// <summary>
        /// 子类需要在构造函数里初始化_Signal
        /// </summary>
        protected JMPMessaeBase()
        {
            _Signal = string.Empty;
        }

        /// <summary>
        /// 必要时可以重写本方法
        /// </summary>
        /// <returns></returns>
        public virtual byte[] ToBytes(out int bytes)
        {
            return JMPParser.GetMessageBytes(this,out bytes);
        }
    }
}
