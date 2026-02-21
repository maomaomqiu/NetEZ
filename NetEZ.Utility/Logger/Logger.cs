using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NetEZ.Utility.Logger
{
    public class Logger:ILogger,IDisposable
    {
        private const string TEMPLATE_INFO = "[INFO][{0}]{1}";
        private const string TEMPLATE_DEBUG = "[DEBUG][{0}]{1}";
        private const string TEMPLATE_ERROR = "[ERROR][{0}]{1}";
        private LogFileRollingType _RollingType = LogFileRollingType.Daily;
        private string _Path = string.Empty;
        private string _TemplateFileName = string.Empty;
        private int _Loglevel = (int)LogLevel.Error;
        private LoggerWriter _LogWriter = null;

        public bool IsInfoEnabled { get { return (_Loglevel & (int)LogLevel.Info) > 0; } }
        public bool IsDebugEnabled { get { return (_Loglevel & (int)LogLevel.Debug) > 0; } }
        public bool IsErrorEnabled { get { return (_Loglevel & (int)LogLevel.Error) > 0; } }

        public LogFileRollingType RollingType { get { return _RollingType; } }
        public string LogPath { get { return _Path; } }
        public string LogTemplateName { get { return _TemplateFileName; } }

        public void Dispose()
        {
            //调用带参数的Dispose方法，释放托管和非托管资源
            Dispose(true);
            //手动调用了Dispose释放资源，那么析构函数就是不必要的了，这里阻止GC调用析构函数
            //System.GC.SuppressFinalize(this);
            GC.SuppressFinalize(this);
        }

        //protected的Dispose方法，保证不会被外部调用。
        //传入bool值disposing以确定是否释放托管资源
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                ///TODO:在这里加入清理"托管资源"的代码
                _LogWriter.Dispose();
            }
            ///TODO:在这里加入清理"非托管资源"的代码
        }

        //供GC调用的析构函数
        ~Logger()
        {
            Dispose(false);//释放非托管资源
        }

        public string LogFileFullName 
        {
            get
            {
                return string.Format("{0}{1}", LogPath, LogTemplateName);
            }
        }

        public void EnableLogLevel(LogLevel level)
        {
            _Loglevel = _Loglevel | (int)level;
        }

        public void DisableLogLevel(LogLevel level)
        {
            _Loglevel = _Loglevel & ((int)level ^ 0xffff);
        }


        private void InitLogger(string logPath, string logTemplateName, LogFileRollingType type = LogFileRollingType.Daily)
        {
            _Path = logPath;
            if (!_Path.EndsWith("\\"))
                _Path += "\\";
            try
            {
                if (!Directory.Exists(_Path))
                    Directory.CreateDirectory(_Path);
            }
            catch
            {
                _Path = System.Environment.CurrentDirectory;
            }

            _TemplateFileName = !string.IsNullOrEmpty(logTemplateName) ? logTemplateName.Trim() : "log.txt";
            if (_TemplateFileName.Length < 1)
                _TemplateFileName = "log.txt";

            _RollingType = type;

            _LogWriter = new LoggerWriter(this);
        }

        public Logger(string fullPathName, LogFileRollingType type = LogFileRollingType.Daily)
        {
            try 
            {
                string path = Path.GetDirectoryName(fullPathName);
                string file = Path.GetFileName(fullPathName);

                InitLogger(path, file, type);
            }
            catch { }
        }

        public Logger(string logPath,string logTemplateName,LogFileRollingType type = LogFileRollingType.Daily)
        {
            InitLogger(logPath, logTemplateName, type);
        }

        public void Log(LogLevel level, string msg)
        {
            if (level == LogLevel.Info)
            {
                Info(msg);
            }
            else if (level == LogLevel.Debug)
            {
                Debug(msg);
            }
            else if (level == LogLevel.Error)
            {
                Error(msg);
            }
        }

        public void Info(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;
            if (IsInfoEnabled)
            {
                msg = string.Format(TEMPLATE_INFO, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), msg);
                LoggerContext ctx = new LoggerContext(msg);
                _LogWriter.WriteLog(ctx);
            }
                
        }

        public void Debug(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;
            if (IsDebugEnabled)
            {
                msg = string.Format(TEMPLATE_DEBUG, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), msg);
                LoggerContext ctx = new LoggerContext(msg);
                _LogWriter.WriteLog(ctx);
            }
                
        }

        public void Error(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;
            if (IsErrorEnabled)
            {
                msg = string.Format(TEMPLATE_ERROR, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), msg);
                LoggerContext ctx = new LoggerContext(msg);
                _LogWriter.WriteLog(ctx);
            }
        }
    }
}
