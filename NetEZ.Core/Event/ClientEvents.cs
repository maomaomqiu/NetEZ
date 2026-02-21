using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEZ.Core.Client;
using NetEZ.Core.Protocol;

namespace NetEZ.Core.Event
{
    /*===================================== 客户端事件 =====================================*/
    
    public delegate void OnConnected(IClient client, int code,string msg);
    public delegate void OnDisConnected(IClient client, int code, string msg);
    public delegate void OnRecvServerData(IClient client, byte[] rawMsgBytesList);
    public delegate void OnSendCompleted(IClient client, object state, int code);

}
