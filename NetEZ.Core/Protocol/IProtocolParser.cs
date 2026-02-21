using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Core.Protocol
{
    public interface IProtocolParser
    {
        bool ParseMessageFromBytes(byte[] rawMsgBytesList, out IMessage msg);

        byte[] GetMessageBytes(IMessage msg,out int bytes);
    }
}
