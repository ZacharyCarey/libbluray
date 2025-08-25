using DiscUtils.Iso9660;
using DiscUtils.Udf;
using libbluray.disc;
using libbluray.file;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static libbluray.disc.dec_dev;
using static libbluray.disc.fs_access;

namespace libbluray.disc
{
    internal class UDF : IDisposable
    {
        private FileStream BaseStream;
        private UdfReader handle;

        private UDF(FileStream baseStream, UdfReader reader)
        {
            this.BaseStream = baseStream;
            this.handle = reader;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                handle.Dispose();
                BaseStream.Dispose();
            }
        }

        internal static UDF? udf_image_open(string img_path, object read_block_handle, ReadBlocksFunc? read_blocks)
        {
            try
            {
                FileStream? stream = File.OpenRead(img_path);
                UdfReader? reader = new UdfReader(stream);

                if (stream != null && reader != null)
                {
                    return new UDF(stream, reader);
                }

                reader?.Dispose();
                stream?.Close();
                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static void udf_image_close(object? obj)
        {
            if (obj != null && obj is UDF udf)
            {
                udf.Dispose();
            }
        }

        public static BD_FILE_H udf_file_open(object? obj, string filename)
        {
            if (obj != null && obj is UDF udf)
            {
                if (!udf.handle.FileExists(filename))
                {
                    return null;
                }
                return new DefaultFile(udf.handle.OpenFile(filename, FileMode.Open));
            }

            return null;
        }

        public static BD_DIR_H udf_dir_open(object? obj, string dirname)
        {
            if (obj != null && obj is UDF udf)
            {
                if (!udf.handle.DirectoryExists(dirname))
                {
                    return new DefaultDirectory(Array.Empty<string>());
                }

                List<string> names = new();
                foreach (var entry in udf.handle.GetDirectories(dirname))
                {
                    names.Add(Path.GetFileName(Path.TrimEndingDirectorySeparator(entry)));
                }

                foreach(var entry in udf.handle.GetFiles(dirname))
                {
                    names.Add(Path.GetFileName(entry));
                }

                return new DefaultDirectory(names.ToArray());
            }

            return null;
        }

        public static string udf_volume_id(object? obj)
        {
            if (obj != null && obj is UDF udf)
            {
                return udf.handle.VolumeLabel;
            }

            return null;
        }
    }
}
