using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Core.Protocol.PureText
{
    public interface IPureTextMessage:IMessage
    {
        byte[] ToBytes(out int bytes);
    }
}
