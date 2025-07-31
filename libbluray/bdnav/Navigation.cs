using libbluray.bdnav;
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
    public struct NAV_TITLE
    {
        public BD_DISC? disc;
        public string name;
        public byte angle_count;
        public byte angle;
        public NAV_CLIP_LIST clip_list;
        public NAV_MARK_LIST chap_list;
        public NAV_MARK_LIST mark_list;

        public uint sub_path_count;
        public Ref<NAV_SUB_PATH> sub_path;

        public UInt32 packets;
        public UInt32 duration;

        public Ref<MPLS_PL> pl;
    }

    public struct NAV_MARK
    {
        public int number;
        public int mark_type;
        public uint clip_ref;
        public UInt32 clip_pkt;
        public UInt32 clip_time;

        // Title relative metrics
        public UInt32 title_pkt;
        public UInt32 title_time;
        public UInt32 duration;
    }

    public struct NAV_MARK_LIST
    {
        public uint count;
        public Ref<NAV_MARK> mark;
    }

    public struct NAV_CLIP
    {
        public string name;
        public UInt32 clip_id;
        public uint _ref;
        public UInt32 start_pkt;
        public UInt32 end_pkt;
        public byte connection;
        public byte angle;

        public UInt32 duration;

        public UInt32 in_time;
        public UInt32 out_time;

        // Title relative metrics
        public UInt32 title_pkt;
        public UInt32 title_time;

        public Ref<NAV_TITLE> title;

        public UInt32 stc_spn;  /* start packet of clip STC sequence */

        public byte still_mode;
        public ushort still_time;

        public Ref<CLPI_CL> cl;
    }

    public struct NAV_CLIP_LIST
    {
        public uint count;
        public Ref<NAV_CLIP> clip;
    }

    public struct NAV_SUB_PATH
    {
        public byte type;
        public NAV_CLIP_LIST clip_list;
    }

    public struct NAV_TITLE_INFO
    {
        public string name;
        public UInt32 mpls_id;
        public UInt32 duration;
        public uint _ref;
    }

    public struct NAV_TITLE_LIST
    {
        public uint count;
        public Ref<NAV_TITLE_INFO> title_info;

        public uint main_title_idx;
    }

    public static class Navigation
    {
        // TODO create enum
        public const int CONNECT_NON_SEAMLESS = 0;
        public const int CONNECT_SEAMLESS = 1;

        // TODO create enum
        public const int TITLES_ALL = 0;
        public const int TITLES_FILTER_DUP_TITLE = 0x01;
        public const int TITLES_FILTER_DUP_CLIP = 0x02;
        public const int TITLES_RELEVANT = (TITLES_FILTER_DUP_TITLE | TITLES_FILTER_DUP_CLIP);

        static UInt32
_pl_duration(Ref<MPLS_PL> pl)
        {
            uint ii;
            UInt32 duration = 0;
            Ref<MPLS_PI> pi;

            for (ii = 0; ii < pl.Value.list_count; ii++)
            {
                pi = pl.Value.play_item.AtIndex(ii);
                duration += pi.Value.out_time - pi.Value.in_time;
            }
            return duration;
        }

        static UInt32
        _pl_chapter_count(Ref<MPLS_PL> pl)
        {
            uint ii, chapters = 0;

            // Count the number of "entry" marks (skipping "link" marks)
            // This is the the number of chapters
            for (ii = 0; ii < pl.Value.mark_count; ii++)
            {
                if (pl.Value.play_mark[ii].mark_type == MplsParse.BD_MARK_ENTRY)
                {
                    chapters++;
                }
            }
            return chapters;
        }

        static UInt32
        _pl_streams_score(Ref<MPLS_PL> pl)
        {
            Ref<MPLS_PI> pi;
            UInt32 i_num_audio = 0;
            UInt32 i_num_pg = 0;

            for (int ii = 0; ii < pl.Value.list_count; ii++)
            {
                pi = pl.Value.play_item.AtIndex(ii);
                if (pi.Value.stn.Value.num_audio > i_num_audio)
                    i_num_audio = pi.Value.stn.Value.num_audio;

                if (pi.Value.stn.Value.num_pg > i_num_pg)
                    i_num_pg = pi.Value.stn.Value.num_pg;
            }

            return i_num_audio * 2 + i_num_pg;
        }

        /*
         * Check if two playlists are the same
         */

        static bool _stream_cmp(Ref<MPLS_STREAM> a, Ref<MPLS_STREAM> b)
        {
            if (a.Value.stream_type == b.Value.stream_type &&
                a.Value.coding_type == b.Value.coding_type &&
                a.Value.pid == b.Value.pid &&
                a.Value.subpath_id == b.Value.subpath_id &&
                a.Value.subclip_id == b.Value.subclip_id &&
                a.Value.format == b.Value.format &&
                a.Value.rate == b.Value.rate &&
                a.Value.char_code == b.Value.char_code &&
                a.Value.color_space == b.Value.color_space &&
                (a.Value.lang == b.Value.lang))
            {
                return false;
            }
            return true;
        }

        static bool _streams_cmp(Ref<MPLS_STREAM> s1, Ref<MPLS_STREAM> s2, uint count)
        {
            uint ii;
            for (ii = 0; ii < count; ii++)
            {
                if (_stream_cmp(s1.AtIndex(ii), s2.AtIndex(ii)))
                {
                    return true;
                }
            }
            return false;
        }

        static bool _pi_cmp(Ref<MPLS_PI> pi1, Ref<MPLS_PI> pi2)
        {
            if ((pi1.Value.clip[0].clip_id != pi2.Value.clip[0].clip_id) ||
                pi1.Value.in_time != pi2.Value.in_time ||
                pi1.Value.out_time != pi2.Value.out_time)
            {
                return true;
            }

            if (pi1.Value.stn.Value.num_video != pi2.Value.stn.Value.num_video ||
                pi1.Value.stn.Value.num_audio != pi2.Value.stn.Value.num_audio ||
                pi1.Value.stn.Value.num_pg != pi2.Value.stn.Value.num_pg ||
                pi1.Value.stn.Value.num_ig != pi2.Value.stn.Value.num_ig ||
                pi1.Value.stn.Value.num_secondary_audio != pi2.Value.stn.Value.num_secondary_audio ||
                pi1.Value.stn.Value.num_secondary_video != pi2.Value.stn.Value.num_secondary_video)
            {
                return true;
            }

            if (_streams_cmp(pi1.Value.stn.Value.video, pi2.Value.stn.Value.video, pi1.Value.stn.Value.num_video) ||
                _streams_cmp(pi1.Value.stn.Value.audio, pi2.Value.stn.Value.audio, pi1.Value.stn.Value.num_audio) ||
                _streams_cmp(pi1.Value.stn.Value.pg, pi2.Value.stn.Value.pg, pi1.Value.stn.Value.num_pg) ||
                _streams_cmp(pi1.Value.stn.Value.ig, pi2.Value.stn.Value.ig, pi1.Value.stn.Value.num_ig) ||
                _streams_cmp(pi1.Value.stn.Value.secondary_audio, pi2.Value.stn.Value.secondary_audio, pi1.Value.stn.Value.num_secondary_audio) ||
                _streams_cmp(pi1.Value.stn.Value.secondary_video, pi2.Value.stn.Value.secondary_video, pi1.Value.stn.Value.num_secondary_video))
            {
                return true;
            }

            return false;
        }

        static bool _pm_cmp(Ref<MPLS_PLM> pm1, Ref<MPLS_PLM> pm2)
        {
            if (pm1.Value.mark_type == pm2.Value.mark_type &&
                pm1.Value.play_item_ref == pm2.Value.play_item_ref &&
                pm1.Value.time == pm2.Value.time &&
                pm1.Value.entry_es_pid == pm2.Value.entry_es_pid &&
                pm1.Value.duration == pm2.Value.duration)
            {
                return false;
            }

            return true;
        }

        static bool _pl_cmp(Ref<MPLS_PL> pl1, Ref<MPLS_PL> pl2)
        {
            uint ii;

            if (pl1.Value.list_count != pl2.Value.list_count)
            {
                return true;
            }
            if (pl1.Value.mark_count != pl2.Value.mark_count)
            {
                return true;
            }
            if (pl1.Value.sub_count != pl2.Value.sub_count)
            {
                return true;
            }
            if (pl1.Value.ext_sub_count != pl2.Value.ext_sub_count)
            {
                return true;
            }

            for (ii = 0; ii < pl1.Value.mark_count; ii++)
            {
                if (_pm_cmp(pl1.Value.play_mark.AtIndex(ii), pl2.Value.play_mark.AtIndex(ii)))
                {
                    return true;
                }
            }
            for (ii = 0; ii < pl1.Value.list_count; ii++)
            {
                if (_pi_cmp(pl1.Value.play_item.AtIndex(ii), pl2.Value.play_item.AtIndex(ii)))
                {
                    return true;
                }
            }

            return false;
        }

        /*
         * Playlist filtering
         */

        /* return 0 if duplicate playlist */
        static bool _filter_dup(Ref<Ref<MPLS_PL>> pl_list, uint count, Ref<MPLS_PL> pl)
        {
            uint ii;

            for (ii = 0; ii < count; ii++)
            {
                if (!_pl_cmp(pl, pl_list[ii]))
                {
                    return false;
                }
            }
            return true;
        }

        static uint
        _find_repeats(Ref<MPLS_PL> pl, string m2ts, UInt32 in_time, UInt32 out_time)
        {
            uint ii, count = 0;

            for (ii = 0; ii < pl.Value.list_count; ii++)
            {
                Ref<MPLS_PI> pi;

                pi = pl.Value.play_item.AtIndex(ii);
                // Ignore titles with repeated segments
                if ((pi.Value.clip[0].clip_id == m2ts) &&
                    pi.Value.in_time == in_time &&
                    pi.Value.out_time == out_time)
                {
                    count++;
                }
            }
            return count;
        }

        static bool
        _filter_repeats(Ref<MPLS_PL> pl, uint repeats)
        {
            uint ii;

            for (ii = 0; ii < pl.Value.list_count; ii++)
            {
                Ref<MPLS_PI> pi;

                pi = pl.Value.play_item.AtIndex(ii);
                // Ignore titles with repeated segments
                if (_find_repeats(pl, pi.Value.clip[0].clip_id, pi.Value.in_time, pi.Value.out_time) > repeats)
                {
                    return false;
                }
            }
            return true;
        }

        /*
         * find main movie playlist
         */

        private const DebugMaskEnum DBG_MAIN_PL = DebugMaskEnum.DBG_NAV;

        static void _video_props(Ref<MPLS_STN> s, Ref<int> format, Ref<int> codec)
        {
            uint ii;
            codec.Value = 0;
            format.Value = 0;
            for (ii = 0; ii < s.Value.num_video; ii++)
            {
                if (s.Value.video[ii].coding_type > 4)
                {
                    if (codec.Value < 1)
                    {
                        codec.Value = 1;
                    }
                }
                if (s.Value.video[ii].coding_type == BdParse.BD_STREAM_TYPE_VIDEO_HEVC)
                {
                    codec.Value = 2;
                }
                if (s.Value.video[ii].format == BdParse.BD_VIDEO_FORMAT_1080I || s.Value.video[ii].format == BdParse.BD_VIDEO_FORMAT_1080P)
                {
                    if (format.Value < 1)
                    {
                        format.Value = 1;
                    }
                }
                if (s.Value.video[ii].format == BdParse.BD_VIDEO_FORMAT_2160P)
                {
                    format.Value = 2;
                }
            }
        }

        static void _audio_props(Ref<MPLS_STN> s, Ref<int> hd_audio)
        {
            uint ii;
            hd_audio.Value = 0;
            for (ii = 0; ii < s.Value.num_audio; ii++)
            {
                if (s.Value.audio[ii].format == BdParse.BD_STREAM_TYPE_AUDIO_LPCM || s.Value.audio[ii].format >= BdParse.BD_STREAM_TYPE_AUDIO_TRUHD)
                {
                    hd_audio.Value = 1;
                }
            }
        }

        static int _cmp_video_props(Ref<MPLS_PL> p1, Ref<MPLS_PL> p2)
        {
            Ref<MPLS_STN> s1 = p1.Value.play_item[0].stn.Ref;
            Ref<MPLS_STN> s2 = p2.Value.play_item[0].stn.Ref;
            Variable<int> format1, format2, codec1, codec2;
            format1 = format2 = codec1 = codec2 = new();

            _video_props(s1, format1.Ref, codec1.Ref);
            _video_props(s2, format2.Ref, codec2.Ref);

            /* prefer UHD over FHD over HD/SD */
            if (format1.Value != format2.Value)
                return format2.Value - format1.Value;

            /* prefer H.265 over H.264/VC1 over MPEG1/2 */
            return codec2.Value - codec1.Value;
        }

        static int _cmp_audio_props(Ref<MPLS_PL> p1, Ref<MPLS_PL> p2)
        {
            Ref<MPLS_STN> s1 = p1.Value.play_item[0].stn.Ref;
            Ref<MPLS_STN> s2 = p2.Value.play_item[0].stn.Ref;
            Variable<int> hda1, hda2;
            hda1 = hda2 = new();

            _audio_props(s1, hda1.Ref);
            _audio_props(s2, hda2.Ref);

            /* prefer HD audio formats */
            return hda2.Value - hda1.Value;
        }

        static int _pl_guess_main_title(Ref<MPLS_PL> p1, Ref<MPLS_PL> p2,
                                        string mpls_id1, string mpls_id2,
                                        string known_mpls_ids)
        {
            UInt32 d1 = _pl_duration(p1);
            UInt32 d2 = _pl_duration(p2);

            /* if both longer than 30 min */
            if (d1 > 30 * 60 * 45000 && d2 > 30 * 60 * 45000)
            {

                /* prefer many chapters over no chapters */
                int chap1 = (int)_pl_chapter_count(p1);
                int chap2 = (int)_pl_chapter_count(p2);
                int chap_diff = chap2 - chap1;
                if ((chap1 < 2 || chap2 < 2) && (chap_diff < -5 || chap_diff > 5))
                {
                    /* chapter count differs by more than 5 */
                    Logging.bd_debug(DBG_MAIN_PL, $"main title ({mpls_id1},{mpls_id2}): chapter count difference {chap_diff}");
                    return chap_diff;
                }

                /* Check video: prefer HD over SD, H.264/VC1 over MPEG1/2 */
                int vid_diff = _cmp_video_props(p1, p2);
                if (vid_diff != 0)
                {
                    Logging.bd_debug(DBG_MAIN_PL, $"main title ({mpls_id1},{mpls_id2}): video properties difference {vid_diff}");
                    return vid_diff;
                }

                /* compare audio: prefer HD audio */
                int aud_diff = _cmp_audio_props(p1, p2);
                if (aud_diff != 0)
                {
                    Logging.bd_debug(DBG_MAIN_PL, $"main title ({mpls_id1},{mpls_id2}): audio properties difference {aud_diff}");
                    return aud_diff;
                }

                /* prefer "known good" playlists */
                if (known_mpls_ids != null)
                {
                    int known1 = (known_mpls_ids.IndexOf(mpls_id1) >= 0) ? 1 : 0;
                    int known2 = (known_mpls_ids.IndexOf(mpls_id2) >= 0) ? 1 : 0;
                    int known_diff = known2 - known1;
                    if (known_diff != 0)
                    {
                        Logging.bd_debug(DBG_MAIN_PL, $"main title ({mpls_id1},{mpls_id2}): prefer \"known\" playlist {(known_diff < 0 ? mpls_id1 : mpls_id2)}");
                        return known_diff;
                    }
                }
            }

            /* compare playlist duration, select longer playlist */
            if (d1 < d2)
            {
                return 1;
            }
            if (d1 > d2)
            {
                return -1;
            }

            /* prefer playlist with higher number of tracks */
            int sc1 = (int)_pl_streams_score(p1);
            int sc2 = (int)_pl_streams_score(p2);
            return sc2 - sc1;
        }

        /*
         * title list
         */

        public static Ref<NAV_TITLE_LIST> nav_get_title_list(BD_DISC? disc, UInt32 flags, UInt32 min_title_length)
        {
            BD_DIR_H? dir;
            BD_DIRENT ent = new();
            Ref<Ref<MPLS_PL>> pl_list = Ref<Ref<MPLS_PL>>.Null;
            Ref<MPLS_PL> pl = Ref<MPLS_PL>.Null;
            uint ii, pl_list_size = 0;
            int res;
            Ref<NAV_TITLE_LIST> title_list = Ref<NAV_TITLE_LIST>.Null;
            uint title_info_alloc = 100;
            string known_mpls_ids;

            dir = disc.disc_open_dir(Path.Combine("BDMV", "PLAYLIST"));
            if (dir == null)
            {
                return Ref<NAV_TITLE_LIST>.Null;
            }

            title_list = Ref<NAV_TITLE_LIST>.Allocate();
            title_list.Value.title_info = Ref<NAV_TITLE_INFO>.Allocate(title_info_alloc);
            known_mpls_ids = disc.disc_property_get(Disc.DISC_PROPERTY_MAIN_FEATURE);
            if (known_mpls_ids == null)
            {
                known_mpls_ids = disc.disc_property_get(Disc.DISC_PROPERTY_PLAYLISTS);
            }

            ii = 0;
            for (res = dir.dir_read(out ent); res == 0; res = dir.dir_read(out ent))
            {

                if (ent.d_name[0] == '.')
                {
                    continue;
                }
                if (ii >= pl_list_size)
                {
                    Ref<Ref<MPLS_PL>> tmp = Ref<Ref<MPLS_PL>>.Null;

                    pl_list_size += 100;
                    tmp = pl_list.Reallocate(pl_list_size);
                    if (tmp == null)
                    {
                        break;
                    }
                    pl_list = tmp;
                }
                pl = MplsParse.mpls_get(disc, ent.d_name);
                if (pl != null)
                {
                    if ((flags & TITLES_FILTER_DUP_TITLE) != 0 &&
                        !_filter_dup(pl_list, ii, pl))
                    {
                        MplsParse.mpls_free(ref pl);
                        continue;
                    }
                    if ((flags & TITLES_FILTER_DUP_CLIP) != 0 && !_filter_repeats(pl, 2))
                    {
                        MplsParse.mpls_free(ref pl);
                        continue;
                    }
                    if (min_title_length > 0 &&
                        _pl_duration(pl) < min_title_length * 45000)
                    {
                        MplsParse.mpls_free(ref pl);
                        continue;
                    }
                    if (ii >= title_info_alloc)
                    {
                        Ref<NAV_TITLE_INFO> tmp = Ref<NAV_TITLE_INFO>.Null;
                        title_info_alloc += 100;

                        tmp = title_list.Value.title_info.Reallocate(title_info_alloc);
                        if (tmp == null)
                        {
                            break;
                        }
                        title_list.Value.title_info = tmp;
                    }
                    pl_list[ii] = pl;

                    /* main title guessing */
                    if (_filter_dup(pl_list, ii, pl) &&
                        _filter_repeats(pl, 2))
                    {

                        if (_pl_guess_main_title(pl_list[ii], pl_list[title_list.Value.main_title_idx],
                                                 ent.d_name,
                                                 title_list.Value.title_info[title_list.Value.main_title_idx].name,
                                                 known_mpls_ids) <= 0)
                        {
                            title_list.Value.main_title_idx = ii;
                        }
                    }

                    title_list.Value.title_info[ii].name = ent.d_name;
                    title_list.Value.title_info[ii]._ref = ii;
                    title_list.Value.title_info[ii].mpls_id = uint.Parse(ent.d_name);
                    title_list.Value.title_info[ii].duration = _pl_duration(pl_list[ii]);
                    ii++;
                }
            }
            dir.dir_close();

            title_list.Value.count = ii;
            for (ii = 0; ii < title_list.Value.count; ii++)
            {
                MplsParse.mpls_free(ref pl_list[ii]);
            }
            known_mpls_ids = null;
            pl_list.Free();
            return title_list;
        }

        public static void nav_free_title_list(ref Ref<NAV_TITLE_LIST> title_list)
        {
            if (title_list)
            {
                title_list.Value.title_info.Free();
                title_list.Free();
            }
        }

        /*
         *
         */

        public static byte nav_clip_lookup_aspect(Ref<NAV_CLIP> clip, int pid)
        {
            Ref<CLPI_PROG> progs;
            int ii, jj;

            if (clip.Value.cl == null)
            {
                return 0;
            }

            progs = clip.Value.cl.Value.program.Value.progs;
            for (ii = 0; ii < clip.Value.cl.Value.program.Value.num_prog; ii++)
            {
                Ref<CLPI_PROG_STREAM> ps = progs[ii].streams;
                for (jj = 0; jj < progs[ii].num_streams; jj++)
                {
                    if (ps[jj].pid == pid)
                    {
                        return ps[jj].aspect;
                    }
                }
            }
            return 0;
        }

        static void
        _fill_mark(Ref<NAV_TITLE> title, Ref<NAV_MARK> mark, int entry)
        {
            Ref<MPLS_PL> pl = title.Value.pl;
            Ref<MPLS_PLM> plm;
            Ref<MPLS_PI> pi;
            Ref<NAV_CLIP> clip;

            plm = pl.Value.play_mark.AtIndex(entry);

            mark.Value.mark_type = plm.Value.mark_type;
            mark.Value.clip_ref = plm.Value.play_item_ref;
            clip = title.Value.clip_list.clip.AtIndex(mark.Value.clip_ref);
            if (clip.Value.cl != null && mark.Value.clip_ref < title.Value.pl.Value.list_count)
            {
                mark.Value.clip_pkt = ClpiParse.clpi_lookup_spn(clip.Value.cl, plm.Value.time, 1,
                    title.Value.pl.Value.play_item[mark.Value.clip_ref].clip[title.Value.angle].stc_id);
            }
            else
            {
                mark.Value.clip_pkt = clip.Value.start_pkt;
            }
            mark.Value.title_pkt = clip.Value.title_pkt + mark.Value.clip_pkt - clip.Value.start_pkt;
            mark.Value.clip_time = plm.Value.time;

            // Calculate start of mark relative to beginning of playlist
            if (plm.Value.play_item_ref < title.Value.clip_list.count)
            {
                clip = title.Value.clip_list.clip.AtIndex(plm.Value.play_item_ref);
                pi = pl.Value.play_item.AtIndex(plm.Value.play_item_ref);
                mark.Value.title_time = clip.Value.title_time + plm.Value.time - pi.Value.in_time;
            }
        }

        static void
        _extrapolate_title(Ref<NAV_TITLE> title)
        {
            UInt32 duration = 0;
            UInt32 pkt = 0;
            uint ii, jj;
            Ref<MPLS_PL> pl = title.Value.pl;
            Ref<MPLS_PI> pi;
            Ref<MPLS_PLM> plm;
            Ref<NAV_MARK> mark = Ref<NAV_MARK>.Null, prev = Ref<NAV_MARK>.Null;
            Ref<NAV_CLIP> clip;

            for (ii = 0; ii < title.Value.clip_list.count; ii++)
            {
                clip = title.Value.clip_list.clip.AtIndex(ii);
                pi = pl.Value.play_item.AtIndex(ii);
                if (pi.Value.angle_count > title.Value.angle_count)
                {
                    title.Value.angle_count = pi.Value.angle_count;
                }

                clip.Value.title_time = duration;
                clip.Value.duration = pi.Value.out_time - pi.Value.in_time;
                clip.Value.title_pkt = pkt;
                duration += clip.Value.duration;
                pkt += clip.Value.end_pkt - clip.Value.start_pkt;
            }
            title.Value.duration = duration;
            title.Value.packets = pkt;

            for (ii = 0, jj = 0; ii < pl.Value.mark_count; ii++)
            {
                plm = pl.Value.play_mark.AtIndex(ii);
                if (plm.Value.mark_type == MplsParse.BD_MARK_ENTRY)
                {

                    mark = title.Value.chap_list.mark.AtIndex(jj);
                    _fill_mark(title, mark, (int)ii);
                    mark.Value.number = (int)jj;

                    // Calculate duration of "entry" marks (chapters)
                    if (plm.Value.duration != 0)
                    {
                        mark.Value.duration = plm.Value.duration;
                    }
                    else if (prev != null)
                    {
                        if (prev.Value.duration == 0)
                        {
                            prev.Value.duration = mark.Value.title_time - prev.Value.title_time;
                        }
                    }
                    prev = mark;
                    jj++;
                }
                mark = title.Value.mark_list.mark.AtIndex(ii);
                _fill_mark(title, mark, (int)ii);
                mark.Value.number = (int)ii;
            }
            title.Value.chap_list.count = jj;
            if (prev != null && prev.Value.duration == 0)
            {
                prev.Value.duration = title.Value.duration - prev.Value.title_time;
            }
        }

        static void _fill_clip(Ref<NAV_TITLE> title,
                               Ref<MPLS_CLIP> mpls_clip,
                               byte connection_condition, UInt32 in_time, UInt32 out_time,
                               uint pi_angle_count, uint still_mode, uint still_time,
                               Ref<NAV_CLIP> clip,
                               uint _ref, Ref<UInt32> pos, Ref<UInt32> time)

        {
            string file;

            clip.Value.title = title;
            clip.Value._ref = _ref;
            clip.Value.still_mode = (byte)still_mode;
            clip.Value.still_time = (byte)still_time;

            if (title.Value.angle >= pi_angle_count)
            {
                clip.Value.angle = 0;
            }
            else
            {
                clip.Value.angle = title.Value.angle;
            }

            clip.Value.name = mpls_clip[clip.Value.angle].clip_id[0..5];
            if (mpls_clip[clip.Value.angle].codec_id == "FMTS")
                clip.Value.name += ".fmts";
            else
                clip.Value.name += ".m2ts";
            clip.Value.clip_id = uint.Parse(mpls_clip[clip.Value.angle].clip_id);

            ClpiParse.clpi_unref(ref clip.Value.cl);

            file = $"{mpls_clip[clip.Value.angle].clip_id}.clpi";
            clip.Value.cl = ClpiParse.clpi_get(title.Value.disc, file);
            file = null;

            if (clip.Value.cl == null)
            {
                clip.Value.start_pkt = 0;
                clip.Value.end_pkt = 0;
                return;
            }

            switch (connection_condition)
            {
                case 5:
                case 6:
                    clip.Value.start_pkt = 0;
                    clip.Value.connection = CONNECT_SEAMLESS;
                    break;
                default:
                    if (_ref != 0)
                    {
                        clip.Value.start_pkt = ClpiParse.clpi_lookup_spn(clip.Value.cl, in_time, 1,
                                                      mpls_clip[clip.Value.angle].stc_id);
                    }
                    else
                    {
                        clip.Value.start_pkt = 0;
                    }
                    clip.Value.connection = CONNECT_NON_SEAMLESS;
                    break;
            }
            clip.Value.end_pkt = ClpiParse.clpi_lookup_spn(clip.Value.cl, out_time, 0,
                                            mpls_clip[clip.Value.angle].stc_id);
            clip.Value.in_time = in_time;
            clip.Value.out_time = out_time;
            clip.Value.title_pkt = pos.Value;
            pos.Value += clip.Value.end_pkt - clip.Value.start_pkt;
            clip.Value.title_time = time.Value;
            time.Value += clip.Value.out_time - clip.Value.in_time;

            clip.Value.stc_spn = ClpiParse.clpi_find_stc_spn(clip.Value.cl, mpls_clip[clip.Value.angle].stc_id);
        }

        static
        void _nav_title_close(Ref<NAV_TITLE> title)
        {
            uint ii, ss;

            if (title.Value.sub_path)
            {
                for (ss = 0; ss < title.Value.sub_path_count; ss++)
                {
                    if (title.Value.sub_path[ss].clip_list.clip)
                    {
                        for (ii = 0; ii < title.Value.sub_path[ss].clip_list.count; ii++)
                        {
                            ClpiParse.clpi_unref(ref title.Value.sub_path[ss].clip_list.clip[ii].cl);
                        }
                        title.Value.sub_path[ss].clip_list.clip.Free();
                    }
                }
                title.Value.sub_path.Free();
            }

            if (title.Value.clip_list.clip)
            {
                for (ii = 0; ii < title.Value.clip_list.count; ii++)
                {
                    ClpiParse.clpi_unref(ref title.Value.clip_list.clip[ii].cl);
                }
                title.Value.clip_list.clip.Free();
            }

            MplsParse.mpls_free(ref title.Value.pl);
            title.Value.chap_list.mark.Free();
            title.Value.mark_list.mark.Free();
            title.Free();
        }

        internal static void nav_title_close(ref Ref<NAV_TITLE> title)
        {
            if (title)
            {
                _nav_title_close(title);
                title = Ref<NAV_TITLE>.Null;
            }
        }

        internal static Ref<NAV_TITLE> nav_title_open(BD_DISC disc, string playlist, uint angle)
        {
            Ref<NAV_TITLE> title = Ref<NAV_TITLE>.Null;
            uint ii, ss;
            Variable<UInt32> pos = new(0);
            Variable<UInt32> time = new(0);

            title = Ref<NAV_TITLE>.Allocate();
            if (title == null)
            {
                return Ref<NAV_TITLE>.Null;
            }
            title.Value.disc = disc;
            title.Value.name = playlist[..10];
            title.Value.angle_count = 0;
            title.Value.angle = (byte)angle;
            title.Value.pl = MplsParse.mpls_get(disc, playlist);
            if (title.Value.pl == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, $"Fail: Playlist parse {playlist}");
                title.Free();
                return Ref<NAV_TITLE>.Null;
            }

            // Find length in packets and end_pkt for each clip
            if (title.Value.pl.Value.list_count != 0)
            {
                title.Value.clip_list.count = title.Value.pl.Value.list_count;
                title.Value.clip_list.clip = Ref<NAV_CLIP>.Allocate(title.Value.pl.Value.list_count);
                if (!title.Value.clip_list.clip)
                {
                    _nav_title_close(title);
                    return Ref<NAV_TITLE>.Null;
                }
                title.Value.packets = 0;
                for (ii = 0; ii < title.Value.pl.Value.list_count; ii++)
                {
                    Ref<MPLS_PI> pi;
                    Ref<NAV_CLIP> clip;

                    pi = title.Value.pl.Value.play_item.AtIndex(ii);

                    clip = title.Value.clip_list.clip.AtIndex(ii);

                    _fill_clip(title, pi.Value.clip, pi.Value.connection_condition, pi.Value.in_time, pi.Value.out_time, pi.Value.angle_count,
                               pi.Value.still_mode, pi.Value.still_time, clip, ii, pos.Ref, time.Ref);
                }
            }

            // sub paths
            // Find length in packets and end_pkt for each clip
            if (title.Value.pl.Value.sub_count > 0)
            {
                title.Value.sub_path_count = title.Value.pl.Value.sub_count;
                title.Value.sub_path = Ref<NAV_SUB_PATH>.Allocate(title.Value.sub_path_count);
                if (!title.Value.sub_path)
                {
                    _nav_title_close(title);
                    return Ref<NAV_TITLE>.Null;
                }

                for (ss = 0; ss < title.Value.sub_path_count; ss++)
                {
                    Ref<NAV_SUB_PATH> sub_path = title.Value.sub_path.AtIndex(ss);

                    sub_path.Value.type = title.Value.pl.Value.sub_path[ss].type;
                    sub_path.Value.clip_list.count = title.Value.pl.Value.sub_path[ss].sub_playitem_count;
                    if (sub_path.Value.clip_list.count == 0)
                        continue;

                    sub_path.Value.clip_list.clip = Ref<NAV_CLIP>.Allocate(sub_path.Value.clip_list.count);
                    if (!sub_path.Value.clip_list.clip)
                    {
                        _nav_title_close(title);
                        return Ref<NAV_TITLE>.Null;
                    }

                    pos.Value = time.Value = 0;
                    for (ii = 0; ii < sub_path.Value.clip_list.count; ii++)
                    {
                        Ref<MPLS_SUB_PI> pi = title.Value.pl.Value.sub_path[ss].sub_play_item.AtIndex(ii);
                        Ref<NAV_CLIP> clip = sub_path.Value.clip_list.clip.AtIndex(ii);

                        _fill_clip(title, pi.Value.clip, pi.Value.connection_condition, pi.Value.in_time, pi.Value.out_time, 0,
                                   0, 0, clip, ii, pos.Ref, time.Ref);
                    }
                }
            }

            title.Value.chap_list.count = _pl_chapter_count(title.Value.pl);
            if (title.Value.chap_list.count != 0)
            {
                title.Value.chap_list.mark = Ref<NAV_MARK>.Allocate(title.Value.chap_list.count);
            }
            title.Value.mark_list.count = title.Value.pl.Value.mark_count;
            if (title.Value.mark_list.count != 0)
            {
                title.Value.mark_list.mark = Ref<NAV_MARK>.Allocate(title.Value.pl.Value.mark_count);
            }

            _extrapolate_title(title);

            if (title.Value.angle >= title.Value.angle_count)
            {
                title.Value.angle = 0;
            }

            return title;
        }

        // Search for random access point closest to the requested packet
        // Packets are 192 byte TS packets
        internal static Ref<NAV_CLIP> nav_chapter_search(Ref<NAV_TITLE> title, uint chapter,
                                           Ref<UInt32> clip_pkt, Ref<UInt32> out_pkt)
        {
            Ref<NAV_CLIP> clip;

            if (chapter > title.Value.chap_list.count)
            {
                clip = title.Value.clip_list.clip.AtIndex(0);
                clip_pkt.Value = clip.Value.start_pkt;
                out_pkt.Value = clip.Value.title_pkt;
                return clip;
            }
            clip = title.Value.clip_list.clip.AtIndex(title.Value.chap_list.mark[chapter].clip_ref);
            clip_pkt.Value = title.Value.chap_list.mark[chapter].clip_pkt;
            out_pkt.Value = clip.Value.title_pkt + clip_pkt.Value - clip.Value.start_pkt;
            return clip;
        }

        internal static UInt32 nav_chapter_get_current(Ref<NAV_TITLE> title, UInt32 title_pkt)
        {
            Ref<NAV_MARK> mark;
            UInt32 ii;

            if (title == null)
            {
                return 0;
            }
            for (ii = 0; ii < title.Value.chap_list.count; ii++)
            {
                mark = title.Value.chap_list.mark.AtIndex(ii);
                if (mark.Value.title_pkt <= title_pkt)
                {
                    if (ii == title.Value.chap_list.count - 1)
                    {
                        return ii;
                    }
                    mark = title.Value.chap_list.mark.AtIndex(ii + 1);
                    if (mark.Value.title_pkt > title_pkt)
                    {
                        return ii;
                    }
                }
            }
            return 0;
        }

        // Search for random access point closest to the requested packet
        // Packets are 192 byte TS packets
        internal static Ref<NAV_CLIP> nav_mark_search(Ref<NAV_TITLE> title, uint mark,
                                        Ref<UInt32> clip_pkt, Ref<UInt32> out_pkt)
        {
            Ref<NAV_CLIP> clip;

            if (mark > title.Value.mark_list.count)
            {
                clip = title.Value.clip_list.clip.AtIndex(0);
                clip_pkt.Value = clip.Value.start_pkt;
                out_pkt.Value = clip.Value.title_pkt;
                return clip;
            }
            clip = title.Value.clip_list.clip.AtIndex(title.Value.mark_list.mark[mark].clip_ref);
            clip_pkt.Value = title.Value.mark_list.mark[mark].clip_pkt;
            out_pkt.Value = clip.Value.title_pkt + clip_pkt.Value - clip.Value.start_pkt;
            return clip;
        }

        internal static void nav_clip_packet_search(Ref<NAV_CLIP> clip, UInt32 pkt,
                                    Ref<UInt32> clip_pkt, Ref<UInt32> clip_time)
        {
            clip_time.Value = clip.Value.in_time;
            if (clip.Value.cl != null)
            {
                clip_pkt.Value = ClpiParse.clpi_access_point(clip.Value.cl, pkt, 0, 0, clip_time);
                if (clip_pkt.Value < clip.Value.start_pkt)
                {
                    clip_pkt.Value = clip.Value.start_pkt;
                }
                if (clip_time.Value != 0 && clip_time.Value < clip.Value.in_time)
                {
                    /* EP map does not store lowest 8 bits of timestamp */
                    clip_time.Value = clip.Value.in_time;
                }

            }
            else
            {
                clip_pkt.Value = clip.Value.start_pkt;
            }
        }

        // Search for random access point closest to the requested packet
        // Packets are 192 byte TS packets
        // pkt is relative to the beginning of the title
        // out_pkt and out_time is relative to the the clip which the packet falls in
        internal static Ref<NAV_CLIP> nav_packet_search(Ref<NAV_TITLE> title, UInt32 pkt,
                                          Ref<UInt32> clip_pkt, Ref<UInt32> out_pkt, Ref<UInt32> out_time)
        {
            Ref<NAV_CLIP> clip;
            UInt32 pos, len;
            uint ii;

            out_time.Value = 0;
            pos = 0;
            for (ii = 0; ii < title.Value.pl.Value.list_count; ii++)
            {
                clip = title.Value.clip_list.clip.AtIndex(ii);
                len = clip.Value.end_pkt - clip.Value.start_pkt;
                if (pkt < pos + len)
                    break;
                pos += len;
            }
            if (ii == title.Value.pl.Value.list_count)
            {
                clip = title.Value.clip_list.clip.AtIndex(ii - 1);
                out_time.Value = clip.Value.duration + clip.Value.in_time;
                clip_pkt.Value = clip.Value.end_pkt;
            }
            else
            {
                clip = title.Value.clip_list.clip.AtIndex(ii);
                nav_clip_packet_search(clip, pkt - pos + clip.Value.start_pkt, clip_pkt, out_time);
            }
            if (out_time.Value < clip.Value.in_time)
                out_time.Value = 0;
            else
                out_time.Value -= clip.Value.in_time;
            out_pkt.Value = clip.Value.title_pkt + clip_pkt.Value - clip.Value.start_pkt;
            return clip;
        }

        // Search for the nearest random access point after the given pkt
        // which is an angle change point.
        // Packets are 192 byte TS packets
        // pkt is relative to the clip
        // time is the clip relative time where the angle change point occurs
        // returns a packet number
        //
        // To perform a seamless angle change, perform the following sequence:
        // 1. Find the next angle change point with nav_angle_change_search.
        // 2. Read and process packets until the angle change point is reached.
        //    This may mean progressing to the next play item if the angle change
        //    point is at the end of the current play item.
        // 3. Change angles with nav_set_angle. Changing angles means changing
        //    m2ts files. The new clip information is returned from nav_set_angle.
        // 4. Close the current m2ts file and open the new one returned 
        //    from nav_set_angle.
        // 4. If the angle change point was within the time period of the current
        //    play item (i.e. the angle change point is not at the end of the clip),
        //    Search to the timestamp obtained from nav_angle_change_search using
        //    nav_clip_time_search. Otherwise start at the start_pkt defined 
        //    by the clip.
        internal static UInt32 nav_clip_angle_change_search(Ref<NAV_CLIP> clip, UInt32 pkt, Ref<UInt32> time)
        {
            if (clip.Value.cl == null)
            {
                return pkt;
            }
            return ClpiParse.clpi_access_point(clip.Value.cl, pkt, 1, 1, time);
        }

        // Search for random access point closest to the requested time
        // Time is in 45khz ticks
        internal static Ref<NAV_CLIP> nav_time_search(Ref<NAV_TITLE> title, UInt32 tick,
                                        Ref<UInt32> clip_pkt, Ref<UInt32> out_pkt)
        {
            UInt32 pos, len;
            Ref<MPLS_PI> pi = Ref<MPLS_PI>.Null;
            Ref<NAV_CLIP> clip;
            uint ii;

            if (!title.Value.pl)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"Time search failed (title not opened)");
                return Ref<NAV_CLIP>.Null;
            }
            if (title.Value.pl.Value.list_count < 1)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"Time search failed (empty playlist)");
                return Ref<NAV_CLIP>.Null;
            }

            pos = 0;
            for (ii = 0; ii < title.Value.pl.Value.list_count; ii++)
            {
                pi = title.Value.pl.Value.play_item.AtIndex(ii);
                len = pi.Value.out_time - pi.Value.in_time;
                if (tick < pos + len)
                    break;
                pos += len;
            }
            if (ii == title.Value.pl.Value.list_count)
            {
                clip = title.Value.clip_list.clip.AtIndex(ii - 1);
                clip_pkt.Value = clip.Value.end_pkt;
            }
            else
            {
                clip = title.Value.clip_list.clip.AtIndex(ii);
                nav_clip_time_search(clip, tick - pos + pi.Value.in_time, clip_pkt, out_pkt);
            }
            out_pkt.Value = clip.Value.title_pkt + clip_pkt.Value - clip.Value.start_pkt;
            return clip;
        }

        // Search for random access point closest to the requested time
        // Time is in 45khz ticks, between clip in_time and out_time.
        internal static void nav_clip_time_search(Ref<NAV_CLIP> clip, UInt32 tick, Ref<UInt32> clip_pkt, Ref<UInt32> out_pkt)
        {
            if (tick >= clip.Value.out_time)
            {
                clip_pkt.Value = clip.Value.end_pkt;
            }
            else
            {
                if (clip.Value.cl != null)
                {
                    clip_pkt.Value = ClpiParse.clpi_lookup_spn(clip.Value.cl, tick, 1,
                       clip.Value.title.Value.pl.Value.play_item[clip.Value._ref].clip[clip.Value.angle].stc_id);
                    if (clip_pkt.Value < clip.Value.start_pkt)
                    {
                        clip_pkt.Value = clip.Value.start_pkt;
                    }

                }
                else
                {
                    clip_pkt.Value = clip.Value.start_pkt;
                }
            }
            if (out_pkt)
            {
                out_pkt.Value = clip.Value.title_pkt + clip_pkt.Value - clip.Value.start_pkt;
            }
        }

        /*
         * Input Parameters:
         * title     - title struct obtained from nav_title_open
         *
         * Return value:
         * Pointer to NAV_CLIP struct
         * NULL - End of clip list
         */
        internal static Ref<NAV_CLIP> nav_next_clip(Ref<NAV_TITLE> title, Ref<NAV_CLIP> clip)
        {
            if (clip == null)
            {
                return title.Value.clip_list.clip.AtIndex(0);
            }
            if (clip.Value._ref >= title.Value.clip_list.count - 1)
            {
                return Ref<NAV_CLIP>.Null;
            }
            return title.Value.clip_list.clip.AtIndex(clip.Value._ref + 1);
        }

        internal static void nav_set_angle(Ref<NAV_TITLE> title, uint angle)
        {
            int ii;
            Variable<UInt32> pos = new(0);
            Variable<UInt32> time = new(0);

            if (title == null)
            {
                return;
            }
            if (angle > 8)
            {
                // invalid angle
                return;
            }
            if (angle == title.Value.angle)
            {
                // no change
                return;
            }

            title.Value.angle = (byte)angle;
            // Find length in packets and end_pkt for each clip
            title.Value.packets = 0;
            for (ii = 0; ii < title.Value.pl.Value.list_count; ii++)
            {
                Ref<MPLS_PI> pi;
                Ref<NAV_CLIP> cl;

                pi = title.Value.pl.Value.play_item.AtIndex(ii);
                cl = title.Value.clip_list.clip.AtIndex(ii);

                _fill_clip(title, pi.Value.clip, pi.Value.connection_condition, pi.Value.in_time, pi.Value.out_time, pi.Value.angle_count,
                           pi.Value.still_mode, pi.Value.still_time, cl, (uint)ii, pos.Ref, time.Ref);
            }
            _extrapolate_title(title);
        }

        internal static string nav_clip_textst_font(Ref<NAV_CLIP> clip, int index)
        {
            string file;

            if (index < 0 || index >= clip.Value.cl.Value.clip.font_info.Value.font_count)
                return null;

            file = $"{clip.Value.cl.Value.clip.font_info.Value.font[index].file_id}.otf";
            return file;
        }
    }
}
