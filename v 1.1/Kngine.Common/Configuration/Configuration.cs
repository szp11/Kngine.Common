using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace Kngine.Configuration
{
    /// <summary>
    /// This class designed to load configuration  file that follow the following standard:
    /// (Proeprty Name = Value) one property per line. Using reflection we it fil the class with the file information.
    /// This class support only field.
    /// </summary>
    public static class Configer
    {

        public static T LoadFromFile<T>(string fileName, Logger logger = null, bool throwErrors = true)
            where T : class
        {

            T R = Utilities.InitObjectAndThrowException<T>();
            LoadFromFile<T>(fileName, R, logger, throwErrors);

            return R;
        }

        public static void LoadFromFile<T>(string fileName, T obj, Logger logger = null, bool throwErrors = true)
            where T : class
        {
            if (!File.Exists(fileName)) return;

            int loc;
            string key, value;
            string[] lines = null;
            lines = File.ReadAllLines(fileName);

            for (int i = 0; i < lines.Length; ++i)
            {
                key = null;
                try
                {
                    loc = lines[i].IndexOf('=');
                    if (loc < 1) continue;

                    key = lines[i].Substring(0, loc).Trim();
                    value = lines[i].Substring(loc + 1).Trim();


                    var FA = new Kngine.Reflection.FastInvoker.FieldAccessor<T, string>(key);
                    FA.Set(obj, value);
                }
                catch (Exception ex)
                {
                    if (logger != null) logger.Error("Error reading Configuration file (" + fileName + "). Wrong option = (" + key + "), Line number : (" + i + 1 + ").");
                    if (!throwErrors) throw ex;
                }
            }
        }

        public static void SaveToFile<T>(string fileName, T obj)
            where T : class 

        {
            var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
            StringBuilder SB = new StringBuilder();

            foreach (var item in fields)
            {
                try
                {
                    var val = item.GetValue(obj).ToString();
                    SB.AppendLine(item.Name + "=" + val);
                }
                catch
                {
                }
            }

            File.WriteAllText(fileName, SB.ToString());

        }



        

    }
}
