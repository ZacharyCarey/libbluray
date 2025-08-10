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
        public static Time bluray_chapter_duration(BLURAY bd, UInt32 title_ix, UInt32 chapter_ix, byte angle_ix)
        {
            bool retval = false;
            retval = BLURAY.bd_select_title(bd, title_ix);

            if (retval == false)
                return Time.Zero;

            retval = BLURAY.bd_select_angle(bd, angle_ix);

            if (retval == false)
                return Time.Zero;

            BLURAY_TITLE_INFO? bluray_title_info = null;
            bluray_title_info = BLURAY.bd_get_playlist_info(bd, title_ix, angle_ix);

            if (bluray_title_info == null)
                return Time.Zero;

            UInt32 chapter_number;
            chapter_number = chapter_ix + 1;

            if (chapter_number > bluray_title_info.chapter_count)
                return Time.Zero;

            Ref<BLURAY_TITLE_CHAPTER> bd_chapter = Ref<BLURAY_TITLE_CHAPTER>.Null;
            bd_chapter = bluray_title_info.chapters.AtIndex(chapter_ix);

            return bd_chapter.Value.duration;
        }

    }
}
