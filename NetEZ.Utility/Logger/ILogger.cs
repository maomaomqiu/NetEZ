using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Utility.Logger
{
    public interface ILogger
    {
        bool IsInfoEnabled { get; }
        bool IsDebugEnabled { get; }
        bool IsErrorEnabled { get; }

        LogFileRollingType RollingType { get;}
        string LogPath { get;}
        string LogTemplateName { get;}


        void EnableLogLevel(LogLevel level);
        
        void DisableLogLevel(LogLevel level);
        
        void Info(string msg);

        void Debug(string msg);

        void Error(string msg);
        
    }
}
