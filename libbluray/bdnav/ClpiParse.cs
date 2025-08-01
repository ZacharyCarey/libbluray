using libbluray.disc;
using libbluray.file;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.bdnav
{
    internal static class ClpiParse
    {

        private const UInt32 CLPI_SIG1 = ('H' << 24) | ('D' << 16) | ('M' << 8) | 'V';

        private static bool _parse_stream_attr(Ref<BITSTREAM> bits, Ref<CLPI_PROG_STREAM> ss)
        {
            Int64 pos;
            uint len;

            if (!bits.Value.bs_is_align(0x07))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, "_parse_stream_attr(): Stream alignment error");
            }

            len = bits.Value.bs_read<uint>(8);
            pos = bits.Value.bs_pos() >> 3;

            ss.Value.lang = "";
            Array.Fill<byte>(ss.Value.isrc, 0, 0, 12);
            ss.Value.coding_type = bits.Value.bs_read<byte>(8);
            switch(ss.Value.coding_type)
            {
                case 0x01:
                case 0x02:
                case 0xea:
                case 0x1b:
                case 0x20:
                case 0x24:
                    ss.Value.format = bits.Value.bs_read<byte>(4);
                    ss.Value.rate = bits.Value.bs_read<byte>(4);
                    ss.Value.aspect = bits.Value.bs_read<byte>(4);
                    bits.Value.bs_skip(2);
                    ss.Value.oc_flag = bits.Value.bs_read<byte>(1);
                    if (ss.Value.coding_type == 0x24)
                    {
                        ss.Value.cr_flag = bits.Value.bs_read<byte>(1);
                        ss.Value.dynamic_range_type = bits.Value.bs_read<byte>(4);
                        ss.Value.color_space = bits.Value.bs_read<byte>(4);
                        ss.Value.hdr_plus_flag = bits.Value.bs_read<byte>(1);
                        bits.Value.bs_skip(7);
                    }else
                    {
                        bits.Value.bs_skip(17);
                    }
                    break;

                case 0x03:
                case 0x04:
                case 0x80:
                case 0x81:
                case 0x82:
                case 0x83:
                case 0x84:
                case 0x85:
                case 0x86:
                case 0xa1:
                case 0xa2:
                    ss.Value.format = bits.Value.bs_read<byte>(4);
                    ss.Value.rate = bits.Value.bs_read<byte>(4);
                    ss.Value.lang = bits.Value.bs_read_string(3);
                    break;

                case 0x90:
                case 0x91:
                case 0xa0:
                    ss.Value.lang = bits.Value.bs_read_string(3);
                    bits.Value.bs_skip(8);
                    break;

                case 0x92:
                    ss.Value.char_code = bits.Value.bs_read<byte>(8);
                    ss.Value.lang = bits.Value.bs_read_string(3);
                    break;

                default:
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_stream_attr(): unrecognized coding type {ss.Value.coding_type:x2}");
                    break;
            }

            bits.Value.bs_read_bytes(ss.Value.isrc, 12);

            // Skip over any padding
            if (bits.Value.bs_seek_byte(pos + len) < 0)
            {
                return false;
            }
            return true;
        }

        private static bool _parse_header(Ref<BITSTREAM> bits, Ref<CLPI_CL> cl)
        {
            cl.Value.type_indicator = CLPI_SIG1;
            if (!BdmvParse.bdmv_parse_header(bits, cl.Value.type_indicator, cl.Value.type_indicator2.Ref))
            {
                return false;
            }

            if (bits.Value.bs_avail() < 5 * 32)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, "_parse_header: unexpected end of file");
                return false;
            }

            cl.Value.sequence_info_start_addr = bits.Value.bs_read<uint>(32);
            cl.Value.program_info_start_addr = bits.Value.bs_read<uint>(32);
            cl.Value.cpi_start_addr = bits.Value.bs_read<uint>(32);
            cl.Value.clip_mark_start_addr = bits.Value.bs_read<uint>(32);
            cl.Value.ext_data_start_addr = bits.Value.bs_read<uint>(32);
            return true;
        }

        private static bool _parse_clipinfo(Ref<BITSTREAM> bits, Ref<CLPI_CL> cl)
        {
            Int64 pos;
            int len;
            int ii;

            if (bits.Value.bs_seek_byte(40) < 0)
            {
                return false;
            }

            // ClipInfo len
            bits.Value.bs_skip(32);
            // reserved
            bits.Value.bs_skip(16);
            cl.Value.clip.clip_stream_type = bits.Value.bs_read<byte>(8);
            cl.Value.clip.application_type = bits.Value.bs_read<byte>(8);
            // skip reserved 31 bits
            bits.Value.bs_skip(31);
            cl.Value.clip.is_atc_delta = bits.Value.bs_read<byte>(1);
            cl.Value.clip.ts_recording_rate = bits.Value.bs_read<uint>(32);
            cl.Value.clip.num_source_packets = bits.Value.bs_read<uint>(32);

            // skip reserved 128 bytes
            bits.Value.bs_skip(128 * 8);

            // ts type info block
            len = bits.Value.bs_read<int>(16);
            pos = bits.Value.bs_pos() >> 3;
            if (len != 0)
            {
                cl.Value.clip.ts_type_info.validity = bits.Value.bs_read<byte>(8);
                cl.Value.clip.ts_type_info.format_id = bits.Value.bs_read_string(4);
                // Seek past the stuff we dont know anything about
                if (bits.Value.bs_seek_byte(pos + len) < 0)
                {
                    return false;
                }
            }
            if (cl.Value.clip.is_atc_delta != 0)
            {
                // Skip reserved bytes
                bits.Value.bs_skip(8);
                cl.Value.clip.atc_delta_count = bits.Value.bs_read<byte>(8);
                cl.Value.clip.atc_delta = Ref<CLPI_ATC_DELTA>.Allocate(cl.Value.clip.atc_delta_count);
                for (ii = 0; ii < cl.Value.clip.atc_delta_count; ii++)
                {
                    cl.Value.clip.atc_delta[ii].delta = bits.Value.bs_read<uint>(32);
                    cl.Value.clip.atc_delta[ii].file_id = bits.Value.bs_read_string(5);
                    cl.Value.clip.atc_delta[ii].file_code = bits.Value.bs_read_string(4);
                    bits.Value.bs_skip(8);
                }
            }

            // font file
            if (cl.Value.clip.application_type == 6) // Sub TS for a sub-path of Text subtitle
            {
                Ref<CLPI_FONT_INFO> fi = cl.Value.clip.font_info.Ref;
                bits.Value.bs_skip(8);
                fi.Value.font_count = bits.Value.bs_read<byte>(8);
                if (fi.Value.font_count != 0)
                {
                    fi.Value.font = Ref<CLPI_FONT>.Allocate(fi.Value.font_count);
                    for (ii = 0; ii < fi.Value.font_count; ii++)
                    {
                        fi.Value.font[ii].file_id = bits.Value.bs_read_string(5);
                        bits.Value.bs_skip(8);
                    }
                }
            }

            return true;
        }

        private static bool _parse_sequence(Ref<BITSTREAM> bits, Ref<CLPI_CL> cl)
        {
            int ii, jj;

            if (bits.Value.bs_seek_byte(cl.Value.sequence_info_start_addr) < 0)
            {
                return false;
            }

            // Skip the length field, and a reserved bytes
            bits.Value.bs_skip(5 * 8);
            // Then get the number of sequences
            cl.Value.sequence.num_atc_seq = bits.Value.bs_read<byte>(8);

            Ref<CLPI_ATC_SEQ> atc_seq;
            atc_seq = Ref<CLPI_ATC_SEQ>.Allocate(cl.Value.sequence.num_atc_seq);
            cl.Value.sequence.atc_seq = atc_seq;
            for (ii = 0; ii < cl.Value.sequence.num_atc_seq; ii++)
            {
                atc_seq[ii].spn_atc_start = bits.Value.bs_read<uint>(32);
                atc_seq[ii].num_stc_seq = bits.Value.bs_read<byte>(8);
                atc_seq[ii].offset_stc_id = bits.Value.bs_read<byte>(8);

                Ref<CLPI_STC_SEQ> stc_seq;
                stc_seq = Ref<CLPI_STC_SEQ>.Allocate(atc_seq[ii].num_stc_seq);
                atc_seq[ii].stc_seq = stc_seq;
                for (jj = 0; jj < atc_seq[ii].num_stc_seq; jj++)
                {
                    stc_seq[jj].pcr_pid = bits.Value.bs_read<ushort>(16);
                    stc_seq[jj].spn_stc_start = bits.Value.bs_read<uint>(32);
                    stc_seq[jj].presentation_start_time = bits.Value.bs_read<uint>(32);
                    stc_seq[jj].presentation_end_time = bits.Value.bs_read<uint>(32);
                }
            }

            return true;
        }

        private static bool _parse_program(Ref<BITSTREAM> bits, Ref<CLPI_PROG_INFO> program)
        {
            int ii, jj;

            // Skip the length field, and a reserved byte
            bits.Value.bs_skip(5 * 8);
            // Then get the number of sequences
            program.Value.num_prog = bits.Value.bs_read<byte>(8);

            Ref<CLPI_PROG> progs;
            progs = Ref<CLPI_PROG>.Allocate(program.Value.num_prog);
            program.Value.progs = progs;
            for (ii = 0; ii < program.Value.num_prog; ii++)
            {
                progs[ii].spn_program_sequence_start = bits.Value.bs_read<uint>(32);
                progs[ii].program_map_pid = bits.Value.bs_read<ushort>(16);
                progs[ii].num_streams = bits.Value.bs_read<byte>(8);
                progs[ii].num_groups = bits.Value.bs_read<byte>(8);

                Ref<CLPI_PROG_STREAM> ps;
                ps = Ref<CLPI_PROG_STREAM>.Allocate(progs[ii].num_streams);
                progs[ii].streams = ps;
                for (jj = 0; jj < progs[ii].num_streams; jj++)
                {
                    ps[jj].pid = bits.Value.bs_read<ushort>(16);
                    if (!_parse_stream_attr(bits, ps.AtIndex(jj)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool _parse_program_info(Ref<BITSTREAM> bits, Ref<CLPI_CL> cl)
        {
            if (bits.Value.bs_seek_byte(cl.Value.program_info_start_addr) < 0)
            {
                return false;
            }

            return _parse_program(bits, cl.Value.program.Ref);
        }

        private static bool _parse_ep_map_stream(Ref<BITSTREAM> bits, Ref<CLPI_EP_MAP_ENTRY> ee)
        {
            UInt32 fine_start;
            int ii;
            Ref<CLPI_EP_COARSE> coarse;
            Ref<CLPI_EP_FINE> fine;

            if (bits.Value.bs_seek_byte(ee.Value.ep_map_stream_start_addr) < 0)
            {
                return false;
            }
            fine_start = bits.Value.bs_read<uint>(32);

            if (bits.Value.bs_avail() / (8 * 8) < ee.Value.num_ep_coarse)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, "clpi_parse: unexpected EOF (EP coarse)");
                return false;
            }

            coarse = Ref<CLPI_EP_COARSE>.Allocate(ee.Value.num_ep_coarse);
            ee.Value.coarse = coarse;
            for (ii = 0; ii < ee.Value.num_ep_coarse; ii++)
            {
                coarse[ii].ref_ep_fine_id = bits.Value.bs_read<int>(18);
                coarse[ii].pts_ep = bits.Value.bs_read<int>(14);
                coarse[ii].spn_ep = bits.Value.bs_read<uint>(32);
            }

            if (bits.Value.bs_seek_byte(ee.Value.ep_map_stream_start_addr + fine_start) < 0)
            {
                return false;
            }

            if (bits.Value.bs_avail() / (8 * 4) < ee.Value.num_ep_fine)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, "clpi_parse: unexpected EOF (EP fine)");
                return false;
            }

            fine = Ref<CLPI_EP_FINE>.Allocate(ee.Value.num_ep_fine);
            ee.Value.fine = fine;
            for (ii = 0; ii < ee.Value.num_ep_fine; ii++)
            {
                fine[ii].is_angle_change_point = bits.Value.bs_read<byte>(1);
                fine[ii].i_end_position_offset = bits.Value.bs_read<byte>(3);
                fine[ii].pts_ep = bits.Value.bs_read<int>(11);
                fine[ii].spn_ep = bits.Value.bs_read<int>(17);
            }
            return true;
        }

        private static bool _parse_cpi(Ref<BITSTREAM> bits, Ref<CLPI_CPI> cpi)
        {
            int ii;
            UInt32 ep_map_pos, len;

            len = bits.Value.bs_read<uint>(32);
            if (len == 0)
            {
                return true;
            }

            bits.Value.bs_skip(12);
            cpi.Value.type = bits.Value.bs_read<byte>(4);
            ep_map_pos = (UInt32)(bits.Value.bs_pos() >> 3);

            // EP Map starts here
            bits.Value.bs_skip(8);
            cpi.Value.num_stream_pid = bits.Value.bs_read<byte>(8);

            Ref<CLPI_EP_MAP_ENTRY> entry;
            entry = Ref<CLPI_EP_MAP_ENTRY>.Allocate(cpi.Value.num_stream_pid);
            cpi.Value.entry = entry;
            for (ii = 0; ii < cpi.Value.num_stream_pid; ii++)
            {
                entry[ii].pid = bits.Value.bs_read<ushort>(16);
                bits.Value.bs_skip(10);
                entry[ii].ep_stream_type = bits.Value.bs_read<byte>(4);
                entry[ii].num_ep_coarse = bits.Value.bs_read<int>(16);
                entry[ii].num_ep_fine = bits.Value.bs_read<int>(18);
                entry[ii].ep_map_stream_start_addr = bits.Value.bs_read<uint>(32) + ep_map_pos;
            }
            for (ii = 0; ii < cpi.Value.num_stream_pid; ii++)
            {
                if (!_parse_ep_map_stream(bits, cpi.Value.entry.AtIndex(ii)))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool _parse_cpi_info(Ref<BITSTREAM> bits, Ref<CLPI_CL> cl)
        {
            if (bits.Value.bs_seek_byte(cl.Value.cpi_start_addr) < 0)
            {
                return false;
            }

            return _parse_cpi(bits, cl.Value.cpi.Ref);
        }

        internal static Ref<CLPI_CL> clpi_get(BD_DISC disc, string file)
        {
            Ref<CLPI_CL> cl = new();

            cl = (Ref<CLPI_CL>)(disc.disc_cache_get(file) ?? Ref<CLPI_CL>.Null);
            if (cl)
            {
                return cl;
            }

            cl = _clpi_get(disc, Path.Combine("BDMV", "CLIPINF"), file);
            if (!cl)
            {
                /* if failed, try backup file */
                cl = _clpi_get(disc, Path.Combine("BDMV", "BACKUP", "CLIPINF"), file);
            }

            if (cl)
            {
                disc.disc_cache_put(file, cl);
            }

            return cl;
        }
        internal static UInt32 clpi_find_stc_spn(Ref<CLPI_CL> cl, byte stc_id)
        {
            int ii;
            Ref<CLPI_ATC_SEQ> atc;

            for (ii = 0; ii < cl.Value.sequence.num_atc_seq; ii++)
            {
                atc = cl.Value.sequence.atc_seq.AtIndex(ii);
                if (stc_id < atc.Value.offset_stc_id + atc.Value.num_stc_seq)
                {
                    return atc.Value.stc_seq[stc_id - atc.Value.offset_stc_id].spn_stc_start;
                }
            }
            return 0;
        }

        // Looks up the start packet number for the timestamp
        // Returns the spn for the entry that is closest to but
        // before the given timestamp
        internal static UInt32 clpi_lookup_spn(Ref<CLPI_CL> cl, UInt32 timestamp, int before, byte stc_id)
        {
            Ref<CLPI_EP_MAP_ENTRY> entry;
            Ref<CLPI_CPI> cpi = cl.Value.cpi.Ref;
            int ii, jj;
            UInt64 coarse_pts, pts; // 45khz timestamps
            UInt32 spn, coarse_spn, stc_spn;
            int start, end;
            int _ref = 0;

            if (cpi.Value.num_stream_pid < 1 || !cpi.Value.entry)
            {
                if (before != 0)
                {
                    return 0;
                }
                return cl.Value.clip.num_source_packets;
            }

            // Assumes that there is only one pid of interest
            entry = cpi.Value.entry.AtIndex(0);

            // Use sequence info to find spn_stc_start before doing
            // PTS search. The spn_stc_start defines the point in
            // the EP map to start searching.
            stc_spn = clpi_find_stc_spn(cl, stc_id);
            for (ii = 0; ii < entry.Value.num_ep_coarse; ii++)
            {
                _ref = entry.Value.coarse[ii].ref_ep_fine_id;
                if (entry.Value.coarse[ii].spn_ep >= stc_spn)
                {
                    // The desired starting point is either after this point
                    // or in the middle of the previous coarse entry
                    break;
                }
            }
            if (ii >= entry.Value.num_ep_coarse)
            {
                return cl.Value.clip.num_source_packets;
            }
            pts = ((UInt64)(entry.Value.coarse[ii].pts_ep & ~0x01) << 18) +
                  ((UInt64)entry.Value.fine[_ref].pts_ep << 8);
            if (pts > timestamp && ii != 0)
            {
                // The starting point and desired PTS is in the previous coarse entry
                ii--;
                coarse_pts = (UInt32)(entry.Value.coarse[ii].pts_ep & ~0x01) << 18;
                coarse_spn = entry.Value.coarse[ii].spn_ep;
                start = entry.Value.coarse[ii].ref_ep_fine_id;
                end = entry.Value.coarse[ii + 1].ref_ep_fine_id;
                // Find a fine entry that has bothe spn > stc_spn and ptc > timestamp
                for (jj = start; jj < end; jj++)
                {

                    pts = coarse_pts + ((UInt32)entry.Value.fine[jj].pts_ep << 8);
                    spn = (coarse_spn & ~0x1FFFFu) + (uint)entry.Value.fine[jj].spn_ep;
                    if (stc_spn >= spn && pts > timestamp)
                        break;
                }
                goto done;
            }

            // If we've gotten this far, the desired timestamp is somewhere
            // after the coarse entry we found the stc_spn in.
            start = ii;
            for (ii = start; ii < entry.Value.num_ep_coarse; ii++)
            {
                _ref = entry.Value.coarse[ii].ref_ep_fine_id;
                pts = ((UInt64)(entry.Value.coarse[ii].pts_ep & ~0x01) << 18) +
                        ((UInt64)entry.Value.fine[_ref].pts_ep << 8);
                if (pts > timestamp)
                {
                    break;
                }
            }
            // If the timestamp is before the first entry, then return
            // the beginning of the clip
            if (ii == 0)
            {
                return 0;
            }
            ii--;
            coarse_pts = (UInt32)(entry.Value.coarse[ii].pts_ep & ~0x01) << 18;
            start = entry.Value.coarse[ii].ref_ep_fine_id;
            if (ii < entry.Value.num_ep_coarse - 1)
            {
                end = entry.Value.coarse[ii + 1].ref_ep_fine_id;
            }
            else
            {
                end = entry.Value.num_ep_fine;
            }
            for (jj = start; jj < end; jj++)
            {

                pts = coarse_pts + ((UInt32)entry.Value.fine[jj].pts_ep << 8);
                if (pts > timestamp)
                    break;
            }

        done:
            if (before != 0)
            {
                jj--;
            }
            if (jj == end)
            {
                ii++;
                if (ii >= entry.Value.num_ep_coarse)
                {
                    // End of file
                    return cl.Value.clip.num_source_packets;
                }
                jj = entry.Value.coarse[ii].ref_ep_fine_id;
            }
            spn = (entry.Value.coarse[ii].spn_ep & ~0x1FFFFu) + (uint)entry.Value.fine[jj].spn_ep;
            return spn;
        }

        // Looks up the start packet number that is closest to the requested packet
        // Returns the spn for the entry that is closest to but
        // before the given packet
        internal static UInt32 clpi_access_point(Ref<CLPI_CL> cl, UInt32 pkt, int next, int angle_change, Ref<UInt32> time)
        {
            Ref<CLPI_EP_MAP_ENTRY> entry;
            Ref<CLPI_CPI> cpi = cl.Value.cpi.Ref;
            int ii, jj;
            UInt32 coarse_spn, spn = 0;
            int start, end;
            int _ref;

            // Assumes that there is only one pid of interest
            entry = cpi.Value.entry.AtIndex(0);

            for (ii = 0; ii < entry.Value.num_ep_coarse; ii++)
            {
                _ref = entry.Value.coarse[ii].ref_ep_fine_id;
                spn = (entry.Value.coarse[ii].spn_ep & ~0x1FFFFu) + (uint)entry.Value.fine[_ref].spn_ep;
                if (spn > pkt)
                {
                    break;
                }
            }
            // If the timestamp is before the first entry, then return
            // the beginning of the clip
            if (ii == 0)
            {
                time.Value = 0;
                return 0;
            }
            ii--;
            coarse_spn = (entry.Value.coarse[ii].spn_ep & ~0x1FFFFu);
            start = entry.Value.coarse[ii].ref_ep_fine_id;
            if (ii < entry.Value.num_ep_coarse - 1)
            {
                end = entry.Value.coarse[ii + 1].ref_ep_fine_id;
            }
            else
            {
                end = entry.Value.num_ep_fine;
            }
            for (jj = start; jj < end; jj++)
            {

                spn = coarse_spn + (uint)entry.Value.fine[jj].spn_ep;
                if (spn >= pkt)
                {
                    break;
                }
            }
            if (jj == end && next != 0)
            {
                ii++;
                jj = 0;
            }
            else if (spn != pkt && next == 0)
            {
                jj--;
            }
            if (ii == entry.Value.num_ep_coarse)
            {
                time.Value = 0;
                return cl.Value.clip.num_source_packets;
            }
            coarse_spn = (entry.Value.coarse[ii].spn_ep & ~0x1FFFFu);
            if (angle_change != 0)
            {
                // Keep looking till there's an angle change point
                for (; jj < end; jj++)
                {

                    if (entry.Value.fine[jj].is_angle_change_point != 0)
                    {
                        time.Value = (UInt32)(((UInt64)(entry.Value.coarse[ii].pts_ep & ~0x01) << 18) +
                                ((UInt64)entry.Value.fine[jj].pts_ep << 8));
                        return coarse_spn + (uint)entry.Value.fine[jj].spn_ep;
                    }
                }
                for (ii++; ii < entry.Value.num_ep_coarse; ii++)
                {
                    start = entry.Value.coarse[ii].ref_ep_fine_id;
                    if (ii < entry.Value.num_ep_coarse - 1)
                    {
                        end = entry.Value.coarse[ii + 1].ref_ep_fine_id;
                    }
                    else
                    {
                        end = entry.Value.num_ep_fine;
                    }
                    for (jj = start; jj < end; jj++)
                    {

                        if (entry.Value.fine[jj].is_angle_change_point != 0)
                        {
                            time.Value = (UInt32)(((UInt64)(entry.Value.coarse[ii].pts_ep & ~0x01) << 18) +
                                    ((UInt64)entry.Value.fine[jj].pts_ep << 8));
                            return coarse_spn + (uint)entry.Value.fine[jj].spn_ep;
                        }
                    }
                }
                time.Value = 0;
                return cl.Value.clip.num_source_packets;
            }
            time.Value = (UInt32)(((UInt64)(entry.Value.coarse[ii].pts_ep & ~0x01) << 18) +
                    ((UInt64)entry.Value.fine[jj].pts_ep << 8));
            return coarse_spn + (UInt32)entry.Value.fine[jj].spn_ep;
        }

        private static bool _parse_extent_start_points(Ref<BITSTREAM> bits, Ref<CLPI_EXTENT_START> es)
        {
            uint ii;

            bits.Value.bs_skip(32); // length
            es.Value.num_point = bits.Value.bs_read<uint>(32);

            es.Value.point = Ref<UInt32>.Allocate(es.Value.num_point);
            for (ii = 0; ii < es.Value.num_point; ii++)
            {
                es.Value.point[ii] = bits.Value.bs_read<uint>(32);
            }

            return true;
        }

        private static bool _parse_clpi_extension(Ref<BITSTREAM> bits, int id1, int id2, Ref<CLPI_CL> cl)
        {
            if (id1 == 1)
            {
                if (id2 == 2)
                {
                    // LPCM down mix coefficient
                    //_parse_lpcm_down_mix_coeff(bits, &cl.Value.lpcm_down_mix_coeff);
                    return false;
                }
            }

            if (id1 == 2)
            {
                if (id2 == 4)
                {
                    // Extent start point
                    return _parse_extent_start_points(bits, cl.Value.extent_start.Ref);
                }
                if (id2 == 5)
                {
                    // ProgramInfo SS
                    return _parse_program(bits, cl.Value.program_ss.Ref);
                }
                if (id2 == 6)
                {
                    // CPI SS
                    return _parse_cpi(bits, cl.Value.cpi_ss.Ref);
                }
            }

            Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"_parse_clpi_extension(): unhandled extension {id1}.{id2}");
            return false;
        }

        private static void _clean_program(Ref<CLPI_PROG_INFO> p)
        {
            int ii;

            if (p && p.Value.progs)
            {
                for (ii = 0; ii < p.Value.num_prog; ii++)
                {
                    p.Value.progs[ii].streams.Free();
                }
                p.Value.progs.Free();
            }
        }

        private static void _clean_cpi(Ref<CLPI_CPI> cpi)
        {
            int ii;

            if (cpi && cpi.Value.entry)
            {
                for (ii = 0; ii < cpi.Value.num_stream_pid; ii++)
                {
                    cpi.Value.entry[ii].coarse.Free();
                    cpi.Value.entry[ii].fine.Free();
                }
                cpi.Value.entry.Free();
            }
        }

        private static void _clpi_clean(Ref<CLPI_CL> cl)
        {
            int ii;

            cl.Value.clip.atc_delta.Free();
            cl.Value.clip.font_info.Value.font.Free();

            if (cl.Value.sequence.atc_seq)
            {
                for (ii = 0; ii < cl.Value.sequence.num_atc_seq; ii++)
                {
                    cl.Value.sequence.atc_seq[ii].stc_seq.Free();
                }

                cl.Value.sequence.atc_seq.Free();
            }

            _clean_program(cl.Value.program.Ref);
            _clean_cpi(cl.Value.cpi.Ref);

            cl.Value.extent_start.Value.point.Free();

            _clean_program(cl.Value.program_ss.Ref);
            _clean_cpi(cl.Value.cpi_ss.Ref);
        }

        private static void _clpi_free(Ref<CLPI_CL> cl)
        {
            // refcnt_dec(cl)
        }

        public static void clpi_unref(ref Ref<CLPI_CL> cl)
        {
            if (cl)
            {
                _clpi_free(cl);
                cl = Ref<CLPI_CL>.Null;
            }
        }

        public static void clpi_free(ref Ref<CLPI_CL> cl)
        {
            if (cl)
            {
                _clpi_free(cl);
                cl = Ref<CLPI_CL>.Null;
            }
        }

        private static Ref<CLPI_CL> _clpi_parse(BD_FILE_H fp)
        {
            Variable<BITSTREAM> bits = new();
            Ref<CLPI_CL> cl;

            if (bits.Value.bs_init(fp) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, "?????.clpi: read error");
                return Ref<CLPI_CL>.Null;
            }

            cl = Ref<CLPI_CL>.Allocate();
            if (!_parse_header(bits.Ref, cl))
            {
                _clpi_free(cl);
                return Ref<CLPI_CL>.Null;
            }

            if (cl.Value.ext_data_start_addr > 0)
            {
                ExtDataParse.bdmv_parse_extension_data(bits.Ref,
                                           (int)cl.Value.ext_data_start_addr,
                                           _parse_clpi_extension,
                                           cl);
            }

            if (!_parse_clipinfo(bits.Ref, cl))
            {
                _clpi_free(cl);
                return Ref<CLPI_CL>.Null;
            }
            if (!_parse_sequence(bits.Ref, cl))
            {
                _clpi_free(cl);
                return Ref<CLPI_CL>.Null;
            }
            if (!_parse_program_info(bits.Ref, cl))
            {
                _clpi_free(cl);
                return Ref<CLPI_CL>.Null;
            }
            if (!_parse_cpi_info(bits.Ref, cl))
            {
                _clpi_free(cl);
                return Ref<CLPI_CL>.Null;
            }

            return cl;
        }

        public static Ref<CLPI_CL> clpi_parse(string path)
        {
            BD_FILE_H? fp;
            Ref<CLPI_CL> cl;

            fp = BD_FILE_H.file_open(path, true);
            if (fp == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"Failed to open {path}");
                return Ref<CLPI_CL>.Null;
            }

            cl = _clpi_parse(fp);
            fp.file_close();
            return cl;
        }

        private static Ref<CLPI_CL> _clpi_get(BD_DISC disc, string dir, string file)
        {
            BD_FILE_H? fp = null;
            Ref<CLPI_CL> cl;

            fp = disc.disc_open_file(dir, file);
            if (fp == null)
            {
                return Ref<CLPI_CL>.Null;
            }

            cl = _clpi_parse(fp);
            fp.file_close();
            return cl;
        }

        public static Ref<CLPI_CL> clpi_copy(Ref<CLPI_CL> src_cl)
        {
            Ref<CLPI_CL> dest_cl = Ref<CLPI_CL>.Null;
            int ii, jj;

            if (src_cl)
            {
                dest_cl = Ref<CLPI_CL>.Allocate();
                dest_cl.Value.clip.clip_stream_type = src_cl.Value.clip.clip_stream_type;
                dest_cl.Value.clip.application_type = src_cl.Value.clip.application_type;
                dest_cl.Value.clip.is_atc_delta = src_cl.Value.clip.is_atc_delta;
                dest_cl.Value.clip.atc_delta_count = src_cl.Value.clip.atc_delta_count;
                dest_cl.Value.clip.ts_recording_rate = src_cl.Value.clip.ts_recording_rate;
                dest_cl.Value.clip.num_source_packets = src_cl.Value.clip.num_source_packets;
                dest_cl.Value.clip.ts_type_info.validity = src_cl.Value.clip.ts_type_info.validity;
                dest_cl.Value.clip.ts_type_info.format_id = src_cl.Value.clip.ts_type_info.format_id;
                dest_cl.Value.clip.atc_delta = Ref<CLPI_ATC_DELTA>.Allocate(src_cl.Value.clip.atc_delta_count);
                for (ii = 0; ii < src_cl.Value.clip.atc_delta_count; ii++)
                {
                    dest_cl.Value.clip.atc_delta[ii].delta = src_cl.Value.clip.atc_delta[ii].delta;
                    dest_cl.Value.clip.atc_delta[ii].file_id = src_cl.Value.clip.atc_delta[ii].file_id;
                    dest_cl.Value.clip.atc_delta[ii].file_code = src_cl.Value.clip.atc_delta[ii].file_code;
                }

                dest_cl.Value.sequence.num_atc_seq = src_cl.Value.sequence.num_atc_seq;
                dest_cl.Value.sequence.atc_seq = Ref<CLPI_ATC_SEQ>.Allocate(src_cl.Value.sequence.num_atc_seq);
                for (ii = 0; ii < src_cl.Value.sequence.num_atc_seq; ii++)
                {
                    dest_cl.Value.sequence.atc_seq[ii].spn_atc_start = src_cl.Value.sequence.atc_seq[ii].spn_atc_start;
                    dest_cl.Value.sequence.atc_seq[ii].offset_stc_id = src_cl.Value.sequence.atc_seq[ii].offset_stc_id;
                    dest_cl.Value.sequence.atc_seq[ii].num_stc_seq = src_cl.Value.sequence.atc_seq[ii].num_stc_seq;
                    dest_cl.Value.sequence.atc_seq[ii].stc_seq = Ref<CLPI_STC_SEQ>.Allocate(src_cl.Value.sequence.atc_seq[ii].num_stc_seq);
                    for (jj = 0; jj < src_cl.Value.sequence.atc_seq[ii].num_stc_seq; jj++)
                    {
                        dest_cl.Value.sequence.atc_seq[ii].stc_seq[jj].spn_stc_start = src_cl.Value.sequence.atc_seq[ii].stc_seq[jj].spn_stc_start;
                        dest_cl.Value.sequence.atc_seq[ii].stc_seq[jj].pcr_pid = src_cl.Value.sequence.atc_seq[ii].stc_seq[jj].pcr_pid;
                        dest_cl.Value.sequence.atc_seq[ii].stc_seq[jj].presentation_start_time = src_cl.Value.sequence.atc_seq[ii].stc_seq[jj].presentation_start_time;
                        dest_cl.Value.sequence.atc_seq[ii].stc_seq[jj].presentation_end_time = src_cl.Value.sequence.atc_seq[ii].stc_seq[jj].presentation_end_time;
                    }
                }

                dest_cl.Value.program.Value.num_prog = src_cl.Value.program.Value.num_prog;
                dest_cl.Value.program.Value.progs = Ref<CLPI_PROG>.Allocate(src_cl.Value.program.Value.num_prog);
                for (ii = 0; ii < src_cl.Value.program.Value.num_prog; ii++)
                {
                    dest_cl.Value.program.Value.progs[ii].spn_program_sequence_start = src_cl.Value.program.Value.progs[ii].spn_program_sequence_start;
                    dest_cl.Value.program.Value.progs[ii].program_map_pid = src_cl.Value.program.Value.progs[ii].program_map_pid;
                    dest_cl.Value.program.Value.progs[ii].num_streams = src_cl.Value.program.Value.progs[ii].num_streams;
                    dest_cl.Value.program.Value.progs[ii].num_groups = src_cl.Value.program.Value.progs[ii].num_groups;
                    dest_cl.Value.program.Value.progs[ii].streams = Ref<CLPI_PROG_STREAM>.Allocate(src_cl.Value.program.Value.progs[ii].num_streams);
                    for (jj = 0; jj < src_cl.Value.program.Value.progs[ii].num_streams; jj++)
                    {
                        dest_cl.Value.program.Value.progs[ii].streams[jj].coding_type = src_cl.Value.program.Value.progs[ii].streams[jj].coding_type;
                        dest_cl.Value.program.Value.progs[ii].streams[jj].pid = src_cl.Value.program.Value.progs[ii].streams[jj].pid;
                        dest_cl.Value.program.Value.progs[ii].streams[jj].format = src_cl.Value.program.Value.progs[ii].streams[jj].format;
                        dest_cl.Value.program.Value.progs[ii].streams[jj].rate = src_cl.Value.program.Value.progs[ii].streams[jj].rate;
                        dest_cl.Value.program.Value.progs[ii].streams[jj].aspect = src_cl.Value.program.Value.progs[ii].streams[jj].aspect;
                        dest_cl.Value.program.Value.progs[ii].streams[jj].oc_flag = src_cl.Value.program.Value.progs[ii].streams[jj].oc_flag;
                        dest_cl.Value.program.Value.progs[ii].streams[jj].cr_flag = src_cl.Value.program.Value.progs[ii].streams[jj].cr_flag;
                        dest_cl.Value.program.Value.progs[ii].streams[jj].dynamic_range_type = src_cl.Value.program.Value.progs[ii].streams[jj].dynamic_range_type;
                        dest_cl.Value.program.Value.progs[ii].streams[jj].color_space = src_cl.Value.program.Value.progs[ii].streams[jj].color_space;
                        dest_cl.Value.program.Value.progs[ii].streams[jj].hdr_plus_flag = src_cl.Value.program.Value.progs[ii].streams[jj].hdr_plus_flag;
                        dest_cl.Value.program.Value.progs[ii].streams[jj].char_code = src_cl.Value.program.Value.progs[ii].streams[jj].char_code;
                        dest_cl.Value.program.Value.progs[ii].streams[jj].lang = src_cl.Value.program.Value.progs[ii].streams[jj].lang;
                        Array.Copy(src_cl.Value.program.Value.progs[ii].streams[jj].isrc, dest_cl.Value.program.Value.progs[ii].streams[jj].isrc, 12);
                    }
                }

                dest_cl.Value.cpi.Value.num_stream_pid = src_cl.Value.cpi.Value.num_stream_pid;
                dest_cl.Value.cpi.Value.entry = Ref<CLPI_EP_MAP_ENTRY>.Allocate(src_cl.Value.cpi.Value.num_stream_pid);
                for (ii = 0; ii < dest_cl.Value.cpi.Value.num_stream_pid; ii++)
                {
                    dest_cl.Value.cpi.Value.entry[ii].pid = src_cl.Value.cpi.Value.entry[ii].pid;
                    dest_cl.Value.cpi.Value.entry[ii].ep_stream_type = src_cl.Value.cpi.Value.entry[ii].ep_stream_type;
                    dest_cl.Value.cpi.Value.entry[ii].num_ep_coarse = src_cl.Value.cpi.Value.entry[ii].num_ep_coarse;
                    dest_cl.Value.cpi.Value.entry[ii].num_ep_fine = src_cl.Value.cpi.Value.entry[ii].num_ep_fine;
                    dest_cl.Value.cpi.Value.entry[ii].ep_map_stream_start_addr = src_cl.Value.cpi.Value.entry[ii].ep_map_stream_start_addr;
                    dest_cl.Value.cpi.Value.entry[ii].coarse = Ref<CLPI_EP_COARSE>.Allocate(src_cl.Value.cpi.Value.entry[ii].num_ep_coarse);
                    for (jj = 0; jj < src_cl.Value.cpi.Value.entry[ii].num_ep_coarse; jj++)
                    {
                        dest_cl.Value.cpi.Value.entry[ii].coarse[jj].ref_ep_fine_id = src_cl.Value.cpi.Value.entry[ii].coarse[jj].ref_ep_fine_id;
                        dest_cl.Value.cpi.Value.entry[ii].coarse[jj].pts_ep = src_cl.Value.cpi.Value.entry[ii].coarse[jj].pts_ep;
                        dest_cl.Value.cpi.Value.entry[ii].coarse[jj].spn_ep = src_cl.Value.cpi.Value.entry[ii].coarse[jj].spn_ep;
                    }
                    dest_cl.Value.cpi.Value.entry[ii].fine = Ref<CLPI_EP_FINE>.Allocate(src_cl.Value.cpi.Value.entry[ii].num_ep_fine);
                    for (jj = 0; jj < src_cl.Value.cpi.Value.entry[ii].num_ep_fine; jj++)
                    {
                        dest_cl.Value.cpi.Value.entry[ii].fine[jj].is_angle_change_point = src_cl.Value.cpi.Value.entry[ii].fine[jj].is_angle_change_point;
                        dest_cl.Value.cpi.Value.entry[ii].fine[jj].i_end_position_offset = src_cl.Value.cpi.Value.entry[ii].fine[jj].i_end_position_offset;
                        dest_cl.Value.cpi.Value.entry[ii].fine[jj].pts_ep = src_cl.Value.cpi.Value.entry[ii].fine[jj].pts_ep;
                        dest_cl.Value.cpi.Value.entry[ii].fine[jj].spn_ep = src_cl.Value.cpi.Value.entry[ii].fine[jj].spn_ep;
                    }
                }

                dest_cl.Value.clip.font_info.Value.font_count = src_cl.Value.clip.font_info.Value.font_count;
                if (dest_cl.Value.clip.font_info.Value.font_count != 0)
                {
                    dest_cl.Value.clip.font_info.Value.font = Ref<CLPI_FONT>.Allocate(dest_cl.Value.clip.font_info.Value.font_count);
                    src_cl.Value.clip.font_info.Value.font.AsSpan().Slice(0, dest_cl.Value.clip.font_info.Value.font_count).CopyTo(dest_cl.Value.clip.font_info.Value.font.AsSpan());
                }
            }

            return dest_cl;
        }
    }
}
