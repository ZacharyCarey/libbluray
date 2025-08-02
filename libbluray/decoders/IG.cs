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
    public struct BD_IG_BUTTON
    {
        public UInt16 id;

        public UInt16 numeric_select_value;
        public byte auto_action_flag;

        public UInt16 x_pos;
        public UInt16 y_pos;

        /* neighbor info */
        public UInt16 upper_button_id_ref;
        public UInt16 lower_button_id_ref;
        public UInt16 left_button_id_ref;
        public UInt16 right_button_id_ref;

        /* normal state */
        public UInt16 normal_start_object_id_ref;
        public UInt16 normal_end_object_id_ref;
        public byte normal_repeat_flag;

        /* selected state */
        public byte selected_sound_id_ref;
        public UInt16 selected_start_object_id_ref;
        public UInt16 selected_end_object_id_ref;
        public byte selected_repeat_flag;

        /* activated state */
        public byte activated_sound_id_ref;
        public UInt16 activated_start_object_id_ref;
        public UInt16 activated_end_object_id_ref;

        /* navigation commands */
        public UInt16 num_nav_cmds;
        public Ref<MOBJ_CMD> nav_cmds = new();

        public BD_IG_BUTTON() { }
    }

    public struct BD_IG_BOG
    {
        public UInt16 default_valid_button_id_ref;

        public uint num_buttons;
        public Ref<BD_IG_BUTTON> button = new();

        public BD_IG_BOG() { }
    }

    public struct BD_IG_EFFECT
    {
        public UInt32 duration;        /* 90kHz ticks */
        public byte palette_id_ref;

        public uint num_composition_objects;
        public Ref<BD_PG_COMPOSITION_OBJECT> composition_object = new();

        public BD_IG_EFFECT() { }
    }

    public struct BD_IG_EFFECT_SEQUENCE
    {
        public byte num_windows;
        public Ref<BD_PG_WINDOW> window = new();

        public byte num_effects;
        public Ref<BD_IG_EFFECT> effect = new();

        public BD_IG_EFFECT_SEQUENCE() { }
    }

    public struct BD_IG_PAGE
    {
        public byte id;
        public byte version;

        public Variable<BD_UO_MASK> uo_mask_table = new();

        public Variable<BD_IG_EFFECT_SEQUENCE> in_effects = new();
        public Variable<BD_IG_EFFECT_SEQUENCE> out_effects = new();

        public byte animation_frame_rate_code;
        public UInt16 default_selected_button_id_ref;
        public UInt16 default_activated_button_id_ref;
        public byte palette_id_ref;

        /* button overlap groups */
        public uint num_bogs;
        public Ref<BD_IG_BOG> bog = new();

        public BD_IG_PAGE() { }
    }

    public struct BD_IG_INTERACTIVE_COMPOSITION
    {
        public byte stream_model;

        /// <summary>
        /// 0 - always on, 1 - pop-up
        /// </summary>
        public byte ui_model;      

        public UInt64 composition_timeout_pts;
        public UInt64 selection_timeout_pts;
        public UInt32 user_timeout_duration;

        public uint num_pages;
        public Ref<BD_IG_PAGE> page = new();

        public BD_IG_INTERACTIVE_COMPOSITION() { }
    }

    public struct BD_IG_INTERACTIVE
    {
        public Int64 pts;

        public Variable<BD_PG_VIDEO_DESCRIPTOR> video_descriptor = new();
        public Variable<BD_PG_COMPOSITION_DESCRIPTOR> composition_descriptor = new();
        public Variable<BD_IG_INTERACTIVE_COMPOSITION> interactive_composition = new();

        public BD_IG_INTERACTIVE() { }
    }
}
