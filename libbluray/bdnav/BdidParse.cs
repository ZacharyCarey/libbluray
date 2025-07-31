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
    public struct BDID_DATA
    {
        public string org_id;
        public string disc_id;
    }

    public static class BdidParse
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

        static Ref<BDID_DATA> _bdid_parse(BD_FILE_H fp)
        {
            Variable<BITSTREAM> bs = new();
            Ref<BDID_DATA> bdid = Ref<BDID_DATA>.Null;

            Variable<UInt32> data_start = new(), extension_data_start = new();
            byte[] tmp = new byte[16];

            if (bs.Value.bs_init(fp) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, $"id.bdmv: read error");
                return Ref<BDID_DATA>.Null;
            }

            if (!_parse_header(bs.Ref, data_start.Ref, extension_data_start.Ref))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, "id.bdmv: invalid header");
                return Ref<BDID_DATA>.Null;
            }

            if (bs.Value.bs_seek_byte(40) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, "id.bdmv: read error");
                return Ref<BDID_DATA>.Null;
            }

            bdid = Ref<BDID_DATA>.Allocate();
            bs.Value.bs_read_bytes(tmp, 4);
            Util.str_print_hex(out bdid.Value.org_id, tmp, 4);

            bs.Value.bs_read_bytes(tmp, 16);
            Util.str_print_hex(out bdid.Value.disc_id, tmp, 16);

            if (extension_data_start.Value != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, "id.bdmv: ignoring unknown extension data");
            }

            return bdid;
        }

        static Ref<BDID_DATA> _bdid_get(BD_DISC disc, string path)
        {
            BD_FILE_H? fp;
            Ref<BDID_DATA> bdid;

            fp = disc.disc_open_path(path);
            if (fp == null) {
                return Ref<BDID_DATA>.Null;
            }

            bdid = _bdid_parse(fp);
            fp.file_close();
            return bdid;
        }

        public static Ref<BDID_DATA> bdid_get(BD_DISC disc)
        {
            Ref<BDID_DATA> bdid;

            bdid = _bdid_get(disc, Path.Combine("CERTIFICATE", "id.bdmv"));

            /* if failed, try backup file */
            if (!bdid)
            {
                bdid = _bdid_get(disc, Path.Combine("CERTIFICATE", "BACKUP", "id.bdmv"));
            }

            return bdid;
        }

        public static void bdid_free(ref Ref<BDID_DATA> p)
        {
            if (p)
            {
                p.Free();
            }
        }
    }
}
