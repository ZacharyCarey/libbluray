using libbluray;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTests;
using libbluray.bdnav;
using System.Numerics;
using libbluray.util;
using System.Collections.ObjectModel;
using Iso639;

namespace UnitTests.BlurayInfo
{

    public class bluray_info
    {
        public string disc_name;
        public string udf_volume_id;
        public string disc_id;
        public UInt32 titles;
        public UInt32 main_title;
        public bool first_play_supported;
        public bool top_menu_supported;
        public UInt32 disc_num_titles;
        public UInt32 hdmv_titles;
        public UInt32 bdj_titles;
        public UInt32 unsupported_titles;
        public bool aacs;
        public bool bdplus;
        public bool bdj;
        public bool content_exist_3D;
        public string provider_data;
        public string initial_output_mode_preference;

        public UInt32 longest_playlist;
    }

    public class bluray_title
    {
        public uint json_ix;
        public UInt32 ix;
        public UInt32 number;
        public UInt32 playlist;
        public Time duration;
        public UInt64 seconds;
        public UInt64 minutes;
        public UInt64 size;
        public UInt64 size_mbs;
        public UInt64 blocks;
        public UInt32 chapters;
        public UInt32 clips;
        public byte angles;
        public byte video_streams;
        public byte audio_streams;
        public byte pg_streams;
        public string length;
        public ReadOnlyCollection<BLURAY_CLIP_INFO> clip_info;
        public ReadOnlyCollection<BLURAY_TITLE_CHAPTER> title_chapters;

        public List<bluray_video> VideoStreams = new();
        public List<bluray_audio> AudioStreams = new();
        public List<bluray_pgs> SubtitleStreams = new();
        public List<bluray_chapter> Chapters = new();
    }

    public class bluray_chapter
    {
        public Time duration;
        public Time start;
        public string start_time;
        public string length;
        public Int64[] range = new Int64[2];
        public UInt64 size;
        public UInt64 size_mbs;
        public UInt64 blocks;

        public uint chapter_number;
    }

    public partial class BlurayInfo
    {
        public const int BLURAY_BLOCK_SIZE = 192;
        public const int BLURAY_LANG_STRLEN = 4;
        public const int BLURAY_INFO_DISC_ID_STRLEN = 41;
        public const int BLURAY_INFO_UDF_VOLUME_ID_STRLEN = 33;
        public const int BLURAY_INFO_PROVIDER_DATA_STRLEN = 33;
        public const int BLURAY_INFO_DISC_NAME_STRLEN = 256;

        /**
        * Get main Blu-ray metadata from disc
        */
        public static int bluray_info_init(BLURAY bd, out bluray_info bluray_info, bool display_duplicates)
        {
            bluray_info = new();

            // Get main disc information
            BLURAY_DISC_INFO? bd_disc_info = null;
            bd_disc_info = BLURAY.bd_get_disc_info(bd);

            // Quit if couldn't open disc
            if (bd_disc_info == null)
                return 1;

            // Set Blu-ray disc name
            bluray_info.disc_name = "";
            Ref<META_DL> bd_meta = Ref<META_DL>.Null;
            bd_meta = BLURAY.bd_get_meta(bd);
            if (bd_meta != null)
                bluray_info.disc_name = bd_meta.Value.di_name;

            // Use the UDF volume name as disc title; will only work if input file
            // is an image or disc.
            bluray_info.udf_volume_id = "";
            if (!string.IsNullOrWhiteSpace(bd_disc_info.UdfVolumeID))
                bluray_info.udf_volume_id = bd_disc_info.UdfVolumeID;

            // Set the disc ID if AACS is present
            bluray_info.disc_id = "";
            UInt32 ix = 0;
            if (bd_disc_info.LibAacsDetected == true)
            {
                //for (ix = 0; ix < 20; ix++)
                //{
                //    bluray_info.disc_id += $"{bd_disc_info.disc_id[ix]:X2}";
                //}
                bluray_info.disc_id = bd_disc_info.DiscID;
            }

            // Titles, Indexes and Playlists
            //
            // libbluray has a "title" which is a really an index it uses to list the
            // playlists based on the type queried. It has stuck as the "title index" for
            // media players (mplayer, mpv).
            //
            // The de facto title index can cause problems if using another application
            // that prefers another index method for accessing the playlists (if such
            // a thing exists). bdpslice (part of libbluray) takes both a title number
            // or a playlist number as an argument, and passing the playlist number
            // is more certain.
            //
            // libbluray indexes titles starting at 0, but for human-readable, bluray_info
            // starts at 1. Playlists start at 0, because they are indexed as such on the
            // filesystem.
            //
            // There are two ways to display the titles using libbluray: you can display
            // the "relevant" titles which filters out duplicate titles and clips, or
            // you can display all the titles. Programs like mpv and ffmpeg will display
            // relevant titles only, so that is the default here as well.
            //
            // You can choose to display all the titles, including the duplicates. It is
            // important to note that titles that are marked as duplicates can vary
            // across environments, so if you want consistency, then display all using
            // the option flag.
            if (display_duplicates)
                bluray_info.titles = BLURAY.bd_get_titles(bd, BLURAY.TITLES_ALL, 0);
            else
                bluray_info.titles = BLURAY.bd_get_titles(bd, BLURAY.TITLES_RELEVANT, 0);
            bluray_info.main_title = 0;

            int bd_main_title = BLURAY.bd_get_main_title(bd);
            if (bd_main_title == -1)
                return 1;
            bluray_info.main_title = (UInt32)bd_main_title;

            // These are going to change depending on if you have the JVM installed or not
            bluray_info.first_play_supported = bd_disc_info.FirsPlaySupported;
            bluray_info.top_menu_supported = bd_disc_info.TopMenuSupported;
            bluray_info.disc_num_titles = bd_disc_info.NumberOfTitles;
            bluray_info.hdmv_titles = bd_disc_info.NumberHdmvTitles;
            bluray_info.bdj_titles = bd_disc_info.NumberBdjTitles;
            bluray_info.unsupported_titles = bd_disc_info.NumberUnsupportedTitles;
            bluray_info.aacs = bd_disc_info.AacsDetected;
            bluray_info.bdplus = bd_disc_info.BdPlusDetected;
            bluray_info.bdj = bd_disc_info.BdjDetected;
            bluray_info.content_exist_3D = bd_disc_info.ContentExist3D;
            bluray_info.provider_data = bd_disc_info.ProviderData;
            bluray_info.initial_output_mode_preference = (bd_disc_info.InitialOutputModePreference ? "3D" : "2D");

            return 0;

        }

