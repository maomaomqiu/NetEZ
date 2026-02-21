using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEZ.Core.Server;
using NetEZ.Core.Protocol;
using NetEZ.Core.Event;
using NetEZ.Core.IO;

namespace NetEZ.Core.Client
{
    public class ClientMessageContext
    {
        public IClient Client;
        public byte[] MsgBytes;
        public object Arg;

        public void Reset()
        {
            Client = null;
            MsgBytes = null;
        }

        public ClientMessageContext() { }

        public ClientMessageContext(IClient client, byte[] msgBytes,object arg = null)
        {
            Client = client;
            MsgBytes = msgBytes;
            Arg = arg;
        }
    }

    /*
    internal class ClientMessageContextFactory : IEntryFactory<ClientMessageContext>
    {
        public ClientMessageContext Create()
        {
            return new ClientMessageContext();
        }
    }
    */
    
}
