using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.file
{
    public abstract class BD_FILE_H
    {
        /// <summary>
        /// Close file
        /// </summary>
        public abstract void close();

        /// <summary>
        /// Reposition file offset
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns>current file offset, <0 on error</returns>
        public abstract Int64 seek(Int64 offset, SeekOrigin origin);

        /// <summary>
        /// Get current read or write position
        /// </summary>
        /// <returns>current file offset, <0 on error</returns>
        public abstract Int64 tell();

        /// <summary>
        /// Check for end of file
        /// - optional, currently not used
        /// </summary>
        /// <returns>1 of EOF, <0 on error, 0 if not EOF</returns>
        public abstract int eof();

        /// <summary>
        /// Read from file
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="size"></param>
        /// <returns>number of bytes read, 0 on EOF, <0 on error</returns>
        public abstract Int64 read(Span<byte> buf, Int64 size);

        /// <inheritdoc cref="read(Span{byte}, long)"/>
        public abstract Int64 read(Ref<byte> buf, Int64 size);

        /// <summary>
        /// Write to file
        /// Writing 0 bytes can be used to flush previous writes and check for errors
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="size"></param>
        /// <returns>number of bytes written, <0 on error</returns>
        public abstract Int64 write(ReadOnlySpan<byte> buf, Int64 size);

        public Int64 size()
        {
            Int64 pos = this.file_tell();
            Int64 res1 = this.file_seek(0, SeekOrigin.End);
            Int64 length = this.file_tell();
            Int64 res2 = this.file_seek(pos, SeekOrigin.Begin);

            if (res1 < 0 || res2 < 0 || pos < 0 || length < 0)
            {
                return -1;
            }

            // TODO use FileInfo.Length?
            return length;
        }

        public delegate BD_FILE_H? BD_FILE_OPEN(string filename, bool isReadMode);
        internal static BD_FILE_OPEN file_open = DefaultFile._file_open;

        internal static BD_FILE_OPEN open_default()
        {
            return DefaultFile._file_open;
        }
    }

    internal class DefaultFile : BD_FILE_H
    {
        private FileStream Handle;

        private DefaultFile(FileStream handle)
        {
            this.Handle = handle;
        }

        public override void close()
        {
            Handle.Close();
            Handle = null;
        }

        public override int eof()
        {
            try
            {
                if (Handle.Position >= Handle.Length) return 1;
                else return 0;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public override long read(Span<byte> buf, long size)
        {
            try
            {
                if (size < buf.Length) buf = buf.Slice(0, (int)size);
                return Handle.Read(buf);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public override long read(Ref<byte> buf, long size)
        {
            try
            {
                return buf.ReadFromStream(this.Handle, size);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public override long seek(long offset, SeekOrigin origin)
        {
            try
            {
                return Handle.Seek(offset, origin);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public override long tell()
        {
            try
            {
                return Handle.Position;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public override long write(ReadOnlySpan<byte> buf, long size)
        {
            try
            {
                if (size == 0)
                {
                    Handle.Flush();
                    return 0;
                }
                else
                {
                    if (buf.Length > size) buf = buf.Slice(0, (int)size);
                    Handle.Write(buf);
                    return buf.Length;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        internal static BD_FILE_H? _file_open(string filename, bool isReadMode)
        {
            try
            {
                FileStream stream;
                if (isReadMode)
                {
                    stream = File.OpenRead(filename);
                }
                else
                {
                    stream = File.OpenWrite(filename);
                }
                return new DefaultFile(stream);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class BD_DIRENT {
        public string d_name;

        public BD_DIRENT()
        {
            d_name = "";
        }
    }

    public abstract class BD_DIR_H
    {
        /// <summary>
        /// Close directory stream
        /// </summary>
        public abstract void close();

        /// <summary>
        /// Read next directory entry
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>0 on success, 1 on EOF, <0 on error</returns>
        public abstract int read(out BD_DIRENT entry);

        public delegate BD_DIR_H? BD_DIR_OPEN(string dirname);
        internal static BD_DIR_OPEN dir_open = DefaultDirectory._dir_open;

        internal static BD_DIR_OPEN open_default()
        {
            return DefaultDirectory._dir_open;
        }
    }

    internal class DefaultDirectory : BD_DIR_H
    {
        private string[] Entries;
        private int Index = 0;
        private DefaultDirectory(string[] entries)
        {
            this.Entries = entries;
        }

        /// <summary>
        /// Close directory stream
        /// </summary>
        public override void close()
        {
            Index = Entries.Length;
        }

        /// <summary>
        /// Read next directory entry.
        /// Return 0 on success, 1 on EOF, <0 on error
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public override int read(out BD_DIRENT entry)
        {
            try
            {
                if (Index < Entries.Length)
                {
                    entry = new();
                    entry.d_name = Entries[Index++];
                    return 0;
                } else
                {
                    entry = new();
                    return 1;
                }
            }catch(Exception)
            {
                entry = new BD_DIRENT();
                return -1;
            }
        }

        /// <summary>
        /// Prototype for a function that returns BD_diR_H implementation.
        /// return NULL on error
        /// </summary>
        /// <param name="dirname"></param>
        /// <returns></returns>
        internal static BD_DIR_H? _dir_open(string dirname)
        {
            try
            {
                List<string> names = new();
                foreach(var entry in Directory.GetDirectories(dirname))
                {
                    names.Add(Path.GetFileName(Path.TrimEndingDirectorySeparator(entry)));
                }

                foreach(var entry in Directory.GetFiles(dirname))
                {
                    names.Add(Path.GetFileName(entry));
                }

                //string[] entries = Directory.GetFileSystemEntries(dirname);
                return new DefaultDirectory(names.ToArray());
            }
            catch(Exception)
            {
                return null;
            }
        }
    }

    internal static class Filesystem
    {
        public static void file_close(this BD_FILE_H fp)
        {
            fp.close();
        }

        public static Int64 file_tell(this BD_FILE_H fp)
        {
            return fp.tell();
        }

        public static Int64 file_seek(this BD_FILE_H fp, Int64 offset, SeekOrigin origin)
        {
            return fp.seek(offset, origin);
        }

        public static UInt64 file_read(this BD_FILE_H fp, Span<byte> buf, UInt64 size)
        {
            return (UInt64)fp.read(buf, (Int64)size);
        }

        public static UInt64 file_read(this BD_FILE_H fp, Ref<byte> buf, UInt64 size)
        {
            return (UInt64)fp.read(buf, (Int64)size);
        }

        internal static Int64 file_size(this BD_FILE_H fp)
        {
            return fp.size();
        }

        internal static BD_FILE_H.BD_FILE_OPEN file_open_default()
        {
            return BD_FILE_H.open_default();
        }

        internal static void dir_close(this BD_DIR_H dir)
        {
            dir.close();
        }

        internal static int dir_read(this BD_DIR_H dir, out BD_DIRENT entry)
        {
            return dir.read(out entry);
        }

        internal static  BD_DIR_H.BD_DIR_OPEN dir_open_default()
        {
            return BD_DIR_H.open_default();
        }

        internal static int file_path_exists(string path)
        {
            try
            {
                return Path.Exists(path) ? 1 : 0;
            }
            catch (Exception)
            {
                return -1;
            }
        }
        internal static int file_mkdir(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                return 0;
            }
            catch (Exception)
            {
                return -1;
            }
        }
        internal static int file_mkdirs(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return 0;
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
