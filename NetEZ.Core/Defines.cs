using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Core
{
    public class Defines
    {
        //  接收消息时最大支持数据长度;
        //  不等于消息最大长度，因为有可能粘包
        public const int MAX_TRANSFER_LENGTH = 1024 * 1024 * 4;

        public const int MAX_MESSAGE_LENGTH = 1024 * 512;

        public const int SUCCESS = 0;
        public const int SYS_FAIL_BASE = 1000;
        public const int USER_FAIL_BASE = 10000;
    }
}
