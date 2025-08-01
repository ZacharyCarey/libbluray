using libbluray.bdj;
using libbluray.bdnav;
using libbluray.decoders;
using libbluray.disc;
using libbluray.file;
using libbluray.hdmv;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray
{
    public struct BLURAY_TITLE
    {
        public string name;         /**< optional title name in preferred language */
        public byte interactive;  /**< 1 if title is interactive (title length and playback position should not be shown in UI) */
        public byte accessible;   /**< 1 if it is allowed to jump into this title */
        public byte hidden;       /**< 1 if title number should not be shown during playback */

        public byte bdj;          /**< 0 - HDMV title. 1 - BD-J title */
        public UInt32 id_ref;       /**< Movie Object number / bdjo file number */
    }

    public struct BLURAY_DISC_INFO
    {
        public byte bluray_detected;   /**< 1 if BluRay disc was detected */

        /* Disc ID */
        public string disc_name;      /**< optional disc name in preferred language */
        public string udf_volume_id;  /**< optional UDF volume identifier */
        public byte[] disc_id = new byte[20];    /**< Disc ID */

        /** HDMV / BD-J titles */
        public byte no_menu_support;            /**< 1 if this disc can't be played using on-disc menus */
        public byte first_play_supported;       /**< 1 if First Play title is present on the disc and can be played */
        public byte top_menu_supported;         /**< 1 if Top Menu title is present on the disc and can be played */

        public UInt32 num_titles;     /**< number of titles on the disc, not including "First Play" and "Top Menu" */
        public Ref<BLURAY_TITLE> titles;  /**< index is title number 1 ... N */
        public Ref<BLURAY_TITLE> first_play;     /**< titles[N+1].   null if not present on the disc. */
        public Ref<BLURAY_TITLE> top_menu;       /**< titles[0]. null if not present on the disc. */

        public UInt32 num_hdmv_titles;            /**< number of HDMV titles */
        public UInt32 num_bdj_titles;             /**< number of BD-J titles */
        public UInt32 num_unsupported_titles;     /**< number of unsupported titles */

        /** BD-J info (valid only if disc uses BD-J) */
        public byte bdj_detected;     /**< 1 if disc uses BD-J */
        public byte bdj_supported;    /**< (deprecated) */
        public byte libjvm_detected;  /**< 1 if usable Java VM was found */
        public byte bdj_handled;      /**< 1 if usable Java VM + libbluray.jar was found */

        public string bdj_org_id = "";        /**< (BD-J) disc organization ID */
        public string bdj_disc_id = "";      /**< (BD-J) disc ID */

        /* disc application info */
        public byte video_format;                     /**< \ref bd_video_format_e */
        public byte frame_rate;                       /**< \ref bd_video_rate_e */
        public byte content_exist_3D;                 /**< 1 if 3D content exists on the disc */
        public byte initial_output_mode_preference;   /**< 0 - 2D, 1 - 3D */
        public string provider_data = "";                /**< Content provider data */

        /* AACS info  (valid only if disc uses AACS) */
        public byte aacs_detected;     /**< 1 if disc is using AACS encoding */
        public byte libaacs_detected;  /**< 1 if usable AACS decoding library was found */
        public byte aacs_handled;      /**< 1 if disc is using supported AACS encoding */

        public int aacs_error_code;   /**< AACS error code (BD_AACS_*) */
        public int aacs_mkbv;         /**< AACS MKB version */

        /* BD+ info  (valid only if disc uses BD+) */
        public byte bdplus_detected;     /**< 1 if disc is using BD+ encoding */
        public byte libbdplus_detected;  /**< 1 if usable BD+ decoding library was found */
        public byte bdplus_handled;      /**< 1 if disc is using supporred BD+ encoding */

        public byte bdplus_gen;          /**< BD+ content code generation */
        public UInt32 bdplus_date;         /**< BD+ content code relese date ((year<<16)|(month<<8)|day) */

        /* disc application info (libbluray > 1.2.0) */
        public byte initial_dynamic_range_type; /**< bd_dynamic_range_type_e */

        public BLURAY_DISC_INFO() { }
    }

    public enum bd_stream_type_e
    {
        BLURAY_STREAM_TYPE_VIDEO_MPEG1 = 0x01,
        BLURAY_STREAM_TYPE_VIDEO_MPEG2 = 0x02,
        BLURAY_STREAM_TYPE_AUDIO_MPEG1 = 0x03,
        BLURAY_STREAM_TYPE_AUDIO_MPEG2 = 0x04,
        BLURAY_STREAM_TYPE_AUDIO_LPCM = 0x80,
        BLURAY_STREAM_TYPE_AUDIO_AC3 = 0x81,
        BLURAY_STREAM_TYPE_AUDIO_DTS = 0x82,
        BLURAY_STREAM_TYPE_AUDIO_TRUHD = 0x83,
        BLURAY_STREAM_TYPE_AUDIO_AC3PLUS = 0x84,
        BLURAY_STREAM_TYPE_AUDIO_DTSHD = 0x85,
        BLURAY_STREAM_TYPE_AUDIO_DTSHD_MASTER = 0x86,
        BLURAY_STREAM_TYPE_VIDEO_VC1 = 0xea,
        BLURAY_STREAM_TYPE_VIDEO_H264 = 0x1b,
        BLURAY_STREAM_TYPE_VIDEO_HEVC = 0x24,
        BLURAY_STREAM_TYPE_SUB_PG = 0x90,
        BLURAY_STREAM_TYPE_SUB_IG = 0x91,
        BLURAY_STREAM_TYPE_SUB_TEXT = 0x92,
        BLURAY_STREAM_TYPE_AUDIO_AC3PLUS_SECONDARY = 0xa1,
        BLURAY_STREAM_TYPE_AUDIO_DTSHD_SECONDARY = 0xa2
    }

    public enum bd_video_format_e
    {
        BLURAY_VIDEO_FORMAT_480I = 1,  /**< ITU-R BT.601-5 */
        BLURAY_VIDEO_FORMAT_576I = 2,  /**< ITU-R BT.601-4 */
        BLURAY_VIDEO_FORMAT_480P = 3,  /**< SMPTE 293M */
        BLURAY_VIDEO_FORMAT_1080I = 4,  /**< SMPTE 274M */
        BLURAY_VIDEO_FORMAT_720P = 5,  /**< SMPTE 296M */
        BLURAY_VIDEO_FORMAT_1080P = 6,  /**< SMPTE 274M */
        BLURAY_VIDEO_FORMAT_576P = 7,  /**< ITU-R BT.1358 */
        BLURAY_VIDEO_FORMAT_2160P = 8,  /**< BT.2020 */
    }

    public enum bd_video_rate_e
    {
        BLURAY_VIDEO_RATE_24000_1001 = 1,  /**< 23.976 Hz */
        BLURAY_VIDEO_RATE_24 = 2,  /**< 24 Hz */
        BLURAY_VIDEO_RATE_25 = 3,  /**< 25 Hz */
        BLURAY_VIDEO_RATE_30000_1001 = 4,  /**< 29.97 Hz */
        BLURAY_VIDEO_RATE_50 = 6,  /**< 50 Hz */
        BLURAY_VIDEO_RATE_60000_1001 = 7   /**< 59.94 Hz */
    }

    public enum bd_video_aspect_e
    {
        BLURAY_ASPECT_RATIO_4_3 = 2,
        BLURAY_ASPECT_RATIO_16_9 = 3
    }

    public enum bd_audio_format_e
    {
        BLURAY_AUDIO_FORMAT_MONO = 1,
        BLURAY_AUDIO_FORMAT_STEREO = 3,
        BLURAY_AUDIO_FORMAT_MULTI_CHAN = 6,
        BLURAY_AUDIO_FORMAT_COMBO = 12  // Stereo ac3/dts, 
    }

    public enum bd_audio_rate_e
    {
        BLURAY_AUDIO_RATE_48 = 1,
        BLURAY_AUDIO_RATE_96 = 4,
        BLURAY_AUDIO_RATE_192 = 5,
        BLURAY_AUDIO_RATE_192_COMBO = 12, // 48 or 96 ac3/dts
                                          // 192 mpl/dts-hd
        BLURAY_AUDIO_RATE_96_COMBO = 14  // 48 ac3/dts
                                         // 96 mpl/dts-hd
    }

    public enum bd_char_code_e
    {
        BLURAY_TEXT_CHAR_CODE_UTF8 = 0x01,
        BLURAY_TEXT_CHAR_CODE_UTF16BE = 0x02,
        BLURAY_TEXT_CHAR_CODE_SHIFT_JIS = 0x03,
        BLURAY_TEXT_CHAR_CODE_EUC_KR = 0x04,
        BLURAY_TEXT_CHAR_CODE_GB18030_20001 = 0x05,
        BLURAY_TEXT_CHAR_CODE_CN_GB = 0x06,
        BLURAY_TEXT_CHAR_CODE_BIG5 = 0x07
    }

    public enum bd_still_mode_e
    {
        BLURAY_STILL_NONE = 0x00,  /**< No still (normal playback) */
        BLURAY_STILL_TIME = 0x01,  /**< Still playback for fixed time */
        BLURAY_STILL_INFINITE = 0x02,  /**< Infinite still */
    }

    public enum bd_mark_type_e
    {
        BLURAY_MARK_ENTRY = 0x01,  /**< entry mark for chapter search */
        BLURAY_MARK_LINK = 0x02,  /**< link point */
    }

    public enum bd_dynamic_range_type_e
    {
        BLURAY_DYNAMIC_RANGE_SDR = 0,
        BLURAY_DYNAMIC_RANGE_HDR10 = 1,
        BLURAY_DYNAMIC_RANGE_DOLBY_VISION = 2
    }

    public struct BLURAY_STREAM_INFO
    {
        public byte coding_type;  /**< Stream coding (\ref bd_stream_type_e) */
        public byte format;       /**< Stream format (\ref bd_video_format_e or \ref bd_audio_format_e) */
        public byte rate;         /**< Stream frame rate (\ref bd_audio_rate_e or \ref bd_video_rate_e) */
        public byte char_code;    /**< Text subtitle charachter code (\ref bd_char_code_e) */
        public string lang = "";      /**< Language code */
        public UInt16 pid;          /**< mpeg-ts PID */
        public byte aspect;       /**< Stream video aspect ratio (\ref bd_video_aspect_e) */
        public byte subpath_id;   /**< Sub path identifier (= separate mpeg-ts mux / .m2ts file) */

        public BLURAY_STREAM_INFO() { }
    }

    public struct BLURAY_CLIP_INFO
    {
        public UInt32 pkt_count;               /**< Number of mpeg-ts packets */
        public byte still_mode;              /**< Clip still mode (\ref bd_still_mode_e) */
        public UInt16 still_time;              /**< Still time (seconds) if still_mode == BD_STILL_TIME */
        public byte video_stream_count;      /**< Number of video streams */
        public byte audio_stream_count;      /**< Number of audio streams */
        public byte pg_stream_count;         /**< Number of PG (Presentation Graphics) streams */
        public byte ig_stream_count;         /**< Number of IG (Interactive Graphics) streams */
        public byte sec_audio_stream_count;  /**< Number of secondary audio streams */
        public byte sec_video_stream_count;  /**< Number of secondary video streams */
        public Ref<BLURAY_STREAM_INFO> video_streams;          /**< Video streams information */
        public Ref<BLURAY_STREAM_INFO> audio_streams;          /**< Audio streams information */
        public Ref<BLURAY_STREAM_INFO> pg_streams;             /**< PG (Presentation Graphics) streams information */
        public Ref<BLURAY_STREAM_INFO> ig_streams;             /**< IG (Interactive Graphics) streams information */
        public Ref<BLURAY_STREAM_INFO> sec_audio_streams;      /**< Secondary audio streams information */
        public Ref<BLURAY_STREAM_INFO> sec_video_streams;      /**< Secondary video streams information */

        public UInt64 start_time;  /**< start media time, 90kHz, ("playlist time") */
        public UInt64 in_time;     /**< start timestamp, 90kHz */
        public UInt64 out_time;    /**< end timestamp, 90kHz */
        public string clip_id = "";  /**< Clip identifier (.m2ts file name) */

        public BLURAY_CLIP_INFO() { }
    }

    public struct BLURAY_TITLE_CHAPTER
    {
        public UInt32 idx;       /**< Chapter index (number - 1) */
        public UInt64 start;     /**< start media time, 90kHz, ("playlist time") */
        public UInt64 duration;  /**< duration */
        public UInt64 offset;    /**< distance from title start, bytes */
        public uint clip_ref;  /**< Clip reference (index to playlist clips list) */
    }

    public struct BLURAY_TITLE_MARK
    {
        public UInt32 idx;       /**< Mark index (number - 1) */
        public int type;      /**< \ref bd_mark_type_e */
        public UInt64 start;     /**< mark media time, 90kHz, ("playlist time") */
        public UInt64 duration;  /**< time to next mark */
        public UInt64 offset;    /**< mark distance from title start, bytes */
        public uint clip_ref;  /**< Clip reference (index to playlist clips list) */
    }

    public struct BLURAY_TITLE_INFO
    {
        public UInt32 idx;            /**< Playlist index number (filled only with bd_get_title_info()) */
        public UInt32 playlist;       /**< Playlist ID (mpls file name) */
        public UInt64 duration;       /**< Playlist duration, 90 kHz */
        public UInt32 clip_count;     /**< Number of clips */
        public byte angle_count;    /**< Number of angles */
        public UInt32 chapter_count;  /**< Number of chapters */
        public UInt32 mark_count;     /**< Number of playmarks */
        public Ref<BLURAY_CLIP_INFO> clips;         /**< Clip information */
        public Ref<BLURAY_TITLE_CHAPTER> chapters;      /**< Chapter information */
        public Ref<BLURAY_TITLE_MARK> marks;         /**< Playmark information */

        public byte mvc_base_view_r_flag;  /**< MVC base view (0 - left, 1 - right) */
    }

    public struct BLURAY_SOUND_EFFECT
    {
        public byte num_channels; /**< 1 - mono, 2 - stereo */
        public UInt32 num_frames;   /**< Number of audio frames */
        public Ref<UInt16> samples;      /**< 48000 Hz, 16 bit LPCM. Interleaved if stereo */
    }

    public enum bd_player_setting
    {
        BLURAY_PLAYER_SETTING_AUDIO_LANG = 16,    /**< Initial audio language.      String (ISO 639-2/T). */
        BLURAY_PLAYER_SETTING_PG_LANG = 17,    /**< Initial PG/SPU language.     String (ISO 639-2/T). */
        BLURAY_PLAYER_SETTING_MENU_LANG = 18,    /**< Initial menu language.       String (ISO 639-2/T). */
        BLURAY_PLAYER_SETTING_COUNTRY_CODE = 19,    /**< Player country code.         String (ISO 3166-1/alpha-2). */
        BLURAY_PLAYER_SETTING_REGION_CODE = 20,    /**< Player region code.          Integer. */
        BLURAY_PLAYER_SETTING_OUTPUT_PREFER = 21,    /**< Output mode preference.      Integer. */
        BLURAY_PLAYER_SETTING_PARENTAL = 13,    /**< Age for parental control.    Integer. */
        BLURAY_PLAYER_SETTING_AUDIO_CAP = 15,    /**< Audio capability.            Bit mask. */
        BLURAY_PLAYER_SETTING_VIDEO_CAP = 29,    /**< Video capability.            Bit mask. */
        BLURAY_PLAYER_SETTING_DISPLAY_CAP = 23,    /**< Display capability.          Bit mask. */
        BLURAY_PLAYER_SETTING_3D_CAP = 24,    /**< 3D capability.               Bit mask. */
        BLURAY_PLAYER_SETTING_UHD_CAP = 25,   /**< UHD capability.              */
        BLURAY_PLAYER_SETTING_UHD_DISPLAY_CAP = 26,   /**< UHD display capability.      */
        BLURAY_PLAYER_SETTING_HDR_PREFERENCE = 27,   /**< HDR preference.              */
        BLURAY_PLAYER_SETTING_SDR_CONV_PREFER = 28,   /**< SDR conversion preference.   */
        BLURAY_PLAYER_SETTING_TEXT_CAP = 30,    /**< Text Subtitle capability.    Bit mask. */
        BLURAY_PLAYER_SETTING_PLAYER_PROFILE = 31,    /**< Player profile and version. */

        BLURAY_PLAYER_SETTING_DECODE_PG = 0x100, /**< Enable/disable PG (subtitle) decoder. Integer. Default: disabled. */
        BLURAY_PLAYER_SETTING_PERSISTENT_STORAGE = 0x101, /**< Enable/disable BD-J persistent storage. Integer. Default: enabled. */

        BLURAY_PLAYER_PERSISTENT_ROOT = 0x200, /**< Root path to the BD_J persistent storage location. String. */
        BLURAY_PLAYER_CACHE_ROOT = 0x201, /**< Root path to the BD_J cache storage location. String. */
        BLURAY_PLAYER_JAVA_HOME = 0x202, /**< Location of JRE. String. Default: null (autodetect). */
    }

    public enum bd_event_e
    {
        BD_EVENT_NONE = 0,  /**< no pending events */

        /*
         * errors
         */

        BD_EVENT_ERROR = 1,  /**< Fatal error. Playback can't be continued. */
        BD_EVENT_READ_ERROR = 2,  /**< Reading of .m2ts aligned unit failed. Next call to read will try next block. */
        BD_EVENT_ENCRYPTED = 3,  /**< .m2ts file is encrypted and can't be played */

        /*
         * current playback position
         */

        BD_EVENT_ANGLE = 4,  /**< current angle, 1...N */
        BD_EVENT_TITLE = 5,  /**< current title, 1...N (0 = top menu) */
        BD_EVENT_PLAYLIST = 6,  /**< current playlist (xxxxx.mpls) */
        BD_EVENT_PLAYITEM = 7,  /**< current play item, 0...N-1  */
        BD_EVENT_CHAPTER = 8,  /**< current chapter, 1...N */
        BD_EVENT_PLAYMARK = 9,  /**< playmark reached */
        BD_EVENT_END_OF_TITLE = 10, /**< end of title reached */

        /*
         * stream selection
         */

        BD_EVENT_AUDIO_STREAM = 11,  /**< 1..32,  0xff  = none */
        BD_EVENT_IG_STREAM = 12,  /**< 1..32                */
        BD_EVENT_PG_TEXTST_STREAM = 13,  /**< 1..255, 0xfff = none */
        BD_EVENT_PIP_PG_TEXTST_STREAM = 14,  /**< 1..255, 0xfff = none */
        BD_EVENT_SECONDARY_AUDIO_STREAM = 15,  /**< 1..32,  0xff  = none */
        BD_EVENT_SECONDARY_VIDEO_STREAM = 16,  /**< 1..32,  0xff  = none */

        BD_EVENT_PG_TEXTST = 17,  /**< 0 - disable, 1 - enable */
        BD_EVENT_PIP_PG_TEXTST = 18,  /**< 0 - disable, 1 - enable */
        BD_EVENT_SECONDARY_AUDIO = 19,  /**< 0 - disable, 1 - enable */
        BD_EVENT_SECONDARY_VIDEO = 20,  /**< 0 - disable, 1 - enable */
        BD_EVENT_SECONDARY_VIDEO_SIZE = 21,  /**< 0 - PIP, 0xf - fullscreen */

        /*
         * playback control
         */

        /** HDMV VM or JVM stopped playlist playback. Flush all buffers. */
        BD_EVENT_PLAYLIST_STOP = 22,

        /** discontinuity in the stream (non-seamless connection). Reset demuxer PES buffers. */
        BD_EVENT_DISCONTINUITY = 23,  /**< new timestamp (45 kHz) */

        /** HDMV VM or JVM seeked the stream. Next read() will return data from new position. Flush all buffers. */
        BD_EVENT_SEEK = 24,  /**< new media time (45 kHz) */

        /** still playback (pause) */
        BD_EVENT_STILL = 25,  /**< 0 - off, 1 - on */

        /** Still playback for n seconds (reached end of still mode play item).
         *  Playback continues by calling bd_read_skip_still(). */
        BD_EVENT_STILL_TIME = 26,  /**< 0 = infinite ; 1...300 = seconds */

        /** Play sound effect */
        BD_EVENT_SOUND_EFFECT = 27,  /**< effect ID */

        /*
         * status
         */

        /** Nothing to do. Playlist is not playing, but title applet is running.
         *  Application should not call bd_read*() immediately again to avoid busy loop. */
        BD_EVENT_IDLE = 28,

        /** Pop-Up menu available */
        BD_EVENT_POPUP = 29,  /**< 0 - no, 1 - yes */

        /** Interactive menu visible */
        BD_EVENT_MENU = 30,  /**< 0 - no, 1 - yes */

        /** 3D */
        BD_EVENT_STEREOSCOPIC_STATUS = 31,  /**< 0 - 2D, 1 - 3D */

        /** BD-J key interest table changed */
        BD_EVENT_KEY_INTEREST_TABLE = 32,  /**< bitmask, BLURAY_KIT_* */

        /** UO mask changed */
        BD_EVENT_UO_MASK_CHANGED = 33,  /**< bitmask, BLURAY_UO_* */

        /*BD_EVENT_LAST = 33, */
    }

    public struct BD_EVENT
    {
        public bd_event_e _event;  /**< Event type (\ref bd_event_e) */
        public UInt32 param;  /**< Event data */
    }

    internal enum BD_TITLE_TYPE
    {
        title_undef = 0,
        title_hdmv,
        title_bdj,
    }

    internal struct BD_STREAM
    {
        /* current clip */
        public Ref<NAV_CLIP> clip;
        public BD_FILE_H? fp;
        public UInt64 clip_size;
        public UInt64 clip_block_pos;
        public UInt64 clip_pos;

        /* current aligned unit */
        public UInt16 int_buf_off;

        /* current stream UO mask (combined from playlist and current clip UO masks) */
        public BD_UO_MASK uo_mask;

        /* internally handled pids */
        public UInt16 ig_pid; /* pid of currently selected IG stream */
        public UInt16 pg_pid; /* pid of currently selected PG stream */

        /* */
        public byte eof_hit;
        public byte encrypted_block_cnt;
        public byte seek_flag;  /* used to fine-tune first read after seek */

        public Ref<M2TS_FILTER> m2ts_filter;
    }

    internal struct BD_PRELOAD
    {
        public Ref<NAV_CLIP> clip;
        public UInt64 clip_size;
        public Ref<byte> buf;
    }

    public struct BLURAY
    {
        public const int TITLES_ALL = 0;    /**< all titles. */
        public const int TITLES_FILTER_DUP_TITLE = 0x01; /**< remove duplicate titles. */
        public const int TITLES_FILTER_DUP_CLIP = 0x02; /**< remove titles that have duplicate clips. */
        public const int TITLES_RELEVANT = (TITLES_FILTER_DUP_TITLE | TITLES_FILTER_DUP_CLIP); /**< remove duplicate titles and clips */

        /* AACS error codes */
        public const int BD_AACS_CORRUPTED_DISC = -1;  /**< Corrupt disc (missing/invalid files) */
        public const int BD_AACS_NO_CONFIG = -2;  /**< AACS configuration file missing      */
        public const int BD_AACS_NO_PK = -3;  /**< No valid processing key found        */
        public const int BD_AACS_NO_CERT = -4;  /**< No valid certificate found           */
        public const int BD_AACS_CERT_REVOKED = -5;  /**< All certificates have been revoked   */
        public const int BD_AACS_MMC_FAILED = -6;  /**< MMC (disc drive interaction) failed  */

        /* BD_EVENT_ERROR param values */
        public const int BD_ERROR_HDMV = 1;                /**< HDMV VM failed to play the title  */
        public const int BD_ERROR_BDJ = 2;                /**< BD-J failed to play the title     */

        /* bd_event_e.BD_EVENT_ENCRYPTED param vlues */
        public const int BD_ERROR_AACS = 3;                /**< AACS failed or not supported      */
        public const int BD_ERROR_BDPLUS = 4;                /**< BD+ failed or not supported       */

        /* BD_EVENT_TITLE special titles */
        public const int BLURAY_TITLE_FIRST_PLAY = 0xffff;   /**< "First Play" title started        */
        public const int BLURAY_TITLE_TOP_MENU = 0;        /**< "Top Menu" title started          */

        /* BD_EVENT_KEY_INTEREST flags */
        public const int BLURAY_KIT_PLAY = 0x1;      /**< BD-J requests to handle "Play" UO          */
        public const int BLURAY_KIT_STOP = 0x2;      /**< BD-J requests to handle "Stop" UO          */
        public const int BLURAY_KIT_FFW = 0x4;      /**< BD-J requests to handle "Fast Forward" UO  */
        public const int BLURAY_KIT_REW = 0x8;      /**< BD-J requests to handle "Reverse" UO       */
        public const int BLURAY_KIT_TRACK_NEXT = 0x10;     /**< BD-J requests to handle "Next Track" UO    */
        public const int BLURAY_KIT_TRACK_PREV = 0x20;     /**< BD-J requests to handle "Prev Track" UO    */
        public const int BLURAY_KIT_PAUSE = 0x40;     /**< BD-J requests to handle "Pause" UO         */
        public const int BLURAY_KIT_STILL_OFF = 0x80;     /**< BD-J requests to handle "Still Off" UO     */
        public const int BLURAY_KIT_SEC_AUDIO = 0x100;    /**< BD-J requests to handle "Sec. Audio" UO    */
        public const int BLURAY_KIT_SEC_VIDEO = 0x200;    /**< BD-J requests to handle "Sec. Video" UO    */
        public const int BLURAY_KIT_PG_TEXTST = 0x400;    /**< BD-J requests to handle "Subtitle" UO      */

        /* BD_EVENT_UO_MASK flags */
        public const int BLURAY_UO_MENU_CALL = 0x1;      /**< "Menu Call" masked (not allowed)    */
        public const int BLURAY_UO_TITLE_SEARCH = 0x2;      /**< "Title Search" masked (not allowed) */

        public const int BLURAY_RATE_PAUSED = 0;      /**< Set playback rate to PAUSED  */
        public const int BLURAY_RATE_NORMAL = 90000;  /**< Set playback rate to NORMAL  */

        public delegate void bd_overlay_proc_f(object? handle, Ref<BD_OVERLAY> _event);

        public delegate void bd_argb_overlay_proc_f(object? handle, Ref<BD_ARGB_OVERLAY> _event);

        BD_MUTEX mutex = new();  /* protect API function access to internal data */

        /* current disc */
        BD_DISC? disc;
        Variable<BLURAY_DISC_INFO> disc_info = new();
        Ref<BLURAY_TITLE> titles;  /* titles from disc index */
        Ref<META_ROOT> meta;
        Ref<NAV_TITLE_LIST> title_list;

        /* current playlist */
        NAV_TITLE? title;
        UInt32 title_idx;
        UInt64 s_pos;

        /* streams */
        Variable<BD_STREAM> st0 = new();       /* main path */
        Variable<BD_PRELOAD> st_ig = new();     /* preloaded IG stream sub path */
        Variable<BD_PRELOAD> st_textst = new(); /* preloaded TextST sub path */

        /* buffer for bd_read(): current aligned unit of main stream (st0) */
        byte[] int_buf = new byte[6144];

        /* seamless angle change request */
        int seamless_angle_change;
        UInt32 angle_change_pkt;
        Variable<UInt32> angle_change_time = new();
        UInt32 request_angle;

        /* mark tracking */
        UInt64 next_mark_pos;
        int next_mark;

        /* player state */
        Ref<BD_REGISTERS> regs;            /* player registers */
        Ref<BD_EVENT_QUEUE<BD_EVENT>> event_queue;     /* navigation mode _event queue */
        BD_UO_MASK uo_mask;         /* Current UO mask */
        BD_UO_MASK title_uo_mask;   /* UO mask from current .bdjo file or Movie Object */
        BD_TITLE_TYPE title_type;      /* type of current title (in navigation mode) */
        /* Pending action after playlist end
         * BD-J: delayed sending of BDJ_EVENT_END_OF_PLAYLIST
         *       1 - message pending. 3 - message sent.
         */
        byte end_of_playlist; /* 1 - reached. 3 - processed . */
        byte app_scr;         /* 1 if application provides presentation timetamps */

        /* HDMV */
        Ref<HDMV_VM> hdmv_vm;
        byte hdmv_suspended;
        byte hdmv_num_invalid_pl;

        /* BD-J */
        Ref<BDJAVA> bdjava;
        BDJ_CONFIG bdj_config;
        byte bdj_wait_start;  /* BD-J has selected playlist (prefetch) but not yet started playback */

        /* HDMV graphics */
        Ref<GRAPHICS_CONTROLLER> graphics_controller;
        Ref<SOUND_DATA> sound_effects;
        BD_UO_MASK gc_uo_mask;      /* UO mask from current menu page */
        UInt32 gc_status;
        byte decode_pg;

        /* TextST */
        UInt32 gc_wakeup_time;  /* stream timestamp of next subtitle */
        UInt64 gc_wakeup_pos;   /* stream position of gc_wakeup_time */

        /* ARGB overlay output */
        object? argb_overlay_proc_handle;
        bd_argb_overlay_proc_f argb_overlay_proc;
        Ref<BD_ARGB_BUFFER> argb_buffer;
        BD_MUTEX argb_buffer_mutex = new();

        static UInt32 SPN(UInt64 pos) => (((UInt32)(pos >> 6)) / 3);

        public BLURAY() { }

        /*
 * Library version
 */
        public static void bd_get_version(out uint major, out uint minor, out uint micro)
        {
            major = BlurayVersion.BLURAY_VERSION_MAJOR;
            minor = BlurayVersion.BLURAY_VERSION_MINOR;
            micro = BlurayVersion.BLURAY_VERSION_MICRO;
        }

        /*
         * Navigation mode _event queue
         */

        public static string bd_event_name(bd_event_e _event)
        {
            return _event.ToString();
        }

        static bool _get_event(Ref<BLURAY> bd, Ref<BD_EVENT> ev)
        {
            bool result = BD_EVENT_QUEUE<BD_EVENT>.event_queue_get(bd.Value.event_queue, ev);
            if (!result) {
                ev.Value._event = bd_event_e.BD_EVENT_NONE;
            }
            return result;
        }

        static bool _queue_event(Ref<BLURAY> bd, bd_event_e _event, UInt32 param)
        {
            bool result = false;
            if (bd.Value.event_queue) {
                Variable<BD_EVENT> ev = new();
                ev.Value._event = _event;
                ev.Value.param = param;

                result = BD_EVENT_QUEUE<BD_EVENT>.event_queue_put(bd.Value.event_queue, ev.Ref);
                if (!result) {
                    string name = bd_event_name(_event);
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_queue_event({((name != null) ? name : "?")}:{_event}, {param}): queue overflow !");
                }
            }
            return result;
        }

        /*
         * PSR utils
         */

        static void _update_time_psr(Ref<BLURAY> bd, UInt32 time)
        {
            /*
             * Update PSR8: Presentation Time
             * The PSR8 represents presentation time in the playing interval from IN_time until OUT_time of
             * the current PlayItem, measured in units of a 45 kHz clock.
             */

            if (bd.Value.title == null || !bd.Value.st0.Value.clip) {
                return;
            }
            if (time < bd.Value.st0.Value.clip.Value.in_time) {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_update_time_psr(): timestamp before clip start");
                return;
            }
            if (time > bd.Value.st0.Value.clip.Value.out_time) {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_update_time_psr(): timestamp after clip end");
                return;
            }

            Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_TIME, time);
        }

        static UInt32 _update_time_psr_from_stream(Ref<BLURAY> bd)
        {
            /* update PSR_TIME from stream. Not real presentation time (except when seeking), but near enough. */
            Ref<NAV_CLIP> clip = bd.Value.st0.Value.clip;

            if (bd.Value.title != null && clip) {

                Variable<UInt32> clip_pkt = new(), clip_time = new();
                Navigation.nav_clip_packet_search(bd.Value.st0.Value.clip, SPN(bd.Value.st0.Value.clip_pos), clip_pkt.Ref, clip_time.Ref);
                if (clip_time.Value >= clip.Value.in_time && clip_time.Value <= clip.Value.out_time) {
                    _update_time_psr(bd, clip_time.Value);
                    return clip_time.Value;
                } else {
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"{clip.Value.name}: no timestamp for SPN {SPN(bd.Value.st0.Value.clip_pos)} (got {clip_time}). clip {clip.Value.in_time}-{clip.Value.out_time}.");
                }
            }

            return 0;
        }

        static void _update_stream_psr_by_lang(Ref<BD_REGISTERS> regs,
                                               bd_psr_idx psr_lang, bd_psr_idx psr_stream,
                                               UInt32 enable_flag,
                                               Ref<MPLS_STREAM> streams, uint num_streams,
                                               Ref<UInt32> lang, UInt32 blacklist)
        {
            UInt32 preferred_lang;
            int stream_idx = -1;
            uint ii;
            UInt32 stream_lang = 0;

            /* get preferred language */
            preferred_lang = Register.bd_psr_read(regs, psr_lang);

            /* find stream */
            for (ii = 0; ii < num_streams; ii++) {
                if (preferred_lang == Util.str_to_uint32(streams[ii].lang, 3)) {
                    stream_idx = (int)ii;
                    break;
                }
            }

            /* requested language not found ? */
            if (stream_idx < 0) {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"Stream with preferred language not found");
                /* select first stream */
                stream_idx = 0;
                /* no subtitles if preferred language not found */
                enable_flag = 0;
            }

            stream_lang = Util.str_to_uint32(streams[stream_idx].lang, 3);

            /* avoid enabling subtitles if audio is in the same language */
            if (blacklist != 0 && blacklist == stream_lang) {
                enable_flag = 0;
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"Subtitles disabled (audio is in the same language)");
            }

            if (lang) {
                lang.Value = stream_lang;
            }

            /* update PSR */

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"Selected stream {stream_idx} (language {streams[stream_idx].lang})");

            Register.bd_psr_write_bits(regs, psr_stream,
                              (uint)(stream_idx + 1) | enable_flag,
                              0x80000fff);
        }

        static void _update_clip_psrs(Ref<BLURAY> bd, Ref<NAV_CLIP> clip)
        {
            Ref<MPLS_STN> stn = clip.Value.title.pl.Value.play_item[clip.Value._ref].stn.Ref;
            Variable<UInt32> audio_lang = new(0);
            UInt32 psr_val;

            Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_PLAYITEM, clip.Value._ref);
            Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_TIME, clip.Value.in_time);

            /* Validate selected audio, subtitle and IG stream PSRs */
            if (stn.Value.num_audio != 0) {
                Register.bd_psr_lock(bd.Value.regs);
                psr_val = Register.bd_psr_read(bd.Value.regs, bd_psr_idx.PSR_PRIMARY_AUDIO_ID);
                if (psr_val == 0 || psr_val > stn.Value.num_audio) {
                    _update_stream_psr_by_lang(bd.Value.regs,
                                               bd_psr_idx.PSR_AUDIO_LANG, bd_psr_idx.PSR_PRIMARY_AUDIO_ID, 0,
                                               stn.Value.audio, stn.Value.num_audio,
                                               audio_lang.Ref, 0);
                } else {
                    audio_lang.Value = Util.str_to_uint32(stn.Value.audio[psr_val - 1].lang, 3);
                }
                Register.bd_psr_unlock(bd.Value.regs);
            }
            if (stn.Value.num_pg != 0) {
                Register.bd_psr_lock(bd.Value.regs);
                psr_val = Register.bd_psr_read(bd.Value.regs, bd_psr_idx.PSR_PG_STREAM) & 0xfff;
                if ((psr_val == 0) || (psr_val > stn.Value.num_pg)) {
                    _update_stream_psr_by_lang(bd.Value.regs,
                                               bd_psr_idx.PSR_PG_AND_SUB_LANG, bd_psr_idx.PSR_PG_STREAM, 0x80000000,
                                               stn.Value.pg, stn.Value.num_pg,
                                               Ref<uint>.Null, audio_lang.Value);
                }
                Register.bd_psr_unlock(bd.Value.regs);
            }
            if (stn.Value.num_ig != 0 && bd.Value.title_type != BD_TITLE_TYPE.title_undef) {
                Register.bd_psr_lock(bd.Value.regs);
                psr_val = Register.bd_psr_read(bd.Value.regs, bd_psr_idx.PSR_IG_STREAM_ID);
                if ((psr_val == 0) || (psr_val > stn.Value.num_ig)) {
                    Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_IG_STREAM_ID, 1);
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Selected IG stream 1 (stream {psr_val} not available)");
                }
                Register.bd_psr_unlock(bd.Value.regs);
            }
        }

        static void _update_playlist_psrs(Ref<BLURAY> bd)
        {
            Ref<NAV_CLIP> clip = bd.Value.st0.Value.clip;

            Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_PLAYLIST, uint.Parse(Path.GetFileNameWithoutExtension(bd.Value.title.name)));
            Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_ANGLE_NUMBER, bd.Value.title.angle + 1u);
            Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_CHAPTER, 0xffff);

            if (clip && bd.Value.title_type == BD_TITLE_TYPE.title_undef) {
                /* Initialize selected audio and subtitle stream PSRs when not using menus.
                 * Selection is based on language setting PSRs and clip STN.
                 */
                Ref<MPLS_STN> stn = clip.Value.title.pl.Value.play_item[clip.Value._ref].stn.Ref;
                Variable<UInt32> audio_lang = new(0);

                /* make sure clip is up-to-date before STREAM events are triggered */
                Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_PLAYITEM, clip.Value._ref);

                if (stn.Value.num_audio != 0) {
                    _update_stream_psr_by_lang(bd.Value.regs,
                                               bd_psr_idx.PSR_AUDIO_LANG, bd_psr_idx.PSR_PRIMARY_AUDIO_ID, 0,
                                               stn.Value.audio, stn.Value.num_audio,
                                               audio_lang.Ref, 0);
                }

                if (stn.Value.num_pg != 0) {
                    _update_stream_psr_by_lang(bd.Value.regs,
                                               bd_psr_idx.PSR_PG_AND_SUB_LANG, bd_psr_idx.PSR_PG_STREAM, 0x80000000,
                                               stn.Value.pg, stn.Value.num_pg,
                                               Ref<uint>.Null, audio_lang.Value);
                }
            }
        }

        static bool _is_interactive_title(Ref<BLURAY> bd)
        {
            if (bd.Value.titles && bd.Value.title_type != BD_TITLE_TYPE.title_undef) {
                uint title = Register.bd_psr_read(bd.Value.regs, bd_psr_idx.PSR_TITLE_NUMBER);
                if (title == BLURAY_TITLE_FIRST_PLAY && bd.Value.disc_info.Value.first_play.Value.interactive != 0) {
                    return true;
                }
                if (title <= bd.Value.disc_info.Value.num_titles/* && bd.Value.titles[title]*/) {
                    return bd.Value.titles[title].interactive != 0;
                }
            }
            return false;
        }

        static void _update_chapter_psr(Ref<BLURAY> bd)
        {
            if (!_is_interactive_title(bd) && bd.Value.title.chap_list.count > 0) {
                UInt32 current_chapter = bd_get_current_chapter(bd);
                Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_CHAPTER, current_chapter + 1);
            }
        }

        /*
         * PG
         */

        static bool _find_pg_stream(Ref<BLURAY> bd, Ref<UInt16> pid, Ref<int> sub_path_idx, Ref<uint> sub_clip_idx, Ref<byte> char_code)
        {
            uint main_clip_idx = bd.Value.st0.Value.clip ? bd.Value.st0.Value.clip.Value._ref : 0;
            uint pg_stream = Register.bd_psr_read(bd.Value.regs, bd_psr_idx.PSR_PG_STREAM);
            Ref<MPLS_STN> stn = bd.Value.title.pl.Value.play_item[main_clip_idx].stn.Ref;

#if false
    /* Enable decoder unconditionally (required for forced subtitles).
       Display flag is checked in graphics controller. */
    /* check PG display flag from PSR */
    if (!(pg_stream & 0x80000000)) {
      return 0;
    }
#endif

            pg_stream &= 0xfff;

            if (pg_stream > 0 && pg_stream <= stn.Value.num_pg) {
                pg_stream--; /* stream number to table index */
                if (stn.Value.pg[pg_stream].stream_type == 2) {
                    sub_path_idx.Value = stn.Value.pg[pg_stream].subpath_id;
                    sub_clip_idx.Value = stn.Value.pg[pg_stream].subclip_id;
                }
                pid.Value = stn.Value.pg[pg_stream].pid;

                if (char_code && stn.Value.pg[pg_stream].coding_type == (byte)bd_stream_type_e.BLURAY_STREAM_TYPE_SUB_TEXT) {
                    char_code.Value = stn.Value.pg[pg_stream].char_code;
                }

                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"_find_pg_stream(): current PG stream pid 0x{pid.Value:x4} sub-path {sub_path_idx.Value}");
                return true;
            }

            return false;
        }

        static bool _init_pg_stream(Ref<BLURAY> bd)
        {
            Variable<int> pg_subpath = new(-1);
            Variable<uint> pg_subclip = new Variable<uint>(0);
            Variable<UInt16> pg_pid = new(0);

            bd.Value.st0.Value.pg_pid = 0;

            if (!bd.Value.graphics_controller) {
                return false;
            }

            /* reset PG decoder and controller */
            GraphicsController.gc_run(bd.Value.graphics_controller, gc_ctrl_e.GC_CTRL_PG_RESET, 0, Ref<GC_NAV_CMDS>.Null);

            if (bd.Value.decode_pg == 0 || bd.Value.title == null) {
                return false;
            }

            _find_pg_stream(bd, pg_pid.Ref, pg_subpath.Ref, pg_subclip.Ref, Ref<byte>.Null);

            /* store PID of main path embedded PG stream */
            if (pg_subpath.Value < 0) {
                bd.Value.st0.Value.pg_pid = pg_pid.Value;
                return pg_pid.Value != 0;
            }

            return false;
        }

        static void _update_textst_timer(Ref<BLURAY> bd)
        {
            if (bd.Value.st_textst.Value.clip) {
                if (bd.Value.st0.Value.clip_block_pos >= bd.Value.gc_wakeup_pos) {
                    Variable<GC_NAV_CMDS> cmds = new(); //{-1, null, -1, 0, 0, EMPTY_UO_MASK};
                    cmds.Value.num_nav_cmds = -1;
                    cmds.Value.nav_cmds = Ref<MOBJ_CMD>.Null;
                    cmds.Value.sound_id_ref = -1;
                    cmds.Value.status = 0;
                    cmds.Value.wakeup_time = 0;
                    cmds.Value.page_uo_mask = new();

                    GraphicsController.gc_run(bd.Value.graphics_controller, gc_ctrl_e.GC_CTRL_PG_UPDATE, bd.Value.gc_wakeup_time, cmds.Ref);

                    bd.Value.gc_wakeup_time = cmds.Value.wakeup_time;
                    bd.Value.gc_wakeup_pos = ulong.MaxValue; /* no wakeup */

                    /* next _event in this clip ? */
                    if (cmds.Value.wakeup_time >= bd.Value.st0.Value.clip.Value.in_time && cmds.Value.wakeup_time < bd.Value.st0.Value.clip.Value.out_time) {
                        /* find _event position in main path clip */
                        Ref<NAV_CLIP> clip = bd.Value.st0.Value.clip;
                        if (clip.Value.cl) {
                            Variable<UInt32> spn = new();
                            Navigation.nav_clip_time_search(clip, cmds.Value.wakeup_time, spn.Ref, Ref<uint>.Null);
                            if (spn.Value != 0) {
                                bd.Value.gc_wakeup_pos = (UInt64)spn.Value * 192L;
                            }
                        }
                    }
                }
            }
        }

        static void _init_textst_timer(Ref<BLURAY> bd)
        {
            if (bd.Value.st_textst.Value.clip && bd.Value.st0.Value.clip.Value.cl) {
                Variable<UInt32> clip_time = new(), clip_pkt = new();
                Navigation.nav_clip_packet_search(bd.Value.st0.Value.clip, SPN(bd.Value.st0.Value.clip_block_pos), clip_pkt.Ref, clip_time.Ref);
                bd.Value.gc_wakeup_time = clip_time.Value;
                bd.Value.gc_wakeup_pos = 0;
                _update_textst_timer(bd);
            }
        }

        /*
         * UO mask
         */

        static UInt32 _compressed_mask(BD_UO_MASK mask)
        {
            return (mask.menu_call ? 1u : 0u) | ((mask.title_search ? 1u : 0u) << 1);
        }

        static void _update_uo_mask(Ref<BLURAY> bd)
        {
            BD_UO_MASK old_mask = bd.Value.uo_mask;
            BD_UO_MASK new_mask;

            new_mask = BD_UO_MASK.uo_mask_combine(bd.Value.title_uo_mask, bd.Value.st0.Value.uo_mask);
            new_mask = BD_UO_MASK.uo_mask_combine(bd.Value.gc_uo_mask, new_mask);
            if (_compressed_mask(old_mask) != _compressed_mask(new_mask)) {
                _queue_event(bd, bd_event_e.BD_EVENT_UO_MASK_CHANGED, _compressed_mask(new_mask));
            }
            bd.Value.uo_mask = new_mask;
        }

        static void _update_hdmv_uo_mask(Ref<BLURAY> bd)
        {
            UInt32 mask = HdmvVm.hdmv_vm_get_uo_mask(bd.Value.hdmv_vm);
            bd.Value.title_uo_mask.title_search = ((mask & HdmvVm.HDMV_TITLE_SEARCH_MASK) != 0);
            bd.Value.title_uo_mask.menu_call = ((mask & HdmvVm.HDMV_MENU_CALL_MASK) != 0);

            _update_uo_mask(bd);
        }


        /*
         * clip access (BD_STREAM)
         */

        static void _close_m2ts(Ref<BD_STREAM> st)
        {
            if (st.Value.fp != null) {
                st.Value.fp.file_close();
                st.Value.fp = null;
            }

            M2tsFilter.m2ts_filter_close(ref st.Value.m2ts_filter);
        }

        static bool _open_m2ts(Ref<BLURAY> bd, Ref<BD_STREAM> st)
        {
            _close_m2ts(st);

            if (!st.Value.clip) {
                return false;
            }

            st.Value.fp = bd.Value.disc.disc_open_stream(st.Value.clip.Value.name);

            st.Value.clip_size = 0;
            st.Value.clip_pos = (UInt64)st.Value.clip.Value.start_pkt * 192;
            st.Value.clip_block_pos = (st.Value.clip_pos / 6144) * 6144;
            st.Value.eof_hit = 0;
            st.Value.encrypted_block_cnt = 0;

            if (st.Value.fp != null) {
                Int64 clip_size = st.Value.fp.file_size();
                if (clip_size > 0) {

                    if (st.Value.fp.file_seek((long)st.Value.clip_block_pos, SeekOrigin.Begin) < 0) {
                        Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Unable to seek clip {st.Value.clip.Value.name}!");
                        _close_m2ts(st);
                        return false;
                    }

                    st.Value.clip_size = (ulong)clip_size;
                    st.Value.int_buf_off = 6144;

                    if (st == bd.Value.st0.Ref) {
                        Ref<MPLS_PL> pl = st.Value.clip.Value.title.pl;
                        Ref<MPLS_STN> stn = pl.Value.play_item[st.Value.clip.Value._ref].stn.Ref;

                        st.Value.uo_mask = BD_UO_MASK.uo_mask_combine(pl.Value.app_info.Value.uo_mask.Value,
                                                      pl.Value.play_item[st.Value.clip.Value._ref].uo_mask.Value);
                        _update_uo_mask(bd);

                        st.Value.m2ts_filter = M2tsFilter.m2ts_filter_init((Int64)st.Value.clip.Value.in_time << 1,
                                                           (Int64)st.Value.clip.Value.out_time << 1,
                                                           stn.Value.num_video, stn.Value.num_audio,
                                                           stn.Value.num_ig, stn.Value.num_pg);

                        _update_clip_psrs(bd, st.Value.clip);

                        _init_pg_stream(bd);

                        _init_textst_timer(bd);
                    }

                    return true;
                }

                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Clip {st.Value.clip.Value.name} empty!");
                _close_m2ts(st);
            }

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Unable to open clip {st.Value.clip.Value.name}!");

            return false;
        }

        static int _validate_unit(Ref<BLURAY> bd, Ref<BD_STREAM> st, Ref<byte> buf)
        {
            /* Check TP_extra_header Copy_permission_indicator. If != 0, unit may be encrypted. */
            /* Check first sync byte. It should never be encrypted. */
            if ((buf[0] & 0xc0) != 0 || (buf[4] != 0x47)) {

                /* Check first sync bytes. If not OK, drop unit. */
                if (buf[4] != 0x47 || buf[4 + 192] != 0x47 || buf[4 + 2 * 192] != 0x47 || buf[4 + 3 * 192] != 0x47) {

                    /* Some streams have Copy_permission_indicator incorrectly set. */
                    /* Check first TS sync byte. If unit is encrypted, first 16 bytes are plain, rest not. */
                    /* not 100% accurate (can be random data too). But the unit is broken anyway ... */
                    if (buf[4] == 0x47) {

                        /* most likely encrypted stream. Check couple of blocks before erroring out. */
                        st.Value.encrypted_block_cnt++;

                        if (st.Value.encrypted_block_cnt > 10) {
                            /* error out */
                            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"TP header copy permission indicator != 0. Stream seems to be encrypted.");
                            _queue_event(bd, bd_event_e.BD_EVENT_ENCRYPTED, BD_ERROR_AACS);
                            return -1;
                        }
                    }

                    /* broken block, ignore it */
                    _queue_event(bd, bd_event_e.BD_EVENT_READ_ERROR, 1);
                    return 0;
                }
            }

            st.Value.eof_hit = 0;
            st.Value.encrypted_block_cnt = 0;
            return 1;
        }

        static int _skip_unit(Ref<BLURAY> bd, Ref<BD_STREAM> st)
        {
            UInt64 len = 6144;

            /* skip broken unit */
            st.Value.clip_block_pos += len;
            st.Value.clip_pos += len;

            _queue_event(bd, bd_event_e.BD_EVENT_READ_ERROR, 0);

            /* seek to next unit start */
            if (st.Value.fp.file_seek((long)st.Value.clip_block_pos, SeekOrigin.Begin) < 0) {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Unable to seek clip {st.Value.clip.Value.name}!");
                return -1;
            }

            return 0;
        }

        static int _read_block(Ref<BLURAY> bd, Ref<BD_STREAM> st, Ref<byte> buf)
        {
            UInt64 len = 6144;

            if (st.Value.fp != null) {
                Logging.bd_debug(DebugMaskEnum.DBG_STREAM, $"Reading unit at {st.Value.clip_block_pos}...");

                if (len + st.Value.clip_block_pos <= st.Value.clip_size) {
                    UInt64 read_len;

                    if ((read_len = st.Value.fp.file_read(buf, len)) != 0) {
                        int error;

                        if (read_len != len) {
                            Logging.bd_debug(DebugMaskEnum.DBG_STREAM | DebugMaskEnum.DBG_CRIT, $"Read {(int)read_len} bytes at {st.Value.clip_block_pos} ; requested {(int)len} !");
                            return _skip_unit(bd, st);
                        }
                        st.Value.clip_block_pos += len;

                        if ((error = _validate_unit(bd, st, buf)) <= 0) {
                            /* skip broken unit */
                            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Skipping broken unit at {(st.Value.clip_block_pos - len)}");
                            st.Value.clip_pos += len;
                            return error;
                        }

                        if (st.Value.m2ts_filter) {
                            int result = M2tsFilter.m2ts_filter(st.Value.m2ts_filter, buf);
                            if (result < 0) {
                                M2tsFilter.m2ts_filter_close(ref st.Value.m2ts_filter);
                                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"m2ts filter error");
                            }
                        }

                        Logging.bd_debug(DebugMaskEnum.DBG_STREAM, $"Read unit OK!");

                        return 1;
                    }

                    Logging.bd_debug(DebugMaskEnum.DBG_STREAM | DebugMaskEnum.DBG_CRIT, $"Read unit at {st.Value.clip_block_pos} failed !");

                    return _skip_unit(bd, st);
                }

                /* This is caused by truncated .m2ts file or invalid clip length.
                 *
                 * Increase position to avoid infinite loops.
                 * Next clip won't be selected until all packets of this clip have been read.
                 */
                st.Value.clip_block_pos += len;
                st.Value.clip_pos += len;

                if (st.Value.eof_hit == 0) {
                    Logging.bd_debug(DebugMaskEnum.DBG_STREAM | DebugMaskEnum.DBG_CRIT, $"Read past EOF !");
                    st.Value.eof_hit = 1;
                }

                return 0;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"No valid title selected!");

            return -1;
        }

        /*
         * clip preload (BD_PRELOAD)
         */

        static void _close_preload(ref BD_PRELOAD p)
        {
            p.buf.Free();
            p = new();
        }

        const uint PRELOAD_SIZE_LIMIT = (512 * 1024 * 1024);  /* do not preload clips larger than 512M */

        static bool _preload_m2ts(Ref<BLURAY> bd, Ref<BD_PRELOAD> p)
        {
            /* setup and open Ref<BD_STREAM> */

            Variable<BD_STREAM> st = new();

            st.Value = new();
            st.Value.clip = p.Value.clip;

            if (st.Value.clip_size > PRELOAD_SIZE_LIMIT)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_preload_m2ts(): too large clip ({st.Value.clip_size})");
                return false;
            }

            if (!_open_m2ts(bd, st.Ref))
            {
                return false;
            }

            /* allocate buffer */
            p.Value.clip_size = (UInt64)st.Value.clip_size;
            Ref<byte> tmp = p.Value.buf.Reallocate(p.Value.clip_size);

            p.Value.buf = tmp;

            /* read clip to buffer */

            Ref<byte> buf = p.Value.buf;
            Ref<byte> end = p.Value.buf + p.Value.clip_size;

            for (; buf < end; buf += 6144)
            {
                if (_read_block(bd, st.Ref, buf) <= 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_preload_m2ts(): error loading {st.Value.clip.Value.name} at {(UInt64)(buf - p.Value.buf)}");
                    _close_m2ts(st.Ref);
                    _close_preload(ref p.Value);
                    return false;
                }
            }

            /* */

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"_preload_m2ts(): loaded {st.Value.clip_size} bytes from {st.Value.clip.Value.name}");

            _close_m2ts(st.Ref);

            return true;
        }

        static Int64 _seek_stream(Ref<BLURAY> bd, Ref<BD_STREAM> st,
                                    Ref<NAV_CLIP> clip, UInt32 clip_pkt)
        {
            if (!clip)
                return -1;

            if (st.Value.fp == null || !st.Value.clip || clip.Value._ref != st.Value.clip.Value._ref) {
                // The position is in a new clip
                st.Value.clip = clip;
                if (!_open_m2ts(bd, st))
                {
                    return -1;
                }
            }

            if (st.Value.m2ts_filter)
            {
                M2tsFilter.m2ts_filter_seek(st.Value.m2ts_filter, 0, (Int64)st.Value.clip.Value.in_time << 1);
            }

            st.Value.clip_pos = (UInt64)clip_pkt * 192;
            st.Value.clip_block_pos = (st.Value.clip_pos / 6144) * 6144;

            if (st.Value.fp.file_seek((long)st.Value.clip_block_pos, SeekOrigin.Begin) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Unable to seek clip {st.Value.clip.Value.name}!");
            }

            st.Value.int_buf_off = 6144;
            st.Value.seek_flag = 1;

            return (long)st.Value.clip_pos;
        }

        /*
         * Graphics controller interface
         */

        static int _run_gc(Ref<BLURAY> bd, gc_ctrl_e msg, UInt32 param)
        {
            int result = -1;

            if (!bd)
            {
                return -1;
            }

            if (bd.Value.graphics_controller && bd.Value.hdmv_vm)
            {
                Variable<GC_NAV_CMDS> cmds = new();
                cmds.Value.num_nav_cmds = -1;
                cmds.Value.nav_cmds = Ref<MOBJ_CMD>.Null;
                cmds.Value.sound_id_ref = -1;
                cmds.Value.status = 0;
                cmds.Value.wakeup_time = 0;
                cmds.Value.page_uo_mask = new();

                result = GraphicsController.gc_run(bd.Value.graphics_controller, msg, param, cmds.Ref);

                if (cmds.Value.num_nav_cmds > 0)
                {
                    HdmvVm.hdmv_vm_set_object(bd.Value.hdmv_vm, cmds.Value.num_nav_cmds, cmds.Value.nav_cmds);
                    bd.Value.hdmv_suspended = (byte)((HdmvVm.hdmv_vm_running(bd.Value.hdmv_vm) == 0) ? 1u : 0u);
                }

                if (cmds.Value.status != bd.Value.gc_status)
                {
                    UInt32 changed_flags = cmds.Value.status ^ bd.Value.gc_status;
                    bd.Value.gc_status = cmds.Value.status;
                    if ((changed_flags & GraphicsController.GC_STATUS_MENU_OPEN) != 0)
                    {
                        _queue_event(bd, bd_event_e.BD_EVENT_MENU, ((bd.Value.gc_status & GraphicsController.GC_STATUS_MENU_OPEN) == 0) ? 0u : 1u);
                    }
                    if ((changed_flags & GraphicsController.GC_STATUS_POPUP) != 0)
                    {
                        _queue_event(bd, bd_event_e.BD_EVENT_POPUP, ((bd.Value.gc_status & GraphicsController.GC_STATUS_POPUP) == 0) ? 0u : 1u);
                    }
                }

                if (cmds.Value.sound_id_ref >= 0 && cmds.Value.sound_id_ref < 0xff)
                {
                    _queue_event(bd, bd_event_e.BD_EVENT_SOUND_EFFECT, (uint)cmds.Value.sound_id_ref);
                }

                bd.Value.gc_uo_mask = cmds.Value.page_uo_mask;
                _update_uo_mask(bd);

            }
            else
            {
                if ((bd.Value.gc_status & GraphicsController.GC_STATUS_MENU_OPEN) != 0)
                {
                    _queue_event(bd, bd_event_e.BD_EVENT_MENU, 0);
                }
                if ((bd.Value.gc_status & GraphicsController.GC_STATUS_POPUP) != 0)
                {
                    _queue_event(bd, bd_event_e.BD_EVENT_POPUP, 0);
                }
                bd.Value.gc_status = GraphicsController.GC_STATUS_NONE;
            }

            return result;
        }

        /*
         * disc info
         */

        static void _check_bdj(Ref<BLURAY> bd)
        {
            if (bd.Value.disc_info.Value.bdj_handled == 0)
            {
                if (bd.Value.disc == null || bd.Value.disc_info.Value.bdj_detected == 0)
                {

                    /* Check if jvm + jar can be loaded ? */
                    switch (BDJ.bdj_jvm_available(bd.Value.bdj_config))
                    {
                        case BdjStatus.BDJ_CHECK_OK:
                            bd.Value.disc_info.Value.bdj_handled = 1;
                            goto case BdjStatus.BDJ_CHECK_NO_JAR;
                        /* fall thru */
                        case BdjStatus.BDJ_CHECK_NO_JAR:
                            bd.Value.disc_info.Value.libjvm_detected = 1;
                            goto case default;
                        /* fall thru */
                        default:
                            break;
                    }
                }
            }
        }

        static void _fill_disc_info(Ref<BLURAY> bd, Ref<BD_ENC_INFO> enc_info)
        {
            Ref<INDX_ROOT> index = Ref<INDX_ROOT>.Null;

            if (enc_info)
            {
                bd.Value.disc_info.Value.aacs_detected = enc_info.Value.aacs_detected;
                bd.Value.disc_info.Value.libaacs_detected = enc_info.Value.libaacs_detected;
                bd.Value.disc_info.Value.aacs_error_code = enc_info.Value.aacs_error_code;
                bd.Value.disc_info.Value.aacs_handled = enc_info.Value.aacs_handled;
                bd.Value.disc_info.Value.aacs_mkbv = enc_info.Value.aacs_mkbv;
                Array.Copy(enc_info.Value.disc_id, bd.Value.disc_info.Value.disc_id, 20);
                bd.Value.disc_info.Value.bdplus_detected = enc_info.Value.bdplus_detected;
                bd.Value.disc_info.Value.libbdplus_detected = enc_info.Value.libbdplus_detected;
                bd.Value.disc_info.Value.bdplus_handled = enc_info.Value.bdplus_handled;
                bd.Value.disc_info.Value.bdplus_gen = enc_info.Value.bdplus_gen;
                bd.Value.disc_info.Value.bdplus_date = enc_info.Value.bdplus_date;
                bd.Value.disc_info.Value.no_menu_support = enc_info.Value.no_menu_support;
            }

            bd.Value.disc_info.Value.bluray_detected = 0;
            bd.Value.disc_info.Value.top_menu_supported = 0;
            bd.Value.disc_info.Value.first_play_supported = 0;
            bd.Value.disc_info.Value.num_hdmv_titles = 0;
            bd.Value.disc_info.Value.num_bdj_titles = 0;
            bd.Value.disc_info.Value.num_unsupported_titles = 0;

            bd.Value.disc_info.Value.bdj_detected = 0;
            bd.Value.disc_info.Value.bdj_supported = 1;

            bd.Value.disc_info.Value.num_titles = 0;
            bd.Value.disc_info.Value.titles = Ref<BLURAY_TITLE>.Null;
            bd.Value.disc_info.Value.top_menu = Ref<BLURAY_TITLE>.Null;
            bd.Value.disc_info.Value.first_play = Ref<BLURAY_TITLE>.Null;

            bd.Value.titles.Free();

            bd.Value.disc_info.Value.bdj_org_id = "";
            bd.Value.disc_info.Value.bdj_disc_id = "";

            if (bd.Value.disc != null)
            {
                bd.Value.disc_info.Value.udf_volume_id = Disc.disc_volume_id(bd.Value.disc);
                index = IndexParse.indx_get(bd.Value.disc);
                if (!index)
                {
                    /* check for incomplete disc */
                    Ref<NAV_TITLE_LIST> title_list = Navigation.nav_get_title_list(bd.Value.disc, 0, 0);
                    if (title_list && title_list.Value.count > 0)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Possible incomplete BluRay image detected. No menu support.");
                        bd.Value.disc_info.Value.bluray_detected = 1;
                        bd.Value.disc_info.Value.no_menu_support = 1;
                    }
                    Navigation.nav_free_title_list(ref title_list);
                }
            }

            if (index)
            {
                Ref<INDX_PLAY_ITEM> pi;
                uint ii;

                bd.Value.disc_info.Value.bluray_detected = 1;

                /* application info */
                bd.Value.disc_info.Value.video_format = (byte)index.Value.app_info.Value.video_format;
                bd.Value.disc_info.Value.frame_rate = (byte)index.Value.app_info.Value.frame_rate;
                bd.Value.disc_info.Value.initial_dynamic_range_type = (byte)index.Value.app_info.Value.initial_dynamic_range_type;
                bd.Value.disc_info.Value.content_exist_3D = (byte)index.Value.app_info.Value.content_exist_flag;
                bd.Value.disc_info.Value.initial_output_mode_preference = (byte)index.Value.app_info.Value.initial_output_mode_preference;
                bd.Value.disc_info.Value.provider_data = index.Value.app_info.Value.user_data;

                /* allocate array for title info */
                Ref<BLURAY_TITLE> titles = Ref<BLURAY_TITLE>.Allocate(index.Value.num_titles + 2);

                bd.Value.titles = titles;
                bd.Value.disc_info.Value.titles = titles;
                bd.Value.disc_info.Value.num_titles = index.Value.num_titles;

                /* count titles and fill title info */

                for (ii = 0; ii < index.Value.num_titles; ii++)
                {
                    if (index.Value.titles[ii].object_type == indx_object_type.indx_object_type_hdmv)
                    {
                        bd.Value.disc_info.Value.num_hdmv_titles++;
                        titles[ii + 1].interactive = (byte)((index.Value.titles[ii].hdmv.Value.playback_type == indx_hdmv_playback_type.indx_hdmv_playback_type_interactive) ? 1u : 0u);
                        titles[ii + 1].id_ref = index.Value.titles[ii].hdmv.Value.id_ref;
                    }
                    if (index.Value.titles[ii].object_type == indx_object_type.indx_object_type_bdj)
                    {
                        bd.Value.disc_info.Value.num_bdj_titles++;
                        bd.Value.disc_info.Value.bdj_detected = 1;
                        titles[ii + 1].bdj = 1;
                        titles[ii + 1].interactive = (byte)((index.Value.titles[ii].bdj.Value.playback_type == indx_bdj_playback_type.indx_bdj_playback_type_interactive) ? 1u : 0u);
                        titles[ii + 1].id_ref = uint.Parse(index.Value.titles[ii].bdj.Value.name);
                    }

                    titles[ii + 1].accessible = (byte)(((index.Value.titles[ii].access_type & IndexParse.INDX_ACCESS_PROHIBITED_MASK) == 0) ? 1u : 0u);
                    titles[ii + 1].hidden = (byte)(((index.Value.titles[ii].access_type & IndexParse.INDX_ACCESS_HIDDEN_MASK) == 0) ? 0u : 1u);
                }

                pi = index.Value.first_play.Ref;
                if (pi.Value.object_type == indx_object_type.indx_object_type_bdj)
                {
                    bd.Value.disc_info.Value.bdj_detected = 1;
                    titles[index.Value.num_titles + 1].bdj = 1;
                    titles[index.Value.num_titles + 1].interactive = (byte)((pi.Value.bdj.Value.playback_type == indx_bdj_playback_type.indx_bdj_playback_type_interactive) ? 1u : 0u);
                    titles[index.Value.num_titles + 1].id_ref = uint.Parse(pi.Value.bdj.Value.name);
                }
                if (pi.Value.object_type == indx_object_type.indx_object_type_hdmv && pi.Value.hdmv.Value.id_ref != 0xffff)
                {
                    titles[index.Value.num_titles + 1].interactive = (byte)((pi.Value.hdmv.Value.playback_type == indx_hdmv_playback_type.indx_hdmv_playback_type_interactive) ? 1u : 0u);
                    titles[index.Value.num_titles + 1].id_ref = pi.Value.hdmv.Value.id_ref;
                }

                pi = index.Value.top_menu.Ref;
                if (pi.Value.object_type == indx_object_type.indx_object_type_bdj)
                {
                    bd.Value.disc_info.Value.bdj_detected = 1;
                    titles[0].bdj = 1;
                    titles[0].interactive = (byte)((pi.Value.bdj.Value.playback_type == indx_bdj_playback_type.indx_bdj_playback_type_interactive) ? 1u : 0u);
                    titles[0].id_ref = uint.Parse(pi.Value.bdj.Value.name);
                }
                if (pi.Value.object_type == indx_object_type.indx_object_type_hdmv && pi.Value.hdmv.Value.id_ref != 0xffff)
                {
                    titles[0].interactive = (byte)((pi.Value.hdmv.Value.playback_type == indx_hdmv_playback_type.indx_hdmv_playback_type_interactive) ? 1u : 0u);
                    titles[0].id_ref = pi.Value.hdmv.Value.id_ref;
                }

                /* mark supported titles */

                _check_bdj(bd);

                if (bd.Value.disc_info.Value.bdj_detected != 0 && bd.Value.disc_info.Value.bdj_handled == 0)
                {
                    bd.Value.disc_info.Value.num_unsupported_titles = bd.Value.disc_info.Value.num_bdj_titles;
                }

                pi = index.Value.first_play.Ref;
                if (pi.Value.object_type == indx_object_type.indx_object_type_hdmv && pi.Value.hdmv.Value.id_ref != 0xffff)
                {
                    bd.Value.disc_info.Value.first_play_supported = 1;
                }
                if (pi.Value.object_type == indx_object_type.indx_object_type_bdj)
                {
                    bd.Value.disc_info.Value.first_play_supported = bd.Value.disc_info.Value.bdj_handled;
                }

                pi = index.Value.top_menu.Ref;
                if (pi.Value.object_type == indx_object_type.indx_object_type_hdmv && pi.Value.hdmv.Value.id_ref != 0xffff)
                {
                    bd.Value.disc_info.Value.top_menu_supported = 1;
                }
                if (pi.Value.object_type == indx_object_type.indx_object_type_bdj)
                {
                    bd.Value.disc_info.Value.top_menu_supported = bd.Value.disc_info.Value.bdj_handled;
                }

                /* */

                if (bd.Value.disc_info.Value.first_play_supported != 0)
                {
                    titles[index.Value.num_titles + 1].accessible = 1;
                    bd.Value.disc_info.Value.first_play = titles.AtIndex(index.Value.num_titles + 1);
                }
                if (bd.Value.disc_info.Value.top_menu_supported != 0)
                {
                    titles[0].accessible = 1;
                    bd.Value.disc_info.Value.top_menu = titles.AtIndex(0);
                }

                /* increase player profile and version when 3D or UHD disc is detected */

                if (index.Value.indx_version.Value >= (('0' << 24) | ('3' << 16) | ('0' << 8) | '0'))
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"Detected 4K UltraHD (profile 6) disc");
                    /* Switch to UHD profile */
                    Register.psr_init_UHD(bd.Value.regs, 1);
                }
                if (((index.Value.indx_version.Value >> 16) & 0xff) == '2')
                {
                    if (index.Value.app_info.Value.content_exist_flag != 0)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"Detected Blu-Ray 3D (profile 5) disc");
                        /* Switch to 3D profile */
                        Register.psr_init_3D(bd.Value.regs, (int)index.Value.app_info.Value.initial_output_mode_preference, 0);
                    }
                }

                IndexParse.indx_free(ref index);

                /* populate title names */
                bd_get_meta(bd);
            }

