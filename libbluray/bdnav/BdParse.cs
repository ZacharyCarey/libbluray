using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.bdnav
{
    internal static class BdParse
    {

        public const uint BD_STREAM_TYPE_VIDEO_MPEG1 = 0x01;
        public const uint BD_STREAM_TYPE_VIDEO_MPEG2 = 0x02;
        public const uint BD_STREAM_TYPE_AUDIO_MPEG1 = 0x03;
        public const uint BD_STREAM_TYPE_AUDIO_MPEG2 = 0x04;
        public const uint BD_STREAM_TYPE_AUDIO_LPCM = 0x80;
        public const uint BD_STREAM_TYPE_AUDIO_AC3 = 0x81;
        public const uint BD_STREAM_TYPE_AUDIO_DTS = 0x82;
        public const uint BD_STREAM_TYPE_AUDIO_TRUHD = 0x83;
        public const uint BD_STREAM_TYPE_AUDIO_AC3PLUS = 0x84;
        public const uint BD_STREAM_TYPE_AUDIO_DTSHD = 0x85;
        public const uint BD_STREAM_TYPE_AUDIO_DTSHD_MASTER = 0x86;
        public const uint BD_STREAM_TYPE_VIDEO_VC1 = 0xea;
        public const uint BD_STREAM_TYPE_VIDEO_H264 = 0x1b;
        public const uint BD_STREAM_TYPE_VIDEO_HEVC = 0x24;
        public const uint BD_STREAM_TYPE_SUB_PG = 0x90;
        public const uint BD_STREAM_TYPE_SUB_IG = 0x91;
        public const uint BD_STREAM_TYPE_SUB_TEXT = 0x92;

        /// <summary>
        /// ITU-R BT.601-5
        /// </summary>
        public const uint BD_VIDEO_FORMAT_480I = 1;

        /// <summary>
        /// ITU-R BT.601-4
        /// </summary>
        public const uint BD_VIDEO_FORMAT_576I = 2;

        /// <summary>
        /// SMPTE 293M
        /// </summary>
        public const uint BD_VIDEO_FORMAT_480P = 3;

        /// <summary>
        /// SMPTE 274M
        /// </summary>
        public const uint BD_VIDEO_FORMAT_1080I = 4;

        /// <summary>
        /// SMPTE 296M
        /// </summary>
        public const uint BD_VIDEO_FORMAT_720P = 5;

        /// <summary>
        /// SMPTE 274M
        /// </summary>
        public const uint BD_VIDEO_FORMAT_1080P = 6;

        /// <summary>
        /// ITU-R BT.1358
        /// </summary>
        public const uint BD_VIDEO_FORMAT_576P = 7;  
        public const uint BD_VIDEO_FORMAT_2160P = 8;

        /// <summary>
        /// 23.976
        /// </summary>
        public const uint BD_VIDEO_RATE_24000_1001 = 1;
        public const uint BD_VIDEO_RATE_24 = 2;
        public const uint BD_VIDEO_RATE_25 = 3;

        /// <summary>
        /// 29.97
        /// </summary>
        public const uint BD_VIDEO_RATE_30000_1001 = 4;  
        public const uint BD_VIDEO_RATE_50 = 6;

        /// <summary>
        /// 59.94
        /// </summary>
        public const uint BD_VIDEO_RATE_60000_1001 = 7; 

        public const uint BD_ASPECT_RATIO_4_3 = 2;
        public const uint BD_ASPECT_RATIO_16_9 = 3;

        public const uint BD_COLOR_SPACE_BT_709 = 1;
        public const uint BD_COLOR_SPACE_BT_2020 = 2;

        public const uint BD_AUDIO_FORMAT_MONO = 1;
        public const uint BD_AUDIO_FORMAT_STEREO = 3;
        public const uint BD_AUDIO_FORMAT_MULTI_CHAN = 6;

        /// <summary>
        /// Stereo ac3/dts,  
        /// multi mlp/dts-hd
        /// </summary>
        public const uint BD_AUDIO_FORMAT_COMBO = 12;  

        public const uint BD_AUDIO_RATE_48 = 1;
        public const uint BD_AUDIO_RATE_96 = 4;
        public const uint BD_AUDIO_RATE_192 = 5;

        /// <summary>
        /// 48 or 96 ac3/dts
        /// 192 mpl/dts-hd
        /// </summary>
        public const uint BD_AUDIO_RATE_192_COMBO = 12;

        /// <summary>
        /// 48 ac3/dts
        /// 96 mpl/dts-hd
        /// </summary>
        public const uint BD_AUDIO_RATE_96_COMBO = 14;  
                                                        

        public const uint BD_TEXT_CHAR_CODE_UTF8 = 0x01;
        public const uint BD_TEXT_CHAR_CODE_UTF16BE = 0x02;
        public const uint BD_TEXT_CHAR_CODE_SHIFT_JIS = 0x03;
        public const uint BD_TEXT_CHAR_CODE_EUC_KR = 0x04;
        public const uint BD_TEXT_CHAR_CODE_GB18030_20001 = 0x05;
        public const uint BD_TEXT_CHAR_CODE_CN_GB = 0x06;
        public const uint BD_TEXT_CHAR_CODE_BIG5 = 0x07;

    }
}
