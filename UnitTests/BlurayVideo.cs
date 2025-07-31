using libbluray;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.BlurayInfo
{
    public class bluray_video
    {
        public byte video_stream_number;
        public ushort pid;

        public string codec = "'";
        public string codec_name = "";
        public string format = "";
        public double framerate;
        public string aspect_ratio = "";
    }

    public partial class BlurayInfo
    {
        public const int BLURAY_INFO_VIDEO_CODEC_STRLEN = 6;
        public const int BLURAY_INFO_VIDEO_CODEC_NAME_STRLEN = 7;
        public const int BLURAY_INFO_VIDEO_FORMAT_STRLEN = 7;
        public const int BLURAY_INFO_VIDEO_ASPECT_RATIO_STRLEN = 7;

        public static void bluray_video_codec(out string str, bd_stream_type_e coding_type)
        {
            str = "";

            switch (coding_type)
            {

                case bd_stream_type_e.BLURAY_STREAM_TYPE_VIDEO_H264:
                    str = "h264";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_VIDEO_HEVC:
                    str = "hevc";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_VIDEO_MPEG1:
                    str = "mpeg1";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_VIDEO_MPEG2:
                    str = "mpeg2";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_VIDEO_VC1:
                    str = "vc1";
                    break;

            }

        }

        public static void bluray_video_codec_name(out string str, bd_stream_type_e coding_type)
        {

            str = "";

            switch (coding_type)
            {

                case bd_stream_type_e.BLURAY_STREAM_TYPE_VIDEO_H264:
                    str = "H264";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_VIDEO_HEVC:
                    str = "HEVC";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_VIDEO_MPEG1:
                    str = "MPEG-1";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_VIDEO_MPEG2:
                    str = "MPEG-2";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_VIDEO_VC1:
                    str = "VC-1";
                    break;

            }

        }

        public static void bluray_video_format(out string str, bd_video_format_e format)
        {

            str = "";

            switch (format)
            {

                case bd_video_format_e.BLURAY_VIDEO_FORMAT_480I:
                    str = "480i";
                    break;

                case bd_video_format_e.BLURAY_VIDEO_FORMAT_480P:
                    str = "480p";
                    break;

                case bd_video_format_e.BLURAY_VIDEO_FORMAT_576I:
                    str = "576i";
                    break;

                case bd_video_format_e.BLURAY_VIDEO_FORMAT_576P:
                    str = "576p";
                    break;

                case bd_video_format_e.BLURAY_VIDEO_FORMAT_720P:
                    str = "720p";
                    break;

                case bd_video_format_e.BLURAY_VIDEO_FORMAT_1080I:
                    str = "1080i";
                    break;

                case bd_video_format_e.BLURAY_VIDEO_FORMAT_1080P:
                    str = "1080p";
                    break;

                case bd_video_format_e.BLURAY_VIDEO_FORMAT_2160P:
                    str = "2160p";
                    break;

            }

        }

        public static double bluray_video_framerate(bd_video_rate_e rate)
        {

            switch (rate)
            {

                case bd_video_rate_e.BLURAY_VIDEO_RATE_24000_1001:
                    return 23.97;

                case bd_video_rate_e.BLURAY_VIDEO_RATE_24:
                    return 24;

                case bd_video_rate_e.BLURAY_VIDEO_RATE_25:
                    return 25;

                case bd_video_rate_e.BLURAY_VIDEO_RATE_30000_1001:
                    return 29.97;

                case bd_video_rate_e.BLURAY_VIDEO_RATE_50:
                    return 50;

                case bd_video_rate_e.BLURAY_VIDEO_RATE_60000_1001:
                    return 59.94;

                default:
                    return 0;

            }

        }

        public static void bluray_video_aspect_ratio(out string str, bd_video_aspect_e aspect)
        {

            str = "";

            switch (aspect)
            {

                case bd_video_aspect_e.BLURAY_ASPECT_RATIO_4_3:
                    str = "4:3";
                    break;

                case bd_video_aspect_e.BLURAY_ASPECT_RATIO_16_9:
                    str = "16:9";
                    break;

            }

        }
    }
}
