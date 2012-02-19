using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kngine;
using System.Threading;
using System.Collections.Concurrent;

namespace Kngine.Configuration
{
    

    /// <summary>
    /// Cached Logging, this class inhiret from Logger beside the logging, this class cache the latest N element and latest N errors will be cached
    /// </summary>
    public class CachedLogger : Logger, IDisposable
    {

        protected int ItemToCache;

        protected ConcurrentQueue<LogItem> ExceptionsCache;

        protected ConcurrentQueue<LogItem> LatestItemsCache;

        /*/////////////////////////////////////////////////////////////////////////////////////////*/


        private CachedLogger(int itemToCache, string component)
            : this(itemToCache, component, mDefaultLogDirectory, false, true)
        {
        }

        private CachedLogger(int itemToCache, string component, string path, bool autoFlush, bool filePerDay)
            : this(itemToCache, component, path, autoFlush, (filePerDay == true) ? FilePeriod.FilePerDay : FilePeriod.OneFile)
        {
        }

        private CachedLogger(int itemToCache, string component, string path, bool autoFlush, FilePeriod filePeriod)
            : base(component, path, autoFlush, filePeriod)
        {
            if (itemToCache < 1) itemToCache = 100;
            ItemToCache = itemToCache;
            ExceptionsCache = new ConcurrentQueue<LogItem>();
            LatestItemsCache = new ConcurrentQueue<LogItem>();
        }


        /*/////////////////////////////////////////////////////////////////////////////////////////*/

        /// <summary>
        /// Get Logger Object. The logger buffer will be flushed every 3 sec. The logger file will be saved into the default directory.
        /// </summary>
        /// <param name="component">Component name (should be file system friendly)</param>
        public static CachedLogger GetLogger(string component, int itemsToCache)
        {
            return GetLogger(component, itemsToCache, mDefaultLogDirectory, false, true);
        }

        /// <summary>
        /// Get Logger Object. The logger file will be saved into the default directory.
        /// </summary>
        /// <param name="component">Component name (should be file system friendly)</param>
        /// <param name="autoFlush">Auto flush after every write, even if you didn't set it true the log will flush the memory every 3 sec.</param>
        /// <param name="filePerDay">Create file per day (if true), or just one file</param>
        public static CachedLogger GetLogger(string component, int itemsToCache, bool autoFlush, bool filePerDay)
        {
            return GetLogger(component, itemsToCache, mDefaultLogDirectory, autoFlush, filePerDay);
        }

        /// <summary>
        /// Get Logger Object.
        /// </summary>
        /// <param name="component">Component name (should be file system friendly)</param>
        /// <param name="logDirectory">Log Path</param>
        /// <param name="autoFlush">Auto flush after every write, even if you didn't set it true the log will flush the memory every 3 sec.</param>
        /// <param name="filePerDay">Create file per day (if true), or just one file</param>
        /// <returns></returns>
        public static CachedLogger GetLogger(string component, int itemsToCache,string logDirectory, bool autoFlush, bool filePerDay)
        {
            return GetLogger(component, itemsToCache, mDefaultLogDirectory, autoFlush, filePerDay == true ? FilePeriod.FilePerDay : FilePeriod.OneFile);

        }

        /// <summary>
        /// Get Logger Object.
        /// </summary>
        /// <param name="component">Component name (should be file system friendly)</param>
        /// <param name="logDirectory">Log Path</param>
        /// <param name="autoFlush">Auto flush after every write, even if you didn't set it true the log will flush the memory every 3 sec.</param>
        /// <param name="filePerDay">Create file per day (if true), or just one file</param>
        /// <returns></returns>
        public static CachedLogger GetLogger(string component, int itemsToCache, string logDirectory, bool autoFlush, FilePeriod filePeriod)
        {
            Logger L;
            if (!mLoggers.TryGetValue(component, out L))
            {
                lock (mGlobalSyncObject)
                {
                    if (mLoggers.TryGetValue(component, out L)) return L as CachedLogger;
                    L = new CachedLogger(itemsToCache, component, logDirectory, autoFlush, filePeriod);
                    mLoggers[component] = L;
                }
            }

            return L as CachedLogger;
        }

        /*/////////////////////////////////////////////////////////////////////////////////////////*/

        
        /// <summary>
        /// Log message
        /// </summary>
        protected override void LogMessage(LogItem logItem)
        {
            base.LogMessage(logItem);

            try
            {
                LogItem t;
                if (logItem.Type == MessageType.Error)
                {
                    if (ExceptionsCache.Count >= ItemToCache) ExceptionsCache.TryDequeue(out t);
                    ExceptionsCache.Enqueue(logItem);
                }

                if (LatestItemsCache.Count >= ItemToCache) LatestItemsCache.TryDequeue(out t);
                LatestItemsCache.Enqueue(logItem);
            }
            catch
            {

            }
        }

        public LogItem[] CachedItems
        {
            get  { return LatestItemsCache != null ? LatestItemsCache.ToArray() : null; }
        }

        public LogItem[] CachedExceptions
        {
            get { return ExceptionsCache != null ? ExceptionsCache.ToArray() : null; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            base.Dispose();
        }

        #endregion
    }
}
