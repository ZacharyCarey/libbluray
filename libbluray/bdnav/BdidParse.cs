using libbluray.bdnav;
using libbluray.disc;
using libbluray.file;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.bdnav
{
    internal class BDID_DATA
    {
        public string OrganizationID => org_id;
        internal string org_id = null;
        
        public string DiscID => disc_id;
        internal string disc_id = null;

        internal BDID_DATA() { }
    }

    internal static class BdidParse
    {
        const UInt32 BDID_SIG1 = ('B' << 24) | ('D' << 16) | ('I' << 8) | 'D';

        static bool _parse_header(Ref<BITSTREAM> bs, Ref<UInt32> data_start, Ref<UInt32> extension_data_start)
        {
            if (!BdmvParse.bdmv_parse_header(bs, BDID_SIG1, Ref<uint>.Null))
            {
                return false;
            }

            data_start.Value = bs.Value.bs_read<UInt32>(32);
            extension_data_start.Value = bs.Value.bs_read<UInt32>(32);

            return true;
        }

        static BDID_DATA? _bdid_parse(BD_FILE_H fp)
        {
            Variable<BITSTREAM> bs = new();
            BDID_DATA? bdid = null;

            Variable<UInt32> data_start = new(), extension_data_start = new();
            byte[] tmp = new byte[16];

            if (bs.Value.bs_init(fp) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, $"id.bdmv: read error");
                return null;
            }

            if (!_parse_header(bs.Ref, data_start.Ref, extension_data_start.Ref))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, "id.bdmv: invalid header");
                return null;
            }

            if (bs.Value.bs_seek_byte(40) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, "id.bdmv: read error");
                return null;
            }

            bdid = new BDID_DATA();
            bs.Value.bs_read_bytes(tmp, 4);
            Util.str_print_hex(out bdid.org_id, tmp, 4);

            bs.Value.bs_read_bytes(tmp, 16);
            Util.str_print_hex(out bdid.disc_id, tmp, 16);

            if (extension_data_start.Value != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, "id.bdmv: ignoring unknown extension data");
            }

            return bdid;
        }

        static BDID_DATA? _bdid_get(BD_DISC disc, string path)
        {
            BD_FILE_H? fp;
            BDID_DATA? bdid;

            fp = disc.disc_open_path(path);
            if (fp == null) {
                return null;
            }

            bdid = _bdid_parse(fp);
            fp.file_close();
            return bdid;
        }

        public static BDID_DATA? bdid_get(BD_DISC disc)
        {
            BDID_DATA? bdid;

            bdid = _bdid_get(disc, Path.Combine("CERTIFICATE", "id.bdmv"));

            /* if failed, try backup file */
            if (bdid == null)
            {
                bdid = _bdid_get(disc, Path.Combine("CERTIFICATE", "BACKUP", "id.bdmv"));
            }

            return bdid;
        }

        public static void bdid_free(ref BDID_DATA? p)
        {
            if (p != null)
            {
                p = null;
            }
        }
    }
}
