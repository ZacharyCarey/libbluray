using libbluray.file;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.util
{
    internal struct BITBUFFER
    {
        public Ref<byte> p_start = new();
        public Ref<byte> p = new();
        public Ref<byte> p_end = new();

        public int i_left; // Number of available bits

        public BITBUFFER() { }

        public void bb_init(Ref<byte> p_data, UInt64 i_data)
        {
            this.p_start = p_data;
            this.p = this.p_start;
            this.p_end = this.p_start + i_data;
            this.i_left = 8;
        }

        public void bb_skip(UInt64 i_count)
        {
            this.p += (i_count >> 3);
            this.i_left -= (int)(i_count & 0x07);

            if (this.i_left <= 0)
            {
                this.p++;
                this.i_left += 8;
            }
        }
        private static readonly UInt32[] i_mask = new UInt32[33] {
            0x00,
            0x01,      0x03,      0x07,      0x0f,
            0x1f,      0x3f,      0x7f,      0xff,
            0x1ff,     0x3ff,     0x7ff,     0xfff,
            0x1fff,    0x3fff,    0x7fff,    0xffff,
            0x1ffff,   0x3ffff,   0x7ffff,   0xfffff,
            0x1fffff,  0x3fffff,  0x7fffff,  0xffffff,
            0x1ffffff, 0x3ffffff, 0x7ffffff, 0xfffffff,
            0x1fffffff,0x3fffffff,0x7fffffff,0xffffffff
        };
        public T bb_read<T>(int i_count) where T : INumberBase<T>, IBitwiseOperators<T, T, T>
        {
            int i_shr;
            T i_result = T.Zero;

            while (i_count > 0)
            {
                if (this.p >= this.p_end)
                {
                    break;
                }

                i_shr = this.i_left - i_count;
                if (i_shr >= 0)
                {
                    // more in the buffer than requested
                    i_result |= (T)Convert.ChangeType(((uint)this.p.Value >> i_shr) & i_mask[i_count], typeof(T));
                    this.i_left -= i_count;
                    if (this.i_left == 0)
                    {
                        this.p++;
                        this.i_left = 8;
                    }
                    return i_result;
                } else
                {
                    // less in the buffer than requested
                    i_result |= (T)Convert.ChangeType((uint)(this.p.Value & i_mask[i_left]) << -i_shr, typeof(T));
                    i_count -= this.i_left;
                    this.p++;
                    this.i_left = 8;
                }
            }

            return i_result;
        }

        public bool bb_readbool()
        {
            byte b = bb_read<byte>(1);
            return b != 0;
        }

        public Int64 bb_pos()
        {
            return 8 * (this.p - this.p_start) + 8 - this.i_left;
        }
        public bool bb_eof()
        {
            return this.p >= this.p_end ? true : false;
        }
        public void bb_read_bytes(Span<byte> buf, int i_count)
        {
            for (int ii = 0; ii < i_count; ii++)
            {
                buf[ii] = bb_read<byte>(8);
            }
        }
        public void bb_read_bytes(Ref<byte> buf, int i_count)
        {
            for (int ii = 0; ii < i_count; ii++)
            {
                buf[ii] = bb_read<byte>(8);
            }
        }
        public string bb_read_string(int i_count)
        {
            byte[] buf = new byte[i_count];
            bb_read_bytes(buf, i_count);
            return Encoding.ASCII.GetString(buf).TrimEnd('\0');
        }
        public T bb_show<T>(int i_count) where T : INumberBase<T>, IBitwiseOperators<T, T, T>
        {
            BITBUFFER temp = this; // Make a copy of the current state
            return temp.bb_read<T>(i_count);
        }
        public bool bb_is_align(UInt32 mask)
        {
            Int64 off = bb_pos();
            return (off & mask) == 0;
        }
    }

    internal struct BITSTREAM
    {
        public const Int64 BF_BUF_SIZE = 1024 * 32;

        public BD_FILE_H fp = null;
        public byte[] buf = new byte[BF_BUF_SIZE];
        public BITBUFFER bb = new();
        public Int64 pos; // File offset of buffer start buf[0]
        public Int64 end; // size of file
        public UInt64 size; // bytes in buf

        public BITSTREAM() { }

        public int bs_init(BD_FILE_H fp)
        {
            Int64 size = fp.file_size();
            this.fp = fp;
            this.pos = 0;
            this.end = (size < 0) ? 0 : size;

            return this._bs_read();
        }
        public int bs_seek_byte(Int64 off)
        {
            return this._bs_seek(off << 3, SeekOrigin.Begin);
        }
        public void bs_skip(UInt64 i_count) // Note: i_count must be less than BF_BUF_SIZE
        {
            int left;
            UInt64 bytes = (i_count + 7) >> 3;

            if (this.bb.p + bytes >= this.bb.p_end)
            {
                this.pos = this.pos + (this.bb.p - this.bb.p_start);
                left = this.bb.i_left;
                this.fp.file_seek(this.pos, SeekOrigin.Begin);
                this.size = this.fp.file_read(this.buf, BF_BUF_SIZE);
                this.bb.bb_init(new Ref<byte>(this.buf), this.size);
                this.bb.i_left = left;
            }
            this.bb.bb_skip(i_count);
        }
        public T bs_read<T>(int i_count) where T : INumberBase<T>, IBitwiseOperators<T, T, T>
        {
            int left;
            int bytes = (i_count + 7) >> 3;

            if (this.bb.p + bytes >= this.bb.p_end)
            {
                this.pos = this.pos + (this.bb.p - this.bb.p_start);
                left = this.bb.i_left;
                this.fp.file_seek(this.pos, SeekOrigin.Begin);
                this.size = this.fp.file_read(this.buf, BF_BUF_SIZE);
                this.bb.bb_init(new Ref<byte>(this.buf), this.size);
                this.bb.i_left = left;
            }
            return this.bb.bb_read<T>(i_count);
        }
        public Int64 bs_pos()
        {
            return this.pos * 8 + this.bb.bb_pos();
        }
        public Int64 bs_end()
        {
            return this.end * 8;
        }
        public Int64 bs_avail()
        {
            return this.bs_end() - this.bs_pos();
        }
        public void bs_read_bytes(Span<byte> buf, int i_count)
        {
            for (int ii = 0; ii < i_count; ii++)
            {
                buf[ii] = bs_read<byte>(8);
            }
        }
        public void bs_read_bytes(Ref<byte> buf, int i_count)
        {
            for (int ii = 0; ii < i_count; ii++)
            {
                buf[ii] = bs_read<byte>(8);
            }
        }
        public string bs_read_string(int i_count)
        {
            byte[] buf = new byte[i_count];
            bs_read_bytes(buf, i_count);
            return Encoding.ASCII.GetString(buf).TrimEnd('\0');
        }
        public void bs_read_string(Ref<byte> buf, int i_count)
        {
            bs_read_bytes(buf, i_count);
            buf[i_count] = 0;
        }
        public bool bs_is_align(UInt32 mask)
        {
            Int64 off = bs_pos();
            return (off & mask) == 0;
        }
        private int _bs_read()
        {
            int result = 0;
            Int64 got;

            got = (long)fp.file_read(this.buf, (ulong)BF_BUF_SIZE);
            if (got <= 0 || got > BF_BUF_SIZE)
            {
                Logging.bd_debug("_bs_read(): read error");
                got = 0;
                result = -1;
            }

            this.size = (UInt64)got;
            this.bb.bb_init(new Ref<byte>(this.buf), size);

            return result;
        }
        private int _bs_read_at(Int64 off)
        {
            if (this.fp.file_seek(off, SeekOrigin.Begin) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, "bs_read(): seek failed");
                // No change in state. Caller _must_ check return value
                return -1;
            }
            this.pos = off;
            return this._bs_read();
        }
        private int _bs_seek(Int64 off, SeekOrigin whence)
        {
            int result = 0;
            Int64 b;

            switch (whence)
            {
                case SeekOrigin.Current:
                    off = this.pos * 8 + (this.bb.p - this.bb.p_start) * 8 + off;
                    break;
                case SeekOrigin.End:
                    off = this.end * 8 - off;
                    break;
                case SeekOrigin.Begin:
                default:
                    break;
            }
            if (off < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, "bs_seek():  seek failed (negative offset)");
                return -1;
            }

            b = off >> 3;
            if (b >= this.end)
            {
                Int64 pos;
                if (BF_BUF_SIZE < this.end)
                {
                    pos = this.end - BF_BUF_SIZE;
                }
                else
                {
                    pos = 0;
                }
                result = this._bs_read_at(pos);
                this.bb.p = this.bb.p_end;
            }
            else if (b < this.pos || b >= (this.pos + BF_BUF_SIZE))
            {
                result = this._bs_read_at(b);
            } else
            {
                b -= this.pos;
                this.bb.p = this.bb.p_start.AtIndex(b);
                this.bb.i_left = 8 - (byte)(off & 0x07);
            }

            return result;
        }
    }
}
