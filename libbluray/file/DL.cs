using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.file
{
    internal static class DL
    {
        public static string dlerror(byte[] buf, int size)
        {
            return "";
        }

        public static object? dl_dlopen(string path, string version)
        {
            return null;
        } 

        public static object? dl_dlsym(object handle, string symbol)
        {
            return null;
        }

        public static int dl_dlclose(object handle)
        {
            return 0;
        }

        public static string dl_get_path()
        {
            return null;
        }
    }
}
