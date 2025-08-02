using libbluray.disc;
using libbluray.file;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.bdnav
{
    internal enum indx_video_format
    {
        indx_video_format_ignored,
        indx_video_480i,
        indx_video_576i,
        indx_video_480p,
        indx_video_1080i,
        indx_video_720p,
        indx_video_1080p,
        indx_video_576p,
    }

    internal enum indx_frame_rate
    {
        indx_fps_reserved1,
        indx_fps_23_976,
        indx_fps_24,
        indx_fps_25,
        indx_fps_29_97,
        indx_fps_reserved2,
        indx_fps_50,
        indx_fps_59_94,
    }

    internal enum indx_object_type
    {
        indx_object_type_hdmv = 1,
        indx_object_type_bdj = 2,
    }

    internal enum indx_hdmv_playback_type
    {
        indx_hdmv_playback_type_movie = 0,
        indx_hdmv_playback_type_interactive = 1,
    }

    internal enum indx_bdj_playback_type
    {
        indx_bdj_playback_type_movie = 2,
        indx_bdj_playback_type_interactive = 3,
    }

    internal enum indx_access_type
    {
        /// <summary>
        /// jump into this title is permitted.  title number may be shown on UI.
        /// </summary>
        indx_access_permitted = 0,

        /// <summary>
        /// jump into this title is prohibited. title number may be shown on UI.
        /// </summary>
        indx_access_prohibited = 1,

        /// <summary>
        /// jump into this title is prohibited. title number shall not be shown on UI.
        /// </summary>
        indx_access_hidden = 3,  
    }

    internal struct INDX_APP_INFO
    {
        /// <summary>
        /// 0 = 2D, 1 = 3D
        /// </summary>
        public uint initial_output_mode_preference;

        public uint content_exist_flag;
        public uint initial_dynamic_range_type;
        public uint video_format;
        public uint frame_rate;

        public string user_data = "";

        public INDX_APP_INFO() { }
    }

    internal struct INDX_BDJ_OBJ
    {
        public indx_bdj_playback_type playback_type;

        /// <summary>
        /// 6 bytes
        /// </summary>
        public string name;

        public INDX_BDJ_OBJ() { }
    }

    internal struct INDX_HDMV_OBJ
    {
        public indx_hdmv_playback_type playback_type;
        public ushort id_ref;

        public INDX_HDMV_OBJ() { }
    }

    internal struct INDX_PLAY_ITEM
    {
        public indx_object_type object_type;
        public Variable<INDX_BDJ_OBJ> bdj = new();
        public Variable<INDX_HDMV_OBJ> hdmv = new();

        public INDX_PLAY_ITEM() { }
    }

    internal struct INDX_TITLE
    {
        public indx_object_type object_type;
        public byte access_type;
        public Variable<INDX_BDJ_OBJ> bdj = new();
        public Variable<INDX_HDMV_OBJ> hdmv = new();

        public INDX_TITLE() { }
    }

    internal struct INDX_ROOT
    {
        public Variable<INDX_APP_INFO> app_info = new();
        public Variable<INDX_PLAY_ITEM> first_play = new();
        public Variable<INDX_PLAY_ITEM> top_menu = new();

        public ushort num_titles;
        public Ref<INDX_TITLE> titles = new();

        public Variable<UInt32> indx_version = new();

        // UHD extension
        public byte disc_type;
        public byte exist_4k_flag;
        public byte hdrplus_flag;
        public byte dv_flag;
        public byte hdr_flags;

        public INDX_ROOT() { }
    }

    internal static class IndexParse
    {
        /// <summary>
        /// if set, jump to this title is not allowed
        /// </summary>
        public const int INDX_ACCESS_PROHIBITED_MASK = 0x01;

        /// <summary>
        /// if set, title number shall not be displayed on UI
        /// </summary>
        public const int INDX_ACCESS_HIDDEN_MASK = 0x02; 

        private const UInt32 INDX_SIG1 = ('I' << 24) | ('N' << 16) | ('D' << 8) | 'X';

        static bool _parse_hdmv_obj(Ref<BITSTREAM> bs, Ref<INDX_HDMV_OBJ> hdmv)
        {
            hdmv.Value.playback_type = (indx_hdmv_playback_type)bs.Value.bs_read<int>(2);
            bs.Value.bs_skip(14);
            hdmv.Value.id_ref = bs.Value.bs_read<ushort>(16);
            bs.Value.bs_skip(32);

            if (hdmv.Value.playback_type != indx_hdmv_playback_type.indx_hdmv_playback_type_movie &&
                hdmv.Value.playback_type != indx_hdmv_playback_type.indx_hdmv_playback_type_interactive)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"index.bdmv: invalid HDMV playback type {hdmv.Value.playback_type}");
            }

            return true;
        }

        static bool _parse_bdj_obj(Ref<BITSTREAM> bs, Ref<INDX_BDJ_OBJ> bdj)
        {
            bdj.Value.playback_type = (indx_bdj_playback_type)bs.Value.bs_read<int>(2);
            bs.Value.bs_skip(14);
            bdj.Value.name = bs.Value.bs_read_string(5);
            bs.Value.bs_skip(8);

            if (bdj.Value.playback_type != indx_bdj_playback_type.indx_bdj_playback_type_movie &&
                bdj.Value.playback_type != indx_bdj_playback_type.indx_bdj_playback_type_interactive)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"index.bdmv: invalid BD-J playback type {bdj.Value.playback_type}");
            }

            return true;
        }

        static bool _parse_playback_obj(Ref<BITSTREAM> bs, Ref<INDX_PLAY_ITEM> obj)
        {
            obj.Value.object_type = (indx_object_type)bs.Value.bs_read<int>(2);
            bs.Value.bs_skip(30);

            switch (obj.Value.object_type)
            {
                case indx_object_type.indx_object_type_hdmv:
                    return _parse_hdmv_obj(bs, obj.Value.hdmv.Ref);

                case indx_object_type.indx_object_type_bdj:
                    return _parse_bdj_obj(bs, obj.Value.bdj.Ref);
            }

            Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"index.bdmv: unknown object type {obj.Value.object_type}");
            return false;
        }

        static bool _parse_index(Ref<BITSTREAM> bs, Ref<INDX_ROOT> index)
        {
            UInt32 index_len, i;

            index_len = bs.Value.bs_read<UInt32>(32);

            /* TODO: check if goes to extension data area */

            if ((bs.Value.bs_end() - bs.Value.bs_pos()) / 8 < (Int64)index_len)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"index.bdmv: invalid index_len {index_len} !");
                return false;
            }

            if (!_parse_playback_obj(bs, index.Value.first_play.Ref) ||
                !_parse_playback_obj(bs, index.Value.top_menu.Ref))
            {
                return false;
            }

            index.Value.num_titles = bs.Value.bs_read<UInt16>(16);
            if (index.Value.num_titles == 0)
            {
                /* no "normal" titles - check for first play and top menu */
                if ((index.Value.first_play.Value.object_type == indx_object_type.indx_object_type_hdmv && index.Value.first_play.Value.hdmv.Value.id_ref == 0xffff) &&
                (index.Value.top_menu.Value.object_type == indx_object_type.indx_object_type_hdmv && index.Value.top_menu.Value.hdmv.Value.id_ref == 0xffff))
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_CRIT, "empty index");
                    return false;
                }
                return true;
            }

            index.Value.titles = Ref<INDX_TITLE>.Allocate(index.Value.num_titles);
            if (bs.Value.bs_avail() / (12 * 8) < index.Value.num_titles)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"index.bdmv: unexpected EOF");
                return false;
            }

            for (i = 0; i < index.Value.num_titles; i++)
            {

                index.Value.titles[i].object_type = (indx_object_type)bs.Value.bs_read<int>(2);
                index.Value.titles[i].access_type = bs.Value.bs_read<byte>(2);
                bs.Value.bs_skip(28);

                switch (index.Value.titles[i].object_type)
                {
                    case indx_object_type.indx_object_type_hdmv:
                        if (!_parse_hdmv_obj(bs, index.Value.titles[i].hdmv.Ref))
                            return false;
                        break;

                    case indx_object_type.indx_object_type_bdj:
                        if (!_parse_bdj_obj(bs, index.Value.titles[i].bdj.Ref))
                            return false;
                        break;

                    default:
                        Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"index.bdmv: unknown object type {index.Value.titles[i].object_type} (#{i})");
                        return false;
                }
            }

            return true;
        }

        static bool _parse_app_info(Ref<BITSTREAM> bs, Ref<INDX_APP_INFO> app_info)
        {
            UInt32 len;

            if (bs.Value.bs_seek_byte(40) < 0)
            {
                return false;
            }

            len = bs.Value.bs_read<UInt32>(32);

            if (len != 34)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, $"index.bdmv app_info length is {len}, expected 34 !");
            }

            bs.Value.bs_skip(1);
            app_info.Value.initial_output_mode_preference = bs.Value.bs_read<uint>(1);
            app_info.Value.content_exist_flag = bs.Value.bs_read<uint>(1);
            bs.Value.bs_skip(1);
            app_info.Value.initial_dynamic_range_type = bs.Value.bs_read<uint>(4);
            app_info.Value.video_format = bs.Value.bs_read<uint>(4);
            app_info.Value.frame_rate = bs.Value.bs_read<uint>(4);

            app_info.Value.user_data = bs.Value.bs_read_string(32);

            return true;
        }

        static bool _parse_header(Ref<BITSTREAM> bs, Ref<UInt32> index_start, Ref<UInt32> extension_data_start, Ref<UInt32> indx_version)
        {
            if (!BdmvParse.bdmv_parse_header(bs, INDX_SIG1, indx_version))
            {
                return false;
            }

            index_start.Value = bs.Value.bs_read<UInt32>(32);
            extension_data_start.Value = bs.Value.bs_read<UInt32>(32);

            return true;
        }

        static bool _parse_indx_extension_hevc(Ref<BITSTREAM> bs, Ref<INDX_ROOT> index)
        {
            UInt32 len;
            uint unk0, unk1, unk2, unk3, unk4, unk5;

            len = bs.Value.bs_read<UInt32>(32);
            if (len < 8)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"index.bdmv: unsupported extension 3.1 length ({len})");
                return false;
            }

            index.Value.disc_type     = bs.Value.bs_read<byte>(4);
            unk0                      = bs.Value.bs_read<uint>(3);
            index.Value.exist_4k_flag = bs.Value.bs_read<byte>(1);
            unk1                      = bs.Value.bs_read<uint>(8);
            unk2                      = bs.Value.bs_read<uint>(3);
            index.Value.hdrplus_flag  = bs.Value.bs_read<byte>(1);
            unk3                      = bs.Value.bs_read<uint>(1);
            index.Value.dv_flag       = bs.Value.bs_read<byte>(1);
            index.Value.hdr_flags     = bs.Value.bs_read<byte>(2);
            unk4                      = bs.Value.bs_read<uint>(8);
            unk5                      = bs.Value.bs_read<uint>(32);

            Logging.bd_debug(DebugMaskEnum.DBG_NAV, $"UHD disc type: {index.Value.disc_type}, 4k: {index.Value.exist_4k_flag}, HDR: {index.Value.hdr_flags}, HDR10+: {index.Value.hdrplus_flag}, Dolby Vision: {index.Value.dv_flag}");

            if ((unk0 | unk1 | unk2 | unk3 | unk4 | unk5) != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_CRIT | DebugMaskEnum.DBG_NAV, $"index.bdmv: unknown data in extension 3.1: 0x{unk0:x1} 0x{unk1:x2} 0x{unk2:x1} 0x{unk3:x1} 0x{unk4:x2} 0x{unk5:x8}");
            }

            return true;
        }

        static bool _parse_indx_extension(Ref<BITSTREAM> bits, int id1, int id2, Ref<INDX_ROOT> index)
        {
            if (id1 == 3)
            {
                if (id2 == 1)
                {
                    return _parse_indx_extension_hevc(bits, index);
                }
            }

            Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_indx_extension(): unknown extension {id1}.{id2}");

            return false;
        }

        static Ref<INDX_ROOT> _indx_parse(BD_FILE_H fp)
        {
            Variable<BITSTREAM> bs = new();
            Ref<INDX_ROOT> index = Ref<INDX_ROOT>.Null;
            Variable<UInt32> indexes_start = new(), extension_data_start = new();

            if (bs.Value.bs_init(fp) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, "index.bdmv: read error");
                return Ref<INDX_ROOT>.Null;
            }

            index = Ref<INDX_ROOT>.Allocate();
            if (!_parse_header(bs.Ref, indexes_start.Ref, extension_data_start.Ref, index.Value.indx_version.Ref) ||
                !_parse_app_info(bs.Ref, index.Value.app_info.Ref))
            {
                IndexParse.indx_free(ref index);
                return Ref<INDX_ROOT>.Null;
            }

            if (bs.Value.bs_seek_byte(indexes_start.Value) < 0)
            {
                IndexParse.indx_free(ref index);
                return Ref<INDX_ROOT>.Null;
            }

            if (!_parse_index(bs.Ref, index))
            {
                IndexParse.indx_free(ref index);
                return Ref<INDX_ROOT>.Null;
            }

            if (extension_data_start.Value != 0)
            {
                ExtDataParse.bdmv_parse_extension_data(bs.Ref,
                                          (int)extension_data_start.Value,
                                          _parse_indx_extension,
                                          index);
            }

            return index;
        }

        static Ref<INDX_ROOT> _indx_get(BD_DISC disc, string path)
        {
            BD_FILE_H fp;
            Ref<INDX_ROOT> index;

            fp = disc.disc_open_path(path);
            if (fp == null)
            {
                return Ref<INDX_ROOT>.Null;
            }

            index = _indx_parse(fp);
            fp.close();
            return index;
        }

        // parse index.bdmv
        internal static Ref<INDX_ROOT> indx_get(BD_DISC disc)
        {
            Ref<INDX_ROOT> index;
            index = _indx_get(disc, Path.Combine("BDMV", "index.bdmv"));

            if (!index)
            {
                // try backup
                index = _indx_get(disc, Path.Combine("BDMV", "BACKUP", "index.bdmv"));
            }

            return index;
        }

        internal static void indx_free(ref Ref<INDX_ROOT> p)
        {
            if (p)
            {
                p.Value.titles.Free();
                p.Free();
            }
        }
    }
}
