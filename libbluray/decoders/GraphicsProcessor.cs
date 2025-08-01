using libbluray.decoders;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{

    public struct GRAPHICS_PROCESSOR
    {
        public UInt16 pid;
        public Ref<M2TS_DEMUX> demux = new();
        public Ref<PES_BUFFER> queue = new();

        public GRAPHICS_PROCESSOR() { }
    }

    public struct PG_DISPLAY_SET
    {
        public Int64 valid_pts;
        public byte complete;     /* set complete: last decoded segment was END_OF_DISPLAY */
        public byte epoch_start;

        public uint num_palette;
        public uint num_object;
        public uint num_window;
        public uint num_dialog;    /* number of decoded dialog segments */
        public uint total_dialog;  /* total dialog segments in stream */

        public Ref<BD_PG_PALETTE> palette = new();
        public Ref<BD_PG_OBJECT> _object = new();
        public Ref<BD_PG_WINDOW> window = new();
        public Ref<BD_TEXTST_DIALOG_PRESENTATION> dialog = new();

        /* only one of the following segments can be present */
        public Ref<BD_IG_INTERACTIVE> ics = new();
        public Ref<BD_PG_COMPOSITION> pcs = new();
        public Ref<BD_TEXTST_DIALOG_STYLE> style = new();

        public byte decoding; /* internal flag: PCS/ICS decoded, but no end of presentation seen yet */

        public PG_DISPLAY_SET() { }
    }

    public enum pgs_segment_type_e
    {
        PGS_PALETTE = 0x14,
        PGS_OBJECT = 0x15,
        PGS_PG_COMPOSITION = 0x16,
        PGS_WINDOW = 0x17,
        PGS_IG_COMPOSITION = 0x18,
        PGS_END_OF_DISPLAY = 0x80,
        /* Text subtitles */
        TGS_DIALOG_STYLE = 0x81,
        TGS_DIALOG_PRESENTATION = 0x82,
    }

    public static class GraphicsProcessor
    {
        static void GP_TRACE(string msg) => throw new Exception(msg);

        /*
 * PG_DISPLAY_SET
 */

        static void _free_dialogs(Ref<PG_DISPLAY_SET> s)
        {
            uint ii;

            TextstDecode.textst_free_dialog_style(ref s.Value.style);

            for (ii = 0; ii < s.Value.num_dialog; ii++)
            {
                TextstDecode.textst_clean_dialog_presentation(s.Value.dialog.AtIndex(ii));
            }
            s.Value.dialog.Free();

            s.Value.num_dialog = 0;
            s.Value.total_dialog = 0;
        }

        internal static void pg_display_set_free(ref Ref<PG_DISPLAY_SET> s)
        {
            if (s)
            {
                uint ii;
                for (ii = 0; ii < s.Value.num_object; ii++)
                {
                    PgDecode.pg_clean_object(s.Value._object.AtIndex(ii));
                }
                IgDecode.ig_free_interactive(ref s.Value.ics);

                s.Value.window.Free();
                s.Value._object.Free();
                s.Value.palette.Free();

                _free_dialogs(s);

                s.Free();
            }
        }

        /*
         * segment handling
         */

        static Ref<PES_BUFFER> _find_segment_by_idv(Ref<PES_BUFFER> p,
                                                byte seg_type, uint idv_pos,
                                                Ref<byte> idv, uint idv_len)
        {
            while (p && (p.Value.buf[0] != seg_type || Util.memcmp(p.Value.buf + idv_pos, idv, idv_len) == 0))
            {
                p = p.Value.next;
            }
            return p;
        }

        static void _join_fragments(Ref<PES_BUFFER> p1, Ref<PES_BUFFER> p2, int data_pos)
        {
            uint new_len = p1.Value.len + p2.Value.len - (uint)data_pos;

            if (p1.Value.size < new_len)
            {
                Ref<byte> tmp;
                p1.Value.size = new_len + 1;
                tmp = p1.Value.buf.Reallocate(p1.Value.size);
                p1.Value.buf = tmp;
            }

            (p2.Value.buf + data_pos).AsSpan().Slice(0, (int)(p2.Value.len - (uint)data_pos)).CopyTo((p1.Value.buf + p1.Value.len).AsSpan());
            p1.Value.len = new_len;
            p2.Value.len = 0;
        }

        /* return 1 if segment is ready for decoding, 0 if more data is needed */
        static bool _join_segment_fragments(Ref<PES_BUFFER> p)
        {
            byte type;
            uint id_pos = 0, id_len = 3, sd_pos = 6, data_pos = 0;

            if (p.Value.len < 3)
            {
                return true;
            }

            /* check segment type */

            type = p.Value.buf[0];
            if (type == (byte)pgs_segment_type_e.PGS_OBJECT)
            {
                id_pos = 3;
                sd_pos = 6;
                data_pos = 7;
            }
            else if (type == (byte)pgs_segment_type_e.PGS_IG_COMPOSITION)
            {
                id_pos = 8;
                sd_pos = 11;
                data_pos = 12;
            }
            else
            {
                return true;
            }

            /* check sequence descriptor - is segment complete ? */

            Variable<BD_PG_SEQUENCE_DESCRIPTOR> sd = new();
            Variable<BITBUFFER> bb = new();
            bb.Value.bb_init(p.Value.buf + sd_pos, 3);
            PgDecode.pg_decode_sequence_descriptor(bb.Ref, sd.Ref);

            if (sd.Value.last_in_seq != 0)
            {
                return true;
            }
            if (sd.Value.first_in_seq == 0)
            {
                return true;
            }

            /* find next fragment(s) */

            Ref<PES_BUFFER> next;
            while ((next = _find_segment_by_idv(p.Value.next, p.Value.buf[0], id_pos, p.Value.buf + id_pos, id_len)) != null)
            {

                bb.Value.bb_init(next.Value.buf + sd_pos, 3);
                PgDecode.pg_decode_sequence_descriptor(bb.Ref, sd.Ref);

                _join_fragments(p, next, (int)data_pos);

                PesBuffer.pes_buffer_remove(ref p, next);

                if (sd.Value.last_in_seq != 0)
                {
                    /* set first + last in sequence descriptor */
                    p.Value.buf[sd_pos] = 0xff;
                    return true;
                }
            }

            /* do not delay decoding if there are other segments queued (missing fragment ?) */
            return !!p.Value.next;
        }

        /*
         * segment decoding
         */

        static bool _decode_wds(Ref<PG_DISPLAY_SET> s, Ref<BITBUFFER> bb, Ref<PES_BUFFER> p)
        {
            Variable<BD_PG_WINDOWS> w = new();

            if (s.Value.decoding == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"skipping orphan window definition segment");
                return false;
            }

            s.Value.num_window = 0;

            if (PgDecode.pg_decode_windows(bb, w.Ref))
            {
                s.Value.window.Free();
                s.Value.window = w.Value.window;
                s.Value.num_window = w.Value.num_windows;
                return true;
            }

            PgDecode.pg_clean_windows(w.Ref);

            return false;
        }

        static bool _decode_ods(Ref<PG_DISPLAY_SET> s, Ref<BITBUFFER> bb, Ref<PES_BUFFER> p)
        {
            if (s.Value.decoding == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"skipping orphan object definition segment");
                return false;
            }

            /* search for object to be updated */

            if (s.Value._object)
            {
                BITBUFFER bb_tmp = bb.Value;
                UInt16 id = bb_tmp.bb_read<ushort>(16);
                uint ii;

                for (ii = 0; ii < s.Value.num_object; ii++)
                {
                    if (s.Value._object[ii].id == id)
                    {
                        if (PgDecode.pg_decode_object(bb, s.Value._object.AtIndex(ii)))
                        {
                            s.Value._object[ii].pts = p.Value.pts;
                            return true;
                        }
                        PgDecode.pg_clean_object(s.Value._object.AtIndex(ii));
                        return false;
                    }
                }
            }

            /* add and decode new object */

            Ref<BD_PG_OBJECT> tmp = s.Value._object.Reallocate(s.Value.num_object + 1);
            s.Value._object = tmp;
            s.Value._object[s.Value.num_object] = new();

            if (PgDecode.pg_decode_object(bb, s.Value._object.AtIndex(s.Value.num_object)))
            {
                s.Value._object[s.Value.num_object].pts = p.Value.pts;
                s.Value.num_object++;
                return true;
            }

            PgDecode.pg_clean_object(s.Value._object.AtIndex(s.Value.num_object));

            return false;
        }

        static bool _decode_pds(Ref<PG_DISPLAY_SET> s, Ref<BITBUFFER> bb, Ref<PES_BUFFER> p)
        {
            if (s.Value.decoding == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"skipping orphan palette definition segment");
                return false;
            }

            /* search for palette to be updated */

            if (s.Value.palette)
            {
                BITBUFFER bb_tmp = bb.Value;
                byte id = bb_tmp.bb_read<byte>(8);
                uint ii;

                for (ii = 0; ii < s.Value.num_palette; ii++)
                {
                    if (s.Value.palette[ii].id == id)
                    {
                        bool rr;
                        if ((s.Value.ics && s.Value.ics.Value.composition_descriptor.Value.state == 0) ||
                             (s.Value.pcs && s.Value.pcs.Value.composition_descriptor.Value.state == 0))
                        {
                            /* 8.8.3.1.1 */
                            rr = PgDecode.pg_decode_palette_update(bb, s.Value.palette.AtIndex(ii));
                        }
                        else
                        {
                            rr = PgDecode.pg_decode_palette(bb, s.Value.palette.AtIndex(ii));
                        }
                        if (rr)
                        {
                            s.Value.palette[ii].pts = p.Value.pts;
                            return true;
                        }
                        return false;
                    }
                }
            }

            /* add and decode new palette */

            Ref<BD_PG_PALETTE> tmp = s.Value.palette.Reallocate(s.Value.num_palette + 1);
            s.Value.palette = tmp;
            s.Value.palette[s.Value.num_palette] = new();

            if (PgDecode.pg_decode_palette(bb, s.Value.palette.AtIndex(s.Value.num_palette)))
            {
                s.Value.palette[s.Value.num_palette].pts = p.Value.pts;
                s.Value.num_palette++;
                return true;
            }

            return false;
        }

        static void _check_epoch_start(Ref<PG_DISPLAY_SET> s)
        {
            if ((s.Value.pcs && s.Value.pcs.Value.composition_descriptor.Value.state == 2) ||
                (s.Value.ics && s.Value.ics.Value.composition_descriptor.Value.state == 2))
            {
                /* epoch start, drop all cached data */

                uint ii;
                for (ii = 0; ii < s.Value.num_object; ii++)
                {
                    PgDecode.pg_clean_object(s.Value._object.AtIndex(ii));
                }

                s.Value.num_palette = 0;
                s.Value.num_window = 0;
                s.Value.num_object = 0;

                s.Value.epoch_start = 1;

            }
            else
            {
                s.Value.epoch_start = 0;
            }
        }

        static bool _decode_pcs(Ref<PG_DISPLAY_SET> s, Ref<BITBUFFER> bb, Ref<PES_BUFFER> p)
        {
            if (s.Value.complete != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, $"ERROR: updating complete (non-consumed) PG composition");
                s.Value.complete = 0;
            }

            PgDecode.pg_free_composition(ref s.Value.pcs);
            s.Value.pcs = Ref<BD_PG_COMPOSITION>.Allocate();

            if (!PgDecode.pg_decode_composition(bb, s.Value.pcs))
            {
                PgDecode.pg_free_composition(ref s.Value.pcs);
                return false;
            }

            s.Value.pcs.Value.pts = p.Value.pts;
            s.Value.valid_pts = p.Value.pts;

            _check_epoch_start(s);

            s.Value.decoding = 1;

            return true;
        }

        static bool _decode_ics(Ref<PG_DISPLAY_SET> s, Ref<BITBUFFER> bb, Ref<PES_BUFFER> p)
        {
            if (s.Value.complete != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, $"ERROR: updating complete (non-consumed) IG composition");
                s.Value.complete = 0;
            }

            IgDecode.ig_free_interactive(ref s.Value.ics);
            s.Value.ics = Ref<BD_IG_INTERACTIVE>.Allocate();

            if (!IgDecode.ig_decode_interactive(bb, s.Value.ics))
            {
                IgDecode.ig_free_interactive(ref s.Value.ics);
                return false;
            }

            s.Value.ics.Value.pts = p.Value.pts;
            s.Value.valid_pts = p.Value.pts;

            _check_epoch_start(s);

            s.Value.decoding = 1;

            return true;
        }

        static bool _decode_dialog_style(Ref<PG_DISPLAY_SET> s, Ref<BITBUFFER> bb)
        {
            _free_dialogs(s);

            s.Value.complete = 0;

            s.Value.style = Ref<BD_TEXTST_DIALOG_STYLE>.Allocate();

            if (!TextstDecode.textst_decode_dialog_style(bb, s.Value.style))
            {
                TextstDecode.textst_free_dialog_style(ref s.Value.style);
                return false;
            }

            if (bb.Value.p != bb.Value.p_end - 2 || bb.Value.i_left != 8)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, $"_decode_dialog_style() failed: bytes in buffer {(int)(bb.Value.p_end - bb.Value.p)}");
                TextstDecode.textst_free_dialog_style(ref s.Value.style);
                return false;
            }

            s.Value.total_dialog = bb.Value.bb_read<ushort>(16);
            if (s.Value.total_dialog < 1)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, $"_decode_dialog_style(): no dialog segments");
                TextstDecode.textst_free_dialog_style(ref s.Value.style);
                return false;
            }

            s.Value.dialog = Ref<BD_TEXTST_DIALOG_PRESENTATION>.Allocate(s.Value.total_dialog);

            Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"_decode_dialog_style(): {s.Value.total_dialog} dialogs in stream");
            return true;
        }

        static bool _decode_dialog_presentation(Ref<PG_DISPLAY_SET> s, Ref<BITBUFFER> bb)
        {
            if (!s.Value.style || s.Value.total_dialog < 1)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"_decode_dialog_presentation() failed: style segment not decoded");
                return false;
            }
            if (s.Value.num_dialog >= s.Value.total_dialog)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, $"_decode_dialog_presentation(): unexpected dialog segment");
                return false;
            }

            if (!TextstDecode.textst_decode_dialog_presentation(bb, s.Value.dialog.AtIndex(s.Value.num_dialog)))
            {
                TextstDecode.textst_clean_dialog_presentation(s.Value.dialog.AtIndex(s.Value.num_dialog));
                return false;
            }

            s.Value.num_dialog++;

            if (s.Value.num_dialog == s.Value.total_dialog)
            {
                s.Value.complete = 1;
            }

            return true;
        }

        static bool _decode_segment(Ref<PG_DISPLAY_SET> s, Ref<PES_BUFFER> p)
        {
            Variable<BITBUFFER> bb = new();
            bb.Value.bb_init(p.Value.buf, p.Value.len);

            byte type = bb.Value.bb_read<byte>(8);
            /*UInt16 len = */
            bb.Value.bb_read<ushort>(16);
            switch ((pgs_segment_type_e)type)
            {
                case pgs_segment_type_e.PGS_OBJECT:
                    return _decode_ods(s, bb.Ref, p);

                case pgs_segment_type_e.PGS_PALETTE:
                    return _decode_pds(s, bb.Ref, p);

                case pgs_segment_type_e.PGS_WINDOW:
                    return _decode_wds(s, bb.Ref, p);

                case pgs_segment_type_e.PGS_PG_COMPOSITION:
                    return _decode_pcs(s, bb.Ref, p);

                case pgs_segment_type_e.PGS_IG_COMPOSITION:
                    return _decode_ics(s, bb.Ref, p);

                case pgs_segment_type_e.PGS_END_OF_DISPLAY:
                    if (s.Value.decoding == 0)
                    {
                        /* avoid duplicate initialization / presenataton */
                        Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"skipping orphan end of display segment");
                        return false;
                    }
                    s.Value.complete = 1;
                    s.Value.decoding = 0;
                    return true;

                case pgs_segment_type_e.TGS_DIALOG_STYLE:
                    return _decode_dialog_style(s, bb.Ref);

                case pgs_segment_type_e.TGS_DIALOG_PRESENTATION:
                    return _decode_dialog_presentation(s, bb.Ref);

                default:
                    Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, $"unknown segment type 0x{type:x}");
                    break;
            }

            return false;
        }

        /*
         * mpeg-pes interface
         */
        const Int64 MAX_STC_DTS_DIFF = (90000L * 30L); /* 30 seconds */
        static bool graphics_processor_decode_pes(ref Ref<PG_DISPLAY_SET> s, ref Ref<PES_BUFFER> p, Int64 stc)
        {
            if (!s)
            {
                return false;
            }

            if (s == null)
            {
                s = Ref<PG_DISPLAY_SET>.Allocate();
            }

            while (p)
            {

                /* time to decode next segment ? */
                if (stc >= 0 && p.Value.dts > stc)
                {

                    /* filter out values that seem to be incorrect (if stc is not updated) */
                    Int64 diff = p.Value.dts - stc;
                    if (diff < MAX_STC_DTS_DIFF)
                    {
                        GP_TRACE($"Segment dts > stc ({p.Value.dts} > {stc} ; diff {diff})");
                        return false;
                    }
                }

                /* all fragments present ? */
                if (!_join_segment_fragments(p))
                {
                    GP_TRACE("splitted segment not complete, waiting for next fragment");
                    return false;
                }

                if (p.Value.len <= 2)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DECODE, $"segment too short, skipping ({p.Value.len} bytes)");
                    PesBuffer.pes_buffer_next(ref p);
                    continue;
                }

                /* decode segment */

                GP_TRACE($"Decoding segment, dts {p.Value.dts:0000000000} pts {p.Value.pts:0000000000} len {p.Value.len}");

                _decode_segment(s, p);

                PesBuffer.pes_buffer_next(ref p);

                if (s.Value.complete != 0)
                {
                    return true;
                }

            }

            return false;
        }

        internal static Ref<GRAPHICS_PROCESSOR> graphics_processor_init()
        {
            Ref<GRAPHICS_PROCESSOR> p = Ref<GRAPHICS_PROCESSOR>.Allocate();

            return p;
        }

        internal static void graphics_processor_free(ref Ref<GRAPHICS_PROCESSOR> p)
        {
            if (p)
            {
                M2tsDemux.m2ts_demux_free(ref p.Value.demux);
                PesBuffer.pes_buffer_free(ref p.Value.queue);

                p.Free();
            }
        }

        internal static bool graphics_processor_decode_ts(Ref<GRAPHICS_PROCESSOR> p,
                                         ref Ref<PG_DISPLAY_SET> s,
                                         UInt16 pid, Ref<byte> unit, uint num_units,
                                         Int64 stc)
        {
            uint ii;
            bool result = false;

            if (pid != p.Value.pid)
            {
                M2tsDemux.m2ts_demux_free(ref p.Value.demux);
                PesBuffer.pes_buffer_free(ref p.Value.queue);
            }
            if (!p.Value.demux)
            {
                p.Value.demux = M2tsDemux.m2ts_demux_init(pid);
                if (!p.Value.demux)
                {
                    return false;
                }
                p.Value.pid = pid;
            }

            for (ii = 0; ii < num_units; ii++)
            {
                PesBuffer.pes_buffer_append(ref p.Value.queue, M2tsDemux.m2ts_demux(p.Value.demux, unit));
                unit += 6144;
            }

            if (p.Value.queue)
            {
                result = graphics_processor_decode_pes(ref s, ref p.Value.queue, stc);
            }

            return result;
        }
    }

}