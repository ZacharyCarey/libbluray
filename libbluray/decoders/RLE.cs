using libbluray.decoders;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{
    public struct RLE_ENC
    {
        public Ref<BD_PG_RLE_ELEM> elem;     /* current element */
        public uint free_elem;/* unused element count */
        public uint num_elem; /* allocated element count */

        public int error;
    }

    public static class RLE
    {
        public static int rle_begin(Ref<RLE_ENC> p)
        {
            p.Value.num_elem = 1024;
            p.Value.free_elem = 1024;
            p.Value.elem = Ref<BD_PG_RLE_ELEM>.Allocate(p.Value.num_elem);
            if (!p.Value.elem)
            {
                return -1;
            }
            p.Value.elem.Value.len = 0;
            p.Value.elem.Value.color = 0xffff;

            p.Value.error = 0;

            return 0;
        }

        public static Ref<BD_PG_RLE_ELEM> rle_get(Ref<RLE_ENC> p)
        {
            Ref<BD_PG_RLE_ELEM> start = (p.Value.elem ? p.Value.elem - (p.Value.num_elem - p.Value.free_elem) : Ref<BD_PG_RLE_ELEM>.Null);
            if (p.Value.error != 0)
            {
                if (start)
                {
                    p.Value.elem = Ref<BD_PG_RLE_ELEM>.Null;
                }
                return Ref<BD_PG_RLE_ELEM>.Null;
            }
            return start;
        }

        public static void rle_end(Ref<RLE_ENC> p)
        {
            Ref<BD_PG_RLE_ELEM> start = rle_get(p);
            if (start)
            {
                //bd_refcnt_dec(start);
            }
            p.Value.elem = Ref<BD_PG_RLE_ELEM>.Null;
        }

        /*
 * util
 */

        static int _rle_ensure_size(Ref<RLE_ENC> p)
        {
            if (p.Value.free_elem == 0)
            {
                Ref<BD_PG_RLE_ELEM> start = rle_get(p);
                if (p.Value.error != 0)
                {
                    return -1;
                }
                /* realloc to 2x */
                var tmp = start.Reallocate(p.Value.num_elem * 2);
                if (!tmp)
                {
                    p.Value.error = 1;
                    return -1;
                }
                start = tmp;
                p.Value.elem = start + p.Value.num_elem;
                p.Value.free_elem = p.Value.num_elem;
                p.Value.num_elem *= 2;
            }

            return 0;
        }

        /*
         * crop encoded image
         */

        static int _enc_elem(Ref<RLE_ENC> p, UInt16 color, UInt16 len)
        {
            if (_rle_ensure_size(p) < 0)
            {
                return -1;
            }

            p.Value.elem.Value.color = color;
            p.Value.elem.Value.len = len;

            p.Value.free_elem--;
            p.Value.elem++;

            return 0;
        }

        static int _enc_eol(Ref<RLE_ENC> p)
        {
            return _enc_elem(p, 0, 0);
        }

        internal static Ref<BD_PG_RLE_ELEM> rle_crop_object(Ref<BD_PG_RLE_ELEM> orig, int width,
                                int crop_x, int crop_y, int crop_w, int crop_h)
        {
            Variable<RLE_ENC> rle = new();
            int x0 = crop_x;
            int x1 = crop_x + crop_w; /* first pixel outside of cropped region */
            int x, y;

            if (rle_begin(rle.Ref) < 0)
            {
                return Ref<BD_PG_RLE_ELEM>.Null;
            }

            /* skip crop_y */
            for (y = 0; y < crop_y; y++)
            {
                for (x = 0; x < width; x += orig.Value.len, orig++) ;
            }

            /* crop lines */

            for (y = 0; y < crop_h; y++)
            {
                for (x = 0; x < width;)
                {
                    BD_PG_RLE_ELEM bite = (orig++).Value;

                    if (bite.len < 1)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_GC | DebugMaskEnum.DBG_CRIT, $"rle eol marker in middle of line (x={x}/{width})");
                        continue;
                    }

                    /* starts outside, ends outside */
                    if (x + bite.len < x0 || x >= x1)
                    {
                        x += bite.len;
                        continue;
                    }

                    /* starts before ? */
                    if (x < x0)
                    {
                        bite.len -= (ushort)(x0 - x);
                        x = x0;
                    }

                    x += bite.len;

                    /* ends after ? */
                    if (x >= x1)
                    {
                        bite.len -= (ushort)(x - x1);
                    }

                    if (_enc_elem(rle.Ref, bite.color, bite.len) < 0)
                    {
                        goto _out;
                    }
                }

                if (orig.Value.len == 0)
                {
                    /* skip eol marker */
                    orig++;
                }
                else
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_GC | DebugMaskEnum.DBG_CRIT, "rle eol marker missing");
                }

                if (_enc_eol(rle.Ref) < 0)
                {
                    goto _out;
                }
            }

        _out:
            return rle_get(rle.Ref);
        }

        /*
         * compression
         */

        static int _rle_grow(Ref<RLE_ENC> p)
        {
            p.Value.free_elem--;
            p.Value.elem++;

            if (_rle_ensure_size(p) < 0)
            {
                return -1;
            }

            p.Value.elem.Value.len = 0;

            return 0;
        }

        internal static int rle_add_eol(Ref<RLE_ENC> p)
        {
            if (p.Value.elem.Value.len != 0)
            {
                if (_rle_grow(p) < 0)
                {
                    return -1;
                }
            }
            p.Value.elem.Value.color = 0;

            if (_rle_grow(p) < 0)
            {
                return -1;
            }
            p.Value.elem.Value.color = 0xffff;

            return 0;
        }

        internal static int rle_add_bite(Ref<RLE_ENC> p, byte color, int len)
        {
            if (color == p.Value.elem.Value.color)
            {
                p.Value.elem.Value.len += (ushort)len;
            }
            else
            {
                if (p.Value.elem.Value.len != 0)
                {
                    if (_rle_grow(p) < 0)
                    {
                        return -1;
                    }
                }
                p.Value.elem.Value.color = color;
                p.Value.elem.Value.len = (ushort)len;
            }

            return 0;
        }

        internal static int rle_compress_chunk(Ref<RLE_ENC> p, Ref<byte> mem, uint width)
        {
            uint ii;
            for (ii = 0; ii < width; ii++)
            {
                if (rle_add_bite(p, mem[ii], 1) < 0)
                {
                    return -1;
                }
            }
            return 0;
        }

    }
}
