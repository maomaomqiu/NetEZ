using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Utility.Logger
{
    internal class LoggerContext
    {
        public string Msg;

        public LoggerContext(string msg) { Msg = msg; }
    }

    public enum LogFileRollingType
    {
        SingleFile = 0,
        Minutes5 = 1,
        Minutes15 = 2,
        Hourly = 3,
        Daily = 4,
        Monthly = 5,
        Yearly = 6,
    }

    public enum LogLevel
    {
        Info = 1,
        Debug = 2,
        Error = 4,
    }
}
