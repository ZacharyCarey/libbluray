using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.util
{
    internal partial class Util
    {
        /*public static string str_dup(this string str)
        {
            return str;
        }*/

        public static UInt32 str_to_uint32(string s, int n)
        {
            return str_to_uint32(Encoding.ASCII.GetBytes(s), n);
        }

        public static UInt32 str_to_uint32(Span<byte> s, int n)
        {
            UInt32 val = 0;

            if (n > 4) n = 4;

            while (n-- != 0)
            {
                if (s.Length > 0)
                {
                    val = (val << 8) | s[0];
                    s = s.Slice(1);
                } else
                {
                    val = (val << 8);
                }
            }

            return val;
        }

        /*
        public static void str_tolower(this string s, out string result)
        {
            result = s.ToLower();
        }*/

        public static string str_print_hex(out string _out, Ref<byte> buf, int count)
        {
            _out = "";
            for (int zz = 0; zz < count; zz++)
            {
                _out += $"{buf[zz]:x2}";
            }
            return _out;
        }

        public static string str_print_hex(out string _out, ReadOnlySpan<byte> buf, int count)
        {
            _out = "";
            for (int zz = 0; zz < count; zz++)
            {
                _out += $"{buf[zz]:x2}";
            }
            return _out;
        }

        /*public static string str_strcasestr(string haystack, string needle)
        {
            int result = -1;

            int h = 0;
            int n = 0;
            if (haystack != null && needle != null)
            {
                haystack.str_tolower(out haystack);
                needle.str_tolower(out needle);
                result = haystack.IndexOf(needle);
                if (result >= 0)
                {
                    return haystack.Substring(result);
                }
            }

            return null;
        }*/

    }
}
