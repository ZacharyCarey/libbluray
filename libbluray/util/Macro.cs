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

        public static ushort MKINT_BE16(ReadOnlySpan<byte> data)
        {
            return (ushort)((data[0] << 8) | data[1]);
        }

        public static uint MKINT_BE24(ReadOnlySpan<byte> data)
        {
            return ((uint)data[0] << 16) | ((uint)data[1] << 8) | (uint)data[2];
        }

        public static uint MKINT_BE32(ReadOnlySpan<byte> data)
        {
            return ((uint)data[0] << 24) | ((uint)data[1] << 16) | ((uint)data[2] << 8) | (uint)data[3];
        }

        public static T BD_MIN<T>(T a, T b) where T : IComparisonOperators<T, T, bool>
        {
            return (a < b) ? a : b;
        }

        public static T BD_MAX<T>(T a, T b) where T : IComparisonOperators<T, T, bool>
        {
            return (a > b) ? a : b;
        }

        public const Int64 BD_MAX_SSIZE = Int64.MaxValue; 

    }
}
