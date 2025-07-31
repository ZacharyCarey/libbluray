using libbluray;
using libbluray.bdnav;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.BlurayInfo
{
    public partial class BlurayInfo
    {
        string device_filename;

        bool p_bluray_info = true;
        bool p_bluray_json = false;
        bool p_bluray_xchap = false;
        bool d_playlist_number = false;
        UInt32 arg_playlist_number = 0;
        bool d_main_playlist = false;
        bool d_video = false;
        bool d_audio = false;
        bool d_subtitles = false;
        bool d_chapters = false;
        bool d_duplicates = false;
        bool d_has_alang = false;
        bool d_has_slang = false;
        string d_alang = "";
        string d_slang = "";
        ulong arg_number = 0;
        UInt32 d_min_seconds = 0;
        UInt32 d_min_minutes = 0;
        UInt32 d_min_audio_streams = 0;
        UInt32 d_min_pg_streams = 0;
        public UInt32 main_playlist = 0;
        UInt32 json_ix = 1;
        bool exit_help = false;
        string key_db_filename = "";
        int g_opt = 0;
        int g_ix = 0;

        public bluray_info Info;
        public Ref<META_DL> bd_meta = Ref<META_DL>.Null;
        public List<bluray_title> Titles = new();

        public BlurayInfo(string root)
        {
            // --all
            d_video = true;
            d_audio = true;
            d_chapters = true;
            d_subtitles = true;

            // --json
            p_bluray_info = false;
            p_bluray_xchap = false;
            p_bluray_json = true;

            // initialize
            device_filename = root;
            Ref<BLURAY> bd = Ref<BLURAY>.Null;
            bd = BLURAY.bd_open(device_filename, null);
            Assert.IsNotNull(bd, "Failed to open Bluray library.");

            // Blu-ray
            bluray_info bluray_info;
            int retval = bluray_info_init(bd, out bluray_info, d_duplicates);
            Assert.AreEqual(0, retval, "Failed to initialize bluray_info");
            this.Info = bluray_info;

            /** SORTING PLAYLISTS **/
            UInt32 ix = 0;
            byte angle_ix = 0;

            UInt32[] arr_playlists = new UInt32[bluray_info.titles];

            Ref<BLURAY_TITLE_INFO> bd_title = Ref<BLURAY_TITLE_INFO>.Null;

            UInt32 num_playlists = 0;

            for (ix = 0; ix < bluray_info.titles; ix++)
            {

                num_playlists++;

                bd_title = BLURAY.bd_get_title_info(bd, ix, angle_ix);

                // Ideally this should probably skip this title and keep going, but
                // I don't know what the consequences would be and how to fix it.
                // Randomly removing playlist and media files doesn't seem to
                // affect it, as libbluray simply works around them as if they
                // weren't there to begin with.
                Assert.IsNotNull(bd_title, $"Couldn't open title {ix}");

                arr_playlists[ix] = bd_title.Value.playlist;

                if (bd_title.Value.idx == bluray_info.main_title)
                    main_playlist = bd_title.Value.playlist;

                //bd_free_title_info(bd_title);
                bd_title = Ref<BLURAY_TITLE_INFO>.Null;

            }

            Array.Sort(arr_playlists, 0, (int)bluray_info.titles);

            /** END SORTING **/

            if (d_playlist_number)
            {

                retval = BLURAY.bd_select_playlist(bd, arg_playlist_number) ? 1 : 0;
                Assert.AreEqual(0, retval, $"Playlist {arg_playlist_number} is not valid, choose another one.");
            }

            if (p_bluray_info)
            {
                Console.WriteLine($"Disc title: '{bluray_info.disc_name}', Volume name: '{bluray_info.udf_volume_id}', Main playlist: {main_playlist}, AACS: {(bluray_info.aacs ? "yes" : "no")}, BD-J: {(bluray_info.bdj ? "yes" : "no")}, BD+: {(bluray_info.bdplus ? "yes" : "no")}");
            }

            if (p_bluray_json)
            {

                // Find the longest title
                UInt64 max_duration = 0;
                UInt32 longest_playlist = 0;
                for (ix = 0; ix < bluray_info.titles; ix++)
                {

                    bd_title = BLURAY.bd_get_title_info(bd, ix, angle_ix);

                    if (bd_title == null)
                    {
                        continue;
                    }

                    if (bd_title.Value.duration > max_duration)
                    {
                        longest_playlist = bd_title.Value.playlist;
                        max_duration = bd_title.Value.duration;
                    }

                    //bd_title.bd_free_title_info();
                    bd_title = Ref<BLURAY_TITLE_INFO>.Null;

                }

                bluray_info.longest_playlist = longest_playlist;

                //printf("{\n");
                //printf(" \"bluray\": {\n");
                //printf("  \"disc name\": \"%s\",\n", bluray_info.disc_name);
                //printf("  \"udf title\": \"%s\",\n", bluray_info.udf_volume_id);
                //printf("  \"disc id\": \"%s\",\n", bluray_info.disc_id);
                //printf("  \"main playlist\": %" PRIu32 ",\n", main_playlist);
                //printf("  \"longest playlist\": %" PRIu32 ",\n", longest_playlist);
                //printf("  \"first play supported\": %s,\n", (bluray_info.first_play_supported ? "true" : "false"));
                //printf("  \"top menu supported\": %s,\n", (bluray_info.top_menu_supported ? "true" : "false"));
                //printf("  \"provider data\": \"%s\",\n", bluray_info.provider_data);
                //printf("  \"3D content\": %s,\n", (bluray_info.content_exist_3D ? "true" : "false"));
                //printf("  \"initial mode\": \"%s\",\n", bluray_info.initial_output_mode_preference);
                //printf("  \"titles\": %" PRIu32 ",\n", bluray_info.titles);
                //printf("  \"bdinfo titles\": %" PRIu32 ",\n", bluray_info.disc_num_titles);
                //printf("  \"hdmv titles\": %" PRIu32 ",\n", bluray_info.hdmv_titles);
                //printf("  \"bd-j titles\": %" PRIu32 ",\n", bluray_info.bdj_titles);
                //printf("  \"unsupported titles\": %" PRIu32 ",\n", bluray_info.unsupported_titles);
                //printf("  \"aacs\": %s,\n", (bluray_info.aacs ? "true" : "false"));
                //printf("  \"bdplus\": %s,\n", (bluray_info.bdplus ? "true" : "false"));
                //printf("  \"bd-j\": %s\n", (bluray_info.bdj ? "true" : "false"));
                //printf(" },\n");

                // Fetch metadata from optional XML file (generally bdmt_eng.xml)
                bd_meta = BLURAY.bd_get_meta(bd);

                if (bd_meta != null)
                {

                    //printf(" \"xml\": {\n");
                    //printf("  \"filename\": \"%s\",\n", bd_meta.filename);
                    //printf("  \"language\": \"%s\",\n", bd_meta.language_code);
                    //printf("  \"num sets\": %" PRIu8 ",\n", bd_meta.di_num_sets);
                    //printf("  \"set number\": %" PRIu8 "\n", bd_meta.di_set_number);
                    //printf(" },\n");

                }

            }

            Ref<BLURAY_STREAM_INFO> bd_stream = Ref<BLURAY_STREAM_INFO>.Null;
            Ref<BLURAY_TITLE_CHAPTER> bd_chapter = Ref<BLURAY_TITLE_CHAPTER>.Null;


            bluray_title bluray_title = new();
            bluray_video bluray_video = new();
            bluray_audio bluray_audio = new();
            bluray_pgs bluray_pgs = new();
            bluray_chapter bluray_chapter = new();
            bluray_chapter.duration = 0;
            bluray_chapter.length = "00:00:00.000";
            bluray_chapter.size = 0;
            bluray_chapter.size_mbs = 0;
            bluray_chapter.blocks = 0;

            //if (p_bluray_json)
            //    printf(" \"titles\": [\n");

            byte video_stream_ix = 0;
            byte video_stream_number = 1;
            byte audio_stream_ix = 0;
            byte audio_stream_number = 1;
            byte pg_stream_ix = 0;
            byte pg_stream_number = 1;
            UInt32 chapter_ix = 0;
            UInt32 chapter_number = 1;
            UInt64 chapter_start = 0;
            UInt32 d_num_json_titles = 0;
            UInt32 d_num_json_displayed = 0;
            angle_ix = 0;

            // Get the total number of titles expected to display in JSON output
            if (p_bluray_json) {

                if (d_main_playlist || d_playlist_number) {

                    d_num_json_titles = 1;

                } else {

                    for (ix = 0; ix < num_playlists; ix++) {

                        retval = bluray_title_init(bd, out bluray_title, ix, angle_ix, false);

                        // Skip if there was a problem getting it
                        if (retval != 0) {
                            Console.Error.WriteLine($"Could not open title {ix}, skippping");
                            continue;
                        }

                        if (!(bluray_title.seconds >= d_min_seconds && bluray_title.minutes >= d_min_minutes && bluray_title.audio_streams >= d_min_audio_streams && bluray_title.pg_streams >= d_min_pg_streams))
                            continue;

                        if (d_has_alang && (bluray_title.audio_streams == 0 || !(bluray_title_has_alang(bluray_title, d_alang))))
                            continue;

                        if (d_has_slang && (bluray_title.pg_streams == 0 || !(bluray_title_has_slang(bluray_title, d_slang))))
                            continue;

                        d_num_json_titles++;

                    }

                }

            }

            // Display the titles in bluray_info / bluray_json
            for (ix = 0; ix < num_playlists; ix++)
            {

                retval = bluray_title_init(bd, out bluray_title, arr_playlists[ix], angle_ix, true);

                //if (debug)
                //    fprintf(stderr, "bluray_title_init: %s\n", retval ? "failed" : "opened");

                // Skip if there was a problem getting it
                if (retval != 0)
                    continue;

                //if (debug)
                //    fprintf(stderr, "examining playlist %u\n", bluray_title.playlist);

                if (d_main_playlist && bluray_title.playlist != main_playlist)
                {
                    //if (debug)
                    //    fprintf(stderr, "not main playlist, skipping\n");
                    continue;
                }

                if (d_playlist_number && bluray_title.playlist != arg_playlist_number)
                {
                    //if (debug)
                    //{
                    //    fprintf(stderr, "arg playlist number: %u\n", arg_playlist_number);
                    //    fprintf(stderr, "skipping playlist %u for not arg playlist number\n", bluray_title.playlist);
                    //}
                    continue;
                }

                if (bluray_title.seconds < d_min_seconds)
                    continue;

                if (bluray_title.minutes < d_min_minutes)
                    continue;

                if (bluray_title.audio_streams < d_min_audio_streams)
                    continue;

                if (bluray_title.pg_streams < d_min_pg_streams)
                    continue;

                if (d_has_alang && (bluray_title.audio_streams == 0 || !(bluray_title_has_alang(bluray_title, d_alang))))
                {
                    //if (debug)
                    //    fprintf(stderr, "doesn't match audio lang skipping playlist %u\n", bluray_title.playlist);
                    continue;
                }

                if (d_has_slang && (bluray_title.pg_streams == 0 || !(bluray_title_has_slang(bluray_title, d_slang))))
                {
                    //if (debug)
                    //    fprintf(stderr, "doesn't match subtitle lang skipping playlist %u\n", bluray_title.playlist);
                    continue;
                }

                if (p_bluray_info)
                {

                    //printf("Playlist: %*" PRIu32 ", Length: %s, Chapters: %*"PRIu32 ", Video streams: %*" PRIu8 ", Audio streams: %*" PRIu8 ", Subtitles: %*" PRIu8 ", Angles: %*" PRIu8 ", Filesize: %*" PRIu64 " MBs\n", 5, bluray_title.playlist, bluray_title.length, 3, bluray_title.chapters, 2, bluray_title.video_streams, 2, bluray_title.audio_streams, 2, bluray_title.pg_streams, 2, bluray_title.angles, 6, bluray_title.size_mbs);

                }

                if (p_bluray_json)
                {
                    Titles.Add(bluray_title);
                    //printf("  {\n");
                    //printf("   \"title\": %u,\n", json_ix);
                    //printf("   \"playlist\": %" PRIu32 ",\n", bluray_title.playlist);
                    //printf("   \"length\": \"%s\",\n", bluray_title.length);
                    //printf("   \"msecs\": %" PRIu64 ",\n", bluray_title.duration / 900);
                    //printf("   \"angles\": %" PRIu8 ",\n", bluray_title.angles);
                    //printf("   \"blocks\": %" PRIu64 ",\n", bluray_title.blocks);
                    //printf("   \"filesize\": %" PRIu64 ",\n", bluray_title.size);

                    d_num_json_displayed++;

                    json_ix++;

                }

                // Blu-ray video streams
                if ((p_bluray_info && d_video) || p_bluray_json)
                {

                    //if (p_bluray_json)
                    //    printf("   \"video\": [\n");

                    for (video_stream_ix = 0; video_stream_ix < bluray_title.video_streams; video_stream_ix++)
                    {

                        video_stream_number = (byte)(video_stream_ix + 1);
                        bd_stream = bluray_title.clip_info[0].video_streams.AtIndex(video_stream_ix);

                        if (bd_stream == null)
                            continue;

                        bluray_video_codec(out bluray_video.codec, (bd_stream_type_e)bd_stream.Value.coding_type);
                        bluray_video_codec_name(out bluray_video.codec_name, (bd_stream_type_e)bd_stream.Value.coding_type);
                        bluray_video_format(out bluray_video.format, (bd_video_format_e)bd_stream.Value.format);
                        bluray_video.framerate = bluray_video_framerate((bd_video_rate_e)bd_stream.Value.rate);
                        bluray_video_aspect_ratio(out bluray_video.aspect_ratio, (bd_video_aspect_e)bd_stream.Value.aspect);

                        if (p_bluray_info && d_video)
                        {
                            //printf("	Video: %*u, Format: %s, Aspect ratio: %s, FPS: %.02f, Codec: %s\n", 2, video_stream_number, bluray_video.format, bluray_video.aspect_ratio, bluray_video.framerate, bluray_video.codec);
                        }

                        if (p_bluray_json)
                        {
                            bluray_title.VideoStreams.Add(bluray_video);
                            bluray_video.video_stream_number = video_stream_number;
                            bluray_video.pid = bd_stream.Value.pid;

                            //printf("    {\n");
                            //printf("     \"track\": %" PRIu8 ",\n", video_stream_number);
                            //printf("     \"stream\": \"0x%x\",\n", bd_stream.pid);
                            //printf("     \"format\": \"%s\",\n", bluray_video.format);
                            //printf("     \"aspect ratio\": \"%s\",\n", bluray_video.aspect_ratio);
                            //printf("     \"framerate\": %.02f,\n", bluray_video.framerate);
                            //printf("     \"codec\": \"%s\",\n", bluray_video.codec);
                            //printf("     \"codec name\": \"%s\"\n", bluray_video.codec_name);
                            //if (video_stream_number < bluray_title.video_streams)
                            //    printf("    },\n");
                            //else
                            //    printf("    }\n");
                        }

                    }

                    bd_stream = Ref<BLURAY_STREAM_INFO>.Null;

                    //if (p_bluray_json)
                    //    printf("   ],\n");

                }

                // Blu-ray audio streams
                if ((p_bluray_info && d_audio) || p_bluray_json)
                {

                    //if (p_bluray_json)
                    //    printf("   \"audio\": [\n");

                    for (audio_stream_ix = 0; audio_stream_ix < bluray_title.audio_streams; audio_stream_ix++)
                    {

                        audio_stream_number = (byte)(audio_stream_ix + 1);
                        bd_stream = bluray_title.clip_info[0].audio_streams.AtIndex(audio_stream_ix);

                        if (bd_stream == null)
                            continue;

                        bluray_audio_lang(out bluray_audio.lang, bd_stream.Value.lang);
                        bluray_audio_codec(out bluray_audio.codec, (bd_stream_type_e)bd_stream.Value.coding_type);
                        bluray_audio_codec_name(out bluray_audio.codec_name, (bd_stream_type_e)bd_stream.Value.coding_type);
                        bluray_audio_format(out bluray_audio.format, (bd_audio_format_e)bd_stream.Value.format);
                        bluray_audio_rate(out bluray_audio.rate, (bd_audio_rate_e)bd_stream.Value.rate);

                        //if (p_bluray_info && d_audio)
                        //{
                        //    printf("	Audio: %*" PRIu8 ", Language: %s, Codec: %s, Format: %s, Rate: %s\n", 2, audio_stream_number, bluray_audio.lang, bluray_audio.codec, bluray_audio.format, bluray_audio.rate);
                        //}

                        if (p_bluray_json)
                        {
                            bluray_title.AudioStreams.Add(bluray_audio);
                            bluray_audio.audio_stream_number = audio_stream_number;
                            bluray_audio.pid = bd_stream.Value.pid;

                            //printf("    {\n");
                            //printf("     \"track\": %" PRIu8 ",\n", audio_stream_number);
                            //printf("     \"stream\": \"0x%x\",\n", bd_stream.pid);
                            //printf("     \"language\": \"%s\",\n", bluray_audio.lang);
                            //printf("     \"codec\": \"%s\",\n", bluray_audio.codec);
                            //printf("     \"codec name\": \"%s\",\n", bluray_audio.codec_name);
                            //printf("     \"format\": \"%s\",\n", bluray_audio.format);
                            //printf("     \"rate\": \"%s\"\n", bluray_audio.rate);
                            //if (audio_stream_number < bluray_title.audio_streams)
                            //    printf("    },\n");
                            //else
                            //    printf("    }\n");
                        }

                    }

                    bd_stream = Ref<BLURAY_STREAM_INFO>.Null;

                    //if (p_bluray_json)
                    //    printf("   ],\n");

                }

                // Blu-ray PGS streams
                if ((p_bluray_info && d_subtitles) || p_bluray_json)
                {

                    //if (p_bluray_json)
                    //    printf("   \"subtitles\": [\n");

                    for (pg_stream_ix = 0; pg_stream_ix < bluray_title.pg_streams; pg_stream_ix++)
                    {

                        pg_stream_number = (byte)(pg_stream_ix + 1);
                        bd_stream = bluray_title.clip_info[0].pg_streams.AtIndex(pg_stream_ix);

                        if (bd_stream == null)
                            continue;

                        bluray_pgs_lang(out bluray_pgs.lang, bd_stream.Value.lang);

                        //if (p_bluray_info && d_subtitles)
                        //{
                        //    printf("	Subtitle: %*" PRIu8 ", Language: %s\n", 2, pg_stream_number, bluray_pgs.lang);
                        //}

                        if (p_bluray_json)
                        {
                            bluray_title.SubtitleStreams.Add(bluray_pgs);
                            bluray_pgs.pg_stream_number = pg_stream_number;
                            bluray_pgs.pid = bd_stream.Value.pid;

                            //printf("    {\n");
                            //printf("     \"track\": %" PRIu8 ",\n", pg_stream_number);
                            //printf("     \"stream\": \"0x%x\",\n", bd_stream.pid);
                            //printf("     \"language\": \"%s\"\n", bluray_pgs.lang);
                            //if (pg_stream_number < bluray_title.pg_streams)
                            //    printf("    },\n");
                            //else
                            //    printf("    }\n");
                        }

                        bd_stream = Ref<BLURAY_STREAM_INFO>.Null;

                    }

                    //if (p_bluray_json)
                    //    printf("   ],\n");

                }

                // Blu-ray chapters
                if ((p_bluray_info && d_chapters) || p_bluray_json || p_bluray_xchap)
                {

                    //if (p_bluray_json)
                    //    printf("   \"chapters\": [\n");

                    for (chapter_ix = 0; chapter_ix < bluray_title.chapters; chapter_ix++)
                    {

                        chapter_number = chapter_ix + 1;
                        bd_chapter = bluray_title.title_chapters.AtIndex(chapter_ix);

                        if (bd_chapter == null)
                            continue;

                        bluray_chapter.start = chapter_start;
                        bluray_chapter.duration = bd_chapter.Value.duration;
                        BlurayTime.bluray_duration_length(out bluray_chapter.length, bluray_chapter.duration);
                        BlurayTime.bluray_duration_length(out bluray_chapter.start_time, bluray_chapter.start);
                        bluray_chapter.size = bluray_chapter_size(bd, bluray_title.number - 1, chapter_ix);
                        bluray_chapter.blocks = bluray_chapter.size / BLURAY_BLOCK_SIZE;

                        //if (p_bluray_info && d_chapters)
                        //{
                        //    printf("	Chapter: %*" PRIu32 ", Start: %s, Length: %s\n", 3, chapter_number, bluray_chapter.start_time, bluray_chapter.length);
                        //}

                        if (p_bluray_json)
                        {
                            bluray_title.Chapters.Add(bluray_chapter);
                            bluray_chapter.chapter_number = chapter_number;

                            //printf("    {\n");
                            //printf("     \"chapter\": %" PRIu32 ",\n", chapter_number);
                            //printf("     \"start time\": \"%s\",\n", bluray_chapter.start_time);
                            //printf("     \"length\": \"%s\",\n", bluray_chapter.length);
                            //printf("     \"start\": %" PRIu64 ",\n", bluray_chapter.start / 900);
                            //printf("     \"duration\": %" PRIu64 ",\n", bd_chapter.duration / 900);
                            //printf("     \"blocks\": %" PRIu64 ",\n", bluray_chapter.blocks);
                            //printf("     \"filesize\": %" PRIu64 "\n", bluray_chapter.size);
                            //if (chapter_number < bluray_title.chapters)
                            //    printf("    },\n");
                            //else
                            //    printf("    }\n");
                        }

                        //if (p_bluray_xchap && (bluray_title.playlist == arg_playlist_number || bluray_title.playlist == main_playlist))
                        //{
                        //    printf("CHAPTER%03" PRIu32 "=%s\n", chapter_number, bluray_chapter.start_time);
                        //    printf("CHAPTER%03" PRIu32 "NAME=Chapter %03" PRIu32 "\n", chapter_number, chapter_number);
                        //}

                        chapter_start += bluray_chapter.duration;

                        bd_chapter = Ref<BLURAY_TITLE_CHAPTER>.Null;

                    }

                    //if (p_bluray_json)
                    //    printf("   ]\n");

                }

                if (p_bluray_json)
                {
                    //if (d_num_json_displayed < d_num_json_titles)
                    //    printf("  },\n");
                    //else
                    //    printf("  }\n");
                }

                // Exit out if we found our playlist
                if ((d_main_playlist && bluray_title.playlist == main_playlist) || (d_playlist_number && bluray_title.playlist == arg_playlist_number))
                    break;

            }

            //if (p_bluray_json)
            //{
            //    printf(" ]\n");
            //    printf("}\n");
            //}

            BLURAY.bd_close(bd);
            bd = Ref<BLURAY>.Null;

            //return 0;
        }
    }
}
