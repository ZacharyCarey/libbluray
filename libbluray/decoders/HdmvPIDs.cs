using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{
    public static class HdmvPIDs
    {
        /*
         * HDMV transport stream PIDs
         */

        public const uint HDMV_PID_PAT = 0;
        public const uint HDMV_PID_PMT = 0x0100;
        public const uint HDMV_PID_PCR = 0x1001;

        /* primary streams */

        public const uint HDMV_PID_VIDEO = 0x1011;
        public const uint HDMV_PID_VIDEO_SS = 0x1012;

        public const uint HDMV_PID_AUDIO_FIRST = 0x1100;
        public const uint HDMV_PID_AUDIO_LAST = 0x111f;

        /* graphics streams */

        public const uint HDMV_PID_PG_FIRST = 0x1200;
        public const uint HDMV_PID_PG_LAST = 0x121f;

        public const uint HDMV_PID_PG_B_FIRST = 0x1220;  /* base view */
        public const uint HDMV_PID_PG_B_LAST = 0x123f;
        public const uint HDMV_PID_PG_E_FIRST = 0x1240;  /* enhanced view */
        public const uint HDMV_PID_PG_E_LAST = 0x125f;

        public const uint HDMV_PID_IG_FIRST = 0x1400;
        public const uint HDMV_PID_IG_LAST = 0x141f;

        public const uint HDMV_PID_TEXTST = 0x1800;

        /* secondary streams */

        public const uint HDMV_PID_SEC_AUDIO_FIRST = 0x1a00;
        public const uint HDMV_PID_SEC_AUDIO_LAST = 0x1a1f;

        public const uint HDMV_PID_SEC_VIDEO_FIRST = 0x1b00;
        public const uint HDMV_PID_SEC_VIDEO_LAST = 0x1b1f;

        /*
         *
         */

        public static bool IS_HDMV_PID_PG(ushort pid) => ((pid) >= HDMV_PID_PG_FIRST && (pid) <= HDMV_PID_PG_LAST);
        public static bool IS_HDMV_PID_IG(ushort pid) => ((pid) >= HDMV_PID_IG_FIRST && (pid) <= HDMV_PID_IG_LAST);
        public static bool IS_HDMV_PID_TEXTST(ushort pid) => ((pid) == HDMV_PID_TEXTST);

        /*
         * Extract PID from HDMV MPEG-TS packet
         */

        public static ushort TS_PID(Ref<byte> buf) => (ushort)(((buf[4 + 1] & 0x1fu) << 8) | buf[4 + 2]);
    }
}
