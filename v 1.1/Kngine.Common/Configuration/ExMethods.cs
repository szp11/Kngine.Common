using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Kngine.Configuration
{
    public static class ExMethods
    {
        public static string GetExceptionMethodName(this Exception ex)
        {
            if (ex == null) return "Unknown";
            try
            {
                return new StackTrace(ex).GetFrame(0).GetMethod().Name;
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
