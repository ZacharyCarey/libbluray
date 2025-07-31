using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{
    public static class TextstDecode
    {
        static sbyte _decode_int8(Ref<BITBUFFER> bb)
        {
            uint sign = bb.Value.bb_read<byte>(1);
            sbyte val = bb.Value.bb_read<sbyte>(7);
            return (sign != 0) ? (sbyte)(-val) : val;
        }

        static Int16 _decode_int16(Ref<BITBUFFER> bb)
        {
            uint sign = bb.Value.bb_read<byte>(1);
            Int16 val = bb.Value.bb_read<Int16>(15);
            return (sign != 0) ? (Int16)(-val) : val;
        }

        static Int64 _decode_pts(Ref<BITBUFFER> bb)
        {
            return ((Int64)bb.Value.bb_read<byte>(1)) << 32 | bb.Value.bb_read<UInt32>(32);
        }

        static void _decode_rect(Ref<BITBUFFER> bb, Ref<BD_TEXTST_RECT> p)
        {
            p.Value.xpos = bb.Value.bb_read<UInt16>(16); ;
            p.Value.ypos = bb.Value.bb_read<UInt16>(16); ;
            p.Value.width = bb.Value.bb_read<UInt16>(16); ;
            p.Value.height = bb.Value.bb_read<UInt16>(16); ;
        }

        static void _decode_region_info(Ref<BITBUFFER> bb, Ref<BD_TEXTST_REGION_INFO> p)
        {
            _decode_rect(bb, p.Value.region.Ref);
            p.Value.background_color = bb.Value.bb_read<byte>(8);
            bb.Value.bb_skip(8);
        }

        static void _decode_font_style(Ref<BITBUFFER> bb, Ref<BD_TEXTST_FONT_STYLE> p)
        {
            byte font_style = bb.Value.bb_read<byte>(8);
            p.Value.bold = ((font_style & 1) != 0);
            p.Value.italic = ((font_style & 2) != 0);
            p.Value.outline_border = ((font_style & 4) != 0);
        }

        static void _decode_region_style(Ref<BITBUFFER> bb, Ref<BD_TEXTST_REGION_STYLE> p)
        {
            p.Value.region_style_id = bb.Value.bb_read<byte>(8);

            _decode_region_info(bb, p.Value.region_info.Ref);
            _decode_rect(bb, p.Value.text_box.Ref);

            p.Value.text_flow = bb.Value.bb_read<byte>(8);
            p.Value.text_halign = bb.Value.bb_read<byte>(8);
            p.Value.text_valign = bb.Value.bb_read<byte>(8);
            p.Value.line_space = bb.Value.bb_read<byte>(8);
            p.Value.font_id_ref = bb.Value.bb_read<byte>(8);

            _decode_font_style(bb, p.Value.font_style.Ref);

            p.Value.font_size = bb.Value.bb_read<byte>(8);
            p.Value.font_color = bb.Value.bb_read<byte>(8);
            p.Value.outline_color = bb.Value.bb_read<byte>(8);
            p.Value.outline_thickness = bb.Value.bb_read<byte>(8);
        }

        static void _decode_user_style(Ref<BITBUFFER> bb, Ref<BD_TEXTST_USER_STYLE> p)
        {
            p.Value.user_style_id = bb.Value.bb_read<byte>(8);
            p.Value.region_hpos_delta = _decode_int16(bb);
            p.Value.region_vpos_delta = _decode_int16(bb);
            p.Value.text_box_hpos_delta = _decode_int16(bb);
            p.Value.text_box_vpos_delta = _decode_int16(bb);
            p.Value.text_box_width_delta = _decode_int16(bb);
            p.Value.text_box_height_delta = _decode_int16(bb);
            p.Value.font_size_delta = _decode_int8(bb);
            p.Value.line_space_delta = _decode_int8(bb);
        }

        static bool _decode_dialog_region(Ref<BITBUFFER> bb, Ref<BD_TEXTST_DIALOG_REGION> p)
        {
            p.Value.continous_present_flag = bb.Value.bb_read<byte>(1);
            p.Value.forced_on_flag = bb.Value.bb_read<byte>(1);
            bb.Value.bb_skip(6);
            p.Value.region_style_id_ref = bb.Value.bb_read<byte>(8);

            UInt16 data_length = bb.Value.bb_read<UInt16>(16);
            int bytes_allocated = data_length;
            UInt16 bytes_read = 0;

            p.Value.elem = Ref<BD_TEXTST_DATA>.Null; //Ref<BD_TEXTST_DATA>.Allocate(bytes_allocated);
            p.Value.elem_count = 0;
            p.Value.line_count = 1;

            //Ref<byte> ptr = (uint8_t*)p.Value.elem;
            List<BD_TEXTST_DATA> elems = new();

            while (bytes_read < data_length)
            {

                /* parse header */

                byte code = bb.Value.bb_read<byte>(8);
                bytes_read++;
                if (code != 0x1b)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DECODE, "_decode_dialog_region(): missing escape");
                    continue;
                }

                byte type = bb.Value.bb_read<byte>(8);
                byte length = bb.Value.bb_read<byte>(8);
                bytes_read += (ushort)(2u + length);

                /* realloc */

                /*int bytes_used = ((intptr_t)ptr - (intptr_t)p.Value.elem);
                int need = bytes_used + length + sizeof(BD_TEXTST_DATA);
                if (bytes_allocated < need)
                {
                    bytes_allocated = need * 2;
                    BD_TEXTST_DATA* tmp = realloc(p.Value.elem, bytes_allocated);
                    if (!tmp)
                    {
                        BD_DEBUG(DBG_DECODE | DBG_CRIT, "out of memory\n");
                        return 0;
                    }
                    p.Value.elem = tmp;
                    ptr = ((uint8_t*)p.Value.elem) + bytes_used;
                }*/

                /* parse content */

                BD_TEXTST_DATA data = new();
                //memset(data, 0, sizeof(*data));

                data.type = (BD_TEXTST_DATA_Type)type;
                switch (data.type)
                {
                    case BD_TEXTST_DATA_Type.BD_TEXTST_DATA_STRING:
                        data._string = bb.Value.bb_read_string(length);
                        data.length = length;
                        //ptr += length;
                        break;
                    case BD_TEXTST_DATA_Type.BD_TEXTST_DATA_FONT_ID:
                        data.font_id_ref = bb.Value.bb_read<byte>(8);
                        break;
                    case BD_TEXTST_DATA_Type.BD_TEXTST_DATA_FONT_STYLE:
                        _decode_font_style(bb, data.style.Ref);
                        data.outline_color = bb.Value.bb_read<byte>(8);
                        data.outline_thickness = bb.Value.bb_read<byte>(8);
                        break;
                    case BD_TEXTST_DATA_Type.BD_TEXTST_DATA_FONT_SIZE:
                        data.font_size = bb.Value.bb_read<byte>(8);
                        break;
                    case BD_TEXTST_DATA_Type.BD_TEXTST_DATA_FONT_COLOR:
                        data.font_color = bb.Value.bb_read<byte>(8);
                        break;
                    case BD_TEXTST_DATA_Type.BD_TEXTST_DATA_NEWLINE:
                        p.Value.line_count++;
                        break;
                    case BD_TEXTST_DATA_Type.BD_TEXTST_DATA_RESET_STYLE:
                        break;
                    default:
                        Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"_decode_dialog_region(): unknown marker {type} (len {length})");
                        bb.Value.bb_skip(8u * length);
                        continue;
                }
                //ptr += sizeof(BD_TEXTST_DATA);
                p.Value.elem_count++;
                elems.Add(data);
            }

            p.Value.elem = Ref<BD_TEXTST_DATA>.Allocate(elems.Count);
            for(int i = 0; i < elems.Count; i++)
            {
                p.Value.elem[i] = elems[i];
            }

            return true;
        }

        static void _decode_palette(Ref<BITBUFFER> bb, Ref<BD_PG_PALETTE_ENTRY> p)
        {
            UInt16 entries = (ushort)(bb.Value.bb_read<UInt16>(16) / 5u);
            uint ii;

            p.AsSpan().Slice(0, 256).Fill(new());
            for (ii = 0; ii < entries; ii++)
            {
                PgDecode.pg_decode_palette_entry(bb, p);
            }
        }

        /*
         * segments
         */


        internal static bool textst_decode_dialog_style(Ref<BITBUFFER> bb, Ref<BD_TEXTST_DIALOG_STYLE> p)
        {
            uint ii;

            p.Value.player_style_flag = bb.Value.bb_read<byte>(1);
            bb.Value.bb_skip(15);
            p.Value.region_style_count = bb.Value.bb_read<byte>(8);
            p.Value.user_style_count = bb.Value.bb_read<byte>(8);

            if (p.Value.region_style_count != 0)
            {
                p.Value.region_style = Ref<BD_TEXTST_REGION_STYLE>.Allocate(p.Value.region_style_count);
                for (ii = 0; ii < p.Value.region_style_count; ii++)
                {
                    _decode_region_style(bb, p.Value.region_style.AtIndex(ii));
                }
            }

            if (p.Value.user_style_count != 0)
            {
                p.Value.user_style = Ref<BD_TEXTST_USER_STYLE>.Allocate(p.Value.user_style_count);
                for (ii = 0; ii < p.Value.user_style_count; ii++)
                {
                    _decode_user_style(bb, p.Value.user_style.AtIndex(ii));
                }
            }

            _decode_palette(bb, new Ref<BD_PG_PALETTE_ENTRY>(p.Value.palette));

            return true;
        }

        internal static bool textst_decode_dialog_presentation(Ref<BITBUFFER> bb, Ref<BD_TEXTST_DIALOG_PRESENTATION> p)
        {
            uint ii, palette_update_flag;

            bb.Value.bb_skip(7);
            p.Value.start_pts = _decode_pts(bb);
            bb.Value.bb_skip(7);
            p.Value.end_pts = _decode_pts(bb);

            palette_update_flag = bb.Value.bb_read<byte>(1);
            bb.Value.bb_skip(7);

            if (palette_update_flag != 0)
            {
                p.Value.palette_update = Ref<BD_PG_PALETTE_ENTRY>.Allocate(256);
                _decode_palette(bb, p.Value.palette_update);
            }

            p.Value.region_count = bb.Value.bb_read<byte>(8);
            if (p.Value.region_count != 0)
            {
                if (p.Value.region_count > 2)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, $"too many regions ({p.Value.region_count})");
                    return false;
                }
                for (ii = 0; ii < p.Value.region_count; ii++)
                {
                    if (!_decode_dialog_region(bb, new Ref<BD_TEXTST_DIALOG_REGION>(p.Value.region).AtIndex(ii)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /*
         * cleanup
         */

        internal static void textst_clean_dialog_presentation(Ref<BD_TEXTST_DIALOG_PRESENTATION> p)
        {
            if (p)
            {
                p.Value.palette_update.Free();
                p.Value.region[0].elem.Free();
                p.Value.region[1].elem.Free();
            }
        }

        static void _clean_style(Ref<BD_TEXTST_DIALOG_STYLE> p)
        {
            if (p)
            {
                p.Value.region_style.Free();
                p.Value.user_style.Free();
            }
        }

        internal static void textst_free_dialog_style(ref Ref<BD_TEXTST_DIALOG_STYLE> p)
        {
            if (p)
            {
                _clean_style(p);
                p.Free();
            }
        }
    }
}
