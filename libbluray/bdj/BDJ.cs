using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.bdj
{
    /// <summary>
    /// these must be in sync with Libbluray.java !
    /// </summary>
    internal enum BDJ_EVENT
    {
        BDJ_EVENT_NONE = 0,

        /* Application control */

        BDJ_EVENT_START = 1, /* param: title number */
        BDJ_EVENT_STOP = 2,
        BDJ_EVENT_PSR102 = 3,

        /* Playback status */

        BDJ_EVENT_PLAYLIST = 4,
        BDJ_EVENT_PLAYITEM = 5,
        BDJ_EVENT_CHAPTER = 6,
        BDJ_EVENT_MARK = 7,
        BDJ_EVENT_PTS = 8,
        BDJ_EVENT_END_OF_PLAYLIST = 9,

        BDJ_EVENT_SEEK = 10,
        BDJ_EVENT_RATE = 11,

        BDJ_EVENT_ANGLE = 12,
        BDJ_EVENT_AUDIO_STREAM = 13,
        BDJ_EVENT_SUBTITLE = 14,
        BDJ_EVENT_SECONDARY_STREAM = 15,

        /* User interaction */

        BDJ_EVENT_VK_KEY = 16,
        BDJ_EVENT_UO_MASKED = 17,
        BDJ_EVENT_MOUSE = 18,

        BDJ_EVENT_LAST = 18,
    }

    internal struct BDJ_CONFIG
    {
        /// <summary>
        /// BD-J Xlet persistent storage
        /// </summary>
        public string persistent_root;

        /// <summary>
        /// BD-J binding unit data area
        /// </summary>
        public string cache_root;

        /// <summary>
        /// JAVA_HOME override from application
        /// </summary>
        public string java_home;

        /// <summary>
        /// BD-J implementation class path (location of libbluray.jar)
        /// </summary>
        public string[] classpath = new string[2];

        /// <summary>
        /// disable persistent storage (remove files at close)
        /// </summary>
        public byte no_persistent_storage; 

        public BDJ_CONFIG() { }
    }

    internal enum BdjStatus
    {
        BDJ_CHECK_OK = 0,
        BDJ_CHECK_NO_JVM = 1,
        BDJ_CHECK_NO_JAR = 2,
    }

    internal struct BDJAVA
    {

    }

    /// <summary>
    /// BDJ is not currently supported as it requires Java, so this class is largly
    /// stubbed out.
    /// </summary>
    internal static class BDJ
    {

        public static BdjStatus bdj_jvm_available(BDJ_CONFIG storage)
        {
            string java_home = null;
            object? jvm_lib = null; //_load_jvm(&java_home, storage->java_home);
            //if (jvm_lib == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BDJ | DebugMaskEnum.DBG_CRIT, "BD-J check: Failed to load JVM library");
                return BdjStatus.BDJ_CHECK_NO_JVM;
            }
            /*dl_dlclose(jvm_lib);

            if (!_find_libbluray_jar(storage))
            {
                BD_DEBUG(DBG_BDJ | DBG_CRIT, "BD-J check: Failed to load libbluray.jar\n");
                return BDJ_CHECK_NO_JAR;
            }

            BD_DEBUG(DBG_BDJ, "BD-J check: OK\n");

            return BDJ_CHECK_OK;*/
        }

        public static Ref<BDJAVA> bdj_open(string path, Ref<BLURAY> bd, string bdj_disc_id, ref BDJ_CONFIG cfg)
        {
            return Ref<BDJAVA>.Null;
        }

        public static int bdj_process_event(Ref<BDJAVA> bdjava, BDJ_EVENT ev, uint param)
        {
            return -1;
        }

        public static void bdj_close(Ref<BDJAVA> bdjava)
        {
            return;
        }

        public static void bdj_config_cleanup(ref BDJ_CONFIG p)
        {

        }
    }
}
