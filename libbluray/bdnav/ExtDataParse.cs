using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.bdnav
{
    internal static class ExtDataParse
    {
        public static bool bdmv_parse_extension_data<T>(Ref<BITSTREAM> bits, int start_address, Func<Ref<BITSTREAM>, int, int, Ref<T>, bool> handler, Ref<T> handle) where T : struct
        {
            Int64 length;
            int num_entries, n;

            if (start_address < 1) return false;
            if (start_address > bits.Value.end - 12) return false;

            if (bits.Value.bs_seek_byte(start_address) < 0)
            {
                return false;
            }

            length = bits.Value.bs_read<uint>(32); /* length of extension data block */
            if (length < 1) return false;
            bits.Value.bs_skip(32); /* relative start address of extension data */
            bits.Value.bs_skip(24); /* padding */
            num_entries = bits.Value.bs_read<int>(8);

            if (start_address > bits.Value.end - 12 - num_entries * 12) return false;

            for (n = 0; n < num_entries; n++)
            {
                UInt16 id1 = bits.Value.bs_read<UInt16>(16);
                UInt16 id2 = bits.Value.bs_read<UInt16>(16);
                Int64 ext_start = bits.Value.bs_read<uint>(32);
                Int64 ext_len = bits.Value.bs_read<uint>(32);

                Int64 saved_pos = bits.Value.bs_pos() >> 3;

                if (ext_start + start_address + ext_len > bits.Value.end) return false;

                if (bits.Value.bs_seek_byte(start_address + ext_start) >= 0)
                {
                    handler.Invoke(bits, id1, id2, handle);
                }

                if (bits.Value.bs_seek_byte(saved_pos) < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
