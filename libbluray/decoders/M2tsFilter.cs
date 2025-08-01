using libbluray.decoders;
using libbluray.disc;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{
    public struct M2TS_FILTER
    {
        public Ref<UInt16> wipe_pid = new();
        public Ref<UInt16> pass_pid = new();

        public Int64 in_pts;
        public Int64 out_pts;
        public UInt32 pat_packets; /* how many packets to search for PAT (seeked pat_packets packets before the actual seek point) */
        public byte pat_seen;

        public M2TS_FILTER() { }
    }

    public static class M2tsFilter
    {
        internal static void M2TS_TRACE(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Logging.bd_debug(DebugMaskEnum.DBG_STREAM, msg, file, line);
        }

        internal static Ref<M2TS_FILTER> m2ts_filter_init(Int64 in_pts, Int64 out_pts,
                              uint num_video, uint num_audio,
                              uint num_ig, uint num_pg)
        {
            Ref<M2TS_FILTER> p = Ref<M2TS_FILTER>.Allocate();

            if (p)
            {
                uint ii, npid;
                Ref<UInt16> pid;

                p.Value.in_pts = in_pts;
                p.Value.out_pts = out_pts;
                p.Value.wipe_pid = Ref<UInt16>.Allocate(num_audio + num_video + num_ig + num_pg + 1);
                p.Value.pass_pid = Ref<UInt16>.Allocate(num_audio + num_video + num_ig + num_pg + 1);
                if (!p.Value.pass_pid || !p.Value.wipe_pid)
                {
                    m2ts_filter_close(ref p);
                    return Ref<M2TS_FILTER>.Null;
                }

                pid = (in_pts >= 0) ? p.Value.wipe_pid : p.Value.pass_pid;

                for (ii = 0, npid = 0; ii < num_video; ii++)
                {
                    pid[npid++] = (ushort)(HdmvPIDs.HDMV_PID_VIDEO + ii);
                }
                for (ii = 0; ii < num_audio; ii++)
                {
                    pid[npid++] = (ushort)(HdmvPIDs.HDMV_PID_AUDIO_FIRST + ii);
                }
                for (ii = 0; ii < num_ig; ii++)
                {
                    pid[npid++] = (ushort)(HdmvPIDs.HDMV_PID_IG_FIRST + ii);
                }
                for (ii = 0; ii < num_pg; ii++)
                {
                    pid[npid++] = (ushort)(HdmvPIDs.HDMV_PID_PG_FIRST + ii);
                }
            }

            return p;
        }

        internal static void m2ts_filter_close(ref Ref<M2TS_FILTER> p)
        {
            if (p)
            {
                p.Value.wipe_pid.Free();
                p.Value.pass_pid.Free();
                p.Free();
            }
        }

        /*
         *
         */
        /*
        #define DUMPLIST(msg,list)                          \
            {                                               \
                uint ii = 0;                            \
                fprintf(stderr, "list " msg " : ");         \
                for (ii = 0; list[ii]; ii++) {              \
                    fprintf(stderr, " 0x%04x", list[ii]);   \
                }                                           \
                fprintf(stderr, "\n");                      \
            }
        */
        static bool _pid_in_list(Ref<UInt16> list, UInt16 pid)
        {
            for (; list.Value != 0 && list.Value <= pid; list++)
            {
                if (list.Value == pid)
                {
                    return true;
                }
            }
            return false;
        }

        static void _remove_pid(Ref<UInt16> list, UInt16 pid)
        {
            for (; list.Value != 0 && list.Value != pid; list++) ;

            for (; list.Value != 0; list++)
            {
                list[0] = list[1];
            }
        }

        static void _add_pid(Ref<UInt16> list, UInt16 pid)
        {
            for (; list.Value != 0 && list.Value < pid; list++) ;

            for (; list.Value != 0; list++)
            {
                UInt16 tmp = list.Value;
                list.Value = pid;
                pid = tmp;
            }
            list.Value = pid;
        }

        static Int64 _parse_timestamp(Ref<byte> p)
        {
            Int64 ts;
            ts = ((Int64)(p[0] & 0x0E)) << 29;
            ts |= ((Int64)p[1]) << 22;
            ts |= ((Int64)(p[2] & 0xFE)) << 14;
            ts |= ((Int64)p[3]) << 7;
            ts |= ((Int64)(p[4] & 0xFE)) >> 1;
            return ts;
        }

        static Int64 _es_timestamp(Ref<byte> buf, uint len)
        {
            if (buf[0] != 0 || buf[1] != 0 || buf[2] != 1)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"invalid BDAV TS");
                return -1;
            }

            if (len < 9)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"invalid BDAV TS (no payload ?)");
                return -1;
            }

            /* Parse PES header */
            uint pes_pid = buf[3];
            if (pes_pid != 0xbf)
            {

                uint pts_exists = (uint)(buf[7] & 0x80);
                if (pts_exists != 0)
                {
                    Int64 pts = _parse_timestamp(buf + 9);

                    return pts;
                }
            }

            return -1;
        }

        internal static void m2ts_filter_seek(Ref<M2TS_FILTER> p, UInt32 pat_packets, Int64 in_pts)
        {
            M2TS_TRACE("seek notify\n");

            /* move all pids to wipe list */
            Ref<UInt16> pid = p.Value.pass_pid;
            while (pid.Value != 0)
            {
                _add_pid(p.Value.wipe_pid, pid.Value);
                pid.Value = 0;
                pid++;
            }

            p.Value.in_pts = in_pts;
            p.Value.pat_seen = 0;
            p.Value.pat_packets = pat_packets;
        }

        static int _filter_es_pts(Ref<M2TS_FILTER> p, Ref<byte> buf, UInt16 pid)
        {
            uint tp_error = (uint)(buf[4 + 1] & 0x80);
            uint payload_exists = (uint)(buf[4 + 3] & 0x10);
            int payload_offset = ((buf[4 + 3] & 0x20) != 0) ? buf[4 + 4] + 5 : 4;

            if (buf[4] != 0x47)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, $"missing sync byte. scrambled data ? Filtering aborted.");
                return -1;
            }
            if (tp_error != 0 || payload_exists == 0 || payload_offset >= 188)
            {
                M2TS_TRACE("skipping packet (no payload)\n");
                return 0;
            }

            if (_pid_in_list(p.Value.wipe_pid, pid))
            {

                Int64 pts = _es_timestamp(buf + 4 + payload_offset, (uint)(188 - payload_offset));
                if (pts >= p.Value.in_pts && (p.Value.out_pts < 0 || pts <= p.Value.out_pts))
                {
                    M2TS_TRACE($"Pid 0x%{pid:x4} pts {pts} passed IN timestamp {p.Value.in_pts} (pts {pts})");
                    _remove_pid(p.Value.wipe_pid, pid);
                    _add_pid(p.Value.pass_pid, pid);

                }
                else
                {
                    M2TS_TRACE($"Pid 0x{pid:x4} pts {pts} outside of clip ({p.Value.in_pts}-{p.Value.out_pts} -> keep wiping out");
                }
            }
            if (p.Value.out_pts >= 0)
            {
                /*
                 * Note: we can't compare against in_pts here (after passing it once):
                 * PG / IG streams can have timestamps before in_time (except for composition segments), and those are valid.
                 */
                if (_pid_in_list(p.Value.pass_pid, pid))
                {

                    Int64 pts = _es_timestamp(buf + 4 + payload_offset, (uint)(188 - payload_offset));
                    if (pts >= p.Value.out_pts)
                    {
                        /*
                         * audio/video streams are cutted after out_time (unit with pts==out_time is included in the clip).
                         * PG/IG streams are cutted before out_time (unit with pts==out_time is dropped out).
                         */
                        if (pts > p.Value.out_pts ||
                            HdmvPIDs.IS_HDMV_PID_PG(pid) ||
                            HdmvPIDs.IS_HDMV_PID_IG(pid))
                        {
                            M2TS_TRACE($"Pid 0x{pid:x4} passed OUT timestamp {p.Value.out_pts} (pts {pts}) -> start wiping");
                            _remove_pid(p.Value.pass_pid, pid);
                            _add_pid(p.Value.wipe_pid, pid);
                        }
                    }
                }
            }

            return 0;
        }

        static void _wipe_packet(Ref<byte> p)
        {
            /* set pid to 0x1fff (padding) */
            p[4 + 2] = 0xff;
            p[4 + 1] |= 0x1f;
        }

        internal static int m2ts_filter(Ref<M2TS_FILTER> p, Ref<byte> buf)
        {
            Ref<byte> end = buf + 6144;
            int result = 0;

            for (; buf < end; buf += 192)
            {

                UInt16 pid = (UInt16)((((uint)buf[4 + 1] & 0x1f) << 8) | buf[4 + 2]);
                if (pid == HdmvPIDs.HDMV_PID_PAT)
                {
                    p.Value.pat_seen = 1;
                    p.Value.pat_packets = 0;
                    continue;
                }
                if (p.Value.pat_packets != 0)
                {
                    p.Value.pat_packets--;
                    if (p.Value.pat_seen == 0)
                    {
                        M2TS_TRACE($"Wiping pid 0x{pid:c4} (inside seek buffer, no PAT)");
                        _wipe_packet(buf);
                        continue;
                    }
                    M2TS_TRACE($"NOT Wiping pid 0x{pid:x4} (inside seek buffer, PAT seen)");
                }
                if (pid < HdmvPIDs.HDMV_PID_VIDEO)
                {
                    /* pass PMT, PCR, SIT */
                    /*M2TS_TRACE("NOT Wiping pid 0x%04x (< 0x1011)\n", pid);*/
                    continue;
                }
#if false
        /* no PAT yet ? */
        if (!p.Value.pat_seen) {
            /* Wipe packet (pid .Value. padding stream) */
            M2TS_TRACE("Wiping pid 0x%04x before PAT\n", pid);
            _wipe_packet(buf);
            continue;
        }
#endif
                /* payload start indicator ? check ES timestamp */
                uint pusi = (uint)(buf[4 + 1] & 0x40);
                if (pusi != 0)
                {
                    if (_filter_es_pts(p, buf, pid) < 0)
                        return -1;
                }

                if (_pid_in_list(p.Value.wipe_pid, pid))
                {
                    /* Wipe packet (pid .Value. padding stream) */
                    M2TS_TRACE($"Wiping pid 0x{pid:x4}");
                    _wipe_packet(buf);
                }
            }

            /*result = !!(p.Value.after ? p.Value.wipe_pid[0] : p.Value.pass_pid[0]);*/

            return result;
        }
    }
}
