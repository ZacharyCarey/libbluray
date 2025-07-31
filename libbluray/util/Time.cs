using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.util
{
    internal static class Time
    {
        private static DateTime t0;
        private static bool initialized = false;
        public static UInt64 bd_get_scr()
        {
            DateTime now = DateTime.Now;
            if (!initialized)
            {
                initialized = true;
                t0 = now;
            }

            TimeSpan delta = now - t0;
            return (UInt64)delta.TotalMilliseconds * 90;
        }

    }
}
