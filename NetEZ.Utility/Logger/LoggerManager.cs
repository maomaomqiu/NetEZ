using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Utility.Logger
{
    public static class LoggerManager
    {
        private static ConcurrentDictionary<string, Logger> _LoggerTable = new ConcurrentDictionary<string, Logger>();

        public static Logger GetLogger(string name,string defaultPath = "",string defaultTemplateName="log.txt")
        {
            if (name == null)
                return null;

            name = name.Trim().ToLower();

            if (name.Length < 1)
                return null;

            Logger logger = null;

            if (_LoggerTable.TryGetValue(name, out logger))
                return logger;

            try
            {
                if (string.IsNullOrEmpty(defaultPath))
                {
                    defaultPath = string.Format("{0}\\log", System.Environment.CurrentDirectory);
                }

                if (!Directory.Exists(defaultPath))
                    Directory.CreateDirectory(defaultPath);

                if (string.IsNullOrEmpty(defaultTemplateName))
                    defaultTemplateName = "log.txt";
                logger = new Logger(defaultPath, defaultTemplateName);
                logger.EnableLogLevel(LogLevel.Info);
                logger.EnableLogLevel(LogLevel.Debug);
                logger.EnableLogLevel(LogLevel.Error);

                _LoggerTable.AddOrUpdate(name, logger, (k, v) => logger);

                return logger;

            }
            catch { }

            return null;
        }
    }
}
