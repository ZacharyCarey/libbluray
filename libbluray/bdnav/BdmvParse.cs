using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.bdnav
{
    internal static class BdmvParse
    {
        public const UInt32 BDMV_VERSION_0100 = ('0' << 24) | ('1' << 16) | ('0' << 8) | '0';
        public const UInt32 BDMV_VERSION_0200 = ('0' << 24) | ('2' << 16) | ('0' << 8) | '0';
        public const UInt32 BDMV_VERSION_0240 = ('0' << 24) | ('2' << 16) | ('4' << 8) | '0';
        public const UInt32 BDMV_VERSION_0300 = ('0' << 24) | ('3' << 16) | ('0' << 8) | '0';

        private static byte[] U32CHARS(UInt32 u)
        {
            return [
                (byte)((u >> 24) & 0xFF),
                (byte)((u >> 16) & 0xFF),
                (byte)((u >> 8) & 0xFF),
                (byte)(u & 0xFF)
            ];
        }

        public static bool bdmv_parse_header(Ref<BITSTREAM> bs, UInt32 type, Ref<UInt32> version)
        {
            UInt32 tag, ver;

            if (bs.Value.bs_seek_byte(0) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"bdmv_parse_header({U32CHARS(type)}): seek failed");
                return false;
            }

            /* read and verify magic bytes and version code */

            if (bs.Value.bs_avail() / 8 < 8)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"bdmv_parse_header({U32CHARS(type)}): unexpected EOF");
                return false;
            }

            tag = bs.Value.bs_read<UInt32>(32);
            ver = bs.Value.bs_read<UInt32>(32);

            if (tag != type)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"bdmv_parse_header({U32CHARS(type)}): invalid signature {U32CHARS(tag)}");
                return false;
            }

            switch (ver)
            {
                case BDMV_VERSION_0100:
                case BDMV_VERSION_0200:
                case BDMV_VERSION_0240:
                case BDMV_VERSION_0300:
                    break;
                default:
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"bdmv_parse_header({U32CHARS(type)}): unsupported file version {U32CHARS(ver)}");
                    return false;
            }

            if (version)
            {
                version.Value = ver;
            }

            return true;
        }
    }
}
