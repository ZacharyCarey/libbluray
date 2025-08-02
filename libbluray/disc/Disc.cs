using libbluray.file;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static libbluray.disc.BD_DISC;

namespace libbluray.disc
{
    /// <summary>
    /// application provided file system access (optional)
    /// </summary>
    internal class fs_access {
        public object fs_handle = default;

        // Method 1: block (device) access
        public delegate int ReadBlocksFunc(object fs_handle, Span<byte> buf, int lba, int num_blocks);
        public ReadBlocksFunc? read_blocks = null;

        // Method 2: file access
        public delegate BD_DIR_H OpenDirFunc(object fs_handle, string rel_path);
        public OpenDirFunc? open_dir = null;

        public delegate BD_FILE_H OpenFileFunc(object fs_handle, string rel_path);
        public OpenFileFunc? open_file = null;

        public fs_access() { }
    }

    internal static class Disc
    {
        /*
         * BluRay Virtual File System
         *
         * Map file access to BD-ROM file system or binding unit data area
         */
        internal static BD_DISC disc_open(string device_path, fs_access? p_fs, Ref<BD_ENC_INFO> enc_info, string keyfile_path, Ref<BD_REGISTERS> regs, Func<Ref<BD_REGISTERS>, bd_psr_idx, uint> psr_read, Func<Ref<BD_REGISTERS>, bd_psr_idx, uint, int> psr_write)
        {
            BD_DISC p = _disc_init();

            if (p == null)
            {
                return null;
            }

            if (p_fs != null && p_fs.open_dir != null)
            {
                p.fs_handle = p_fs.fs_handle;
                p.pf_file_open_bdrom = p_fs.open_file;
                p.pf_dir_open_bdrom = p_fs.open_dir;
            }

            _set_paths(p, device_path);

            /* check if disc root directory can be opened. If not, treat it as device/image file. */
            BD_DIR_H dp_img = (device_path != null) ? BD_DIR_H.dir_open(device_path) : null;
            if (dp_img == null)
            {
                object udf = null;// udf_image_open(device_path, (p_fs != null) ? p_fs.fs_handle : null, (p_fs != null) ? p_fs.read_blocks : null);
                if (udf == null)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"failed opening UDF image {device_path}");
                }
                else
                {
                    /*p.fs_handle = udf;
                    p.pf_fs_close = udf_image_close;
                    p.pf_file_open_bdrom = udf_file_open;
                    p.pf_dir_open_bdrom = udf_dir_open;

                    p.udf_volid = udf_volume_id(udf);*/
                    throw new NotImplementedException();

                    /* root not accessible with stdio */
                }
            }
            else
            {
                Filesystem.dir_close(dp_img);
                Logging.bd_debug(DebugMaskEnum.DBG_FILE, $"{device_path} does not seem to be image file or device node");
            }

            dec_dev dev = new();
            dev.file_open_bdrom_handle = p.fs_handle;
            dev.pf_file_open_bdrom = new dec_dev.file_openFp(p.pf_file_open_bdrom);
            dev.file_open_vfs_handle = p;
            dev.pf_file_open_vfs = (object a, string b) => disc_open_path((BD_DISC)a, b);
            dev.root = p.disc_root;
            dev.device = device_path;

            p.dec = BD_DEC.dec_init(dev, enc_info, keyfile_path, regs, psr_read, psr_write);

