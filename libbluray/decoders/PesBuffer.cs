using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{
    public struct PES_BUFFER
    {
        public Ref<byte> buf = new();
        public UInt32 len;  // payload length
        public uint size; // allocated size

        public Int64 pts;
        public Int64 dts;

        public Ref<PES_BUFFER> next = new();

        public PES_BUFFER() { }
    }

    public static class PesBuffer
    {
        internal static Ref<PES_BUFFER> pes_buffer_alloc()
        {
            Ref<PES_BUFFER> p = Ref<PES_BUFFER>.Allocate();

            return p;
        }

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
