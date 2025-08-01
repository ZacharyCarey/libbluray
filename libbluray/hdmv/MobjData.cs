using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.hdmv
{
    public struct HDMV_INSN
    {
        public byte sub_grp;//    : 3;  /* command sub-group */
        public byte op_cnt;//     : 3;  /* operand count */
        public byte grp;//        : 2;  /* command group */

        public byte branch_opt;// : 4;  /* branch option */
        public byte reserved1;//  : 2;
        public byte imm_op2;//    : 1;  /* I-flag for operand 2 */
        public byte imm_op1;//    : 1;  /* I-flag for operand 1 */

        public byte cmp_opt;//    : 4;  /* compare option */
        public byte reserved2;//  : 4;

        public byte set_opt;//    : 5;  /* set option */
        public byte reserved3;//  : 3;

        public uint DebugInt => ((uint)sub_grp << 29) | ((uint)op_cnt << 26) | ((uint)grp << 24) | ((uint)branch_opt << 20) | ((uint)reserved1 << 18) | ((uint)imm_op2 << 17) | ((uint)imm_op1 << 16) | ((uint)cmp_opt << 12) | ((uint)reserved2 << 8) | ((uint)set_opt << 3) | ((uint)reserved3);

        public HDMV_INSN() { }
    }

    public struct MOBJ_CMD
    {
        public Variable<HDMV_INSN> insn = new();
        public UInt32 dst;
        public UInt32 src;

        public MOBJ_CMD() { }
    }

    public struct MOBJ_OBJECT
    {
        public byte resume_intention_flag /*: 1*/;
        public byte menu_call_mask        /*: 1*/;
        public byte title_search_mask     /*: 1*/;

        public UInt16 num_cmds;
        public Ref<MOBJ_CMD> cmds = new();

        public MOBJ_OBJECT() { }
    }

    public struct MOBJ_OBJECTS
    {
        public Variable<UInt32> mobj_version = new();
        public UInt16 num_objects;
        public Ref<MOBJ_OBJECT> objects = new();

        public MOBJ_OBJECTS() { }
    }
}
