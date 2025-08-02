using libbluray.bdnav;
using libbluray.hdmv;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{
    internal static class IgDecode
    {
        public const int MAX_NUM_BOGS = 256;
        public const int IG_UI_MODEL_ALWAYS_ON = 0;
        public const int IG_UI_MODEL_POPUP = 1;

        static bool _decode_button(Ref<BITBUFFER> bb, Ref<BD_IG_BUTTON> p)
        {
            uint ii;

            p.Value.id = bb.Value.bb_read<UInt16>(16);

            p.Value.numeric_select_value = bb.Value.bb_read<UInt16>(16);
            p.Value.auto_action_flag = bb.Value.bb_read<byte>(1);
            bb.Value.bb_skip(7);

            p.Value.x_pos = bb.Value.bb_read<UInt16>(16);
            p.Value.y_pos = bb.Value.bb_read<UInt16>(16);

            p.Value.upper_button_id_ref = bb.Value.bb_read<UInt16>(16);
            p.Value.lower_button_id_ref = bb.Value.bb_read<UInt16>(16);
            p.Value.left_button_id_ref = bb.Value.bb_read<UInt16>(16);
            p.Value.right_button_id_ref = bb.Value.bb_read<UInt16>(16);

            p.Value.normal_start_object_id_ref = bb.Value.bb_read<UInt16>(16);
            p.Value.normal_end_object_id_ref = bb.Value.bb_read<UInt16>(16);
            p.Value.normal_repeat_flag = bb.Value.bb_read<byte>(1);
            bb.Value.bb_skip(7);

            p.Value.selected_sound_id_ref = bb.Value.bb_read<byte>(8);
            p.Value.selected_start_object_id_ref = bb.Value.bb_read<UInt16>(16);
            p.Value.selected_end_object_id_ref = bb.Value.bb_read<UInt16>(16);
            p.Value.selected_repeat_flag = bb.Value.bb_read<byte>(1);
            bb.Value.bb_skip(7);

            p.Value.activated_sound_id_ref = bb.Value.bb_read<byte>(8);
            p.Value.activated_start_object_id_ref = bb.Value.bb_read<UInt16>(16);
            p.Value.activated_end_object_id_ref = bb.Value.bb_read<UInt16>(16);

            p.Value.num_nav_cmds = bb.Value.bb_read<UInt16>(16);
            p.Value.nav_cmds = Ref< MOBJ_CMD>.Allocate(p.Value.num_nav_cmds);

            for (ii = 0; ii < p.Value.num_nav_cmds; ii++)
            {
                byte[] buf = new byte[12];
                bb.Value.bb_read_bytes(buf, 12);

                MobjParse.mobj_parse_cmd(new Ref<byte>(buf), p.Value.nav_cmds.AtIndex(ii));
            }

            return true;
        }

        static void _clean_button(Ref<BD_IG_BUTTON> p)
        {
            p.Value.nav_cmds.Free();
        }

        static bool _decode_bog(Ref<BITBUFFER> bb, Ref<BD_IG_BOG> p)
        {
            uint ii;

            p.Value.default_valid_button_id_ref = bb.Value.bb_read<UInt16>(16);

            p.Value.num_buttons = bb.Value.bb_read<byte>(8);
            p.Value.button = Ref<BD_IG_BUTTON>.Allocate(p.Value.num_buttons);

            for (ii = 0; ii < p.Value.num_buttons; ii++)
            {
                if (!_decode_button(bb, p.Value.button.AtIndex(ii)))
                {
                    return false;
                }
            }

            return true;
        }

        static void _clean_bog(Ref<BD_IG_BOG> p)
        {
            uint ii;

            if (p.Value.button)
            {
                for (ii = 0; ii < p.Value.num_buttons; ii++)
                {
                    _clean_button(p.Value.button.AtIndex(ii));
                }
            }

            p.Value.button.Free();
        }

        static bool _decode_effect(Ref<BITBUFFER> bb, Ref<BD_IG_EFFECT> p)
        {
            uint ii;

            p.Value.duration = bb.Value.bb_read<uint>(24);
            p.Value.palette_id_ref = bb.Value.bb_read<byte>(8);

            p.Value.num_composition_objects = bb.Value.bb_read<byte>(8);
            p.Value.composition_object = Ref< BD_PG_COMPOSITION_OBJECT>.Allocate(p.Value.num_composition_objects);

            for (ii = 0; ii < p.Value.num_composition_objects; ii++)
            {
                PgDecode.pg_decode_composition_object(bb, p.Value.composition_object.AtIndex(ii));
            }

            return true;
        }

        static void _clean_effect(Ref<BD_IG_EFFECT> p)
        {
            p.Value.composition_object.Free();
        }

        static bool _decode_effect_sequence(Ref<BITBUFFER> bb, Ref<BD_IG_EFFECT_SEQUENCE> p)
        {
            uint ii;

            p.Value.num_windows = bb.Value.bb_read<byte>(8);
            p.Value.window = Ref<BD_PG_WINDOW>.Allocate(p.Value.num_windows);

            for (ii = 0; ii < p.Value.num_windows; ii++)
            {
                PgDecode.pg_decode_window(bb, p.Value.window.AtIndex(ii));
            }

            p.Value.num_effects = bb.Value.bb_read<byte>(8);
            p.Value.effect = Ref<BD_IG_EFFECT>.Allocate(p.Value.num_effects);

            for (ii = 0; ii < p.Value.num_effects; ii++)
            {
                if (!_decode_effect(bb, p.Value.effect.AtIndex(ii)))
                {
                    return false;
                }
            }

            return true;
        }

        static void _clean_effect_sequence(Ref<BD_IG_EFFECT_SEQUENCE> p)
        {
            uint ii;

            if (p.Value.effect)
            {
                for (ii = 0; ii < p.Value.num_effects; ii++)
                {
                    _clean_effect(p.Value.effect.AtIndex(ii));
                }
            }

            p.Value.effect.Free();

            p.Value.window.Free();
        }


        static bool _decode_uo_mask_table(Ref<BITBUFFER> bb, Ref<BD_UO_MASK> p)
        {
            byte[] buf = new byte[8];
            bb.Value.bb_read_bytes(buf, 8);

            return BD_UO_MASK.uo_mask_parse(new Ref<byte>(buf), p);
        }

        static bool _decode_page(Ref<BITBUFFER> bb, Ref<BD_IG_PAGE> p)
        {
            uint ii;

            p.Value.id = bb.Value.bb_read<byte>(8);
            p.Value.version = bb.Value.bb_read<byte>(8);

            _decode_uo_mask_table(bb, p.Value.uo_mask_table.Ref);

            if (!_decode_effect_sequence(bb, p.Value.in_effects.Ref))
            {
                return false;
            }
            if (!_decode_effect_sequence(bb, p.Value.out_effects.Ref))
            {
                return false;
            }

            p.Value.animation_frame_rate_code = bb.Value.bb_read<byte>(8);
            p.Value.default_selected_button_id_ref = bb.Value.bb_read<UInt16>(16);
            p.Value.default_activated_button_id_ref = bb.Value.bb_read<UInt16>(16);
            p.Value.palette_id_ref = bb.Value.bb_read<byte>(8);

            p.Value.num_bogs = bb.Value.bb_read<byte>(8);
            p.Value.bog = Ref<BD_IG_BOG>.Allocate(p.Value.num_bogs);

            for (ii = 0; ii < p.Value.num_bogs; ii++)
            {
                if (!_decode_bog(bb, p.Value.bog.AtIndex(ii)))
                {
                    return false;
                }
            }

            return true;
        }

        static void _clean_page(Ref<BD_IG_PAGE> p)
        {
            uint ii;

            _clean_effect_sequence(p.Value.in_effects.Ref);
            _clean_effect_sequence(p.Value.out_effects.Ref);

            if (p.Value.bog)
            {
                for (ii = 0; ii < p.Value.num_bogs; ii++)
                {
                    _clean_bog(p.Value.bog.AtIndex(ii));
                }
            }

            p.Value.bog.Free();
        }

        static UInt64 bb_read_u64(Ref<BITBUFFER> bb, int i_count)
        {
            UInt64 result = 0;
            if (i_count > 32)
            {
                i_count -= 32;
                result = (UInt64)bb.Value.bb_read<UInt32>(32) << i_count;
            }
            result |= bb.Value.bb_read<UInt64>(i_count);
            return result;
        }

        static bool _decode_interactive_composition(Ref<BITBUFFER> bb, Ref<BD_IG_INTERACTIVE_COMPOSITION> p)
        {
            uint ii;

            UInt32 data_len = bb.Value.bb_read<UInt32>(24);
            UInt32 buf_len = (UInt32)(bb.Value.p_end - bb.Value.p);
            if (data_len != buf_len)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"ig_decode_interactive(): buffer size mismatch (expected {data_len}, have {buf_len})");
                return false;
            }

            p.Value.stream_model = bb.Value.bb_read<byte>(1);
            p.Value.ui_model = bb.Value.bb_read<byte>(1);
            bb.Value.bb_skip(6);

            if (p.Value.stream_model == 0)
            {
                bb.Value.bb_skip(7);
                p.Value.composition_timeout_pts = bb_read_u64(bb, 33);
                bb.Value.bb_skip(7);
                p.Value.selection_timeout_pts = bb_read_u64(bb, 33);
            }

            p.Value.user_timeout_duration = bb.Value.bb_read<uint>(24);

            p.Value.num_pages = bb.Value.bb_read<byte>(8);
            p.Value.page = Ref<BD_IG_PAGE>.Allocate(p.Value.num_pages);

            for (ii = 0; ii < p.Value.num_pages; ii++)
            {
                if (!_decode_page(bb, p.Value.page.AtIndex(ii)))
                {
                    return false;
                }
            }

            return true;
        }

        static void _clean_interactive_composition(Ref<BD_IG_INTERACTIVE_COMPOSITION> p)
        {
            uint ii;

            if (p.Value.page)
            {
                for (ii = 0; ii < p.Value.num_pages; ii++)
                {
                    _clean_page(p.Value.page.AtIndex(ii));
                }
            }

            p.Value.page.Free();
        }

        /*
         * segment
         */

        internal static bool ig_decode_interactive(Ref<BITBUFFER> bb, Ref<BD_IG_INTERACTIVE> p)
        {
            Variable<BD_PG_SEQUENCE_DESCRIPTOR> sd = new();

            PgDecode.pg_decode_video_descriptor(bb, p.Value.video_descriptor.Ref);
            PgDecode.pg_decode_composition_descriptor(bb, p.Value.composition_descriptor.Ref);
            PgDecode.pg_decode_sequence_descriptor(bb, sd.Ref);

            if (sd.Value.first_in_seq == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"ig_decode_interactive(): not first in seq");
                return false;
            }
            if (sd.Value.last_in_seq == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"ig_decode_interactive(): not last in seq");
                return false;
            }
            if (!bb.Value.bb_is_align(0x07))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"ig_decode_interactive(): alignment error");
                return false;
            }

            return _decode_interactive_composition(bb, p.Value.interactive_composition.Ref);
        }

        internal static void ig_free_interactive(ref Ref<BD_IG_INTERACTIVE> p)
        {
            if (p)
            {
                _clean_interactive_composition(p.Value.interactive_composition.Ref);
                p.Free();
            }
        }
    }
}
