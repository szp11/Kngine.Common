using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Kngine
{
    public static class ThreadPoolTimeoutWorkaround
    {
        /*
         * This class solve the ThreadPool problem with WCF, which affect the performance
         * Know more: http://blogs.msdn.com/b/wenlong/archive/2010/02/11/why-does-wcf-become-slow-after-being-idle-for-15-seconds.aspx
         */


        static ManualResetEvent s_dummyEvent;
        static RegisteredWaitHandle s_registeredWait;

        public static void DoWorkaround()
        {
            int workerThreads, completionPortThreads;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMinThreads(workerThreads + 4, completionPortThreads  + 4);

            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMaxThreads(workerThreads + 4, completionPortThreads + 4);


            // Create an event that is never set
            s_dummyEvent = new ManualResetEvent(false);

            // Register a wait for the event, with a periodic timeout. This causes callbacks
            // to be queued to an IOCP thread, keeping it alive
            s_registeredWait = ThreadPool.RegisterWaitForSingleObject(s_dummyEvent, (a, b) =>
                {
                    return;
                }, null, 1000, false);
        }
    }

}
