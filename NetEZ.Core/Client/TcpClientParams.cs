using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Core.Client
{
    public class TcpClientParams
    {
        private const int BUFFER_SIZE_DEFAULT = 4096;
        private const int MESSAGE_LENGTH_MIN = 4;
        private const int MESSAGE_LENGTH_MAX = Defines.MAX_MESSAGE_LENGTH;
        private const int SEND_TIMEOUT = 2000;
        private const int CONNECT_TIMEOUT = 3000;

        public int ReceiveBufferSize = BUFFER_SIZE_DEFAULT;
        public int MessageMinLength = MESSAGE_LENGTH_MIN;
        public int MessageMaxLength = MESSAGE_LENGTH_MAX;
        public int SendTimeout = SEND_TIMEOUT;
        public bool EnableLogger = false;
        public int ConnectTimeOut = CONNECT_TIMEOUT;
        public bool ReConnect = false;
    }
}
