using libbluray;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.BlurayInfo
{
	public partial class BlurayInfo
	{
		public static UInt64 bluray_chapter_first_position(BLURAY bd, UInt32 title_ix, UInt32 chapter_ix)
		{


			bool retval = false;
			retval = BLURAY.bd_select_playlist(bd, title_ix);

			if (retval == false)
				return 0;

			BLURAY_TITLE_INFO? bluray_title_info = null;
			bluray_title_info = BLURAY.bd_get_playlist_info(bd, title_ix, 0);

			if (bluray_title_info == null)
				return 0;

			UInt32 chapter_number;
			chapter_number = chapter_ix + 1;

			if (chapter_number > bluray_title_info.ChapterCount)
				return 0;

			// libbluray.h has two functions to jump to a chapter and return a seek position.
			// The first one, bd_seek_chapter returns the seek position after jumping to it,
			// while the second one specifically is documented to return its start position.
			// To be safe, jump to the chapter first, although it may not be needed.
			BLURAY.bd_seek_chapter(bd, chapter_ix);

			UInt64 position;
			position = (UInt64)BLURAY.bd_chapter_pos(bd, chapter_ix);

			return position;
		}

		public static UInt64 bluray_chapter_last_position(BLURAY bd, UInt32 title_ix, UInt32 chapter_ix)
		{

			// Start with the first position. It's not safe to assume that it's
			// already been checked before this.
			UInt64 first_position = 0;
			first_position = bluray_chapter_first_position(bd, title_ix, chapter_ix);

			bool retval = false;
			retval = BLURAY.bd_select_playlist(bd, title_ix);

			if (retval == false)
				return 0;

			// Selecting other than the first angle is not supported right now
			UInt32 angle = 0;

			BLURAY_TITLE_INFO? bluray_title_info = null;
			bluray_title_info = BLURAY.bd_get_playlist_info(bd, title_ix, angle);

			if (bluray_title_info == null)
				return 0;

			UInt32 chapter_number;
			chapter_number = chapter_ix + 1;

			if (chapter_number > bluray_title_info.ChapterCount)
				return 0;

			UInt64 last_position = 0;

			// If only one chapter, or the final one, return the title size as the last position
			if (bluray_title_info.ChapterCount == 1 || chapter_number == bluray_title_info.ChapterCount)
			{
				// Casting this here makes me nervous, even though the highest a position
				last_position = BLURAY.bd_get_title_size(bd);
			}

			// If this not the final chapter, simply calculate the position against the
			// next chapter's position.
			if (chapter_number != bluray_title_info.ChapterCount)
			{
				last_position = bluray_chapter_first_position(bd, title_ix, chapter_ix + 1);
			}

			// This shouldn't happen
			if (last_position < first_position)
				last_position = first_position;

			return last_position;
		}

		/**
		 * In libbluray, number of bytes is the same value as the distance between positions.
		 *
		 * Each title has a padding of 768 bytes at its front, add it to the first chapter.
		 */

		public static UInt64 bluray_chapter_size(BLURAY bd, UInt32 title_ix, UInt32 chapter_ix)
		{

			UInt64 size;
			UInt64 first_position;
			UInt64 last_position;

			first_position = bluray_chapter_first_position(bd, title_ix, chapter_ix);
			last_position = bluray_chapter_last_position(bd, title_ix, chapter_ix);

			if (last_position < first_position)
				return 0;

			size = last_position - first_position;

			if (chapter_ix == 0)
				size += 768;

			return size;
		}
	}
}
