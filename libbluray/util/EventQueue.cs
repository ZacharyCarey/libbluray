using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.util
{
    internal struct BD_EVENT_QUEUE<T> where T : struct
    {
        private const uint MAX_EVENTS = 31;

        BD_MUTEX mutex = new();
        uint _in; // next free slot
        uint _out; // next event
        T[] ev = new T[MAX_EVENTS + 1];

        public BD_EVENT_QUEUE() {
            for (int i = 0; i < ev.Length; i++)
            {
                ev[i] = new T();
            }
        }

        public static void event_queue_destroy(ref Ref<BD_EVENT_QUEUE<T>> pp)
        {
            if (pp)
            {
                pp.Value.mutex.bd_mutex_destroy();
                pp.Free();
            }
        }

        public static Ref<BD_EVENT_QUEUE<T>> event_queue_new()
        {
            Ref<BD_EVENT_QUEUE<T>> eq = Ref<BD_EVENT_QUEUE<T>>.Allocate();
            eq.Value.mutex = new BD_MUTEX();
            return eq;
        } 

        public static bool event_queue_get(Ref<BD_EVENT_QUEUE<T>> eq, Ref<T> ev)
        {
            bool result = false;

            if (eq)
            {
                eq.Value.mutex.bd_mutex_lock();
                if (eq.Value._in != eq.Value._out)
                {
                    ev.Value = eq.Value.ev[eq.Value._out];
                    eq.Value._out = (eq.Value._out + 1) % MAX_EVENTS;
                    result = true;
                }
                else
                {
                    ev = default;
                }
                eq.Value.mutex.bd_mutex_unlock();
            }

            return result;
        }

        public static bool event_queue_put(Ref<BD_EVENT_QUEUE<T>> eq, Ref<T> ev)
        {
            bool result = false;

            if (eq)
            {
                eq.Value.mutex.bd_mutex_lock();

                uint new_in = (eq.Value._in + 1) & MAX_EVENTS;
                if (new_in != eq.Value._out)
                {
                    eq.Value.ev[eq.Value._in] = ev.Value;
                    eq.Value._in = new_in;

                    result = true;
                }

                eq.Value.mutex.bd_mutex_unlock();
            }

            return result;
        }

    }
}
