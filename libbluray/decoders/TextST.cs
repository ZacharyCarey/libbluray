using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{
    public enum BD_TEXTST_DATA_Type
    {
        BD_TEXTST_DATA_STRING = 1,
        BD_TEXTST_DATA_FONT_ID = 2,
        BD_TEXTST_DATA_FONT_STYLE = 3,
        BD_TEXTST_DATA_FONT_SIZE = 4,
        BD_TEXTST_DATA_FONT_COLOR = 5,
        BD_TEXTST_DATA_NEWLINE = 0x0a,
        BD_TEXTST_DATA_RESET_STYLE = 0x0b
    }

    public struct BD_TEXTST_RECT
    {
        public UInt16 xpos;
        public UInt16 ypos;
        public UInt16 width;
        public UInt16 height;

        public BD_TEXTST_RECT() { }
    }

    public struct BD_TEXTST_REGION_INFO
    {
        public Variable<BD_TEXTST_RECT> region = new();
        public byte background_color; /* palette entry id ref */

        public BD_TEXTST_REGION_INFO() { }
    }

    public struct BD_TEXTST_FONT_STYLE
    {
        public bool bold;
        public bool italic;
        public bool outline_border;

        public BD_TEXTST_FONT_STYLE() { }
    }

    public struct BD_TEXTST_REGION_STYLE
    {
        public byte region_style_id;
        public Variable<BD_TEXTST_REGION_INFO> region_info = new();
        public Variable<BD_TEXTST_RECT> text_box = new();          /* relative to region */
        public byte text_flow;         /* BD_TEXTST_FLOW_* */
        public byte text_halign;       /* BD_TEXTST_HALIGN_* */
        public byte text_valign;       /* BD_TEXTST_VALIGN_* */
        public byte line_space;
        public byte font_id_ref;
        public Variable<BD_TEXTST_FONT_STYLE> font_style = new();
        public byte font_size;
        public byte font_color;        /* palette entry id ref */
        public byte outline_color;     /* palette entry id ref */
        public byte outline_thickness; /* BD_TEXTST_FONT_OUTLINE_* */

        public BD_TEXTST_REGION_STYLE() { }
    }

    public struct BD_TEXTST_USER_STYLE
    {
        public byte user_style_id;
        public Int16 region_hpos_delta;
        public Int16 region_vpos_delta;
        public Int16 text_box_hpos_delta;
        public Int16 text_box_vpos_delta;
        public Int16 text_box_width_delta;
        public Int16 text_box_height_delta;
        public sbyte font_size_delta;
        public sbyte line_space_delta;

        public BD_TEXTST_USER_STYLE() { }
    }

    public struct BD_TEXTST_DATA
    {
        public BD_TEXTST_DATA_Type type;  // BD_TEXTST_DATA_

        public byte font_id_ref;
        public byte font_size;
        public byte font_color;

        // style
        public Variable<BD_TEXTST_FONT_STYLE> style = new();
        public byte outline_color;
        public byte outline_thickness;

        // text
        public byte length;
        public string _string;

        /*union {
            byte font_id_ref;
            byte font_size;
            byte font_color;
            struct {
                BD_TEXTST_FONT_STYLE style;
                byte outline_color;
                byte outline_thickness;
            }
            style;
            struct {
                byte length;
                byte string[1];
            }
            text;
        }
        data;*/

        public BD_TEXTST_DATA() { }
    }

    public struct BD_TEXTST_DIALOG_REGION
    {
        public byte continous_present_flag;
        public byte forced_on_flag;
        public byte region_style_id_ref;

        public uint elem_count;
        public Ref<BD_TEXTST_DATA> elem = new(); /* note: variable-sized elements */

        public uint line_count;

        public BD_TEXTST_DIALOG_REGION() { }
    }

    public struct BD_TEXTST_DIALOG_STYLE
    {
        public byte player_style_flag;
        public byte region_style_count;
        public byte user_style_count;
        public Ref<BD_TEXTST_REGION_STYLE> region_style = new();
        public Ref<BD_TEXTST_USER_STYLE> user_style = new();
        public BD_PG_PALETTE_ENTRY[] palette = new BD_PG_PALETTE_ENTRY[256];

        public BD_TEXTST_DIALOG_STYLE() {
            for (int i = 0; i < palette.Length; i++)
            {
                palette[i] = new();
            }
        }
    }

    public struct BD_TEXTST_DIALOG_PRESENTATION
    {
        public Int64 start_pts;
        public Int64 end_pts;

        public Ref<BD_PG_PALETTE_ENTRY> palette_update = new();

        public byte region_count;
        public BD_TEXTST_DIALOG_REGION[] region = new BD_TEXTST_DIALOG_REGION[2];

        public BD_TEXTST_DIALOG_PRESENTATION() { }
    }

    public static class TextST
    {
        // TODO enums
        public const int BD_TEXTST_FLOW_LEFT_RIGHT = 1;  /* Left-to-Right character progression, Top-to-Bottom line progression */
        public const int BD_TEXTST_FLOW_RIGHT_LEFT = 2;  /* Right-to-Left character progression, Top-to-Bottom line progression */
        public const int BD_TEXTST_FLOW_TOP_BOTTOM = 3;  /* Top-to-Bottom character progression, Right-to-Left line progression */

        public const int BD_TEXTST_HALIGN_LEFT = 1;
        public const int BD_TEXTST_HALIGN_CENTER = 2;
        public const int BD_TEXTST_HALIGN_RIGHT = 3;

        public const int BD_TEXTST_VALIGN_TOP = 1;
        public const int BD_TEXTST_VALIGN_MIDDLE = 2;
        public const int BD_TEXTST_VALIGN_BOTTOM = 3;

        public const int BD_TEXTST_FONT_OUTLINE_THIN = 1;
        public const int BD_TEXTST_FONT_OUTLINE_MEDIUM = 2;
        public const int BD_TEXTST_FONT_OUTLINE_THICK = 3;
    }
}
