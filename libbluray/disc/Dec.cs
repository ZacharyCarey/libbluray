using libbluray.file;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.disc
{
    internal class dec_dev
    {
        public delegate BD_FILE_H file_openFp(object a, string b);

        internal object file_open_bdrom_handle;
        internal file_openFp pf_file_open_bdrom;
        internal object file_open_vfs_handle;
        internal file_openFp pf_file_open_vfs;
        internal string? root; // May be null if disc is not mounted
        internal string? device; // may be null if not reading from real device
    
        internal static int _bdrom_have_file(object p, string dir, string file)
        {
            dec_dev dev = (dec_dev)p;
            BD_FILE_H fp = null;
            string path = null;

            path = Path.Combine(dir, file);
            fp = dev.pf_file_open_bdrom(dev.file_open_bdrom_handle, path);

            if (fp != null)
            {
                Filesystem.file_close(fp);
                return 1;
            }

            return 0;
        }

        private static int _libaacs_init(BD_DEC dec, dec_dev dev, BD_ENC_INFO i, string keyfile_path)
        {
            int result;
            byte[] disc_id = null;

            //if (dec.aacs == null)
            //{
                return 0;
            //}

            /*result = libaacs_open(dec.aacs, dev.device, dev.file_open_vfs_handle, (object)dev.pf_file_open_vfs, keyfile_path);

            i.aacs_error_code = result;
            i.aacs_handled = !result;
            i.aacs_mkbv = libaacs_get_mkbv(dec.aacs);
            disc_id = libaacs_get_aacs_data(dec.aacs, BD_AACS_DISC_ID);
            if (disc_id != null)
            {
                Array.Copy(disc_id, i.disc_id, 20);
            }

            if (result != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"aacs_open() failed: {result}!");
                libaacs_unload(dec.aacs);
                return 0;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, "Opened libaacs");
            return 1;*/
        }

        /*private static int _libbdplus_init(BD_DEC dec, dec_dev dev, BD_ENC_INFO i, object regs, object psr_read, object psr_write)
        {
            if (dec.bdplus == null)
            {
                return 0;
            }

            byte[] vid = libaacs_get_aacs_data(dec.aacs, BD_AACS_MEDIA_VID);
            byte[] mk = libaacs_get_aacs_data(dec.aacs, BD_AACS_MEDIA_KEY);
            if (vid == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, "BD+ initialization failed (no AACS ?)");
                libbdplus_unload(dec.bdplus);
                return 0;
            }

            if (libbdplus_init(dec.bdplus, dev.root, dev.device, dev.file_open_bdrom_handle, (object)dev.pf_file_open_bdrom, vid, mk))
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, "bdplus_init() failed");

                i->bdplus_handled = 0;
                libbdplus_unload(dec.bdplus);
                return 0;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, "libbdplus initialized");

            // map player memory regions
            libbdplus_mmap(dec.bdplus, 0, regs);
            libbdplus_mmap(dec.bdplus, 1, (void*)((byte[])regs + sizeof(uint32_t) * 128));

            // connect registers 
            libbdplus_psr(dec.bdplus, regs, psr_read, psr_write);

            i.bdplus_gen = libbdplus_get_gen(dec.bdplus);
            i.bdplus_date = libbdplus_get_date(dec.bdplus);
            i.bdplus_handled = 1;

            if (i.bdplus_date == 0)
            {
                // libmmbd -> no menu support
                //BD_DEBUG(DBG_BLURAY | DBG_CRIT, "WARNING: using libmmbd for BD+. On-disc menus won't work with all discs.\n");
                //i->no_menu_support = 1;
            }

            return 1;
        }*/

        internal static int _dec_detect(dec_dev dev, BD_ENC_INFO i)
        {
            /* Check for AACS */
            i.aacs_detected = 0; //libaacs_required((object)dev, _bdrom_have_file);
            /*if (!i->aacs_detected)
            {
                // No AACS (=> no BD+) 
                return 0;
            }*/

            /* check for BD+ */
            i.bdplus_detected = 0; // libbdplus_required((object)dev, _bdrom_have_file);
            return 1;
        }

        internal static void _dec_load(BD_DEC dec, BD_ENC_INFO i)
        {
            int force_mmbd_aacs = 0;

            /*if (i->bdplus_detected)
            {
                // load BD+ library and check BD+ library type. libmmbd doesn't work with libaacs 
                dec.bdplus = libbdplus_load();
                force_mmbd_aacs = dec.bdplus && libbdplus_is_mmbd(dec.bdplus);
            }*/

            // load AACS library 
            //dec.aacs = libaacs_load(force_mmbd_aacs);

            i.libaacs_detected = 0; //(dec.aacs != null) ? 1 : 0;
            i.libbdplus_detected = 0; //(dec.bdplus != null) ? 1 : 0;
        }
    }

    // Low-level stream decoding
    internal class BD_DEC
    {
        internal int use_menus;
        //internal BD_AACS aacs = null;
        //internal BD_BDPLUS bdplus = null;

        public static BD_DEC dec_init(dec_dev dev, out BD_ENC_INFO enc_info, string keyfile_path, object regs, object psr_read, object psr_write)
        {
            BD_DEC dec = null;

            enc_info = new();

            /* detect AACS/BD+ */
            if (dec_dev._dec_detect(dev, enc_info) == 0)
            {
                return null;
            }

            dec = new BD_DEC();
            if (dec == null)
            {
                return null;
            }

            /* load compatible libraries */
            dec_dev._dec_load(dec, enc_info);

            /* init decoding libraries */
            /* BD+ won't help unless AACS works ... */
            /*if (_libaacs_init(dec, dev, enc_info, keyfile_path) != 0)
            {
                // _libbdplus_init(dec, dev, enc_info, regs, psr_read, psr_write);
            }*/

            if (enc_info.aacs_handled == 0)
            {
                /* AACS failed, clean up */
                dec.dec_close();
            }

            /* BD+ failure may be non-fatal (not all titles in disc use BD+).
             * Keep working AACS decoder even if BD+ init failed
             */

            return dec;
        }

        public void dec_close()
        {
            //libaacs_unload(this.aacs);
            //libbdplus_unload(this.bdplus);
        }

        public byte[] dec_data(int type)
        {
            byte[] ret = null;

            if (type >= 0x1000)
            {
                /*if (this.bdplus != null)
                {
                    ret = libbdplus_get_data(dec.bdplus, type);
                }*/
            }
            else
            {
                /*if (this.aacs != null)
                {
                    ret = libaacs_get_aacs_data(dec.aacs, type);
                }*/
            }

            return ret;
        }

        public byte[] dec_disc_id()
        {
            return dec_data(/*BD_AACS_DISC_ID*/ 1);
        }

        public void dec_start(UInt32 num_titles)
        {
            if (num_titles == 0)
            {
                this.use_menus = 1;
                /*if (this.bdplus != null)
                {
                    libbdplus_start(this.bdplus);
                    libbdplus_event(this.bdplus, 0x110, 0xffff, 0);
                }*/
            }
            else
            {
                /*if (this.bdplus != null)
                {
                    libbdplus_start(this.bdplus);
                    libbdplus_event(this.bdplus, 0xffffffff, num_titles, 0);
                }*/
            }
        }

        public void dec_title(UInt32 title)
        {
            /*if (this.aacs != null)
            {
                libaacs_select_title(this.aacs, title);
            }*/
            /*if (this.bdplus != null)
            {
                libbdplus_event(this.bdplus, 0x110, title, 0);
            }*/
        }

        public void dec_application(UInt32 data)
        {
            /*if (this.bdplus != null)
            {
                libbdplus_event(this.bdplus, 0x210, data, 0);
            }*/
        }
    }

    internal class DEC_STREAM : BD_FILE_H
    {
        BD_FILE_H fp = null;
        //BD_AACS aacs = null;
        //BD_BDPLUS_ST bdplus = null;

        private DEC_STREAM() { }

        public override long read(Span<byte> buf, long size)
        {
            DEC_STREAM st = this;
            Int64 result;

            if (size != 6144)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_CRIT, "read size != unit size");
                return 0;
            }

            result = st.fp.read(buf, size);
            if (result <= 0)
            {
                return result;
            }

            /*if (st.aacs != null)
            {
                if (libaacs_decrypt_unit(st.aacs, buf))
                {
                    // failure is detected from TP header 
                }
            }*/

            /*if (st.bdplus != null)
            {
                if (libbdplus_fixup(st.bdplus, buf, (int)size) < 0)
                {
                    // there's no way to verify if the stream was decoded correctly 
                }
            }*/

            return result;
        }

        public override long read(Ref<byte> buf, long size)
        {
            DEC_STREAM st = this;
            Int64 result;

            if (size != 6144)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_CRIT, "read size != unit size");
                return 0;
            }

            result = st.fp.read(buf, size);
            if (result <= 0)
            {
                return result;
            }

            /*if (st.aacs != null)
            {
                if (libaacs_decrypt_unit(st.aacs, buf))
                {
                    // failure is detected from TP header 
                }
            }*/

            /*if (st.bdplus != null)
            {
                if (libbdplus_fixup(st.bdplus, buf, (int)size) < 0)
                {
                    // there's no way to verify if the stream was decoded correctly 
                }
            }*/

            return result;
        }

        public override long seek(long offset, SeekOrigin origin)
        {
            DEC_STREAM st = this;
            Int64 result = st.fp.seek(offset, origin);
            /*if (result >= 0 && st.bdplus != null)
            {
                libbdplus_seek(st.bdplus, st.fp.tell(st.fp));
            }*/
            return result;
        }

        public override long tell()
        {
            DEC_STREAM st = this;
            return st.fp.tell();
        }

        public override void close()
        {
            DEC_STREAM st = this;
            /*if (st.bdplus != null)
            {
                libbdplus_m2ts_close(st.bdplus);
            }*/
            st.fp.close();
        }

        public override int eof()
        {
            throw new NotImplementedException();
        }

        public override long write(ReadOnlySpan<byte> buf, long size)
        {
            throw new NotImplementedException();
        }

        public static BD_FILE_H dec_open_stream(BD_DEC dec, BD_FILE_H fp, UInt32 clip_id)
        {
            DEC_STREAM st = new DEC_STREAM();
            st.fp = fp;

            /*if (dec.bdplus != null)
            {
                st.bdplus = libbdplus_m2ts(dec.bdplus, clip_id, 0);
            }*/

            /*if (dec.aacs != null)
            {
                st.aacs = dec.aacs;
                if (dec.use_menus == null)
                {
                    // There won't be title events --> need to manually reset AACS CPS 
                    libaacs_select_title(dec.aacs, 0xffff);
                }
            }*/

            return st;
        }

    }
}
