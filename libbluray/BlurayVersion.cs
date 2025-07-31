using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray
{
    public static class BlurayVersion
    {

        public static UInt32 BLURAY_VERSION_CODE(uint major, uint minor, uint micro) => (major * 10000) + (minor * 100) + (micro * 1);

        public const uint BLURAY_VERSION_MAJOR = 1;

        public const uint BLURAY_VERSION_MINOR = 3;

        public const uint BLURAY_VERSION_MICRO = 4;

        public static string BLURAY_VERSION_STRING => $"{BLURAY_VERSION_MAJOR}.{BLURAY_VERSION_MINOR}.{BLURAY_VERSION_MICRO}";
        public static uint BLURAY_VERSION => BLURAY_VERSION_CODE(BLURAY_VERSION_MAJOR, BLURAY_VERSION_MINOR, BLURAY_VERSION_MICRO);

    }
}
