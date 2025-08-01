using libbluray.disc;
using libbluray.file;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.bdnav
{
    public struct MPLS_STREAM
    {
        public byte stream_type;
        public byte coding_type;
        public ushort pid;
        public byte subpath_id;
        public byte subclip_id;
        public byte format;
        public byte rate;
        public byte dynamic_range_type;
        public byte color_space;
        public byte cr_flag;
        public byte hdr_plus_flag;
        public byte char_code;
        public string lang = "";
        // Secondary audio specific fields
        public byte sa_num_primary_audio_ref;
        public Ref<byte> sa_primary_audio_ref = new();
        // Secondary video specific fields
        public byte sv_num_secondary_audio_ref;
        public byte sv_num_pip_pg_ref;
        public Ref<byte> sv_secondary_audio_ref = new();
        public Ref<byte> sv_pip_pg_ref = new();

        public MPLS_STREAM() { }
    }

    public struct MPLS_STN
    {
        public byte num_video;
        public byte num_audio;
        public byte num_pg;
        public byte num_ig;
        public byte num_secondary_audio;
        public byte num_secondary_video;
        public byte num_pip_pg;
        public byte num_dv;
        public Ref<MPLS_STREAM> video = new();
        public Ref<MPLS_STREAM> audio = new();

        /// <summary>
        /// Presentation graphics
        /// </summary>
        public Ref<MPLS_STREAM> pg;

        /// <summary>
        /// Interactive graphics
        /// </summary>
        public Ref<MPLS_STREAM> ig = new();
        public Ref<MPLS_STREAM> secondary_audio = new();
        public Ref<MPLS_STREAM> secondary_video = new();
        public Ref<MPLS_STREAM> dv = new();

        public MPLS_STN() { }
    }

    public struct MPLS_CLIP
    {
        public string clip_id = "";
        public string codec_id = "";
        public byte stc_id;

        public MPLS_CLIP() { }
    }

    public struct MPLS_PI
    {
        public byte is_multi_angle;
        public byte connection_condition;
        public UInt32 in_time;
        public UInt32 out_time;
        public Variable<BD_UO_MASK> uo_mask = new();
        public byte random_access_flag;
        public byte still_mode;
        public UInt16 still_time;
        public byte angle_count;
        public byte is_different_audio;
        public byte is_seamless_angle;
        public Ref<MPLS_CLIP> clip = new();
        public Variable<MPLS_STN> stn = new();

        public MPLS_PI() { }
    }

    public struct MPLS_PLM
    {
        public byte mark_type;
        public UInt16 play_item_ref;
        public UInt32 time;
        public UInt16 entry_es_pid;
        public UInt32 duration;

        public MPLS_PLM() { }
    }

    public struct MPLS_AI
    {
        public byte playback_type;
        public UInt16 playback_count;
        public Variable<BD_UO_MASK> uo_mask = new();
        public byte random_access_flag;
        public byte audio_mix_flag;
        public byte lossless_bypass_flag;
        public byte mvc_base_view_r_flag;
        public byte sdr_conversion_notification_flag;

        public MPLS_AI() { }
    }

    public struct MPLS_SUB_PI
    {
        public byte connection_condition;
        public byte is_multi_clip;
        public UInt32 in_time;
        public UInt32 out_time;
        public UInt16 sync_play_item_id;
        public UInt32 sync_pts;
        public byte clip_count;
        public Ref<MPLS_CLIP> clip = new();

        public MPLS_SUB_PI() { }
    }

    public enum mpls_sub_path_type
    {
        //mpls_sub_path_        = 2,  /* Primary audio of the Browsable slideshow */
        mpls_sub_path_ig_menu = 3,  /* Interactive Graphics presentation menu */
        mpls_sub_path_textst = 4,  /* Text Subtitle */
        //mpls_sub_path_        = 5,  /* Out-of-mux Synchronous elementary streams */
        mpls_sub_path_async_pip = 6,  /* Out-of-mux Asynchronous Picture-in-Picture presentation */
        mpls_sub_path_sync_pip = 7,  /* In-mux Synchronous Picture-in-Picture presentation */
        mpls_sub_path_ss_video = 8,  /* SS Video */
        mpls_sub_path_dv_el = 10, /* Dolby Vision Enhancement Layer */
    }

    public struct MPLS_SUB
    {
        public byte type;       /* enum mpls_sub_path_type */
        public byte is_repeat;
        public byte sub_playitem_count;
        public Ref<MPLS_SUB_PI> sub_play_item = new();

        public MPLS_SUB() { }
    }

    public enum mpls_pip_scaling
    {
        pip_scaling_none = 1,       /* unscaled */
        pip_scaling_half = 2,       /* 1:2 */
        pip_scaling_quarter = 3,    /* 1:4 */
        pip_scaling_one_half = 4,   /* 3:2 */
        pip_scaling_fullscreen = 5, /* scale to main video size */
    }

    public struct MPLS_PIP_DATA
    {
        public UInt32 time;          /* start timestamp (clip time) when the block is valid */
        public UInt16 xpos;
        public UInt16 ypos;
        public byte scale_factor;  /* mpls_pip_scaling. Note: PSR14 may override this ! */

        public MPLS_PIP_DATA() { }
    }

    public enum mpls_pip_timeline
    {
        pip_timeline_sync_mainpath = 1,  /* timeline refers to main path */
        pip_timeline_async_subpath = 2,  /* timeline refers to sub-path time */
        pip_timeline_async_mainpath = 3, /* timeline refers to main path */
    }

    public struct MPLS_PIP_METADATA
    {
        public UInt16 clip_ref;             /* clip id for secondary_video_ref (STN) */
        public byte secondary_video_ref;  /* secondary video stream id (STN) */
        public byte timeline_type;        /* mpls_pip_timeline */
        public byte luma_key_flag;        /* use luma keying */
        public byte upper_limit_luma_key; /* luma key (secondary video pixels with Y <= this value are transparent) */
        public byte trick_play_flag;      /* show synchronous PiP when playing trick speed */

        public UInt16 data_count;
        public Ref<MPLS_PIP_DATA> data = new();

        public MPLS_PIP_METADATA() { }
    }

    // /* They are stored as GBR, we would like to show them as RGB */
    public enum mpls_static_primaries
    {
        primary_green,
        primary_blue,
        primary_red
    }

    public struct MPLS_STATIC_METADATA
    {
        public byte dynamic_range_type;
        public UInt16[] display_primaries_x = new ushort[3];
        public UInt16[] display_primaries_y = new ushort[3];
        public UInt16 white_point_x;
        public UInt16 white_point_y;
        public UInt16 max_display_mastering_luminance;
        public UInt16 min_display_mastering_luminance;
        public UInt16 max_CLL;
        public UInt16 max_FALL;

        public MPLS_STATIC_METADATA() { }
    }

    public struct MPLS_PL
    {
        public UInt32 type_indicator;   /* 'MPLS' */
        public Variable<UInt32> type_indicator2 = new();  /* version */
        public UInt32 list_pos;
        public UInt32 mark_pos;
        public UInt32 ext_pos;
        public Variable<MPLS_AI> app_info = new();
        public UInt16 list_count;
        public UInt16 sub_count;
        public UInt16 mark_count;
        public Ref<MPLS_PI> play_item = new();
        public Ref<MPLS_SUB> sub_path = new();
        public Ref<MPLS_PLM> play_mark = new();

        // extension data (profile 5, version 2.4)
        public UInt16 ext_sub_count;
        public Ref<MPLS_SUB> ext_sub_path = new();  // sub path entries extension

        // extension data (Picture-In-Picture metadata)
        public UInt16 ext_pip_data_count;
        public Ref<MPLS_PIP_METADATA> ext_pip_data = new();  // pip metadata extension

        // extension data (Static Metadata)
        public byte ext_static_metadata_count;
        public Ref<MPLS_STATIC_METADATA> ext_static_metadata = new();

        public MPLS_PL() { }
    }

    public static class MplsParse
    {
        internal const UInt32 MPLS_SIG1 = ('M' << 24) | ('P' << 16) | ('L' << 8) | 'S';
        // TODO make enum
        public const byte BD_MARK_ENTRY = 0x01;
        public const byte BD_MARK_LINK = 0x02;

        static bool
        _parse_uo(Ref<BITSTREAM> bits, Ref<BD_UO_MASK> uo)
        {
            byte[] buf = new byte[8];
            bits.Value.bs_read_bytes(buf, 8);
            return BD_UO_MASK.uo_mask_parse(new Ref<byte>(buf), uo);
        }

        static bool
        _parse_appinfo(Ref<BITSTREAM> bits, Ref<MPLS_AI> ai)
        {
            Int64 /*pos,*/ len;

            if (!bits.Value.bs_is_align(0x07))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_appinfo: alignment error");
            }
            //pos = bits.Value.bs_pos() >> 3;
            len = bits.Value.bs_read<UInt32>(32);

            if (bits.Value.bs_avail() < len * 8)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_appinfo: unexpected end of file");
                return false;
            }

            // Reserved
            bits.Value.bs_skip(8);
            ai.Value.playback_type = bits.Value.bs_read<byte>(8);
            if (ai.Value.playback_type == 2 || ai.Value.playback_type == 3)
            {
                ai.Value.playback_count = bits.Value.bs_read<UInt16>(16);
            }
            else
            {
                // Reserved
                bits.Value.bs_skip(16);
            }
            _parse_uo(bits, ai.Value.uo_mask.Ref);
            ai.Value.random_access_flag = bits.Value.bs_read<byte>(1);
            ai.Value.audio_mix_flag = bits.Value.bs_read<byte>(1);
            ai.Value.lossless_bypass_flag = bits.Value.bs_read<byte>(1);
            ai.Value.mvc_base_view_r_flag = bits.Value.bs_read<byte>(1);
            ai.Value.sdr_conversion_notification_flag = bits.Value.bs_read<byte>(1);
#if false
    // Reserved
    bits.Value.bs_skip(11);
    bits.Value.bs_seek_byte(pos + len);
#endif
            return true;
        }

        static bool
        _parse_header(Ref<BITSTREAM> bits, Ref<MPLS_PL> pl)
        {
            pl.Value.type_indicator = MPLS_SIG1;
            if (!BdmvParse.bdmv_parse_header(bits, pl.Value.type_indicator, pl.Value.type_indicator2.Ref))
            {
                return false;
            }

            if (bits.Value.bs_avail() < 5 * 32 + 160)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_header: unexpected end of file");
                return false;
            }

            pl.Value.list_pos = bits.Value.bs_read<UInt32>(32);
            pl.Value.mark_pos = bits.Value.bs_read<UInt32>(32);
            pl.Value.ext_pos = bits.Value.bs_read<UInt32>(32);

            // Skip 160 reserved bits
            bits.Value.bs_skip(160);

            _parse_appinfo(bits, pl.Value.app_info.Ref);
            return true;
        }

        static bool
        _parse_stream(Ref<BITSTREAM> bits, Ref<MPLS_STREAM> s)
        {
            int len;
            Int64 pos;

            if (!bits.Value.bs_is_align(0x07))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_stream: Stream alignment error");
            }
            len = bits.Value.bs_read<byte>(8);
            pos = bits.Value.bs_pos() >> 3;

            s.Value.stream_type = bits.Value.bs_read<byte>(8);
            switch (s.Value.stream_type)
            {
                case 1:
                    s.Value.pid = bits.Value.bs_read<UInt16>(16);
                    break;

                case 2:
                    s.Value.subpath_id = bits.Value.bs_read<byte>(8);
                    s.Value.subclip_id = bits.Value.bs_read<byte>(8);
                    s.Value.pid = bits.Value.bs_read<UInt16>(16);
                    break;

                case 3:
                case 4:
                    s.Value.subpath_id = bits.Value.bs_read<byte>(8);
                    s.Value.pid = bits.Value.bs_read<UInt16>(16);
                    break;

                default:
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"unrecognized stream type 0x{s.Value.stream_type:x2}");
                    break;
            };

            if (bits.Value.bs_seek_byte(pos + len) < 0)
            {
                return false;
            }

            len = bits.Value.bs_read<byte>(8);
            pos = bits.Value.bs_pos() >> 3;

            s.Value.lang = "";
            s.Value.coding_type = bits.Value.bs_read<byte>(8);
            switch (s.Value.coding_type)
            {
                case 0x01:
                case 0x02:
                case 0xea:
                case 0x1b:
                case 0x24:
                    s.Value.format = bits.Value.bs_read<byte>(4);
                    s.Value.rate = bits.Value.bs_read<byte>(4);
                    if (s.Value.coding_type == 0x24)
                    {
                        s.Value.dynamic_range_type = bits.Value.bs_read<byte>(4);
                        s.Value.color_space = bits.Value.bs_read<byte>(4);
                        s.Value.cr_flag = bits.Value.bs_read<byte>(1);
                        s.Value.hdr_plus_flag = bits.Value.bs_read<byte>(1);
                    }
                    break;

                case 0x03:
                case 0x04:
                case 0x80:
                case 0x81:
                case 0x82:
                case 0x83:
                case 0x84:
                case 0x85:
                case 0x86:
                case 0xa1:
                case 0xa2:
                    s.Value.format = bits.Value.bs_read<byte>(4);
                    s.Value.rate = bits.Value.bs_read<byte>(4);
                    s.Value.lang = bits.Value.bs_read_string(3);
                    break;

                case 0x90:
                case 0x91:
                    s.Value.lang = bits.Value.bs_read_string(3);
                    break;

                case 0x92:
                    s.Value.char_code = bits.Value.bs_read<byte>(8);
                    s.Value.lang = bits.Value.bs_read_string(3);
                    break;

                default:
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"unrecognized coding type 0x{s.Value.coding_type:x2}");
                    break;
            };

            if (bits.Value.bs_seek_byte(pos + len) < 0)
            {
                return false;
            }

            return true;
        }

        static bool
        _parse_stn(Ref<BITSTREAM> bits, Ref<MPLS_STN> stn)
        {
            int len;
            Int64 pos;
            Ref<MPLS_STREAM> ss;
            int ii, jj;

            if (!bits.Value.bs_is_align(0x07))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_stream: Stream alignment error");
            }
            // Skip STN len
            len = bits.Value.bs_read<UInt16>(16);
            pos = bits.Value.bs_pos() >> 3;

            // Skip 2 reserved bytes
            bits.Value.bs_skip(16);

            stn.Value.num_video = bits.Value.bs_read<byte>(8);
            stn.Value.num_audio = bits.Value.bs_read<byte>(8);
            stn.Value.num_pg = bits.Value.bs_read<byte>(8);
            stn.Value.num_ig = bits.Value.bs_read<byte>(8);
            stn.Value.num_secondary_audio = bits.Value.bs_read<byte>(8);
            stn.Value.num_secondary_video = bits.Value.bs_read<byte>(8);
            stn.Value.num_pip_pg = bits.Value.bs_read<byte>(8);
            stn.Value.num_dv = bits.Value.bs_read<byte>(8);

            // 4 reserve bytes
            bits.Value.bs_skip(4 * 8);

            // Primary Video Streams
            ss = Ref<MPLS_STREAM>.Null;
            if (stn.Value.num_video != 0)
            {
                ss = Ref<MPLS_STREAM>.Allocate(stn.Value.num_video);
                for (ii = 0; ii < stn.Value.num_video; ii++)
                {
                    if (!_parse_stream(bits, ss.AtIndex(ii)))
                    {
                        ss.Free();
                        Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing video entry");
                        return false;
                    }
                }
            }
            stn.Value.video = ss;

            // Primary Audio Streams
            ss = Ref<MPLS_STREAM>.Null;
            if (stn.Value.num_audio != 0)
            {
                ss = Ref<MPLS_STREAM>.Allocate(stn.Value.num_audio);
                for (ii = 0; ii < stn.Value.num_audio; ii++)
                {

                    if (!_parse_stream(bits, ss.AtIndex(ii)))
                    {
                        ss.Free();
                        Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing audio entry");
                        return false;
                    }
                }
            }
            stn.Value.audio = ss;

            // Presentation Graphic Streams
            ss = Ref<MPLS_STREAM>.Null;
            if (stn.Value.num_pg != 0 || stn.Value.num_pip_pg != 0)
            {
                ss = Ref<MPLS_STREAM>.Allocate(stn.Value.num_pg + stn.Value.num_pip_pg);
                for (ii = 0; ii < (stn.Value.num_pg + stn.Value.num_pip_pg); ii++)
                {
                    if (!_parse_stream(bits, ss.AtIndex(ii)))
                    {
                        ss.Free();
                        Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing pg/pip-pg entry");
                        return false;
                    }
                }
            }
            stn.Value.pg = ss;

            // Interactive Graphic Streams
            ss = Ref<MPLS_STREAM>.Null;
            if (stn.Value.num_ig != 0)
            {
                ss = Ref<MPLS_STREAM>.Allocate(stn.Value.num_ig);
                for (ii = 0; ii < stn.Value.num_ig; ii++)
                {
                    if (!_parse_stream(bits, ss.AtIndex(ii)))
                    {
                        ss.Free();
                        Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing ig entry");
                        return false;
                    }
                }
            }
            stn.Value.ig = ss;

            // Secondary Audio Streams
            if (stn.Value.num_secondary_audio != 0)
            {
                ss = Ref<MPLS_STREAM>.Allocate(stn.Value.num_secondary_audio);
                stn.Value.secondary_audio = ss;
                for (ii = 0; ii < stn.Value.num_secondary_audio; ii++)
                {
                    if (!_parse_stream(bits, ss.AtIndex(ii)))
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing secondary audio entry");
                        return false;
                    }
                    // Read Secondary Audio Extra Attributes
                    ss[ii].sa_num_primary_audio_ref = bits.Value.bs_read<byte>(8);
                    bits.Value.bs_skip(8);
                    if (ss[ii].sa_num_primary_audio_ref != 0)
                    {
                        ss[ii].sa_primary_audio_ref = Ref<byte>.Allocate(ss[ii].sa_num_primary_audio_ref);
                        if (!ss[ii].sa_primary_audio_ref)
                        {
                            return false;
                        }
                        for (jj = 0; jj < ss[ii].sa_num_primary_audio_ref; jj++)
                        {
                            ss[ii].sa_primary_audio_ref[jj] = bits.Value.bs_read<byte>(8);
                        }
                        if ((ss[ii].sa_num_primary_audio_ref % 2) != 0)
                        {
                            bits.Value.bs_skip(8);
                        }
                    }
                }
            }

            // Secondary Video Streams
            if (stn.Value.num_secondary_video != 0)
            {
                ss = Ref<MPLS_STREAM>.Allocate(stn.Value.num_secondary_video);
                stn.Value.secondary_video = ss;
                for (ii = 0; ii < stn.Value.num_secondary_video; ii++)
                {
                    if (!_parse_stream(bits, ss.AtIndex(ii)))
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing secondary video entry");
                        return false;
                    }
                    // Read Secondary Video Extra Attributes
                    ss[ii].sv_num_secondary_audio_ref = bits.Value.bs_read<byte>(8);
                    bits.Value.bs_skip(8);
                    if (ss[ii].sv_num_secondary_audio_ref != 0)
                    {
                        ss[ii].sv_secondary_audio_ref = Ref<byte>.Allocate(ss[ii].sv_num_secondary_audio_ref);
                        for (jj = 0; jj < ss[ii].sv_num_secondary_audio_ref; jj++)
                        {
                            ss[ii].sv_secondary_audio_ref[jj] = bits.Value.bs_read<byte>(8);
                        }
                        if ((ss[ii].sv_num_secondary_audio_ref % 2) != 0)
                        {
                            bits.Value.bs_skip(8);
                        }
                    }
                    ss[ii].sv_num_pip_pg_ref = bits.Value.bs_read<byte>(8);
                    bits.Value.bs_skip(8);
                    if (ss[ii].sv_num_pip_pg_ref != 0)
                    {
                        ss[ii].sv_pip_pg_ref = Ref<byte>.Allocate(ss[ii].sv_num_pip_pg_ref);
                        for (jj = 0; jj < ss[ii].sv_num_pip_pg_ref; jj++)
                        {
                            ss[ii].sv_pip_pg_ref[jj] = bits.Value.bs_read<byte>(8);
                        }
                        if ((ss[ii].sv_num_pip_pg_ref % 2) != 0)
                        {
                            bits.Value.bs_skip(8);
                        }
                    }

                }
            }

            // Dolby Vision Enhancement Layer Streams
            ss = Ref<MPLS_STREAM>.Null;
            if (stn.Value.num_dv != 0)
            {
                ss = Ref<MPLS_STREAM>.Allocate(stn.Value.num_dv);
                for (ii = 0; ii < stn.Value.num_dv; ii++)
                {
                    if (!_parse_stream(bits, ss.AtIndex(ii)))
                    {
                        ss.Free();
                        Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing dv entry");
                        return false;
                    }
                }
            }
            stn.Value.dv = ss;

            if (bits.Value.bs_seek_byte(pos + len) < 0)
            {
                return false;
            }

            return true;
        }

        static void
        _clean_stn(Ref<MPLS_STN> stn)
        {
            uint ii;

            if (stn.Value.secondary_audio)
            {
                for (ii = 0; ii < stn.Value.num_secondary_audio; ii++)
                {
                    stn.Value.secondary_audio[ii].sa_primary_audio_ref.Free();
                }
            }
            if (stn.Value.secondary_video)
            {
                for (ii = 0; ii < stn.Value.num_secondary_video; ii++)
                {
                    stn.Value.secondary_video[ii].sv_secondary_audio_ref.Free();
                    stn.Value.secondary_video[ii].sv_pip_pg_ref.Free();
                }
            }

            stn.Value.video.Free();
            stn.Value.audio.Free();
            stn.Value.pg.Free();
            stn.Value.ig.Free();
            stn.Value.secondary_audio.Free();
            stn.Value.secondary_video.Free();
        }

        static bool
        _parse_playitem(Ref<BITSTREAM> bits, Ref<MPLS_PI> pi)
        {
            int len, ii;
            Int64 pos;
            string clip_id, codec_id;
            byte stc_id;

            if (!bits.Value.bs_is_align(0x07))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_playitem: Stream alignment error");
            }

            // PlayItem Length
            len = bits.Value.bs_read<UInt16>(16);
            pos = bits.Value.bs_pos() >> 3;

            if (len < 18)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_playitem: invalid length {len}");
                return false;
            }
            if (bits.Value.bs_avail() / 8 < len)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_playitem: unexpected EOF");
                return false;
            }

            // Primary Clip identifer
            clip_id = bits.Value.bs_read_string(5);

            codec_id = bits.Value.bs_read_string(4);
            if ((codec_id != "M2TS") && (codec_id != "FMTS"))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"Incorrect CodecIdentifier ({codec_id})");
            }

            // Skip reserved 11 bits
            bits.Value.bs_skip(11);

            pi.Value.is_multi_angle = bits.Value.bs_read<byte>(1);

            pi.Value.connection_condition = bits.Value.bs_read<byte>(4);
            if (pi.Value.connection_condition != 0x01 &&
                pi.Value.connection_condition != 0x05 &&
                pi.Value.connection_condition != 0x06)
            {

                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"Unexpected connection condition {pi.Value.connection_condition:x2}");
            }

            stc_id = bits.Value.bs_read<byte>(8);
            pi.Value.in_time = bits.Value.bs_read<UInt32>(32);
            pi.Value.out_time = bits.Value.bs_read<UInt32>(32);

            _parse_uo(bits, pi.Value.uo_mask.Ref);
            pi.Value.random_access_flag = bits.Value.bs_read<byte>(1);
            bits.Value.bs_skip(7);
            pi.Value.still_mode = bits.Value.bs_read<byte>(8);
            if (pi.Value.still_mode == 0x01)
            {
                pi.Value.still_time = bits.Value.bs_read<UInt16>(16);
            }
            else
            {
                bits.Value.bs_skip(16);
            }

            pi.Value.angle_count = 1;
            if (pi.Value.is_multi_angle != 0)
            {
                pi.Value.angle_count = bits.Value.bs_read<byte>(8);
                if (pi.Value.angle_count < 1)
                {
                    pi.Value.angle_count = 1;
                }
                bits.Value.bs_skip(6);
                pi.Value.is_different_audio = bits.Value.bs_read<byte>(1);
                pi.Value.is_seamless_angle = bits.Value.bs_read<byte>(1);
            }
            pi.Value.clip = Ref<MPLS_CLIP>.Allocate(pi.Value.angle_count);
            if (!pi.Value.clip)
            {
                return false;
            }
            pi.Value.clip[0].clip_id = clip_id;
            pi.Value.clip[0].codec_id = codec_id;
            pi.Value.clip[0].stc_id = stc_id;
            for (ii = 1; ii < pi.Value.angle_count; ii++)
            {
                pi.Value.clip[ii].clip_id = bits.Value.bs_read_string(5);

                pi.Value.clip[ii].codec_id = bits.Value.bs_read_string(4);
                if ((pi.Value.clip[ii].codec_id != "M2TS") && (pi.Value.clip[ii].codec_id != "FMTS"))
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"Incorrect CodecIdentifier ({pi.Value.clip[ii].codec_id})");
                }
                pi.Value.clip[ii].stc_id = bits.Value.bs_read<byte>(8);
            }
            if (!_parse_stn(bits, pi.Value.stn.Ref))
            {
                return false;
            }

            // Seek past any unused items
            if (bits.Value.bs_seek_byte(pos + len) < 0)
            {
                return false;
            }

            return true;
        }

        static void
        _clean_playitem(Ref<MPLS_PI> pi)
        {
            pi.Value.clip.Free();
            _clean_stn(pi.Value.stn.Ref);
        }

        static bool
        _parse_subplayitem(Ref<BITSTREAM> bits, Ref<MPLS_SUB_PI> spi)
        {
            int len, ii;
            Int64 pos;
            string clip_id, codec_id;
            byte stc_id;

            if (!bits.Value.bs_is_align(0x07))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_subplayitem: alignment error");
            }

            // PlayItem Length
            len = bits.Value.bs_read<UInt16>(16);
            pos = bits.Value.bs_pos() >> 3;

            if (len < 24)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_subplayitem: invalid length {len}");
                return false;
            }

            if (bits.Value.bs_avail() / 8 < len)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_subplayitem: unexpected EOF");
                return false;
            }

            // Primary Clip identifer
            clip_id = bits.Value.bs_read_string(5);

            codec_id = bits.Value.bs_read_string(4);
            if ((codec_id != "M2TS") && (codec_id != "FMTS"))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"Incorrect CodecIdentifier ({codec_id})");
            }

            bits.Value.bs_skip(27);

            spi.Value.connection_condition = bits.Value.bs_read<byte>(4);

            if (spi.Value.connection_condition != 0x01 &&
                spi.Value.connection_condition != 0x05 &&
                spi.Value.connection_condition != 0x06)
            {

                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"Unexpected connection condition {spi.Value.connection_condition:x2}");
            }
            spi.Value.is_multi_clip = bits.Value.bs_read<byte>(1);
            stc_id = bits.Value.bs_read<byte>(8);
            spi.Value.in_time = bits.Value.bs_read<UInt32>(32);
            spi.Value.out_time = bits.Value.bs_read<UInt32>(32);
            spi.Value.sync_play_item_id = bits.Value.bs_read<UInt16>(16);
            spi.Value.sync_pts = bits.Value.bs_read<UInt32>(32);
            spi.Value.clip_count = 1;
            if (spi.Value.is_multi_clip != 0)
            {
                spi.Value.clip_count = bits.Value.bs_read<byte>(8);
                if (spi.Value.clip_count < 1)
                {
                    spi.Value.clip_count = 1;
                }
            }
            spi.Value.clip = Ref<MPLS_CLIP>.Allocate(spi.Value.clip_count);
            spi.Value.clip[0].clip_id = clip_id;
            spi.Value.clip[0].codec_id = codec_id;
            spi.Value.clip[0].stc_id = stc_id;
            for (ii = 1; ii < spi.Value.clip_count; ii++)
            {
                // Primary Clip identifer
                spi.Value.clip[ii].clip_id = bits.Value.bs_read_string(5);

                spi.Value.clip[ii].codec_id = bits.Value.bs_read_string(4);
                if ((spi.Value.clip[ii].codec_id != "M2TS") && (spi.Value.clip[ii].codec_id != "FMTS"))
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"Incorrect CodecIdentifier ({spi.Value.clip[ii].codec_id})");
                }
                spi.Value.clip[ii].stc_id = bits.Value.bs_read<byte>(8);
            }


            // Seek to end of subpath
            if (bits.Value.bs_seek_byte(pos + len) < 0)
            {
                return false;
            }

            return true;
        }

        static void
        _clean_subplayitem(Ref<MPLS_SUB_PI> spi)
        {
            spi.Value.clip.Free();
        }

        static bool
        _parse_subpath(Ref<BITSTREAM> bits, Ref<MPLS_SUB> sp)
        {
            int len, ii;
            Int64 pos;
            Ref<MPLS_SUB_PI> spi = Ref<MPLS_SUB_PI>.Null;

            if (!bits.Value.bs_is_align(0x07))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_subpath: alignment error");
            }

            // PlayItem Length
            len = bits.Value.bs_read<Int32>(32);
            pos = bits.Value.bs_pos() >> 3;

            bits.Value.bs_skip(8);
            sp.Value.type = bits.Value.bs_read<byte>(8);
            bits.Value.bs_skip(15);
            sp.Value.is_repeat = bits.Value.bs_read<byte>(1);
            bits.Value.bs_skip(8);
            sp.Value.sub_playitem_count = bits.Value.bs_read<byte>(8);

            if (sp.Value.sub_playitem_count != 0)
            {
                spi = Ref<MPLS_SUB_PI>.Allocate(sp.Value.sub_playitem_count);

                sp.Value.sub_play_item = spi;
                for (ii = 0; ii < sp.Value.sub_playitem_count; ii++)
                {
                    if (!_parse_subplayitem(bits, spi.AtIndex(ii)))
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing sub play item");
                        return false;
                    }
                }
            }

            // Seek to end of subpath
            if (bits.Value.bs_seek_byte(pos + len) < 0)
            {
                return false;
            }

            return true;
        }

        static void
        _clean_subpath(Ref<MPLS_SUB> sp)
        {
            int ii;

            for (ii = 0; ii < sp.Value.sub_playitem_count; ii++)
            {
                _clean_subplayitem(sp.Value.sub_play_item.AtIndex(ii));
            }
            sp.Value.sub_play_item.Free();
        }

        static bool
        _parse_playlistmark(Ref<BITSTREAM> bits, Ref<MPLS_PL> pl)
        {
            Int64 len;
            int ii;
            Ref<MPLS_PLM> plm = Ref<MPLS_PLM>.Null;

            if (bits.Value.bs_seek_byte(pl.Value.mark_pos) < 0)
            {
                return false;
            }

            // length field
            len = bits.Value.bs_read<UInt32>(32);

            if (bits.Value.bs_avail() / 8 < len)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_playlistmark: unexpected EOF");
                return false;
            }

            // Then get the number of marks
            pl.Value.mark_count = bits.Value.bs_read<UInt16>(16);

            if (bits.Value.bs_avail() / (8 * 14) < pl.Value.mark_count)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_playlistmark: unexpected EOF");
                return false;
            }

            plm = Ref<MPLS_PLM>.Allocate(pl.Value.mark_count);
            for (ii = 0; ii < pl.Value.mark_count; ii++)
            {
                bits.Value.bs_skip(8); /* reserved */
                plm[ii].mark_type = bits.Value.bs_read<byte>(8);
                plm[ii].play_item_ref = bits.Value.bs_read<UInt16>(16);
                plm[ii].time = bits.Value.bs_read<UInt32>(32);
                plm[ii].entry_es_pid = bits.Value.bs_read<UInt16>(16);
                plm[ii].duration = bits.Value.bs_read<UInt32>(32);
            }
            pl.Value.play_mark = plm;
            return true;
        }

        static bool
        _parse_playlist(Ref<BITSTREAM> bits, Ref<MPLS_PL> pl)
        {
            Int64 len;
            int ii;
            Ref<MPLS_PI> pi = Ref<MPLS_PI>.Null;
            Ref<MPLS_SUB> sub_path = Ref<MPLS_SUB>.Null;

            if (bits.Value.bs_seek_byte(pl.Value.list_pos) < 0)
            {
                return false;
            }

            // playlist length
            len = bits.Value.bs_read<UInt32>(32);

            if (bits.Value.bs_avail() < len * 8)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_playlist: unexpected end of file");
                return false;
            }

            // Skip reserved bytes
            bits.Value.bs_skip(16);

            pl.Value.list_count = bits.Value.bs_read<UInt16>(16);
            pl.Value.sub_count = bits.Value.bs_read<UInt16>(16);

            if (pl.Value.list_count != 0)
            {
                pi = Ref<MPLS_PI>.Allocate(pl.Value.list_count);

                pl.Value.play_item = pi;
                for (ii = 0; ii < pl.Value.list_count; ii++)
                {
                    if (!_parse_playitem(bits, pi.AtIndex(ii)))
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing play list item");
                        return false;
                    }
                }
            }

            if (pl.Value.sub_count != 0)
            {
                sub_path = Ref<MPLS_SUB>.Allocate(pl.Value.sub_count);

                pl.Value.sub_path = sub_path;
                for (ii = 0; ii < pl.Value.sub_count; ii++)
                {
                    if (!_parse_subpath(bits, sub_path.AtIndex(ii)))
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing subpath");
                        return false;
                    }
                }
            }

            return true;
        }

        static void _clean_pip_data(Ref<MPLS_PIP_METADATA> p)
        {
            p.Value.data.Free();
        }

        static void
        _clean_playlist(Ref<MPLS_PL> pl)
        {
            int ii;

            if (pl.Value.play_item != null)
            {
                for (ii = 0; ii < pl.Value.list_count; ii++)
                {
                    _clean_playitem(pl.Value.play_item.AtIndex(ii));
                }
                pl.Value.play_item.Free();
            }
            if (pl.Value.sub_path != null)
            {
                for (ii = 0; ii < pl.Value.sub_count; ii++)
                {
                    _clean_subpath(pl.Value.sub_path.AtIndex(ii));
                }
                pl.Value.sub_path.Free();
            }
            if (pl.Value.ext_sub_path != null)
            {
                for (ii = 0; ii < pl.Value.ext_sub_count; ii++)
                {
                    _clean_subpath(pl.Value.ext_sub_path.AtIndex(ii));
                }
                pl.Value.ext_sub_path.Free();
            }
            if (pl.Value.ext_pip_data != null)
            {
                for (ii = 0; ii < pl.Value.ext_pip_data_count; ii++)
                {
                    _clean_pip_data(pl.Value.ext_pip_data.AtIndex(ii));
                }
                pl.Value.ext_pip_data.Free();
            }

            pl.Value.ext_static_metadata.Free();
            pl.Value.play_mark.Free();
            pl.Free();
        }

        internal static void
        mpls_free(ref Ref<MPLS_PL> pl)
        {
            if (pl)
            {
                _clean_playlist(pl);
                pl = Ref<MPLS_PL>.Null;
            }
        }

        static bool
        _parse_pip_data(Ref<BITSTREAM> bits, Ref<MPLS_PIP_METADATA> block)
        {
            Ref<MPLS_PIP_DATA> data;
            uint ii;

            UInt16 entries = bits.Value.bs_read<UInt16>(16);
            if (entries < 1)
            {
                return true;
            }

            data = Ref<MPLS_PIP_DATA>.Allocate(entries);

            for (ii = 0; ii < entries; ii++)
            {

                data[ii].time = bits.Value.bs_read<UInt32>(32);
                data[ii].xpos = bits.Value.bs_read<UInt16>(12);
                data[ii].ypos = bits.Value.bs_read<UInt16>(12);
                data[ii].scale_factor = bits.Value.bs_read<byte>(4);
                bits.Value.bs_skip(4);
            }

            block.Value.data_count = entries;
            block.Value.data = data;

            return true;
        }

        static bool
        _parse_pip_metadata_block(Ref<BITSTREAM> bits, UInt32 start_address, Ref<MPLS_PIP_METADATA> data)
        {
            UInt32 data_address;
            bool result;
            Int64 pos;

            data.Value.clip_ref = bits.Value.bs_read<UInt16>(16);
            data.Value.secondary_video_ref = bits.Value.bs_read<byte>(8);
            bits.Value.bs_skip(8);
            data.Value.timeline_type = bits.Value.bs_read<byte>(4);
            data.Value.luma_key_flag = bits.Value.bs_read<byte>(1);
            data.Value.trick_play_flag = bits.Value.bs_read<byte>(1);
            bits.Value.bs_skip(10);
            if (data.Value.luma_key_flag != 0)
            {
                bits.Value.bs_skip(8);
                data.Value.upper_limit_luma_key = bits.Value.bs_read<byte>(8);
            }
            else
            {
                bits.Value.bs_skip(16);
            }
            bits.Value.bs_skip(16);

            data_address = bits.Value.bs_read<UInt32>(32);

            pos = bits.Value.bs_pos() / 8;
            if (bits.Value.bs_seek_byte(start_address + data_address) < 0)
            {
                return false;
            }
            result = _parse_pip_data(bits, data);
            if (bits.Value.bs_seek_byte(pos) < 0)
            {
                return false;
            }

            return result;
        }

        static bool
        _parse_pip_metadata_extension(Ref<BITSTREAM> bits, Ref<MPLS_PL> pl)
        {
            Ref<MPLS_PIP_METADATA> data;
            int ii;

            UInt32 start_address = (UInt32)bits.Value.bs_pos() / 8;
            UInt32 len = bits.Value.bs_read<UInt32>(32);
            int entries = bits.Value.bs_read<UInt16>(16);

            if (len < 1 || entries < 1)
            {
                return false;
            }

            data = Ref<MPLS_PIP_METADATA>.Allocate(entries);

            for (ii = 0; ii < entries; ii++)
            {
                if (!_parse_pip_metadata_block(bits, start_address, data.AtIndex(ii)))
                {
                    goto error;
                }
            }

            pl.Value.ext_pip_data_count = (ushort)entries;
            pl.Value.ext_pip_data = data;

            return true;

        error:
            Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing pip metadata extension");
            for (ii = 0; ii < entries; ii++)
            {
                _clean_pip_data(data.AtIndex(ii));
            }
            data.Free();
            return false;

        }

        static bool
        _parse_subpath_extension(Ref<BITSTREAM> bits, Ref<MPLS_PL> pl)
        {
            Ref<MPLS_SUB> sub_path;
            int ii;

            UInt32 len = bits.Value.bs_read<UInt32>(32);
            int sub_count = bits.Value.bs_read<UInt16>(16);

            if (len < 1 || sub_count < 1)
            {
                return false;
            }

            sub_path = Ref<MPLS_SUB>.Allocate(sub_count);

            for (ii = 0; ii < sub_count; ii++)
            {
                if (!_parse_subpath(bits, sub_path.AtIndex(ii)))
                {
                    goto error;
                }
            }
            pl.Value.ext_sub_path = sub_path;
            pl.Value.ext_sub_count = (ushort)sub_count;

            return true;

        error:
            Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing extension subpath");
            for (ii = 0; ii < sub_count; ii++)
            {
                _clean_subpath(sub_path.AtIndex(ii));
            }
            sub_path.Free();
            return false;
        }

        static bool
        _parse_static_metadata(Ref<BITSTREAM> bits, Ref<MPLS_STATIC_METADATA> data)
        {
            int ii;

            if (bits.Value.bs_avail() < 28 * 8)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_static_metadata: unexpected end of file");
                return false;
            }

            data.Value.dynamic_range_type = bits.Value.bs_read<byte>(4);
            bits.Value.bs_skip(4);
            bits.Value.bs_skip(24);
            for (ii = 0; ii < 3; ii++)
            {
                data.Value.display_primaries_x[ii] = bits.Value.bs_read<UInt16>(16);
                data.Value.display_primaries_y[ii] = bits.Value.bs_read<UInt16>(16);
            }
            data.Value.white_point_x = bits.Value.bs_read<UInt16>(16);
            data.Value.white_point_y = bits.Value.bs_read<UInt16>(16);
            data.Value.max_display_mastering_luminance = bits.Value.bs_read<UInt16>(16);
            data.Value.min_display_mastering_luminance = bits.Value.bs_read<UInt16>(16);
            data.Value.max_CLL = bits.Value.bs_read<UInt16>(16);
            data.Value.max_FALL = bits.Value.bs_read<UInt16>(16);

            return true;
        }

        static bool
        _parse_static_metadata_extension(Ref<BITSTREAM> bits, Ref<MPLS_PL> pl)
        {
            Ref<MPLS_STATIC_METADATA> static_metadata;
            UInt32 len;
            int ii;

            len = bits.Value.bs_read<UInt32>(32);
            if (len < 32)
            {     // At least one static metadata entry
                return false;
            }
            if (bits.Value.bs_avail() < len * 8)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_static_metadata_extension: unexpected end of file");
                return false;
            }

            byte sm_count = bits.Value.bs_read<byte>(8);
            if (sm_count < 1)
            {
                return false;
            }
            bits.Value.bs_skip(24);

            static_metadata = Ref<MPLS_STATIC_METADATA>.Allocate(sm_count);

            for (ii = 0; ii < sm_count; ii++)
            {
                if (!_parse_static_metadata(bits, static_metadata.AtIndex(ii)))
                {
                    goto error;
                }
            }
            pl.Value.ext_static_metadata = static_metadata;
            pl.Value.ext_static_metadata_count = sm_count;

            return true;

        error:
            Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing static metadata extension");
            static_metadata.Free();
            return false;
        }

        static bool
        _parse_mpls_extension(Ref<BITSTREAM> bits, int id1, int id2, Ref<MPLS_PL> pl)
        {
            if (id1 == 1)
            {
                if (id2 == 1)
                {
                    // PiP metadata extension
                    return _parse_pip_metadata_extension(bits, pl);
                }
            }

            if (id1 == 2)
            {
                if (id2 == 1)
                {
                    return false;
                }
                if (id2 == 2)
                {
                    // SubPath entries extension
                    return _parse_subpath_extension(bits, pl);
                }
            }

            if (id1 == 3)
            {
                if (id2 == 5)
                {
                    // Static metadata extension
                    return _parse_static_metadata_extension(bits, pl);
                }
            }

            Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_mpls_extension(): unhandled extension {id1}.{id2}");

            return false;
        }

        static Ref<MPLS_PL>
        _mpls_parse(BD_FILE_H fp)
        {
            Variable<BITSTREAM> bits = new();
            Ref<MPLS_PL> pl = Ref<MPLS_PL>.Null;

            if (bits.Value.bs_init(fp) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, "?????.mpls: read error");
                return Ref<MPLS_PL>.Null;
            }

            pl = Ref<MPLS_PL>.Allocate();

            if (!_parse_header(bits.Ref, pl))
            {
                _clean_playlist(pl);
                return Ref<MPLS_PL>.Null;
            }
            if (!_parse_playlist(bits.Ref, pl))
            {
                _clean_playlist(pl);
                return Ref<MPLS_PL>.Null;
            }
            if (!_parse_playlistmark(bits.Ref, pl))
            {
                _clean_playlist(pl);
                return Ref<MPLS_PL>.Null;
            }

            if (pl.Value.ext_pos > 0)
            {
                ExtDataParse.bdmv_parse_extension_data(bits.Ref,
                                          (int)pl.Value.ext_pos,
                                          _parse_mpls_extension,
                                          pl);
            }

            return pl;
        }

        internal static Ref<MPLS_PL>
        mpls_parse(string path)
        {
            Ref<MPLS_PL> pl;
            BD_FILE_H? fp;

            fp = BD_FILE_H.file_open(path, true);
            if (fp == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"Failed to open {path}");
                return Ref<MPLS_PL>.Null;
            }

            pl = _mpls_parse(fp);
            fp.file_close();
            return pl;
        }

        static Ref<MPLS_PL>
        _mpls_get(BD_DISC disc, string dir, string file)
        {
            Ref<MPLS_PL> pl;
            BD_FILE_H? fp;

            fp = disc.disc_open_file(dir, file);
            if (fp == null)
            {
                return Ref<MPLS_PL>.Null;
            }

            pl = _mpls_parse(fp);
            fp.file_close();
            return pl;
        }

        internal static Ref<MPLS_PL>
        mpls_get(BD_DISC disc, string file)
        {
            Ref<MPLS_PL> pl;

            pl = _mpls_get(disc, Path.Combine("BDMV", "PLAYLIST"), file);
            if (pl)
            {
                return pl;
            }

            /* if failed, try backup file */
            pl = _mpls_get(disc, Path.Combine("BDMV", "BACKUP", "PLAYLIST"), file);
            return pl;
        }

    }
}
