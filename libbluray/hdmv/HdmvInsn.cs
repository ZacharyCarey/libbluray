﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.hdmv
{
    /// <summary>
    /// instruction groups
    /// </summary>
    public enum hdmv_insn_grp
    {
        INSN_GROUP_BRANCH = 0,
        INSN_GROUP_CMP = 1,
        INSN_GROUP_SET = 2,
    }

    /// <summary>
    /// BRANCH sub-groups
    /// </summary>
    public enum hdmv_insn_grp_branch
    {
        BRANCH_GOTO = 0x00,
        BRANCH_JUMP = 0x01,
        BRANCH_PLAY = 0x02,
    }

    /// <summary>
    /// GOTO sub-group
    /// </summary>
    public enum hdmv_insn_goto
    {
        INSN_NOP = 0x00,
        INSN_GOTO = 0x01,
        INSN_BREAK = 0x02,
    }

    /// <summary>
    /// JUMP sub-group
    /// </summary>
    public enum hdmv_insn_jump
    {
        INSN_JUMP_OBJECT = 0x00,
        INSN_JUMP_TITLE = 0x01,
        INSN_CALL_OBJECT = 0x02,
        INSN_CALL_TITLE = 0x03,
        INSN_RESUME = 0x04,
    }

    /// <summary>
    /// PLAY sub-group
    /// </summary>
    public enum hdmv_insn_play
    {
        INSN_PLAY_PL = 0x00,
        INSN_PLAY_PL_PI = 0x01,
        INSN_PLAY_PL_PM = 0x02,
        INSN_TERMINATE_PL = 0x03,
        INSN_LINK_PI = 0x04,
        INSN_LINK_MK = 0x05,
    }

    /// <summary>
    /// COMPARE group
    /// </summary>
    public enum hdmv_insn_cmp
    {
        INSN_BC = 0x01,
        INSN_EQ = 0x02,
        INSN_NE = 0x03,
        INSN_GE = 0x04,
        INSN_GT = 0x05,
        INSN_LE = 0x06,
        INSN_LT = 0x07,
    }

    /// <summary>
    /// SET sub-groups
    /// </summary>
    public enum hdmv_insn_grp_set
    {
        SET_SET = 0x00,
        SET_SETSYSTEM = 0x01,
    }

    /// <summary>
    /// SET sub-group
    /// </summary>
    public enum hdmv_insn_set
    {
        INSN_MOVE = 0x01,
        INSN_SWAP = 0x02,
        INSN_ADD = 0x03,
        INSN_SUB = 0x04,
        INSN_MUL = 0x05,
        INSN_DIV = 0x06,
        INSN_MOD = 0x07,
        INSN_RND = 0x08,
        INSN_AND = 0x09,
        INSN_OR = 0x0a,
        INSN_XOR = 0x0b,
        INSN_BITSET = 0x0c,
        INSN_BITCLR = 0x0d,
        INSN_SHL = 0x0e,
        INSN_SHR = 0x0f,
    }

    /// <summary>
    /// SETSYSTEM sub-group
    /// </summary>
    public enum hdmv_insn_setsystem
    {
        INSN_SET_STREAM = 0x01,
        INSN_SET_NV_TIMER = 0x02,
        INSN_SET_BUTTON_PAGE = 0x03,
        INSN_ENABLE_BUTTON = 0x04,
        INSN_DISABLE_BUTTON = 0x05,
        INSN_SET_SEC_STREAM = 0x06,
        INSN_POPUP_OFF = 0x07,
        INSN_STILL_ON = 0x08,
        INSN_STILL_OFF = 0x09,
        INSN_SET_OUTPUT_MODE = 0x0a,
        INSN_SET_STREAM_SS = 0x0b,

        INSN_SETSYSTEM_0x10 = 0x10,
    }
}
