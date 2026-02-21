using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Core.Protocol.PureText
{
    public class PureTextMessage:IPureTextMessage
    {
        protected string _Value = string.Empty;

        public PureTextMessage(string val)
        {
            _Value = val;
        }

        public byte[] ToBytes(out int bytes)
        {
            return PureTextParser.GetMessageBytes(_Value, out bytes);
        }

        public override string ToString()
        {
            return _Value;
        }
    }
}
