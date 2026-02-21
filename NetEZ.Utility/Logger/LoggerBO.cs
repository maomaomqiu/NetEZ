using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Utility.Logger
{
    public class LoggerBO
    {
        protected volatile Logger _Logger = null;

        public void SetLogger(Logger logger)
        {
            _Logger = logger;
        }

        public void LogInfo(string msg)
        {
            if (_Logger != null)
                _Logger.Info(msg);
        }

        public void LogError(string msg)
        {
            if (_Logger != null)
                _Logger.Error(msg);
        }

        public void LogDebug(string msg)
        {
            if (_Logger != null)
                _Logger.Debug(msg);
        }
    }
}
