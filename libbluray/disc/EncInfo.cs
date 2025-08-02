using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.disc
{
    internal class BD_ENC_INFO
    {
        public byte AacsDetected => aacs_detected;
        internal byte aacs_detected;

        public byte LibAacsDetected => libaacs_detected;
        internal byte libaacs_detected;

        public byte AacsHandles => aacs_handled;
        internal byte aacs_handled;

        public byte BdPlusDetected => bdplus_detected;
        internal byte bdplus_detected;

        public byte LibBdPlusDetected => libbdplus_detected;
        internal byte libbdplus_detected;

        public byte BdPlusHandles => bdplus_handled;
        internal byte bdplus_handled;

        public int AacsErrorCode => aacs_error_code;
        internal int aacs_error_code;

        public int AacsMkbv => aacs_mkbv;
        internal int aacs_mkbv;

        public string DiscID => disc_id;
        internal string disc_id = "";

        public byte BdPlusGen => bdplus_gen;
        internal byte bdplus_gen;

        public uint BdPlusDate => bdplus_date;
        internal UInt32 bdplus_date;

        public byte NoMenuSupport => no_menu_support;
        internal byte no_menu_support;

        internal BD_ENC_INFO() { }
    }
}