        /**
         * Initialize and populate a bluray_title struct
         */
        public static int bluray_title_init(BLURAY bd, out bluray_title bluray_title, UInt32 title_ix, byte angle_ix, bool playlist)
        {
            bluray_title = new();
            // Initialize to safe values
            bluray_title.ix = title_ix;
            bluray_title.number = title_ix + 1;
            bluray_title.playlist = 0;
            bluray_title.duration = Time.Zero;
            bluray_title.seconds = 0;
            bluray_title.minutes = 0;
            bluray_title.size = 0;
            bluray_title.size_mbs = 0;
            bluray_title.blocks = 0;
            bluray_title.chapters = 0;
            bluray_title.clips = 0;
            bluray_title.angles = 0;
            bluray_title.video_streams = 0;
            bluray_title.audio_streams = 0;
            bluray_title.pg_streams = 0;
            bluray_title.length = "00:00:00.000";

            bool retval = false;

            // Quit if couldn't open title
            if (playlist)
                retval = BLURAY.bd_select_playlist(bd, title_ix);
            else
                retval = BLURAY.bd_select_title(bd, title_ix);
            if (retval == false)
                return 1;

            // Quit if couldn't select angle
            retval = BLURAY.bd_select_angle(bd, angle_ix);
            if (retval == false)
                return 2;

            BLURAY_TITLE_INFO? bd_title = null;
            if (playlist)
                bd_title = BLURAY.bd_get_playlist_info(bd, title_ix, angle_ix);
            else
                bd_title = BLURAY.bd_get_title_info(bd, title_ix, angle_ix);

            // Quit if couldn't get title info
            if (bd_title == null)
                return 3;

            // Populate data
            bluray_title.playlist = bd_title.PlaylistID;
            bluray_title.duration = bd_title.Duration;
            bluray_title.seconds = bluray_title.duration.Seconds;
            bluray_title.minutes = bluray_title.duration.Minutes;
            bluray_title.length = bluray_title.duration.ToString();
            bluray_title.size = BLURAY.bd_get_title_size(bd);
            bluray_title.size_mbs = (bluray_title.size / 1048576) + 1;
            bluray_title.blocks = bluray_title.size / BLURAY_BLOCK_SIZE;
            bluray_title.chapters = bd_title.ChapterCount;
            bluray_title.clips = bd_title.ClipCount;
            bluray_title.angles = bd_title.AngleCount;
            if (bluray_title.clips != 0)
            {
                bluray_title.video_streams = bd_title.Clips[0].VideoStreamCount;
                bluray_title.audio_streams = bd_title.Clips[0].AudioStreamCount;
                bluray_title.pg_streams = bd_title.Clips[0].PresentationStreamCount;
            }

            bluray_title.clip_info = bd_title.Clips;
            bluray_title.title_chapters = bd_title.Chapters;

            return 0;

        }

        public static bool bluray_title_has_alang(bluray_title bluray_title, Language lang)
        {

            BLURAY_STREAM_INFO bd_stream;
            byte ix = 0;

            for (ix = 0; ix < bluray_title.audio_streams; ix++)
            {

                bd_stream = bluray_title.clip_info[0].AudioStreams[ix];

                //if (bd_stream == null)
                //    continue;

                if (bd_stream.Language == lang)
                    return true;

            }

            return false;

        }

        public static bool bluray_title_has_slang(bluray_title bluray_title, Language lang)
        {

            BLURAY_STREAM_INFO bd_stream;
            byte ix = 0;

            for (ix = 0; ix < bluray_title.pg_streams; ix++)
            {

                bd_stream = bluray_title.clip_info[0].PresentationStreams[ix];

                //if (bd_stream == null)
                //    continue;

                if (bd_stream.Language == lang)
                    return true;

            }

            return false;

        }

        /**
         * Compare integers for qsort()
         */
        public static int int_compare(uint a, uint b)
        {

            return a.CompareTo(b);

        }
    }
}
