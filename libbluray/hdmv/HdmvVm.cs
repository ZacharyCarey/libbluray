using libbluray.disc;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.hdmv
{
    public enum hdmv_event_e
    {
        HDMV_EVENT_NONE = 0,       /* no events */
        HDMV_EVENT_END,            /* end of program (movie object) */
        HDMV_EVENT_IG_END,         /* end of program (interactive) */

        /*
         * playback control
         */

        HDMV_EVENT_TITLE,          /* play title (from disc index) */
        HDMV_EVENT_PLAY_PL,        /* select playlist */
        HDMV_EVENT_PLAY_PL_PM,     /* select playlist (and mark) */
        HDMV_EVENT_PLAY_PL_PI,     /* select playlist (and playitem) */
        HDMV_EVENT_PLAY_PI,        /* seek to playitem */
        HDMV_EVENT_PLAY_PM,        /* seek to playmark */
        HDMV_EVENT_PLAY_STOP,      /* stop playing playlist */

        HDMV_EVENT_STILL,          /* param: boolean */

        /*
         * .Value. graphics controller
         */
        HDMV_EVENT_SET_BUTTON_PAGE,
        HDMV_EVENT_ENABLE_BUTTON,
        HDMV_EVENT_DISABLE_BUTTON,
        HDMV_EVENT_POPUP_OFF,
    }

    public struct HDMV_EVENT
    {
        public hdmv_event_e _event;
        public UInt32 _param;
        public UInt32 _param2;
    }

    public struct HDMV_VM
    {
        public BD_MUTEX mutex;

        /* state */
        public UInt32 pc;            /* program counter */
        public Ref<BD_REGISTERS> regs;          /* player registers */
        public Ref<MOBJ_OBJECT> _object;    /* currently running object code */

        public HDMV_EVENT[] _event = new HDMV_EVENT[5];      /* pending events to return */

        public NV_TIMER nv_timer;      /* navigation timer */
        public UInt64 rand;          /* RAND state */

        /* movie objects */
        public Ref<MOBJ_OBJECTS> movie_objects; /* disc movie objects */
        public Ref<MOBJ_OBJECT> ig_object;     /* current object from IG stream */

        /* object currently playing playlist */
        public Ref<MOBJ_OBJECT> playing_object;
        public UInt32 playing_pc;

        /* suspended object */
        public Ref<MOBJ_OBJECT> suspended_object;
        public UInt32 suspended_pc;

        /* Available titles. Used to validate CALL_TITLE/JUMP_TITLE. */
        public byte have_top_menu;
        public byte have_first_play;
        public UInt16 num_titles;

        public HDMV_VM() { }
    }

    public struct NV_TIMER
    {
        public DateTime time;
        public UInt32 mobj_id;
    }

    internal static class HdmvVm
    {
        public const int HDMV_MENU_CALL_MASK = 0x01;
        public const int HDMV_TITLE_SEARCH_MASK = 0x02;
        public const int HDMV_STATE_SIZE = 10;

        /*
 * save / restore VM state
 */

        static int _save_state(Ref<HDMV_VM> p, Ref<UInt32> s)
        {
            s.AsSpan().Slice(0, HDMV_STATE_SIZE).Fill(0u);

            if (p.Value.ig_object)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_save_state() failed: button object running");
                return -1;
            }
            if (p.Value._object) {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_save_state() failed: movie object running");
                return -1;
            }
            if (p.Value._event[0]._event != hdmv_event_e.HDMV_EVENT_NONE) {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_save_state() failed: unprocessed events");
                return -1;
            }

            if (p.Value.playing_object) {
                s[0] = (UInt32)(p.Value.playing_object - p.Value.movie_objects.Value.objects);
                s[1] = p.Value.playing_pc;
            } else {
                s[0] = uint.MaxValue;
            }

            if (p.Value.suspended_object)
            {
                s[2] = (UInt32)(p.Value.suspended_object - p.Value.movie_objects.Value.objects);
                s[3] = p.Value.suspended_pc;
            }
            else
            {
                s[2] = uint.MaxValue;
            }

            /* nv timer ? */

            return 0;
        }

        static int _restore_state(Ref<HDMV_VM> p, Ref<UInt32> s)
        {
            if (s[0] == uint.MaxValue)
            {
                p.Value.playing_object = Ref<MOBJ_OBJECT>.Null;
            }
            else if (s[0] >= p.Value.movie_objects.Value.num_objects)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_restore_state() failed: invalid playing object index");
                return -1;
            }
            else
            {
                p.Value.playing_object = p.Value.movie_objects.Value.objects.AtIndex(s[0]);
            }
            p.Value.playing_pc = s[1];

            if (s[2] == uint.MaxValue)
            {
                p.Value.suspended_object = Ref<MOBJ_OBJECT>.Null;
            }
            else if (s[2] >= p.Value.movie_objects.Value.num_objects)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_restore_state() failed: invalid suspended object index");
                return -1;
            }
            else
            {
                p.Value.suspended_object = p.Value.movie_objects.Value.objects.AtIndex(s[2]);
            }
            p.Value.suspended_pc = s[3];

            p.Value._object = Ref<MOBJ_OBJECT>.Null;
            p.Value.ig_object = Ref<MOBJ_OBJECT>.Null;

            Array.Fill(p.Value._event, new HDMV_EVENT());

            return 0;
        }

        internal static int hdmv_vm_save_state(Ref<HDMV_VM> p, Ref<UInt32> s)
        {
            int result;
            p.Value.mutex.bd_mutex_lock();
            result = _save_state(p, s);
            p.Value.mutex.bd_mutex_unlock();
            return result;
        }

        internal static void hdmv_vm_restore_state(Ref<HDMV_VM> p, Ref<UInt32> s)
        {
            p.Value.mutex.bd_mutex_lock();
            _restore_state(p, s);
            p.Value.mutex.bd_mutex_unlock();
        }


        /*
         * registers: PSR and GPR access
         */

        const UInt32 PSR_FLAG = 0x80000000;

        static bool _is_valid_reg(bd_psr_idx reg)
        {
            if (((uint)reg & PSR_FLAG) != 0)
            {
                if (((uint)reg & ~0x8000007f) != 0)
                {
                    return false;
                }
            }
            else
            {
                if (((uint)reg & ~0x00000fff) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        static int _store_reg(Ref<HDMV_VM> p, bd_psr_idx reg, UInt32 val)
        {
            if (!_is_valid_reg(reg))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_store_reg(): invalid register 0x{reg:x}");
                return -1;
            }

            if (((uint)reg & PSR_FLAG) != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_store_reg(): storing to PSR is not allowed");
                return -1;
            }
            else
            {
                return Register.bd_gpr_write(p.Value.regs, reg, val);
            }
        }

        static UInt32 _read_reg(Ref<HDMV_VM> p, bd_psr_idx reg)
        {
            if (!_is_valid_reg(reg))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_read_reg(): invalid register 0x{reg:x}");
                return 0;
            }

            if (((uint)reg & PSR_FLAG) != 0)
            {
                return Register.bd_psr_read(p.Value.regs, (bd_psr_idx)((uint)reg & 0x7f));
            }
            else
            {
                return Register.bd_gpr_read(p.Value.regs, reg);
            }
        }

        static UInt32 _read_setstream_regs(Ref<HDMV_VM> p, UInt32 val)
        {
            UInt32 flags = val & 0xf000f000;
            UInt32 reg0 = val & 0xfff;
            UInt32 reg1 = (val >> 16) & 0xfff;

            UInt32 val0 = Register.bd_gpr_read(p.Value.regs, (bd_psr_idx)reg0) & 0x0fff;
            UInt32 val1 = Register.bd_gpr_read(p.Value.regs, (bd_psr_idx)reg1) & 0x0fff;

            return flags | val0 | (val1 << 16);
        }

        static UInt32 _read_setbuttonpage_reg(Ref<HDMV_VM> p, UInt32 val)
        {
            UInt32 flags = val & 0xc0000000;
            UInt32 reg0 = val & 0x00000fff;

            UInt32 val0 = Register.bd_gpr_read(p.Value.regs, (bd_psr_idx)reg0) & 0x3fffffff;

            return flags | val0;
        }

        static int _store_result(Ref<HDMV_VM> p, Ref<MOBJ_CMD> cmd, UInt32 src, UInt32 dst, UInt32 src0, UInt32 dst0)
        {
            int ret = 0;

            /* store result to destination register(s) */
            if (dst != dst0)
            {
                if (cmd.Value.insn.Value.imm_op1 != 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"storing to imm !");
                    return -1;
                }
                ret = _store_reg(p, (bd_psr_idx)cmd.Value.dst, dst);
            }

            if (src != src0)
            {
                if (cmd.Value.insn.Value.imm_op1 != 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"storing to imm !");
                    return -1;
                }
                ret += _store_reg(p, (bd_psr_idx)cmd.Value.src, src);
            }

            return ret;
        }

        static UInt32 _fetch_operand(Ref<HDMV_VM> p, int setstream, int setbuttonpage, int imm, UInt32 value)
        {
            if (imm != 0)
            {
                return value;
            }

            if (setstream != 0)
            {
                return _read_setstream_regs(p, value);

            }
            else if (setbuttonpage != 0)
            {
                return _read_setbuttonpage_reg(p, value);

            }
            else
            {
                return _read_reg(p, (bd_psr_idx)value);
            }
        }

        static void _fetch_operands(Ref<HDMV_VM> p, Ref<MOBJ_CMD> cmd, Ref<UInt32> dst, Ref<UInt32> src)
        {
            Ref<HDMV_INSN> insn = cmd.Value.insn.Ref;

            bool setstream = (insn.Value.grp == (uint)hdmv_insn_grp.INSN_GROUP_SET &&
                             insn.Value.sub_grp == (uint)hdmv_insn_grp_set.SET_SETSYSTEM &&
                             (insn.Value.set_opt == (uint)hdmv_insn_setsystem.INSN_SET_STREAM ||
                                insn.Value.set_opt == (uint)hdmv_insn_setsystem.INSN_SET_SEC_STREAM));
            bool setbuttonpage = (insn.Value.grp == (uint)hdmv_insn_grp.INSN_GROUP_SET &&
                                 insn.Value.sub_grp == (uint)hdmv_insn_grp_set.SET_SETSYSTEM &&
                                 insn.Value.set_opt == (uint)hdmv_insn_setsystem.INSN_SET_BUTTON_PAGE);

            dst.Value = src.Value = 0;

            if (insn.Value.op_cnt > 0)
            {
                dst.Value = _fetch_operand(p, setstream ? 1 : 0, setbuttonpage ? 1 : 0, (int)insn.Value.imm_op1, cmd.Value.dst);
            }

            if (insn.Value.op_cnt > 1)
            {
                src.Value = _fetch_operand(p, setstream ? 1 : 0, setbuttonpage ? 1 : 0, (int)insn.Value.imm_op2, cmd.Value.src);
            }
        }

        /*
         * event queue
         */

        internal static string hdmv_event_str(hdmv_event_e _event)
        {
            return _event.ToString();
        }

        static int _get_event(Ref<HDMV_VM> p, Ref<HDMV_EVENT> ev)
        {
            if (p.Value._event[0]._event != hdmv_event_e.HDMV_EVENT_NONE) {
                ev.Value = p.Value._event[0];
                Array.Copy(p.Value._event, 1, p.Value._event, 0, p.Value._event.Length - 1);
                return 0;
            }

            ev.Value._event = hdmv_event_e.HDMV_EVENT_NONE;

            return -1;
        }

        static int _queue_event2(Ref<HDMV_VM> p, hdmv_event_e _event, UInt32 param, UInt32 param2)
        {
            uint i;
            for (i = 0; i < p.Value._event.Length - 1; i++) {
                if (p.Value._event[i]._event == hdmv_event_e.HDMV_EVENT_NONE) {
                    p.Value._event[i]._event = _event;
                    p.Value._event[i]._param = param;
                    p.Value._event[i]._param2 = param2;
                    return 0;
                }
            }

            Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_queue_event({_event}:{hdmv_event_str(_event)}, {param} {param2}): queue overflow !");
            return -1;
        }

        static int _queue_event(Ref<HDMV_VM> p, hdmv_event_e _event, UInt32 param)
        {
            return _queue_event2(p, _event, param, 0);
        }

        /*
         * vm init
         */

        internal static Ref<HDMV_VM> hdmv_vm_init(BD_DISC disc, Ref<BD_REGISTERS> regs,
                              uint num_titles, uint first_play_available, uint top_menu_available)
        {
            Ref<HDMV_VM> p = Ref<HDMV_VM>.Allocate();

            if (!p)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_CRIT, $"out of memory");
                return Ref<HDMV_VM>.Null;
            }

            /* read movie objects */
            p.Value.movie_objects = MobjParse.mobj_get(disc);
            if (!p.Value.movie_objects)
            {
                p.Free();
                return Ref<HDMV_VM>.Null;
            }

            p.Value.regs = regs;
            p.Value.num_titles = (ushort)num_titles;
            p.Value.have_top_menu = (byte)top_menu_available;
            p.Value.have_first_play = (byte)first_play_available;
            p.Value.rand = (ulong)DateTime.Now.Ticks;

            p.Value.mutex = new();

            return p;
        }

        static void _free_ig_object(Ref<HDMV_VM> p)
        {
            if (p.Value.ig_object)
            {
                p.Value.ig_object.Value.cmds.Free();
                p.Value.ig_object.Free();
            }
        }

        internal static void hdmv_vm_free(ref Ref<HDMV_VM> p)
        {
            if (p)
            {

                p.Value.mutex.bd_mutex_destroy();

                MobjParse.mobj_free(ref p.Value.movie_objects);

                _free_ig_object(p);

                p.Free();
            }
        }

        /*
         * suspend/resume ("function call")
         */

        static int _suspended_at_play_pl(Ref<HDMV_VM> p)
        {
            int play_pl = 0;
            if (p && p.Value.suspended_object)
            {
                Ref<MOBJ_CMD> cmd = p.Value.suspended_object.Value.cmds.AtIndex(p.Value.suspended_pc);
                Ref<HDMV_INSN> insn = cmd.Value.insn.Ref;
                play_pl = (insn.Value.grp == (uint)hdmv_insn_grp.INSN_GROUP_BRANCH &&
                           insn.Value.sub_grp == (uint)hdmv_insn_grp_branch.BRANCH_PLAY &&
                           (insn.Value.branch_opt == (uint)hdmv_insn_play.INSN_PLAY_PL ||
                              insn.Value.branch_opt == (uint)hdmv_insn_play.INSN_PLAY_PL_PI ||
                              insn.Value.branch_opt == (uint)hdmv_insn_play.INSN_PLAY_PL_PM)) ? 1 : 0;
            }

            return play_pl;
        }

        static int _suspend_for_play_pl(Ref<HDMV_VM> p)
        {
            if (p.Value.playing_object)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_suspend_for_play_pl(): object already playing playlist !");
                return -1;
            }

            p.Value.playing_object = p.Value._object;
            p.Value.playing_pc = p.Value.pc;

            p.Value._object = Ref<MOBJ_OBJECT>.Null;

            return 0;
        }

        static int _resume_from_play_pl(Ref<HDMV_VM> p)
        {
            if (!p.Value.playing_object)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_resume_from_play_pl(): object not playing playlist !");
                return -1;
            }

            p.Value._object = p.Value.playing_object;
            p.Value.pc = p.Value.playing_pc + 1;

            p.Value.playing_object = Ref<MOBJ_OBJECT>.Null;

            _free_ig_object(p);

            return 0;
        }

        static void _suspend_object(Ref<HDMV_VM> p, int psr_backup)
        {
            Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_suspend_object()");

            if (p.Value.suspended_object)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_suspend_object: object already suspended !");
                // [execute the call, discard old suspended object (10.2.4.2.2)].
            }

            if (psr_backup != 0)
            {
                Register.bd_psr_save_state(p.Value.regs);
            }

            if (p.Value.ig_object)
            {
                if (!p.Value.playing_object)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_suspend_object: IG object tries to suspend, no playing object !");
                    return;
                }
                p.Value.suspended_object = p.Value.playing_object;
                p.Value.suspended_pc = p.Value.playing_pc;

                p.Value.playing_object = Ref<MOBJ_OBJECT>.Null;

            }
            else
            {

                if (p.Value.playing_object)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_suspend_object: Movie object tries to suspend, also playing object present !");
                    return;
                }

                p.Value.suspended_object = p.Value._object;
                p.Value.suspended_pc = p.Value.pc;

            }

            p.Value._object = Ref<MOBJ_OBJECT>.Null;

            _free_ig_object(p);
        }

        static int _resume_object(Ref<HDMV_VM> p, int psr_restore)
        {
            if (!p.Value.suspended_object)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_resume_object: no suspended object!");
                return -1;
            }

            p.Value._object = Ref<MOBJ_OBJECT>.Null;
            p.Value.playing_object = Ref<MOBJ_OBJECT>.Null;
            _free_ig_object(p);

            if (psr_restore != 0)
            {
                /* check if suspended in play_pl */
                if (_suspended_at_play_pl(p) != 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"resuming playlist playback");
                    p.Value.playing_object = p.Value.suspended_object;
                    p.Value.playing_pc = p.Value.suspended_pc;
                    p.Value.suspended_object = Ref<MOBJ_OBJECT>.Null;
                    Register.bd_psr_restore_state(p.Value.regs);

                    return 0;
                }
                Register.bd_psr_restore_state(p.Value.regs);
            }

            p.Value._object = p.Value.suspended_object;
            p.Value.pc = p.Value.suspended_pc + 1;

            p.Value.suspended_object = Ref<MOBJ_OBJECT>.Null;

            Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"resuming object {(long)(p.Value._object - p.Value.movie_objects.Value.objects)} at {p.Value.pc}");

            _queue_event(p, hdmv_event_e.HDMV_EVENT_PLAY_STOP, 0);

            return 0;
        }


        /*
         * branching
         */

        static int _is_valid_title(Ref<HDMV_VM> p, UInt32 title)
        {
            if (title == 0)
            {
                return p.Value.have_top_menu;
            }
            if (title == 0xffff)
            {
                return p.Value.have_first_play;
            }

            return (title > 0 && title <= p.Value.num_titles) ? 1 : 0;
        }

        static int _jump_object(Ref<HDMV_VM> p, UInt32 _object)
        {
            if (_object >= p.Value.movie_objects.Value.num_objects)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_jump_object(): invalid object {_object}");
                return -1;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_jump_object(): jumping to object {_object}");

            _queue_event(p, hdmv_event_e.HDMV_EVENT_PLAY_STOP, 0);

            _free_ig_object(p);

            p.Value.playing_object = Ref<MOBJ_OBJECT>.Null;

            p.Value.pc = 0;
            p.Value._object = p.Value.movie_objects.Value.objects.AtIndex(_object);

            /* suspended object is not discarded */

            return 0;
        }

        static int _jump_title(Ref<HDMV_VM> p, UInt32 title)
        {
            if (_is_valid_title(p, title) != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_jump_title({title})");

                /* discard suspended object */
                p.Value.suspended_object = Ref<MOBJ_OBJECT>.Null;
                p.Value.playing_object = Ref<MOBJ_OBJECT>.Null;
                Register.bd_psr_reset_backup_registers(p.Value.regs);

                _queue_event(p, hdmv_event_e.HDMV_EVENT_TITLE, title);
                return 0;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_jump_title({title}): invalid title number");

            return -1;
        }

        static int _call_object(Ref<HDMV_VM> p, UInt32 _object)
        {
            if (_object >= p.Value.movie_objects.Value.num_objects)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_call_object(): invalid object {_object}");
                return -1;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_call_object({_object})");

            _suspend_object(p, 1);

            return _jump_object(p, _object);
        }

        static int _call_title(Ref<HDMV_VM> p, UInt32 title)
        {
            if (_is_valid_title(p, title) != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_call_title({title})");

                _suspend_object(p, 1);

                _queue_event(p, hdmv_event_e.HDMV_EVENT_TITLE, title);

                return 0;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_call_title({title}): invalid title number");

            return -1;
        }

        /*
         * playback control
         */

        static int _play_at(Ref<HDMV_VM> p, uint playlist, int playitem, int playmark)
        {
            if (p.Value.ig_object)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"play_at(list {playlist}, item {playitem}, mark {playmark}): playlist change not allowed in interactive composition");
                return -1;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"play_at(list {playlist}, item ,{playitem} mark {playmark})");

            if (playitem >= 0)
            {
                _queue_event2(p, hdmv_event_e.HDMV_EVENT_PLAY_PL_PI, playlist, (uint)playitem);
            }
            else if (playmark >= 0)
            {
                _queue_event2(p, hdmv_event_e.HDMV_EVENT_PLAY_PL_PM, playlist, (uint)playmark);
            }
            else
            {
                _queue_event(p, hdmv_event_e.HDMV_EVENT_PLAY_PL, playlist);
            }

            _suspend_for_play_pl(p);

            return 0;
        }

        static int _link_at(Ref<HDMV_VM> p, int playitem, int playmark)
        {
            if (!p.Value.ig_object)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"link_at(item {playitem}, mark {playmark}): link commands not allowed in movie objects");
                return -1;
            }

            if (playitem >= 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"link_at(playitem {playitem})");
                _queue_event(p, hdmv_event_e.HDMV_EVENT_PLAY_PI, (uint)playitem);
            }
            else if (playmark >= 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"link_at(mark {playmark})");
                _queue_event(p, hdmv_event_e.HDMV_EVENT_PLAY_PM, (uint)playmark);
            }

            return 0;
        }

        static int _play_stop(Ref<HDMV_VM> p)
        {
            if (!p.Value.ig_object)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_play_stop() not allowed in movie object");
                return -1;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_play_stop()");
            _queue_event(p, hdmv_event_e.HDMV_EVENT_PLAY_STOP, 1);

            /* terminate IG object. Continue executing movie object.  */
            if (_resume_from_play_pl(p) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_play_stop(): resuming movie object failed !");
                return -1;
            }

            return 0;
        }

        /*
         * SET/SYSTEM setstream
         */

        static void _set_stream(Ref<HDMV_VM> p, UInt32 dst, UInt32 src)
        {
            Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_set_stream(0x{dst:x}, 0x{src:x})");

            /* primary audio stream */
            if ((dst & 0x80000000) != 0)
            {
                Register.bd_psr_write(p.Value.regs, bd_psr_idx.PSR_PRIMARY_AUDIO_ID, (dst >> 16) & 0xfff);
            }

            /* IG stream */
            if ((src & 0x80000000) != 0)
            {
                Register.bd_psr_write(p.Value.regs, bd_psr_idx.PSR_IG_STREAM_ID, (src >> 16) & 0xff);
            }

            /* angle number */
            if ((src & 0x8000) != 0)
            {
                Register.bd_psr_write(p.Value.regs, bd_psr_idx.PSR_ANGLE_NUMBER, src & 0xff);
            }

            /* PSR2 */

            Register.bd_psr_lock(p.Value.regs);

            UInt32 psr2 = Register.bd_psr_read(p.Value.regs, bd_psr_idx.PSR_PG_STREAM);

            /* PG TextST stream number */
            if ((dst & 0x8000) != 0)
            {
                UInt32 text_st_num = dst & 0xfff;
                psr2 = text_st_num | (psr2 & 0xfffff000);
            }

            /* Update PG TextST stream display flag */
            UInt32 disp_s_flag = (dst & 0x4000) << 17;
            psr2 = disp_s_flag | (psr2 & 0x7fffffff);

            Register.bd_psr_write(p.Value.regs, bd_psr_idx.PSR_PG_STREAM, psr2);

            Register.bd_psr_unlock(p.Value.regs);
        }

        static void _set_sec_stream(Ref<HDMV_VM> p, UInt32 dst, UInt32 src)
        {
            Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_set_sec_stream(0x{dst:x}, 0x{src:x})");

            UInt32 disp_v_flag = (dst >> 30) & 1;
            UInt32 disp_a_flag = (src >> 30) & 1;
            UInt32 text_st_flags = (src >> 13) & 3;

            /* PSR14 */

            Register.bd_psr_lock(p.Value.regs);

            UInt32 psr14 = Register.bd_psr_read(p.Value.regs, bd_psr_idx.PSR_SECONDARY_AUDIO_VIDEO);

            /* secondary video */
            if ((dst & 0x80000000) != 0)
            {
                UInt32 sec_video = dst & 0xff;
                psr14 = (sec_video << 8) | (psr14 & 0xffff00ff);
            }

            /* secondary video size */
            if ((dst & 0x00800000) != 0)
            {
                UInt32 video_size = (dst >> 16) & 0xf;
                psr14 = (video_size << 24) | (psr14 & 0xf0ffffff);
            }

            /* secondary audio */
            if ((src & 0x80000000) != 0)
            {
                UInt32 sec_audio = (src >> 16) & 0xff;
                psr14 = sec_audio | (psr14 & 0xffffff00);
            }

            psr14 = (disp_v_flag << 31) | (psr14 & 0x7fffffff);
            psr14 = (disp_a_flag << 30) | (psr14 & 0xbfffffff);

            Register.bd_psr_write(p.Value.regs, bd_psr_idx.PSR_SECONDARY_AUDIO_VIDEO, psr14);

            /* PSR2 */

            UInt32 psr2 = Register.bd_psr_read(p.Value.regs, bd_psr_idx.PSR_PG_STREAM);

            /* PiP PG TextST stream */
            if ((src & 0x8000) != 0)
            {
                UInt32 stream = src & 0xfff;
                psr2 = (stream << 16) | (psr2 & 0xf000ffff);
            }

            psr2 = (text_st_flags << 30) | (psr2 & 0x3fffffff);

            Register.bd_psr_write(p.Value.regs, bd_psr_idx.PSR_PG_STREAM, psr2);

            Register.bd_psr_unlock(p.Value.regs);
        }

        static void _set_stream_ss(Ref<HDMV_VM> p, UInt32 dst, UInt32 src)
        {
            Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_set_stream_ss(0x{dst:x}, 0x{src:x})");

            if ((Register.bd_psr_read(p.Value.regs, bd_psr_idx.PSR_3D_STATUS) & 1) == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_set_stream_ss ignored (PSR22 indicates 2D mode)");
                return;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_set_stream_ss(0x{dst:x}, 0x{src:x}) unimplemented");
        }

        static void _setsystem_0x10(Ref<HDMV_VM> p, UInt32 dst, UInt32 src)
        {
            Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_set_psr103(0x{dst:x}, 0x{src:x})");

            Register.bd_psr_lock(p.Value.regs);

            /* just a guess ... */
            //bd_psr_write(p.Value.regs, 104, 0);
            Register.bd_psr_write(p.Value.regs, (bd_psr_idx)103, dst);

            Register.bd_psr_unlock(p.Value.regs);
        }

        /*
         * SET/SYSTEM navigation control
         */

        static void _set_button_page(Ref<HDMV_VM> p, UInt32 dst, UInt32 src)
        {
            if (p.Value.ig_object)
            {
                UInt32 param;
                param = (src & 0xc0000000) |        /* page and effects flags */
                        ((dst & 0x80000000) >> 2) |  /* button flag */
                        ((src & 0x000000ff) << 16) | /* page id */
                         (dst & 0x0000ffff);         /* button id */

                _queue_event(p, hdmv_event_e.HDMV_EVENT_SET_BUTTON_PAGE, param);

                /* terminate */
                p.Value.pc = 1 << 17;

                return;
            }

            /* selected button */
            if ((dst & 0x80000000) != 0)
            {
                Register.bd_psr_write(p.Value.regs, bd_psr_idx.PSR_SELECTED_BUTTON_ID, dst & 0xffff);
            }

            /* active page */
            if ((src & 0x80000000) != 0)
            {
                Register.bd_psr_write(p.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID, src & 0xff);
            }
        }

        static void _enable_button(Ref<HDMV_VM> p, UInt32 dst, int enable)
        {
            /* not valid in movie objects */
            if (p.Value.ig_object)
            {
                if (enable != 0)
                {
                    _queue_event(p, hdmv_event_e.HDMV_EVENT_ENABLE_BUTTON, dst);
                }
                else
                {
                    _queue_event(p, hdmv_event_e.HDMV_EVENT_DISABLE_BUTTON, dst);
                }
            }
        }

        static void _set_still_mode(Ref<HDMV_VM> p, int enable)
        {
            /* not valid in movie objects */
            if (p.Value.ig_object)
            {
                _queue_event(p, hdmv_event_e.HDMV_EVENT_STILL, (uint)enable);
            }
        }

        static void _popup_off(Ref<HDMV_VM> p)
        {
            /* not valid in movie objects */
            if (p.Value.ig_object)
            {
                _queue_event(p, hdmv_event_e.HDMV_EVENT_POPUP_OFF, 1);
            }
        }

        /*
         * SET/SYSTEM 3D mode
         */

        static void _set_output_mode(Ref<HDMV_VM> p, UInt32 dst)
        {
            if ((Register.bd_psr_read(p.Value.regs, bd_psr_idx.PSR_PROFILE_VERSION) & 0x130240) != 0x130240)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"_set_output_mode ignored (not running as profile 5 player)");
                return;
            }

            Register.bd_psr_lock(p.Value.regs);

            UInt32 psr22 = Register.bd_psr_read(p.Value.regs, bd_psr_idx.PSR_3D_STATUS);

            /* update output mode (bit 0). PSR22 bits 1 and 2 are subtitle alignment (_set_stream_ss()) */
            if ((dst & 1) != 0)
            {
                psr22 |= 1;
            }
            else
            {
                psr22 &= ~1u;
            }

            Register.bd_psr_write(p.Value.regs, bd_psr_idx.PSR_3D_STATUS, psr22);

            Register.bd_psr_unlock(p.Value.regs);
        }

        /*
         * navigation timer
         */

        static void _set_nv_timer(Ref<HDMV_VM> p, UInt32 dst, UInt32 src)
        {
            UInt32 mobj_id = dst & 0xffff;
            UInt32 timeout = src & 0xffff;

            if (timeout == 0)
            {
                /* cancel timer */
                p.Value.nv_timer.time = new DateTime();

                Register.bd_psr_write(p.Value.regs, bd_psr_idx.PSR_NAV_TIMER, 0);

                return;
            }

            /* validate params */
            if (mobj_id >= p.Value.movie_objects.Value.num_objects)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_set_nv_timer(): invalid object id ({mobj_id}) !");
                return;
            }
            if (timeout > 300)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_set_nv_timer(): invalid timeout ({timeout}) !");
                return;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"_set_nv_timer(): navigation timer not implemented !");

            /* set expiration time */
            p.Value.nv_timer.time = DateTime.Now;
            p.Value.nv_timer.time += new TimeSpan(0, 0, (int)timeout);

            p.Value.nv_timer.mobj_id = mobj_id;

            Register.bd_psr_write(p.Value.regs, bd_psr_idx.PSR_NAV_TIMER, timeout);
        }

        /* Unused function.
         * Commenting out to disable "‘_check_nv_timer’ defined but not used" warning
        static int _check_nv_timer(HDMV_VM *p)
        {
            if (p.Value.nv_timer.time > 0) {
                time_t now = time(NULL);

                if (now >= p.Value.nv_timer.time) {
                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"navigation timer expired, jumping to object %d", p.Value.nv_timer.mobj_id);

                    bd_psr_write(p.Value.regs, PSR_NAV_TIMER, 0);

                    p.Value.nv_timer.time = 0;
                    _jump_object(p, p.Value.nv_timer.mobj_id);

                    return 0;
                }

                bd_psr_write(p.Value.regs, PSR_NAV_TIMER, (p.Value.nv_timer.time - now));
            }

            return -1;
        }
        */

        /*
         * trace
         */

        static void _hdmv_trace_cmd(int pc, Ref<MOBJ_CMD> cmd)
        {
            if ((Logging.bd_get_debug_mask() & DebugMaskEnum.DBG_HDMV) != 0)
            {
                /*byte[] buf = new byte[384];, *dst = buf;

                dst += sprintf(dst, "%04d:  ", pc);

                //dst +=
                mobj_sprint_cmd(dst, cmd);

                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"%s", buf);*/
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, "TODO _hdmv_trace_cmd");
            }
        }

        static void _hdmv_trace_res(UInt32 new_src, UInt32 new_dst, UInt32 orig_src, UInt32 orig_dst)
        {
            if ((Logging.bd_get_debug_mask() & DebugMaskEnum.DBG_HDMV) != 0)
            {

                if (new_dst != orig_dst || new_src != orig_src)
                {
                    /*char buf[384], *dst = buf;

                    dst += sprintf(dst, "    :  [");
                    if (new_dst != orig_dst)
                    {
                        dst += sprintf(dst, " dst 0x%x <== 0x%x ", orig_dst, new_dst);
                    }
                    if (new_src != orig_src)
                    {
                        dst += sprintf(dst, " src 0x%x <== 0x%x ", orig_src, new_src);
                    }
                    //dst +=
                    sprintf(dst, "]");

                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"%s", buf);*/
                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"TODO _hdmv_trace_res");
                }
            }
        }

        /*
         * interpreter
         */

        /*
         * tools
         */

        static void SWAP_u32(ref uint a, ref uint b) { UInt32 tmp = a; a = b; b = tmp; }

        static UInt32 RAND_u32(Ref<HDMV_VM> p, UInt32 range)
        {
            unchecked
            {
                p.Value.rand = p.Value.rand * 6364136223846793005ul + 1ul;
            }
            if (range == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"RAND_u32: invalid range (0)");
                return 1;
            }

            unchecked {
                return ((UInt32)(p.Value.rand >> 32)) % range + 1;
            }
        }

        static UInt32 ADD_u32(UInt32 a, UInt32 b)
        {
            /* overflow .Value. saturate */
            UInt64 result = (UInt64)a + b;
            return result < 0xffffffff ? (UInt32)result : 0xffffffff;
        }

        static UInt32 MUL_u32(UInt32 a, UInt32 b)
        {
            /* overflow .Value. saturate */
            UInt64 result = (UInt64)a * b;
            return result < 0xffffffff ? (UInt32)result : 0xffffffff;
        }

        /*
         * _hdmv_step()
         *  - execute next instruction from current program
         */
        static int _hdmv_step(Ref<HDMV_VM> p)
        {
            Ref<MOBJ_CMD> cmd = p.Value._object.Value.cmds.AtIndex(p.Value.pc);
            Ref<HDMV_INSN> insn = cmd.Value.insn.Ref;
            Variable<UInt32> src = new(0);
            Variable<UInt32> dst = new(0);
            int inc_pc = 1;

            /* fetch operand values */
            _fetch_operands(p, cmd, dst.Ref, src.Ref);

            /* trace */
            _hdmv_trace_cmd((int)p.Value.pc, cmd);

            /* execute */
            switch ((hdmv_insn_grp)insn.Value.grp)
            {
                case hdmv_insn_grp.INSN_GROUP_BRANCH:
                    switch ((hdmv_insn_grp_branch)insn.Value.sub_grp)
                    {
                        case hdmv_insn_grp_branch.BRANCH_GOTO:
                            if (insn.Value.op_cnt > 1)
                            {
                                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"too many operands in BRANCH/GOTO opcode 0x{insn.Value.DebugInt:x8}");
                            }
                            switch ((hdmv_insn_goto)insn.Value.branch_opt)
                            {
                                case hdmv_insn_goto.INSN_NOP: break;
                                case hdmv_insn_goto.INSN_GOTO: p.Value.pc = dst.Value - 1; break;
                                case hdmv_insn_goto.INSN_BREAK: p.Value.pc = 1 << 17; break;
                                default:
                                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"unknown BRANCH/GOTO option {insn.Value.branch_opt} in opcode 0x{insn.Value.DebugInt:x8}");
                                    break;
                            }
                            break;
                        case hdmv_insn_grp_branch.BRANCH_JUMP:
                            if (insn.Value.op_cnt > 1)
                            {
                                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"too many operands in BRANCH/JUMP opcode 0x{insn.Value.DebugInt:x8}");
                            }
                            switch ((hdmv_insn_jump)insn.Value.branch_opt)
                            {
                                case hdmv_insn_jump.INSN_JUMP_TITLE: _jump_title(p, dst.Value); break;
                                case hdmv_insn_jump.INSN_CALL_TITLE: _call_title(p, dst.Value); break;
                                case hdmv_insn_jump.INSN_RESUME: _resume_object(p, 1); break;
                                case hdmv_insn_jump.INSN_JUMP_OBJECT: if (_jump_object(p, dst.Value) == 0) { inc_pc = 0; } break;
                                case hdmv_insn_jump.INSN_CALL_OBJECT: if (_call_object(p, dst.Value) == 0) { inc_pc = 0; } break;
                                default:
                                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"unknown BRANCH/JUMP option {insn.Value.branch_opt} in opcode 0x{insn.Value.DebugInt:x8}");
                                    break;
                            }
                            break;
                        case hdmv_insn_grp_branch.BRANCH_PLAY:
                            switch ((hdmv_insn_play)insn.Value.branch_opt)
                            {
                                case hdmv_insn_play.INSN_PLAY_PL: _play_at(p, dst.Value, -1, -1); break;
                                case hdmv_insn_play.INSN_PLAY_PL_PI: _play_at(p, dst.Value, (int)src.Value, -1); break;
                                case hdmv_insn_play.INSN_PLAY_PL_PM: _play_at(p, dst.Value, -1, (int)src.Value); break;
                                case hdmv_insn_play.INSN_LINK_PI: _link_at(p, (int)dst.Value, -1); break;
                                case hdmv_insn_play.INSN_LINK_MK: _link_at(p, -1, (int)dst.Value); break;
                                case hdmv_insn_play.INSN_TERMINATE_PL: if (_play_stop(p) == 0) { inc_pc = 0; } break;
                                default:
                                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"unknown BRANCH/PLAY option {insn.Value.branch_opt} in opcode 0x{insn.Value.DebugInt:x8}");
                                    break;
                            }
                            break;

                        default:
                            Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"unknown BRANCH subgroup {insn.Value.sub_grp} in opcode 0x{insn.Value.DebugInt:x8}");
                            break;
                    }
                    break; /* INSN_GROUP_BRANCH */

                case hdmv_insn_grp.INSN_GROUP_CMP:
                    if (insn.Value.op_cnt < 2)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"missing operand in BRANCH/JUMP opcode 0x{insn.Value.DebugInt:x8}");
                    }
                    switch ((hdmv_insn_cmp)insn.Value.cmp_opt)
                    {
                        case hdmv_insn_cmp.INSN_BC: p.Value.pc += (((dst.Value & ~src.Value) == 0) ? 0u : 1u); break;
                        case hdmv_insn_cmp.INSN_EQ: p.Value.pc += ((dst.Value == src.Value) ? 0u : 1u); break;
                        case hdmv_insn_cmp.INSN_NE: p.Value.pc += ((dst.Value != src.Value) ? 0u : 1u); break;
                        case hdmv_insn_cmp.INSN_GE: p.Value.pc += ((dst.Value >= src.Value) ? 0u : 1u); break;
                        case hdmv_insn_cmp.INSN_GT: p.Value.pc += ((dst.Value > src.Value) ? 0u : 1u); break;
                        case hdmv_insn_cmp.INSN_LE: p.Value.pc += ((dst.Value <= src.Value) ? 0u : 1u); break;
                        case hdmv_insn_cmp.INSN_LT: p.Value.pc += ((dst.Value < src.Value) ? 0u : 1u); break;
                        default:
                            Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"unknown COMPARE option {insn.Value.cmp_opt} in opcode 0x{insn.Value.DebugInt}");
                            break;
                    }
                    break; /* INSN_GROUP_CMP */

                case hdmv_insn_grp.INSN_GROUP_SET:
                    switch ((hdmv_insn_grp_set)insn.Value.sub_grp)
                    {
                        case hdmv_insn_grp_set.SET_SET:
                            {
                                UInt32 src0 = src.Value;
                                UInt32 dst0 = dst.Value;

                                if (insn.Value.op_cnt < 2)
                                {
                                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"missing operand in SET/SET opcode 0x{insn.Value.DebugInt:x8}");
                                }
                                switch ((hdmv_insn_set)insn.Value.set_opt)
                                {
                                    case hdmv_insn_set.INSN_MOVE: dst.Value = src.Value; break;
                                    case hdmv_insn_set.INSN_SWAP: SWAP_u32(ref src.Value, ref dst.Value); break;
                                    case hdmv_insn_set.INSN_SUB: dst.Value = (dst.Value > src.Value) ? (dst.Value - src.Value) : 0u; break;
                                    case hdmv_insn_set.INSN_DIV: dst.Value = (src.Value > 0) ? (dst.Value / src.Value) : 0xffffffff; break;
                                    case hdmv_insn_set.INSN_MOD: dst.Value = (src.Value > 0) ? (dst.Value % src.Value) : 0xffffffff; break;
                                    case hdmv_insn_set.INSN_ADD: dst.Value = ADD_u32(src.Value, dst.Value); break;
                                    case hdmv_insn_set.INSN_MUL: dst.Value = MUL_u32(dst.Value, src.Value); break;
                                    case hdmv_insn_set.INSN_RND: dst.Value = RAND_u32(p, src.Value); break;
                                    case hdmv_insn_set.INSN_AND: dst.Value &= src.Value; break;
                                    case hdmv_insn_set.INSN_OR: dst.Value |= src.Value; break;
                                    case hdmv_insn_set.INSN_XOR: dst.Value ^= src.Value; break;
                                    case hdmv_insn_set.INSN_BITSET: dst.Value |= (1u << (int)src.Value); break;
                                    case hdmv_insn_set.INSN_BITCLR: dst.Value &= ~(1u << (int)src.Value); break;
                                    case hdmv_insn_set.INSN_SHL: dst.Value <<= (int)src.Value; break;
                                    case hdmv_insn_set.INSN_SHR: dst.Value >>= (int)src.Value; break;
                                    default:
                                        Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"unknown SET option {insn.Value.set_opt} in opcode 0x{insn.Value.DebugInt:x8}");
                                        break;
                                }

                                /* store result(s) */
                                if (dst.Value != dst0 || src.Value != src0)
                                {

                                    _hdmv_trace_res(src.Value, dst.Value, src0, dst0);

                                    _store_result(p, cmd, src.Value, dst.Value, src0, dst0);
                                }
                                break;
                            }
                        case hdmv_insn_grp_set.SET_SETSYSTEM:
                            switch ((hdmv_insn_setsystem)insn.Value.set_opt)
                            {
                                case hdmv_insn_setsystem.INSN_SET_STREAM: _set_stream(p, dst.Value, src.Value); break;
                                case hdmv_insn_setsystem.INSN_SET_SEC_STREAM: _set_sec_stream(p, dst.Value, src.Value); break;
                                case hdmv_insn_setsystem.INSN_SET_NV_TIMER: _set_nv_timer(p, dst.Value, src.Value); break;
                                case hdmv_insn_setsystem.INSN_SET_BUTTON_PAGE: _set_button_page(p, dst.Value, src.Value); break;
                                case hdmv_insn_setsystem.INSN_ENABLE_BUTTON: _enable_button(p, dst.Value, 1); break;
                                case hdmv_insn_setsystem.INSN_DISABLE_BUTTON: _enable_button(p, dst.Value, 0); break;
                                case hdmv_insn_setsystem.INSN_POPUP_OFF: _popup_off(p); break;
                                case hdmv_insn_setsystem.INSN_STILL_ON: _set_still_mode(p, 1); break;
                                case hdmv_insn_setsystem.INSN_STILL_OFF: _set_still_mode(p, 0); break;
                                case hdmv_insn_setsystem.INSN_SET_OUTPUT_MODE: _set_output_mode(p, dst.Value); break;
                                case hdmv_insn_setsystem.INSN_SET_STREAM_SS: _set_stream_ss(p, dst.Value, src.Value); break;
                                case hdmv_insn_setsystem.INSN_SETSYSTEM_0x10: _setsystem_0x10(p, dst.Value, src.Value); break;
                                default:
                                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"unknown SETSYSTEM option {insn.Value.set_opt} in opcode 0x{insn.Value.DebugInt:x8}");
                                    break;
                            }
                            break;
                        default:
                            Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"unknown SET subgroup {insn.Value.sub_grp} in opcode 0x{insn.Value.DebugInt:x8}");
                            break;
                    }
                    break; /* INSN_GROUP_SET */

                default:
                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"unknown operation group {insn.Value.grp} in opcode 0x{insn.Value.DebugInt:x8}");
                    break;
            }

            /* inc program counter to next instruction */
            p.Value.pc += (uint)inc_pc;

            return 0;
        }

        /*
         * interface
         */

        internal static int hdmv_vm_select_object(Ref<HDMV_VM> p, UInt32 _object)
        {
            int result;

            if (!p)
            {
                return -1;
            }

            p.Value.mutex.bd_mutex_lock();

            result = _jump_object(p, _object);

            p.Value.mutex.bd_mutex_unlock();
            return result;
        }

        static int _set_object(Ref<HDMV_VM> p, int num_nav_cmds, Ref<MOBJ_CMD> nav_cmds)
        {
            Ref<MOBJ_OBJECT> ig_object = Ref<MOBJ_OBJECT>.Allocate();
            if (!ig_object)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_CRIT, $"out of memory");
                return -1;
            }

            ig_object.Value.num_cmds = (ushort)num_nav_cmds;
            ig_object.Value.cmds = Ref<MOBJ_CMD>.Allocate(num_nav_cmds);

            nav_cmds.AsSpan().Slice(0, num_nav_cmds).CopyTo(ig_object.Value.cmds.AsSpan());

            p.Value.pc = 0;
            p.Value.ig_object = ig_object;
            p.Value._object = ig_object;

            return 0;
        }

        internal static int hdmv_vm_set_object(Ref<HDMV_VM> p, int num_nav_cmds, Ref<MOBJ_CMD> nav_cmds)
        {
            int result = -1;

            if (!p)
            {
                return -1;
            }

            p.Value.mutex.bd_mutex_lock();

            p.Value._object = Ref<MOBJ_OBJECT>.Null;

            _free_ig_object(p);

            if (nav_cmds && num_nav_cmds > 0)
            {
                result = _set_object(p, num_nav_cmds, nav_cmds);
            }

            p.Value.mutex.bd_mutex_unlock();

            return result;
        }

        internal static int hdmv_vm_get_event(Ref<HDMV_VM> p, Ref<HDMV_EVENT> ev)
        {
            int result;
            p.Value.mutex.bd_mutex_lock();

            result = _get_event(p, ev);

            p.Value.mutex.bd_mutex_unlock();
            return result;
        }

        internal static int hdmv_vm_running(Ref<HDMV_VM> p)
        {
            int result;

            if (!p)
            {
                return 0;
            }

            p.Value.mutex.bd_mutex_lock();

            result = p.Value._object ? 0 : 1;

            p.Value.mutex.bd_mutex_unlock();
            return result;
        }

        internal static UInt32 hdmv_vm_get_uo_mask(Ref<HDMV_VM> p)
        {
            UInt32 mask = 0;
            Ref<MOBJ_OBJECT> o = Ref<MOBJ_OBJECT>.Null;

            if (!p)
            {
                return 0;
            }

            p.Value.mutex.bd_mutex_lock();

            if ((o = (p.Value._object && !p.Value.ig_object) ? p.Value._object : (p.Value.playing_object ? p.Value.playing_object : p.Value.suspended_object))) {
                mask |= o.Value.menu_call_mask;
                mask |= (uint)o.Value.title_search_mask << 1;
            }

            p.Value.mutex.bd_mutex_unlock();
            return mask;
        }

        internal static int hdmv_vm_resume(Ref<HDMV_VM> p)
        {
            int result;

            if (!p)
            {
                return -1;
            }

            p.Value.mutex.bd_mutex_lock();

            result = _resume_from_play_pl(p);

            p.Value.mutex.bd_mutex_unlock();
            return result;
        }

        internal static int hdmv_vm_suspend_pl(Ref<HDMV_VM> p)
        {
            int result = -1;

            if (!p)
            {
                return -1;
            }

            p.Value.mutex.bd_mutex_lock();

            if (p.Value._object || p.Value.ig_object) {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"hdmv_vm_suspend_pl(): HDMV VM is still running");

            } else if (!p.Value.playing_object)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"hdmv_vm_suspend_pl(): No playing object");

            }
            else if (p.Value.playing_object.Value.resume_intention_flag == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"hdmv_vm_suspend_pl(): no resume intention flag");

                p.Value.playing_object = Ref<MOBJ_OBJECT>.Null;
                result = 0;

            }
            else
            {
                p.Value.suspended_object = p.Value.playing_object;
                p.Value.suspended_pc = p.Value.playing_pc;

                p.Value.playing_object = Ref<MOBJ_OBJECT>.Null;

                Register.bd_psr_save_state(p.Value.regs);
                result = 0;
            }

            p.Value.mutex.bd_mutex_unlock();
            return result;
        }

        /* terminate program after MAX_LOOP instructions */
        const int MAX_LOOP = 1000000;

        static int _vm_run(Ref<HDMV_VM> p, Ref<HDMV_EVENT> ev)
        {
            int max_loop = MAX_LOOP;

            /* pending events ? */
            if (_get_event(p, ev) == 0)
            {
                return 0;
            }

            /* valid program ? */
            if (!p.Value._object) {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"hdmv_vm_run(): no object selected");
                return -1;
            }

            while (--max_loop > 0)
            {

                /* suspended ? */
                if (!p.Value._object) {
                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"hdmv_vm_run(): object suspended");
                    _get_event(p, ev);
                    return 0;
                }

                /* terminated ? */
                if (p.Value.pc >= p.Value._object.Value.num_cmds) {
                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV, $"terminated with PC={p.Value.pc}");
                    p.Value._object = Ref<MOBJ_OBJECT>.Null;
                    ev.Value._event = hdmv_event_e.HDMV_EVENT_END;

                    if (p.Value.ig_object)
                    {
                        ev.Value._event = hdmv_event_e.HDMV_EVENT_IG_END;
                        _free_ig_object(p);
                    }

                    return 0;
                }

                /* next instruction */
                if (_hdmv_step(p) < 0)
                {
                    p.Value._object = Ref<MOBJ_OBJECT>.Null;
                    return -1;
                }

                /* events ? */
                if (_get_event(p, ev) == 0)
                {
                    return 0;
                }
            }

            Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"hdmv_vm: infinite program ? terminated after {MAX_LOOP} instructions.");
            p.Value._object = Ref<MOBJ_OBJECT>.Null;
            return -1;
        }

        internal static int hdmv_vm_run(Ref<HDMV_VM> p, Ref<HDMV_EVENT> ev)
        {
            int result;

            if (!p)
            {
                return -1;
            }

            p.Value.mutex.bd_mutex_lock();

            result = _vm_run(p, ev);

            p.Value.mutex.bd_mutex_unlock();
            return result;
        }
    }
}