            return p;
        }

        public static void disc_close(ref BD_DISC pp)
        {
            if (pp != null)
            {
                BD_DISC? p = pp;

                p.dec.dec_close();

                if (p.pf_fs_close != null)
                {
                    p.pf_fs_close(p.fs_handle);
                }

                disc_cache_clean(p, null);

                p.ovl_mutex.bd_mutex_destroy();
                p.properties_mutex.bd_mutex_destroy();
                p.cache_mutex.bd_mutex_destroy();

                p.disc_root = null;
                p.properties_file = null;
                pp = null;
            }
        }

        /// <summary>
        /// Get BD-ROM root path
        /// </summary>
        /// <param name="disc"></param>
        /// <returns></returns>
        internal static string disc_root(this BD_DISC disc)
        {
            return disc.disc_root;
        }

        /// <summary>
        /// Get UDF volume ID
        /// </summary>
        /// <param name="disc"></param>
        /// <returns></returns>
        internal static string disc_volume_id(this BD_DISC disc)
        {
            return (disc != null) ? disc.udf_volid : null;
        }

        /// <summary>
        /// Generate pseudo disc ID
        /// </summary>
        /// <param name="disc"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static int disc_pseudo_id(this BD_DISC disc, byte[] id)
        {
            byte[][] h = [new byte[20], new byte[20]];
            int i, r = 0;

            r += _hash_file(disc, "BDMV", "MovieObject.bdmv", h[0]);
            r += _hash_file(disc, "BDMV", "index.bdmv", h[1]);

            for (i = 0; i < 20; i++)
            {
                id[i] = (byte)(h[0][i] ^ h[1][i]);
            }

            return (r > 0) ? 1 : 0;
        }

        /// <summary>
        /// Open VFS file (relative to disc root)
        /// </summary>
        /// <param name="disc"></param>
        /// <param name="dir"></param>
        /// <param name="file"></param>
        /// <returns></returns>

        internal static BD_FILE_H disc_open_file(this BD_DISC disc, string dir, string file)
        {
            BD_FILE_H fp = null;
            string path = null;

            path = Path.Combine(dir, file);
            if (path == null)
            {
                return null;
            }

            fp = disc.disc_open_path(path);

            return fp;
        }

        /// <summary>
        /// Open VFS file (relative to disc root)
        /// </summary>
        /// <param name=""></param>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static BD_FILE_H disc_open_path(this BD_DISC p, string rel_path)
        {
            BD_FILE_H fp = null;

            if (p.avchd > 0)
            {
                string? avchd_path = _avchd_file_name(rel_path);
                if (avchd_path != null)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_FILE, $"AVCHD: {rel_path} -> {avchd_path}");
                    fp = p.pf_file_open_bdrom(p.fs_handle, avchd_path);
                    if (fp != null)
                    {
                        return fp;
                    }
                }
            }

            /* search file from overlay */
            fp = _overlay_open_path(p, rel_path);

            /* if not found, try BD-ROM */
            if (fp == null)
            {
                fp = p.pf_file_open_bdrom(p.fs_handle, rel_path);

                if (fp == null)
                {

                    /* AVCHD short filenames detection */
                    if (p.avchd < 0 && rel_path != Path.Combine("BDMV", "index.bdmv"))
                    {
                        fp = p.pf_file_open_bdrom(p.fs_handle, Path.Combine("BDMV", "INDEX.BDM"));
                        if (fp != null)
                        {
                            Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, "detected AVCHD 8.3 filenames");
                        }
                        p.avchd = (fp != null) ? (sbyte)1 : (sbyte)0;
                    }

                    if (fp == null)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"error opening file {rel_path}");
                    }
                }
            }

            return fp;
        }

        /// <summary>
        /// Open VFS directory (relative to disc root)
        /// </summary>
        /// <param name="disc"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        internal static BD_DIR_H disc_open_dir(this BD_DISC p, string dir)
        {
            BD_DIR_H dp_rom = null;
            BD_DIR_H dp_ovl = null;

            dp_rom = p.pf_dir_open_bdrom(p.fs_handle, dir);
            dp_ovl = _overlay_open_dir(p, dir);

            if (dp_ovl == null)
            {
                if (dp_rom == null)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_FILE, $"error opening dir {dir}");
                }
                return dp_rom;
            }
            if (dp_rom == null)
            {
                return dp_ovl;
            }

            return _combine_dirs(dp_ovl, dp_rom);
        }

        /// <summary>
        /// Read VFS file
        /// </summary>
        /// <param name="disc"></param>
        /// <param name="dir"></param>
        /// <param name="file"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static UInt64 disc_read_file(this BD_DISC disc, string dir, string file, out Ref<byte> data)
        {
            BD_FILE_H fp = null;
            Int64 size;

            data = Ref<byte>.Null;

            if (dir != null)
            {
                fp = disc_open_file(disc, dir, file);
            }
            else
            {
                fp = disc_open_path(disc, file);
            }
            if (fp == null)
            {
                return 0;
            }

            size = Filesystem.file_size(fp);
            if (size > 0 && size < Util.BD_MAX_SSIZE)
            {
                data = Ref<byte>.Allocate(size);
                if (data)
                {
                    Int64 got = (Int64)Filesystem.file_read(fp, data, (ulong)size);
                    if (got != size)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"Error reading file {file} from {dir}");
                        data = Ref<byte>.Null;
                        size = 0;
                    }
                }
                else
                {
                    size = 0;
                }
            }
            else
            {
                size = 0;
            }

            Filesystem.file_close(fp);
            return (UInt64)size;
        }


        /// <summary>
        /// Update virtual package
        /// </summary>
        /// <param name="disc"></param>
        /// <param name="overlay_root"></param>
        internal static void disc_update(this BD_DISC disc, string overlay_root)
        {
            disc.ovl_mutex.bd_mutex_lock();

            disc.overlay_root = null;
            if (overlay_root != null)
            {
                disc.overlay_root = overlay_root;
            }

            disc.ovl_mutex.bd_mutex_unlock();
        }

        /// <summary>
        /// Cache file directly from BD-ROM
        /// </summary>
        /// <param name="p"></param>
        /// <param name="rel_path"></param>
        /// <param name="cache_path"></param>
        /// <returns></returns>
        internal static int disc_cache_bdrom_file(this BD_DISC p, string rel_path, string cache_path)
        {
            BD_FILE_H fp_in = null;
            BD_FILE_H fp_out = null;
            Int64 got;
            UInt64 size;

            if (cache_path == null || cache_path.Length == 0)
            {
                return -1;
            }

            /* make sure cache directory exists */
            if (Filesystem.file_mkdirs(cache_path) < 0)
            {
                return -1;
            }

            /* plain directory ? */
            size = (ulong)rel_path.Length;
            if (size < 1 || rel_path[(int)size - 1] == '/' || rel_path[(int)size - 1] == '\\')
            {
                return 0;
            }

            /* input file from BD-ROM */
            fp_in = p.pf_file_open_bdrom(p.fs_handle, rel_path);
            if (fp_in == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"error caching file {rel_path} (does not exist ?)");
                return -1;
            }

            /* output file in local filesystem */
            fp_out = BD_FILE_H.file_open(cache_path, false);
            if (fp_out == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"error creating cache file {cache_path}");
                Filesystem.file_close(fp_in);
                return -1;
            }

            do
            {
                byte[] buf = new byte[16 * 2048];
                got = (long)Filesystem.file_read(fp_in, buf, (ulong)buf.Length);

                /* we'll call write(fp, buf, 0) after EOF. It is used to check for errors. */
                if (got < 0 || fp_out.write(buf, got) != got)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"error caching file {rel_path}");
                    Filesystem.file_close(fp_out);
                    Filesystem.file_close(fp_in);
                    return -1;
                }
            } while (got > 0);

            Logging.bd_debug(DebugMaskEnum.DBG_FILE, $"cached {rel_path} to {cache_path}");

            Filesystem.file_close(fp_out);
            Filesystem.file_close(fp_in);
            return 0;
        }

        /// <summary>
        /// Open decrypted file 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="rel_path"></param>
        /// <returns></returns>
        internal static BD_FILE_H disc_open_path_dec(this BD_DISC p, string rel_path)
        {
            UInt64 size = (ulong)rel_path.Length;
            int index = (size > 5) ? (int)(size - 5) : 0;
            string suf = (size > 5) ? rel_path[index..] : rel_path;

            /* check if it's a stream */
            if (!rel_path.StartsWith(Path.Combine("BDMV", "STREAM")))
            { // not equal
                return disc_open_path(p, rel_path);
            }
            else if (suf == ".m2ts")
            { // equal
                return disc_open_stream(p, rel_path[(index - 5)..]);
            }
            else if (suf[1..] == ".MTS")
            { // equal
                return disc_open_stream(p, rel_path[(index - 4)..]);
            }
            else if (suf == ".ssif")
            { // equal
                Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"error opening file {rel_path}, ssif is not yet supported.");
            }
            else
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"error opening file {rel_path}");
            }
            return null;
        }

        // persistent properties storage
        private static string _properties_file(BD_DISC p)
        {
            byte[] disc_id = null;
            byte[] pseudo_id = new byte[20];
            byte id_type = 0;
            byte[] id_str = new byte[41];
            string cache_home = null;
            string properties_file = null;

            /* get disc ID */
            if (p.dec != null)
            {
                id_type = (byte)'A';
                disc_id = p.dec.dec_disc_id();
            }
            if (disc_id == null)
            {
                id_type = (byte)'P';
                if (disc_pseudo_id(p, pseudo_id) > 0)
                {
                    disc_id = pseudo_id;
                }
            }
            if (disc_id == null)
            {
                return null;
            }

            cache_home = Dirs.file_get_cache_home();
            if (cache_home == null)
            {
                return null;
            }

            properties_file = Path.Combine(cache_home, "bluray", "properties", $"{(char)id_type}{disc_id:x20}");

            return properties_file;
        }

        private static int _ensure_properties_file(BD_DISC p)
        {
            p.properties_mutex.bd_mutex_lock();
            if (p.properties_file == null)
            {
                p.properties_file = _properties_file(p);
            }
            p.properties_mutex.bd_mutex_unlock();

            return (p.properties_file != null) ? 0 : -1;
        }

        /// <summary>
        /// open BD-ROM directory (relative to disc root) 
        /// </summary>
        /// <param name="disc"></param>
        /// <param name="rel_path"></param>
        /// <returns></returns>
        internal static BD_DIR_H disc_open_bdrom_dir(this BD_DISC disc, string rel_path)
        {
            return disc.pf_dir_open_bdrom(disc.fs_handle, rel_path);
        }

        /// <summary>
        /// m2ts stream interface
        /// </summary>
        /// <param name="disc"></param>
        /// <param name="file"></param>
        /// <returns></returns>

        internal static BD_FILE_H disc_open_stream(this BD_DISC disc, string file)
        {
            BD_FILE_H fp = disc_open_file(disc, Path.Combine("BDMV", "STREAM"), file);
            if (fp == null)
            {
                return null;
            }

            if (disc.dec != null)
            {
                BD_FILE_H st = DEC_STREAM.dec_open_stream(disc.dec, fp, uint.Parse(Path.GetFileNameWithoutExtension(file)));
                if (st != null)
                {
                    return st;
                }
            }

            return fp;
        }

        /// <summary>
        /// Store / fetch persistent properties for disc.
        /// Data is stored in cache directory and persists between playback sessions.
        ///
        /// Property name is ASCII string. '=' or '\n' is not allowed in name.
        /// property data is UTF8 string without line feeds.
        /// </summary>
        /// <param name="disc"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int disc_property_put(this BD_DISC disc, string property, string value)
        {
            int result;

            if (_ensure_properties_file(disc) < 0)
            {
                return -1;
            }

            disc.properties_mutex.bd_mutex_lock();
            result = Properties.properties_put(disc.properties_file, property, value);
            disc.properties_mutex.bd_mutex_unlock();

            return result;
        }
        internal static string disc_property_get(this BD_DISC disc, string property)
        {
            string result = null;

            if (_ensure_properties_file(disc) < 0)
            {
                return null;
            }

            disc.properties_mutex.bd_mutex_lock();
            result = Properties.properties_get(disc.properties_file, property);
            disc.properties_mutex.bd_mutex_unlock();

            return result;
        }

        /* "Known" playlists */
        public const string DISC_PROPERTY_PLAYLISTS = "Playlists";
        public const string DISC_PROPERTY_MAIN_FEATURE = "MainFeature";

        internal static byte[] disc_get_data(this BD_DISC disc, int type)
        {
            if (disc.dec != null)
            {
                return disc.dec.dec_data(type);
            }
            if (type == 0x1000)
            {
                /* this shouldn't cause any extra optical disc access */
                BD_DIR_H d = disc.pf_dir_open_bdrom(disc.fs_handle, "MAKEMKV");
                if (d != null)
                {
                    d.dir_close();
                    Logging.bd_debug(DebugMaskEnum.DBG_FILE, "Detected MakeMKV backup data");
                    return Encoding.ASCII.GetBytes("mmbd;backup");
                }
            }
            return null;
        }

        public enum DiscEventType {
            /// <summary>
            /// param: number of titles, 0 if playing with menus
            /// </summary>
            DISC_EVENT_START,

            /// <summary>
            /// param: title number
            /// </summary>
            DISC_EVENT_TITLE,

            /// <summary>
            /// param: app data
            /// </summary>
            DISC_EVENT_APPLICATION, 
        };

        internal static void disc_event(this BD_DISC disc, DiscEventType _event, UInt32 param)
        {
            if ((disc != null) && (disc.dec != null))
            {
                switch (_event)
                {
                    case DiscEventType.DISC_EVENT_START:
                        disc.dec.dec_start(param);
                        return;
                    case DiscEventType.DISC_EVENT_TITLE:
                        disc.dec.dec_title(param);
                        return;
                    case DiscEventType.DISC_EVENT_APPLICATION:
                        disc.dec.dec_application(param);
                        return;
                }
            }
        }

        /// <summary>
        /// Pseudo disc ID
        /// This is used when AACS disc ID is not available
        /// </summary>
        /// <param name="k"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private static UInt64 ROTL64(UInt64 k, Int32 n)
        {
            return ((k << n) | (k >> (64 - n)));
        }

        private static UInt64 _fmix64(UInt64 k)
        {
            k ^= k >> 33;
            k *= 0xff51afd7ed558ccd;
            k ^= k >> 33;
            k *= 0xc4ceb9fe1a85ec53;
            k ^= k >> 33;
            return k;
        }

        private static void _murmurhash3_128(Ref<byte> _in, UInt64 len, Span<byte> _out)
        {
            // original MurmurHash3 was written by Austin Appleby, and is placed in the public domain.
            // https://code.google.com/p/smhasher/wiki/MurmurHash3
            UInt64 c1 = 0x87c37b91114253d5UL;
            UInt64 c2 = 0x4cf5ad432745937fUL;
            UInt64[] h = [ 0, 0 ];
            UInt64 i;

            /* use only N * 16 bytes, ignore tail */
            len &= ~15ul;

            for (i = 0; i < len; i += 16)
            {
                UInt64 k1, k2;
                k1 = BitConverter.ToUInt64(_in.Slice((int)i, 8));
                k2 = BitConverter.ToUInt64(_in.Slice((int)i + 8, 8));

                k1 *= c1; k1 = ROTL64(k1, 31); k1 *= c2; h[0] ^= k1;

                h[0] = ROTL64(h[0], 27); h[0] += h[1]; h[0] = h[0] * 5 + 0x52dce729;

                k2 *= c2; k2 = ROTL64(k2, 33); k2 *= c1; h[1] ^= k2;

                h[1] = ROTL64(h[1], 31); h[1] += h[0]; h[1] = h[1] * 5 + 0x38495ab5;
            }

            h[0] ^= len;
            h[1] ^= len;

            h[0] += h[1];
            h[1] += h[0];

            h[0] = _fmix64(h[0]);
            h[1] = _fmix64(h[1]);

            h[0] += h[1];
            h[1] += h[0];

            //memcpy(_out, h, 2 * sizeof(uint64_t));
            BitConverter.TryWriteBytes(_out, h[0]);
            BitConverter.TryWriteBytes(_out.Slice(0, sizeof(UInt64)), h[1]);
        }

        private static int _hash_file(BD_DISC p, string dir, string file, Span<byte> hash)
        {
            Ref<byte> data;
            UInt64 sz;

            sz = disc_read_file(p, dir, file, out data);
            if (sz > 16)
            {
                _murmurhash3_128(data, sz, hash);
            }

            return (sz > 16) ? 1 : 0;
        }

        /*
         * cache
         *
         * Cache can hold any reference-counted objects (= allocated with refcnt_*).
         *
         */

        internal static object? disc_cache_get(this BD_DISC disc, string key)
        {
            object? data = null;

            disc.cache_mutex.bd_mutex_lock();
            if (disc.cache != null)
            {
                if (!disc.cache.TryGetValue(key, out data))
                {
                    data = null;
                }
            }
            disc.cache_mutex.bd_mutex_unlock();
            
            return data;
        }
        internal static void disc_cache_put(this BD_DISC disc, string key, object? data)
        {
            /*if (key.Length >= sizeof(p->cache[0].name)) {
                BD_DEBUG(DBG_FILE | DBG_CRIT, "disc_cache_put: key %s too large\n", name);
                return;
            }*/
            if (data == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"disc_cache_put: NULL for key {key} ignored");
                return;
            }
            
            disc.cache_mutex.bd_mutex_lock();

            if (disc.cache == null)
            {
                disc.cache = new Dictionary<string, object?>();
            }

            if (disc.cache != null)
            {
                if (disc.cache.ContainsKey(key))
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"disc_cache_put(): duplicate key {key}");
                }

                disc.cache[key] = data;
                Logging.bd_debug(DebugMaskEnum.DBG_FILE, $"disc_cache_put: added {key} (pointer here)");
            }
            else
            {
                Logging.bd_debug(DebugMaskEnum.DBG_FILE | DebugMaskEnum.DBG_CRIT, $"disc_cache_put: error adding {key} (pointer goes here): Out of memory");
            }

            disc.cache_mutex.bd_mutex_unlock();
        }

        /* NULL key == drop all */
        internal static void disc_cache_clean(this BD_DISC disc, string key)
        {
            disc.cache_mutex.bd_mutex_lock();
            disc.cache.Clear();
            disc.cache_mutex.bd_mutex_unlock();
        }
        private static BD_FILE_H _bdrom_open_path(BD_DISC disc, string rel_path)
        {
            BD_FILE_H fp = null;
            string abs_path = null;

            abs_path = $"{disc.disc_root}{rel_path}";

            fp = BD_FILE_H.file_open(abs_path, true);
            return fp;
        }

        private static BD_DIR_H _bdrom_open_dir(BD_DISC disc, string dir)
        {
            BD_DIR_H dp = null;
            string path = null;

            path = $"{disc.disc_root}{dir}";
            dp = BD_DIR_H.dir_open(path);

            return dp;
        }

        private static readonly string[][] map = [
            [".mpls", ".MPL"],
            [".clpi", ".CPI"],
            [".m2ts", ".MTS"],
            [".bdmv", ".BDM"]
        ];

        private static string? _avchd_file_name(string rel_path)
        {
            if (rel_path == null) return null;
            if (!Path.HasExtension(rel_path)) return null;

            // Take up to 8 chars from file name
            string name = Path.GetFileNameWithoutExtension(rel_path).ToUpper();
            if (name.Length > 8) name = name[..8];

            // Convert extension
            string ext = Path.GetExtension(rel_path).ToLower();
            string? newExt = map.Where(x => ext == x[0]).Select(x => x[1]).FirstOrDefault();
            if (newExt == null) return null;

            return $"{name}{newExt}";
        }

        private static BD_FILE_H _overlay_open_path(BD_DISC p, string rel_path)
        {
            BD_FILE_H fp = null;

            p.ovl_mutex.bd_mutex_lock();

            if (p.overlay_root != null)
            {
                string abs_path = $"{p.overlay_root}{rel_path}";
                fp = BD_FILE_H.file_open(abs_path, true);
            }

            p.ovl_mutex.bd_mutex_unlock();

            return fp;
        }

        private static BD_DIR_H _overlay_open_dir(BD_DISC p, string dir)
        {
            BD_DIR_H dp = null;

            p.ovl_mutex.bd_mutex_lock();

            if (p.overlay_root != null)
            {
                string abs_path = $"{p.disc_root}{dir}";
                dp = Filesystem.dir_open_default()(abs_path);
            }

            p.ovl_mutex.bd_mutex_unlock();

            return dp;
        }

        private class COMB_DIR : BD_DIR_H
        {
            public uint count;
            public uint pos;
            public BD_DIRENT[] entry = new BD_DIRENT[1];

            public override int read(out BD_DIRENT entry)
            {
                if (this.pos < this.count)
                {
                    entry = new();
                    entry.d_name = this.entry[pos++].d_name;
                    return 0;
                }
                entry = null;
                return 1;
            }

            public override void close()
            {
                
            }

            internal void _comb_dir_append(BD_DIRENT entry)
            {
                uint i;

                /* no duplicates */
                for (i = 0; i < this.count; i++)
                {
                    if (this.entry[i].d_name == entry.d_name)
                    {
                        return;
                    }
                }

                /* append */
                var temp = new BD_DIRENT[this.count + 1];
                Array.Copy(this.entry, temp, this.entry.Length);
                this.entry = temp;
                this.entry[this.count] = new BD_DIRENT();
                this.entry[this.count].d_name = entry.d_name;
                this.count++;
            }
        }

        private static BD_DIR_H _combine_dirs(BD_DIR_H ovl, BD_DIR_H rom)
        {
            COMB_DIR dp = new COMB_DIR();
            BD_DIRENT entry;

            if (dp != null)
            {
                while (Filesystem.dir_read(ovl, out entry) == 0) {
                    dp._comb_dir_append(entry);
                }
                while (Filesystem.dir_read(rom, out entry) == 0)
                {
                    dp._comb_dir_append(entry);
                }
            }

        _out:
            Filesystem.dir_close(ovl);
            Filesystem.dir_close(rom);

            return dp;
        }

        // Disc open / close
        private static BD_DISC _disc_init()
        {
            BD_DISC p = new BD_DISC();
            if (p != null)
            {
                p.ovl_mutex = new BD_MUTEX();
                p.properties_mutex = new BD_MUTEX();
                p.cache_mutex = new BD_MUTEX();

                /* default file access functions */
                p.fs_handle = (object)p;
                p.pf_file_open_bdrom = (object a, string b) => _bdrom_open_path((BD_DISC)a, b);
                p.pf_dir_open_bdrom = (object a, string b) => _bdrom_open_dir((BD_DISC)a, b);

                p.avchd = -1;
            }
            return p;
        }

        private static void _set_paths(BD_DISC p, string device_path)
        {
            if (device_path != null)
            {
                string disc_root = device_path;//mount_get_mountpoint(device_path);

                /* make sure path ends to slash */
                if (disc_root == null || Path.EndsInDirectorySeparator(disc_root))
                {
                    p.disc_root = disc_root;
                }
                else
                {
                    p.disc_root = Path.TrimEndingDirectorySeparator(disc_root) + Path.DirectorySeparatorChar;
                }
            }
        }
    }

    internal class BD_DISC
    {
        /// <summary>
        /// protect access to overlay root
        /// </summary>
        internal BD_MUTEX ovl_mutex = new();

        /// <summary>
        /// protect access to properties file
        /// </summary>
        internal BD_MUTEX properties_mutex = new();

        /// <summary>
        /// disc filesystem root (if disc is mounted)
        /// </summary>
        internal string disc_root = null;

        /// <summary>
        /// overlay filesystem root (if set)
        /// </summary>
        internal string overlay_root = null;  

        internal BD_DEC dec = null;

        internal object fs_handle = null;
        internal fs_access.OpenFileFunc? pf_file_open_bdrom = null;
        internal fs_access.OpenDirFunc? pf_dir_open_bdrom = null;
        internal Action<object> pf_fs_close = null;

        internal string udf_volid = null;

        /// <summary>
        /// NULL if not yet used
        /// </summary>
        internal string properties_file = null;

        /// <summary>
        /// -1 - unknown. 0 - no. 1 - yes
        /// </summary>
        internal sbyte avchd = -1;  

        /* disc cache */
        internal BD_MUTEX cache_mutex = new();
        internal Dictionary<string, object?> cache = new();
    }
}
