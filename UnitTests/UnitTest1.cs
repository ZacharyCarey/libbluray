namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        private const string DiscBackupFolder = @"C:\Users\Zack\Videos\DiscBackup";


        private void TestMovie(string movieName, string infoFileName)
        {
            string rootFolderPath = Path.Combine(DiscBackupFolder, movieName);
            UnitTests.Json.MovieInfo info = UnitTests.Json.MovieInfo.Load(infoFileName);
            Assert.IsNotNull(info, "Failed to load expected info from json file.");

            // Read actual data using libbluray
            UnitTests.BlurayInfo.BlurayInfo actual = new(rootFolderPath);

            // Bluray
            Assert.AreEqual(info.Bluray.DiscName, actual.Info.disc_name);
            Assert.AreEqual(info.Bluray.UdfTitle, actual.Info.udf_volume_id);
            Assert.AreEqual(info.Bluray.DiscID, actual.Info.disc_id);
            Assert.AreEqual(info.Bluray.MainPlaylist, (int)actual.main_playlist);
            Assert.AreEqual(info.Bluray.LongestPlaylist, (int)actual.Info.longest_playlist);
            Assert.AreEqual(info.Bluray.FirstPlaySupported, actual.Info.first_play_supported);
            Assert.AreEqual(info.Bluray.TopMenuSupported, actual.Info.top_menu_supported);
            Assert.AreEqual(info.Bluray.ProviderData, actual.Info.provider_data);
            Assert.AreEqual(info.Bluray.Content3D, actual.Info.content_exist_3D);
            Assert.AreEqual(info.Bluray.InitialMode, actual.Info.initial_output_mode_preference);
            Assert.AreEqual(info.Bluray.Titles, (int)actual.Info.titles);
            Assert.AreEqual(info.Bluray.BdInfoTitles, (int)actual.Info.disc_num_titles);
            Assert.AreEqual(info.Bluray.HdmvTitles, (int)actual.Info.hdmv_titles);
            Assert.AreEqual(info.Bluray.BdjTitles, (int)actual.Info.bdj_titles);
            Assert.AreEqual(info.Bluray.UnsupportedTitles, (int)actual.Info.unsupported_titles);
            Assert.AreEqual(info.Bluray.AACS, actual.Info.aacs);
            Assert.AreEqual(info.Bluray.BdPlus, actual.Info.bdplus);
            Assert.AreEqual(info.Bluray.BDJ, actual.Info.bdj);

            // XML
            Assert.AreEqual(info.xml != null, actual.bd_meta != null, "XML existance does not match.");
            if (actual.bd_meta != null)
            {
                Assert.AreEqual(info.xml.Filename, actual.bd_meta.Value.filename);
                Assert.AreEqual(info.xml.Language, actual.bd_meta.Value.language_code);
                Assert.AreEqual(info.xml.NumSets, actual.bd_meta.Value.di_num_sets);
                Assert.AreEqual(info.xml.SetNumber, actual.bd_meta.Value.di_set_number);
            }

            // Titles
            Assert.AreEqual(info.Titles.Count, actual.Titles.Count, "Number of titles did not match.");
            foreach(var titleTuple in info.Titles.Zip(actual.Titles))
            {
                Assert.AreEqual(titleTuple.First.Title, (int)titleTuple.Second.json_ix);
                Assert.AreEqual(titleTuple.First.Playlist, (int)titleTuple.Second.playlist);
                Assert.AreEqual(titleTuple.First.Length, titleTuple.Second.length);
                Assert.AreEqual(titleTuple.First.Msecs, titleTuple.Second.duration / 900);
                Assert.AreEqual(titleTuple.First.Angles, titleTuple.Second.angles);
                Assert.AreEqual(titleTuple.First.Blocks, titleTuple.Second.blocks);
                Assert.AreEqual(titleTuple.First.FileSize, titleTuple.Second.size);

                // Video streams
                Assert.AreEqual(titleTuple.First.Videos.Count, titleTuple.Second.VideoStreams.Count, "Number of video streams did not match.");
                foreach(var videoTuple in titleTuple.First.Videos.Zip(titleTuple.Second.VideoStreams))
                {
                    Assert.AreEqual(videoTuple.First.Track, videoTuple.Second.video_stream_number);
                    Assert.AreEqual(videoTuple.First.Stream, $"0x{videoTuple.Second.pid:X}");
                    Assert.AreEqual(videoTuple.First.Format, videoTuple.Second.format);
                    Assert.AreEqual(videoTuple.First.AspectRatio, videoTuple.Second.aspect_ratio);
                    Assert.AreEqual(videoTuple.First.FrameRate, videoTuple.Second.framerate);
                    Assert.AreEqual(videoTuple.First.Codec, videoTuple.Second.codec);
                    Assert.AreEqual(videoTuple.First.CodecName, videoTuple.Second.codec_name);
                }

                // Audio streams
                Assert.AreEqual(titleTuple.First.Audios.Count, titleTuple.Second.AudioStreams.Count, "Number of audio streams did not match.");
                foreach (var audioTuple in titleTuple.First.Audios.Zip(titleTuple.Second.AudioStreams))
                {
                    Assert.AreEqual(audioTuple.First.Track, audioTuple.Second.audio_stream_number);
                    Assert.AreEqual(audioTuple.First.Stream, $"0x{audioTuple.Second.pid:X}");
                    Assert.AreEqual(audioTuple.First.Language, audioTuple.Second.lang);
                    Assert.AreEqual(audioTuple.First.Codec, audioTuple.Second.codec);
                    Assert.AreEqual(audioTuple.First.CodecName, audioTuple.Second.codec_name);
                    Assert.AreEqual(audioTuple.First.Format, audioTuple.Second.format);
                    Assert.AreEqual(audioTuple.First.Rate.ToString(), audioTuple.Second.rate);
                }

                // Subtitles
                Assert.AreEqual(titleTuple.First.Subtitles.Count, titleTuple.Second.SubtitleStreams.Count, "Number of subtitle streams did not match.");
                foreach(var subTuple in titleTuple.First.Subtitles.Zip(titleTuple.Second.SubtitleStreams))
                {
                    Assert.AreEqual(subTuple.First.Track, subTuple.Second.pg_stream_number);
                    Assert.AreEqual(subTuple.First.Stream, $"0x{subTuple.Second.pid:X}");
                    Assert.AreEqual(subTuple.First.Language, subTuple.Second.lang);
                }

                // Chapters
                Assert.AreEqual(titleTuple.First.Chapters.Count, titleTuple.Second.Chapters.Count, "Number of chapters did not match.");
                foreach(var chapTuple in titleTuple.First.Chapters.Zip(titleTuple.Second.Chapters))
                {
                    Assert.AreEqual(chapTuple.First.Chapter, (int)chapTuple.Second.chapter_number);
                    Assert.AreEqual(chapTuple.First.StartTime, chapTuple.Second.start_time);
                    Assert.AreEqual(chapTuple.First.Length, chapTuple.Second.length);
                    Assert.AreEqual(chapTuple.First.Start, chapTuple.Second.start / 900);
                    Assert.AreEqual(chapTuple.First.Duration, chapTuple.Second.duration / 900);
                    Assert.AreEqual(chapTuple.First.Blocks, chapTuple.Second.blocks);
                    Assert.AreEqual(chapTuple.First.FileSize, chapTuple.Second.size);
                }
            }

        }


        [TestMethod]
        public void TestMinecraftMovie()
        {
            TestMovie("A_MINECRAFT_MOVIE", "a_minecraft_movie.json");
        }

        [TestMethod]
        public void TestDragonMovie()
        {
            TestMovie("HOW_TO_TRAIN_YOUR_DRAGON_2", "how_to_train_your_dragon_2.json");
        }

        [TestMethod]
        public void TestRedVsBlueMovie()
        {
            TestMovie("Red vs Blue Season 1 279270_1", "red_vs_blue.json");
        }
    }
}