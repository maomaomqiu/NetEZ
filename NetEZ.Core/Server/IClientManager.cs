using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEZ.Core.Event;
using NetEZ.Core.Client;

namespace NetEZ.Core.Server
{
    public interface IClientManager:IClient
    {
        int HostId { get; set; }
        DateTime LastRecvTime { get; }
        ClientStatus Status { get; }

        int GetTotalRecvMsgs();
        long GetTotalRecvBytes();
        int GetTotalSendMsgs();
        long GetTotalSendBytes();

        void PlusRecvMsgs(int plus = 1);
        void PlusRecvBytes(int bytes);
        void PlusSendMsgs(int plus = 1);
        void PlusSendBytes(int bytes);

        void SetClosed();
        void RefreshLastRecvTime();

    }
}
