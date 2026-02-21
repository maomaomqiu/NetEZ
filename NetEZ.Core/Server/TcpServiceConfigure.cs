using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetEZ.Utility.Configure;

namespace NetEZ.Core.Server
{
    public class TcpServiceDefines
    {
        public const int CLIENT_BUFFER_SIZE_MIN = 16;
        public const int CLIENT_BUFFER_SIZE_DEFAULT = 4096;

        public const int BACKLOG_DEFAULT = 200;
    }

    public class HostInfo
    {
        public string Host;
        public int Port;

        public HostInfo(){ Host = ""; Port = 0; }

        public HostInfo(string hostPort)
        {
            if (string.IsNullOrEmpty(hostPort))
                throw new Exception("Invalid host and port string");

            int idx = hostPort.IndexOf(':');
            if (idx > 0 && idx < hostPort.Length - 1)
            {
                Host = hostPort.Substring(0, idx).Trim();
                Port = Convert.ToInt32(hostPort.Substring(idx + 1));
            }
        }

        public HostInfo(string host, int port) { Host = host; Port = port; }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Host, Port);
        }

        public static HostInfo ParseFromString(string hostString)
        {
            if (string.IsNullOrEmpty(hostString))
                return null;

            int idx = hostString.IndexOf(':');
            if (idx < 1 || idx >= hostString.Length - 1)
                return null;

            string host = hostString.Substring(0, idx);
            string portString = hostString.Substring(idx + 1);
            int port = 0;
            if (!Int32.TryParse(portString, out port) && port > 0)
                return null;

            return new HostInfo(host.Trim(), port);
        }
    }

    public class TcpServiceConfigure:SimpleXmlConfigure
    {
        private const string CONFIG_SECTION_SOCKET = "Socket";

        //protected int _Port = 0;
        protected int _Backlog = 0;
        protected int _Heartbeat = 0;           //  !0 = 启用心跳监测，无心跳多少秒后断开连接; 0 = 不启用心跳监测
        protected uint _ClientBuffSize = 0;      //  客户端接收数据缓存大小

        public List<HostInfo> ListenHosts = new List<HostInfo>();       //  监听本地ip列表

        //public int Port { get { return _Port; } }
        public int Backlog { get { return _Backlog; } }

        /// <summary>
        /// 单位:秒. 大于0时表示启动心跳检测，距离上次心跳时间超过该秒数时，认为客户端已经断线
        /// </summary>
        public int HeartbeatIdle { get { return _Heartbeat; } }

        public uint ClientBuffSize { get { return _ClientBuffSize; } }

        public TcpServiceConfigure(string cfgFile) : base(cfgFile) 
        {
            if (!Int32.TryParse(GetItemValue(CONFIG_SECTION_SOCKET, "Backlog"), out _Backlog))
                _Backlog = TcpServiceDefines.BACKLOG_DEFAULT;

            if (!Int32.TryParse(GetItemValue(CONFIG_SECTION_SOCKET, "Heartbeat"), out _Heartbeat))
                _Heartbeat = 0;

            if (!UInt32.TryParse(GetItemValue(CONFIG_SECTION_SOCKET, "ClientBuffSize"), out _ClientBuffSize))
                _ClientBuffSize = TcpServiceDefines.CLIENT_BUFFER_SIZE_DEFAULT;
            

            //  读取本地监听ip列表
            string listenIps = GetItemValue(CONFIG_SECTION_SOCKET, "Hosts");
            string[] hosts = listenIps.Split(',', ';');          //  这里不检查ip地址合法性
            if (hosts == null || hosts.Length < 1)
            {
                throw new Exception("Invalid hosts configure.");
            }

            foreach (string hostAndPort in hosts)
            {
                if (string.IsNullOrEmpty(hostAndPort))
                    continue;
                HostInfo hostInfo = HostInfo.ParseFromString(hostAndPort);
                if (hostInfo != null)
                    ListenHosts.Add(hostInfo);
            }

            if (ListenHosts.Count < 1)
            {
                throw new Exception("Invalid hosts configure.");
            }


            if (_Backlog < 1)
                _Backlog = 200;

            if (_Heartbeat < 0)
                _Heartbeat = 0;

            if (_ClientBuffSize < TcpServiceDefines.CLIENT_BUFFER_SIZE_MIN)
                _ClientBuffSize = TcpServiceDefines.CLIENT_BUFFER_SIZE_DEFAULT;
        }


    }
}
