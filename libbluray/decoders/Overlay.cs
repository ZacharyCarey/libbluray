using libbluray.util;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace libbluray.decoders
{
    /// <summary>
    /// Overlay plane. Sometimes referred to as a layer.
    /// </summary>
    public enum bd_overlay_plane_e
    {
        /// <summary>
        /// Presentation Graphics plane
        /// </summary>
        BD_OVERLAY_PG = 0,

        /// <summary>
        /// Interactive Graphics plane (on top of PG plane)
        /// </summary>
        BD_OVERLAY_IG = 1,  
    }

    /// <summary>
    /// YUV overlay event type
    /// </summary>
    public enum bd_overlay_cmd_e
    {
        /* following events are executed immediately */
        /// <summary>
        /// Executed immediately.
        /// Initialize overlay plane. Size and position of plane in x,y,w,h.
        /// </summary>
        BD_OVERLAY_INIT = 0,

        /// <summary>
        /// Executed immediately.
        /// Close overlay plane.
        /// </summary>
        BD_OVERLAY_CLOSE = 1,

        /* following events can be processed immediately, but changes
         * should not be flushed to display before next FLUSH event
         */
        /// <summary>
        /// Clear overlay plane
        /// </summary>
        BD_OVERLAY_CLEAR = 2,

        /// <summary>
        /// Draw bitmap. Size and position within plane (x, y, w, h) and image (img, palette).
        /// </summary>
        BD_OVERLAY_DRAW = 3,

        /// <summary>
        /// Clear area. Size and position within plane (x, y, w, h).
        /// </summary>
        BD_OVERLAY_WIPE = 4,

        /// <summary>
        /// Overlay is empty and can be hidden
        /// </summary>
        BD_OVERLAY_HIDE = 5,

        /// <summary>
        /// All changes have been done, flush overlay to display at given pts
        /// </summary>
        BD_OVERLAY_FLUSH = 6,    
    }

    /// <summary>
    /// Overlay palette entry.
    /// Y, Cr and Cb have the same color matrix as the associated video stream.
    /// Entry 0xff is always transparent.
    /// </summary>
    public struct BD_PG_PALETTE_ENTRY
    {
        /// <summary>
        /// Y component  (16...235)
        /// </summary>
        public byte Y;

        /// <summary>
        /// Cr component (16...240)
        /// </summary>
        public byte Cr;

        /// <summary>
        /// Cb component (16...240)
        /// </summary>
        public byte Cb;

        /// <summary>
        /// Transparency ( 0...255). 0 - transparent, 255 - opaque.
        /// </summary>
        public byte T;      

        public BD_PG_PALETTE_ENTRY() { }
    }

    /// <summary>
    /// RLE Element
    /// </summary>
    public struct BD_PG_RLE_ELEM
    {
        /// <summary>
        /// RLE run length
        /// </summary>
        public UInt16 len;

        /// <summary>
        /// palette index
        /// </summary>
        public UInt16 color; 

        public BD_PG_RLE_ELEM() { }
    }

    /// <summary>
    /// YUV overlay event
    /// </summary>
    public struct BD_OVERLAY
    {
        /// <summary>
        /// Version number of the interface described in this file.
        /// </summary>
        public const int BD_OVERLAY_INTERFACE_VERSION = 2;

        /// <summary>
        /// Timestamp, on video grid
        /// </summary>
        public UInt64 pts;

        /// <summary>
        /// Overlay plane (\ref bd_overlay_plane_e)
        /// </summary>
        public byte plane;

        /// <summary>
        /// Overlay event type (\ref bd_overlay_cmd_e)
        /// </summary>
        public byte cmd;

        /// <summary>
        /// Set if only overlay palette is changed
        /// </summary>
        public byte palette_update_flag;

        /// <summary>
        /// top-left x coordinate
        /// </summary>
        public UInt16 x;

        /// <summary>
        /// top-left y coordinate
        /// </summary>
        public UInt16 y;

        /// <summary>
        /// region width
        /// </summary>
        public UInt16 w;

        /// <summary>
        /// region height 
        /// </summary>
        public UInt16 h;

        /// <summary>
        /// overlay palette (256 entries)
        /// </summary>
        public Ref<BD_PG_PALETTE_ENTRY> palette = new();

        /// <summary>
        /// RLE-compressed overlay image
        /// </summary>
        public Ref<BD_PG_RLE_ELEM> img = new();     

        public BD_OVERLAY() { }
    }

    /// <summary>
    /// ARGB overlay event type
    /// </summary>
    public enum bd_argb_overlay_cmd_e
    {
        /* following events are executed immediately */
        /// <summary>
        /// Executed immediately.
        /// Initialize overlay plane. Size and position of plane are in x,y,w,h
        /// </summary>
        BD_ARGB_OVERLAY_INIT = 0,

        /// <summary>
        /// Executed immediately.
        /// Close overlay plane
        /// </summary>
        BD_ARGB_OVERLAY_CLOSE = 1,

        /* following events can be processed immediately, but changes
         * should not be flushed to display before next FLUSH event
         */
        /// <summary>
        /// Draw ARGB image on plane
        /// </summary>
        BD_ARGB_OVERLAY_DRAW = 3,

        /// <summary>
        /// All changes have been done, flush overlay to display at given pts
        /// </summary>
        BD_ARGB_OVERLAY_FLUSH = 6, 
    }

    /// <summary>
    /// ARGB overlay event
    /// </summary>
    public struct BD_ARGB_OVERLAY
    {
        /// <summary>
        /// Event timestamp, on video grid
        /// </summary>
        public UInt64 pts;

        /// <summary>
        /// Overlay plane (\ref bd_overlay_plane_e)
        /// </summary>
        public byte plane;

        /// <summary>
        /// Overlay event type (\ref bd_argb_overlay_cmd_e)
        /// </summary>
        public byte cmd;

        /* following fileds are used only when not using application-allocated
         * frame buffer
         */

        /* destination clip on the overlay plane */
        /// <summary>
        /// top-left x coordinate
        /// </summary>
        public UInt16 x;

        /// <summary>
        /// top-left y coordinate
        /// </summary>
        public UInt16 y;

        /// <summary>
        /// region width
        /// </summary>
        public UInt16 w;

        /// <summary>
        /// region height
        /// </summary>
        public UInt16 h;

        /// <summary>
        /// ARGB buffer stride
        /// </summary>
        public UInt16 stride;

        /// <summary>
        /// ARGB image data, 'h' lines, line stride 'stride' pixels
        /// </summary>
        public Ref<UInt32> argb = new(); 

        public BD_ARGB_OVERLAY() { }
    }

    /// <summary>
    /// Application-allocated frame buffer for ARGB overlays
    /// 
    /// When using application-allocated frame buffer DRAW events are
    /// executed by libbluray.
    /// Application needs to handle only OPEN/FLUSH/CLOSE events.
    /// 
    /// DRAW events can still be used for optimizations.
    /// </summary>
    public struct BD_ARGB_BUFFER
    {
        /* optional lock / unlock functions
         *  - Set by application
         *  - Called when buffer is accessed or modified
         */
        /// <summary>
        /// Lock (or prepare) buffer for writing
        /// </summary>
        public Action<Ref<BD_ARGB_BUFFER>> _lock;

        /// <summary>
        /// Unlock buffer (write complete)
        /// </summary>
        public Action<Ref<BD_ARGB_BUFFER>> unlock;

        /// <summary>
        /// ARGB frame buffers
        /// - Allocated by application(BD_ARGB_OVERLAY_INIT).
        /// - Buffer can be freed after BD_ARGB_OVERLAY_CLOSE.
        /// - buffer can be replaced in overlay callback or lock().
        /// 
        /// [0] - PG plane, [1] - IG plane. [2], [3] reserved for stereoscopic overlay.
        /// </summary>
        public Ref<UInt32>[] buf = new Ref<uint>[4];

        /* size of buffers
         * - Set by application
         * - If the buffer size is smaller than the size requested in BD_ARGB_OVERLAY_INIT,
         *   the buffer points only to the dirty area.
         */
        /// <summary>
        /// overlay buffer width (pixels)
        /// </summary>
        public int width;

        /// <summary>
        /// overlay buffer height (pixels) 
        /// </summary>
        public int height;  

        public struct Rect
        {
            /// <summary>
            /// top-left x coordinate
            /// </summary>
            public UInt16 x0;

            /// <summary>
            /// top-left y coordinate
            /// </summary>
            public UInt16 y0;

            /// <summary>
            /// bottom-down x coordinate
            /// </summary>
            public UInt16 x1;

            /// <summary>
            /// bottom-down y coordinate
            /// </summary>
            public UInt16 y1;
        }

        /// <summary>
        /// Dirty area of frame buffers
        /// - Updated by library before lock() call.
        /// - Reset after each BD_ARGB_OVERLAY_FLUSH.
        /// 
        /// [0] - PG plane, [1] - IG plane
        /// </summary>
        public Rect[] dirty = new Rect[2]; 

        public BD_ARGB_BUFFER() {
            for (int i = 0; i < dirty.Length; i++)
            {
                dirty[i] = new Rect();
            }
        }
    }
}
