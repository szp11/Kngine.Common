using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {

            // How to use Kngine.Serializer
            var o = new Object1 { A = "alpha", B = 585 };
            Kngine.Serializer.Serializer S = new Kngine.Serializer.Serializer();
            S.Initialize(typeof(Object1));
            var bytes = S.Serialize(o);
            var objt = S.Deserialize(bytes);
            return;



            // Test String Op
            StringOpTest.MeasurePerformance();
            return;

            // Test the new log
            TestNewLog.Test();
            return;
            
        }
    }

    [Serializable]
    public class Object1
    {
        public string A;

        public int B;
    }
}
