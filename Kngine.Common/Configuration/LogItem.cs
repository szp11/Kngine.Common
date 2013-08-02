using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kngine.Configuration
{
    [Serializable]
    public class LogItem
    {
        public DateTime Date;

        public Logger.MessageType Type;

        public string Message;

        public object[] Objects;



        public void AppendLineToStringBuilder(ref StringBuilder sb)
        {
            sb.Append("LOG BEGIN\t" + Date.Year + "-" + Date.Month + "-" + Date.Day + "\t" + Date.Hour + ":" + Date.Minute + ":" + Date.Second + ":" + Date.Millisecond + "\t" +
                      Type.ToString() + "\t" + Message.Replace("\t", "").Replace("\r", ""));

            if (Objects != null && Objects.Length > 0)
            {
                for (int i = 0; i < Objects.Length; i++)
                {
                    var item = Objects[i];
                    if (item == null) continue;
                    sb.Append("\t" + ConvertObjectToString(item));
                }
            }

            sb.Append("\tLOG END");
            sb.Append(Environment.NewLine);
        }

        private static string ConvertObjectToString(object o)
        {
            if (o is Exception)
            {
                var ex = o as Exception;
                return ex.GetExceptionMethodName() + "||" + ex.Message.Replace("\t", "").Replace("\r", "") + "||" + ex.StackTrace.Replace("\t", "").Replace("\r", "");
            }
            else
                return o.ToString();
        }



        public override string ToString()
        {
            return Date.Year + "-" + Date.Month + "-" + Date.Day + " " + Date.Hour + ":" + Date.Minute + ":" + Date.Second + " - " + Type + " - " + Message;
        }
    }
}
