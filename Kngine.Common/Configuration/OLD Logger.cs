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

        public enum MessageType { Error, Warn, Information }

        protected string mPath;
        protected StreamWriter mWriter;
        protected short mMessagesCounter;
        protected string mComponent;
        protected DateTime mLastFileDate;
        protected bool mAutoFlush;
        protected bool mFilePerDay;
        protected bool mIsDispoed;

        private static Timer mFileSyncTimer;
        private static Timer mDailyFileChengerTimer;
        public static string mDefaultLogDirectory = @"D:\log";
        protected static ConcurrentDictionary<string, Logger> mLoggers;

        protected static object mSyncObject = new object();

        /*/////////////////////////////////////////////////////////////////////////////////////////*/

        static Logger()
        {
            mLoggers = new ConcurrentDictionary<string, Logger>();
            var SpanBeforeEndOfTheDay = DateTime.Today.Add(new TimeSpan(1, 0, 1, 0, 0)) - DateTime.Now;
            mDailyFileChengerTimer = new Timer(new TimerCallback(ChangeFilesRoutine), null, SpanBeforeEndOfTheDay, new TimeSpan(1, 0, 0, 0, 0));
        }

        protected Logger(string component)
            : this(component, mDefaultLogDirectory, false, true)
        {

        }

        protected Logger(string component, string path, bool autoFlush, bool filePerDay)
        {
            mPath = path;
            mAutoFlush = autoFlush;
            mFilePerDay = filePerDay;
            mComponent = component;
            Directory.CreateDirectory(path);
            if (mPath.EndsWith("\\")) mPath = mPath.Substring(0, mPath.Length - 1);
            lock (mSyncObject)
            {
                if (mFileSyncTimer == null) mFileSyncTimer = new Timer(new TimerCallback(FileSyncRoutine), null, 3000, 3000);
            }
            

            BuildInternalStatus();
        }

        protected void BuildInternalStatus()
        {
            try
            {
                mLastFileDate = DateTime.Now;
                var fileName = mPath + @"\" + mComponent + "-" + mLastFileDate.Year + "-" + mLastFileDate.Month + "-" + mLastFileDate.Day + ".Klog";

                var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 1024 * 64);
                fs.Seek(0, SeekOrigin.End);
                mWriter = new StreamWriter(fs, new UTF8Encoding());
                if (mAutoFlush) mWriter.AutoFlush = true;
            }
            catch
            {
                return;
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
            Logger L;
            if (!mLoggers.TryGetValue(component, out L))
            {
                lock (mSyncObject)
                {
                    if (mLoggers.TryGetValue(component, out L)) return L;
                    L = new Logger(component, logDirectory, autoFlush, filePerDay);
                    mLoggers[component] = L;
                }
            }

            return L;

        }

        /// <summary>
        /// This method run every day to check the logs that need to update they files
        /// </summary>
        /// <param name="obj"></param>
        private static void ChangeFilesRoutine(object obj)
        {
            DateTime now = DateTime.Now;
            var loggers = mLoggers.ToArray();

            foreach (var item in loggers)
            {
                if (!item.Value.mFilePerDay || item.Value.mIsDispoed) continue;
                if (now.Day != item.Value.mLastFileDate.Day)
                {
                    lock (item.Value.mWriter)
                    {
                        item.Value.mWriter.Flush();
                        item.Value.mWriter.Dispose();
                        item.Value.BuildInternalStatus();
                    }
                }
            }
        }

        private static void FileSyncRoutine(object obj)
        {
            var loggers = mLoggers.ToArray();

            foreach (var item in loggers)
            {
                if (!item.Value.mFilePerDay || item.Value.mIsDispoed || item.Value.mAutoFlush) continue;
                lock (item.Value.mWriter)
                    item.Value.mWriter.Flush();
            }
        }

        public static int LogCount
        {
            get
            {
                return mLoggers.Count;
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

        /// <summary>
        /// Log Warn message
        /// </summary>
        public virtual void Warn(string message)
        {
            Message(message, null, MessageType.Warn);
        }

        /// <summary>
        /// Log simple message
        /// </summary>
        public virtual void Message(string message, MessageType type = MessageType.Information)
        {
            Message(message, null, type);
        }

        /// <summary>
        /// Log Error message
        /// </summary>
        public virtual void Error(string message, Exception objToEmit = null)
        {
            Message(message, objToEmit, MessageType.Error);
        }

        /// <summary>
        /// Log message
        /// </summary>
        public virtual void Message(string message, object objToEmit, MessageType type = MessageType.Information)
        {
            try
            {
                var objectString = objToEmit == null ? null : Utilities.ObjectToString(objToEmit);

                lock (mWriter)
                {
                    var date = DateTime.Now;
                    mWriter.WriteLine("LOG BEGIN\t" + date.Year + "-" + date.Month + "-" + date.Day + "\t" + date.Hour + ":" + date.Minute + ":" + date.Second + ":" + date.Millisecond + "\t" +
                                      type.ToString() + "\t" + message + "\t" + objectString);

                    mMessagesCounter++;
                    if (mAutoFlush) return;
                    float d = mMessagesCounter / 1000f;
                    if (d == (int)(mMessagesCounter / 1000)) mWriter.Flush();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Flush the logger stream
        /// </summary>
        public void Flush()
        {
            try
            {
                if (mWriter == null) return;
                lock (mWriter)
                {
                    mWriter.Flush();
                }
            }
            catch
            {
            }
        }


        #region IDisposable Members

        public void Dispose()
        {
            lock (mWriter)
            {
                mIsDispoed = true;
                mWriter.Flush();
                mWriter.Dispose();
            }
        }

        #endregion
    }
}
