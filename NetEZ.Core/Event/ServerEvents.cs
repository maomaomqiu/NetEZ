using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEZ.Core.Server;
using NetEZ.Core.Protocol;
using NetEZ.Core.Client;

namespace NetEZ.Core.Event
{
    /*===================================== 服务器响应事件 =====================================*/

    /// <summary>
    /// client连接成功的事件
    /// </summary>
    /// <param name="client"></param>
    public delegate void OnClientConnectedEvent(IClientManager client);


    /// <summary>
    /// client正在断开的事件
    /// </summary>
    /// <param name="client"></param>
    public delegate void OnClientClosingEvent(IClientManager client);

    /// <summary>
    /// client已经断开后的事件
    /// </summary>
    /// <param name="client"></param>
    public delegate void OnClientDisconnectedEvent(IClientManager client);

    /// <summary>
    /// client发来数据
    /// </summary>
    /// <param name="client"></param>
    /// <param name="bytes"></param>
    public delegate void OnClientReceivedEvent(IClientManager client, IMessage msg);

    /// <summary>
    /// 消息到来事件;Server内部事件
    /// </summary>
    /// <param name="cmc"></param>
    public delegate void OnMessageArrivedEvent(ClientMessageContext cmc);

    /// <summary>
    /// 向客户端发送数据后事件
    /// </summary>
    /// <param name="client"></param>
    /// <param name="state"></param>
    public delegate void OnClientSentEvent(IClientManager client, object state);

    /*
    public class ClientMessageContextBatchHandler<T> : IBatchHandler<ClientMessageContext>
    {

        public OnMessageArrivedEvent OnMessageArrivedCallback = null;

        public void OnAvailable(ClientMessageContext entry)
        {
            OnMessageArrivedCallback(entry);
        }

        public void OnEndOfBatch()
        {

        }

        public void OnCompletion()
        {

        }
    }*/
}
