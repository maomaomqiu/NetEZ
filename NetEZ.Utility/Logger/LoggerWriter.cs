using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Utility.Logger
{
    internal class LoggerWriter:IDisposable
    {
        const int DEFAULT_CAPACITY = 2048;
        private ConcurrentQueue<LoggerContext> _WriteQueue = new ConcurrentQueue<LoggerContext>();
        private volatile bool _IsWorking = false;
        private Thread _WorkingThread = null;
        private ILogger _Logger = null;
        private AutoResetEvent _ThreadEvent = new AutoResetEvent(true);

        public LoggerWriter(ILogger logger)
        {
            _Logger = logger;
            StartWorkingThread();
        }

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
                StopWorkingThread();
            }
            ///TODO:在这里加入清理"非托管资源"的代码
        }

        //供GC调用的析构函数
        ~LoggerWriter()
        {
            Dispose(false);//释放非托管资源
        }

        private void StartWorkingThread()
        {
            if (_IsWorking)
                return;

            lock (this)
            {
                if (_IsWorking)
                    return;

                _IsWorking = true;

                _WorkingThread = new Thread(CheckAndWrite);
                _WorkingThread.IsBackground = true;
                _WorkingThread.Start();
                while (!_WorkingThread.IsAlive)
                {
                    Thread.Sleep(3);
                }
            }
        }

        private void StopWorkingThread()
        {
            try
            {
                _IsWorking = false;
                _WorkingThread.Join(100);
            }
            catch { }
            finally
            {
                _WorkingThread = null;
            }

        }

        private StreamWriter GetStreamWriter()
        {
            StreamWriter sw = null;
            if (_Logger == null)
                return null;

            string fileFullPath = string.Empty;
            string fileNameNoExt = Path.GetFileNameWithoutExtension(_Logger.LogTemplateName);
            string fileNameExt = Path.GetExtension(_Logger.LogTemplateName);

            LogFileRollingType rollingType = _Logger.RollingType;

            int scale = 0;
            string scaleStr = "";

            switch (rollingType)
            { 
                case LogFileRollingType.SingleFile:
                    fileFullPath = string.Format("{0}{1}", _Logger.LogPath, _Logger.LogTemplateName);
                    break;
                case LogFileRollingType.Minutes5:
                    scale = (DateTime.Now.Minute / 5) * 5;
                    scaleStr = scale > 9 ? scale.ToString() : "0" + scale.ToString();
                    fileFullPath = string.Format("{0}{1}\\{2}_{3}{4}{5}", _Logger.LogPath, DateTime.Now.ToString("yyyyMMdd"), fileNameNoExt, DateTime.Now.ToString("HH"), scaleStr, fileNameExt);
                    break;
                case LogFileRollingType.Minutes15:
                    scale = (DateTime.Now.Minute / 15) * 15;
                    scaleStr = scale > 9 ? scale.ToString() : "0" + scale.ToString();
                    fileFullPath = string.Format("{0}{1}\\{2}_{3}{4}{5}", _Logger.LogPath, DateTime.Now.ToString("yyyyMMdd"), fileNameNoExt, DateTime.Now.ToString("HH"), scaleStr, fileNameExt);
                    break;
                case LogFileRollingType.Hourly:
                    fileFullPath = string.Format("{0}{1}\\{2}_{3}{4}", _Logger.LogPath, DateTime.Now.ToString("yyyyMMdd"), fileNameNoExt, DateTime.Now.ToString("HH"), fileNameExt);
                    break;
                case LogFileRollingType.Daily:
                    fileFullPath = string.Format("{0}{1}\\{2}", _Logger.LogPath, DateTime.Now.ToString("yyyyMMdd"), _Logger.LogTemplateName);
                    break;
                case LogFileRollingType.Monthly:
                    fileFullPath = string.Format("{0}{1}_{2}{3}", _Logger.LogPath, fileNameNoExt, DateTime.Now.ToString("yyyyMM"), fileNameExt);
                    break;
                case LogFileRollingType.Yearly:
                    fileFullPath = string.Format("{0}{1}_{2}{3}", _Logger.LogPath, fileNameNoExt, DateTime.Now.Year, fileNameExt);
                    break;
                default:
                    fileFullPath = string.Format("{0}{1}",_Logger.LogPath,_Logger.LogTemplateName);
                    break;
            }

            try 
            {
                string path = Path.GetDirectoryName(fileFullPath);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                sw = new StreamWriter(fileFullPath, true, Encoding.UTF8);
            }
            catch { }

            return sw;
        }

        private void CheckAndWrite()
        {
            List<LoggerContext> buf = new List<LoggerContext>(128);
            int batchSize = 128;

            while (_IsWorking)
            {
                _ThreadEvent.WaitOne(3000);

                LoggerContext lc = null;
                int i = 0;

                while (i++ < batchSize && _WriteQueue.TryDequeue(out lc))
                {
                    buf.Add(lc);
                }

                if (buf.Count > 0)
                {
                    try
                    {
                        using (StreamWriter sw = GetStreamWriter())
                        {
                            if (sw != null)
                            {
                                foreach (LoggerContext ctx in buf)
                                {
                                    sw.WriteLine(ctx.Msg);
                                }
                                sw.Close();
                            }
                        }
                    }
                    catch { }
                    finally { buf.Clear();}

                }
            }
        }

        public void WriteLog(LoggerContext ctx)
        {
            if (ctx == null || string.IsNullOrEmpty(ctx.Msg))
                return;

            _WriteQueue.Enqueue(ctx);

            _ThreadEvent.Set();
        }

        public void WriteLog(string msg)
        {
            WriteLog(new LoggerContext(msg));
        }
    }
}
