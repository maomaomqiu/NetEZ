using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEZ.Core.Event;
using NetEZ.Core.Server;
using NetEZ.Core.Protocol;

namespace NetEZ.Core.Server
{
    public interface IService
    {
        string ServiceName { get; }
        
        int ClientReceivingBufferSize { get; }

        void RegisterMessageParser(IProtocolParser parser);
        void RegisterOnClientConnectedCallback(OnClientConnectedEvent handler);
        void RegisterOnClientClosingCallback(OnClientClosingEvent handler);
        void RegisterOnClientDisconnectedCallback(OnClientDisconnectedEvent handler);
        void RegisterOnClientReceivedCallback(OnClientReceivedEvent handler);
        void RegisterOnClientSentCallback(OnClientSentEvent handler);

        bool Start();
        void Stop();
        
    }
}
