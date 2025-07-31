using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.BlurayInfo
{
    public class bluray_pgs
    {
        public byte pg_stream_number;
        public ushort pid;

        public string lang = "";
    }

    public partial class BlurayInfo
    {
        public static void bluray_pgs_lang(out string str, string lang)
        {
            str = lang;
        }
    }
}
