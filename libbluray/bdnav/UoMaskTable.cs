using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.bdnav
{
    internal enum UoMaskIndexType
    {
        UO_MASK_MENU_CALL_INDEX = 0,
        UO_MASK_TITLE_SEARCH_INDEX = 1
    }

    public struct BD_UO_MASK
    {
        public bool menu_call;
        public bool title_search;
        public bool chapter_search;
        public bool time_search;
        public bool skip_to_next_point;
        public bool skip_to_prev_point;
        public bool play_firstplay;
        public bool stop;
        public bool pause_on;
        public bool pause_off;
        public bool still_off;
        public bool forward;
        public bool backward;
        public bool resume;
        public bool move_up;
        public bool move_down;
        public bool move_left;
        public bool move_right;
        public bool select;
        public bool activate;
        public bool select_and_activate;
        public bool primary_audio_change;
        public bool reserved0;
        public bool angle_change;
        public bool popup_on;
        public bool popup_off;
        public bool pg_enable_disable;
        public bool pg_change;
        public bool secondary_video_enable_disable;
        public bool secondary_video_change;
        public bool secondary_audio_enable_disable;
        public bool secondary_audio_change;
        public bool reserved1;
        public bool pip_pg_change;

        public BD_UO_MASK() { }

        internal UInt64 AsInt {
            get => ((menu_call ? 1ul : 0ul) << 33)
                | ((title_search ? 1ul : 0ul) << 32)
                | ((chapter_search ? 1ul : 0ul) << 31)
                | ((time_search ? 1ul : 0ul) << 30)
                | ((skip_to_next_point ? 1ul : 0ul) << 29)
                | ((skip_to_prev_point ? 1ul : 0ul) << 28)
                | ((play_firstplay ? 1ul : 0ul) << 27)
                | ((stop ? 1ul : 0ul) << 26)
                | ((pause_on ? 1ul : 0ul) << 25)
                | ((pause_off ? 1ul : 0ul) << 24)
                | ((still_off ? 1ul : 0ul) << 23)
                | ((forward ? 1ul : 0ul) << 22)
                | ((backward ? 1ul : 0ul) << 21)
                | ((resume ? 1ul : 0ul) << 20)
                | ((move_up ? 1ul : 0ul) << 19)
                | ((move_down ? 1ul : 0ul) << 18)
                | ((move_left ? 1ul : 0ul) << 17)
                | ((move_right ? 1ul : 0ul) << 16)
                | ((select ? 1ul : 0ul) << 15)
                | ((activate ? 1ul : 0ul) << 14)
                | ((select_and_activate ? 1ul : 0ul) << 13)
                | ((primary_audio_change ? 1UL : 0UL) << 12)
                | ((reserved0 ? 1ul : 0ul) << 11)
                | ((angle_change ? 1ul : 0ul) << 10)
                | ((popup_on ? 1ul : 0ul) << 9)
                | ((popup_off ? 1ul : 0ul) << 8)
                | ((pg_enable_disable ? 1ul : 0ul) << 7)
                | ((pg_change ? 1ul : 0ul) << 6)
                | ((secondary_video_enable_disable ? 1ul : 0ul) << 5)
                | ((secondary_video_change ? 1ul : 0ul) << 4)
                | ((secondary_audio_enable_disable ? 1ul : 0ul) << 3)
                | ((secondary_audio_change ? 1ul : 0ul) << 2)
                | ((reserved1 ? 1ul : 0ul) << 1)
                | (pip_pg_change ? 1ul : 0ul);

            set
            {
                menu_call = (((value >> 33) & 0x01) != 0);
                title_search = (((value >> 32) & 0x01) != 0);
                chapter_search = (((value >> 31) & 0x01) != 0);
                time_search = (((value >> 30) & 0x01) != 0);
                skip_to_next_point = (((value >> 29) & 0x01) != 0);
                skip_to_prev_point = (((value >> 28) & 0x01) != 0);
                play_firstplay = (((value >> 27) & 0x01) != 0);
                stop = (((value >> 26) & 0x01) != 0);
                pause_on = (((value >> 25) & 0x01) != 0);
                pause_off = (((value >> 24) & 0x01) != 0);
                still_off = (((value >> 23) & 0x01) != 0);
                forward = (((value >> 22) & 0x01) != 0);
                backward = (((value >> 21) & 0x01) != 0);
                resume = (((value >> 20) & 0x01) != 0);
                move_up = (((value >> 19) & 0x01) != 0);
                move_down = (((value >> 18) & 0x01) != 0);
                move_left = (((value >> 17) & 0x01) != 0);
                move_right = (((value >> 16) & 0x01) != 0);
                select = (((value >> 15) & 0x01) != 0);
                activate = (((value >> 14) & 0x01) != 0);
                select_and_activate = (((value >> 13) & 0x01) != 0);
                primary_audio_change = (((value >> 12) & 0x01) != 0);
                reserved0 = (((value >> 11) & 0x01) != 0);
                angle_change = (((value >> 10) & 0x01) != 0);
                popup_on = (((value >> 9) & 0x01) != 0);
                popup_off = (((value >> 8) & 0x01) != 0);
                pg_enable_disable = (((value >> 7) & 0x01) != 0);
                pg_change = (((value >> 6) & 0x01) != 0);
                secondary_video_enable_disable = (((value >> 5) & 0x01) != 0);
                secondary_video_change = (((value >> 4) & 0x01) != 0);
                secondary_audio_enable_disable = (((value >> 3) & 0x01) != 0);
                secondary_audio_change = (((value >> 2) & 0x01) != 0);
                reserved1 = (((value >> 1) & 0x01) != 0);
                pip_pg_change = ((value & 0x01) != 0);
            }
        }

        public static BD_UO_MASK uo_mask_combine(BD_UO_MASK a, BD_UO_MASK b)
        {
            BD_UO_MASK result = new();
            result.AsInt = a.AsInt | b.AsInt;
            return result;
        }

        internal static bool uo_mask_parse(Ref<byte> buf, Ref<BD_UO_MASK> uo)
        {
            BITBUFFER bb = new();
            bb.bb_init(buf, 8);

            uo.Value.menu_call = bb.bb_readbool();
            uo.Value.title_search = bb.bb_readbool();
            uo.Value.chapter_search = bb.bb_readbool();
            uo.Value.time_search = bb.bb_readbool();
            uo.Value.skip_to_next_point = bb.bb_readbool();
            uo.Value.skip_to_prev_point = bb.bb_readbool();
            uo.Value.play_firstplay = bb.bb_readbool();
            uo.Value.stop = bb.bb_readbool();
            uo.Value.pause_on = bb.bb_readbool();
            uo.Value.pause_off = bb.bb_readbool();
            uo.Value.still_off = bb.bb_readbool();
            uo.Value.forward = bb.bb_readbool();
            uo.Value.backward = bb.bb_readbool();
            uo.Value.resume = bb.bb_readbool();
            uo.Value.move_up = bb.bb_readbool();
            uo.Value.move_down = bb.bb_readbool();
            uo.Value.move_left = bb.bb_readbool();
            uo.Value.move_right = bb.bb_readbool();
            uo.Value.select = bb.bb_readbool();
            uo.Value.activate = bb.bb_readbool();
            uo.Value.select_and_activate = bb.bb_readbool();
            uo.Value.primary_audio_change = bb.bb_readbool();
            bb.bb_skip(1);
            uo.Value.angle_change = bb.bb_readbool();
            uo.Value.popup_on = bb.bb_readbool();
            uo.Value.popup_off = bb.bb_readbool();
            uo.Value.pg_enable_disable = bb.bb_readbool();
            uo.Value.pg_change = bb.bb_readbool();
            uo.Value.secondary_video_enable_disable = bb.bb_readbool();
            uo.Value.secondary_video_change = bb.bb_readbool();
            uo.Value.secondary_audio_enable_disable = bb.bb_readbool();
            uo.Value.secondary_audio_change = bb.bb_readbool();
            bb.bb_skip(1);
            uo.Value.pip_pg_change = bb.bb_readbool();
            bb.bb_skip(30);
            return true;
        }
    }
}
