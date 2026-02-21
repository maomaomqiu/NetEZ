using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Core.Protocol.JMP
{
    public interface IJMPMessage : IMessage
    {
        string GetSignal();

        string GetInstanceJson();

        byte[] ToBytes(out int bytes);
    }
}
