using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Utility.Cache
{
    public class LRUCache<Tkey, Tvalue> : NetEZ.Utility.Logger.LoggerBO, IDisposable
    {
        class LRUCacheItem<Tval>
        {
            private DateTime _ExpiredTime = DateTime.MaxValue;
            private Tval _Value = default(Tval);

            public Tval Value { get { return _Value; } }
            public bool IsExpired { get { return _ExpiredTime <= DateTime.Now ? true : false; } }

            public LRUCacheItem(Tval val)
            {
                //  默认缓存不过期，这里设置一个较长的时间
                _Value = val;
                Refresh(0);
            }

            public LRUCacheItem(Tval val, int expiredSeconds)
            {
                _Value = val;
                Refresh(expiredSeconds);
            }

            public void Refresh(int expiredSeconds)
            {
                //  expiredSeconds < 1表示不过期，这里设置为一个较长的时间
                if (expiredSeconds < 1)
                    _ExpiredTime = DateTime.Now.AddDays(30);
                else
                    _ExpiredTime = DateTime.Now.AddSeconds(expiredSeconds);
            }
        }

        private ConcurrentDictionary<Tkey, LRUCacheItem<Tvalue>> _ItemTable = null;
        private Thread _ExpiredItemsCleanThread = null;
        private bool _LRUMode = false;
        private int _DefaultExpiredSeconds = 0;
        private volatile bool _IsThreadRunnning = false;

        protected bool _Disposed = false;

        public int Count { get { return _ItemTable != null ? _ItemTable.Count : 0; } }

        public LRUCache(NetEZ.Utility.Logger.Logger logger = null)
        {
            //  非LRU模式
            SetLogger(logger);

            _DefaultExpiredSeconds = 0;

            InitCache();
        }

        public LRUCache(bool lruMode = false, int expiredSeconds = 0, NetEZ.Utility.Logger.Logger logger = null, int concurrencyLevel = 0, int initCapcity = 0)
        {
            if (concurrencyLevel < 1 || initCapcity < 1)
            {
                _ItemTable = new ConcurrentDictionary<Tkey, LRUCacheItem<Tvalue>>();
            }
            else
            {
                _ItemTable = new ConcurrentDictionary<Tkey, LRUCacheItem<Tvalue>>(concurrencyLevel, initCapcity);
            }

            SetLogger(logger);

            //  LRU模式必须提供expired seconds
            if (lruMode)
            {
                if (expiredSeconds < 1)
                    throw new Exception("LRU mode must supply expired seconds;");

                _LRUMode = true;
            }

            //  如果lruMode==true，则表示根据最后命中时间过期; 否则表示绝对过期时间（当前时间加上expiredSeconds秒数）
            _DefaultExpiredSeconds = expiredSeconds;

            InitCache();
        }

        ~LRUCache()
        {
            Dispose();
        }

        public void Dispose()
        {
            //  释放所有的资源
            Dispose(true);
            //不需要再调用本对象的Finalize方法
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
                return;

            if (disposing)
            {
                //清理托管资源
                Release();
            }

            _Disposed = true;
        }

        private bool InitCache()
        {
            _IsThreadRunnning = true;
            _ExpiredItemsCleanThread = new Thread(CleanProc);
            _ExpiredItemsCleanThread.IsBackground = true;
            _ExpiredItemsCleanThread.Start();
            while (!_ExpiredItemsCleanThread.IsAlive)
                Thread.Sleep(3);

            return true;
        }

        private void Release()
        {
            _IsThreadRunnning = false;
            try
            {
                if (_ExpiredItemsCleanThread != null)
                    _ExpiredItemsCleanThread.Join(100);
            }
            catch { }
            finally { _ExpiredItemsCleanThread = null; }

            try
            {
                _ItemTable.Clear();
            }
            catch { }
            finally { _ItemTable = null; }
        }

        private void CleanProc(object state)
        {
            int batchSize = 10240;
            List<Tkey> keysBuf = new List<Tkey>(10240);
            LRUCacheItem<Tvalue> valTmp = null;

            while (_IsThreadRunnning)
            {
                //  定时扫描
                Thread.Sleep(20000);


                DateTime startTime = DateTime.Now;
                int removed = 0;
                try
                {

                    if (_ItemTable.IsEmpty)
                        continue;

                    if (_ItemTable.Count < batchSize * 1.2)
                    {
                        keysBuf.AddRange(_ItemTable.Keys);
                    }
                    else
                    {
                        keysBuf.AddRange(_ItemTable.Keys.Take(batchSize));
                    }

                    //  尝试清除
                    foreach (Tkey key in keysBuf)
                    {
                        if (!_ItemTable.TryGetValue(key, out valTmp))
                            continue;

                        if (valTmp.IsExpired)
                        {
                            _ItemTable.TryRemove(key, out valTmp);
                            removed++;
                        }

                        valTmp = null;
                    }
                }
                catch { }
                finally
                {
                    if (keysBuf.Count > 0)
                        keysBuf.Clear();
                }

                DateTime endTime = DateTime.Now;
                long elapse = (long)endTime.Subtract(startTime).TotalMilliseconds;
                LogInfo(string.Format("#{0}[LRUCache][CleanProc] Completed. removed={1}; elapse={2}ms; cache count={3}", Thread.CurrentThread.ManagedThreadId, removed, elapse, _ItemTable.Count));
            }
        }

        /// <summary>
        /// 这里不检查是否过期，调用该方法时务必了解
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(Tkey key)
        {
            return _ItemTable.ContainsKey(key);
        }

        public bool TryGetValue(Tkey key, out Tvalue val)
        {
            LRUCacheItem<Tvalue> item = null;
            if (!_ItemTable.TryGetValue(key, out item))
            {
                val = default(Tvalue);
                return false;
            }

            //  检查过期时间
            if (item.IsExpired)
            {
                val = default(Tvalue);
                return false;
            }

            //  LRU模式还要刷新过期时间
            if (_LRUMode)
                item.Refresh(_DefaultExpiredSeconds);

            val = item.Value;
            return true;
        }

        public bool TryRemove(Tkey key, out Tvalue val)
        {
            LRUCacheItem<Tvalue> item = null;
            if (!_ItemTable.TryRemove(key, out item))
            {
                val = default(Tvalue);
                return false;
            }

            //  LRU模式要检查过期时间
            if (item.IsExpired)
            {
                val = default(Tvalue);
                return false;
            }

            val = item.Value;
            return true;
        }

        public void AddOrUpdate(Tkey key, Tvalue val)
        {
            LRUCacheItem<Tvalue> item = new LRUCacheItem<Tvalue>(val, _DefaultExpiredSeconds);
            _ItemTable.AddOrUpdate(key, item, (k, v) => item);

        }

        public bool TryAdd(Tkey key, Tvalue val)
        {
            LRUCacheItem<Tvalue> item = new LRUCacheItem<Tvalue>(val, _DefaultExpiredSeconds);
            return _ItemTable.TryAdd(key, item);
        }
    }
}
