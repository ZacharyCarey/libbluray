using libbluray;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.BlurayInfo
{
    public class bluray_audio
    {
        public byte audio_stream_number;
        public ushort pid;

        public string lang = "";
        public string codec = "";
        public string codec_name = "";
        public string format = "";
        public string rate = "";
    };

    public partial class BlurayInfo
    {
        public const int BLURAY_INFO_AUDIO_CODEC_STRLEN = 9;
        public const int BLURAY_INFO_AUDIO_CODEC_NAME_STRLEN = 19;
        public const int BLURAY_INFO_AUDIO_FORMAT_STRLEN = 11;
        public const int BLURAY_INFO_AUDIO_RATE_STRLEN = 10;
        public const int BLURAY_INFO_AUDIO_LANG_STRLEN = 4;

        public static void bluray_audio_lang(out string str, string lang)
        {
            str = lang;
        }

        public static void bluray_audio_codec(out string str, bd_stream_type_e coding_type)
        {
            str = "";
            switch (coding_type)
            {

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_MPEG1:
                    str = "mpeg1";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_MPEG2:
                    str = "mpeg2";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_LPCM:
                    str = "lpcm";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_AC3:
                    str = "ac3";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_DTS:
                    str = "dts";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_TRUHD:
                    str = "truhd";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_AC3PLUS:
                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_AC3PLUS_SECONDARY:
                    str = "ac3plus";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_DTSHD:
                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_DTSHD_SECONDARY:
                    str = "dtshd";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_DTSHD_MASTER:
                    str = "dtshd-ma";
                    break;

            }

        }

        public static void bluray_audio_codec_name(out string str, bd_stream_type_e coding_type)
        {
            str = "";
            switch (coding_type)
            {

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_MPEG1:
                    str = "MPEG-1";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_MPEG2:
                    str = "MPEG-2";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_LPCM:
                    str = "LPCM";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_AC3:
                    str = "Dolby Digital";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_DTS:
                    str = "DTS";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_TRUHD:
                    str = "Dolby TrueHD";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_AC3PLUS:
                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_AC3PLUS_SECONDARY:
                    str = "Dolby Digital Plus";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_DTSHD:
                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_DTSHD_SECONDARY:
                    str = "DTS-HD";
                    break;

                case bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_DTSHD_MASTER:
                    str = "DTS-HD Master";
                    break;

            }

        }

        public static bool bluray_audio_secondary_stream(bd_stream_type_e coding_type)
        {

            if (coding_type == bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_AC3PLUS_SECONDARY || coding_type == bd_stream_type_e.BLURAY_STREAM_TYPE_AUDIO_DTSHD_SECONDARY)
                return true;
            else
                return false;

        }

        public static void bluray_audio_format(out string str, bd_audio_format_e format)
        {
            str = "";
            switch (format)
            {

                case bd_audio_format_e.BLURAY_AUDIO_FORMAT_MONO:
                    str = "mono";
                    break;

                case bd_audio_format_e.BLURAY_AUDIO_FORMAT_STEREO:
                    str = "stereo";
                    break;

                case bd_audio_format_e.BLURAY_AUDIO_FORMAT_MULTI_CHAN:
                    str = "multi_chan";
                    break;

                case bd_audio_format_e.BLURAY_AUDIO_FORMAT_COMBO:
                    str = "combo";
                    break;

            }

        }

        public static void bluray_audio_rate(out string str, bd_audio_rate_e rate)
        {
            str = "";
            switch (rate)
            {

                case bd_audio_rate_e.BLURAY_AUDIO_RATE_48:
                    str = "48";
                    break;

                case bd_audio_rate_e.BLURAY_AUDIO_RATE_96:
                    str = "96";
                    break;

                case bd_audio_rate_e.BLURAY_AUDIO_RATE_192:
                    str = "192";
                    break;

                case bd_audio_rate_e.BLURAY_AUDIO_RATE_192_COMBO:
                    str = "192_combo";
                    break;

                case bd_audio_rate_e.BLURAY_AUDIO_RATE_96_COMBO:
                    str = "96_combo";
                    break;

            }

        }
    }
}
