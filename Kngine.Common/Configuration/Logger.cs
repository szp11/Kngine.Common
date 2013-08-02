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
    /// Genric Logging system
    /// </summary>
    public class Logger : IDisposable
    {
        public enum FilePeriod { OneFile, FilePerDay, FilePerHour }
        public enum MessageType { Error, Warn, Information }

        // Static
        protected static Thread mWriterThread; 
        protected static object mGlobalSyncObject = new object(); 
        protected static ConcurrentDictionary<string, Logger> mLoggers;
        public static string mDefaultLogDirectory = @"D:\log";

        // Instance
        protected string mPath;
        protected bool mAutoFlush;
        protected string mComponent;
        protected FilePeriod mFilePeriod;
        protected ConcurrentQueue<LogItem> mQueue;
        protected object mSyncObject = new object();
        
        protected bool mIsDispoed;
        protected int mMessagesCounter;

        /*/////////////////////////////////////////////////////////////////////////////////////////*/

        static Logger()
        {
            mLoggers = new ConcurrentDictionary<string, Logger>();
            mWriterThread = new Thread(new ThreadStart(FlushRoutine));
            mWriterThread.IsBackground = true;
            mWriterThread.Start();

            AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => Shutdown());
            AppDomain.CurrentDomain.DomainUnload += new EventHandler((s, e) => Shutdown());
        }

        protected Logger(string component)
            : this(component, mDefaultLogDirectory, false, true)
        {

        }

        protected Logger(string component, string path, bool autoFlush, bool filePerDay)
            : this(component, path, autoFlush, (filePerDay == true) ? FilePeriod.FilePerDay : FilePeriod.OneFile) { }

        protected Logger(string component, string path, bool autoFlush, FilePeriod filePeriod)
        {
            mPath = path;
            EnsurePathExist(path);
            mAutoFlush = autoFlush;
            mComponent = component;
            mFilePeriod = filePeriod; 
            mQueue = new ConcurrentQueue<LogItem>();
            
            Directory.CreateDirectory(path);
            if (mPath.EndsWith("\\")) mPath = mPath.Substring(0, mPath.Length - 1);
        }

        private void EnsurePathExist(string path)
        {
            try
            {
                if (Directory.Exists(path)) return;
                Directory.CreateDirectory(path);
            }
            catch
            {
            }
        }

        /*/////////////////////////////////////////////////////////////////////////////////////////*/

        /// <summary>
        /// Get Logger Object. The logger buffer will be flushed every 3 sec. The logger file will be saved into the default directory.
        /// </summary>
        /// <param name="component">Component name (should be file system friendly)</param>
        public static Logger GetLogger(string component)
        {
            return GetLogger(component, mDefaultLogDirectory, false, true);
        }

        /// <summary>
        /// Get Logger Object. The logger file will be saved into the default directory.
        /// </summary>
        /// <param name="component">Component name (should be file system friendly)</param>
        /// <param name="autoFlush">Auto flush after every write, even if you didn't set it true the log will flush the memory every 3 sec.</param>
        /// <param name="filePerDay">Create file per day (if true), or just one file</param>
        public static Logger GetLogger(string component, bool autoFlush, bool filePerDay)
        {
            return GetLogger(component, mDefaultLogDirectory, autoFlush, filePerDay);
        }

        /// <summary>
        /// Get Logger Object.
        /// </summary>
        /// <param name="component">Component name (should be file system friendly)</param>
        /// <param name="logDirectory">Log Path</param>
        /// <param name="autoFlush">Auto flush after every write, even if you didn't set it true the log will flush the memory every 3 sec.</param>
        /// <param name="filePerDay">Create file per day (if true), or just one file</param>
        /// <returns></returns>
        public static Logger GetLogger(string component, string logDirectory, bool autoFlush, bool filePerDay)
        {
            return GetLogger(component, logDirectory, autoFlush, filePerDay == true ? FilePeriod.FilePerDay : FilePeriod.OneFile);
        }

        /// <summary>
        /// Get Logger Object.
        /// </summary>
        /// <param name="component">Component name (should be file system friendly)</param>
        /// <param name="logDirectory">Log Path</param>
        /// <param name="autoFlush">Auto flush after every write, even if you didn't set it true the log will flush the memory every 3 sec.</param>
        /// <param name="filePerDay">Create file per day (if true), or just one file</param>
        /// <returns></returns>
        public static Logger GetLogger(string component, string logDirectory, bool autoFlush, FilePeriod filePeriod)
        {
            Logger L;
            if (!mLoggers.TryGetValue(component, out L))
            {
                lock (mGlobalSyncObject)
                {
                    if (mLoggers.TryGetValue(component, out L)) return L;
                    L = new Logger(component, logDirectory, autoFlush, filePeriod);
                    mLoggers[component] = L;
                }
            }

            return L;
        }

        /*/////////////////////////////////////////////////////////////////////////////////////////*/

        public static int LoggerCount
        {
            get
            {
                try
                {
                    return mLoggers.Count;
                }
                catch
                {
                    return -1;
                }
            }
        }

        public int MessagesCount
        {
            get
            {
                return mMessagesCounter;
            }
        }

        /*/////////////////////////////////////////////////////////////////////////////////////////*/

        public void Flush()
        {
            string fileName = null;
            if (mIsDispoed) return;

            try
            {
                // 1. Check if we need to create new file or not: By check the current file size and date
                if (mFilePeriod == FilePeriod.OneFile)
                    fileName = mPath + @"\" + mComponent;
                else
                {
                    var dt = DateTime.Now;
                    if (mFilePeriod == FilePeriod.FilePerDay)
                        fileName = mPath + @"\" + mComponent + " " + dt.Year + "-" + dt.Month + "-" + dt.Day;
                    else
                        fileName = mPath + @"\" + mComponent + " " + dt.Year + "-" + dt.Month + "-" + dt.Day + " " + dt.Hour;
                }

                lock (mSyncObject)
                {
                    // 2. Get the data and append it
                    if (mQueue == null || mQueue.Count < 1) return;
                    var SB = new StringBuilder(mQueue.Count * 100);

                    for (int i = 0; i < mQueue.Count; i++)
                    {
                        LogItem tmp;
                        if (mQueue.TryDequeue(out tmp)) tmp.AppendLineToStringBuilder(ref SB);
                    }

                    File.AppendAllText(fileName, SB.ToString(), new UTF8Encoding());
                }
            }
            catch
            {
            }
        }

        private static void FlushRoutine()
        {
            while (true)
            {
                try
                {
                    KeyValuePair<string, Logger>[] currentLogs;

                    lock (mGlobalSyncObject)
                        currentLogs = mLoggers.ToArray();

                    if (currentLogs == null) return;
                    foreach (var item in currentLogs)
                    {
                        try
                        {
                            if (item.Value.mIsDispoed) continue;
                            item.Value.Flush();
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }

                Thread.Sleep(500);
            }
        }

        public static void Shutdown()
        {
            try
            {
                KeyValuePair<string, Logger>[] currentLogs;

                lock (mGlobalSyncObject)
                    currentLogs = mLoggers.ToArray();

                if (currentLogs == null) return;
                foreach (var item in currentLogs)
                {
                    try
                    {
                        if (item.Value.mIsDispoed) continue;
                        item.Value.Dispose();
                    }
                    catch
                    {
                    }
                }
                lock (mGlobalSyncObject)
                {
                    mLoggers.Clear();
                    mLoggers = new ConcurrentDictionary<string, Logger>();
                    mWriterThread.Abort();
                }
            }
            catch
            {
            }
        }

        /*/////////////////////////////////////////////////////////////////////////////////////////*/

        /// <summary>
        /// Log Warn message
        /// </summary>
        public virtual void Warn(string message)
        {
            Message(MessageType.Warn, message, null);
        }

        /// <summary>
        /// Log Warn message
        /// </summary>
        public virtual void Warn(string message, params object[] objs)
        {
            Message(MessageType.Warn, message, objs);
        }


        /// <summary>
        /// Log Information message
        /// </summary>
        public virtual void Information(string message)
        {
            Message(MessageType.Information, message, null);
        }

        /// <summary>
        /// Log Information message
        /// </summary>
        public virtual void Information(string message, params object[] objs)
        {
            Message(MessageType.Information, message, objs);
        }


        /// <summary>
        /// Log Error message
        /// </summary>
        public virtual void Error(string message, Exception objToEmit = null)
        {
            Message(MessageType.Error, message, objToEmit);
        }

        /// <summary>
        /// Log Error message
        /// </summary>
        public virtual void Error(string message, Exception objToEmit = null, params object[] objs)
        {
            Message(MessageType.Error, message, objToEmit, objs);
        }


        /// <summary>
        /// Log simple message
        /// </summary>
        public virtual void Message(string message, MessageType type = MessageType.Information)
        {
            Message(type, message, null);
        }

        /// <summary>
        /// Log simple message
        /// </summary>
        public virtual void Message(MessageType type, string message)
        {
            Message(type, message, null);
        }
        
        /// <summary>
        /// Log message
        /// </summary>
        public virtual void Message(MessageType type, string message, params object[] objs)
        {
            try
            {
                LogMessage(new LogItem { Type = type, Message = message, Date = DateTime.Now, Objects = objs });
            }
            catch
            {
            }
        }


        /// <summary>
        /// Log message
        /// </summary>
        protected virtual void LogMessage(LogItem logItem)
        {
            try
            {
                mQueue.Enqueue(logItem);
                Interlocked.Increment(ref mMessagesCounter);
            }
            catch
            {
            }
        }

        


        #region IDisposable Members

        public void Dispose()
        {
            Flush();
            mIsDispoed = true;
        }

        #endregion

        
    }
}
