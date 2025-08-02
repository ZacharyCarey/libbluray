using libbluray.disc;
using libbluray.file;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.bdnav
{
    internal struct SOUND_OBJECT
    {
        public UInt32 sample_rate;
        public byte num_channels;
        public byte bits_per_sample;

        public UInt32 num_frames;

        /// <summary>
        /// LPCM, interleaved
        /// </summary>
        public Ref<UInt16> samples = new(); 

        public SOUND_OBJECT() { }
    }

    internal struct SOUND_DATA
    {
        public UInt16 num_sounds;
        public Ref<SOUND_OBJECT> sounds = new();

        public SOUND_DATA() { }
    }

    internal static class SoundParse
    {
        private const UInt32 BCLK_SIG1 = ('B' << 24) | ('C' << 16) | ('L' << 8) | 'K';
        static bool _bclk_parse_header(Ref<BITSTREAM> bs, Ref<UInt32> data_start, Ref<UInt32> extension_data_start)
        {
            if (!BdmvParse.bdmv_parse_header(bs, BCLK_SIG1, Ref<uint>.Null))
            {
                return false;
            }

            if (bs.Value.bs_avail() < 2 * 32)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, "_parse_header: unexpected end of file");
                return false;
            }

            data_start.Value = bs.Value.bs_read<UInt32>(32);
            extension_data_start.Value = bs.Value.bs_read<UInt32>(32);

            return true;
        }

        static bool _sound_parse_attributes(Ref<BITSTREAM> bs, Ref<SOUND_OBJECT> obj)
        {
            int i;

            switch (i = bs.Value.bs_read<byte>(4))
            {
                case 1:
                    obj.Value.num_channels = 1;
                    break;
                case 3:
                    obj.Value.num_channels = 2;
                    break;
                default:
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV, $"unknown channel configuration code {i}");
                    obj.Value.num_channels = 1;
                    break;
            };
            switch (i = bs.Value.bs_read<byte>(4))
            {
                case 1:
                    obj.Value.sample_rate = 48000;
                    break;
                default:
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV, $"unknown sample rate code {i}");
                    obj.Value.sample_rate = 48000;
                    break;
            };
            switch (i = bs.Value.bs_read<byte>(2))
            {
                case 1:
                    obj.Value.bits_per_sample = 16;
                    break;
                default:
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV, $"unknown bits per sample code {i}");
                    obj.Value.bits_per_sample = 16;
                    break;
            };

            bs.Value.bs_skip(6); /* padding */

            return true;
        }

        static bool _sound_parse_index(Ref<BITSTREAM> bs, Ref<UInt32> sound_data_index, Ref<SOUND_OBJECT> obj)
        {
            if (!_sound_parse_attributes(bs, obj))
            {
                return false;
            }

            sound_data_index.Value = bs.Value.bs_read<UInt32>(32);
            obj.Value.num_frames = bs.Value.bs_read<UInt32>(32);
            obj.Value.num_frames /= (uint)(obj.Value.bits_per_sample / 8) * obj.Value.num_channels;

            return true;
        }

        static bool _sound_read_samples(Ref<BITSTREAM> bs, Ref<SOUND_OBJECT> obj)
        {
            UInt32 n;
            UInt32 num_samples = obj.Value.num_frames * obj.Value.num_channels;

            if (num_samples == 0)
            {
                return true;
            }

            if (bs.Value.bs_avail() / 16 < num_samples)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, "sound.bdmv: unexpected EOF");
                return false;
            }

            obj.Value.samples = Ref<UInt16>.Allocate(num_samples);
            for (n = 0; n < num_samples; n++)
            {
                obj.Value.samples[n] = bs.Value.bs_read<ushort>(16);
            }

            return true;
        }

        internal static void sound_free(ref Ref<SOUND_DATA> p)
        {
            if (p)
            {
                if (p.Value.sounds)
                {
                    uint i;
                    for (i = 0; i < p.Value.num_sounds; i++)
                    {
                        p.Value.sounds[i].samples.Free();
                    }

                    p.Value.sounds.Free();
                }
                p.Free();
            }
        }

        static Ref<SOUND_DATA> _sound_parse(BD_FILE_H fp)
        {
            Variable<BITSTREAM> bs = new();
            Ref<SOUND_DATA> data = Ref<SOUND_DATA>.Null;
            UInt16 num_sounds;
            UInt32 data_len;
            int i;
            Variable<UInt32> data_start = new(), extension_data_start = new();
            Ref<UInt32> data_offsets = Ref<uint>.Null;

            if (bs.Value.bs_init(fp) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, "sound.bdmv: read error");
                goto error;
            }

            if (!_bclk_parse_header(bs.Ref, data_start.Ref, extension_data_start.Ref))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, "invalid header");
                goto error;
            }

            if (bs.Value.bs_seek_byte(40) < 0)
            {
                goto error;
            }

            data_len = bs.Value.bs_read<UInt32>(32);
            bs.Value.bs_skip(8); /* reserved */
            num_sounds = bs.Value.bs_read<byte>(8);

            if (data_len < 1 || num_sounds < 1)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, "empty database");
                goto error;
            }

            data_offsets = Ref<uint>.Allocate(num_sounds);
            data = Ref<SOUND_DATA>.Allocate();
            data.Value.num_sounds = num_sounds;
            data.Value.sounds = Ref<SOUND_OBJECT>.Allocate(num_sounds);

            /* parse headers */

            for (i = 0; i < data.Value.num_sounds; i++)
            {
                if (!_sound_parse_index(bs.Ref, data_offsets + i, data.Value.sounds.AtIndex(i)))
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error parsing sound {i} attributes");
                    goto error;
                }
            }

            /* read samples */

            for (i = 0; i < data.Value.num_sounds; i++)
            {

                if (bs.Value.bs_seek_byte(data_start.Value + data_offsets[i]) < 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error reading samples for sound {i}");
                    data.Value.sounds[i].num_frames = 0;
                    continue;
                }

                if (!_sound_read_samples(bs.Ref, data.Value.sounds.AtIndex(i)))
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error reading samples for sound {i}");
                    goto error;
                }
            }

            data_offsets.Free();
            return data;

        error:
            sound_free(ref data);
            data_offsets.Free();
            return Ref<SOUND_DATA>.Null;
        }

        internal static Ref<SOUND_DATA> sound_get(BD_DISC disc)
        {
            BD_FILE_H? fp = null;
            Ref<SOUND_DATA> p;

            // There's no backup copy for sound.bdmv

            fp = disc.disc_open_path(Path.Combine("BDMV", "AUXDATA", "sound.bdmv"));
            if (fp == null) return Ref<SOUND_DATA>.Null;

            p = _sound_parse(fp);
            fp.file_close();
            return p;
        }
    }
}
