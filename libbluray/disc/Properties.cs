using libbluray.file;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace libbluray.disc
{
    internal static class Properties
    {
        /*
         * property name: ASCII string, no '=' or '\n'
         * property value: UTF-8 string, no '\n'
         */

        private const UInt32 MAX_PROP_FILE_SIZE = 64 * 1024;

        private static int _read_prop_file(string file, out string? data)
        {
            BD_FILE_H? fp = null;
            Int64 size = -1;

            data = null;

            if (Filesystem.file_path_exists(file) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE, $"Properties file {file} does not exist");
                data = null;
                if (data == null)
                {
                    return -1;
                }
                return 0;
            }

            fp = BD_FILE_H.file_open(file, true);
            if (fp == null)
            {
                goto unlink;
            }

            size = fp.file_size();
            if (size < 1 || size > MAX_PROP_FILE_SIZE)
            {
                goto unlink;
            }

            byte[] tmp = new byte[size];
            if (fp.file_read(tmp, (ulong)size) != (ulong)size)
            {
                goto unlink;
            }
            data = Encoding.ASCII.GetString(tmp);

            fp.file_close();
            return 0;

        unlink:
            Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"Removing invalid properties file {file} ({size} bytes)");

            data = null;
            if (fp != null)
            {
                fp.file_close();
            }
            /*if (file.file_unlink(file) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE, "Error removing invalid properties file");
            }*/
            data = null;
            return (data != null) ? 0 : -1;
        }

        private static int _write_prop_file(string file, ReadOnlySpan<byte> data)
        {
            BD_FILE_H fp = null;
            UInt64 size;
            Int64 written;

            size = (UInt64)data.Length;
            if (size > MAX_PROP_FILE_SIZE)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"Not writing too large properties file: {file} is {size} bytes");
                return -1;
            }

            if (Filesystem.file_mkdirs(file) < 0)
            {
                return -1;
            }

            fp = BD_FILE_H.file_open(file, false);
            if (fp == null)
            {
                return -1;
            }

            written = fp.write(data, (Int64)size);
            fp.file_close();

            if (written != (Int64)size)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE, $"Writing properties file {file} failed");
                /*if (file.file_unlink(file) < 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_FILE, $"Error removing properties file {file}");
                }*/
                return -1;
            }

            return 0;
        }

        private static string _scan_prop(string data, string key, ref UInt64 data_size)
        {
            UInt64 key_size = (UInt64)key.Length;

            foreach(var entry in data.Split('\n'))
            {
                if (entry.StartsWith(key))
                {
                    string result = data[key.Length..];
                    data_size = (ulong)result.Length;
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Add / replace property value in file
        /// </summary>
        /// <param name="file">full path to properties file</param>
        /// <param name="_property">property name</param>
        /// <param name="val">value for property</param>
        /// <returns>0 on success, -1 on error</returns>
        internal static int properties_put(string file, string _property, string val)
        {
            string key = null, old_data = null, new_data = null;
            string old_val;
            UInt64 old_size = 0;
            int result = -1;

            if (_property.IndexOf('\n') >= 0 || _property.IndexOf('=') >= 0 || val.IndexOf('\n') >= 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"Ignoring invalid property '{_property}'='{val}'");
                goto _out;
            }

            if (_read_prop_file(file, out old_data) < 0)
            {
                goto _out;
            }

            key = $"{_property}=";
            if (key == null)
            {
                goto _out;
            }

            old_val = _scan_prop(old_data, key, ref old_size);
            if (old_val == null)
            {
                new_data = $"{old_data}{key}{val}";
            }
            else
            {
                old_val = null;
                new_data = $"{old_data}{val}{(old_val + old_size)}";
            }

            if (new_data == null)
            {
                goto _out;
            }

            result = _write_prop_file(file, Encoding.ASCII.GetBytes(new_data));

        _out:
            old_data = null;
            new_data = null;
            key = null;
            return result;
        }

        /// <summary>
        /// Read property value from file.
        /// </summary>
        /// <param name="file">full path to properties file</param>
        /// <param name="_property">property name</param>
        /// <returns>value or null</returns>
        internal static string properties_get(string file, string _property)
        {
            string key, data;
            UInt64 data_size = 0;
            string result = null;

            if (_property.IndexOf('\n') >= 0 || _property.IndexOf('=') >= 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"Ignoring invalid property '{_property}'");
                return null;
            }

            if (_read_prop_file(file, out data) < 0)
            {
                return null;
            }

            key = $"{_property}=";
            if (key == null)
            {
                data = null;
                return null;
            }

            result = _scan_prop(data, key, ref data_size);
            if (result != null)
            {
                //result[(int)data_size] = 0;
                result = result;
            }

            key = null;
            data = null;
            return result;
        }

    }
}
