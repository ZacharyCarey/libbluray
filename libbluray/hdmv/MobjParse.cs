using libbluray.bdnav;
using libbluray.disc;
using libbluray.file;
using libbluray.hdmv;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.hdmv
{
    public static class MobjParse
    {
        const UInt32 MOBJ_SIG1 = ('M' << 24) | ('O' << 16) | ('B' << 8) | 'J';

        static bool _mobj_parse_header(Ref<BITSTREAM> bs, Ref<int> extension_data_start, Ref<UInt32> mobj_version)
        {
            if (!BdmvParse.bdmv_parse_header(bs, MOBJ_SIG1, mobj_version))
            {
                return false;
            }

            extension_data_start.Value = (int)bs.Value.bs_read<UInt32>(32);

            return true;
        }

        internal static void mobj_parse_cmd(Ref<byte> buf, Ref<MOBJ_CMD> cmd)
        {
            BITBUFFER bb = new();
            bb.bb_init(buf, 12);

            cmd.Value.insn.Value.op_cnt = bb.bb_read<byte>(3);
            cmd.Value.insn.Value.grp = bb.bb_read<byte>(2);
            cmd.Value.insn.Value.sub_grp = bb.bb_read<byte>(3);

            cmd.Value.insn.Value.imm_op1 = bb.bb_read<byte>(1);
            cmd.Value.insn.Value.imm_op2 = bb.bb_read<byte>(1);
            bb.bb_skip(2);    /* reserved */
            cmd.Value.insn.Value.branch_opt = bb.bb_read<byte>(4);

            bb.bb_skip(4);    /* reserved */
            cmd.Value.insn.Value.cmp_opt = bb.bb_read<byte>(4);

            bb.bb_skip(3);    /* reserved */
            cmd.Value.insn.Value.set_opt = bb.bb_read<byte>(5);

            cmd.Value.dst = bb.bb_read<UInt32>(32);
            cmd.Value.src = bb.bb_read<UInt32>(32);
        }

        static bool _mobj_parse_object(Ref<BITSTREAM> bs, Ref<MOBJ_OBJECT> obj)
        {
            int i;

            obj.Value.resume_intention_flag = bs.Value.bs_read<byte>(1);
            obj.Value.menu_call_mask = bs.Value.bs_read<byte>(1);
            obj.Value.title_search_mask = bs.Value.bs_read<byte>(1);

            bs.Value.bs_skip(13); /* padding */

            obj.Value.num_cmds = bs.Value.bs_read<UInt16>(16);
            if (obj.Value.num_cmds == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"MovieObject.bdmv: empty object");
                return true;
            }
            obj.Value.cmds = Ref<MOBJ_CMD>.Allocate(obj.Value.num_cmds);

            for (i = 0; i < obj.Value.num_cmds; i++)
            {
                byte[] buf = new byte[12];
                if (bs.Value.bs_avail() < 12 * 8)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_HDMV | DebugMaskEnum.DBG_CRIT, $"MovieObject.bdmv: unexpected EOF");
                    return false;
                }
                bs.Value.bs_read_bytes(buf, 12);
                mobj_parse_cmd(new Ref<byte>(buf), obj.Value.cmds.AtIndex(i));
            }

            return true;
        }

        internal static void mobj_free(ref Ref<MOBJ_OBJECTS> p)
        {
            if (p)
            {

                if (p.Value.objects)
                {
                    int i;
                    for (i = 0; i < p.Value.num_objects; i++)
                    {
                        p.Value.objects[i].cmds.Free();
                    }

                    p.Value.objects.Free();
                }

                p.Free();
            }
        }

        static Ref<MOBJ_OBJECTS> _mobj_parse(BD_FILE_H fp)
        {
            Variable<BITSTREAM> bs = new();
            Ref<MOBJ_OBJECTS> objects = Ref<MOBJ_OBJECTS>.Null;
            UInt16 num_objects;
            UInt32 data_len;
            Variable<int> extension_data_start = new();
            int i;

            if (bs.Value.bs_init(fp) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, $"MovieObject.bdmv: read error");
                goto error;
            }

            objects = Ref<MOBJ_OBJECTS>.Allocate();

            if (!_mobj_parse_header(bs.Ref, extension_data_start.Ref, objects.Value.mobj_version.Ref))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"MovieObject.bdmv: invalid header");
                goto error;
            }

            if (extension_data_start.Value != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"MovieObject.bdmv: unknown extension data at {extension_data_start}");
            }

            if (bs.Value.bs_seek_byte(40) < 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV, $"MovieObject.bdmv: read error");
                goto error;
            }

            data_len = bs.Value.bs_read<UInt32>(32);

            if ((bs.Value.bs_end() - bs.Value.bs_pos()) / 8 < (Int64)data_len)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"MovieObject.bdmv: invalid data_len {data_len} !");
                goto error;
            }

            bs.Value.bs_skip(32); /* reserved */
            num_objects = bs.Value.bs_read<UInt16>(16);

            objects.Value.num_objects = num_objects;
            objects.Value.objects = Ref<MOBJ_OBJECT>.Allocate(num_objects);

            for (i = 0; i < objects.Value.num_objects; i++)
            {
                if (!_mobj_parse_object(bs.Ref, objects.Value.objects.AtIndex(i)))
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"MovieObject.bdmv: error parsing object {i}");
                    goto error;
                }
            }

            return objects;

        error:
            mobj_free(ref objects);
            return Ref<MOBJ_OBJECTS>.Null;
        }

        internal static Ref<MOBJ_OBJECTS> mobj_parse(string file_name)
        {
            BD_FILE_H? fp;
            Ref<MOBJ_OBJECTS> objects;

            fp = BD_FILE_H.file_open(file_name, true);
            if (fp == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_NAV | DebugMaskEnum.DBG_CRIT, $"error opening {file_name}");
                return Ref<MOBJ_OBJECTS>.Null;
            }

            objects = _mobj_parse(fp);
            fp.file_close();
            return objects;
        }

        static Ref<MOBJ_OBJECTS> _mobj_get(BD_DISC disc, string path)
        {
            BD_FILE_H? fp;
            Ref<MOBJ_OBJECTS> objects;

            fp = disc.disc_open_path(path);
            if (fp == null)
            {
                return Ref<MOBJ_OBJECTS>.Null;
            }

            objects = _mobj_parse(fp);
            fp.file_close();
            return objects;
        }

        internal static Ref<MOBJ_OBJECTS> mobj_get(BD_DISC disc)
        {
            Ref<MOBJ_OBJECTS> objects;

            objects = _mobj_get(disc, Path.Combine("BDMV", "MovieObject.bdmv"));
            if (objects)
            {
                return objects;
            }

            /* if failed, try backup file */
            objects = _mobj_get(disc, Path.Combine("BDMV", "BACKUP", "MovieObject.bdmv"));
            return objects;
        }

    }
}
