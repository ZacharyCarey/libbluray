using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.util
{

    // Util functions imported from bluray_info-1.14
    public static class BlurayTime
    {

        public static UInt64 bluray_duration_seconds(UInt64 duration)
        {

            UInt64 seconds = duration / 90000;

            return seconds;

        }

        public static UInt64 bluray_duration_minutes(UInt64 duration)
        {

            UInt64 seconds = duration / 90000;
            UInt64 minutes = seconds / 60;

            return minutes;

        }

        public static void bluray_duration_length(out string str, UInt64 duration)
        {

            UInt64 msecs = duration / 90000;
            UInt64 d_hours = msecs / 3600;
            UInt64 d_mins = (msecs % 3600) / 60;
            UInt64 d_secs = msecs % 60;
            // UInt64 d_msecs = (UInt64)(round((double)(duration % 90000) / 90));
            double d_msecs = Math.Floor(((double)(duration % 90000) / 90.0) + 0.5);

            str = $"{d_hours:00}:{d_mins:00}:{d_secs:00}.{d_msecs:000.}";
        }

        public static void bluray_duration_length(out string str, TimeSpan duration)
        {
            // TotalSeconds % 1 returns the fractional component of seconds 
            double d_msecs = Math.Floor((duration.TotalSeconds % 1) * 1000 + 0.5);
            str = $"{duration.Hours:00}:{duration.Minutes:00}:{duration.Seconds:00}.{d_msecs:000.}";
        }

        public static TimeSpan ConvertRaw(UInt64 raw)
        {
            return TimeSpan.FromSeconds(raw / 90000.0);
        } 

        public static UInt64 bluray_chapter_duration(BLURAY bd, UInt32 title_ix, UInt32 chapter_ix, byte angle_ix)
        {
            bool retval = false;
            retval = BLURAY.bd_select_title(bd, title_ix);

            if (retval == false)
                return 0;

            retval = BLURAY.bd_select_angle(bd, angle_ix);

            if (retval == false)
                return 0;

            BLURAY_TITLE_INFO? bluray_title_info = null;
            bluray_title_info = BLURAY.bd_get_playlist_info(bd, title_ix, angle_ix);

            if (bluray_title_info == null)
                return 0;

            UInt32 chapter_number;
            chapter_number = chapter_ix + 1;

            if (chapter_number > bluray_title_info.chapter_count)
                return 0;

            Ref<BLURAY_TITLE_CHAPTER> bd_chapter = Ref<BLURAY_TITLE_CHAPTER>.Null;
            bd_chapter = bluray_title_info.chapters.AtIndex(chapter_ix);

            return bd_chapter.Value.duration;
        }

        public static void bluray_chapter_length(string dest_str, BLURAY bd, UInt32 title_ix, UInt32 chapter_ix, byte angle_ix)
        {
            UInt64 duration = 0;
            duration = bluray_chapter_duration(bd, title_ix, chapter_ix, angle_ix);
            bluray_duration_length(out string duration_str, duration);
            dest_str = duration_str;
        }


    }
}
