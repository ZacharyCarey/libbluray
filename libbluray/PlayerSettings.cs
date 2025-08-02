using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace libbluray
{
    /// <summary>
    /// Player capability for audio (bitmask) (PSR15)
    /// </summary>
    [Flags]
    public enum BLURAY_PLAYER_SETTING_AUDIO_CAP : uint
    {
        /* LPCM capability */

        /* 48/96kHz (mandatory) */
        /// <summary>
        /// LPCM 48kHz and 96kHz stereo capable
        /// </summary>
        BLURAY_ACAP_LPCM_48_96_STEREO_ONLY = 0x0001,

        /// <summary>
        /// LPCM 48kHz and 96kHz surround capable
        /// </summary>
        BLURAY_ACAP_LPCM_48_96_SURROUND = 0x0002,

        /* 192kHz (optional) */
        /// <summary>
        /// LPCM 192kHz not supported
        /// </summary>
        BLURAY_ACAP_LPCM_192_NONE = 0x0000,

        /// <summary>
        /// LPCM 192kHz stereo capable
        /// </summary>
        BLURAY_ACAP_LPCM_192_STEREO_ONLY = 0x0004,

        /// <summary>
        /// LPCM 192kHz surround capable
        /// </summary>
        BLURAY_ACAP_LPCM_192_SURROUND = 0x0008,

        /* Dolby Digital Plus capability */

        /* independent substream (mandatory) */
        /// <summary>
        /// DD Plus independent substream stereo capable
        /// </summary>
        BLURAY_ACAP_DDPLUS_STEREO_ONLY = 0x0010,

        /// <summary>
        /// DD Plus independent substream surround capable
        /// </summary>
        BLURAY_ACAP_DDPLUS_SURROUND = 0x0020,

        /* dependent substream (optional) */
        /// <summary>
        /// DD Plus dependent substream not supported
        /// </summary>
        BLURAY_ACAP_DDPLUS_DEP_NONE = 0x0000,

        /// <summary>
        /// DD Plus dependent substream stereo capable
        /// </summary>
        BLURAY_ACAP_DDPLUS_DEP_STEREO_ONLY = 0x0040,

        /// <summary>
        /// DD Plus dependent substream surround capable
        /// </summary>
        BLURAY_ACAP_DDPLUS_DEP_SURROUND = 0x0080,

        /* DTS-HD */

        /* Core substream (mandatory) */
        /// <summary>
        /// DTS-HD Core stereo capable
        /// </summary>
        BLURAY_ACAP_DTSHD_CORE_STEREO_ONLY = 0x0100,

        /// <summary>
        ///  DTS-HD Core surround capable
        /// </summary>
        BLURAY_ACAP_DTSHD_CORE_SURROUND = 0x0200,

        /* Extension substream (optional) */
        /// <summary>
        /// DTS-HD extension substream not supported
        /// </summary>
        BLURAY_ACAP_DTSHD_EXT_NONE = 0x0000,

        /// <summary>
        /// DTS-HD extension substream stereo capable
        /// </summary>
        BLURAY_ACAP_DTSHD_EXT_STEREO_ONLY = 0x0400,

        /// <summary>
        /// DTS-HD extension substream surround capable
        /// </summary>
        BLURAY_ACAP_DTSHD_EXT_SURROUND = 0x0800,

        /* Dolby lossless (TrueHD) */

        /* Dolby Digital (mandatory) */
        /// <summary>
        /// Dolby Digital audio stereo capable
        /// </summary>
        BLURAY_ACAP_DD_STEREO_ONLY = 0x1000,

        /// <summary>
        /// Dolby Digital audio surround capable
        /// </summary>
        BLURAY_ACAP_DD_SURROUND = 0x2000,

        /* MLP (optional) */
        /// <summary>
        /// MLP not supported
        /// </summary>
        BLURAY_ACAP_MLP_NONE = 0x0000,

        /// <summary>
        /// MLP stereo capable
        /// </summary>
        BLURAY_ACAP_MLP_STEREO_ONLY = 0x4000,

        /// <summary>
        /// MLP surround capable
        /// </summary>
        BLURAY_ACAP_MLP_SURROUND = 0x8000,  
    }

    /// <summary>
    /// Player region code (integer) (PSR20)
    /// </summary>
    [Flags]
    public enum BLURAY_PLAYER_SETTING_REGION_CODE : uint
    {
        /// <summary>
        /// Region A: the Americas, East and Southeast Asia, U.S. territories, and Bermuda.
        /// </summary>
        BLURAY_REGION_A = 1,

        /// <summary>
        /// Region B: Africa, Europe, Oceania, the Middle East, the Kingdom of the Netherlands,
        /// British overseas territories, French territories, and Greenland.
        /// </summary>
        BLURAY_REGION_B = 2,

        /// <summary>
        /// Region C: Central and South Asia, Mongolia, Russia, and the People's Republic of China.
        /// </summary>
        BLURAY_REGION_C = 4,
    }

    /// <summary>
    /// Output mode preference (integer) (PSR21)
    /// </summary>
    public enum BLURAY_PLAYER_SETTING_OUTPUT_PREFER
    {
        /// <summary>
        /// 2D output preferred
        /// </summary>
        BLURAY_OUTPUT_PREFER_2D = 0,

        /// <summary>
        /// 3D output preferred
        /// </summary>
        BLURAY_OUTPUT_PREFER_3D = 1, 
    }

    /// <summary>
    /// Display capability (bit mask) and display size (PSR23)
    /// </summary>
    public enum BLURAY_PLAYER_SETTING_DISPLAY_CAP
    {
        /// <summary>
        /// capable of 1920x1080 23.976Hz and 1280x720 59.94Hz 3D
        /// </summary>
        BLURAY_DCAP_1080p_720p_3D = 0x01,

        /// <summary>
        /// capable of 1280x720 50Hz 3D
        /// </summary>
        BLURAY_DCAP_720p_50Hz_3D = 0x02,

        /// <summary>
        /// 3D glasses are not required
        /// </summary>
        BLURAY_DCAP_NO_3D_CLASSES_REQUIRED = 0x04,

        /// <summary>
        /// capable of interlaced 3D
        /// </summary>
        BLURAY_DCAP_INTERLACED_3D = 0x08,  
    }

    /// <summary>
    /// Player capability for video (bit mask) (PSR29)
    /// </summary>
    [Flags]
    public enum BLURAY_PLAYER_SETTING_VIDEO_CAP
    {
        /// <summary>
        /// player can play secondary stream in HD
        /// </summary>
        BLURAY_VCAP_SECONDARY_HD = 0x01,

        /// <summary>
        /// player can play 25Hz and 50Hz video
        /// </summary>
        BLURAY_VCAP_25Hz_50Hz = 0x02,  
    }

    /// <summary>
    /// Player profile and version (PSR31)
    /// Profile 1, version 1.0: no local storage, no VFS, no internet
    /// Profile 1, version 1.1: PiP, VFS, sec.audio, 256MB local storage, no internet
    /// Profile 2, version 2.0: BdLive(internet), 1GB local storage
    /// </summary>
    public enum BLURAY_PLAYER_SETTING_PLAYER_PROFILE : uint
    {
        /// <summary>
        /// Profile 1, version 1.0 (Initial Standard Profile)
        /// </summary>
        BLURAY_PLAYER_PROFILE_1_v1_0 = ((0x00 << 16) | (0x0100)),

        /// <summary>
        /// Profile 1, version 1.1 (secondary stream support)
        /// </summary>
        BLURAY_PLAYER_PROFILE_1_v1_1 = ((0x01 << 16) | (0x0110)),

        /// <summary>
        /// Profile 2, version 2.0 (network access, BdLive)
        /// </summary>
        BLURAY_PLAYER_PROFILE_2_v2_0 = ((0x03 << 16) | (0x0200)),

        /// <summary>
        /// Profile 3, version 2.0 (audio only player)
        /// </summary>
        BLURAY_PLAYER_PROFILE_3_v2_0 = ((0x08 << 16) | (0x0200)),

        /// <summary>
        /// Profile 5, version 2.4 (3D)
        /// </summary>
        BLURAY_PLAYER_PROFILE_5_v2_4 = ((0x13 << 16) | (0x0240)),

        /// <summary>
        /// Profile 6, version 3.0 (UHD)
        /// </summary>
        BLURAY_PLAYER_PROFILE_6_v3_0 = ((0x00 << 16) | (0x0300)),

        /// <summary>
        /// Profile 6, version 3.1 (UHD)
        /// </summary>
        BLURAY_PLAYER_PROFILE_6_v3_1 = ((0x00 << 16) | (0x0310)),   
    }

    /// <summary>
    /// Enable Presentation Graphics and Text Subtitle decoder
    /// </summary>
    public enum BLURAY_PLAYER_SETTING_DECODE_PG
    {
        BLURAY_PG_TEXTST_DECODER_DISABLE = 0,  /**< disable both decoders */
        BLURAY_PG_TEXTST_DECODER_ENABLE = 1,  /**< enable both decoders */
    }

    /// <summary>
    /// Enable / disable BD-J persistent storage.
    ///
    /// If persistent storage is disabled, BD-J Xlets can't access any data
    /// stored during earlier playback sessions.Persistent data stored during
    /// current playback session will be removed and can't be accessed later.
    ///
    /// This setting can't be changed after bd_play() has been called.
    /// </summary>
    public enum BLURAY_PLAYER_SETTING_PERSISTENT_STORAGE
    {
        BLURAY_PERSISTENT_STORAGE_DISABLE = 0,  /**< disable persistent storage between playback sessions */
        BLURAY_PERSISTENT_STORAGE_ENABLE = 1,  /**< enable persistent storage */
    }

    internal static class PlayerSettings
    {
        /* horizontal display size in centimeters */
        /// <summary>
        /// connected display physical size unknown/undefined
        /// </summary>
        public const uint BLURAY_DCAP_DISPLAY_SIZE_UNDEFINED = 0;

        /// <summary>
        /// display size mask
        /// </summary>
        public const uint BLURAY_DCAP_DISPLAY_SIZE_MASK = 0xfff00;

        /// <summary>
        /// connected display physical size (cm)
        /// </summary>
        /// <param name="cm"></param>
        /// <returns></returns>
        public static uint BLURAY_DCAP_DISPLAY_SIZE(uint cm)
        {
            if (cm > 0xfff)
            {
                cm = 0xfff;
            }

            return cm << 8;
        }

        /// <summary>
        /// set for 3D profiles
        /// </summary>
        public const uint BLURAY_PLAYER_PROFILE_3D_FLAG = 0x100000;

        /// <summary>
        /// bit mask for player version
        /// </summary>
        public const uint BLURAY_PLAYER_PROFILE_VERSION_MASK = 0xffff;
    }
}
