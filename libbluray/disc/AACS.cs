/*using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static libbluray.disc.dec_dev;

namespace libbluray.disc
{
    internal enum AacsProperty
    {
        DiscID = 1,
        MediaVID = 2,
        MediaPMSN = 3,
        DeviceBindingID = 4,
        DeviceNONCE = 5,
        MediaKey = 6,
        ContentCertID = 7,
        BdjRootCertHash = 8
    }

    internal enum AacsImplementation
    {
        User,
        LibAACS,
        LibMMBD
    }

    internal class BD_AACS
    {
        object h_libaacs; // library handle from dlopen
        object aacs; // aacs handle from aacs_open()

        byte[] disc_id;
        UInt32 mkbv;

        // function pointers
        fptr_int decrypt_unit;
        fptr_int decrypt_bus;

        AacsImplementation impl_id;

        private void _libaacs_close()
        {
            if (this.aacs != null)
            {
                DL_CALL(this.h_libaacs, aacs_close, this.aacs);
                this.aacs = null;
            }
        }

        private void _unload()
        {
            _libaacs_close();

            if (this.h_libaacs != null)
            {
                dl_dlclose(this.h_libaacs);
            }
        }

        public void libaacs_unload()
        {
            _unload();
        }

        public int libaacs_required(object have_file_handle, Func<object, string, string, int> have_file)
        {
            if (have_file(have_file_handle, "AACS", "Unit_Key_RO.inf") != 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"{Path.Combine("AACS", "Unit_key_RO.inf")} found. Disc seems to be AACS protected.");
                return 1;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"{Path.Combine("AACS", "Unit_Key_RO.inf")} not found. No AACS protection.");
            return 0;
        }

        private static readonly string[] libaacs = [
          null,//getenv("LIBAACS_PATH"),
          "libaacs",
          "libmmbd",
    //#ifdef _WIN64
          "libmmbd64",
    //#endif
        ];

        private object _open_libaacs(ref AacsImplementation impl_id)
        {
            uint ii;

            for (ii = (uint)impl_id; ii < libaacs.Length; ii++)
            {
                if (libaacs[ii] != null)
                {
                    object handle = dl_dlopen(libaacs[ii], "0");
                    if (handle != null)
                    {
                        // One more libmmbd check. This is needed if libaacs is just a link to libmmbd ... 
                        fptr_int32 fp;
                        *(void**)(&fp) = dl_dlsym(handle, "bdplus_get_code_date");
                        if (fp && fp(NULL) == 0)
                        {
                            ii = IMPL_LIBMMBD;
                        }

                        *impl_id = ii;
                        BD_DEBUG(DBG_BLURAY, "Using %s for AACS\n", libaacs[ii]);
                        return handle;
                    }
                }
            }

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, "No usable AACS libraries found!");
            return null;
        }

        private static BD_AACS? _load(AacsImplementation impl_id)
        {
            BD_AACS p = new BD_AACS();
            if (p == null)
            {
                return null;
            }
            p.impl_id = impl_id;

            p.h_libaacs = _open_libaacs(ref p.impl_id);
            if (p.h_libaacs == null)
            {
                return null;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, $"Loading aacs library (pointer goes here)");

            *(void**)(&p->decrypt_unit) = dl_dlsym(p->h_libaacs, "aacs_decrypt_unit");
            *(void**)(&p->decrypt_bus) = dl_dlsym(p->h_libaacs, "aacs_decrypt_bus");

            if (p.decrypt_unit == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, "libaacs dlsym failed! (pointer goes here)");
                p.libaacs_unload();
                return null;
            }

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, "Loaded libaacs (pointer goes here)");

            if (file_open != file_open_default())
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY, "Registering libaacs filesystem handler p (p)");
                DL_CALL(p.h_libaacs, aacs_register_file, file_open);
            }

            return p;
        }

        public static BD_AACS? libaacs_load(int force_mmbd)
        {
            return _load((force_mmbd != 0) ? AacsImplementation.LibMMBD : 0);
        }

        public int libaacs_open(string device, object file_open_handle, object file_open_fp, string keyfile_path)
        {
            int error_code = 0;

            fptr_p_void open;
            fptr_p_void open2;
            fptr_p_void init;
            fptr_int open_device;
            fptr_int aacs_get_mkb_version;
            fptr_p_void aacs_get_disc_id;

            _libaacs_close(p);

            *(void**)(&open) = dl_dlsym(p->h_libaacs, "aacs_open");
            *(void**)(&open2) = dl_dlsym(p->h_libaacs, "aacs_open2");
            *(void**)(&init) = dl_dlsym(p->h_libaacs, "aacs_init");
            *(void**)(&aacs_get_mkb_version) = dl_dlsym(p->h_libaacs, "aacs_get_mkb_version");
            *(void**)(&aacs_get_disc_id) = dl_dlsym(p->h_libaacs, "aacs_get_disc_id");
            *(void**)(&open_device) = dl_dlsym(p->h_libaacs, "aacs_open_device");

            if (init && open_device)
            {
                p.aacs = init();
                DL_CALL(p->h_libaacs, aacs_set_fopen, p->aacs, file_open_handle, file_open_fp);
                error_code = open_device(p->aacs, device, keyfile_path);
            }
            else if (open2)
            {
                BD_DEBUG(DBG_BLURAY, "Using old aacs_open2(), no UDF support available\n");
                p.aacs = open2(device, keyfile_path, &error_code);

                // libmmbd needs dev: for devices 
                if (!p->aacs && p->impl_id == IMPL_LIBMMBD && !strncmp(device, "/dev/", 5))
                {
                    char* tmp_device = str_printf("dev:%s", device);
                    if (tmp_device)
                    {
                        p->aacs = open2(tmp_device, keyfile_path, &error_code);
                        X_FREE(tmp_device);
                    }
                }
            }
            else if (open)
            {
                BD_DEBUG(DBG_BLURAY, "Using old aacs_open(), no verbose error reporting available\n");
                p->aacs = open(device, keyfile_path);
            }
            else
            {
                BD_DEBUG(DBG_BLURAY, "aacs_open() not found\n");
            }

            if (error_code)
            {
                // failed. try next aacs implementation if available. 
                BD_AACS* p2 = _load(p->impl_id + 1);
                if (p2)
                {
                    if (!libaacs_open(p2, device, file_open_handle, file_open_fp, keyfile_path))
                    {
                        // succeed - swap implementations 
                        _unload(p);
                        *p = *p2;
                        X_FREE(p2);
                        return 0;
                    }
                    // failed - report original errors 
                    libaacs_unload(&p2);
                }
            }

            if (p->aacs)
            {
                if (aacs_get_mkb_version)
                {
                    p->mkbv = aacs_get_mkb_version(p->aacs);
                }
                if (aacs_get_disc_id)
                {
                    p->disc_id = (const uint8_t*)aacs_get_disc_id(p->aacs);
                }
                return error_code;
            }

            return error_code ? error_code : 1;
        }

        public void libaacs_select_title(UInt32 title)
        {
            if (this.aacs != null)
            {
                DL_CALL(p->h_libaacs, aacs_select_title, p->aacs, title);
            }
        }

        public int libaacs_decrypt_unit(byte[] buf)
        {
            if (this.aacs != null)
            {
                if (this.decrypt_unit(this.aacs, buf) == 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_AACS | DebugMaskEnum.DBG_CRIT, "Unable decrypt unit (AACS)!");

                    return -1;
                } // decrypt
            } // aacs

            return 0;
        }

        public int libaacs_decrypt_bus(byte[] buf)
        {
            if (p && p->aacs && p->decrypt_bus)
            {
                if (p->decrypt_bus(p->aacs, buf) > 0)
                {
                    return 0;
                }
            }

            BD_DEBUG(DBG_AACS | DBG_CRIT, "Unable to BUS decrypt unit (AACS)!\n");
            return -1;
        }

        public UInt32 libaacs_get_mkbv()
        {
            return this.mkbv;
        }

        public int libaacs_get_bec_enabled()
        {
            fptr_int get_bec;

            if (!p || !p->h_libaacs)
            {
                return 0;
            }

            *(void**)(&get_bec) = dl_dlsym(p->h_libaacs, "aacs_get_bus_encryption");
            if (!get_bec)
            {
                BD_DEBUG(DBG_BLURAY, "aacs_get_bus_encryption() dlsym failed!\n");
                return 0;
            }

            return get_bec(p->aacs) == 3;
        }

        private byte[] _get_data(string func)
        {
            fptr_p_void fp;

            *(void**)(&fp) = dl_dlsym(p->h_libaacs, func);
            if (!fp)
            {
                BD_DEBUG(DBG_BLURAY | DBG_CRIT, "%s() dlsym failed!\n", func);
                return NULL;
            }

            return (const uint8_t*)fp(p->aacs);
        }

        private static string _type2str(AacsProperty type)
        {
            switch(type)
            {
                case AacsProperty.DiscID: return "DISC_ID";
                case AacsProperty.MediaVID: return "MEDIA_VID";
                case AacsProperty.MediaPMSN: return "MEDIA_PMSN";
                case AacsProperty.DeviceBindingID: return "DEVICE_BINDING_ID";
                case AacsProperty.DeviceNONCE: return "DEVICE_NONCE";
                case AacsProperty.MediaKey: return "MEDIA_KEY";
                case AacsProperty.ContentCertID: return "CONTENT_CERT_ID";
                case AacsProperty.BdjRootCertHash: return "BDJ_ROOT_CERT_HASH";
                default: return "???";
            }
        }

        private byte[] libaacs_get_aacs_data(AacsProperty type)
        {
            if (this.aacs == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"get_aacs_data({_type2str(type)}): libaacs not initialized!");
                return null;
            }

            switch (type)
            {
                case AacsProperty.DiscID:
                    return this.disc_id;

                case AacsProperty.MediaVID:
                    return _get_data("aacs_get_vid");

                case AacsProperty.MediaPMSN:
                    return _get_data("aacs_get_pmsn");

                case AacsProperty.DeviceBindingID:
                    return _get_data("aacs_get_device_binding_id");

                case AacsProperty.DeviceNONCE:
                    return _get_data("aacs_get_device_nonce");

                case AacsProperty.MediaKey:
                    return _get_data("aacs_get_mk");

                case AacsProperty.ContentCertID:
                    return _get_data("aacs_get_content_cert_id");

                case AacsProperty.BdjRootCertHash:
                    return _get_data("aacs_get_bdj_root_cert_hash");
            }

            Logging.bd_debug(DebugMaskEnum.DBG_BLURAY | DebugMaskEnum.DBG_CRIT, $"get_aacs_data(): unknown query {type}");
            return null;
        }
    }
}
*/