using libbluray;
using libbluray.decoders;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray
{
    // Player status registers

    public enum bd_psr_idx : uint
    {
        PSR_IG_STREAM_ID = 0,
        PSR_PRIMARY_AUDIO_ID = 1,
        PSR_PG_STREAM = 2, /* PG TextST and PIP PG TextST stream number */
        PSR_ANGLE_NUMBER = 3, /* 1..N */
        PSR_TITLE_NUMBER = 4, /* 1..N  (0 = top menu, 0xffff = first play) */
        PSR_CHAPTER = 5, /* 1..N  (0xffff = invalid) */
        PSR_PLAYLIST = 6, /* playlist file name number */
        PSR_PLAYITEM = 7, /* 0..N-1 (playitem_id) */
        PSR_TIME = 8, /* presetation time */
        PSR_NAV_TIMER = 9,
        PSR_SELECTED_BUTTON_ID = 10,
        PSR_MENU_PAGE_ID = 11,
        PSR_STYLE = 12,
        PSR_PARENTAL = 13,
        PSR_SECONDARY_AUDIO_VIDEO = 14,
        PSR_AUDIO_CAP = 15,
        PSR_AUDIO_LANG = 16,
        PSR_PG_AND_SUB_LANG = 17,
        PSR_MENU_LANG = 18,
        PSR_COUNTRY = 19,
        PSR_REGION = 20,
        PSR_OUTPUT_PREFER = 21,
        PSR_3D_STATUS = 22,
        PSR_DISPLAY_CAP = 23,
        PSR_3D_CAP = 24,
        PSR_UHD_CAP = 25,
        PSR_UHD_DISPLAY_CAP = 26,
        PSR_UHD_HDR_PREFER = 27,
        PSR_UHD_SDR_CONV_PREFER = 28,
        PSR_VIDEO_CAP = 29,
        PSR_TEXT_CAP = 30, /* text subtitles */
        PSR_PROFILE_VERSION = 31, /* player profile and version */
        PSR_BACKUP_PSR4 = 36,
        PSR_BACKUP_PSR5 = 37,
        PSR_BACKUP_PSR6 = 38,
        PSR_BACKUP_PSR7 = 39,
        PSR_BACKUP_PSR8 = 40,
        PSR_BACKUP_PSR10 = 42,
        PSR_BACKUP_PSR11 = 43,
        PSR_BACKUP_PSR12 = 44,
        /* 48-61: caps for characteristic text subtitle */
    }

    public struct BD_PSR_EVENT
    {
        public uint ev_type; /* event type */

        public bd_psr_idx psr_idx; /* register index */
        public UInt32 old_val; /* old value of register */
        public UInt32 new_val; /* new value of register */
    }

    public struct PSR_CB_DATA
    {
        public object handle;
        public Action<object, Ref<BD_PSR_EVENT>> cb;
    }

    public struct BD_REGISTERS
    {
        public UInt32[] psr = new uint[Register.BD_PSR_COUNT];
        public UInt32[] gpr = new uint[Register.BD_GPR_COUNT];

        /* callbacks */
        public uint num_cb;
        public Ref<PSR_CB_DATA> cb;

        public BD_MUTEX mutex = new();

        public BD_REGISTERS() { }
    }

    public static class Register
    {
        public const int BD_PSR_SAVE = 1; /* backup player state. Single event, psr_idx and values undefined */
        public const int BD_PSR_WRITE = 2; /* write, value unchanged */
        public const int BD_PSR_CHANGE = 3; /* write, value changed */
        public const int BD_PSR_RESTORE = 4; /* restore backup values */

        public const int BD_PSR_COUNT = 128;
        public const int BD_GPR_COUNT = 4096;

        /// <summary>
        /// Initial values for player status/setting registers (5.8.2).
        /// PS in comment indicates player setting .Value. register can't be changed from movie object code.
        /// </summary>
        private static readonly UInt32[] bd_psr_init = new uint[BD_PSR_COUNT] {
            1,           /*     PSR0:  Interactive graphics stream number */
            0xff,        /*     PSR1:  Primary audio stream number */
            0x0fff0fff,  /*     PSR2:  PG TextST stream number and PiP PG stream number*/
            1,           /*     PSR3:  Angle number */
            0xffff,      /*     PSR4:  Title number */
            0xffff,      /*     PSR5:  Chapter number */
            0,           /*     PSR6:  PlayList ID */
            0,           /*     PSR7:  PlayItem ID */
            0,           /*     PSR8:  Presentation time */
            0,           /*     PSR9:  Navigation timer */
            0xffff,      /*     PSR10: Selected button ID */
            0,           /*     PSR11: Page ID */
            0xff,        /*     PSR12: User style number */
            0xff,        /* PS: PSR13: User age */
            0xffff,      /*     PSR14: Secondary audio stream number and secondary video stream number */
                         /* PS: PSR15: player capability for audio */
            (uint)(BLURAY_PLAYER_SETTING_AUDIO_CAP.BLURAY_ACAP_LPCM_48_96_SURROUND |
            BLURAY_PLAYER_SETTING_AUDIO_CAP.BLURAY_ACAP_LPCM_192_SURROUND   |
            BLURAY_PLAYER_SETTING_AUDIO_CAP.BLURAY_ACAP_DDPLUS_SURROUND     |
            BLURAY_PLAYER_SETTING_AUDIO_CAP.BLURAY_ACAP_DDPLUS_DEP_SURROUND |
            BLURAY_PLAYER_SETTING_AUDIO_CAP.BLURAY_ACAP_DTSHD_CORE_SURROUND |
            BLURAY_PLAYER_SETTING_AUDIO_CAP.BLURAY_ACAP_DTSHD_EXT_SURROUND  |
            BLURAY_PLAYER_SETTING_AUDIO_CAP.BLURAY_ACAP_DD_SURROUND         |
            BLURAY_PLAYER_SETTING_AUDIO_CAP.BLURAY_ACAP_MLP_SURROUND),

            0xffffff,    /* PS: PSR16: Language code for audio */
            0xffffff,    /* PS: PSR17: Language code for PG and Text subtitles */
            0xffffff,    /* PS: PSR18: Menu description language code */
            0xffff,      /* PS: PSR19: Country code */
                         /* PS: PSR20: Region code */ /* 1 - A, 2 - B, 4 - C */
            (uint)BLURAY_PLAYER_SETTING_REGION_CODE.BLURAY_REGION_B,
                         /* PS: PSR21: Output mode preference */
            (uint)BLURAY_PLAYER_SETTING_OUTPUT_PREFER.BLURAY_OUTPUT_PREFER_2D,
            0,           /*     PSR22: Stereoscopic status */
            0,           /* PS: PSR23: Display capability */
            0,           /* PS: PSR24: 3D capability */
            0,           /* PS: PSR25: UHD capability */
            0,           /* PS: PSR26: UHD display capability */
            0,           /* PS: PSR27: HDR preference */
            0,           /* PS: PSR28: SDR conversion preference */
                         /* PS: PSR29: player capability for video */
            (uint)(BLURAY_PLAYER_SETTING_VIDEO_CAP.BLURAY_VCAP_SECONDARY_HD |
            BLURAY_PLAYER_SETTING_VIDEO_CAP.BLURAY_VCAP_25Hz_50Hz),

            0x1ffff,     /* PS: PSR30: player capability for text subtitle */
                         /* PS: PSR31: Player profile and version */
            (uint)BLURAY_PLAYER_SETTING_PLAYER_PROFILE.BLURAY_PLAYER_PROFILE_2_v2_0,
            0,           /*     PSR32 */
            0,           /*     PSR33 */
            0,           /*     PSR34 */
            0,           /*     PSR35 */
            0xffff,      /*     PSR36: backup PSR4 */
            0xffff,      /*     PSR37: backup PSR5 */
            0,           /*     PSR38: backup PSR6 */
            0,           /*     PSR39: backup PSR7 */
            0,           /*     PSR40: backup PSR8 */
            0,           /*     PSR41: */
            0xffff,      /*     PSR42: backup PSR10 */
            0,           /*     PSR43: backup PSR11 */
            0xff,        /*     PSR44: backup PSR12 */
            0,           /*     PSR45: */
            0,           /*     PSR46: */
            0,           /*     PSR47: */
            0xffffffff,  /* PS: PSR48: Characteristic text caps */
            0xffffffff,  /* PS: PSR49: Characteristic text caps */
            0xffffffff,  /* PS: PSR50: Characteristic text caps */
            0xffffffff,  /* PS: PSR51: Characteristic text caps */
            0xffffffff,  /* PS: PSR52: Characteristic text caps */
            0xffffffff,  /* PS: PSR53: Characteristic text caps */
            0xffffffff,  /* PS: PSR54: Characteristic text caps */
            0xffffffff,  /* PS: PSR55: Characteristic text caps */
            0xffffffff,  /* PS: PSR56: Characteristic text caps */
            0xffffffff,  /* PS: PSR57: Characteristic text caps */
            0xffffffff,  /* PS: PSR58: Characteristic text caps */
            0xffffffff,  /* PS: PSR59: Characteristic text caps */
            0xffffffff,  /* PS: PSR60: Characteristic text caps */
            0xffffffff,  /* PS: PSR61: Characteristic text caps */
            /* 62-95:   reserved */
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            /* 96-111:  reserved for BD system use */
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            /* 112-127: reserved */
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
        };

        /// <summary>
        /// PSR ids for debugging
        /// </summary>
        private static readonly string[] bd_psr_name = new string[BD_PSR_COUNT]
        {
            "IG_STREAM_ID",
            "PRIMARY_AUDIO_ID",
            "PG_STREAM",
            "ANGLE_NUMBER",
            "TITLE_NUMBER",
            "CHAPTER",
            "PLAYLIST",
            "PLAYITEM",
            "TIME",
            "NAV_TIMER",
            "SELECTED_BUTTON_ID",
            "MENU_PAGE_ID",
            "STYLE",
            "PARENTAL",
            "SECONDARY_AUDIO_VIDEO",
            "AUDIO_CAP",
            "AUDIO_LANG",
            "PG_AND_SUB_LANG",
            "MENU_LANG",
            "COUNTRY",
            "REGION",
            "OUTPUT_PREFER",
            "3D_STATUS",
            "DISPLAY_CAP",
            "3D_CAP",
            //"PSR_VIDEO_CAP",
            null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,
            null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,
            null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,
            null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,
            null, null, null
        };

        /*
 * init / free
 */

        internal static Ref<BD_REGISTERS> bd_registers_init()
        {
            Ref<BD_REGISTERS> p = Ref<BD_REGISTERS>.Allocate();

            if (p)
            {
                bd_psr_init.AsSpan().CopyTo(p.Value.psr);

                p.Value.mutex = new(); //.bd_mutex_init();
            }

            return p;
        }

        internal static void bd_registers_free(Ref<BD_REGISTERS> p)
        {
            if (p)
            {
                p.Value.mutex.bd_mutex_destroy();

                p.Value.cb.Free();
            }

            p.Free();
        }

        /*
         * PSR lock / unlock
         */

        internal static void bd_psr_lock(Ref<BD_REGISTERS> p)
        {
            p.Value.mutex.bd_mutex_lock();
        }

        internal static void bd_psr_unlock(Ref<BD_REGISTERS> p)
        {
            p.Value.mutex.bd_mutex_unlock();
        }

        /*
         * PSR change callback register / unregister
         */

        internal static void bd_psr_register_cb(Ref<BD_REGISTERS> p, Action<object, Ref<BD_PSR_EVENT>> callback, object cb_handle)
        {
            /* no duplicates ! */
            Ref<PSR_CB_DATA> cb;
            uint i;

            bd_psr_lock(p);

            for (i = 0; i < p.Value.num_cb; i++)
            {
                if (p.Value.cb[i].handle == cb_handle && p.Value.cb[i].cb == callback)
                {

                    bd_psr_unlock(p);
                    return;
                }
            }

            cb = p.Value.cb.Reallocate(p.Value.num_cb + 1);
            if (cb)
            {
                p.Value.cb = cb;
                p.Value.cb[p.Value.num_cb].cb = callback;
                p.Value.cb[p.Value.num_cb].handle = cb_handle;
                p.Value.num_cb++;
            }
            else
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_psr_register_cb(): realloc failed");
            }

            bd_psr_unlock(p);
        }

        internal static void bd_psr_unregister_cb(Ref<BD_REGISTERS> p, Action<object, Ref<BD_PSR_EVENT>> callback, object cb_handle)
        {
            uint i = 0;

            bd_psr_lock(p);

            while (i < p.Value.num_cb)
            {
                if (p.Value.cb[i].handle == cb_handle && p.Value.cb[i].cb == callback)
                {
                    if ((--p.Value.num_cb) != 0 && i < p.Value.num_cb)
                    {
                        (p.Value.cb + i + 1).AsSpan().Slice(0, (int)(p.Value.num_cb - i)).CopyTo((p.Value.cb + i).AsSpan());
                        continue;
                    }
                }
                i++;
            }

            bd_psr_unlock(p);
        }

        /*
         * PSR state save / restore
         */

        internal static void bd_psr_save_state(Ref<BD_REGISTERS> p)
        {
            /* store registers 4-8 and 10-12 to backup registers */

            bd_psr_lock(p);

            Ref<uint> ptr = new Ref<uint>(p.Value.psr);
            (ptr + 4).AsSpan().Slice(0, 5).CopyTo((ptr + 36).AsSpan());
            (ptr + 10).AsSpan().Slice(0, 3).CopyTo((ptr + 42).AsSpan());

            /* generate save event */

            if (p.Value.num_cb != 0)
            {
                Variable<BD_PSR_EVENT> ev = new();
                ev.Value.ev_type = BD_PSR_SAVE;
                ev.Value.psr_idx = (bd_psr_idx)uint.MaxValue;
                ev.Value.old_val = 0;
                ev.Value.new_val = 0;

                uint j;
                for (j = 0; j < p.Value.num_cb; j++)
                {
                    p.Value.cb[j].cb(p.Value.cb[j].handle, ev.Ref);
                }
            }

            bd_psr_unlock(p);
        }

        internal static void bd_psr_reset_backup_registers(Ref<BD_REGISTERS> p)
        {
            bd_psr_lock(p);

            /* init backup registers to default */
            Ref<uint> src = new Ref<uint>(bd_psr_init);
            Ref<uint> dst = new Ref<uint>(p.Value.psr);
            (src + 36).AsSpan().Slice(0, 5).CopyTo((dst + 36).AsSpan());
            (src + 42).AsSpan().Slice(0, 3).CopyTo((dst + 42).AsSpan());

            bd_psr_unlock(p);
        }

        internal static void bd_psr_restore_state(Ref<BD_REGISTERS> p)
        {
            UInt32[] old_psr = new UInt32[13];
            UInt32[] new_psr = new UInt32[13];

            bd_psr_lock(p);

            if (p.Value.num_cb != 0)
            {
                Array.Copy(p.Value.psr, old_psr, 13);
            }

            /* restore backup registers */
            Ref<uint> ptr = new Ref<uint>(p.Value.psr);
            (ptr + 36).AsSpan().Slice(0, 5).CopyTo((ptr + 4).AsSpan());
            (ptr + 42).AsSpan().Slice(0, 3).CopyTo((ptr + 10).AsSpan());

            if (p.Value.num_cb != 0)
            {
                Array.Copy(p.Value.psr, new_psr, 13);
            }

            /* init backup registers to default */
            Ref<uint> src = new Ref<uint>(bd_psr_init);
            (src + 36).AsSpan().Slice(0, 5).CopyTo((ptr + 36).AsSpan());
            (src + 42).AsSpan().Slice(0, 3).CopyTo((ptr + 42).AsSpan());

            /* generate restore events */
            if (p.Value.num_cb != 0)
            {
                Variable<BD_PSR_EVENT> ev = new();
                uint i, j;

                ev.Value.ev_type = BD_PSR_RESTORE;

                for (i = 4; i < 13; i++)
                {
                    if (i != (int)bd_psr_idx.PSR_NAV_TIMER)
                    {

                        ev.Value.psr_idx = (bd_psr_idx)i;
                        ev.Value.old_val = old_psr[i];
                        ev.Value.new_val = new_psr[i];

                        for (j = 0; j < p.Value.num_cb; j++)
                        {
                            p.Value.cb[j].cb(p.Value.cb[j].handle, ev.Ref);
                        }
                    }
                }
            }

            bd_psr_unlock(p);
        }

        /*
         * GPR read / write
         */

        internal static int bd_gpr_write(Ref<BD_REGISTERS> p, bd_psr_idx reg, UInt32 val)
        {
            if ((uint)reg >= BD_GPR_COUNT)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"bd_gpr_write({reg}): invalid register");
                return -1;
            }

            p.Value.gpr[(uint)reg] = val;
            return 0;
        }

        internal static UInt32 bd_gpr_read(Ref<BD_REGISTERS> p, bd_psr_idx reg)
        {
            if ((uint)reg >= BD_GPR_COUNT)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"bd_gpr_read({reg}): invalid register");
                return 0;
            }

            return p.Value.gpr[(uint)reg];
        }

        /*
         * PSR read / write
         */

        internal static UInt32 bd_psr_read(Ref<BD_REGISTERS> p, bd_psr_idx reg)
        {
            UInt32 val;

            if ((int)reg >= BD_PSR_COUNT)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"bd_psr_read({reg}): invalid register");
                return UInt32.MaxValue;
            }

            bd_psr_lock(p);

            val = p.Value.psr[(int)reg];

            bd_psr_unlock(p);

            return val;
        }

        internal static int bd_psr_setting_write(Ref<BD_REGISTERS> p, bd_psr_idx reg, UInt32 val)
        {
            if ((int)reg >= BD_PSR_COUNT)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"bd_psr_write({reg}, {val}): invalid register");
                return -1;
            }

            bd_psr_lock(p);

            if (p.Value.psr[(int)reg] == val)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"bd_psr_write({reg}, {val}): no change in value");
            }
            else if (bd_psr_name[(int)reg] != null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"bd_psr_write(): PSR{reg} ({bd_psr_name[(int)reg]}) 0x{p.Value.psr[(int)reg]:x} .Value. 0x{val:x}");
            }
            else
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"bd_psr_write(): PSR{reg} 0x{p.Value.psr[(int)reg]:x} .Value. 0x{val:x}");
            }

            if (p.Value.num_cb != 0)
            {
                Variable<BD_PSR_EVENT> ev = new();
                uint i;

                ev.Value.ev_type = (uint)((p.Value.psr[(int)reg] == val) ? BD_PSR_WRITE : BD_PSR_CHANGE);
                ev.Value.psr_idx = (bd_psr_idx)reg;
                ev.Value.old_val = p.Value.psr[(int)reg];
                ev.Value.new_val = val;

                p.Value.psr[(int)reg] = val;

                for (i = 0; i < p.Value.num_cb; i++)
                {
                    p.Value.cb[i].cb(p.Value.cb[i].handle, ev.Ref);
                }

            }
            else
            {

                p.Value.psr[(int)reg] = val;
            }

            bd_psr_unlock(p);

            return 0;
        }

        internal static int bd_psr_write(Ref<BD_REGISTERS> p, bd_psr_idx reg, UInt32 val)
        {
            uint r = (uint)reg;
            if ((r == 13) ||
                (r >= 15 && r <= 21) ||
                (r >= 23 && r <= 31) ||
                (r >= 48 && r <= 61))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"bd_psr_write({reg}, {val}): read-only register !");
                return -2;
            }

            return bd_psr_setting_write(p, reg, val);
        }

        internal static int bd_psr_write_bits(Ref<BD_REGISTERS> p, bd_psr_idx reg, UInt32 val, UInt32 mask)
        {
            int result;

            if (mask == 0xffffffff)
            {
                return bd_psr_write(p, reg, val);
            }

            bd_psr_lock(p);

            UInt32 psr_value = bd_psr_read(p, reg);
            psr_value = (psr_value & (~mask)) | (val & mask);
            result = bd_psr_write(p, reg, psr_value);

            bd_psr_unlock(p);

            return result;
        }

        /*
         * save / restore registers between playback sessions
         */

        internal static void registers_save(Ref<BD_REGISTERS> p, Ref<UInt32> psr, Ref<UInt32> gpr)
        {
            bd_psr_lock(p);

            p.Value.gpr.AsSpan().CopyTo(gpr.AsSpan());
            p.Value.psr.AsSpan().CopyTo(psr.AsSpan());

            bd_psr_unlock(p);
        }

        internal static void registers_restore(Ref<BD_REGISTERS> p, Ref<UInt32> psr, Ref<UInt32> gpr)
        {
            UInt32[] new_psr = new uint[13];

            bd_psr_lock(p);

            gpr.AsSpan().Slice(0, p.Value.gpr.Length).CopyTo(p.Value.gpr);
            psr.AsSpan().Slice(0, p.Value.psr.Length).CopyTo(p.Value.psr);

            Array.Copy(p.Value.psr, new_psr, 13);

            /* generate restore events */
            if (p.Value.num_cb != 0)
            {
                Variable<BD_PSR_EVENT> ev = new();
                uint i, j;

                ev.Value.ev_type = BD_PSR_RESTORE;
                ev.Value.old_val = 0; /* not used with BD_PSR_RESTORE */

                for (i = 4; i < 13; i++)
                {
                    if (i != (int)bd_psr_idx.PSR_NAV_TIMER)
                    {

                        p.Value.psr[i] = new_psr[i];

                        ev.Value.psr_idx = (bd_psr_idx)i;
                        ev.Value.new_val = new_psr[i];

                        for (j = 0; j < p.Value.num_cb; j++)
                        {
                            p.Value.cb[j].cb(p.Value.cb[j].handle, ev.Ref);
                        }
                    }
                }
            }

            bd_psr_unlock(p);
        }

        /*
         *
         */

        internal static int psr_init_3D(Ref<BD_REGISTERS> p, int initial_mode, int force)
        {
            bd_psr_lock(p);

            /* make automatic initialization to fail if app has already changed player profile */
            if (force == 0)
            {
                if ((bd_psr_read(p, bd_psr_idx.PSR_PROFILE_VERSION) & PlayerSettings.BLURAY_PLAYER_PROFILE_VERSION_MASK) >= 0x0300)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"psr_init_3D() failed: profile version already set to >= 0x0300 (profile 6)");
                    bd_psr_unlock(p);
                    return -1;
                }
                if ((bd_psr_read(p, bd_psr_idx.PSR_PROFILE_VERSION) & PlayerSettings.BLURAY_PLAYER_PROFILE_3D_FLAG) != 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"psr_init_3D() failed: 3D already set in profile");
                    bd_psr_unlock(p);
                    return -1;
                }
            }

            bd_psr_setting_write(p, bd_psr_idx.PSR_OUTPUT_PREFER,
                                 (uint)BLURAY_PLAYER_SETTING_OUTPUT_PREFER.BLURAY_OUTPUT_PREFER_3D);

            bd_psr_setting_write(p, bd_psr_idx.PSR_DISPLAY_CAP,
                                 (uint)(BLURAY_PLAYER_SETTING_DISPLAY_CAP.BLURAY_DCAP_1080p_720p_3D |
                                 BLURAY_PLAYER_SETTING_DISPLAY_CAP.BLURAY_DCAP_720p_50Hz_3D |
                                 BLURAY_PLAYER_SETTING_DISPLAY_CAP.BLURAY_DCAP_NO_3D_CLASSES_REQUIRED |
                                 BLURAY_PLAYER_SETTING_DISPLAY_CAP.BLURAY_DCAP_INTERLACED_3D |
                                 0));

            bd_psr_setting_write(p, bd_psr_idx.PSR_3D_CAP,
                                 /* TODO */ 0xffffffff);

            bd_psr_setting_write(p, bd_psr_idx.PSR_PROFILE_VERSION,
                                 (uint)BLURAY_PLAYER_SETTING_PLAYER_PROFILE.BLURAY_PLAYER_PROFILE_5_v2_4);

            bd_psr_write(p, bd_psr_idx.PSR_3D_STATUS,
                         (initial_mode == 0) ? 0u : 1u);

            bd_psr_unlock(p);

            return 0;
        }

        internal static int psr_init_UHD(Ref<BD_REGISTERS> p, int force)
        {
            bd_psr_lock(p);

            /* make automatic initialization to fail if app has already changed player profile */
            if (force == 0)
            {
                if ((bd_psr_read(p, bd_psr_idx.PSR_PROFILE_VERSION) & PlayerSettings.BLURAY_PLAYER_PROFILE_VERSION_MASK) >= 0x0300)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"psr_init_UHD() failed: profile version already >= 0x0300");
                    bd_psr_unlock(p);
                    return -1;
                }
                if ((bd_psr_read(p, bd_psr_idx.PSR_PROFILE_VERSION) & PlayerSettings.BLURAY_PLAYER_PROFILE_3D_FLAG) != 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"psr_init_UHD() failed: 3D already set in profile");
                    bd_psr_unlock(p);
                    return -1;
                }
            }

            bd_psr_setting_write(p, bd_psr_idx.PSR_UHD_CAP,
                                 /* TODO */ 0xffffffff);

            bd_psr_setting_write(p, bd_psr_idx.PSR_UHD_DISPLAY_CAP,
                                 /* TODO */ 0xffffffff);

            bd_psr_setting_write(p, bd_psr_idx.PSR_UHD_HDR_PREFER,
                                 /* TODO */ 0xffffffff);

            bd_psr_setting_write(p, bd_psr_idx.PSR_UHD_SDR_CONV_PREFER,
                                 /* TODO */ 0);

            bd_psr_setting_write(p, bd_psr_idx.PSR_PROFILE_VERSION,
                                 (uint)BLURAY_PLAYER_SETTING_PLAYER_PROFILE.BLURAY_PLAYER_PROFILE_6_v3_1);

            bd_psr_unlock(p);

            return 0;
        }
    }

}
