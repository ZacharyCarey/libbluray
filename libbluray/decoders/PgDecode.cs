using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{
    internal static class PgDecode
    {
        internal static void pg_decode_video_descriptor(Ref<BITBUFFER> bb, Ref<BD_PG_VIDEO_DESCRIPTOR> p)
        {
            p.Value.video_width = bb.Value.bb_read<UInt16>(16);
            p.Value.video_height = bb.Value.bb_read<UInt16>(16);
            p.Value.frame_rate = bb.Value.bb_read<byte>(4);
            bb.Value.bb_skip(4);
        }

        internal static void pg_decode_composition_descriptor(Ref<BITBUFFER> bb, Ref<BD_PG_COMPOSITION_DESCRIPTOR> p)
        {
            p.Value.number = bb.Value.bb_read<UInt16>(16);
            p.Value.state = bb.Value.bb_read<byte>(2);
            bb.Value.bb_skip(6);
        }

        internal static void pg_decode_sequence_descriptor(Ref<BITBUFFER> bb, Ref<BD_PG_SEQUENCE_DESCRIPTOR> p)
        {
            p.Value.first_in_seq = bb.Value.bb_read<byte>(8);
            p.Value.last_in_seq = bb.Value.bb_read<byte>(8);
            bb.Value.bb_skip(6);
        }

        internal static void pg_decode_window(Ref<BITBUFFER> bb, Ref<BD_PG_WINDOW> p)
        {
            p.Value.id = bb.Value.bb_read<byte>(8);
            p.Value.x = bb.Value.bb_read<UInt16>(16);
            p.Value.y = bb.Value.bb_read<UInt16>(16);
            p.Value.width = bb.Value.bb_read<UInt16>(16);
            p.Value.height = bb.Value.bb_read<UInt16>(16);
        }

        internal static void pg_decode_composition_object(Ref<BITBUFFER> bb, Ref<BD_PG_COMPOSITION_OBJECT> p)
        {
            p.Value.object_id_ref = bb.Value.bb_read<UInt16>(16);
            p.Value.window_id_ref = bb.Value.bb_read<byte>(8);

            p.Value.crop_flag = bb.Value.bb_read<byte>(1);
            p.Value.forced_on_flag = bb.Value.bb_read<byte>(1);
            bb.Value.bb_skip(6);

            p.Value.x = bb.Value.bb_read<UInt16>(16);
            p.Value.y = bb.Value.bb_read<UInt16>(16);

            if (p.Value.crop_flag != 0)
            {
                p.Value.crop_x = bb.Value.bb_read<UInt16>(16);
                p.Value.crop_y = bb.Value.bb_read<UInt16>(16);
                p.Value.crop_w = bb.Value.bb_read<UInt16>(16);
                p.Value.crop_h = bb.Value.bb_read<UInt16>(16);
            }
        }

        internal static void pg_decode_palette_entry(Ref<BITBUFFER> bb, Ref<BD_PG_PALETTE_ENTRY> entry)
        {
            byte entry_id = bb.Value.bb_read<byte>(8);

            entry[entry_id].Y = bb.Value.bb_read<byte>(8);
            entry[entry_id].Cr = bb.Value.bb_read<byte>(8);
            entry[entry_id].Cb = bb.Value.bb_read<byte>(8);
            entry[entry_id].T = bb.Value.bb_read<byte>(8);
        }

        /*
         * segments
         */

        internal static bool pg_decode_palette_update(Ref<BITBUFFER> bb, Ref<BD_PG_PALETTE> p)
        {
            p.Value.id = bb.Value.bb_read<byte>(8);
            p.Value.version = bb.Value.bb_read<byte>(8);

            while (!bb.Value.bb_eof())
            {
                pg_decode_palette_entry(bb, new Ref<BD_PG_PALETTE_ENTRY>(p.Value.entry));
            }

            return true;
        }

        internal static bool pg_decode_palette(Ref<BITBUFFER> bb, Ref<BD_PG_PALETTE> p)
        {
            Array.Fill(p.Value.entry, new());

            return pg_decode_palette_update(bb, p);
        }

        internal static bool _decode_rle(Ref<BITBUFFER> bb, Ref<BD_PG_OBJECT> p)
        {
            Ref<BD_PG_RLE_ELEM> tmp;
            int pixels_left = p.Value.width * p.Value.height;
            int num_rle = 0;
            int rle_size = p.Value.width * p.Value.height / 4;

            if (rle_size < 1)
                rle_size = 1;

            tmp = p.Value.img.Reallocate((ulong)rle_size);
            if (!tmp)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, "pg_decode_object(): realloc failed");
                return false;
            }
            p.Value.img = tmp;

            while (!bb.Value.bb_eof())
            {
                UInt32 len = 1;
                byte color = 0;

                if ((color = bb.Value.bb_read<byte>(8)) == 0)
                {
                    if (bb.Value.bb_read<byte>(8) == 0)
                    {
                        if (bb.Value.bb_read<byte>(8) == 0)
                        {
                            len = bb.Value.bb_read<byte>(6);
                        }
                        else
                        {
                            len = bb.Value.bb_read<UInt32>(14);
                        }
                    }
                    else
                    {
                        if (bb.Value.bb_read<byte>(8) == 0)
                        {
                            len = bb.Value.bb_read<byte>(6);
                        }
                        else
                        {
                            len = bb.Value.bb_read<UInt32>(14);
                        }
                        color = bb.Value.bb_read<byte>(8);
                    }
                }

                p.Value.img[num_rle].len = (ushort)len;
                p.Value.img[num_rle].color = color;

                pixels_left -= (int)len;

                if (pixels_left < 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"pg_decode_object(): too many pixels ({(-pixels_left)})");
                    return false;
                }

                num_rle++;
                if (num_rle >= rle_size)
                {
                    rle_size *= 2;
                    tmp = p.Value.img.Reallocate((ulong)rle_size);
                    if (!tmp)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, "pg_decode_object(): realloc failed");
                        return false;
                    }
                    p.Value.img = tmp;
                }
            }

            if (pixels_left > 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"pg_decode_object(): missing {pixels_left} pixels");
                return false;
            }

            return true;
        }

        internal static bool pg_decode_object(Ref<BITBUFFER> bb, Ref<BD_PG_OBJECT> p)
        {
            Variable<BD_PG_SEQUENCE_DESCRIPTOR> sd = new();

            p.Value.id = bb.Value.bb_read<UInt16>(16);
            p.Value.version = bb.Value.bb_read<byte>(8);

            pg_decode_sequence_descriptor(bb, sd.Ref);

            /* splitted segments should be already joined */
            if (sd.Value.first_in_seq == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, "pg_decode_object(): not first in sequence");
                return false;
            }
            if (sd.Value.last_in_seq == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, "pg_decode_object(): not last in sequence");
                return false;
            }

            if (!bb.Value.bb_is_align(0x07))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, "pg_decode_object(): alignment error");
                return false;
            }

            UInt32 data_len = bb.Value.bb_read<UInt32>(24);
            UInt32 buf_len = (UInt32)(bb.Value.p_end - bb.Value.p);
            if (data_len != buf_len)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"pg_decode_object(): buffer size mismatch (expected {data_len}, have {buf_len})");
                return false;
            }

            p.Value.width = bb.Value.bb_read<UInt16>(16);
            p.Value.height = bb.Value.bb_read<UInt16>(16);

            return _decode_rle(bb, p);
        }

        internal static bool pg_decode_composition(Ref<BITBUFFER> bb, Ref<BD_PG_COMPOSITION> p)
        {
            uint ii;

            pg_decode_video_descriptor(bb, p.Value.video_descriptor.Ref);
            pg_decode_composition_descriptor(bb, p.Value.composition_descriptor.Ref);

            p.Value.palette_update_flag = bb.Value.bb_read<byte>(8);
            bb.Value.bb_skip(7);

            p.Value.palette_id_ref = bb.Value.bb_read<byte>(8);

            p.Value.num_composition_objects = bb.Value.bb_read<byte>(8);
            p.Value.composition_object = Ref<BD_PG_COMPOSITION_OBJECT>.Allocate(p.Value.num_composition_objects);

            for (ii = 0; ii < p.Value.num_composition_objects; ii++)
            {
                pg_decode_composition_object(bb, p.Value.composition_object.AtIndex(ii));
            }

            return true;
        }

        internal static bool pg_decode_windows(Ref<BITBUFFER> bb, Ref<BD_PG_WINDOWS> p)
        {
            uint ii;

            p.Value.num_windows = bb.Value.bb_read<byte>(8);
            p.Value.window = Ref<BD_PG_WINDOW>.Allocate(p.Value.num_windows);

            for (ii = 0; ii < p.Value.num_windows; ii++)
            {
                pg_decode_window(bb, p.Value.window.AtIndex(ii));
            }

            return true;
        }

        /*
         * cleanup
         */

        public static void pg_clean_object(Ref<BD_PG_OBJECT> p)
        {
            if (p)
            {
                p.Value.img = Ref<BD_PG_RLE_ELEM>.Null;
            }
        }

        public static void pg_clean_windows(Ref<BD_PG_WINDOWS> p)
        {
            if (p)
            {
                p.Value.window.Free();
            }
        }

        public static void pg_clean_composition(Ref<BD_PG_COMPOSITION> p)
        {
            if (p)
            {
                p.Value.composition_object.Free();
            }
        }

        public static void pg_free_composition(ref Ref<BD_PG_COMPOSITION> p)
        {
            if (p)
            {
                pg_clean_composition(p);
                p.Free();
            }
        }
    }
}
