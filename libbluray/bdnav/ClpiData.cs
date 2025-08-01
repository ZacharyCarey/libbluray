using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.bdnav
{
    public struct CLPI_STC_SEQ
    {
        public UInt16 pcr_pid;
        public UInt32 spn_stc_start;
        public UInt32 presentation_start_time;
        public UInt32 presentation_end_time;

        public CLPI_STC_SEQ() { }
    }

    public struct CLPI_ATC_SEQ
    {
        public UInt32 spn_atc_start;
        public byte num_stc_seq;
        public byte offset_stc_id;
        public Ref<CLPI_STC_SEQ> stc_seq = new();

        public CLPI_ATC_SEQ() { }
    }

    public struct CLPI_SEQ_INFO
    {
        public byte num_atc_seq;
        public Ref<CLPI_ATC_SEQ> atc_seq = new();

        public CLPI_SEQ_INFO() { }
    }

    public struct CLPI_TS_TYPE
    {
        public byte validity;

        /// <summary>
        /// Length 4
        /// </summary>
        public string format_id = "";

        public CLPI_TS_TYPE() { }
    }

    public struct CLPI_ATC_DELTA
    {
        public UInt32 delta;

        /// <summary>
        /// Length 5
        /// </summary>
        public string file_id = "";

        /// <summary>
        /// Length 4
        /// </summary>
        public string file_code = "";

        public CLPI_ATC_DELTA() { }
    }

    public struct CLPI_FONT
    {
        /// <summary>
        /// Length 5
        /// </summary>
        public string file_id = "";

        public CLPI_FONT() { }
    }

    public struct CLPI_FONT_INFO
    {
        public byte font_count;
        public Ref<CLPI_FONT> font = new();

        public CLPI_FONT_INFO() { }
    }

    public struct CLPI_CLIP_INFO
    {
        public byte clip_stream_type;
        public byte application_type;
        public byte is_atc_delta;
        public UInt32 ts_recording_rate;
        public UInt32 num_source_packets;
        public CLPI_TS_TYPE ts_type_info = new();
        public byte atc_delta_count;
        public Ref<CLPI_ATC_DELTA> atc_delta = new();
        public Variable<CLPI_FONT_INFO> font_info = new();      /* Text subtitle stream font files */

        public CLPI_CLIP_INFO() { }
    }

    public struct CLPI_PROG_STREAM
    {
        public UInt16 pid;
        public byte coding_type;
        public byte format;
        public byte rate;
        public byte aspect;
        public byte oc_flag;
        public byte char_code;
        /// <summary>
        /// Length 3
        /// </summary>
        public string lang = "";
        public byte cr_flag;
        public byte dynamic_range_type;
        public byte color_space;
        public byte hdr_plus_flag;
        public byte[] isrc = new byte[12];     /* International Standard Recording Code (usually empty or all zeroes) */

        public CLPI_PROG_STREAM() { }
    }

    public struct CLPI_PROG
    {
        public UInt32 spn_program_sequence_start;
        public UInt16 program_map_pid;
        public byte num_streams;
        public byte num_groups;
        public Ref<CLPI_PROG_STREAM> streams = new();

        public CLPI_PROG() { }
    }

    public struct CLPI_PROG_INFO
    {
        public byte num_prog;
        public Ref<CLPI_PROG> progs = new();

        public CLPI_PROG_INFO() { }
    }

    public struct CLPI_EP_COARSE
    {
        public int ref_ep_fine_id;
        public int pts_ep;
        public UInt32 spn_ep;

        public CLPI_EP_COARSE() { }
    }

    public struct CLPI_EP_FINE
    {
        public byte is_angle_change_point;
        public byte i_end_position_offset;
        public int pts_ep;
        public int spn_ep;

        public CLPI_EP_FINE() { }
    }

    public struct CLPI_EP_MAP_ENTRY
    {
        public UInt16 pid;
        public byte ep_stream_type;
        public int num_ep_coarse;
        public int num_ep_fine;
        public UInt32 ep_map_stream_start_addr;
        public Ref<CLPI_EP_COARSE> coarse = new();
        public Ref<CLPI_EP_FINE> fine = new();

        public CLPI_EP_MAP_ENTRY() { }
    }

    public struct CLPI_CPI
    {
        public byte type;
        // ep_map
        public byte num_stream_pid;
        public Ref<CLPI_EP_MAP_ENTRY> entry = new();

        public CLPI_CPI() { }
    }

    public struct CLPI_EXTENT_START
    {
        public UInt32 num_point;
        public Ref<UInt32> point = new();

        public CLPI_EXTENT_START() { }
    }

    public struct CLPI_CL
    {
        public UInt32 type_indicator;
        public Variable<UInt32> type_indicator2 = new();
        public UInt32 sequence_info_start_addr;
        public UInt32 program_info_start_addr;
        public UInt32 cpi_start_addr;
        public UInt32 clip_mark_start_addr;
        public UInt32 ext_data_start_addr;
        public CLPI_CLIP_INFO clip = new();
        public CLPI_SEQ_INFO sequence = new();
        public Variable<CLPI_PROG_INFO> program = new();
        public Variable<CLPI_CPI> cpi = new();
        // skip clip mark & extension data

        // extensions for 3D

        public Variable<CLPI_EXTENT_START> extent_start = new(); /* extent start points (.ssif interleaving) */
        public Variable<CLPI_PROG_INFO> program_ss = new();
        public Variable<CLPI_CPI> cpi_ss = new();

        public CLPI_CL() { }
    }
}