#if false
    if (!bd.Value.disc_info.first_play_supported || !bd.Value.disc_info.top_menu_supported) {
        bd.Value.disc_info.no_menu_support = 1;
    }
#endif

            if (bd.Value.disc_info.Value.bdj_detected != 0)
            {
                Ref<BDID_DATA> bdid = BdidParse.bdid_get(bd.Value.disc); /* parse id.bdmv */
                if (bdid)
                {
                    bd.Value.disc_info.Value.bdj_org_id = bdid.Value.org_id;
                    bd.Value.disc_info.Value.bdj_disc_id = bdid.Value.disc_id;
                    BdidParse.bdid_free(ref bdid);
                }
            }

            _check_bdj(bd);
        }

        public static Ref<BLURAY_DISC_INFO> bd_get_disc_info(Ref<BLURAY> bd)
        {
            bd.Value.mutex.bd_mutex_lock();
            if (bd.Value.disc == null)
            {
                _fill_disc_info(bd, Ref<BD_ENC_INFO>.Null);
            }
            bd.Value.mutex.bd_mutex_unlock();
            return bd.Value.disc_info.Ref;
        }

        /*
         * bdj callbacks
         */

        const int BDJ_MENU_CALL_MASK = 0x01;
        const int BDJ_TITLE_SEARCH_MASK = 0x02;

        public static void bd_set_bdj_uo_mask(Ref<BLURAY> bd, uint mask)
        {
            bd.Value.title_uo_mask.title_search = (mask & BDJ_TITLE_SEARCH_MASK) != 0;
            bd.Value.title_uo_mask.menu_call = (mask & BDJ_MENU_CALL_MASK) != 0;

            _update_uo_mask(bd);
        }

        public static UInt64 bd_get_uo_mask(Ref<BLURAY> bd)
        {
            /* internal function. Used by BD-J. */
            BD_UO_MASK mask;

            //bd.Value.mutex.bd_mutex_lock();
            mask = bd.Value.uo_mask;
            //bd.Value.mutex.bd_mutex_unlock();

            return mask.AsInt;
        }

        public static void bd_set_bdj_kit(Ref<BLURAY> bd, int mask)
        {
            _queue_event(bd, bd_event_e.BD_EVENT_KEY_INTEREST_TABLE, (uint)mask);
        }

        public static int bd_bdj_sound_effect(Ref<BLURAY> bd, int id)
        {
            if (bd.Value.sound_effects && id >= bd.Value.sound_effects.Value.num_sounds)
            {
                return -1;
            }
            if (id < 0 || id > 0xff)
            {
                return -1;
            }

            _queue_event(bd, bd_event_e.BD_EVENT_SOUND_EFFECT, (uint)id);
            return 0;
        }

        public enum bd_select_rate_reason
        {
            BDJ_RATE_SET = 0,
            BDJ_PLAYBACK_START = 1,
            BDJ_PLAYBACK_STOP = 2,
        }

        public static void bd_select_rate(Ref<BLURAY> bd, float rate, bd_select_rate_reason reason)
        {
            if (reason == bd_select_rate_reason.BDJ_PLAYBACK_STOP)
            {
                /* playback stop. Might want to wait for buffers empty here. */
                return;
            }

            if (reason == bd_select_rate_reason.BDJ_PLAYBACK_START)
            {
                /* playback is triggered by bd_select_rate() */
                bd.Value.bdj_wait_start = 0;
            }

            if (rate < 0.5)
            {
                _queue_event(bd, bd_event_e.BD_EVENT_STILL, 1);
            }
            else
            {
                _queue_event(bd, bd_event_e.BD_EVENT_STILL, 0);
            }
        }

        public static int bd_bdj_seek(Ref<BLURAY> bd, int playitem, int playmark, Int64 time)
        {
            bd.Value.mutex.bd_mutex_lock();

            if (playitem > 0)
            {
                bd_seek_playitem(bd, (uint)playitem);
            }
            if (playmark >= 0)
            {
                bd_seek_mark(bd, (uint)playmark);
            }
            if (time >= 0)
            {
                bd_seek_time(bd, (uint)time);
            }

            bd.Value.mutex.bd_mutex_unlock();

            return 1;
        }

        static int _bd_set_virtual_package(Ref<BLURAY> bd, string vp_path, int psr_init_backup)
        {
            if (bd.Value.title != null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_set_virtual_package() failed: playlist is playing");
                return -1;
            }
            if (bd.Value.title_type != BD_TITLE_TYPE.title_bdj)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_set_virtual_package() failed: HDMV title");
                return -1;
            }

            if (psr_init_backup != 0)
            {
                Register.bd_psr_reset_backup_registers(bd.Value.regs);
            }

            Disc.disc_update(bd.Value.disc, vp_path);

            /* TODO: reload all cached information, update disc info, notify app */

            return 0;
        }

        public static int bd_set_virtual_package(Ref<BLURAY> bd, string vp_path, int psr_init_backup)
        {
            int ret;
            bd.Value.mutex.bd_mutex_lock();
            ret = _bd_set_virtual_package(bd, vp_path, psr_init_backup);
            bd.Value.mutex.bd_mutex_unlock();
            return ret;
        }

        public static BD_DISC? bd_get_disc(Ref<BLURAY> bd)
        {
            return bd ? bd.Value.disc : null;
        }

        public static UInt32 bd_reg_read(Ref<BLURAY> bd, int psr, bd_psr_idx reg)
        {
            if (psr != 0)
            {
                return Register.bd_psr_read(bd.Value.regs, reg);
            }
            else
            {
                return Register.bd_gpr_read(bd.Value.regs, reg);
            }
        }

        public static int bd_reg_write(Ref<BLURAY> bd, int psr, bd_psr_idx reg, UInt32 value, UInt32 psr_value_mask)
        {
            if (psr != 0)
            {
                if (psr < 102)
                {
                    /* avoid deadlocks (psr_write triggers callbacks that may lock this mutex) */
                    bd.Value.mutex.bd_mutex_lock();
                }
                int res = Register.bd_psr_write_bits(bd.Value.regs, reg, value, psr_value_mask);
                if (psr < 102)
                {
                    bd.Value.mutex.bd_mutex_unlock();
                }
                return res;
            }
            else
            {
                return Register.bd_gpr_write(bd.Value.regs, reg, value);
            }
        }

        public static Ref<BD_ARGB_BUFFER> bd_lock_osd_buffer(Ref<BLURAY> bd)
        {
            bd.Value.argb_buffer_mutex.bd_mutex_lock();
            return bd.Value.argb_buffer;
        }

        public static void bd_unlock_osd_buffer(Ref<BLURAY> bd)
        {
            bd.Value.argb_buffer_mutex.bd_mutex_unlock();
        }

        /*
         * handle graphics updates from BD-J layer
         */
        public static void bd_bdj_osd_cb(Ref<BLURAY> bd, Ref<uint> img, int w, int h,
                           int x0, int y0, int x1, int y1)
        {
            Variable<BD_ARGB_OVERLAY> aov = new();

            if (bd.Value.argb_overlay_proc == null)
            {
                _queue_event(bd, bd_event_e.BD_EVENT_MENU, 0);
                return;
            }

            aov = new();
            aov.Value.pts = ulong.MaxValue;
            aov.Value.plane = (byte)bd_overlay_plane_e.BD_OVERLAY_IG;

            /* no image data .Value. init or close */
            if (!img)
            {
                if (w > 0 && h > 0)
                {
                    aov.Value.cmd = (byte)bd_argb_overlay_cmd_e.BD_ARGB_OVERLAY_INIT;
                    aov.Value.w = (ushort)w;
                    aov.Value.h = (ushort)h;
                    _queue_event(bd, bd_event_e.BD_EVENT_MENU, 1);
                }
                else
                {
                    aov.Value.cmd = (byte)bd_argb_overlay_cmd_e.BD_ARGB_OVERLAY_CLOSE;
                    _queue_event(bd, bd_event_e.BD_EVENT_MENU, 0);
                }

                bd.Value.argb_overlay_proc(bd.Value.argb_overlay_proc_handle, aov.Ref);
                return;
            }

            /* no changed pixels ? */
            if (x1 < x0 || y1 < y0)
            {
                return;
            }

            /* pass only changed region */
            if (bd.Value.argb_buffer && (bd.Value.argb_buffer.Value.width < w || bd.Value.argb_buffer.Value.height < h))
            {
                aov.Value.argb = img;
            }
            else
            {
                aov.Value.argb = img + x0 + y0 * w;
            }
            aov.Value.stride = (ushort)w;
            aov.Value.x = (ushort)x0;
            aov.Value.y = (ushort)y0;
            aov.Value.w = (ushort)(x1 - x0 + 1);
            aov.Value.h = (ushort)(y1 - y0 + 1);

            if (bd.Value.argb_buffer)
            {
                /* set dirty region */
                bd.Value.argb_buffer.Value.dirty[(int)bd_overlay_plane_e.BD_OVERLAY_IG].x0 = (ushort)x0;
                bd.Value.argb_buffer.Value.dirty[(int)bd_overlay_plane_e.BD_OVERLAY_IG].x1 = (ushort)x1;
                bd.Value.argb_buffer.Value.dirty[(int)bd_overlay_plane_e.BD_OVERLAY_IG].y0 = (ushort)y0;
                bd.Value.argb_buffer.Value.dirty[(int)bd_overlay_plane_e.BD_OVERLAY_IG].y1 = (ushort)y1;
            }

            /* draw */
            aov.Value.cmd = (byte)bd_argb_overlay_cmd_e.BD_ARGB_OVERLAY_DRAW;
            bd.Value.argb_overlay_proc(bd.Value.argb_overlay_proc_handle, aov.Ref);

            /* commit changes */
            aov.Value.cmd = (byte)bd_argb_overlay_cmd_e.BD_ARGB_OVERLAY_FLUSH;
            bd.Value.argb_overlay_proc(bd.Value.argb_overlay_proc_handle, aov.Ref);

            if (bd.Value.argb_buffer)
            {
                /* reset dirty region */
                bd.Value.argb_buffer.Value.dirty[(int)bd_overlay_plane_e.BD_OVERLAY_IG].x0 = (ushort)bd.Value.argb_buffer.Value.width;
                bd.Value.argb_buffer.Value.dirty[(int)bd_overlay_plane_e.BD_OVERLAY_IG].x1 = (ushort)bd.Value.argb_buffer.Value.height;
                bd.Value.argb_buffer.Value.dirty[(int)bd_overlay_plane_e.BD_OVERLAY_IG].y0 = 0;
                bd.Value.argb_buffer.Value.dirty[(int)bd_overlay_plane_e.BD_OVERLAY_IG].y1 = 0;
            }
        }

        /*
         * BD-J
         */

        static bool _start_bdj(Ref<BLURAY> bd, uint title)
        {
            if (bd.Value.bdjava == null)
            {
                string root = bd.Value.disc.disc_root();
                bd.Value.bdjava = BDJ.bdj_open(root, bd, bd.Value.disc_info.Value.bdj_disc_id, ref bd.Value.bdj_config);
                if (!bd.Value.bdjava)
                {
                    return false;
                }
            }

            return !(BDJ.bdj_process_event(bd.Value.bdjava, BDJ_EVENT.BDJ_EVENT_START, title) != 0);
        }

        static int _bdj_event(Ref<BLURAY> bd, BDJ_EVENT ev, uint param)
        {
            if (bd.Value.bdjava != null)
            {
                return BDJ.bdj_process_event(bd.Value.bdjava, ev, param);
            }
            return -1;
        }

        static void _stop_bdj(Ref<BLURAY> bd)
        {
            if (bd.Value.bdjava != null)
            {
                BDJ.bdj_process_event(bd.Value.bdjava, BDJ_EVENT.BDJ_EVENT_STOP, 0);
                _queue_event(bd, bd_event_e.BD_EVENT_STILL, 0);
                _queue_event(bd, bd_event_e.BD_EVENT_KEY_INTEREST_TABLE, 0);
            }
        }

        static void _close_bdj(Ref<BLURAY> bd)
        {
            if (bd.Value.bdjava != null)
            {
                BDJ.bdj_close(bd.Value.bdjava);
                bd.Value.bdjava = Ref<BDJAVA>.Null;
            }
        }

        /*
         * open / close
         */

        public static Ref<BLURAY> bd_init()
        {
            string? env;

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"libbluray version {BlurayVersion.BLURAY_VERSION_STRING}");

            Ref<BLURAY> bd = Ref<BLURAY>.Allocate();

            bd.Value.regs = Register.bd_registers_init();
            if (!bd.Value.regs)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"bd_registers_init() failed");
                bd.Free();
                return Ref<BLURAY>.Null;
            }

            bd.Value.mutex = new();
            bd.Value.argb_buffer_mutex = new();

            env = Environment.GetEnvironmentVariable("LIBBLURAY_PERSISTENT_STORAGE");
            if (env != null)
            {
                int v = ((env == "yes")) ? 1 : ((env == "no")) ? 0 : int.Parse(env);
                bd.Value.bdj_config.no_persistent_storage = (byte)((!(v != 0)) ? 1 : 0);
            }

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"BLURAY initialized!");

            return bd;
        }

        static bool _bd_open(Ref<BLURAY> bd,
                            string device_path, string keyfile_path,
                            fs_access? p_fs)
        {
            Variable<BD_ENC_INFO> enc_info = new();

            if (!bd)
            {
                return false;
            }

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.disc != null)
            {
                bd.Value.mutex.bd_mutex_unlock();
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Disc already open");
                return false;
            }

            bd.Value.disc = Disc.disc_open(device_path, p_fs,
                                 enc_info.Ref, keyfile_path,
                                 bd.Value.regs, Register.bd_psr_read, Register.bd_psr_write);

            if (bd.Value.disc == null)
            {
                bd.Value.mutex.bd_mutex_unlock();
                return false;
            }

            _fill_disc_info(bd, enc_info.Ref);

            bd.Value.mutex.bd_mutex_unlock();

            return bd.Value.disc_info.Value.bluray_detected != 0;
        }

        public static bool bd_open_disc(Ref<BLURAY> bd, string device_path, string keyfile_path)
        {
            if (device_path == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"No device path provided!");
                return false;
            }

            return _bd_open(bd, device_path, keyfile_path, null);
        }

        //public static bool bd_open_stream(Ref<BLURAY> bd,
        //                   object? read_blocks_handle, Func<object? /*handle*/, object? /*buffer*/, int /*lba*/, int /*num_blocks*/, int> read_blocks)
        /*{
            if (read_blocks == null)
            {
                return false;
            }

            fs_access fs = new();
            fs.fs_handle = read_blocks_handle;
            fs.read_blocks = read_blocks;
            return _bd_open(bd, null, null, fs);
        }*/

        //public static bool bd_open_files(Ref<BLURAY> bd,
        //                  object? handle,
        //                  Func<object? /*handle*/, string /*rel_path*/, BD_DIR_H> open_dir,
        //                  Func<object? /*handle*/, string /*rel_path*/, BD_FILE_H> open_file)
        /*{
            if (open_dir == null || open_file = null) {
                return false;
            }

            fs_access fs = new();
            fs.fs_handle = handle;
            fs.read_blocks = null;
            fs.open_dir = open_dir;
            fs.open_file = open_file;

            return _bd_open(bd, null, null, fs);
        }*/

        public static Ref<BLURAY> bd_open(string device_path, string keyfile_path)
        {
            Ref<BLURAY> bd;

            bd = bd_init();
            if (!bd)
            {
                return Ref<BLURAY>.Null;
            }

            if (!bd_open_disc(bd, device_path, keyfile_path))
            {
                bd_close(bd);
                return Ref<BLURAY>.Null;
            }

            return bd;
        }

        public static void bd_close(Ref<BLURAY> bd)
        {
            if (!bd)
            {
                return;
            }

            _close_bdj(bd);

            _close_m2ts(bd.Value.st0.Ref);
            _close_preload(ref bd.Value.st_ig.Value);
            _close_preload(ref bd.Value.st_textst.Value);

            Navigation.nav_free_title_list(ref bd.Value.title_list);
            Navigation.nav_title_close(ref bd.Value.title);

            HdmvVm.hdmv_vm_free(ref bd.Value.hdmv_vm);

            GraphicsController.gc_free(ref bd.Value.graphics_controller);
            MetaParse.meta_free(ref bd.Value.meta);
            SoundParse.sound_free(ref bd.Value.sound_effects);
            Register.bd_registers_free(bd.Value.regs);

            BD_EVENT_QUEUE<BD_EVENT>.event_queue_destroy(ref bd.Value.event_queue);
            bd.Value.titles.Free();
            BDJ.bdj_config_cleanup(ref bd.Value.bdj_config);

            Disc.disc_close(ref bd.Value.disc);

            bd.Value.mutex.bd_mutex_destroy();
            bd.Value.argb_buffer_mutex.bd_mutex_destroy();

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"BLURAY destroyed!");

            bd.Free();
        }

        /*
         * PlayMark tracking
         */

        static void _find_next_playmark(Ref<BLURAY> bd)
        {
            uint ii;

            bd.Value.next_mark = -1;
            bd.Value.next_mark_pos = ulong.MaxValue;
            for (ii = 0; ii < bd.Value.title.mark_list.count; ii++)
            {
                UInt64 pos = (UInt64)bd.Value.title.mark_list.mark[ii].title_pkt * 192L;
                if (pos > bd.Value.s_pos)
                {
                    bd.Value.next_mark = (int)ii;
                    bd.Value.next_mark_pos = pos;
                    break;
                }
            }

            _update_chapter_psr(bd);
        }

        static void _playmark_reached(Ref<BLURAY> bd)
        {
            while (bd.Value.next_mark >= 0 && bd.Value.s_pos > bd.Value.next_mark_pos)
            {

                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"PlayMark {bd.Value.next_mark} reached ({bd.Value.next_mark_pos})");

                _queue_event(bd, bd_event_e.BD_EVENT_PLAYMARK, (uint)bd.Value.next_mark);
                _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_MARK, (uint)bd.Value.next_mark);

                /* update next mark */
                bd.Value.next_mark++;
                if ((uint)bd.Value.next_mark < bd.Value.title.mark_list.count)
                {
                    bd.Value.next_mark_pos = (UInt64)bd.Value.title.mark_list.mark[bd.Value.next_mark].title_pkt * 192L;
                }
                else
                {
                    /* no marks left */
                    bd.Value.next_mark = -1;
                    bd.Value.next_mark_pos = ulong.MaxValue;
                }
            };

            /* chapter tracking */
            _update_chapter_psr(bd);
        }

        /*
         * seeking and current position
         */

        static void _seek_internal(Ref<BLURAY> bd,
                                   Ref<NAV_CLIP> clip, UInt32 title_pkt, UInt32 clip_pkt)
        {
            if (_seek_stream(bd, bd.Value.st0.Ref, clip, clip_pkt) >= 0)
            {
                UInt32 media_time;

                /* update title position */
                bd.Value.s_pos = (UInt64)title_pkt * 192;

                /* Update PSR_TIME */
                media_time = _update_time_psr_from_stream(bd);

                /* emit notification events */
                if (media_time >= clip.Value.in_time)
                {
                    media_time = media_time - clip.Value.in_time + clip.Value.title_time;
                }
                _queue_event(bd, bd_event_e.BD_EVENT_SEEK, media_time);
                _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_SEEK, media_time);

                /* playmark tracking */
                _find_next_playmark(bd);

                /* reset PG decoder and controller */
                if (bd.Value.graphics_controller)
                {
                    GraphicsController.gc_run(bd.Value.graphics_controller, gc_ctrl_e.GC_CTRL_PG_RESET, 0, Ref<GC_NAV_CMDS>.Null);

                    _init_textst_timer(bd);
                }

                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"Seek to {bd.Value.s_pos}");
            }
        }

        /* _change_angle() should be used only before call to _seek_internal() ! */
        static void _change_angle(Ref<BLURAY> bd)
        {
            if (bd.Value.seamless_angle_change != 0)
            {
                Navigation.nav_set_angle(bd.Value.title, bd.Value.request_angle);
                bd.Value.seamless_angle_change = 0;
                Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_ANGLE_NUMBER, (uint)(bd.Value.title.angle + 1));

                /* force re-opening .m2ts file in _seek_internal() */
                _close_m2ts(bd.Value.st0.Ref);
            }
        }

        public static Int64 bd_seek_time(Ref<BLURAY> bd, UInt64 tick)
        {
            Variable<UInt32> clip_pkt = new(), out_pkt = new();
            Ref<NAV_CLIP> clip;

            if ((tick >> 33) != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_seek_time({tick}) failed: invalid timestamp");
                return (long)bd.Value.s_pos;
            }

            tick /= 2;

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title != null &&
                tick < bd.Value.title.duration)
            {

                _change_angle(bd);

                // Find the closest access unit to the requested position
                clip = Navigation.nav_time_search(bd.Value.title, (UInt32)tick, clip_pkt.Ref, out_pkt.Ref);

                _seek_internal(bd, clip, out_pkt.Value, clip_pkt.Value);

            }
            else
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_seek_time({tick}) failed");
            }

            bd.Value.mutex.bd_mutex_unlock();

            return (long)bd.Value.s_pos;
        }

        public static UInt64 bd_tell_time(Ref<BLURAY> bd)
        {
            Variable<UInt32> clip_pkt = new(0), out_pkt = new(0), out_time = new(0);
            Ref<NAV_CLIP> clip;

            if (!bd)
            {
                return 0;
            }

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title != null)
            {
                clip = Navigation.nav_packet_search(bd.Value.title, SPN(bd.Value.s_pos), clip_pkt.Ref, out_pkt.Ref, out_time.Ref);
                if (clip)
                {
                    out_time.Value += clip.Value.title_time;
                }
            }

            bd.Value.mutex.bd_mutex_unlock();

            return ((UInt64)out_time.Value) * 2;
        }

        public static Int64 bd_seek_chapter(Ref<BLURAY> bd, uint chapter)
        {
            Variable<UInt32> clip_pkt = new(), out_pkt = new();
            Ref<NAV_CLIP> clip;

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title != null &&
                chapter < bd.Value.title.chap_list.count)
            {

                _change_angle(bd);

                // Find the closest access unit to the requested position
                clip = Navigation.nav_chapter_search(bd.Value.title, chapter, clip_pkt.Ref, out_pkt.Ref);

                _seek_internal(bd, clip, out_pkt.Value, clip_pkt.Value);

            }
            else
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_seek_chapter({chapter}) failed");
            }

            bd.Value.mutex.bd_mutex_unlock();

            return (long)bd.Value.s_pos;
        }

        public static Int64 bd_chapter_pos(Ref<BLURAY> bd, uint chapter)
        {
            Variable<UInt32> clip_pkt = new(), out_pkt = new();
            Int64 ret = -1;

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title != null &&
                chapter < bd.Value.title.chap_list.count)
            {

                // Find the closest access unit to the requested position
                Navigation.nav_chapter_search(bd.Value.title, chapter, clip_pkt.Ref, out_pkt.Ref);
                ret = (Int64)out_pkt.Value * 192;
            }

            bd.Value.mutex.bd_mutex_unlock();

            return ret;
        }

        public static UInt32 bd_get_current_chapter(Ref<BLURAY> bd)
        {
            UInt32 ret = 0;

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title != null)
            {
                ret = Navigation.nav_chapter_get_current(bd.Value.title, SPN(bd.Value.s_pos));
            }

            bd.Value.mutex.bd_mutex_unlock();

            return ret;
        }

        public static Int64 bd_seek_playitem(Ref<BLURAY> bd, uint clip_ref)
        {
            UInt32 clip_pkt, out_pkt;
            Ref<NAV_CLIP> clip;

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title != null &&
                clip_ref < bd.Value.title.clip_list.count)
            {

                _change_angle(bd);

                clip = bd.Value.title.clip_list.clip.AtIndex(clip_ref);
                clip_pkt = clip.Value.start_pkt;
                out_pkt = clip.Value.title_pkt;

                _seek_internal(bd, clip, out_pkt, clip_pkt);

            }
            else
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_seek_playitem({clip_ref}) failed");
            }

            bd.Value.mutex.bd_mutex_unlock();

            return (long)bd.Value.s_pos;
        }

        public static Int64 bd_seek_mark(Ref<BLURAY> bd, uint mark)
        {
            Variable<UInt32> clip_pkt = new(), out_pkt = new();
            Ref<NAV_CLIP> clip;

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title != null &&
                mark < bd.Value.title.mark_list.count)
            {

                _change_angle(bd);

                // Find the closest access unit to the requested position
                clip = Navigation.nav_mark_search(bd.Value.title, mark, clip_pkt.Ref, out_pkt.Ref);

                _seek_internal(bd, clip, out_pkt.Value, clip_pkt.Value);

            }
            else
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_seek_mark({mark}) failed");
            }

            bd.Value.mutex.bd_mutex_unlock();

            return (long)bd.Value.s_pos;
        }

        public static Int64 bd_seek(Ref<BLURAY> bd, UInt64 pos)
        {
            Variable<UInt32> pkt = new(), clip_pkt = new(), out_pkt = new(), out_time = new();
            Ref<NAV_CLIP> clip;

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title != null &&
                pos < (UInt64)bd.Value.title.packets * 192)
            {

                pkt.Value = SPN(pos);

                _change_angle(bd);

                // Find the closest access unit to the requested position
                clip = Navigation.nav_packet_search(bd.Value.title, pkt.Value, clip_pkt.Ref, out_pkt.Ref, out_time.Ref);

                _seek_internal(bd, clip, out_pkt.Value, clip_pkt.Value);
            }

            bd.Value.mutex.bd_mutex_unlock();

            return (long)bd.Value.s_pos;
        }

        public static UInt64 bd_get_title_size(Ref<BLURAY> bd)
        {
            Variable<UInt64> ret = new(0);

            if (!bd)
            {
                return 0;
            }

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title != null)
            {
                ret.Value = (UInt64)bd.Value.title.packets * 192;
            }

            bd.Value.mutex.bd_mutex_unlock();

            return ret.Value;
        }

        public static UInt64 bd_tell(Ref<BLURAY> bd)
        {
            UInt64 ret = 0;

            if (!bd)
            {
                return 0;
            }

            bd.Value.mutex.bd_mutex_lock();

            ret = bd.Value.s_pos;

            bd.Value.mutex.bd_mutex_unlock();

            return ret;
        }

        /*
         * read
         */

        static Int64 _clip_seek_time(Ref<BLURAY> bd, UInt32 tick)
        {
            Variable<UInt32> clip_pkt = new(), out_pkt = new();

            if (bd.Value.title == null || !bd.Value.st0.Value.clip)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_clip_seek_time(): no playlist playing");
                return -1;
            }

            if (tick >= bd.Value.st0.Value.clip.Value.out_time)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_clip_seek_time(): timestamp after clip end ({bd.Value.st0.Value.clip.Value.out_time} < {tick})");
                return -1;
            }

            // Find the closest access unit to the requested position
            Navigation.nav_clip_time_search(bd.Value.st0.Value.clip, tick, clip_pkt.Ref, out_pkt.Ref);

            _seek_internal(bd, bd.Value.st0.Value.clip, out_pkt.Value, clip_pkt.Value);

            return (long)bd.Value.s_pos;
        }

        static int _bd_read(Ref<BLURAY> bd, Ref<byte> buf, int len)
        {
            Ref<BD_STREAM> st = bd.Value.st0.Ref;
            int out_len = 0;

            while (len > 0)
            {
                UInt32 clip_pkt;

                uint size = (uint)len;
                // Do we need to read more data?
                clip_pkt = SPN(st.Value.clip_pos);
                if (bd.Value.seamless_angle_change != 0)
                {
                    if (clip_pkt >= bd.Value.angle_change_pkt)
                    {
                        if (clip_pkt >= st.Value.clip.Value.end_pkt)
                        {
                            st.Value.clip = Navigation.nav_next_clip(bd.Value.title, st.Value.clip);
                            if (!_open_m2ts(bd, st))
                            {
                                return -1;
                            }
                            bd.Value.s_pos = (UInt64)st.Value.clip.Value.title_pkt * 192L;
                        }
                        else
                        {
                            _change_angle(bd);
                            _clip_seek_time(bd, bd.Value.angle_change_time.Value);
                        }
                        bd.Value.seamless_angle_change = 0;
                    }
                    else
                    {
                        UInt64 angle_pos;

                        angle_pos = (UInt64)bd.Value.angle_change_pkt * 192L;
                        if (angle_pos - st.Value.clip_pos < size)
                        {
                            size = (uint)(angle_pos - st.Value.clip_pos);
                        }
                    }
                }
                if (st.Value.int_buf_off == 6144 || clip_pkt >= st.Value.clip.Value.end_pkt)
                {

                    // Do we need to get the next clip?
                    if (clip_pkt >= st.Value.clip.Value.end_pkt)
                    {

                        // split read()'s at clip boundary
                        if (out_len != 0)
                        {
                            return out_len;
                        }

                        // handle still mode clips
                        if (st.Value.clip.Value.still_mode == (byte)bd_still_mode_e.BLURAY_STILL_INFINITE)
                        {
                            _queue_event(bd, bd_event_e.BD_EVENT_STILL_TIME, 0);
                            return 0;
                        }
                        if (st.Value.clip.Value.still_mode == (byte)bd_still_mode_e.BLURAY_STILL_TIME)
                        {
                            if (bd.Value.event_queue)
                            {
                                _queue_event(bd, bd_event_e.BD_EVENT_STILL_TIME, st.Value.clip.Value.still_time);
                                return 0;
                            }
                        }

                        // find next clip
                        st.Value.clip = Navigation.nav_next_clip(bd.Value.title, st.Value.clip);
                        if (st.Value.clip == null)
                        {
                            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_STREAM, $"End of title");
                            _queue_event(bd, bd_event_e.BD_EVENT_END_OF_TITLE, 0);
                            bd.Value.end_of_playlist |= 1;
                            return 0;
                        }
                        if (!_open_m2ts(bd, st))
                        {
                            return -1;
                        }

                        if (st.Value.clip.Value.connection == Navigation.CONNECT_NON_SEAMLESS)
                        {
                            /* application layer demuxer buffers must be reset here */
                            _queue_event(bd, bd_event_e.BD_EVENT_DISCONTINUITY, st.Value.clip.Value.in_time);
                        }

                    }

                    int r = _read_block(bd, st, new Ref<byte>(bd.Value.int_buf));
                    if (r > 0)
                    {

                        if (st.Value.ig_pid > 0)
                        {
                            if (GraphicsController.gc_decode_ts(bd.Value.graphics_controller, st.Value.ig_pid, new Ref<byte>(bd.Value.int_buf), 1, -1) > 0)
                            {
                                /* initialize menus */
                                _run_gc(bd, gc_ctrl_e.GC_CTRL_INIT_MENU, 0);
                            }
                        }
                        if (st.Value.pg_pid > 0)
                        {
                            if (GraphicsController.gc_decode_ts(bd.Value.graphics_controller, st.Value.pg_pid, new Ref<byte>(bd.Value.int_buf), 1, -1) > 0)
                            {
                                /* render subtitles */
                                GraphicsController.gc_run(bd.Value.graphics_controller, gc_ctrl_e.GC_CTRL_PG_UPDATE, 0, Ref<GC_NAV_CMDS>.Null);
                            }
                        }
                        if (bd.Value.st_textst.Value.clip)
                        {
                            _update_textst_timer(bd);
                        }

                        st.Value.int_buf_off = (ushort)(st.Value.clip_pos % 6144);

                    }
                    else if (r == 0)
                    {
                        /* recoverable error (EOF, broken block) */
                        return out_len;
                    }
                    else
                    {
                        /* fatal error */
                        return -1;
                    }

                    /* finetune seek point (avoid skipping PAT/PMT/PCR) */
                    if (st.Value.seek_flag != 0)
                    {
                        st.Value.seek_flag = 0;

                        /* rewind if previous packets contain PAT/PMT/PCR */
                        while (st.Value.int_buf_off >= 192 && HdmvPIDs.TS_PID(new Ref<byte>(bd.Value.int_buf) + st.Value.int_buf_off - 192) <= HdmvPIDs.HDMV_PID_PCR)
                        {
                            st.Value.clip_pos -= 192;
                            st.Value.int_buf_off -= 192;
                            bd.Value.s_pos -= 192;
                        }
                    }

                }
                if (size > (uint)6144 - st.Value.int_buf_off) {
                    size = (6144u - st.Value.int_buf_off);
                }

                /* cut read at clip end packet */
                UInt32 new_clip_pkt = SPN(st.Value.clip_pos + size);
                if (new_clip_pkt > st.Value.clip.Value.end_pkt)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_STREAM, $"cut {((new_clip_pkt - st.Value.clip.Value.end_pkt) * 192)} bytes at end of block");
                    size -= (new_clip_pkt - st.Value.clip.Value.end_pkt) * 192;
                }

                        /* copy chunk */
                        (new Ref<byte>(bd.Value.int_buf) + st.Value.int_buf_off).AsSpan().Slice(0, (int)size).CopyTo(buf.AsSpan());
                buf += size;
                len -= (int)size;
                out_len += (int)size;
                st.Value.clip_pos += size;
                st.Value.int_buf_off += (ushort)(size);
                bd.Value.s_pos += size;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_STREAM, $"{out_len} bytes read OK!");
            return out_len;
        }

        static int _bd_read_locked(Ref<BLURAY> bd, Ref<byte> buf, int len)
        {
            Ref<BD_STREAM> st = bd.Value.st0.Ref;
            int r;

            if (st.Value.fp == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_STREAM | DebugMaskEnum.DBG_CRIT, $"bd_read(): no valid title selected!");
                return -1;
            }

            if (st.Value.clip == null)
            {
                // We previously reached the last clip.  Nothing
                // else to read.
                _queue_event(bd, bd_event_e.BD_EVENT_END_OF_TITLE, 0);
                bd.Value.end_of_playlist |= 1;
                return 0;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_STREAM, $"Reading [{len} bytes] at {bd.Value.s_pos}...");

            r = _bd_read(bd, buf, len);

            /* mark tracking */
            if (bd.Value.next_mark >= 0 && bd.Value.s_pos > bd.Value.next_mark_pos)
            {
                _playmark_reached(bd);
            }

            return r;
        }

        public static int bd_read(Ref<BLURAY> bd, Ref<byte> buf, int len)
        {
            int result;

            bd.Value.mutex.bd_mutex_lock();
            result = _bd_read_locked(bd, buf, len);
            bd.Value.mutex.bd_mutex_unlock();

            return result;
        }

        public static bool bd_read_skip_still(Ref<BLURAY> bd)
        {
            Ref<BD_STREAM> st = bd.Value.st0.Ref;
            bool ret = false;

            bd.Value.mutex.bd_mutex_lock();

            if (st.Value.clip)
            {
                if (st.Value.clip.Value.still_mode == (byte)bd_still_mode_e.BLURAY_STILL_TIME)
                {
                    st.Value.clip = Navigation.nav_next_clip(bd.Value.title, st.Value.clip);
                    if (st.Value.clip)
                    {
                        ret = _open_m2ts(bd, st);
                    }
                }
            }

            bd.Value.mutex.bd_mutex_unlock();

            return ret;
        }

        /*
         * synchronous sub paths
         */

        static int _preload_textst_subpath(Ref<BLURAY> bd)
        {
            Variable<byte> char_code = new((byte)bd_char_code_e.BLURAY_TEXT_CHAR_CODE_UTF8);
            Variable<int> textst_subpath = new(-1);
            Variable<uint> textst_subclip = new(0);
            Variable<UInt16> textst_pid = new(0);
            uint ii;
            string font_file;

            if (!bd.Value.graphics_controller)
            {
                return 0;
            }

            if (bd.Value.decode_pg == 0 || bd.Value.title == null)
            {
                return 0;
            }

            _find_pg_stream(bd, textst_pid.Ref, textst_subpath.Ref, textst_subclip.Ref, char_code.Ref);
            if (textst_subpath.Value < 0)
            {
                return 0;
            }
            if (textst_pid.Value != 0x1800)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_preload_textst_subpath(): ignoring pid 0x{(uint)textst_pid.Value:x}");
                return 0;
            }

            if ((uint)textst_subpath.Value >= bd.Value.title.sub_path_count)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_preload_textst_subpath(): invalid subpath id");
                return -1;
            }
            if (textst_subclip.Value >= bd.Value.title.sub_path[textst_subpath.Value].clip_list.count)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_preload_textst_subpath(): invalid subclip id");
                return -1;
            }

            if (bd.Value.st_textst.Value.clip == bd.Value.title.sub_path[textst_subpath.Value].clip_list.clip.AtIndex(textst_subclip.Value))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, "_preload_textst_subpath(): subpath already loaded");
                return 1;
            }

            GraphicsController.gc_run(bd.Value.graphics_controller, gc_ctrl_e.GC_CTRL_PG_RESET, 0, Ref<GC_NAV_CMDS>.Null);

            bd.Value.st_textst.Value.clip = bd.Value.title.sub_path[textst_subpath.Value].clip_list.clip.AtIndex(textst_subclip.Value);
            if (!bd.Value.st_textst.Value.clip.Value.cl)
            {
                /* required for fonts */
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_preload_textst_subpath(): missing clip data");
                return -1;
            }

            if (!_preload_m2ts(bd, bd.Value.st_textst.Ref))
            {
                _close_preload(ref bd.Value.st_textst.Value);
                return 0;
            }

            GraphicsController.gc_decode_ts(bd.Value.graphics_controller, textst_pid.Value, bd.Value.st_textst.Value.buf, SPN(bd.Value.st_textst.Value.clip_size) / 32, -1);

            /* set fonts and encoding from clip info */
            GraphicsController.gc_add_font(bd.Value.graphics_controller, null, ulong.MaxValue); /* reset fonts */
            for (ii = 0; null != (font_file = Navigation.nav_clip_textst_font(bd.Value.st_textst.Value.clip, (int)ii)); ii++)
            {
                Ref<byte> data = Ref<byte>.Null;
                UInt64 size = bd.Value.disc.disc_read_file(Path.Combine("BDMV", "AUXDATA"), font_file, out data);
                if (data && size > 0 && GraphicsController.gc_add_font(bd.Value.graphics_controller, data, size) < 0)
                {
                    data.Free();
                }
                font_file = null;
            }

            GraphicsController.gc_run(bd.Value.graphics_controller, gc_ctrl_e.GC_CTRL_PG_CHARCODE, char_code.Value, Ref<GC_NAV_CMDS>.Null);

            /* start presentation timer */
            _init_textst_timer(bd);

            return 1;
        }

        /*
         * preloader for asynchronous sub paths
         */

        static int _find_ig_stream(Ref<BLURAY> bd, Ref<UInt16> pid, Ref<int> sub_path_idx, Ref<uint> sub_clip_idx)
        {
            uint main_clip_idx = bd.Value.st0.Value.clip ? bd.Value.st0.Value.clip.Value._ref : 0;
            uint ig_stream = Register.bd_psr_read(bd.Value.regs, bd_psr_idx.PSR_IG_STREAM_ID);
            Ref<MPLS_STN> stn = bd.Value.title.pl.Value.play_item[main_clip_idx].stn.Ref;

            if (ig_stream > 0 && ig_stream <= stn.Value.num_ig)
            {
                ig_stream--; /* stream number to table index */
                if (stn.Value.ig[ig_stream].stream_type == 2)
                {
                    sub_path_idx.Value = stn.Value.ig[ig_stream].subpath_id;
                    sub_clip_idx.Value = stn.Value.ig[ig_stream].subclip_id;
                }
                pid.Value = stn.Value.ig[ig_stream].pid;

                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"_find_ig_stream(): current IG stream pid 0x{pid.Value:x4} sub-path {sub_path_idx.Value}");
                return 1;
            }

            return 0;
        }

        static int _preload_ig_subpath(Ref<BLURAY> bd)
        {
            Variable<int> ig_subpath = new(-1);
            Variable<uint> ig_subclip = new(0);
            Variable<UInt16> ig_pid = new(0);

            if (!bd.Value.graphics_controller)
            {
                return 0;
            }

            _find_ig_stream(bd, ig_pid.Ref, ig_subpath.Ref, ig_subclip.Ref);

            if (ig_subpath.Value < 0)
            {
                return 0;
            }

            if (ig_subclip.Value >= bd.Value.title.sub_path[ig_subpath.Value].clip_list.count)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_preload_ig_subpath(): invalid subclip id");
                return -1;
            }

            if (bd.Value.st_ig.Value.clip == bd.Value.title.sub_path[ig_subpath.Value].clip_list.clip.AtIndex(ig_subclip.Value))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, "_preload_ig_subpath(): subpath already loaded");
                //return 1;
            }

            bd.Value.st_ig.Value.clip = bd.Value.title.sub_path[ig_subpath.Value].clip_list.clip.AtIndex(ig_subclip.Value);

            if (bd.Value.title.sub_path[ig_subpath.Value].clip_list.count > 1)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_preload_ig_subpath(): multi-clip sub paths not supported");
            }

            if (!_preload_m2ts(bd, bd.Value.st_ig.Ref))
            {
                _close_preload(ref bd.Value.st_ig.Value);
                return 0;
            }

            return 1;
        }

        static int _preload_subpaths(Ref<BLURAY> bd)
        {
            _close_preload(ref bd.Value.st_ig.Value);
            _close_preload(ref bd.Value.st_textst.Value);

            if (bd.Value.title.sub_path_count <= 0)
            {
                return 0;
            }

            return _preload_ig_subpath(bd) | _preload_textst_subpath(bd);
        }

        static int _init_ig_stream(Ref<BLURAY> bd)
        {
            Variable<int> ig_subpath = new(-1);
            Variable<uint> ig_subclip = new(0);
            Variable<UInt16> ig_pid = new(0);

            bd.Value.st0.Value.ig_pid = 0;

            if (bd.Value.title == null || !bd.Value.graphics_controller)
            {
                return 0;
            }

            _find_ig_stream(bd, ig_pid.Ref, ig_subpath.Ref, ig_subclip.Ref);

            /* decode already preloaded IG sub-path */
            if (bd.Value.st_ig.Value.clip)
            {
                GraphicsController.gc_decode_ts(bd.Value.graphics_controller, ig_pid.Value, bd.Value.st_ig.Value.buf, SPN(bd.Value.st_ig.Value.clip_size) / 32, -1);
                return 1;
            }

            /* store PID of main path embedded IG stream */
            if (ig_subpath.Value < 0)
            {
                bd.Value.st0.Value.ig_pid = ig_pid.Value;
                return 1;
            }

            return 0;
        }

        /*
         * select title / angle
         */

        static void _close_playlist(Ref<BLURAY> bd)
        {
            if (bd.Value.graphics_controller)
            {
                GraphicsController.gc_run(bd.Value.graphics_controller, gc_ctrl_e.GC_CTRL_RESET, 0, Ref<GC_NAV_CMDS>.Null);
            }

            /* stopping playback in middle of playlist ? */
            if (bd.Value.title != null && bd.Value.st0.Value.clip)
            {
                if (bd.Value.st0.Value.clip.Value._ref < bd.Value.title.clip_list.count - 1) {
                    /* not last clip of playlist */
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"close playlist (not last clip)");
                    _queue_event(bd, bd_event_e.BD_EVENT_PLAYLIST_STOP, 0);
                } else
                {
                    /* last clip of playlist */
                    int clip_pkt = (int)SPN(bd.Value.st0.Value.clip_pos);
                    int skip = (int)bd.Value.st0.Value.clip.Value.end_pkt - clip_pkt;
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"close playlist (last clip), packets skipped {skip}");
                    if (skip > 100)
                    {
                        _queue_event(bd, bd_event_e.BD_EVENT_PLAYLIST_STOP, 0);
                    }
                }
            }

            _close_m2ts(bd.Value.st0.Ref);
            _close_preload(ref bd.Value.st_ig.Value);
            _close_preload(ref bd.Value.st_textst.Value);

            Navigation.nav_title_close(ref bd.Value.title);

            bd.Value.st0.Value.clip = Ref<NAV_CLIP>.Null;

            /* reset UO mask */
            bd.Value.st0.Value.uo_mask = new();
            bd.Value.gc_uo_mask = new();
            _update_uo_mask(bd);
        }

        static int _add_known_playlist(BD_DISC? p, string mpls_id)
        {
            string old_mpls_ids;
            string new_mpls_ids = null;
            int result = -1;

            old_mpls_ids = p.disc_property_get(Disc.DISC_PROPERTY_PLAYLISTS);
            if (old_mpls_ids == null)
            {
                return p.disc_property_put(Disc.DISC_PROPERTY_PLAYLISTS, mpls_id);
            }

            /* no duplicates */
            if (old_mpls_ids.IndexOf(mpls_id) >= 0)
            {
                goto _out;
            }

            new_mpls_ids = $"{old_mpls_ids},{mpls_id}";
            result = p.disc_property_put(Disc.DISC_PROPERTY_PLAYLISTS, new_mpls_ids);

        _out:
            old_mpls_ids = null;
            new_mpls_ids = null;
            return result;
        }

        static bool _open_playlist(Ref<BLURAY> bd, uint playlist, uint angle)
        {
            string f_name = "";

            if (playlist > 99999)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Invalid playlist {playlist}!");
                return false;
            }
            f_name = $"{playlist:00000}.mpls";
            if (f_name.Length != 10)
            {
                return false;
            }

            if (!bd.Value.title_list && bd.Value.title_type == BD_TITLE_TYPE.title_undef)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"open_playlist(%s): bd_play() or bd_get_titles() not called", f_name);
                bd.Value.disc.disc_event(Disc.DiscEventType.DISC_EVENT_START, bd.Value.disc_info.Value.num_titles);
            }

            _close_playlist(bd);

            bd.Value.title = Navigation.nav_title_open(bd.Value.disc, f_name, angle);
            if (bd.Value.title == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Unable to open title {f_name}!");
                return false;
            }

            bd.Value.seamless_angle_change = 0;
            bd.Value.s_pos = 0;
            bd.Value.end_of_playlist = 0;
            bd.Value.st0.Value.ig_pid = 0;

            // Get the initial clip of the playlist
            bd.Value.st0.Value.clip = Navigation.nav_next_clip(bd.Value.title, Ref<NAV_CLIP>.Null);

            _update_playlist_psrs(bd);

            if (_open_m2ts(bd, bd.Value.st0.Ref))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"Title {f_name} selected");

                _find_next_playmark(bd);

                _preload_subpaths(bd);

                bd.Value.st0.Value.seek_flag = 1;

                /* remember played playlists when using menus */
                if (bd.Value.title_type != BD_TITLE_TYPE.title_undef)
                {
                    _add_known_playlist(bd.Value.disc, bd.Value.title.name);
                }

                /* inform application about current streams (redundant) */
                Register.bd_psr_lock(bd.Value.regs);
                _queue_event(bd, bd_event_e.BD_EVENT_AUDIO_STREAM, Register.bd_psr_read(bd.Value.regs, bd_psr_idx.PSR_PRIMARY_AUDIO_ID));
                {
                    UInt32 pgreg = Register.bd_psr_read(bd.Value.regs, bd_psr_idx.PSR_PG_STREAM);
                    _queue_event(bd, bd_event_e.BD_EVENT_PG_TEXTST, ((pgreg & 0x80000000) == 0) ? 0u : 1u);
                    _queue_event(bd, bd_event_e.BD_EVENT_PG_TEXTST_STREAM, pgreg & 0xfff);
                }
                Register.bd_psr_unlock(bd.Value.regs);

                return true;
            }
            return false;
        }

        public static bool bd_select_playlist(Ref<BLURAY> bd, UInt32 playlist)
        {
            bool result;

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title_list)
            {
                /* update current title */
                uint i;
                for (i = 0; i < bd.Value.title_list.Value.count; i++)
                {
                    if (playlist == bd.Value.title_list.Value.title_info[i].mpls_id)
                    {
                        bd.Value.title_idx = i;
                        break;
                    }
                }
            }

            result = _open_playlist(bd, playlist, 0);

            bd.Value.mutex.bd_mutex_unlock();

            return result;
        }

        /* BD-J callback */
        static int _play_playlist_at(Ref<BLURAY> bd, int playlist, int playitem, int playmark, Int64 time)
        {
            if (playlist < 0)
            {
                _close_playlist(bd);
                return 1;
            }

            if (!_open_playlist(bd, (uint)playlist, 0))
            {
                return 0;
            }

            bd.Value.bdj_wait_start = 1;  /* playback is triggered by bd_select_rate() */

            bd_bdj_seek(bd, playitem, playmark, time);

            return 1;
        }

        /* BD-J callback */
        public static int bd_play_playlist_at(Ref<BLURAY> bd, int playlist, int playitem, int playmark, Int64 time)
        {
            int result;

            /* select + seek should be atomic (= player can't read data between select and seek to start position) */
            bd.Value.mutex.bd_mutex_lock();
            result = _play_playlist_at(bd, playlist, playitem, playmark, time);
            bd.Value.mutex.bd_mutex_unlock();

            return result;
        }

        // Select a title for playback
        // The title index is an index into the list
        // established by bd_get_titles()
        static bool _select_title(Ref<BLURAY> bd, UInt32 title_idx)
        {
            // Open the playlist
            if (bd.Value.title_list == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_CRIT | DebugMaskEnum.DBG_BLURAY, $"Title list not yet read!");
                return false;
            }
            if (bd.Value.title_list.Value.count <= title_idx)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Invalid title index {title_idx}!");
                return false;
            }

            bd.Value.title_idx = title_idx;

            return _open_playlist(bd, bd.Value.title_list.Value.title_info[title_idx].mpls_id, 0);
        }

        public static bool bd_select_title(Ref<BLURAY> bd, UInt32 title_idx)
        {
            bool result;

            bd.Value.mutex.bd_mutex_lock();
            result = _select_title(bd, title_idx);
            bd.Value.mutex.bd_mutex_unlock();

            return result;
        }

        public static UInt32 bd_get_current_title(Ref<BLURAY> bd)
        {
            return bd.Value.title_idx;
        }

        static bool _bd_select_angle(Ref<BLURAY> bd, uint angle)
        {
            uint orig_angle;

            if (bd.Value.title == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Can't select angle: title not yet selected!");
                return false;
            }

            orig_angle = bd.Value.title.angle;

            Navigation.nav_set_angle(bd.Value.title, angle);

            if (orig_angle == bd.Value.title.angle)
            {
                return true;
            }

            Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_ANGLE_NUMBER, (uint)(bd.Value.title.angle + 1));

            if (!_open_m2ts(bd, bd.Value.st0.Ref))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Error selecting angle {angle} !");
                return false;
            }

            return true;
        }

        public static bool bd_select_angle(Ref<BLURAY> bd, uint angle)
        {
            bool result;
            bd.Value.mutex.bd_mutex_lock();
            result = _bd_select_angle(bd, angle);
            bd.Value.mutex.bd_mutex_unlock();
            return result;
        }

        public static uint bd_get_current_angle(Ref<BLURAY> bd)
        {
            int angle = 0;

            bd.Value.mutex.bd_mutex_lock();
            if (bd.Value.title != null)
            {
                angle = bd.Value.title.angle;
            }
            bd.Value.mutex.bd_mutex_unlock();

            return (uint)angle;
        }


        public static void bd_seamless_angle_change(Ref<BLURAY> bd, uint angle)
        {
            UInt32 clip_pkt;

            bd.Value.mutex.bd_mutex_lock();

            clip_pkt = SPN(bd.Value.st0.Value.clip_pos + 191);
            bd.Value.angle_change_pkt = Navigation.nav_clip_angle_change_search(bd.Value.st0.Value.clip, clip_pkt,
                                                                bd.Value.angle_change_time.Ref);
            bd.Value.request_angle = angle;
            bd.Value.seamless_angle_change = 1;

            bd.Value.mutex.bd_mutex_unlock();
        }

        /*
         * title lists
         */

        public static UInt32 bd_get_titles(Ref<BLURAY> bd, byte flags, UInt32 min_title_length)
        {
            Ref<NAV_TITLE_LIST> title_list;
            UInt32 count;

            if (!bd)
            {
                return 0;
            }

            title_list = Navigation.nav_get_title_list(bd.Value.disc, flags, min_title_length);
            if (!title_list)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"nav_get_title_list({bd.Value.disc.disc_root()}) failed");
                return 0;
            }

            bd.Value.mutex.bd_mutex_lock();

            Navigation.nav_free_title_list(ref bd.Value.title_list);
            bd.Value.title_list = title_list;

            bd.Value.disc.disc_event(Disc.DiscEventType.DISC_EVENT_START, bd.Value.disc_info.Value.num_titles);
            count = bd.Value.title_list.Value.count;

            bd.Value.mutex.bd_mutex_unlock();

            return count;
        }

        public static int bd_get_main_title(Ref<BLURAY> bd)
        {
            int main_title_idx = -1;

            if (!bd)
            {
                return -1;
            }

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title_type != BD_TITLE_TYPE.title_undef)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_CRIT | DebugMaskEnum.DBG_BLURAY, $"bd_get_main_title() can't be used with BluRay menus");
            }

            if (bd.Value.title_list == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Title list not yet read!");
            }
            else
            {
                main_title_idx = (int)bd.Value.title_list.Value.main_title_idx;
            }

            bd.Value.mutex.bd_mutex_unlock();

            return main_title_idx;
        }

        static bool _copy_streams(Ref<NAV_CLIP> clip, ref Ref<BLURAY_STREAM_INFO> pstreams,
                                 Ref<MPLS_STREAM> si, int count)
        {
            Ref<BLURAY_STREAM_INFO> streams;
            int ii;

            if (count == 0)
            {
                return true;
            }
            streams = pstreams = Ref<BLURAY_STREAM_INFO>.Allocate(count);
            if (!streams)
            {
                return false;
            }

            for (ii = 0; ii < count; ii++)
            {
                streams[ii].coding_type = si[ii].coding_type;
                streams[ii].format = si[ii].format;
                streams[ii].rate = si[ii].rate;
                streams[ii].char_code = si[ii].char_code;
                streams[ii].lang = si[ii].lang;
                streams[ii].pid = si[ii].pid;
                streams[ii].aspect = Navigation.nav_clip_lookup_aspect(clip, si[ii].pid);
                if ((si.Value.stream_type == 2) || (si.Value.stream_type == 3))
                    streams[ii].subpath_id = si.Value.subpath_id;
                else
                    streams[ii].subpath_id = byte.MaxValue;
            }

            return true;
        }

        static Ref<BLURAY_TITLE_INFO> _fill_title_info(NAV_TITLE? title, UInt32 title_idx, UInt32 playlist)
        {
            Ref<BLURAY_TITLE_INFO> title_info;
            uint ii;

            title_info = Ref<BLURAY_TITLE_INFO>.Allocate();
            if (!title_info)
            {
                goto error;
            }
            title_info.Value.idx = title_idx;
            title_info.Value.playlist = playlist;
            title_info.Value.duration = (UInt64)title.duration * 2;
            title_info.Value.angle_count = title.angle_count;
            title_info.Value.chapter_count = title.chap_list.count;
            if (title_info.Value.chapter_count != 0)
            {
                title_info.Value.chapters = Ref<BLURAY_TITLE_CHAPTER>.Allocate(title_info.Value.chapter_count);
                if (!title_info.Value.chapters)
                {
                    goto error;
                }
                for (ii = 0; ii < title_info.Value.chapter_count; ii++)
                {
                    title_info.Value.chapters[ii].idx = ii;
                    title_info.Value.chapters[ii].start = (UInt64)title.chap_list.mark[ii].title_time * 2;
                    title_info.Value.chapters[ii].duration = (UInt64)title.chap_list.mark[ii].duration * 2;
                    title_info.Value.chapters[ii].offset = (UInt64)title.chap_list.mark[ii].title_pkt * 192L;
                    title_info.Value.chapters[ii].clip_ref = title.chap_list.mark[ii].clip_ref;
                }
            }
            title_info.Value.mark_count = title.mark_list.count;
            if (title_info.Value.mark_count != 0)
            {
                title_info.Value.marks = Ref<BLURAY_TITLE_MARK>.Allocate(title_info.Value.mark_count);
                if (!title_info.Value.marks)
                {
                    goto error;
                }
                for (ii = 0; ii < title_info.Value.mark_count; ii++)
                {
                    title_info.Value.marks[ii].idx = ii;
                    title_info.Value.marks[ii].type = title.mark_list.mark[ii].mark_type;
                    title_info.Value.marks[ii].start = (UInt64)title.mark_list.mark[ii].title_time * 2;
                    title_info.Value.marks[ii].duration = (UInt64)title.mark_list.mark[ii].duration * 2;
                    title_info.Value.marks[ii].offset = (UInt64)title.mark_list.mark[ii].title_pkt * 192L;
                    title_info.Value.marks[ii].clip_ref = title.mark_list.mark[ii].clip_ref;
                }
            }
            title_info.Value.clip_count = title.clip_list.count;
            if (title_info.Value.clip_count != 0)
            {
                title_info.Value.clips = Ref<BLURAY_CLIP_INFO>.Allocate(title_info.Value.clip_count);
                if (!title_info.Value.clips)
                {
                    goto error;
                }
                for (ii = 0; ii < title_info.Value.clip_count; ii++)
                {
                    Ref<BLURAY_CLIP_INFO> ci = title_info.Value.clips.AtIndex(ii);
                    Ref<MPLS_PI> pi = title.pl.Value.play_item.AtIndex(ii);
                    Ref<NAV_CLIP> nc = title.clip_list.clip.AtIndex(ii);

                    ci.Value.clip_id = pi.Value.clip.Value.clip_id;
                    ci.Value.pkt_count = nc.Value.end_pkt - nc.Value.start_pkt;
                    ci.Value.start_time = (UInt64)nc.Value.title_time * 2;
                    ci.Value.in_time = (UInt64)pi.Value.in_time * 2;
                    ci.Value.out_time = (UInt64)pi.Value.out_time * 2;
                    ci.Value.still_mode = pi.Value.still_mode;
                    ci.Value.still_time = pi.Value.still_time;
                    ci.Value.video_stream_count = pi.Value.stn.Value.num_video;
                    ci.Value.audio_stream_count = pi.Value.stn.Value.num_audio;
                    ci.Value.pg_stream_count = (byte)(pi.Value.stn.Value.num_pg + pi.Value.stn.Value.num_pip_pg);
                    ci.Value.ig_stream_count = pi.Value.stn.Value.num_ig;
                    ci.Value.sec_video_stream_count = pi.Value.stn.Value.num_secondary_video;
                    ci.Value.sec_audio_stream_count = pi.Value.stn.Value.num_secondary_audio;
                    if (!_copy_streams(nc, ref ci.Value.video_streams, pi.Value.stn.Value.video, ci.Value.video_stream_count) ||
                        !_copy_streams(nc, ref ci.Value.audio_streams, pi.Value.stn.Value.audio, ci.Value.audio_stream_count) ||
                        !_copy_streams(nc, ref ci.Value.pg_streams, pi.Value.stn.Value.pg, ci.Value.pg_stream_count) ||
                        !_copy_streams(nc, ref ci.Value.ig_streams, pi.Value.stn.Value.ig, ci.Value.ig_stream_count) ||
                        !_copy_streams(nc, ref ci.Value.sec_video_streams, pi.Value.stn.Value.secondary_video, ci.Value.sec_video_stream_count) ||
                        !_copy_streams(nc, ref ci.Value.sec_audio_streams, pi.Value.stn.Value.secondary_audio, ci.Value.sec_audio_stream_count))
                    {

                        goto error;
                    }
                }
            }

            title_info.Value.mvc_base_view_r_flag = title.pl.Value.app_info.Value.mvc_base_view_r_flag;

            return title_info;

        error:
            Logging.bd_debug(DebugMaskEnum.DBG_CRIT, $"Out of memory");
            bd_free_title_info(title_info);
            return Ref<BLURAY_TITLE_INFO>.Null;
        }

        static Ref<BLURAY_TITLE_INFO> _get_mpls_info(Ref<BLURAY> bd, UInt32 title_idx, UInt32 playlist, uint angle)
        {
            NAV_TITLE? title;
            Ref<BLURAY_TITLE_INFO> title_info;
            string mpls_name;

            if (playlist > 99999)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Invalid playlist {playlist}!");
                return Ref<BLURAY_TITLE_INFO>.Null;
            }

            mpls_name = $"{playlist:00000}.mpls";
            if (mpls_name.Length != 10)
            {
                return Ref<BLURAY_TITLE_INFO>.Null;
            }

            /* current title ? => no need to load mpls file */
            bd.Value.mutex.bd_mutex_lock();
            if (bd.Value.title != null && bd.Value.title.angle == angle && bd.Value.title.name == mpls_name)
            {
                title_info = _fill_title_info(bd.Value.title, title_idx, playlist);
                bd.Value.mutex.bd_mutex_unlock();
                return title_info;
            }
            bd.Value.mutex.bd_mutex_unlock();

            title = Navigation.nav_title_open(bd.Value.disc, mpls_name, angle);
            if (title == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Unable to open title {mpls_name}!");
                return Ref<BLURAY_TITLE_INFO>.Null;
            }

            title_info = _fill_title_info(title, title_idx, playlist);

            Navigation.nav_title_close(ref title);
            return title_info;
        }

        public static Ref<BLURAY_TITLE_INFO> bd_get_title_info(Ref<BLURAY> bd, UInt32 title_idx, uint angle)
        {
            int mpls_id = -1;

            bd.Value.mutex.bd_mutex_lock();

            if (bd.Value.title_list == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Title list not yet read!");
            }
            else if (bd.Value.title_list.Value.count <= title_idx)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Invalid title index {title_idx}!");
            }
            else
            {
                mpls_id = (int)(bd.Value.title_list.Value.title_info[title_idx].mpls_id);
            }

            bd.Value.mutex.bd_mutex_unlock();

            if (mpls_id < 0)
                return Ref<BLURAY_TITLE_INFO>.Null;

            return _get_mpls_info(bd, title_idx, (uint)mpls_id, angle);
        }

        public static Ref<BLURAY_TITLE_INFO> bd_get_playlist_info(Ref<BLURAY> bd, UInt32 playlist, uint angle)
        {
            return _get_mpls_info(bd, 0, playlist, angle);
        }

        public static void bd_free_title_info(Ref<BLURAY_TITLE_INFO> title_info)
        {
            uint ii;

            if (title_info)
            {
                title_info.Value.chapters.Free();
                title_info.Value.marks.Free();
                if (title_info.Value.clips)
                {
                    for (ii = 0; ii < title_info.Value.clip_count; ii++)
                    {
                        title_info.Value.clips[ii].video_streams.Free();
                        title_info.Value.clips[ii].audio_streams.Free();
                        title_info.Value.clips[ii].pg_streams.Free();
                        title_info.Value.clips[ii].ig_streams.Free();
                        title_info.Value.clips[ii].sec_video_streams.Free();
                        title_info.Value.clips[ii].sec_audio_streams.Free();
                    }
                    title_info.Value.clips.Free();
                }
                title_info.Free();
            }
        }

        /*
         * player settings
         */

        private struct MapStruct // temp name for anonymous struct
        {
            public bd_player_setting idx;
            public bd_psr_idx psr;
            public MapStruct(bd_player_setting idx, bd_psr_idx psr)
            {
                this.idx = idx;
                this.psr = psr;
            }
        }

        private static MapStruct[] map =
        {
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_PARENTAL, bd_psr_idx.PSR_PARENTAL),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_AUDIO_CAP, bd_psr_idx.PSR_AUDIO_CAP),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_AUDIO_LANG, bd_psr_idx.PSR_AUDIO_LANG),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_PG_LANG, bd_psr_idx.PSR_PG_AND_SUB_LANG),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_MENU_LANG, bd_psr_idx.PSR_MENU_LANG),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_COUNTRY_CODE, bd_psr_idx.PSR_COUNTRY),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_REGION_CODE, bd_psr_idx.PSR_REGION),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_OUTPUT_PREFER, bd_psr_idx.PSR_OUTPUT_PREFER),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_DISPLAY_CAP, bd_psr_idx.PSR_DISPLAY_CAP),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_3D_CAP, bd_psr_idx.PSR_3D_CAP),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_UHD_CAP, bd_psr_idx.PSR_UHD_CAP),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_UHD_DISPLAY_CAP, bd_psr_idx.PSR_UHD_DISPLAY_CAP),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_HDR_PREFERENCE, bd_psr_idx.PSR_UHD_HDR_PREFER),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_SDR_CONV_PREFER, bd_psr_idx.PSR_UHD_SDR_CONV_PREFER),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_VIDEO_CAP, bd_psr_idx.PSR_VIDEO_CAP),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_TEXT_CAP, bd_psr_idx.PSR_TEXT_CAP),
            new MapStruct(bd_player_setting.BLURAY_PLAYER_SETTING_PLAYER_PROFILE, bd_psr_idx.PSR_PROFILE_VERSION),
        };

        public static int bd_set_player_setting(Ref<BLURAY> bd, bd_player_setting idx, UInt32 value)
        {

            uint i;
            int result;

            if (idx == bd_player_setting.BLURAY_PLAYER_SETTING_DECODE_PG)
            {
                bd.Value.mutex.bd_mutex_lock();

                bd.Value.decode_pg = (byte)((value == 0) ? 0u : 1u);
                result = (Register.bd_psr_write_bits(bd.Value.regs, bd_psr_idx.PSR_PG_STREAM,
                                            ((value == 0) ? 0u : 1u) << 31,
                                            0x80000000u) == 0) ? 1 : 0;

                bd.Value.mutex.bd_mutex_unlock();
                return result;
            }

            if (idx == bd_player_setting.BLURAY_PLAYER_SETTING_PERSISTENT_STORAGE)
            {
                if (bd.Value.title_type != BD_TITLE_TYPE.title_undef)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Can't disable persistent storage during playback");
                    return 0;
                }
                bd.Value.bdj_config.no_persistent_storage = (byte)((value == 0) ? 1 : 0);
                return 1;
            }

            for (i = 0; i < map.Length; i++)
            {
                if (idx == map[i].idx)
                {
                    bd.Value.mutex.bd_mutex_lock();
                    result = (Register.bd_psr_setting_write(bd.Value.regs, map[i].psr, value) == 0) ? 1 : 0;
                    bd.Value.mutex.bd_mutex_unlock();
                    return result;
                }
            }

            return 0;
        }

        public static bool bd_set_player_setting_str(Ref<BLURAY> bd, bd_player_setting idx, string s)
        {
            switch (idx)
            {
                case bd_player_setting.BLURAY_PLAYER_SETTING_AUDIO_LANG:
                case bd_player_setting.BLURAY_PLAYER_SETTING_PG_LANG:
                case bd_player_setting.BLURAY_PLAYER_SETTING_MENU_LANG:
                    return bd_set_player_setting(bd, idx, Util.str_to_uint32(s, 3)) != 0;

                case bd_player_setting.BLURAY_PLAYER_SETTING_COUNTRY_CODE:
                    return bd_set_player_setting(bd, idx, Util.str_to_uint32(s, 2)) != 0;

                case bd_player_setting.BLURAY_PLAYER_CACHE_ROOT:
                    bd.Value.mutex.bd_mutex_lock();
                    bd.Value.bdj_config.cache_root = null;
                    bd.Value.bdj_config.cache_root = s;
                    bd.Value.mutex.bd_mutex_unlock();
                    Logging.bd_debug(DebugMaskEnum.DBG_BDJ, $"Cache root dir set to {bd.Value.bdj_config.cache_root}");
                    return true;

                case bd_player_setting.BLURAY_PLAYER_PERSISTENT_ROOT:
                    bd.Value.mutex.bd_mutex_lock();
                    bd.Value.bdj_config.persistent_root = null;
                    bd.Value.bdj_config.persistent_root = s;
                    bd.Value.mutex.bd_mutex_unlock();
                    Logging.bd_debug(DebugMaskEnum.DBG_BDJ, $"Persistent root dir set to {bd.Value.bdj_config.persistent_root}");
                    return true;

                case bd_player_setting.BLURAY_PLAYER_JAVA_HOME:
                    bd.Value.mutex.bd_mutex_lock();
                    bd.Value.bdj_config.java_home = null;
                    bd.Value.bdj_config.java_home = (s != null) ? s : null;
                    bd.Value.mutex.bd_mutex_unlock();
                    Logging.bd_debug(DebugMaskEnum.DBG_BDJ, $"Java home set to {(bd.Value.bdj_config.java_home ?? "<auto>")}");
                    return true;

                default:
                    return false;
            }
        }

        public const uint BLURAY_AUDIO_STREAM = 0; /**< Select audio stream     */
        public const uint BLURAY_PG_TEXTST_STREAM = 1; /**< Select subtitle stream  */

        public static void bd_select_stream(Ref<BLURAY> bd, UInt32 stream_type, UInt32 stream_id, UInt32 enable_flag)
        {
            bd.Value.mutex.bd_mutex_lock();

            switch (stream_type)
            {
                case BLURAY_AUDIO_STREAM:
                    Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_PRIMARY_AUDIO_ID, stream_id & 0xff);
                    break;
                case BLURAY_PG_TEXTST_STREAM:
                    Register.bd_psr_write_bits(bd.Value.regs, bd_psr_idx.PSR_PG_STREAM,
                                      (((enable_flag == 0) ? 0u : 1u) << 31) | (stream_id & 0xfff),
                                      0x80000fff);
                    break;
                    /*
                    case BLURAY_SECONDARY_VIDEO_STREAM:
                    case BLURAY_SECONDARY_AUDIO_STREAM:
                    */
            }

            bd.Value.mutex.bd_mutex_unlock();
        }

        /*
         * BD-J testing
         */

        public static bool bd_start_bdj(Ref<BLURAY> bd, string start_object)
        {
            Ref<BLURAY_TITLE> t;
            uint title_num = uint.Parse(start_object);
            uint ii;

            if (!bd)
            {
                return false;
            }

            /* first play object ? */
            if (bd.Value.disc_info.Value.first_play_supported != 0)
            {
                t = bd.Value.disc_info.Value.first_play;
                if (t && t.Value.bdj != 0 && t.Value.id_ref == title_num)
                {
                    return _start_bdj(bd, BLURAY_TITLE_FIRST_PLAY);
                }
            }

            /* valid BD-J title from disc index ? */
            if (bd.Value.disc_info.Value.titles)
            {
                for (ii = 0; ii <= bd.Value.disc_info.Value.num_titles; ii++)
                {
                    t = bd.Value.disc_info.Value.titles.AtIndex(ii);
                    if (t && t.Value.bdj != 0 && t.Value.id_ref == title_num)
                    {
                        return _start_bdj(bd, ii);
                    }
                }
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"No {start_object}.bdjo in disc index");
            }
            else
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"No disc index");
            }

            return false;
        }

        public static void bd_stop_bdj(Ref<BLURAY> bd)
        {
            bd.Value.mutex.bd_mutex_lock();
            _close_bdj(bd);
            bd.Value.mutex.bd_mutex_unlock();
        }

        /*
         * Navigation mode interface
         */

        static void _set_scr(Ref<BLURAY> bd, Int64 pts)
        {
            if (pts >= 0)
            {
                UInt32 tick = (UInt32)(((UInt64)pts) >> 1);
                _update_time_psr(bd, tick);

            }
            else if (bd.Value.app_scr == 0)
            {
                _update_time_psr_from_stream(bd);
            }
        }

        static void _process_psr_restore_event(Ref<BLURAY> bd, Ref<BD_PSR_EVENT> ev)
        {
            /* PSR restore events are handled internally.
             * Restore stored playback position.
             */

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"PSR restore: psr{ev.Value.psr_idx} = {ev.Value.new_val}");

            switch (ev.Value.psr_idx)
            {
                case bd_psr_idx.PSR_ANGLE_NUMBER:
                    /* can't set angle before playlist is opened */
                    return;
                case bd_psr_idx.PSR_TITLE_NUMBER:
                    /* pass to the application */
                    _queue_event(bd, bd_event_e.BD_EVENT_TITLE, ev.Value.new_val);
                    return;
                case bd_psr_idx.PSR_CHAPTER:
                    /* will be selected automatically */
                    return;
                case bd_psr_idx.PSR_PLAYLIST:
                    bd_select_playlist(bd, ev.Value.new_val);
                    Navigation.nav_set_angle(bd.Value.title, Register.bd_psr_read(bd.Value.regs, bd_psr_idx.PSR_ANGLE_NUMBER) - 1);
                    return;
                case bd_psr_idx.PSR_PLAYITEM:
                    bd_seek_playitem(bd, ev.Value.new_val);
                    return;
                case bd_psr_idx.PSR_TIME:
                    _clip_seek_time(bd, ev.Value.new_val);
                    _init_ig_stream(bd);
                    _run_gc(bd, gc_ctrl_e.GC_CTRL_INIT_MENU, 0);
                    return;

                case bd_psr_idx.PSR_SELECTED_BUTTON_ID:
                case bd_psr_idx.PSR_MENU_PAGE_ID:
                    /* handled by graphics controller */
                    return;

                default:
                    /* others: ignore */
                    return;
            }
        }

        /*
         * notification events to APP
         */

        static void _process_psr_write_event(Ref<BLURAY> bd, Ref<BD_PSR_EVENT> ev)
        {
            if (ev.Value.ev_type == Register.BD_PSR_WRITE)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"PSR write: psr{ev.Value.psr_idx} = {ev.Value.new_val}");
            }

            switch (ev.Value.psr_idx)
            {

                /* current playback position */

                case bd_psr_idx.PSR_ANGLE_NUMBER:
                    _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_ANGLE, ev.Value.new_val);
                    _queue_event(bd, bd_event_e.BD_EVENT_ANGLE, ev.Value.new_val);
                    break;
                case bd_psr_idx.PSR_TITLE_NUMBER:
                    _queue_event(bd, bd_event_e.BD_EVENT_TITLE, ev.Value.new_val);
                    break;
                case bd_psr_idx.PSR_PLAYLIST:
                    _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_PLAYLIST, ev.Value.new_val);
                    _queue_event(bd, bd_event_e.BD_EVENT_PLAYLIST, ev.Value.new_val);
                    break;
                case bd_psr_idx.PSR_PLAYITEM:
                    _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_PLAYITEM, ev.Value.new_val);
                    _queue_event(bd, bd_event_e.BD_EVENT_PLAYITEM, ev.Value.new_val);
                    break;
                case bd_psr_idx.PSR_TIME:
                    _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_PTS, ev.Value.new_val);
                    break;

                case (bd_psr_idx)102:
                    _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_PSR102, ev.Value.new_val);
                    break;
                case (bd_psr_idx)103:
                    Disc.disc_event(bd.Value.disc, Disc.DiscEventType.DISC_EVENT_APPLICATION, ev.Value.new_val);
                    break;

                default:
                    break;
            }
        }

        static void _process_psr_change_event(Ref<BLURAY> bd, Ref<BD_PSR_EVENT> ev)
        {
            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"PSR change: psr{ev.Value.psr_idx} = {ev.Value.new_val}");

            _process_psr_write_event(bd, ev);

            switch (ev.Value.psr_idx)
            {

                /* current playback position */

                case bd_psr_idx.PSR_TITLE_NUMBER:
                    Disc.disc_event(bd.Value.disc, Disc.DiscEventType.DISC_EVENT_TITLE, ev.Value.new_val);
                    break;

                case bd_psr_idx.PSR_CHAPTER:
                    _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_CHAPTER, ev.Value.new_val);
                    if (ev.Value.new_val != 0xffff)
                    {
                        _queue_event(bd, bd_event_e.BD_EVENT_CHAPTER, ev.Value.new_val);
                    }
                    break;

                /* stream selection */

                case bd_psr_idx.PSR_IG_STREAM_ID:
                    _queue_event(bd, bd_event_e.BD_EVENT_IG_STREAM, ev.Value.new_val);
                    break;

                case bd_psr_idx.PSR_PRIMARY_AUDIO_ID:
                    _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_AUDIO_STREAM, ev.Value.new_val);
                    _queue_event(bd, bd_event_e.BD_EVENT_AUDIO_STREAM, ev.Value.new_val);
                    break;

                case bd_psr_idx.PSR_PG_STREAM:
                    _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_SUBTITLE, ev.Value.new_val);
                    if ((ev.Value.new_val & 0x80000fff) != (ev.Value.old_val & 0x80000fff))
                    {
                        _queue_event(bd, bd_event_e.BD_EVENT_PG_TEXTST, ((ev.Value.new_val & 0x80000000) == 0) ? 0u : 1u);
                        _queue_event(bd, bd_event_e.BD_EVENT_PG_TEXTST_STREAM, ev.Value.new_val & 0xfff);
                    }

                    bd.Value.mutex.bd_mutex_lock();
                    if (bd.Value.st0.Value.clip)
                    {
                        _init_pg_stream(bd);
                        if (bd.Value.st_textst.Value.clip)
                        {
                            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Changing TextST stream");
                            _preload_textst_subpath(bd);
                        }
                    }
                    bd.Value.mutex.bd_mutex_unlock();

                    break;

                case bd_psr_idx.PSR_SECONDARY_AUDIO_VIDEO:
                    /* secondary video */
                    if ((ev.Value.new_val & 0x8f00ff00) != (ev.Value.old_val & 0x8f00ff00))
                    {
                        _queue_event(bd, bd_event_e.BD_EVENT_SECONDARY_VIDEO, ((ev.Value.new_val & 0x80000000) == 0) ? 0u : 1u);
                        _queue_event(bd, bd_event_e.BD_EVENT_SECONDARY_VIDEO_SIZE, (ev.Value.new_val >> 24) & 0xf);
                        _queue_event(bd, bd_event_e.BD_EVENT_SECONDARY_VIDEO_STREAM, (ev.Value.new_val & 0xff00) >> 8);
                    }
                    /* secondary audio */
                    if ((ev.Value.new_val & 0x400000ff) != (ev.Value.old_val & 0x400000ff))
                    {
                        _queue_event(bd, bd_event_e.BD_EVENT_SECONDARY_AUDIO, ((ev.Value.new_val & 0x40000000) == 0) ? 0u : 1u);
                        _queue_event(bd, bd_event_e.BD_EVENT_SECONDARY_AUDIO_STREAM, ev.Value.new_val & 0xff);
                    }
                    _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_SECONDARY_STREAM, ev.Value.new_val);
                    break;

                /* 3D status */
                case bd_psr_idx.PSR_3D_STATUS:
                    _queue_event(bd, bd_event_e.BD_EVENT_STEREOSCOPIC_STATUS, ev.Value.new_val & 1);
                    break;

                default:
                    break;
            }
        }

        static void _process_psr_event(object handle, Ref<BD_PSR_EVENT> ev)
        {
            Ref<BLURAY> bd = (Ref<BLURAY>)handle;

            switch (ev.Value.ev_type)
            {
                case Register.BD_PSR_WRITE:
                    _process_psr_write_event(bd, ev);
                    break;
                case Register.BD_PSR_CHANGE:
                    _process_psr_change_event(bd, ev);
                    break;
                case Register.BD_PSR_RESTORE:
                    _process_psr_restore_event(bd, ev);
                    break;

                case Register.BD_PSR_SAVE:
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"PSR save _event");
                    break;
                default:
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"PSR _event {ev.Value.ev_type}: psr{ev.Value.psr_idx} = {ev.Value.new_val}");
                    break;
            }
        }

        static bd_psr_idx[] psrs = {
        bd_psr_idx.PSR_ANGLE_NUMBER,
        bd_psr_idx.PSR_TITLE_NUMBER,
        bd_psr_idx.PSR_IG_STREAM_ID,
        bd_psr_idx.PSR_PRIMARY_AUDIO_ID,
        bd_psr_idx.PSR_PG_STREAM,
        bd_psr_idx.PSR_SECONDARY_AUDIO_VIDEO,
    };

        static void _queue_initial_psr_events(Ref<BLURAY> bd)
        {

            uint ii;
            Variable<BD_PSR_EVENT> ev = new();

            ev.Value.ev_type = Register.BD_PSR_CHANGE;
            ev.Value.old_val = 0;

            for (ii = 0; ii < psrs.Length; ii++)
            {
                ev.Value.psr_idx = psrs[ii];
                ev.Value.new_val = Register.bd_psr_read(bd.Value.regs, psrs[ii]);

                _process_psr_change_event(bd, ev.Ref);
            }
        }

        static bool _play_bdj(Ref<BLURAY> bd, uint title)
        {
            bool result;

            bd.Value.title_type = BD_TITLE_TYPE.title_bdj;

            result = _start_bdj(bd, title);
            if (result == false)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Can't play BD-J title {title}");
                bd.Value.title_type = BD_TITLE_TYPE.title_undef;
                _queue_event(bd, bd_event_e.BD_EVENT_ERROR, BD_ERROR_BDJ);
            }

            return result;
        }

        static bool _play_hdmv(Ref<BLURAY> bd, uint id_ref)
        {
            bool result = true;

            _stop_bdj(bd);

            bd.Value.title_type = BD_TITLE_TYPE.title_hdmv;

            if (!bd.Value.hdmv_vm)
            {
                bd.Value.hdmv_vm = HdmvVm.hdmv_vm_init(bd.Value.disc, bd.Value.regs, bd.Value.disc_info.Value.num_titles,
                                           bd.Value.disc_info.Value.first_play_supported, bd.Value.disc_info.Value.top_menu_supported);
            }

            if (HdmvVm.hdmv_vm_select_object(bd.Value.hdmv_vm, id_ref) != 0)
            {
                result = false;
            }

            bd.Value.hdmv_suspended = (byte)((HdmvVm.hdmv_vm_running(bd.Value.hdmv_vm) == 0) ? 1u : 0u);

            if (result == false)
            {
                bd.Value.title_type = BD_TITLE_TYPE.title_undef;
                _queue_event(bd, bd_event_e.BD_EVENT_ERROR, BD_ERROR_HDMV);
            }

            return result;
        }

        static bool _play_title(Ref<BLURAY> bd, uint title)
        {
            if (!bd.Value.disc_info.Value.titles)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_play_title(#{title}): No disc index");
                return false;
            }

            if (bd.Value.disc_info.Value.no_menu_support != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_play_title(): no menu support");
                return false;
            }

            /* first play object ? */
            if (title == BLURAY_TITLE_FIRST_PLAY)
            {

                Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_TITLE_NUMBER, BLURAY_TITLE_FIRST_PLAY); /* 5.2.3.3 */

                if (bd.Value.disc_info.Value.first_play_supported == 0)
                {
                    /* no first play title (5.2.3.3) */
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_play_title(): No first play title");
                    bd.Value.title_type = BD_TITLE_TYPE.title_hdmv;
                    return true;
                }

                if (bd.Value.disc_info.Value.first_play.Value.bdj != 0)
                {
                    return _play_bdj(bd, title);
                }
                else
                {
                    return _play_hdmv(bd, bd.Value.disc_info.Value.first_play.Value.id_ref);
                }
            }

            /* bd_play not called ? */
            if (bd.Value.title_type == BD_TITLE_TYPE.title_undef)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_call_title(): bd_play() not called !");
                return false;
            }

            /* top menu ? */
            if (title == BLURAY_TITLE_TOP_MENU)
            {
                if (bd.Value.disc_info.Value.top_menu_supported == 0)
                {
                    /* no top menu (5.2.3.3) */
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_play_title(): No top menu title");
                    bd.Value.title_type = BD_TITLE_TYPE.title_hdmv;
                    return false;
                }
            }

            /* valid title from disc index ? */
            if (title <= bd.Value.disc_info.Value.num_titles)
            {

                Register.bd_psr_write(bd.Value.regs, bd_psr_idx.PSR_TITLE_NUMBER, title); /* 5.2.3.3 */
                if (bd.Value.disc_info.Value.titles[title].bdj != 0)
                {
                    return _play_bdj(bd, title);
                }
                else
                {
                    return _play_hdmv(bd, bd.Value.disc_info.Value.titles[title].id_ref);
                }
            }
            else
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"_play_title(#{title}): Title not found");
            }

            return false;
        }

        /* BD-J callback */
        public static bool bd_play_title_internal(Ref<BLURAY> bd, uint title)
        {
            /* used by BD-J. Like bd_play_title() but bypasses UO mask checks. */
            bool ret;
            bd.Value.mutex.bd_mutex_lock();
            ret = _play_title(bd, title);
            bd.Value.mutex.bd_mutex_unlock();
            return ret;
        }

        public static bool bd_play(Ref<BLURAY> bd)
        {
            bool result;

            bd.Value.mutex.bd_mutex_lock();

            /* reset player state */

            bd.Value.title_type = BD_TITLE_TYPE.title_undef;

            if (bd.Value.hdmv_vm)
            {
                HdmvVm.hdmv_vm_free(ref bd.Value.hdmv_vm);
            }

            if (!bd.Value.event_queue)
            {
                bd.Value.event_queue = BD_EVENT_QUEUE<BD_EVENT>.event_queue_new();

                Register.bd_psr_lock(bd.Value.regs);
                Register.bd_psr_register_cb(bd.Value.regs, _process_psr_event, bd);
                _queue_initial_psr_events(bd);
                Register.bd_psr_unlock(bd.Value.regs);
            }

            Disc.disc_event(bd.Value.disc, Disc.DiscEventType.DISC_EVENT_START, 0);

            /* start playback from FIRST PLAY title */

            result = _play_title(bd, BLURAY_TITLE_FIRST_PLAY);

            bd.Value.mutex.bd_mutex_unlock();

            return result;
        }

        static bool _try_play_title(Ref<BLURAY> bd, uint title)
        {
            if (bd.Value.title_type == BD_TITLE_TYPE.title_undef && title != BLURAY_TITLE_FIRST_PLAY)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_play_title(): bd_play() not called");
                return false;
            }

            if (bd.Value.uo_mask.title_search)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"title search masked");
                _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_UO_MASKED, (uint)UoMaskIndexType.UO_MASK_TITLE_SEARCH_INDEX);
                return false;
            }

            return _play_title(bd, title);
        }

        public static bool bd_play_title(Ref<BLURAY> bd, uint title)
        {
            bool ret;

            if (title == BLURAY_TITLE_TOP_MENU)
            {
                /* menu call uses different UO mask */
                return bd_menu_call(bd, -1);
            }

            bd.Value.mutex.bd_mutex_lock();
            ret = _try_play_title(bd, title);
            bd.Value.mutex.bd_mutex_unlock();
            return ret;
        }

        static bool _try_menu_call(Ref<BLURAY> bd, Int64 pts)
        {
            _set_scr(bd, pts);

            if (bd.Value.title_type == BD_TITLE_TYPE.title_undef)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_menu_call(): bd_play() not called");
                return false;
            }

            if (bd.Value.uo_mask.menu_call)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"menu call masked");
                _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_UO_MASKED, (uint)UoMaskIndexType.UO_MASK_MENU_CALL_INDEX);
                return false;
            }

            if (bd.Value.title_type == BD_TITLE_TYPE.title_hdmv)
            {
                if (HdmvVm.hdmv_vm_suspend_pl(bd.Value.hdmv_vm) < 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_menu_call(): error storing playback location");
                }
            }

            return _play_title(bd, BLURAY_TITLE_TOP_MENU);
        }

        public static bool bd_menu_call(Ref<BLURAY> bd, Int64 pts)
        {
            bool ret;
            bd.Value.mutex.bd_mutex_lock();
            ret = _try_menu_call(bd, pts);
            bd.Value.mutex.bd_mutex_unlock();
            return ret;
        }

        static void _process_hdmv_vm_event(Ref<BLURAY> bd, Ref<HDMV_EVENT> hev)
        {
            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"HDMV _event: {HdmvVm.hdmv_event_str(hev.Value._event)}({hev.Value._event}): {hev.Value._param}");

            switch (hev.Value._event) {
                case hdmv_event_e.HDMV_EVENT_TITLE:
                    _close_playlist(bd);
                    _play_title(bd, hev.Value._param);
                    break;

                case hdmv_event_e.HDMV_EVENT_PLAY_PL:
                case hdmv_event_e.HDMV_EVENT_PLAY_PL_PI:
                case hdmv_event_e.HDMV_EVENT_PLAY_PL_PM:
                    if (!_open_playlist(bd, hev.Value._param, 0))
                    {
                        /* Missing playlist ?
                         * Seen on some discs while checking UHD capability.
                         * It seems only error message playlist is present, on success
                         * non-existing playlist is selected ...
                         */
                        bd.Value.hdmv_num_invalid_pl++;
                        if (bd.Value.hdmv_num_invalid_pl < 10)
                        {
                            HdmvVm.hdmv_vm_resume(bd.Value.hdmv_vm);
                            bd.Value.hdmv_suspended = (byte)((HdmvVm.hdmv_vm_running(bd.Value.hdmv_vm) == 0) ? 1 : 0);
                            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"Ignoring non-existing playlist {hev.Value._param:00000}.mpls in HDMV mode");
                            break;
                        }
                    }
                    else
                    {
                        if (hev.Value._event == hdmv_event_e.HDMV_EVENT_PLAY_PL_PM) {
                            bd_seek_mark(bd, hev.Value._param2);
                        } else if (hev.Value._event == hdmv_event_e.HDMV_EVENT_PLAY_PL_PI) {
                            bd_seek_playitem(bd, hev.Value._param2);
                        }
                        bd.Value.hdmv_num_invalid_pl = 0;
                    }

                    /* initialize menus */
                    _init_ig_stream(bd);
                    _run_gc(bd, gc_ctrl_e.GC_CTRL_INIT_MENU, 0);
                    break;

                case hdmv_event_e.HDMV_EVENT_PLAY_PI:
                    bd_seek_playitem(bd, hev.Value._param);
                    break;

                case hdmv_event_e.HDMV_EVENT_PLAY_PM:
                    bd_seek_mark(bd, hev.Value._param);
                    break;

                case hdmv_event_e.HDMV_EVENT_PLAY_STOP:
                    // stop current playlist
                    _close_playlist(bd);

                    bd.Value.hdmv_suspended = (byte)((HdmvVm.hdmv_vm_running(bd.Value.hdmv_vm) == 0) ? 1 : 0);
                    break;

                case hdmv_event_e.HDMV_EVENT_STILL:
                    _queue_event(bd, bd_event_e.BD_EVENT_STILL, hev.Value._param);
                    break;

                case hdmv_event_e.HDMV_EVENT_ENABLE_BUTTON:
                    _run_gc(bd, gc_ctrl_e.GC_CTRL_ENABLE_BUTTON, hev.Value._param);
                    break;

                case hdmv_event_e.HDMV_EVENT_DISABLE_BUTTON:
                    _run_gc(bd, gc_ctrl_e.GC_CTRL_DISABLE_BUTTON, hev.Value._param);
                    break;

                case hdmv_event_e.HDMV_EVENT_SET_BUTTON_PAGE:
                    _run_gc(bd, gc_ctrl_e.GC_CTRL_SET_BUTTON_PAGE, hev.Value._param);
                    break;

                case hdmv_event_e.HDMV_EVENT_POPUP_OFF:
                    _run_gc(bd, gc_ctrl_e.GC_CTRL_POPUP, 0);
                    break;

                case hdmv_event_e.HDMV_EVENT_IG_END:
                    _run_gc(bd, gc_ctrl_e.GC_CTRL_IG_END, 0);
                    break;

                case hdmv_event_e.HDMV_EVENT_END:
                case hdmv_event_e.HDMV_EVENT_NONE:
                    //default:
                    break;
            }
        }

        static int _run_hdmv(Ref<BLURAY> bd)
        {
            Variable<HDMV_EVENT> hdmv_ev = new();

            /* run VM */
            if (HdmvVm.hdmv_vm_run(bd.Value.hdmv_vm, hdmv_ev.Ref) < 0)
            {
                _queue_event(bd, bd_event_e.BD_EVENT_ERROR, BD_ERROR_HDMV);
                bd.Value.hdmv_suspended = (byte)((HdmvVm.hdmv_vm_running(bd.Value.hdmv_vm) == 0) ? 1 : 0);
                return -1;
            }

            /* process all events */
            do
            {
                _process_hdmv_vm_event(bd, hdmv_ev.Ref);

            } while (HdmvVm.hdmv_vm_get_event(bd.Value.hdmv_vm, hdmv_ev.Ref) == 0);

            /* update VM state */
            bd.Value.hdmv_suspended = (byte)((HdmvVm.hdmv_vm_running(bd.Value.hdmv_vm) == 0) ? 1 : 0);

            /* update UO mask */
            _update_hdmv_uo_mask(bd);

            return 0;
        }

        static int _read_ext(Ref<BLURAY> bd, Ref<byte> buf, int len, Ref<BD_EVENT> _event)
        {
            if (_get_event(bd, _event))
            {
                return 0;
            }

            /* run HDMV VM ? */
            if (bd.Value.title_type == BD_TITLE_TYPE.title_hdmv)
            {

                int loops = 0;
                while (bd.Value.hdmv_suspended == 0)
                {

                    if (_run_hdmv(bd) < 0)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_read_ext(): HDMV VM error");
                        bd.Value.title_type = BD_TITLE_TYPE.title_undef;
                        return -1;
                    }
                    if (loops++ > 100)
                    {
                        /* Detect infinite loops.
                         * Broken disc may cause infinite loop between graphics controller and HDMV VM.
                         * This happens ex. with "Butterfly on a Wheel":
                         * Triggering unmasked "Menu Call" UO in language selection menu skips
                         * menu system initialization code, resulting in infinite loop in root menu.
                         */
                        Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_read_ext(): detected possible HDMV mode live lock ({loops} loops)");
                        _queue_event(bd, bd_event_e.BD_EVENT_ERROR, BD_ERROR_HDMV);
                    }
                    if (_get_event(bd, _event))
                    {
                        return 0;
                    }
                }

                if ((bd.Value.gc_status & GraphicsController.GC_STATUS_ANIMATE) != 0)
                {
                    _run_gc(bd, gc_ctrl_e.GC_CTRL_NOP, 0);
                }
            }

            if (len < 1)
            {
                /* just polled events ? */
                return 0;
            }

            if (bd.Value.title_type == BD_TITLE_TYPE.title_bdj)
            {
                if (bd.Value.end_of_playlist == 1)
                {
                    _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_END_OF_PLAYLIST, Register.bd_psr_read(bd.Value.regs, bd_psr_idx.PSR_PLAYLIST));
                    bd.Value.end_of_playlist |= 2;
                }

                if (bd.Value.title == null)
                {
                    /* BD-J title running but no playlist playing */
                    _queue_event(bd, bd_event_e.BD_EVENT_IDLE, 0);
                    return 0;
                }

                if (bd.Value.bdj_wait_start != 0)
                {
                    /* BD-J playlist prefethed but not yet playing */
                    _queue_event(bd, bd_event_e.BD_EVENT_IDLE, 1);
                    return 0;
                }
            }

            int bytes = _bd_read_locked(bd, buf, len);

            if (bytes == 0)
            {

                // if no next clip (=end of title), resume HDMV VM
                if (!bd.Value.st0.Value.clip && bd.Value.title_type == BD_TITLE_TYPE.title_hdmv)
                {
                    HdmvVm.hdmv_vm_resume(bd.Value.hdmv_vm);
                    bd.Value.hdmv_suspended = (byte)((HdmvVm.hdmv_vm_running(bd.Value.hdmv_vm) == 0) ? 1 : 0);
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"bd_read_ext(): reached end of playlist. hdmv_suspended={bd.Value.hdmv_suspended}");
                }
            }

            _get_event(bd, _event);

            return bytes;
        }

        public static int bd_read_ext(Ref<BLURAY> bd, Ref<byte> buf, int len, Ref<BD_EVENT> _event)
        {
            int ret;
            bd.Value.mutex.bd_mutex_lock();
            ret = _read_ext(bd, buf, len, _event);
            bd.Value.mutex.bd_mutex_unlock();
            return ret;
        }

        public static bool bd_get_event(Ref<BLURAY> bd, Ref<BD_EVENT> _event)
        {
            if (!bd.Value.event_queue)
            {
                bd.Value.event_queue = BD_EVENT_QUEUE<BD_EVENT>.event_queue_new();

                Register.bd_psr_register_cb(bd.Value.regs, _process_psr_event, bd);
                _queue_initial_psr_events(bd);
            }

            if (_event) {
                return _get_event(bd, _event);
            }

            return false;
        }

        /*
         * user interaction
         */

        void bd_set_scr(Ref<BLURAY> bd, Int64 pts)
        {
            bd.Value.mutex.bd_mutex_lock();
            bd.Value.app_scr = 1;
            _set_scr(bd, pts);
            bd.Value.mutex.bd_mutex_unlock();
        }

        static int _set_rate(Ref<BLURAY> bd, UInt32 rate)
        {
            if (bd.Value.title == null)
            {
                return -1;
            }

            if (bd.Value.title_type == BD_TITLE_TYPE.title_bdj)
            {
                return _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_RATE, rate);
            }

            return 0;
        }

        int bd_set_rate(Ref<BLURAY> bd, UInt32 rate)
        {
            int result;

            bd.Value.mutex.bd_mutex_lock();
            result = _set_rate(bd, rate);
            bd.Value.mutex.bd_mutex_unlock();

            return result;
        }

        int bd_mouse_select(Ref<BLURAY> bd, Int64 pts, UInt16 x, UInt16 y)
        {
            UInt32 param = ((UInt32)x << 16) | y;
            int result = -1;

            bd.Value.mutex.bd_mutex_lock();

            _set_scr(bd, pts);

            if (bd.Value.title_type == BD_TITLE_TYPE.title_hdmv)
            {
                result = _run_gc(bd, gc_ctrl_e.GC_CTRL_MOUSE_MOVE, param);
            }
            else if (bd.Value.title_type == BD_TITLE_TYPE.title_bdj)
            {
                result = _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_MOUSE, param);
            }

            bd.Value.mutex.bd_mutex_unlock();

            return result;
        }

        const uint BD_VK_FLAGS_MASK = (Keys.BD_VK_KEY_PRESSED | Keys.BD_VK_KEY_TYPED | Keys.BD_VK_KEY_RELEASED);
        static bd_vk_key_e BD_VK_KEY(uint k) => (bd_vk_key_e)(k & ~BD_VK_FLAGS_MASK);
        static uint BD_VK_FLAGS(uint k) => (k & BD_VK_FLAGS_MASK);
        /* HDMV: key is triggered when pressed down */
        static bool BD_KEY_TYPED(uint k) => (k & (Keys.BD_VK_KEY_TYPED | Keys.BD_VK_KEY_RELEASED)) == 0;

        public static int bd_user_input(Ref<BLURAY> bd, Int64 pts, UInt32 key)
        {
            int result = -1;

            if (BD_VK_KEY(key) == bd_vk_key_e.BD_VK_ROOT_MENU)
            {
                if (BD_KEY_TYPED(key))
                {
                    return bd_menu_call(bd, pts) ? 1 : 0;
                }
                return 0;
            }

            bd.Value.mutex.bd_mutex_lock();

            _set_scr(bd, pts);

            if (bd.Value.title_type == BD_TITLE_TYPE.title_hdmv)
            {
                if (BD_KEY_TYPED(key))
                {
                    result = _run_gc(bd, gc_ctrl_e.GC_CTRL_VK_KEY, (uint)BD_VK_KEY(key));
                }
                else
                {
                    result = 0;
                }

            }
            else if (bd.Value.title_type == BD_TITLE_TYPE.title_bdj)
            {
                if (BD_VK_FLAGS(key) == 0)
                {
                    /* No flags -.Value. single key press _event */
                    key |= Keys.BD_VK_KEY_PRESSED | Keys.BD_VK_KEY_TYPED | Keys.BD_VK_KEY_RELEASED;
                }
                result = _bdj_event(bd, BDJ_EVENT.BDJ_EVENT_VK_KEY, key);
            }

            bd.Value.mutex.bd_mutex_unlock();

            return result;
        }

        public static void bd_register_overlay_proc(Ref<BLURAY> bd, object? handle, GraphicsController.gc_overlay_proc_f func)
        {
            if (!bd)
            {
                return;
            }

            bd.Value.mutex.bd_mutex_lock();

            GraphicsController.gc_free(ref bd.Value.graphics_controller);

            if (func != null)
            {
                bd.Value.graphics_controller = GraphicsController.gc_init(bd.Value.regs, handle, func);
            }

            bd.Value.mutex.bd_mutex_unlock();
        }

        public static void bd_register_argb_overlay_proc(Ref<BLURAY> bd, object? handle, bd_argb_overlay_proc_f func, Ref<BD_ARGB_BUFFER> buf)
        {
            if (!bd)
            {
                return;
            }

            bd.Value.argb_buffer_mutex.bd_mutex_lock();

            bd.Value.argb_overlay_proc = func;
            bd.Value.argb_overlay_proc_handle = handle;
            bd.Value.argb_buffer = buf;

            bd.Value.argb_buffer_mutex.bd_mutex_unlock();
        }

        public static int bd_get_sound_effect(Ref<BLURAY> bd, uint sound_id, Ref<BLURAY_SOUND_EFFECT> effect)
        {
            if (!bd || !effect)
            {
                return -1;
            }

            if (!bd.Value.sound_effects)
            {

                bd.Value.sound_effects = SoundParse.sound_get(bd.Value.disc);
                if (!bd.Value.sound_effects)
                {
                    return -1;
                }
            }

            if (sound_id < bd.Value.sound_effects.Value.num_sounds)
            {
                Ref<SOUND_OBJECT> o = bd.Value.sound_effects.Value.sounds.AtIndex(sound_id);

                effect.Value.num_channels = o.Value.num_channels;
                effect.Value.num_frames = o.Value.num_frames;
                effect.Value.samples = o.Value.samples;

                return 1;
            }

            return 0;
        }

        /*
         * Direct file access
         */

        static int _bd_read_file(Ref<BLURAY> bd, string dir, string file, ref Ref<byte> data, Ref<Int64> size)
        {
            if (!bd || bd.Value.disc == null || file == null || !data || !size)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_CRIT, $"Invalid arguments for bd_read_file()");
                return 0;
            }

            data = Ref<byte>.Null;
            size.Value = (Int64)Disc.disc_read_file(bd.Value.disc, dir, file, out data);
            if (!data || size.Value < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"bd_read_file() failed");
                data.Free();
                return 0;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"bd_read_file(): read {size} bytes from {Path.Combine(dir ?? "", file)}");
            return 1;
        }

        public static int bd_read_file(Ref<BLURAY> bd, string path, ref Ref<byte> data, Ref<Int64> size)
        {
            return _bd_read_file(bd, null, path, ref data, size);
        }

        public static BD_DIR_H? bd_open_dir(Ref<BLURAY> bd, string dir)
        {
            if (!bd || dir == null) {
                return null;
            }
            return Disc.disc_open_dir(bd.Value.disc, dir);
        }

        public static BD_FILE_H? bd_open_file_dec(Ref<BLURAY> bd, string path)
        {
            if (!bd || path == null) {
                return null;
            }
            return Disc.disc_open_path_dec(bd.Value.disc, path);
        }

        /*
         * Metadata
         */

        public static Ref<META_DL> bd_get_meta(Ref<BLURAY> bd)
        {
            Ref<META_DL> meta = Ref<META_DL>.Null;

            if (!bd)
            {
                return Ref<META_DL>.Null;
            }

            if (!bd.Value.meta)
            {
                bd.Value.meta = MetaParse.meta_parse(bd.Value.disc);
            }

            UInt32 psr_menu_lang = Register.bd_psr_read(bd.Value.regs, bd_psr_idx.PSR_MENU_LANG);

            if (psr_menu_lang != 0 && psr_menu_lang != 0xffffff)
            {
                string language_code = Encoding.ASCII.GetString([(byte)((psr_menu_lang >> 16) & 0xff), (byte)((psr_menu_lang >> 8) & 0xff), (byte)(psr_menu_lang & 0xff)]);
                meta = MetaParse.meta_get(bd.Value.meta, language_code);
            }
            else
            {
                meta = MetaParse.meta_get(bd.Value.meta, null);
            }

            /* assign title names to disc_info */
            if (meta && bd.Value.titles)
            {
                uint ii;
                for (ii = 0; ii < meta.Value.toc_count; ii++)
                {
                    if (meta.Value.toc_entries[ii].title_number > 0 && meta.Value.toc_entries[ii].title_number <= bd.Value.disc_info.Value.num_titles)
                    {
                        bd.Value.titles[meta.Value.toc_entries[ii].title_number].name = meta.Value.toc_entries[ii].title_name;
                    }
                }
                bd.Value.disc_info.Value.disc_name = meta.Value.di_name;
            }

            return meta;
        }

        public static int bd_get_meta_file(Ref<BLURAY> bd, string name, ref Ref<byte> data, Ref<Int64> size)
        {
            return _bd_read_file(bd, Path.Combine("BDMV", "META", "DL"), name, ref data, size);
        }

        /*
         * Database access
         */

        public static Ref<CLPI_CL> bd_get_clpi(Ref<BLURAY> bd, uint clip_ref)
        {
            if (bd.Value.title != null && clip_ref < bd.Value.title.clip_list.count)
            {
                Ref<NAV_CLIP> clip = bd.Value.title.clip_list.clip.AtIndex(clip_ref);
                return ClpiParse.clpi_copy(clip.Value.cl);
            }
            return Ref<CLPI_CL>.Null;
        }

        public static Ref<CLPI_CL> bd_read_clpi(string path)
        {
            return ClpiParse.clpi_parse(path);
        }

        public static void bd_free_clpi(Ref<CLPI_CL> cl)
        {
            ClpiParse.clpi_free(ref cl);
        }

        public static Ref<MPLS_PL> bd_read_mpls(string mpls_file)
        {
            return MplsParse.mpls_parse(mpls_file);
        }

        public static void bd_free_mpls(Ref<MPLS_PL> pl)
        {
            MplsParse.mpls_free(ref pl);
        }

        public static Ref<MOBJ_OBJECTS> bd_read_mobj(string mobj_file)
        {
            return MobjParse.mobj_parse(mobj_file);
        }

        public static void bd_free_mobj(Ref<MOBJ_OBJECTS> obj)
        {
            MobjParse.mobj_free(ref obj);
        }

        /*public static Ref<bdjo_data> bd_read_bdjo(string bdjo_file)
        {
            return bdjo_parse(bdjo_file);
        }

        public static void bd_free_bdjo(Ref<bdjo_data> obj)
        {
            bdjo_free(&obj);
        }*/
    }
}
