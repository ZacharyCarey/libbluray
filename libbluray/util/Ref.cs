using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace libbluray.util
{
    public class DataRef<T> where T : struct
    {
        public T Value;

        public DataRef() 
        {
            this.Value = new T();
        }
        public DataRef(T value)
        {
            this.Value = value;
        }
    }

    public struct Variable<T> where T : struct
    {
        private DataRef<T> Data;

        public Variable()
        {
            Data = new DataRef<T>();
        }

        public Variable(T data)
        {
            Data = new DataRef<T>(data);
        }

        public ref T Value => ref Data.Value;
        public Ref<T> Ref => new Ref<T>(Data);
    }

    /// <summary>
    /// Allows storing C-like pointers in C#.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Ref<T> where T : struct
    {
        object? Handle;
        long Index; // Only used for array types
        ulong MallocLength;

        public static Ref<T> Null => new Ref<T>();

        public Ref()
        {
            this.Handle = null;
            this.Index = 0;
            this.MallocLength = 0;
        }

        internal Ref(DataRef<T> value)
        {
            this.Handle = value;
            this.Index = 0;
            MallocLength = 0;
        }

        public Ref(T[] value, long index = 0)
        {
            this.Handle = value;
            this.Index = index;
            MallocLength = 0;
        }

        public static Ref<T> Allocate()
        {
            Ref<T> result = new Ref<T>(new DataRef<T>(new T()));
            result.MallocLength = 1;
            return result;
        }

        public static Ref<T> Allocate(long num)
        {
            return Allocate((ulong)num);
        }

        public static Ref<T> Allocate(ulong num)
        {
            if (num == 0) return new Ref<T>();
            Ref<T> result = new Ref<T>(new T[num]);
            result.MallocLength = num;
            for (ulong i = 0; i < num; i++)
            {
                result[i] = new T();
            }
            return result;
        }

        public Ref<T> Reallocate(ulong new_size)
        {
            if (new_size == 0) return new Ref<T>(); // Size 0 is undefined
            if (this == null)
            {
                return Allocate(new_size);
            }

            if (this.Handle is T[] array && this.Index == 0 && this.MallocLength > 0)
            {
                T[] data = new T[new_size];
                Array.Copy(array, data, Math.Min(array.Length, data.Length));
                this.Free(); // Release old pointer

                Ref<T> result = new Ref<T>(data);
                result.MallocLength = new_size;
                return result;
            } else
            {
                throw new Exception("Tried to realloc an invalid pointer.");
            }
            
        }

        public ref T Value
        {
            get => ref GetAtIndex(0);
        }

        public ref T this[long index]
        {
            get => ref GetAtIndex(index);
        }
        public ref T this[ulong index]
        {
            get => ref GetAtIndex((long)index);
        }

        private ref T GetAtIndex(long index)
        {
            if (Handle == null) throw new NullReferenceException("Tried to dereference a nullptr.");
            else if (Handle is T[] array)
            {
                return ref array[this.Index + index];
            } else if (Handle is DataRef<T> data)
            {
                if ((this.Index + index) != 0) throw new IndexOutOfRangeException("Pointer value was not an array, only index 0 is valid.");
                return ref data.Value;
            } else
            {
                throw new Exception("Unknown error has occured.");
            }
        }

        public Ref<T> AtIndex(long index)
        {
            Ref<T> ptr = this;
            ptr.Index += index;
            return ptr;
        }
        public Ref<T> AtIndex(ulong index) => AtIndex((long)index);

        public void Free()
        {
            if (this.Handle == null) return; // throw new NullReferenceException("Tried to free a null resource.");
            if (this.Index != 0) throw new IndexOutOfRangeException("Tried to free from an index other than 0.");
            if (this.MallocLength == 0) throw new InvalidOperationException("Tried to free a resource that was not malloc'ed");
            this.Handle = null;
            this.Index = 0;
            this.MallocLength = 0;
        }

        /// <summary>
        /// Will fail on anything except a byte array pointer
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public long ReadFromStream(Stream stream, long size)
        {
            byte[] buffer = (byte[])this.Handle;
            return stream.Read(buffer, (int)this.Index, (int)size);
        }

        /// <summary>
        /// Will fail on anything except a byte array pointer
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Span<byte> Slice(int i)
        {
            byte[] buffer = (byte[])this.Handle;
            return buffer.AsSpan((int)this.Index + i);
        }

        /// <summary>
        /// Will fail on anything except a byte array pointer
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Span<byte> Slice(int i, int len)
        {
            byte[] buffer = (byte[])this.Handle;
            return buffer.AsSpan((int)this.Index + i, len);
        }

        public Span<T> AsSpan()
        {
            T[] array = (T[])this.Handle;
            return array.AsSpan((int)this.Index);
        }

        /// <summary>
        /// Will fail on anything except a byte array pointer
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public MemoryStream AsStream()
        {
            byte[] buffer = (byte[])this.Handle;
            return new MemoryStream(buffer, (int)this.Index, buffer.Length - (int)this.Index);
        }

        public static Ref<T> operator ++(Ref<T> ptr) => ptr.AtIndex(1);

        public static Ref<T> operator --(Ref<T> ptr) => ptr.AtIndex(-1);

        public static Ref<T> operator +(Ref<T> ptr, long idx) => ptr.AtIndex(idx);
        public static Ref<T> operator +(Ref<T> ptr, ulong idx) => ptr.AtIndex(idx);
        public static Ref<T> operator -(Ref<T> ptr, long idx) => ptr.AtIndex(idx);
        public static Ref<T> operator -(Ref<T> ptr, ulong idx) => ptr.AtIndex(idx);

        private static bool CheckEqual(Ref<T> left, Ref<T>? right)
        {
            if (right != null)
            {
                return (left.Handle == right.Value.Handle)
                        && (left.Index == right.Value.Index);
                //&& (left.MallocLength == right.Value.MallocLength);
            }
            else
            {
                // Check nullptr
                return (left.Handle == null && left.Index == 0);
            }
        }


        public static implicit operator bool(Ref<T> ptr) => !CheckEqual(ptr, null);

        public static bool operator ==(Ref<T> left, Ref<T>? right) => CheckEqual(left, right);

        public static bool operator !=(Ref<T> left, Ref<T>? right) => !CheckEqual(left, right);

        public static bool operator >(Ref<T> left, Ref<T> right)
        {
            if (left.Handle == null || right.Handle == null || left.Handle != right.Handle) throw new Exception("Can't compare pointers of different variables.");
            return (left.Index > right.Index);
        }

        public static bool operator >=(Ref<T> left, Ref<T> right)
        {
            if (left.Handle == null || right.Handle == null || left.Handle != right.Handle) throw new Exception("Can't compare pointers of different variables.");
            return (left.Index >= right.Index);
        }

        public static bool operator <(Ref<T> left, Ref<T> right)
        {
            if (left.Handle == null || right.Handle == null || left.Handle != right.Handle) throw new Exception("Can't compare pointers of different variables.");
            return (left.Index < right.Index);
        }

        public static bool operator <=(Ref<T> left, Ref<T> right)
        {
            if (left.Handle == null || right.Handle == null || left.Handle != right.Handle) throw new Exception("Can't compare pointers of different variables.");
            return (left.Index <= right.Index);
        }

        public static Int64 operator -(Ref<T> left, Ref<T> right)
        {
            if (left.Handle == null || right.Handle == null || left.Handle != right.Handle) throw new Exception("Can't compare pointers of different variables.");
            return (left.Index - right.Index);
        }
    }
}
