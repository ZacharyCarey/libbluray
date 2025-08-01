using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray.decoders
{

    public enum bd_overlay_plane_e
    {
        BD_OVERLAY_PG = 0,  /**< Presentation Graphics plane */
        BD_OVERLAY_IG = 1,  /**< Interactive Graphics plane (on top of PG plane) */
    }

    public enum bd_overlay_cmd_e
    {
        /* following events are executed immediately */
        BD_OVERLAY_INIT = 0,    /**< Initialize overlay plane. Size and position of plane in x,y,w,h. */
        BD_OVERLAY_CLOSE = 1,    /**< Close overlay plane */

        /* following events can be processed immediately, but changes
         * should not be flushed to display before next FLUSH event
         */
        BD_OVERLAY_CLEAR = 2,    /**< Clear overlay plane */
        BD_OVERLAY_DRAW = 3,    /**< Draw bitmap. Size and position within plane (x, y, w, h) and image (img, palette). */
        BD_OVERLAY_WIPE = 4,    /**< Clear area. Size and position within plane (x, y, w, h). */
        BD_OVERLAY_HIDE = 5,    /**< Overlay is empty and can be hidden */

        BD_OVERLAY_FLUSH = 6,    /**< All changes have been done, flush overlay to display at given pts */
    }

    public struct BD_PG_PALETTE_ENTRY
    {
        public byte Y;      /**< Y component  (16...235) */
        public byte Cr;     /**< Cr component (16...240) */
        public byte Cb;     /**< Cb component (16...240) */
        public byte T;      /**< Transparency ( 0...255). 0 - transparent, 255 - opaque. */

        public BD_PG_PALETTE_ENTRY() { }
    }

    public struct BD_PG_RLE_ELEM
    {
        public UInt16 len;   /**< RLE run length */
        public UInt16 color; /**< palette index */

        public BD_PG_RLE_ELEM() { }
    }

    public struct BD_OVERLAY
    {
        public const int BD_OVERLAY_INTERFACE_VERSION = 1;

        public UInt64 pts;   /**< Timestamp, on video grid */
        public byte plane; /**< Overlay plane (\ref bd_overlay_plane_e) */
        public byte cmd;   /**< Overlay event type (\ref bd_overlay_cmd_e) */

        public byte palette_update_flag; /**< Set if only overlay palette is changed */

        public UInt16 x;     /**< top-left x coordinate */
        public UInt16 y;     /**< top-left y coordinate */
        public UInt16 w;     /**< region width */
        public UInt16 h;     /**< region height */

        public Ref<BD_PG_PALETTE_ENTRY> palette = new(); /**< overlay palette (256 entries) */
        public Ref<BD_PG_RLE_ELEM> img = new();     /**< RLE-compressed overlay image */

        public BD_OVERLAY() { }
    }

    public enum bd_argb_overlay_cmd_e
    {
        /* following events are executed immediately */
        BD_ARGB_OVERLAY_INIT = 0,    /**< Initialize overlay plane. Size and position of plane are in x,y,w,h */
        BD_ARGB_OVERLAY_CLOSE = 1,    /**< Close overlay plane */

        /* following events can be processed immediately, but changes
         * should not be flushed to display before next FLUSH event
         */
        BD_ARGB_OVERLAY_DRAW = 3,    /**< Draw ARGB image on plane */
        BD_ARGB_OVERLAY_FLUSH = 6,    /**< All changes have been done, flush overlay to display at given pts */
    }

    public struct BD_ARGB_OVERLAY
    {
        public UInt64 pts;   /**< Event timestamp, on video grid */
        public byte plane; /**< Overlay plane (\ref bd_overlay_plane_e) */
        public byte cmd;   /**< Overlay event type (\ref bd_argb_overlay_cmd_e) */

        /* following fileds are used only when not using application-allocated
         * frame buffer
         */

        /* destination clip on the overlay plane */
        public UInt16 x;     /**< top-left x coordinate */
        public UInt16 y;     /**< top-left y coordinate */
        public UInt16 w;     /**< region width */
        public UInt16 h;     /**< region height */

        public UInt16 stride;       /**< ARGB buffer stride */
        public Ref<UInt32> argb = new(); /**< ARGB image data, 'h' lines, line stride 'stride' pixels */

        public BD_ARGB_OVERLAY() { }
    }

    public struct BD_ARGB_BUFFER
    {
        /* optional lock / unlock functions
         *  - Set by application
         *  - Called when buffer is accessed or modified
         */
        public Action<Ref<BD_ARGB_BUFFER>> _lock; /**< Lock (or prepare) buffer for writing */
        public Action<Ref<BD_ARGB_BUFFER>> unlock; /**< Unlock buffer (write complete) */

        /* ARGB frame buffers
         * - Allocated by application (BD_ARGB_OVERLAY_INIT).
         * - Buffer can be freed after BD_ARGB_OVERLAY_CLOSE.
         * - buffer can be replaced in overlay callback or lock().
         */

        public Ref<UInt32>[] buf = new Ref<uint>[4]; /**< [0] - PG plane, [1] - IG plane. [2], [3] reserved for stereoscopic overlay. */

        /* size of buffers
         * - Set by application
         * - If the buffer size is smaller than the size requested in BD_ARGB_OVERLAY_INIT,
         *   the buffer points only to the dirty area.
         */
        public int width;   /**< overlay buffer width (pixels) */
        public int height;  /**< overlay buffer height (pixels) */

        public struct Rect
        {
            public UInt16 x0; /**< top-left x coordinate */
            public UInt16 y0; /**< top-left y coordinate */
            public UInt16 x1; /**< bottom-down x coordinate  */
            public UInt16 y1; /**< bottom-down y coordinate */
        }

        /** Dirty area of frame buffers
         * - Updated by library before lock() call.
         * - Reset after each BD_ARGB_OVERLAY_FLUSH.
         */
        public Rect[] dirty = new Rect[2]; /**< [0] - PG plane, [1] - IG plane */

        public BD_ARGB_BUFFER() {
            for (int i = 0; i < dirty.Length; i++)
            {
                dirty[i] = new Rect();
            }
        }
    }
}
