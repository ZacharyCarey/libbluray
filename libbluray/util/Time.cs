using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.util
{
    public readonly struct Time
    {
        private static DateTime t0;
        private static bool initialized = false;
        public static UInt64 bd_get_scr()
        {
            DateTime now = DateTime.Now;
            if (!initialized)
            {
                initialized = true;
                t0 = now;
            }

            TimeSpan delta = now - t0;
            return (UInt64)delta.TotalMilliseconds * 90;
        }

        public static Time Zero => new Time(0);

        public readonly UInt64 Ticks;
        public TimeSpan Value => TimeSpan.FromMilliseconds(Ticks / 90);
        public UInt64 Milliseconds => Ticks / 90;
        public UInt64 Seconds => Ticks / 90000;
        public UInt64 Minutes => Ticks / (90000 * 60);

        public Time(UInt64 ticks = 0)
        {
            Ticks = ticks;
        }

        public static bool operator ==(Time left, Time right)
        {
            return left.Ticks == right.Ticks;
        }
        public static bool operator !=(Time left, Time right)
        {
            return left.Ticks != right.Ticks;
        }

        public static bool operator <(Time left, Time right)
        {
            return left.Ticks < right.Ticks;
        }
        public static bool operator >(Time left, Time right)
        {
            return left.Ticks > right.Ticks;
        }

        public static bool operator <=(Time left, Time right)
        {
            return left.Ticks <= right.Ticks;
        }
        public static bool operator >=(Time left, Time right)
        {
            return left.Ticks >= right.Ticks;
        }

        public static Time operator +(Time left, Time right)
        {
            return new Time(left.Ticks + right.Ticks);
        }
        public static Time operator +(Time left, UInt64 right)
        {
            return new Time(left.Ticks + right);
        }
        public static Time operator +(UInt64 left, Time right)
        {
            return new Time(left + right.Ticks);
        }
        public static Time operator +(Time left, Int64 right)
        {
            return new Time((UInt64)((Int64)left.Ticks + right));
        }
        public static Time operator +(Int64 left, Time right)
        {
            return new Time((UInt64)(left + (Int64)right.Ticks));
        }


        public static Time operator -(Time left, Time right)
        {
            return new Time(left.Ticks - right.Ticks);
        }
        public static Time operator -(Time left, UInt64 right)
        {
            return new Time(left.Ticks - right);
        }
        public static Time operator -(UInt64 left, Time right)
        {
            return new Time(left - right.Ticks);
        }
        public static Time operator -(Time left, Int64 right)
        {
            return new Time((UInt64)((Int64)left.Ticks - right));
        }
        public static Time operator -(Int64 left, Time right)
        {
            return new Time((UInt64)(left - (Int64)right.Ticks));
        }

        public static Time operator *(Time left, UInt64 right)
        {
            return new Time(left.Ticks * right);
        }
        public static Time operator *(UInt64 left, Time right)
        {
            return new Time(left * right.Ticks);
        }
        public static Time operator *(Time left, Int64 right)
        {
            return new Time((UInt64)((Int64)left.Ticks * right));
        }
        public static Time operator *(Int64 left, Time right)
        {
            return new Time((UInt64)(left * (Int64)right.Ticks));
        }

        public static Time operator /(Time left, UInt64 right)
        {
            return new Time(left.Ticks / right);
        }
        public static Time operator /(UInt64 left, Time right)
        {
            return new Time(left / right.Ticks);
        }
        public static Time operator /(Time left, Int64 right)
        {
            return new Time((UInt64)((Int64)left.Ticks / right));
        }
        public static Time operator /(Int64 left, Time right)
        {
            return new Time((UInt64)(left / (Int64)right.Ticks));
        }

        public override string ToString()
        {
            UInt64 total_seconds = Ticks / 90000;
            UInt64 d_hours = total_seconds / 3600;
            UInt64 d_mins = (total_seconds % 3600) / 60;
            UInt64 d_secs = total_seconds % 60;
            // UInt64 d_msecs = (UInt64)(round((double)(duration % 90000) / 90));
            double d_msecs = Math.Floor(((double)(Ticks % 90000) / 90.0) + 0.5);

            return $"{d_hours:00}:{d_mins:00}:{d_secs:00}.{d_msecs:000.}";
        }
    }
}
