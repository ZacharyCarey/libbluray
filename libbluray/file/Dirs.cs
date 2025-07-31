using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.file
{
    internal static class Dirs
    {
        public static string win32_get_font_dir(string font_file)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), font_file);
        }
        private static string appdir = null;
        public static string file_get_config_system(string? dir)
        {
            if (dir == null)
            {
                //first call
                if (appdir != null) return appdir;

                // Get the "application data" folder for all users
                string wdir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                appdir = wdir;
                return appdir;
            } else
            {
                // next call
                return null;
            }

            return dir;
        }
        public static string file_get_config_home()
        {
            return file_get_data_home();
        }
        public static string file_get_cache_home()
        {
            return file_get_data_home();
        }
        public static string file_get_data_home()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
    }
}
