using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{
    internal struct M2TS_DEMUX
    {
        public UInt16 pid;
        public UInt32 pes_length;
        public Ref<PES_BUFFER> buf = new();

        public M2TS_DEMUX() { }
    }

    /// <summary>
    /// simple single-pid demuxer for BDAV m2ts.
    /// </summary>
    internal static class M2tsDemux
    {
        /*
 *
 */

        static Ref<PES_BUFFER> _flush(Ref<M2TS_DEMUX> p)
        {
            Ref<PES_BUFFER> result = Ref<PES_BUFFER>.Null;

            result = p.Value.buf;
            p.Value.buf = Ref<PES_BUFFER>.Null;

            return result;
        }

        internal static void m2ts_demux_reset(Ref<M2TS_DEMUX> p)
        {
            if (p)
            {
                Ref<PES_BUFFER> buf = _flush(p);
                PesBuffer.pes_buffer_free(ref buf);
            }
        }

        /*
         *
         */

        internal static Ref<M2TS_DEMUX> m2ts_demux_init(UInt16 pid)
        {
            Ref<M2TS_DEMUX> p = Ref<M2TS_DEMUX>.Allocate();

            if (p)
            {
                p.Value.pid = pid;
            }

            return p;
        }

        internal static void m2ts_demux_free(ref Ref<M2TS_DEMUX> p)
        {
            if (p)
            {
                m2ts_demux_reset(p);
                p.Free();
            }
        }

        /*
         *
         */

        static int _realloc(Ref<PES_BUFFER> p, UInt64 size)
        {
            Ref<byte> tmp = p.Value.buf.Reallocate(size);

            p.Value.size = (uint)size;
            p.Value.buf = tmp;

            return 0;
        }

        static int _add_ts(Ref<PES_BUFFER> p, Ref<byte> buf, uint len)
        {
            // realloc
            if (p.Value.size < p.Value.len + len)
            {
                if (_realloc(p, p.Value.size * 2) < 0)
                {
                    return -1;
                }
            }

            // append
            buf.AsSpan().Slice(0, (int)len).CopyTo((p.Value.buf + p.Value.len).AsSpan());
            p.Value.len += len;

            return 0;
        }

        /*
         * Parsing
         */

        static Int64 _parse_timestamp(Ref<byte> p)
        {
            Int64 ts;
            ts = ((Int64)(p[0] & 0x0E)) << 29;
            ts |= (Int64)p[1] << 22;
            ts |= (Int64)(p[2] & 0xFE) << 14;
            ts |= (Int64)p[3] << 7;
            ts |= (Int64)(p[4] & 0xFE) >> 1;
            return ts;
        }

        static int _parse_pes(Ref<PES_BUFFER> p, Ref<byte> buf, uint len)
        {
            int result = 0;

            if (len < 6)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"invalid BDAV TS (PES header not in single TS packet)");
                return -1;
            }
            if (buf[0] != 0 || buf[1] != 0 || buf[2] != 1)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, "invalid PES header (00 00 01)");
                return -1;
            }

            // Parse PES header
            uint pes_pid = buf[3];
            uint pes_length = (uint)buf[4] << 8 | buf[5];
            uint hdr_len = 6;
/*
# ifdef __COVERITY__
            // Coverity 
            if (pes_length >= 0xffff)
                pes_length = 0xffff;
#endif
            */
            if (pes_pid != 0xbf)
            {

                if (len < 9)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"invalid BDAV TS (PES header not in single TS packet)");
                    return -1;
                }

                uint pts_exists = (uint)buf[7] & 0x80;
                uint dts_exists = (uint)buf[7] & 0x40;
                hdr_len += (uint)buf[8] + 3;

                if (len < hdr_len)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"invalid BDAV TS (PES header not in single TS packet)");
                    return -1;
                }

                if (pts_exists != 0)
                {
                    p.Value.pts = _parse_timestamp(buf + 9);
                }
                if (dts_exists != 0)
                {
                    p.Value.dts = _parse_timestamp(buf + 14);
                }
            }

            result = (int)(pes_length + 6 - hdr_len);

            if (_realloc(p, (ulong)Util.BD_MAX(result, 0x100)) < 0)
            {
                return -1;
            }

            p.Value.len = len - hdr_len;
            (buf + hdr_len).AsSpan().Slice(0, (int)p.Value.len).CopyTo(p.Value.buf.AsSpan());

            return result;
        }


        /// <summary>
        /// Demux aligned unit (mpeg-ts + pes).
        /// input:  aligned unit(6144 bytes). NULL to flush demuxer buffer.
        /// output: PES payload
        /// Flush demuxer internal cache if block == NULL.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        internal static Ref<PES_BUFFER> m2ts_demux(Ref<M2TS_DEMUX> p, Ref<byte> buf)
        {
            Ref<byte> end = buf + 6144;
            Ref<PES_BUFFER> result = Ref<PES_BUFFER>.Null;

            if (!buf)
            {
                return _flush(p);
            }

            for (; buf < end; buf += 192)
            {

                uint tp_error = (uint)buf[4 + 1] & 0x80;
                uint pusi = (uint)buf[4 + 1] & 0x40;
                UInt16 pid = (UInt16)((((uint)buf[4 + 1] & 0x1f) << 8) | buf[4 + 2]);
                uint payload_exists = (uint)buf[4 + 3] & 0x10;
                int payload_offset = ((buf[4 + 3] & 0x20) != 0) ? buf[4 + 4] + 5 : 4;

                if (buf[4] != 0x47)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"missing sync byte. scrambled data ?");
                    return Ref<PES_BUFFER>.Null;
                }
                if (pid != p.Value.pid)
                {
                    M2tsFilter.M2TS_TRACE($"skipping packet (pid {pid})");
                    continue;
                }
                if (tp_error != 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"skipping packet (transport error)");
                    continue;
                }
                if (payload_exists == 0)
                {
                    M2tsFilter.M2TS_TRACE("skipping packet (no payload)");
                    continue;
                }
                if (payload_offset >= 188)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"skipping packet (invalid payload start address)");
                    continue;
                }

                if (pusi != 0)
                {
                    if (p.Value.buf)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"PES length mismatch: have {p.Value.buf.Value.len}, expected {p.Value.pes_length}");
                        PesBuffer.pes_buffer_free(ref p.Value.buf);
                    }
                    p.Value.buf = PesBuffer.pes_buffer_alloc();
                    if (!p.Value.buf)
                    {
                        continue;
                    }
                    int r = _parse_pes(p.Value.buf, buf + 4 + payload_offset, (uint)(188 - payload_offset));
                    if (r < 0)
                    {
                        PesBuffer.pes_buffer_free(ref p.Value.buf);
                        continue;
                    }
                    p.Value.pes_length = (uint)r;

                }
                else
                {

                    if (!p.Value.buf)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"skipping packet (no pusi seen)");
                        continue;
                    }

                    if (_add_ts(p.Value.buf, buf + 4 + payload_offset, (uint)(188 - payload_offset)) < 0)
                    {
                        PesBuffer.pes_buffer_free(ref p.Value.buf);
                        continue;
                    }
                }

                if (p.Value.buf.Value.len == p.Value.pes_length)
                {
                    M2tsFilter.M2TS_TRACE($"PES complete ({p.Value.pes_length} bytes)");
                    PesBuffer.pes_buffer_append(ref result, p.Value.buf);
                    p.Value.buf = Ref<PES_BUFFER>.Null;
                }
            }

            return result;
        }
    }
}
