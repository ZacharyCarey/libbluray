using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{
    public struct BD_PG_VIDEO_DESCRIPTOR
    {
        public UInt16 video_width;
        public UInt16 video_height;
        public byte frame_rate;
    }

    public struct BD_PG_COMPOSITION_DESCRIPTOR
    {
        public UInt16 number;
        public byte state;
    }

    public struct BD_PG_SEQUENCE_DESCRIPTOR
    {
        public byte first_in_seq;
        public byte last_in_seq;
    }

    public struct BD_PG_WINDOW
    {
        public byte id;
        public UInt16 x;
        public UInt16 y;
        public UInt16 width;
        public UInt16 height;
    }

    public struct BD_PG_COMPOSITION_OBJECT
    {
        public UInt16 object_id_ref;
        public byte window_id_ref;
        public byte forced_on_flag;

        public UInt16 x;
        public UInt16 y;

        public byte crop_flag;
        public UInt16 crop_x;
        public UInt16 crop_y;
        public UInt16 crop_w;
        public UInt16 crop_h;
    }

    public struct BD_PG_PALETTE
    {
        public Int64 pts;

        public byte id;
        public byte version;

        public BD_PG_PALETTE_ENTRY[] entry = new BD_PG_PALETTE_ENTRY[256];

        public BD_PG_PALETTE() { }
    }

    public struct BD_PG_OBJECT
    {
        public Int64 pts;

        public UInt16 id;
        public byte version;

        public UInt16 width;
        public UInt16 height;

        public Ref<BD_PG_RLE_ELEM> img;
    }

    public struct BD_PG_COMPOSITION
    {
        public Int64 pts;

        public Variable<BD_PG_VIDEO_DESCRIPTOR> video_descriptor;
        public Variable<BD_PG_COMPOSITION_DESCRIPTOR> composition_descriptor;

        public byte palette_update_flag;
        public byte palette_id_ref;

        public uint num_composition_objects;
        public Ref<BD_PG_COMPOSITION_OBJECT> composition_object;
    }

    public struct BD_PG_WINDOWS
    {
        public Int64 pts;

        public uint num_windows;
        public Ref<BD_PG_WINDOW> window;
    }
}
