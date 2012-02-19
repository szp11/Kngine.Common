using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kngine.Configuration;

namespace Test
{
    class TestNewLog
    {

        public static void Test()
        {
            LogSomeStuff("A");
            new Action<string>(LogSomeStuff).BeginInvoke("A", null, null);
            Thread.Sleep(352);
            new Action<string>(LogSomeStuff).BeginInvoke("A", null, null);
            new Action<string>(LogSomeStuff).BeginInvoke("A", null, null);
            Thread.Sleep(212);
            new Action<string>(LogSomeStuff).BeginInvoke("A", null, null);
            Thread.Sleep(292);
            new Action<string>(LogSomeStuff).BeginInvoke("A", null, null);
            Thread.Sleep(800);
            LogSomeStuff("A");
            Console.ReadLine();
        }

        public static void LogSomeStuff(string logName)
        {
            var ThreadName = Thread.CurrentThread.ManagedThreadId;
            var log = Logger.GetLogger(logName);

            for (int i = 0; i < 10; i++)
            {
                for (int x = 0; x < 1000; ++x)
                    log.Information(ThreadName + " : A " + i + "-" + x);

                Thread.Sleep(252);
            }

            Console.WriteLine(ThreadName + " - log count : " + log.MessagesCount);
        }
        
    }
}
