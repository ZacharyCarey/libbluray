using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{
    internal struct PES_BUFFER
    {
        public Ref<byte> buf = new();

        /// <summary>
        /// payload length
        /// </summary>
        public UInt32 len;

        /// <summary>
        /// allocated size
        /// </summary>
        public uint size; 

        public Int64 pts;
        public Int64 dts;

        public Ref<PES_BUFFER> next = new();

        public PES_BUFFER() { }
    }

    internal static class PesBuffer
    {
        internal static Ref<PES_BUFFER> pes_buffer_alloc()
        {
            Ref<PES_BUFFER> p = Ref<PES_BUFFER>.Allocate();

            return p;
        }

        /// <summary>
        /// free list of buffers
        /// </summary>
        /// <param name="p"></param>
        internal static void pes_buffer_free(ref Ref<PES_BUFFER> p)
        {
            if (p)
            {
                if (p.Value.next)
                {
                    pes_buffer_free(ref p.Value.next);
                }
                p.Value.buf.Free();
                p.Free();
            }
        }

        /// <summary>
        /// append buf to list
        /// </summary>
        /// <param name="head"></param>
        /// <param name="buf"></param>
        internal static void pes_buffer_append(ref Ref<PES_BUFFER> head, Ref<PES_BUFFER> buf)
        {
            if (!head)
            {
                head = buf;
                return;
            }

            if (buf)
            {
                Ref<PES_BUFFER> tail = head;
                for (; tail.Value.next; tail = tail.Value.next) ;
                tail.Value.next = buf;
            }
        }

        static Ref<PES_BUFFER> _prev_buffer(Ref<PES_BUFFER> head, Ref<PES_BUFFER> buf)
        {
            while (head)
            {
                if (head.Value.next == buf)
                {
                    return head;
                }
                head = head.Value.next;
            }

            return Ref<PES_BUFFER>.Null;
        }

        /// <summary>
        /// remove buf from list and free it
        /// </summary>
        /// <param name="head"></param>
        /// <param name="p"></param>
        internal static void pes_buffer_remove(ref Ref<PES_BUFFER> head, Ref<PES_BUFFER> p)
        {
            if (head && p)
            {
                if (head == p)
                {
                    head = head.Value.next;
                    p.Value.next = Ref<PES_BUFFER>.Null;
                    pes_buffer_free(ref p);
                }
                else
                {
                    Ref<PES_BUFFER> prev = _prev_buffer(head, p);
                    if (prev)
                    {
                        prev.Value.next = p.Value.next;
                        p.Value.next = Ref<PES_BUFFER>.Null;
                        pes_buffer_free(ref p);
                    }
                }
            }
        }

        /// <summary>
        /// free first buffer and advance head to next buffer
        /// </summary>
        /// <param name="head"></param>
        internal static void pes_buffer_next(ref Ref<PES_BUFFER> head)
        {
            if (head)
            {
                Ref<PES_BUFFER> p = head;
                head = (head).Value.next;
                p.Value.next = Ref<PES_BUFFER>.Null;
                pes_buffer_free(ref p);
            }
        }
    }
}
