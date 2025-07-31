using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.disc
{
    public struct BD_ENC_INFO
    {
        public byte aacs_detected;
        public byte libaacs_detected;
        public byte aacs_handled;
        public byte bdplus_detected;
        public byte libbdplus_detected;
        public byte bdplus_handled;
        public int aacs_error_code;
        public int aacs_mkbv;
        public byte[] disc_id = new byte[20];
        public byte bdplus_gen;
        public UInt32 bdplus_date;

        public byte no_menu_support;

        public BD_ENC_INFO() { }
    }
}
