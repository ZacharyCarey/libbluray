using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.util
{
    [Flags]
    internal enum DebugMaskEnum : UInt32
    {
        DBG_RESERVED = 0x00001, /*   (reserved) */
        DBG_CONFIGFILE = 0x00002, /*   (reserved for libaacs) */
        DBG_FILE = 0x00004, /*   (reserved for libaacs) */
        DBG_AACS = 0x00008, /*   (reserved for libaacs) */
        DBG_MKB = 0x00010, /*   (reserved for libaacs) */
        DBG_MMC = 0x00020, /*   (reserved for libaacs) */
        DBG_BLURAY = 0x00040, /**< BluRay player */
        DBG_DIR = 0x00080, /**< Directory access */
        DBG_NAV = 0x00100, /**< Database files (playlist and clip info) */
        DBG_BDPLUS = 0x00200, /*   (reserved for libbdplus) */
        DBG_DLX = 0x00400, /*   (reserved for libbdplus) */
        DBG_CRIT = 0x00800, /**< **Critical messages and errors** (default) */
        DBG_HDMV = 0x01000, /**< HDMV virtual machine execution trace */
        DBG_BDJ = 0x02000, /**< BD-J subsystem and Xlet trace */
        DBG_STREAM = 0x04000, /**< m2ts stream trace */
        DBG_GC = 0x08000, /**< graphics controller trace */
        DBG_DECODE = 0x10000, /**< PG / IG decoders, m2ts demuxer */
        DBG_JNI = 0x20000, /**< JNI calls */
    }

    internal class Logging
    {

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="msg"></param>
        public delegate void BD_LOG_FUNC(string msg);

        private static BD_LOG_FUNC? log_func = null;
        private static DebugMaskEnum debug_mask = (DebugMaskEnum)UInt32.MaxValue;

        /// <summary>
        /// Set (global) debug handler
        /// The function will receive all enabled log messages.
        /// </summary>
        /// <param name="handler"></param>
        public static void bd_set_debug_handler(BD_LOG_FUNC handler)
        {
            log_func = handler;
        }

        /// <summary>
        /// Set (global) debug mask
        /// </summary>
        /// <param name="mask"></param>
        public static void bd_set_debug_mask(DebugMaskEnum mask)
        {
            debug_mask = mask;
        }

        /// <summary>
        /// Get current (global) debug mask
        /// </summary>
        /// <returns></returns>
        public static DebugMaskEnum bd_get_debug_mask()
        {
            return debug_mask;
        }

        private static int debug_init = 0;
        private static int debug_file = 0;
        private static FileStream logfile = null;
        public static void bd_debug(DebugMaskEnum mask, string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (debug_init == 0)
            {
                debug_init = 1;
                //logfile = Console.Error;
            }

            if ((mask & debug_mask) != 0)
            {
                Console.WriteLine(msg);
            }
        }
        public static void bd_debug(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Console.WriteLine(msg);
        }
    }
}
