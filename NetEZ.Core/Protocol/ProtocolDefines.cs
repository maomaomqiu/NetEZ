using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Core.Protocol
{
    public enum ProtocolProviderEnums
    {
        /// <summary>
        /// 纯文本(UTF-8)
        /// </summary>
        PureText = 0,

        /// <summary>
        /// JMP协议，详见JMP类
        /// </summary>
        JMP = 1,

        /// <summary>
        /// Protobuf协议
        /// </summary>
        GoogleProtobuf = 2,
    }

    public enum ProtocolParserPolicy
    {
        /// <summary>
        /// 遇到错误时停止，并返回false
        /// </summary>
        ErrorStoped = 0,

        /// <summary>
        /// 除非全部错误，否则丢弃失败的消息
        /// </summary>
        ErrorIgnored = 1,
    }
}
