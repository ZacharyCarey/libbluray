using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.util
{ 
    internal partial class Util
    {
        public static int memcmp(Ref<byte> a, Ref<byte> b, long len) => memcmp(a, b, (ulong)len);
        public static int memcmp(Ref<byte> a, Ref<byte> b, ulong len)
        {
            for (ulong i = 0; i < len; i++)
            {
                int result = (int)a[i] - (int)b[i];
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }

        /*public static Ref<T> memcpy<T>(Ref<T> dst, Ref<T> src, long len) where T : struct
        {
            for (long i = 0; i < len; i++)
            {
                dst[i] = src[i];
            }

            return dst;
        }

        public delegate void DeepCopyFunc<T>(ref T dst, ref T src) where T : struct;

        public static Ref<T> memset<T>(Ref<T> dest, ref T value, UInt64 count, DeepCopyFunc<T> deepCopyfunc) where T : struct
        {
            for (ulong i = 0; i <= count; i++)
            {
                deepCopyfunc(ref dest[i], ref value);
            }

            return dest;
        }

        public static Ref<T> memset<T>(Ref<T> dest, T value, UInt64 count) where T : struct, INumberBase<T>
        {
            for (ulong i = 0; i <= count; i++)
            {
                dest[i] = value;
            }

            return dest;
        }
        */
    }
}
