using libbluray;
using libbluray.bdnav;
using libbluray.decoders;
using libbluray.hdmv;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static libbluray.decoders.GraphicsController;

namespace libbluray.decoders
{
    internal struct GRAPHICS_CONTROLLER
    {
        public Ref<BD_REGISTERS> regs = new();

        public BD_MUTEX mutex = new();

        /* overlay output */
        public object? overlay_proc_handle;
        public gc_overlay_proc_f overlay_proc;

        /* state */
        public uint ig_open;
        public uint ig_drawn;
        public uint ig_dirty;
        public uint pg_open;
        public uint pg_drawn;
        public uint pg_dirty;
        public uint popup_visible;
        public uint valid_mouse_position;
        public uint auto_action_triggered;
        public BOG_DATA[] bog_data = new BOG_DATA[IgDecode.MAX_NUM_BOGS];
        public Ref<BOG_DATA> saved_bog_data = new();
        public BD_UO_MASK page_uo_mask = new();

        /* page effects */
        public uint effect_idx;
        public Ref<BD_IG_EFFECT_SEQUENCE> in_effects = new();
        public Ref<BD_IG_EFFECT_SEQUENCE> out_effects = new();

        /// <summary>
        /// 90 kHz
        /// </summary>
        public Int64 next_effect_time; 

        /* timers */
        public Int64 user_timeout;

        /* animated buttons */
        public uint frame_interval;
        public uint button_effect_running;
        public uint button_animation_running;

        /* data */
        public Ref<PG_DISPLAY_SET> pgs = new();
        public Ref<PG_DISPLAY_SET> igs = new();

        /// <summary>
        /// TextST
        /// </summary>
        public Ref<PG_DISPLAY_SET> tgs = new();  

        /* */
        public Ref<GRAPHICS_PROCESSOR> pgp = new();
        public Ref<GRAPHICS_PROCESSOR> igp = new();

        /// <summary>
        /// TextST
        /// </summary>
        public Ref<GRAPHICS_PROCESSOR> tgp = new();  

        /* */
        public Ref<TEXTST_RENDER> textst_render = new();
        public int next_dialog_idx;
        public int textst_user_style;

        public GRAPHICS_CONTROLLER() { 
            for (int i = 0; i > bog_data.Length; i++)
            {
                bog_data[i] = new();
            }        
        }
    }

    internal enum gc_ctrl_e
    {
        /* */
        GC_CTRL_INIT_MENU,

        /// <summary>
        /// No input. Render page / run timers / run animations
        /// </summary>
        GC_CTRL_NOP,

        /// <summary>
        /// reset graphics controller
        /// </summary>
        GC_CTRL_RESET,

        /* user input */
        /// <summary>
        /// param: bd_vk_key_e
        /// </summary>
        GC_CTRL_VK_KEY,

        /// <summary>
        /// move selected button to (x,y), param: (x<<16 | y)
        /// </summary>
        GC_CTRL_MOUSE_MOVE,

        /* HDMV VM control messages */
        /// <summary>
        /// param: button_id
        /// </summary>
        GC_CTRL_ENABLE_BUTTON,

        /// <summary>
        /// param: button_id
        /// </summary>
        GC_CTRL_DISABLE_BUTTON,  
        GC_CTRL_SET_BUTTON_PAGE,

        /// <summary>
        /// param: on/off
        /// </summary>
        GC_CTRL_POPUP,

        /// <summary>
        /// execution of IG object is complete
        /// </summary>
        GC_CTRL_IG_END,

        /* PG */
        /// <summary>
        /// render decoded PG composition
        /// </summary>
        GC_CTRL_PG_UPDATE,

        /// <summary>
        /// reset PG composition state
        /// </summary>
        GC_CTRL_PG_RESET,        

        /* TextST */
        GC_CTRL_PG_CHARCODE,

        /// <summary>
        /// select next TextST user style
        /// </summary>
        GC_CTRL_STYLE_SELECT,    
    }

    internal struct GC_NAV_CMDS
    {
        /* HDMV navigation command sequence */
        public int num_nav_cmds;
        public Ref<MOBJ_CMD> nav_cmds = new();

        /* Sound idx */
        public int sound_id_ref;

        /* graphics status (none, menu, popup) */
        public UInt32 status; /* bit mask */

        /* */
        public UInt32 wakeup_time;

        public BD_UO_MASK page_uo_mask = new();

        public GC_NAV_CMDS() { }
    }

    internal struct BOG_DATA
    {
        /// <summary>
        /// enabled button id
        /// </summary>
        public UInt16 enabled_button;

        /// <summary>
        /// button rect on overlay plane (if drawn)
        /// </summary>
        public UInt16 x, y, w, h;

        /// <summary>
        /// id of currently visible object
        /// </summary>
        public int visible_object_id;

        /// <summary>
        /// currently showing object index of animated button, < 0 for static buttons
        /// </summary>
        public int animate_indx;

        /// <summary>
        /// single-loop animation not yet complete 
        /// </summary>
        public int effect_running;  

        public BOG_DATA() { }
    }

    internal enum ButtonState
    {
        BTN_NORMAL,
        BTN_SELECTED,
        BTN_ACTIVATED
    }

    public static class GraphicsController
    {
        public delegate void gc_overlay_proc_f(object? a, Ref<BD_OVERLAY> b);

        internal const int GC_STATUS_NONE = 0;
        internal const int GC_STATUS_POPUP = 1;  /* popup menu loaded */
        internal const int GC_STATUS_MENU_OPEN = 2;  /* menu open */
        internal const int GC_STATUS_ANIMATE = 4;  /* animation or effect running */

        static void GC_ERROR(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Logging.bd_debug(DebugMaskEnum.DBG_GC | DebugMaskEnum.DBG_CRIT, msg, file, line);
        }

        static void GC_TRACE(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Logging.bd_debug(DebugMaskEnum.DBG_GC, msg, file, line);
        }

        /*
 * object lookup
 */

        static Ref<BD_PG_OBJECT> _find_object(Ref<PG_DISPLAY_SET> s, uint object_id)
        {
            uint ii;

            for (ii = 0; ii < s.Value.num_object; ii++)
            {
                if (s.Value._object[ii].id == object_id)
                {
                    return s.Value._object.AtIndex(ii);
                }
            }

            return Ref<BD_PG_OBJECT>.Null;
        }

        static Ref<BD_PG_PALETTE> _find_palette(Ref<PG_DISPLAY_SET> s, uint palette_id)
        {
            uint ii;

            for (ii = 0; ii < s.Value.num_palette; ii++)
            {
                if (s.Value.palette[ii].id == palette_id)
                {
                    return s.Value.palette.AtIndex(ii);
                }
            }

            return Ref<BD_PG_PALETTE>.Null;
        }

        static Ref<BD_IG_BUTTON> _find_button_bog(Ref<BD_IG_BOG> bog, uint button_id)
        {
            uint ii;

            for (ii = 0; ii < bog.Value.num_buttons; ii++)
            {
                if (bog.Value.button[ii].id == button_id)
                {
                    return bog.Value.button.AtIndex(ii);
                }
            }

            return Ref<BD_IG_BUTTON>.Null;
        }

        static Ref<BD_IG_BUTTON> _find_button_page(Ref<BD_IG_PAGE> page, uint button_id, Ref<uint> bog_idx)
        {
            uint ii;

            for (ii = 0; ii < page.Value.num_bogs; ii++)
            {
                Ref<BD_IG_BUTTON> button = _find_button_bog(page.Value.bog.AtIndex(ii), button_id);
                if (button)
                {
                    if (bog_idx)
                    {
                        bog_idx.Value = ii;
                    }
                    return button;
                }
            }

            return Ref<BD_IG_BUTTON>.Null;
        }

        static Ref<BD_IG_PAGE> _find_page(Ref<BD_IG_INTERACTIVE_COMPOSITION> c, uint page_id)
        {
            uint ii;

            for (ii = 0; ii < c.Value.num_pages; ii++)
            {
                if (c.Value.page[ii].id == page_id)
                {
                    return c.Value.page.AtIndex(ii);
                }
            }

            return Ref<BD_IG_PAGE>.Null;
        }

        static Ref<BD_PG_OBJECT> _find_object_for_button(Ref<PG_DISPLAY_SET> s,
                                                     Ref<BD_IG_BUTTON> button, ButtonState state,
                                                     Ref<BOG_DATA> bog_data)
        {
            Ref<BD_PG_OBJECT> _object = Ref<BD_PG_OBJECT>.Null;
            uint object_id = 0xffff;
            uint object_id_end = 0xffff;
            uint repeat = 0;

            switch (state)
            {
                case ButtonState.BTN_NORMAL:
                    object_id = button.Value.normal_start_object_id_ref;
                    object_id_end = button.Value.normal_end_object_id_ref;
                    repeat = button.Value.normal_repeat_flag;
                    break;
                case ButtonState.BTN_SELECTED:
                    object_id = button.Value.selected_start_object_id_ref;
                    object_id_end = button.Value.selected_end_object_id_ref;
                    repeat = button.Value.selected_repeat_flag;
                    break;
                case ButtonState.BTN_ACTIVATED:
                    object_id = button.Value.activated_start_object_id_ref;
                    object_id_end = button.Value.activated_end_object_id_ref;
                    break;
            }

            if (bog_data)
            {
                bog_data.Value.effect_running = 0;
                if (bog_data.Value.animate_indx >= 0)
                {
                    int range = (int)(object_id_end - object_id);

                    if (range > 0 && object_id < 0xffff && object_id_end < 0xffff)
                    {
                        GC_TRACE($"animate button #{button.Value.id}: animate_indx {bog_data.Value.animate_indx}, range {range}, repeat {repeat}");

                        object_id += (uint)(bog_data.Value.animate_indx % (range + 1));
                        bog_data.Value.animate_indx++;
                        if (repeat == 0)
                        {
                            if (bog_data.Value.animate_indx > range)
                            {
                                /* terminate animation to the last object */
                                bog_data.Value.animate_indx = -1;
                            }
                            else
                            {
                                bog_data.Value.effect_running = 1;
                            }
                        }
                    }
                    else
                    {
                        /* no animation for this button */
                        bog_data.Value.animate_indx = -1;
                    }
                }
                else
                {
                    if (object_id_end < 0xfffe)
                    {
                        object_id = object_id_end;
                    }
                }
            }

            _object = _find_object(s, object_id);

            return _object;
        }

        static Ref<BD_TEXTST_REGION_STYLE> _find_region_style(Ref<BD_TEXTST_DIALOG_STYLE> p, uint region_style_id)
        {
            uint ii;

            for (ii = 0; ii < p.Value.region_style_count; ii++)
            {
                if (p.Value.region_style[ii].region_style_id == region_style_id)
                {
                    return p.Value.region_style.AtIndex(ii);
                }
            }

            return Ref<BD_TEXTST_REGION_STYLE>.Null;
        }

        /*
         * util
         */

        static bool _areas_overlap(Ref<BOG_DATA> a, Ref<BOG_DATA> b)
        {
            return !(a.Value.x + a.Value.w <= b.Value.x ||
                     a.Value.x >= b.Value.x + b.Value.w ||
                     a.Value.y + a.Value.h <= b.Value.y ||
                     a.Value.y >= b.Value.y + b.Value.h);
        }

        static bool _is_button_enabled(Ref<GRAPHICS_CONTROLLER> gc, Ref<BD_IG_PAGE> page, uint button_id)
        {
            uint ii;
            for (ii = 0; ii < page.Value.num_bogs; ii++)
            {
                if (gc.Value.bog_data[ii].enabled_button == button_id)
                {
                    return true;
                }
            }
            return false;
        }

        static UInt16 _find_selected_button_id(Ref<GRAPHICS_CONTROLLER> gc)
        {
            /* executed when playback condition changes (ex. new page, popup-on, ...) */
            Ref<PG_DISPLAY_SET> s = gc.Value.igs;
            Ref<BD_IG_PAGE> page = Ref<BD_IG_PAGE>.Null;
            uint page_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID);
            uint button_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_SELECTED_BUTTON_ID);
            uint ii;

            page = _find_page(s.Value.ics.Value.interactive_composition.Ref, page_id);
            if (!page)
            {
                GC_TRACE($"_find_selected_button_id(): unknown page #{page_id} (have {s.Value.ics.Value.interactive_composition.Value.num_pages} pages)");
                return 0xffff;
            }

            /* run 5.9.8.3 */

            /* 1) always use page.Value.default_selected_button_id_ref if it is valid */
            if (_find_button_page(page, page.Value.default_selected_button_id_ref, Ref<uint>.Null) &&
                _is_button_enabled(gc, page, page.Value.default_selected_button_id_ref))
            {

                GC_TRACE($"_find_selected_button_id() .Value. default #{page.Value.default_selected_button_id_ref}");
                return page.Value.default_selected_button_id_ref;
            }

            /* 2) fallback to current PSR10 value if it is valid */
            for (ii = 0; ii < page.Value.num_bogs; ii++)
            {
                Ref<BD_IG_BOG> bog = page.Value.bog.AtIndex(ii);
                UInt16 enabled_button = gc.Value.bog_data[ii].enabled_button;

                if (button_id == enabled_button)
                {
                    if (_find_button_bog(bog, enabled_button))
                    {
                        GC_TRACE($"_find_selected_button_id() .Value. PSR10 #{enabled_button}");
                        return enabled_button;
                    }
                }
            }

            /* 3) fallback to find first valid_button_id_ref from page */
            for (ii = 0; ii < page.Value.num_bogs; ii++)
            {
                Ref<BD_IG_BOG> bog = page.Value.bog.AtIndex(ii);
                UInt16 enabled_button = gc.Value.bog_data[ii].enabled_button;

                if (_find_button_bog(bog, enabled_button))
                {
                    GC_TRACE($"_find_selected_button_id() .Value. first valid #{enabled_button}");
                    return enabled_button;
                }
            }

            GC_TRACE("_find_selected_button_id(): not found .Value. 0xffff\n");
            return 0xffff;
        }

        static void _reset_user_timeout(Ref<GRAPHICS_CONTROLLER> gc)
        {
            gc.Value.user_timeout = 0;

            if (gc.Value.igs.Value.ics.Value.interactive_composition.Value.ui_model == IgDecode.IG_UI_MODEL_POPUP ||
                Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID) != 0)
            {

                gc.Value.user_timeout = gc.Value.igs.Value.ics.Value.interactive_composition.Value.user_timeout_duration;
                if (gc.Value.user_timeout != 0)
                {
                    gc.Value.user_timeout += (long)Time.bd_get_scr();
                }
            }
        }

        static int _save_page_state(Ref<GRAPHICS_CONTROLLER> gc)
        {
            if (!gc.Value.igs || !gc.Value.igs.Value.ics)
            {
                GC_TRACE("_save_page_state(): no IG composition\n");
                return -1;
            }

            Ref<PG_DISPLAY_SET> s = gc.Value.igs;
            Ref<BD_IG_PAGE> page = Ref<BD_IG_PAGE>.Null;
            uint page_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID);
            uint ii;

            page = _find_page(s.Value.ics.Value.interactive_composition.Ref, page_id);
            if (!page)
            {
                GC_ERROR($"_save_page_state(): unknown page #{page_id} (have {s.Value.ics.Value.interactive_composition.Value.num_pages} pages)");
                return -1;
            }

            /* copy enabled button state, clear draw state */

            gc.Value.saved_bog_data.Free();
            gc.Value.saved_bog_data = Ref<BOG_DATA>.Allocate(gc.Value.bog_data.Length);

            for (ii = 0; ii < page.Value.num_bogs; ii++)
            {
                gc.Value.saved_bog_data[ii].enabled_button = gc.Value.bog_data[ii].enabled_button;
                gc.Value.saved_bog_data[ii].animate_indx = gc.Value.bog_data[ii].animate_indx >= 0 ? 0 : -1;
            }

            return 1;
        }

        static int _restore_page_state(Ref<GRAPHICS_CONTROLLER> gc)
        {
            gc.Value.in_effects = Ref<BD_IG_EFFECT_SEQUENCE>.Null;
            gc.Value.out_effects = Ref<BD_IG_EFFECT_SEQUENCE>.Null;

            if (gc.Value.saved_bog_data)
            {
                gc.Value.saved_bog_data.AsSpan().Slice(0, gc.Value.bog_data.Length).CopyTo(gc.Value.bog_data);
                gc.Value.saved_bog_data.Free();
                return 1;
            }
            return -1;
        }

        // Animation frame rate
        static uint[] frame_interval = {
            0,
            90000 / 1001 * 24,
            90000 / 1000 * 24,
            90000 / 1000 * 25,
            90000 / 1001 * 30,
            90000 / 1000 * 50,
            90000 / 1001 * 60,
        };

        static void _reset_page_state(Ref<GRAPHICS_CONTROLLER> gc)
        {
            Ref<PG_DISPLAY_SET> s = gc.Value.igs;
            Ref<BD_IG_PAGE> page = Ref<BD_IG_PAGE>.Null;
            uint page_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID);
            uint ii;

            page = _find_page(s.Value.ics.Value.interactive_composition.Ref, page_id);
            if (!page)
            {
                GC_ERROR($"_reset_page_state(): unknown page #{page_id} (have {s.Value.ics.Value.interactive_composition.Value.num_pages} pages)");
                return;
            }

            gc.Value.bog_data.AsSpan().Fill(new());

            for (ii = 0; ii < page.Value.num_bogs; ii++)
            {
                gc.Value.bog_data[ii].enabled_button = page.Value.bog[ii].default_valid_button_id_ref;
                gc.Value.bog_data[ii].animate_indx = 0;
                gc.Value.bog_data[ii].visible_object_id = -1;
            }

            /* animation frame rate */
            gc.Value.frame_interval = frame_interval[s.Value.ics.Value.video_descriptor.Value.frame_rate] * (page.Value.animation_frame_rate_code + 1u);

            /* effects */
            gc.Value.effect_idx = 0;
            gc.Value.in_effects = Ref<BD_IG_EFFECT_SEQUENCE>.Null;
            gc.Value.out_effects = Ref<BD_IG_EFFECT_SEQUENCE>.Null;

            /* timers */
            _reset_user_timeout(gc);
        }

        /*
         * overlay operations
         */

        static void _open_osd(Ref<GRAPHICS_CONTROLLER> gc, int plane,
                              uint x0, uint y0,
                              uint width, uint height)
        {
            if (gc.Value.overlay_proc != null)
            {
                Variable<BD_OVERLAY> ov = new();
                ov.Value.cmd = (byte)bd_overlay_cmd_e.BD_OVERLAY_INIT;
                ov.Value.pts = ulong.MaxValue;
                ov.Value.plane = (byte)plane;
                ov.Value.x = (ushort)x0;
                ov.Value.y = (ushort)y0;
                ov.Value.w = (ushort)width;
                ov.Value.h = (ushort)height;

                gc.Value.overlay_proc(gc.Value.overlay_proc_handle, ov.Ref);

                if (plane == (int)bd_overlay_plane_e.BD_OVERLAY_IG)
                {
                    gc.Value.ig_open = 1;
                }
                else
                {
                    gc.Value.pg_open = 1;
                }
            }
        }

        static void _close_osd(Ref<GRAPHICS_CONTROLLER> gc, int plane)
        {
            if (gc.Value.overlay_proc != null)
            {
                Variable<BD_OVERLAY> ov = new();
                ov.Value.cmd = (byte)bd_overlay_cmd_e.BD_OVERLAY_CLOSE;
                ov.Value.pts = ulong.MaxValue;
                ov.Value.plane = (byte)plane;

                gc.Value.overlay_proc(gc.Value.overlay_proc_handle, ov.Ref);
            }

            if (plane == (int)bd_overlay_plane_e.BD_OVERLAY_IG)
            {
                gc.Value.ig_open = 0;
                gc.Value.ig_drawn = 0;
            }
            else
            {
                gc.Value.pg_open = 0;
                gc.Value.pg_drawn = 0;
            }
        }

        static void _flush_osd(Ref<GRAPHICS_CONTROLLER> gc, int plane, Int64 pts)
        {
            if (gc.Value.overlay_proc != null)
            {
                Variable<BD_OVERLAY> ov = new();
                ov.Value.cmd = (byte)bd_overlay_cmd_e.BD_OVERLAY_FLUSH;
                ov.Value.pts = (ulong)pts;
                ov.Value.plane = (byte)plane;

                gc.Value.overlay_proc(gc.Value.overlay_proc_handle, ov.Ref);
            }
        }

        static void _hide_osd(Ref<GRAPHICS_CONTROLLER> gc, int plane)
        {
            if (gc.Value.overlay_proc != null)
            {
                Variable<BD_OVERLAY> ov = new();
                ov.Value.cmd = (byte)bd_overlay_cmd_e.BD_OVERLAY_HIDE;
                ov.Value.plane = (byte)plane;

                gc.Value.overlay_proc(gc.Value.overlay_proc_handle, ov.Ref);
            }
        }

        static void _clear_osd_area(Ref<GRAPHICS_CONTROLLER> gc, int plane, Int64 pts,
                                    UInt16 x, UInt16 y, UInt16 w, UInt16 h)
        {
            if (gc.Value.overlay_proc != null)
            {
                /* wipe area */
                Variable<BD_OVERLAY> ov = new();
                ov.Value.cmd = (byte)bd_overlay_cmd_e.BD_OVERLAY_WIPE;
                ov.Value.pts = (ulong)pts;
                ov.Value.plane = (byte)plane;
                ov.Value.x = x;
                ov.Value.y = y;
                ov.Value.w = w;
                ov.Value.h = h;

                gc.Value.overlay_proc(gc.Value.overlay_proc_handle, ov.Ref);
            }
        }

        static void _clear_osd(Ref<GRAPHICS_CONTROLLER> gc, int plane)
        {
            if (gc.Value.overlay_proc != null)
            {
                /* clear plane */
                Variable<BD_OVERLAY> ov = new();
                ov.Value.cmd = (byte)bd_overlay_cmd_e.BD_OVERLAY_CLEAR;
                ov.Value.pts = ulong.MaxValue;
                ov.Value.plane = (byte)plane;

                gc.Value.overlay_proc(gc.Value.overlay_proc_handle, ov.Ref);
            }

            if (plane == (int)bd_overlay_plane_e.BD_OVERLAY_IG)
            {
                gc.Value.ig_drawn = 0;
            }
            else
            {
                gc.Value.pg_drawn = 0;
            }
        }

        static void _clear_bog_area(Ref<GRAPHICS_CONTROLLER> gc, Ref<BOG_DATA> bog_data)
        {
            if (gc.Value.ig_drawn != 0 && bog_data.Value.w != 0 && bog_data.Value.h != 0)
            {

                _clear_osd_area(gc, (int)bd_overlay_plane_e.BD_OVERLAY_IG, -1, bog_data.Value.x, bog_data.Value.y, bog_data.Value.w, bog_data.Value.h);

                bog_data.Value.x = bog_data.Value.y = bog_data.Value.w = bog_data.Value.h = 0;
                bog_data.Value.visible_object_id = -1;

                gc.Value.ig_dirty = 1;
            }
        }

        static void _render_object(Ref<GRAPHICS_CONTROLLER> gc,
                                   Int64 pts, uint plane,
                                   UInt16 x, UInt16 y,
                                   Ref<BD_PG_OBJECT> _object,
                                   Ref<BD_PG_PALETTE> palette)
        {
            if (gc.Value.overlay_proc != null)
            {
                Variable<BD_OVERLAY> ov = new();
                ov.Value.cmd = (byte)bd_overlay_cmd_e.BD_OVERLAY_DRAW;
                ov.Value.pts = (ulong)pts;
                ov.Value.plane = (byte)plane;
                ov.Value.x = x;
                ov.Value.y = y;
                ov.Value.w = _object.Value.width;
                ov.Value.h = _object.Value.height;
                ov.Value.palette = new Ref<BD_PG_PALETTE_ENTRY>(palette.Value.entry);
                ov.Value.img = _object.Value.img;

                gc.Value.overlay_proc(gc.Value.overlay_proc_handle, ov.Ref);
            }
        }

        static void _render_composition_object(Ref<GRAPHICS_CONTROLLER> gc,
                                               Int64 pts, uint plane,
                                               Ref<BD_PG_COMPOSITION_OBJECT> cobj,
                                               Ref<BD_PG_OBJECT> _object,
                                               Ref<BD_PG_PALETTE> palette,
                                               int palette_update_flag)
        {
            if (gc.Value.overlay_proc != null)
            {
                Ref<BD_PG_RLE_ELEM> cropped_img = Ref<BD_PG_RLE_ELEM>.Null;
                Variable<BD_OVERLAY> ov = new();
                ov.Value.cmd = (byte)bd_overlay_cmd_e.BD_OVERLAY_DRAW;
                ov.Value.pts = (ulong)pts;
                ov.Value.plane = (byte)plane;
                ov.Value.x = cobj.Value.x;
                ov.Value.y = cobj.Value.y;
                ov.Value.w = _object.Value.width;
                ov.Value.h = _object.Value.height;
                ov.Value.palette = new Ref<BD_PG_PALETTE_ENTRY>(palette.Value.entry);
                ov.Value.img = _object.Value.img;

                if (cobj.Value.crop_flag != 0)
                {
                    if (cobj.Value.crop_x != 0 || cobj.Value.crop_y != 0 || cobj.Value.crop_w != _object.Value.width)
                    {
                        cropped_img = RLE.rle_crop_object(_object.Value.img, _object.Value.width,
                                                      cobj.Value.crop_x, cobj.Value.crop_y, cobj.Value.crop_w, cobj.Value.crop_h);
                        if (!cropped_img)
                        {
                            Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, $"Error cropping PG object");
                            return;
                        }
                        ov.Value.img = cropped_img;
                    }
                    ov.Value.w = cobj.Value.crop_w;
                    ov.Value.h = cobj.Value.crop_h;
                }

                ov.Value.palette_update_flag = (byte)palette_update_flag;

                gc.Value.overlay_proc(gc.Value.overlay_proc_handle, ov.Ref);

                //refcnt_dec(cropped_img);
            }
        }

        static void _render_rle(Ref<GRAPHICS_CONTROLLER> gc,
                                Int64 pts,
                                Ref<BD_PG_RLE_ELEM> img,
                                UInt16 x, UInt16 y,
                                UInt16 width, UInt16 height,
                                Ref<BD_PG_PALETTE_ENTRY> palette)
        {
            if (gc.Value.overlay_proc != null)
            {
                Variable<BD_OVERLAY> ov = new();
                ov.Value.cmd = (byte)bd_overlay_cmd_e.BD_OVERLAY_DRAW;
                ov.Value.pts = (ulong)pts;
                ov.Value.plane = (byte)bd_overlay_plane_e.BD_OVERLAY_PG;
                ov.Value.x = x;
                ov.Value.y = y;
                ov.Value.w = width;
                ov.Value.h = height;
                ov.Value.palette = palette;
                ov.Value.img = img;

                gc.Value.overlay_proc(gc.Value.overlay_proc_handle, ov.Ref);
            }
        }

        /*
         * page selection and IG effects
         */

        static void _select_button(Ref<GRAPHICS_CONTROLLER> gc, UInt32 button_id)
        {
            Ref<BD_IG_PAGE> page = Ref<BD_IG_PAGE>.Null;
            uint page_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID);
            Variable<uint> bog_idx = new();

            /* reset animation */
            page = _find_page(gc.Value.igs.Value.ics.Value.interactive_composition.Ref, page_id);
            if (page && _find_button_page(page, button_id, bog_idx.Ref))
            {
                gc.Value.bog_data[bog_idx.Value].animate_indx = 0;
                gc.Value.next_effect_time = (long)Time.bd_get_scr();
            }

            /* select page */
            Register.bd_psr_write(gc.Value.regs, bd_psr_idx.PSR_SELECTED_BUTTON_ID, button_id);
            gc.Value.auto_action_triggered = 0;
        }

        static void _select_page(Ref<GRAPHICS_CONTROLLER> gc, UInt16 page_id, int out_effects)
        {
            uint cur_page_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID);
            Ref<BD_IG_PAGE> page = Ref<BD_IG_PAGE>.Null;

            Register.bd_psr_write(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID, page_id);

            _reset_page_state(gc);

            UInt16 button_id = _find_selected_button_id(gc);
            _select_button(gc, button_id);

            gc.Value.valid_mouse_position = 0;

            if (out_effects != 0)
            {
                page = _find_page(gc.Value.igs.Value.ics.Value.interactive_composition.Ref, cur_page_id);
                if (page && page.Value.out_effects.Value.num_effects != 0)
                {
                    gc.Value.next_effect_time = (long)Time.bd_get_scr();
                    gc.Value.out_effects = page.Value.out_effects.Ref;
                }
            }

            page = _find_page(gc.Value.igs.Value.ics.Value.interactive_composition.Ref, page_id);
            if (page && page.Value.in_effects.Value.num_effects != 0)
            {
                gc.Value.next_effect_time = (long)Time.bd_get_scr();
                gc.Value.in_effects = page.Value.in_effects.Ref;
            }

            if (gc.Value.ig_open != 0 && !gc.Value.out_effects)
            {
                _clear_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_IG);
            }
        }

        static void _gc_reset(Ref<GRAPHICS_CONTROLLER> gc)
        {
            if (gc.Value.pg_open != 0)
            {
                _close_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_PG);
            }
            if (gc.Value.ig_open != 0)
            {
                _close_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_IG);
            }

            gc.Value.popup_visible = 0;
            gc.Value.valid_mouse_position = 0;
            gc.Value.page_uo_mask = new BD_UO_MASK();

            GraphicsProcessor.graphics_processor_free(ref gc.Value.igp);
            GraphicsProcessor.graphics_processor_free(ref gc.Value.pgp);
            GraphicsProcessor.graphics_processor_free(ref gc.Value.tgp);

            GraphicsProcessor.pg_display_set_free(ref gc.Value.pgs);
            GraphicsProcessor.pg_display_set_free(ref gc.Value.igs);
            GraphicsProcessor.pg_display_set_free(ref gc.Value.tgs);

            TextstRender.textst_render_free(ref gc.Value.textst_render);
            gc.Value.next_dialog_idx = 0;
            gc.Value.textst_user_style = -1;

            Array.Fill(gc.Value.bog_data, new());
        }

        /*
         * register hook
         */
        static void _process_psr_event(object handle, Ref<BD_PSR_EVENT> ev)
        {
            Ref<GRAPHICS_CONTROLLER> gc = (Ref<GRAPHICS_CONTROLLER>)handle;

            if (ev.Value.ev_type == (uint)Register.BD_PSR_SAVE)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_GC, $"PSR SAVE event");

                /* save menu page state */
                gc.Value.mutex.bd_mutex_lock();
                _save_page_state(gc);
                gc.Value.mutex.bd_mutex_unlock();

                return;
            }

            if (ev.Value.ev_type == (uint)Register.BD_PSR_RESTORE)
            {
                switch (ev.Value.psr_idx)
                {

                    case bd_psr_idx.PSR_SELECTED_BUTTON_ID:
                        return;

                    case bd_psr_idx.PSR_MENU_PAGE_ID:
                        /* restore menus */
                        gc.Value.mutex.bd_mutex_lock();
                        _restore_page_state(gc);
                        gc.Value.mutex.bd_mutex_unlock();
                        return;

                    default:
                        /* others: ignore */
                        return;
                }
            }
        }

        /*
         * init / free
         */

        internal static Ref<GRAPHICS_CONTROLLER> gc_init(Ref<BD_REGISTERS> regs, object? handle, gc_overlay_proc_f func)
        {
            Ref<GRAPHICS_CONTROLLER> p = Ref<GRAPHICS_CONTROLLER>.Allocate();

            p.Value.regs = regs;

            p.Value.overlay_proc_handle = handle;
            p.Value.overlay_proc = func;

            p.Value.mutex = new();//.bd_mutex_init();

            Register.bd_psr_register_cb(regs, _process_psr_event, p);

            p.Value.textst_user_style = -1;

            return p;
        }

        internal static void gc_free(ref Ref<GRAPHICS_CONTROLLER> p)
        {
            if (p)
            {

                Ref<GRAPHICS_CONTROLLER> gc = p;

                Register.bd_psr_unregister_cb(gc.Value.regs, _process_psr_event, gc);

                _gc_reset(gc);

                if (gc.Value.overlay_proc != null)
                {
                    gc.Value.overlay_proc(gc.Value.overlay_proc_handle, Ref<BD_OVERLAY>.Null);
                }

                gc.Value.mutex.bd_mutex_destroy();

                gc.Value.saved_bog_data.Free();

                p.Free();
            }
        }

        /*
         * graphics stream input
         */

        /// <summary>
        /// Decode data from MPEG-TS input stream
        /// </summary>
        /// <param name="gc">GRAPHICS_CONTROLLER object</param>
        /// <param name="pid">mpeg-ts PID to decode (HDMV IG/PG stream)</param>
        /// <param name="block">mpeg-ts data</param>
        /// <param name="num_blocks">number of aligned units in data</param>
        /// <param name="stc">current playback time</param>
        /// <returns> less than 0 on error, 0 when not complete,  greater than 0 when complete</returns>
        internal static int gc_decode_ts(Ref<GRAPHICS_CONTROLLER> gc, UInt16 pid, Ref<byte> block, uint num_blocks, Int64 stc)
        {
            if (!gc)
            {
                GC_TRACE("gc_decode_ts(): no graphics controller\n");
                return -1;
            }
            
            if (HdmvPIDs.IS_HDMV_PID_IG(pid))
            {
                /* IG stream */

                if (!gc.Value.igp)
                {
                    gc.Value.igp = GraphicsProcessor.graphics_processor_init();
                    if (!gc.Value.igp)
                    {
                        return -1;
                    }
                }

                gc.Value.mutex.bd_mutex_lock();

                if (!GraphicsProcessor.graphics_processor_decode_ts(gc.Value.igp, ref gc.Value.igs,
                                                  pid, block, num_blocks,
                                                  stc))
                {
                    /* no new complete display set */
                    gc.Value.mutex.bd_mutex_unlock();
                    return 0;
                }

                if (!gc.Value.igs || gc.Value.igs.Value.complete == 0)
                {
                    gc.Value.mutex.bd_mutex_unlock();
                    return 0;
                }

                /* TODO: */
                if (gc.Value.igs.Value.ics)
                {
                    if (gc.Value.igs.Value.ics.Value.interactive_composition.Value.composition_timeout_pts > 0)
                    {
                        GC_TRACE("gc_decode_ts(): IG composition_timeout_pts not implemented");
                    }
                    if (gc.Value.igs.Value.ics.Value.interactive_composition.Value.selection_timeout_pts != 0)
                    {
                        GC_TRACE("gc_decode_ts(): IG selection_timeout_pts not implemented");
                    }
                    if (gc.Value.igs.Value.ics.Value.interactive_composition.Value.user_timeout_duration != 0)
                    {
                        GC_TRACE($"gc_decode_ts(): IG user_timeout_duration {gc.Value.igs.Value.ics.Value.interactive_composition.Value.user_timeout_duration}");
                    }
                }

                gc.Value.mutex.bd_mutex_unlock();

                return 1;
            }

            else if (HdmvPIDs.IS_HDMV_PID_PG(pid))
            {
                /* PG stream */
                if (!gc.Value.pgp)
                {
                    gc.Value.pgp = GraphicsProcessor.graphics_processor_init();
                    if (!gc.Value.pgp)
                    {
                        return -1;
                    }
                }
                GraphicsProcessor.graphics_processor_decode_ts(gc.Value.pgp, ref gc.Value.pgs,
                                             pid, block, num_blocks,
                                             stc);

                if (!gc.Value.pgs || gc.Value.pgs.Value.complete == 0)
                {
                    return 0;
                }

                return 1;
            }

            else if (HdmvPIDs.IS_HDMV_PID_TEXTST(pid))
            {
                /* TextST stream */
                if (!gc.Value.tgp)
                {
                    gc.Value.tgp = GraphicsProcessor.graphics_processor_init();
                    if (!gc.Value.tgp)
                    {
                        return -1;
                    }
                }
                GraphicsProcessor.graphics_processor_decode_ts(gc.Value.tgp, ref gc.Value.tgs,
                                             pid, block, num_blocks,
                                             stc);

                if (!gc.Value.tgs || gc.Value.tgs.Value.complete == 0)
                {
                    return 0;
                }

                return 1;
            }

            return -1;
        }

        /*
         * TextST rendering
         */

        static int _textst_style_select(Ref<GRAPHICS_CONTROLLER> p, int user_style_idx)
        {
            p.Value.textst_user_style = user_style_idx;

            GC_ERROR("User style selection not implemented");
            return -1;
        }

        /// <summary>
        /// Add TextST font
        /// </summary>
        /// <param name="p"></param>
        /// <param name="data"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        internal static int gc_add_font(Ref<GRAPHICS_CONTROLLER> p, object? data, UInt64 size)
        {
            if (!p)
            {
                return -1;
            }

            if (data == null)
            {
                TextstRender.textst_render_free(ref p.Value.textst_render);
                return 0;
            }

            if (!p.Value.textst_render)
            {
                p.Value.textst_render = TextstRender.textst_render_init();
                if (!p.Value.textst_render)
                {
                    return -1;
                }
            }

            return TextstRender.textst_render_add_font(p.Value.textst_render, data, size);
        }

        static int _render_textst_region(Ref<GRAPHICS_CONTROLLER> p, Int64 pts, Ref<BD_TEXTST_REGION_STYLE> style, Ref<TEXTST_BITMAP> bmp, Ref<BD_PG_PALETTE_ENTRY> palette)
        {
            uint bmp_y;
            UInt16 y;
            Variable<RLE_ENC> rle = new();

            if (RLE.rle_begin(rle.Ref) < 0)
            {
                return -1;
            }

            for (y = 0, bmp_y = 0; y < style.Value.region_info.Value.region.Value.height; y++)
            {
                if (y < style.Value.text_box.Value.ypos || y >= style.Value.text_box.Value.ypos + style.Value.text_box.Value.height)
                {
                    if (RLE.rle_add_bite(rle.Ref, style.Value.region_info.Value.background_color, style.Value.region_info.Value.region.Value.width) < 0)
                        break;
                }
                else
                {
                    if (RLE.rle_add_bite(rle.Ref, style.Value.region_info.Value.background_color, style.Value.text_box.Value.xpos) < 0)
                        break;
                    if (RLE.rle_compress_chunk(rle.Ref, bmp.Value.mem + bmp.Value.stride * bmp_y, bmp.Value.width) < 0)
                        break;
                    bmp_y++;
                    if (RLE.rle_add_bite(rle.Ref, style.Value.region_info.Value.background_color,
                                     style.Value.region_info.Value.region.Value.width - style.Value.text_box.Value.width - style.Value.text_box.Value.xpos) < 0)
                        break;
                }

                if (RLE.rle_add_eol(rle.Ref) < 0)
                    break;
            }

            Ref<BD_PG_RLE_ELEM> img = RLE.rle_get(rle.Ref);
            if (img)
            {
                _render_rle(p, pts, img,
                            style.Value.region_info.Value.region.Value.xpos, style.Value.region_info.Value.region.Value.ypos,
                            style.Value.region_info.Value.region.Value.width, style.Value.region_info.Value.region.Value.height,
                            palette);
            }
            else
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DECODE | DebugMaskEnum.DBG_CRIT, $"Error encoding Text Subtitle region");
            }

            RLE.rle_end(rle.Ref);

            return 0;
        }

        static int _render_textst(Ref<GRAPHICS_CONTROLLER> p, UInt32 stc, Ref<GC_NAV_CMDS> cmds)
        {
            Ref<BD_TEXTST_DIALOG_PRESENTATION> dialog = Ref<BD_TEXTST_DIALOG_PRESENTATION>.Null;
            Ref<PG_DISPLAY_SET> s = p.Value.tgs;
            Int64 now = ((Int64)stc) << 1;
            uint ii, jj;

            if (!s || !s.Value.dialog || !s.Value.style)
            {
                GC_ERROR("_render_textst(): no TextST decoded\n");
                return -1;
            }
            if (!p.Value.textst_render)
            {
                GC_ERROR("_render_textst(): no TextST renderer (missing fonts ?)\n");
                return -1;
            }

            dialog = s.Value.dialog;

            /* loop over all matching dialogs */
            for (ii = (uint)p.Value.next_dialog_idx; ii < s.Value.num_dialog; ii++)
            {

                /* next dialog too far in future ? */
                if (now < 1 || dialog[ii].start_pts >= now + 90000)
                {
                    GC_TRACE($"_render_textst(): next event #{ii} in {((dialog[ii].start_pts - now) / 90000)} seconds (pts {dialog[ii].start_pts})");
                    if (cmds)
                    {
                        cmds.Value.wakeup_time = (UInt32)(dialog[ii].start_pts / 2);
                    }
                    return 1;
                }

                p.Value.next_dialog_idx = (int)(ii + 1);

                /* too late ? */
                if (dialog[ii].start_pts < now - 45000)
                {
                    GC_TRACE($"_render_textst(): not showing #{ii} (start time passed)");
                    continue;
                }
                if (dialog[ii].end_pts < now)
                {
                    GC_TRACE($"_render_textst(): not showing #{ii} (hide time passed)");
                    continue;
                }

                if (dialog[ii].palette_update)
                {
                    GC_ERROR($"_render_textst(): Palette update not implemented");
                    continue;
                }

                GC_TRACE($"_render_textst(): rendering dialog #{ii} (pts {dialog[ii].start_pts}, diff {(dialog[ii].start_pts - now)}");


                if (dialog[ii].region_count == 0)
                {
                    continue;
                }

                /* TODO: */
                if (dialog[ii].region_count > 1)
                {
                    GC_ERROR("_render_textst(): Multiple regions not supported\n");
                }

                /* open PG overlay */
                if (p.Value.pg_open == 0)
                {
                    _open_osd(p, (int)bd_overlay_plane_e.BD_OVERLAY_PG, 0, 0, 1920, 1080);
                }

                /* render all regions */
                for (jj = 0; jj < dialog[ii].region_count; jj++)
                {

                    Ref<BD_TEXTST_DIALOG_REGION> region = new Ref<BD_TEXTST_DIALOG_REGION>(dialog[ii].region).AtIndex(jj);
                    Ref<BD_TEXTST_REGION_STYLE> style = Ref<BD_TEXTST_REGION_STYLE>.Null;

                    // TODO:
                    if (region.Value.continous_present_flag != 0)
                    {
                        GC_ERROR("_render_textst(): continous_present_flag: not implemented");
                    }
                    if (region.Value.forced_on_flag != 0)
                    {
                        GC_ERROR("_render_textst(): forced_on_flag: not implemented");
                    }

                    style = _find_region_style(s.Value.style, region.Value.region_style_id_ref);
                    if (!style)
                    {
                        GC_ERROR($"_render_textst: region style #{region.Value.region_style_id_ref} not found");
                        continue;
                    }

                    Variable<TEXTST_BITMAP> bmp = new();
                    bmp.Value.mem = Ref<byte>.Null;
                    bmp.Value.width = style.Value.text_box.Value.width;
                    bmp.Value.height = style.Value.text_box.Value.height;
                    bmp.Value.stride = style.Value.text_box.Value.width;
                    bmp.Value.argb = 0;

                    bmp.Value.mem = Ref<byte>.Allocate(bmp.Value.width * bmp.Value.height);

                    bmp.Value.mem.AsSpan().Slice(0, bmp.Value.width * bmp.Value.height).Fill(style.Value.region_info.Value.background_color);
                    TextstRender.textst_render(p.Value.textst_render, bmp.Ref, style, region);
                    _render_textst_region(p, dialog[ii].start_pts, style, bmp.Ref, new Ref<BD_PG_PALETTE_ENTRY>(s.Value.style.Value.palette));
                    bmp.Value.mem.Free();

                }

                /* commit changes */
                _flush_osd(p, (int)bd_overlay_plane_e.BD_OVERLAY_PG, dialog[ii].start_pts);

                /* detect overlapping dialogs (not allowed) */
                if (ii < s.Value.num_dialog - 1)
                {
                    if (dialog[ii + 1].start_pts < dialog[ii].end_pts)
                    {
                        GC_ERROR("_render_textst: overlapping dialogs detected\n");
                    }
                }

                /* push hide events */
                for (jj = 0; jj < dialog[ii].region_count; jj++)
                {
                    Ref<BD_TEXTST_DIALOG_REGION> region = new Ref<BD_TEXTST_DIALOG_REGION>(dialog[ii].region).AtIndex(jj);
                    Ref<BD_TEXTST_REGION_STYLE> style = Ref<BD_TEXTST_REGION_STYLE>.Null;

                    style = _find_region_style(s.Value.style, region.Value.region_style_id_ref);
                    if (!style)
                    {
                        continue;
                    }
                    _clear_osd_area(p, (int)bd_overlay_plane_e.BD_OVERLAY_PG, dialog[ii].end_pts,
                                    style.Value.region_info.Value.region.Value.xpos, style.Value.region_info.Value.region.Value.ypos,
                                    style.Value.region_info.Value.region.Value.width, style.Value.region_info.Value.region.Value.height);
                }

                _hide_osd(p, (int)bd_overlay_plane_e.BD_OVERLAY_PG);

                /* commit changes */
                _flush_osd(p, (int)bd_overlay_plane_e.BD_OVERLAY_PG, dialog[ii].end_pts);
            }

            return 0;
        }


        /*
         * PG rendering
         */

        static int _render_pg_composition_object(Ref<GRAPHICS_CONTROLLER> gc,
                                                 Int64 pts,
                                                 Ref<BD_PG_COMPOSITION_OBJECT> cobj,
                                                 Ref<BD_PG_PALETTE> palette)
        {
            Ref<BD_PG_COMPOSITION> pcs = gc.Value.pgs.Value.pcs;
            Ref<BD_PG_OBJECT> _object = Ref<BD_PG_OBJECT>.Null;

            /* lookup object */
            _object = _find_object(gc.Value.pgs, cobj.Value.object_id_ref);
            if (!_object)
            {
                GC_ERROR($"_render_pg_composition_object: object #{cobj.Value.object_id_ref} not found");
                return -1;
            }

            /* open PG overlay */
            if (gc.Value.pg_open == 0)
            {
                _open_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_PG, 0, 0, pcs.Value.video_descriptor.Value.video_width, pcs.Value.video_descriptor.Value.video_height);
            }

            /* render object using composition parameters */
            _render_composition_object(gc, pts, (int)bd_overlay_plane_e.BD_OVERLAY_PG, cobj, _object, palette, pcs.Value.palette_update_flag);

            return 0;
        }

        static int _render_pg(Ref<GRAPHICS_CONTROLLER> gc)
        {
            Ref<PG_DISPLAY_SET> s = gc.Value.pgs;
            Ref<BD_PG_COMPOSITION> pcs = Ref<BD_PG_COMPOSITION>.Null;
            Ref<BD_PG_PALETTE> palette = Ref<BD_PG_PALETTE>.Null;
            uint display_flag;
            uint ii;

            if (!s || !s.Value.pcs || s.Value.complete == 0)
            {
                GC_ERROR("_render_pg(): no composition");
                return -1;
            }
            pcs = s.Value.pcs;

            /* mark PG display set handled */
            gc.Value.pgs.Value.complete = 0;

            /* lookup palette */
            palette = _find_palette(gc.Value.pgs, pcs.Value.palette_id_ref);
            if (!palette)
            {
                GC_ERROR($"_render_pg(): unknown palette id {pcs.Value.palette_id_ref} (have {s.Value.num_palette} palettes)");
                return -1;
            }

            display_flag = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_PG_STREAM) >> 31;

            /* render objects */
            for (ii = 0; ii < pcs.Value.num_composition_objects; ii++)
            {
                Ref<BD_PG_COMPOSITION_OBJECT> cobj = pcs.Value.composition_object.AtIndex(ii);
                if (cobj.Value.forced_on_flag != 0)
                {
                    GC_ERROR("_render_pg(): forced_on_flag not implemented");
                }
                if (cobj.Value.forced_on_flag != 0 || display_flag != 0)
                {
                    _render_pg_composition_object(gc, pcs.Value.pts, cobj, palette);
                }
            }

            if (gc.Value.pg_open == 0)
            {
                return 0;
            }

            /* commit changes at given pts */
            _flush_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_PG, pcs.Value.pts);

            /* clear plane but do not commit changes yet */
            /* .Value. plane will be cleared and hidden when empty composition arrives */
            /* (.Value. no need to store object regions for next update / clear event - or use expensive full plane clear) */
            for (ii = 0; ii < pcs.Value.num_composition_objects; ii++)
            {
                Ref<BD_PG_COMPOSITION_OBJECT> cobj = pcs.Value.composition_object.AtIndex(ii);
                Ref<BD_PG_OBJECT> _object = _find_object(gc.Value.pgs, cobj.Value.object_id_ref);

                if (_object)
                {
                    _clear_osd_area(gc, (int)bd_overlay_plane_e.BD_OVERLAY_PG, -1,
                                    cobj.Value.x, cobj.Value.y, _object.Value.width, _object.Value.height);
                }
            }
            _hide_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_PG);

            return 0;
        }

        static void _reset_pg(Ref<GRAPHICS_CONTROLLER> gc)
        {
            GraphicsProcessor.graphics_processor_free(ref gc.Value.pgp);

            GraphicsProcessor.pg_display_set_free(ref gc.Value.pgs);

            if (gc.Value.pg_open != 0)
            {
                _close_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_PG);
            }

            gc.Value.next_dialog_idx = 0;
        }

        /*
         * IG rendering
         */

        static void _render_button(Ref<GRAPHICS_CONTROLLER> gc, Ref<BD_IG_BUTTON> button, Ref<BD_PG_PALETTE> palette,
                                   ButtonState state, Ref<BOG_DATA> bog_data)
        {
            Ref<BD_PG_OBJECT> _object = _find_object_for_button(gc.Value.igs, button, state, bog_data);
            if (!_object)
            {
                GC_TRACE($"_render_button(#{button.Value.id}): object (state {state}) not found");

                _clear_bog_area(gc, bog_data);

                return;
            }

            /* object already rendered ? */
            if (bog_data.Value.visible_object_id == _object.Value.id &&
                bog_data.Value.x == button.Value.x_pos && bog_data.Value.y == button.Value.y_pos &&
                bog_data.Value.w == _object.Value.width && bog_data.Value.h == _object.Value.height)
            {

                GC_TRACE($"skipping already rendered button #{button.Value.id} (object #{_object.Value.id} at {button.Value.x_pos},{button.Value.y_pos} {_object.Value.width}x{_object.Value.height})");

                return;
            }

            /* new object is smaller than already drawn one, or in different position ? .Value. need to render background */
            if (bog_data.Value.w > _object.Value.width ||
                bog_data.Value.h > _object.Value.height ||
                bog_data.Value.x != button.Value.x_pos ||
                bog_data.Value.y != button.Value.y_pos)
            {

                /* make sure we won't wipe other buttons */
                uint ii, skip = 0;
                for (ii = 0; new Ref<BOG_DATA>(gc.Value.bog_data).AtIndex(ii) != bog_data; ii++)
                {
                    if (_areas_overlap(bog_data, new Ref<BOG_DATA>(gc.Value.bog_data).AtIndex(ii)))
                        skip = 1;
                    /* FIXME: clean non-overlapping area */
                }

                GC_TRACE($"object size changed, {((skip != 0) ? " ** NOT ** " : "")}clearing background at {bog_data.Value.x},{bog_data.Value.y} {bog_data.Value.w}x{bog_data.Value.h}");

                if (skip == 0)
                {
                    _clear_bog_area(gc, bog_data);
                }
            }

            GC_TRACE($"render button #{button.Value.id} using object #{_object.Value.id} at {button.Value.x_pos},{button.Value.y_pos} {_object.Value.width}x{_object.Value.height}");

            _render_object(gc, -1, (uint)bd_overlay_plane_e.BD_OVERLAY_IG,
                           button.Value.x_pos, button.Value.y_pos,
                           _object, palette);

            bog_data.Value.x = button.Value.x_pos;
            bog_data.Value.y = button.Value.y_pos;
            bog_data.Value.w = _object.Value.width;
            bog_data.Value.h = _object.Value.height;
            bog_data.Value.visible_object_id = _object.Value.id;

            gc.Value.ig_drawn = 1;
            gc.Value.ig_dirty = 1;
        }

        static int _render_ig_composition_object(Ref<GRAPHICS_CONTROLLER> gc,
                                                 Int64 pts,
                                                 Ref<BD_PG_COMPOSITION_OBJECT> cobj,
                                                 Ref<BD_PG_PALETTE> palette)
        {
            Ref<BD_PG_OBJECT> _object = Ref<BD_PG_OBJECT>.Null;

            /* lookup object */
            _object = _find_object(gc.Value.igs, cobj.Value.object_id_ref);
            if (!_object)
            {
                GC_ERROR($"_render_ig_composition_object: object #{cobj.Value.object_id_ref} not found");
                return -1;
            }

            /* render object using composition parameters */
            _render_composition_object(gc, pts, (uint)bd_overlay_plane_e.BD_OVERLAY_IG, cobj, _object, palette, 0);

            return 0;
        }

        static int _render_effect(Ref<GRAPHICS_CONTROLLER> gc, Ref<BD_IG_EFFECT> effect)
        {
            Ref<BD_PG_PALETTE> palette = Ref<BD_PG_PALETTE>.Null;
            uint ii;
            Int64 pts = -1;

            if (gc.Value.ig_open == 0)
            {
                _open_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_IG, 0, 0,
                          gc.Value.igs.Value.ics.Value.video_descriptor.Value.video_width,
                          gc.Value.igs.Value.ics.Value.video_descriptor.Value.video_height);
            }

            _clear_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_IG);

            palette = _find_palette(gc.Value.igs, effect.Value.palette_id_ref);
            if (!palette)
            {
                GC_ERROR($"_render_effect: palette #{effect.Value.palette_id_ref} not found");
                return -1;
            }

            for (ii = 0; ii < effect.Value.num_composition_objects; ii++)
            {
                _render_ig_composition_object(gc, pts, effect.Value.composition_object.AtIndex(ii), palette);
            }

            _flush_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_IG, pts);

            _reset_user_timeout(gc);

            return 0;
        }

        static int _render_page(Ref<GRAPHICS_CONTROLLER> gc,
                                 uint activated_button_id,
                                 Ref<GC_NAV_CMDS> cmds)
        {
            Ref<PG_DISPLAY_SET> s = gc.Value.igs;
            Ref<BD_IG_PAGE> page = Ref<BD_IG_PAGE>.Null;
            Ref<BD_PG_PALETTE> palette = Ref<BD_PG_PALETTE>.Null;
            uint page_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID);
            uint ii;
            uint selected_button_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_SELECTED_BUTTON_ID);
            Ref<BD_IG_BUTTON> auto_activate_button = Ref<BD_IG_BUTTON>.Null;

            gc.Value.button_effect_running = 0;
            gc.Value.button_animation_running = 0;

            if (s.Value.ics.Value.interactive_composition.Value.ui_model == IgDecode.IG_UI_MODEL_POPUP && gc.Value.popup_visible == 0)
            {

                gc.Value.page_uo_mask = new();

                if (gc.Value.ig_open != 0)
                {
                    GC_TRACE("_render_page(): popup menu not visible");
                    _close_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_IG);
                    return 1;
                }

                return 0;
            }

            /* running page effects ? */
            if (gc.Value.out_effects)
            {
                if (gc.Value.effect_idx < gc.Value.out_effects.Value.num_effects)
                {
                    _render_effect(gc, gc.Value.out_effects.Value.effect.AtIndex(gc.Value.effect_idx));
                    return 1;
                }
                gc.Value.out_effects = Ref<BD_IG_EFFECT_SEQUENCE>.Null;
            }

            page = _find_page(s.Value.ics.Value.interactive_composition.Ref, page_id);
            if (!page)
            {
                GC_ERROR($"_render_page: unknown page id {page_id} (have {s.Value.ics.Value.interactive_composition.Value.num_pages} pages)");
                return -1;
            }

            gc.Value.page_uo_mask = page.Value.uo_mask_table.Value;

            if (gc.Value.in_effects)
            {
                if (gc.Value.effect_idx < gc.Value.in_effects.Value.num_effects)
                {
                    _render_effect(gc, gc.Value.in_effects.Value.effect.AtIndex(gc.Value.effect_idx));
                    return 1;
                }
                gc.Value.in_effects = Ref<BD_IG_EFFECT_SEQUENCE>.Null;
            }

            palette = _find_palette(s, page.Value.palette_id_ref);
            if (!palette)
            {
                GC_ERROR($"_render_page: unknown palette id {page.Value.palette_id_ref} (have {s.Value.num_palette} palettes)");
                return -1;
            }

            GC_TRACE($"rendering page #{page.Value.id} using palette #{page.Value.palette_id_ref}. page has {page.Value.num_bogs} bogs");

            if (gc.Value.ig_open == 0)
            {
                _open_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_IG, 0, 0,
                          s.Value.ics.Value.video_descriptor.Value.video_width,
                          s.Value.ics.Value.video_descriptor.Value.video_height);
            }

            for (ii = 0; ii < page.Value.num_bogs; ii++)
            {
                Ref<BD_IG_BOG> bog = page.Value.bog.AtIndex(ii);
                uint valid_id = gc.Value.bog_data[ii].enabled_button;
                Ref<BD_IG_BUTTON> button;

                button = _find_button_bog(bog, valid_id);

                if (!button)
                {
                    GC_TRACE($"_render_page(): bog {ii}: button {valid_id} not found");

                    // render background
                    _clear_bog_area(gc, new Ref<BOG_DATA>(gc.Value.bog_data).AtIndex(ii));

                }
                else if (button.Value.id == activated_button_id)
                {
                    GC_TRACE($"    button #{button.Value.id} activated");

                    _render_button(gc, button, palette, ButtonState.BTN_ACTIVATED, new Ref<BOG_DATA>(gc.Value.bog_data).AtIndex(ii));

                }
                else if (button.Value.id == selected_button_id)
                {

                    if (button.Value.auto_action_flag != 0 && gc.Value.auto_action_triggered == 0)
                    {
                        if (cmds)
                        {
                            if (!auto_activate_button)
                            {
                                auto_activate_button = button;
                            }
                        }
                        else
                        {
                            GC_ERROR($"   auto-activate #{button.Value.id} not triggered (!cmds)");
                        }

                        _render_button(gc, button, palette, ButtonState.BTN_ACTIVATED, new Ref<BOG_DATA>(gc.Value.bog_data).AtIndex(ii));

                    }
                    else
                    {
                        _render_button(gc, button, palette, ButtonState.BTN_SELECTED, new Ref<BOG_DATA>(gc.Value.bog_data).AtIndex(ii));
                    }

                }
                else
                {
                    _render_button(gc, button, palette, ButtonState.BTN_NORMAL, new Ref<BOG_DATA>(gc.Value.bog_data).AtIndex(ii));

                }

                gc.Value.button_effect_running += (uint)gc.Value.bog_data[ii].effect_running;
                gc.Value.button_animation_running += (gc.Value.bog_data[ii].animate_indx >= 0) ? 1u : 0u;
            }

            /* process auto-activate */
            if (auto_activate_button)
            {
                GC_TRACE($"   auto-activate #{auto_activate_button.Value.id}");

                /* do not trigger auto action before single-loop animations have been terminated */
                if (gc.Value.button_effect_running != 0)
                {
                    GC_TRACE($"   auto-activate #{auto_activate_button.Value.id} not triggered (ANIMATING)");
                }
                else if (cmds)
                {
                    cmds.Value.num_nav_cmds = auto_activate_button.Value.num_nav_cmds;
                    cmds.Value.nav_cmds = auto_activate_button.Value.nav_cmds;

                    gc.Value.auto_action_triggered = 1;
                }
                else
                {
                    GC_ERROR("_render_page(): auto-activate ignored (missing result buffer)");
                }
            }

            if (gc.Value.ig_dirty != 0)
            {
                _flush_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_IG, -1);
                gc.Value.ig_dirty = 0;
                return 1;
            }

            return 0;
        }

        /*
         * user actions
         */

        static bool VK_IS_NUMERIC(bd_vk_key_e vk) => (/*vk >= BD_VK_0  &&*/ vk <= bd_vk_key_e.BD_VK_9);
        static bool VK_IS_CURSOR(bd_vk_key_e vk) => (vk >= bd_vk_key_e.BD_VK_UP && vk <= bd_vk_key_e.BD_VK_RIGHT);
        static int VK_TO_NUMBER(bd_vk_key_e vk) => ((vk) - bd_vk_key_e.BD_VK_0);

        static int _user_input(Ref<GRAPHICS_CONTROLLER> gc, bd_vk_key_e key, Ref<GC_NAV_CMDS> cmds)
        {
            Ref<PG_DISPLAY_SET> s = gc.Value.igs;
            Ref<BD_IG_PAGE> page = Ref<BD_IG_PAGE>.Null;
            uint page_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID);
            uint cur_btn_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_SELECTED_BUTTON_ID);
            uint new_btn_id = cur_btn_id;
            uint ii;
            int activated_btn_id = -1;

            if (s.Value.ics.Value.interactive_composition.Value.ui_model == IgDecode.IG_UI_MODEL_POPUP && gc.Value.popup_visible == 0)
            {
                GC_TRACE("_user_input(): popup menu not visible");
                return -1;
            }
            if (gc.Value.ig_open == 0)
            {
                GC_ERROR("_user_input(): menu not open");
                return -1;
            }

            if (gc.Value.ig_drawn == 0)
            {
                GC_ERROR("_user_input(): menu not visible");
                return 0;
            }

            _reset_user_timeout(gc);

            if (gc.Value.button_effect_running != 0)
            {
                GC_ERROR("_user_input(): button_effect_running");
                return 0;
            }

            GC_TRACE($"_user_input({key})");

            page = _find_page(s.Value.ics.Value.interactive_composition.Ref, page_id);
            if (!page)
            {
                GC_ERROR($"_user_input(): unknown page id {page_id} (have {s.Value.ics.Value.interactive_composition.Value.num_pages} pages)");
                return -1;
            }

            if (key == bd_vk_key_e.BD_VK_MOUSE_ACTIVATE)
            {
                if (gc.Value.valid_mouse_position == 0)
                {
                    GC_TRACE("_user_input(): BD_VK_MOUSE_ACTIVATE outside of valid buttons");
                    return -1;
                }
                key = bd_vk_key_e.BD_VK_ENTER;
            }

            for (ii = 0; ii < page.Value.num_bogs; ii++)
            {
                Ref<BD_IG_BOG> bog = page.Value.bog.AtIndex(ii);
                uint valid_id = gc.Value.bog_data[ii].enabled_button;
                Ref<BD_IG_BUTTON> button = _find_button_bog(bog, valid_id);
                if (!button)
                {
                    continue;
                }

                /* numeric select */
                if (VK_IS_NUMERIC(key))
                {
                    if (button.Value.numeric_select_value == VK_TO_NUMBER(key))
                    {
                        new_btn_id = button.Value.id;
                    }
                }

                /* cursor keys */
                else if (VK_IS_CURSOR(key) || key == bd_vk_key_e.BD_VK_ENTER)
                {
                    if (button.Value.id == cur_btn_id)
                    {
                        switch (key)
                        {
                            case bd_vk_key_e.BD_VK_UP:
                                new_btn_id = button.Value.upper_button_id_ref;
                                break;
                            case bd_vk_key_e.BD_VK_DOWN:
                                new_btn_id = button.Value.lower_button_id_ref;
                                break;
                            case bd_vk_key_e.BD_VK_LEFT:
                                new_btn_id = button.Value.left_button_id_ref;
                                break;
                            case bd_vk_key_e.BD_VK_RIGHT:
                                new_btn_id = button.Value.right_button_id_ref;
                                break;
                            case bd_vk_key_e.BD_VK_ENTER:
                                activated_btn_id = (int)cur_btn_id;

                                if (cmds)
                                {
                                    cmds.Value.num_nav_cmds = button.Value.num_nav_cmds;
                                    cmds.Value.nav_cmds = button.Value.nav_cmds;
                                    cmds.Value.sound_id_ref = button.Value.activated_sound_id_ref;
                                }
                                else
                                {
                                    GC_ERROR("_user_input(): VD_VK_ENTER action ignored (missing result buffer)");
                                }
                                break;
                            default:
                                break;
                        }
                    }

                    if (new_btn_id != cur_btn_id)
                    {
                        Ref<BD_IG_BUTTON> new_button = _find_button_page(page, new_btn_id, Ref<uint>.Null);
                        if (new_button && cmds)
                        {
                            cmds.Value.sound_id_ref = new_button.Value.selected_sound_id_ref;
                        }
                    }
                }
            }

            /* render page ? */
            if (new_btn_id != cur_btn_id || activated_btn_id >= 0)
            {

                _select_button(gc, new_btn_id);

                _render_page(gc, (uint)activated_btn_id, cmds);

                /* found one*/
                return 1;
            }

            return 0;
        }

        static void _set_button_page(Ref<GRAPHICS_CONTROLLER> gc, UInt32 param)
        {
            uint page_flag = param & 0x80000000;
            uint effect_flag = param & 0x40000000;
            uint button_flag = param & 0x20000000;
            uint page_id = (param >> 16) & 0xff;
            uint button_id = param & 0xffff;
            Variable<uint> bog_idx = new(0);

            Ref<PG_DISPLAY_SET> s = gc.Value.igs;
            Ref<BD_IG_PAGE> page = Ref<BD_IG_PAGE>.Null;
            Ref<BD_IG_BUTTON> button = Ref<BD_IG_BUTTON>.Null;

            GC_TRACE($"_set_button_page(0x{param:x8}): page flag {((page_flag == 0) ? 0 : 1)}, id {page_id}, effects {((effect_flag == 0) ? 0 : 1)}   button flag {((button_flag == 0) ? 0 : 1)}, id {button_id}");

            /* 10.4.3.4 (D) */

            if (page_flag == 0 && button_flag == 0)
            {
                return;
            }

            if (page_flag != 0)
            {

                /* current page -.Value. command is ignored */
                if (page_id == Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID))
                {
                    GC_TRACE("  page is current");
                    return;
                }

                page = _find_page(s.Value.ics.Value.interactive_composition.Ref, page_id);

                /* invalid page -.Value. command is ignored */
                if (!page)
                {
                    GC_TRACE("  page is invalid");
                    return;
                }

                /* page changes */

                _select_page(gc, (ushort)page_id, (effect_flag == 0) ? 1 : 0);

            }
            else
            {
                /* page does not change */
                page_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID);
                page = _find_page(s.Value.ics.Value.interactive_composition.Ref, page_id);

                if (!page)
                {
                    GC_ERROR($"_set_button_page(): PSR_MENU_PAGE_ID refers to unknown page {page_id}");
                    return;
                }
            }

            if (button_flag != 0)
            {
                /* find correct button and overlap group */
                button = _find_button_page(page, button_id, bog_idx.Ref);

                if (page_flag == 0)
                {
                    if (!button)
                    {
                        /* page not given, invalid button -.Value. ignore command */
                        GC_TRACE($"  button is invalid");
                        return;
                    }
                    if (button_id == Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_SELECTED_BUTTON_ID))
                    {
                        /* page not given, current button -.Value. ignore command */
                        GC_TRACE("  button is current");
                        return;
                    }
                }
            }

            if (button)
            {
                gc.Value.bog_data[bog_idx.Value].enabled_button = (ushort)button_id;
                _select_button(gc, button_id);
            }

            _render_page(gc, 0xffff, Ref<GC_NAV_CMDS>.Null); /* auto action not triggered yet */
        }

        static void _enable_button(Ref<GRAPHICS_CONTROLLER> gc, UInt32 button_id, uint enable)
        {
            Ref<PG_DISPLAY_SET> s = gc.Value.igs;
            Ref<BD_IG_PAGE> page = Ref<BD_IG_PAGE>.Null;
            Ref<BD_IG_BUTTON> button = Ref<BD_IG_BUTTON>.Null;
            uint page_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID);
            uint cur_btn_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_SELECTED_BUTTON_ID);
            Variable<uint> bog_idx = new(0);

            GC_TRACE($"_enable_button(#{button_id}, {((enable != 0) ? "enable" : "disable")})");

            page = _find_page(s.Value.ics.Value.interactive_composition.Ref, page_id);
            if (!page)
            {
                GC_TRACE($"_enable_button(): unknown page #{page_id} (have {s.Value.ics.Value.interactive_composition.Value.num_pages} pages)");
                return;
            }

            /* find correct button overlap group */
            button = _find_button_page(page, button_id, bog_idx.Ref);
            if (!button)
            {
                GC_TRACE($"_enable_button(): unknown button #{button_id} (page #{page_id})");
                return;
            }

            if (enable != 0)
            {
                if (gc.Value.bog_data[bog_idx.Value].enabled_button == cur_btn_id)
                {
                    /* selected button goes to disabled state */
                    Register.bd_psr_write(gc.Value.regs, bd_psr_idx.PSR_SELECTED_BUTTON_ID, 0x10000 | button_id);
                }
                gc.Value.bog_data[bog_idx.Value].enabled_button = (ushort)button_id;
                gc.Value.bog_data[bog_idx.Value].animate_indx = 0;

            }
            else
            {
                if (gc.Value.bog_data[bog_idx.Value].enabled_button == button_id)
                {
                    gc.Value.bog_data[bog_idx.Value].enabled_button = 0xffff;
                }

                if (cur_btn_id == button_id)
                {
                    Register.bd_psr_write(gc.Value.regs, bd_psr_idx.PSR_SELECTED_BUTTON_ID, 0xffff);
                }
            }
        }

        static void _update_selected_button(Ref<GRAPHICS_CONTROLLER> gc)
        {
            /* executed after IG command sequence terminates */
            uint button_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_SELECTED_BUTTON_ID);

            GC_TRACE($"_update_selected_button(): currently enabled button is #{button_id}");

            /* special case: triggered only after enable button disables selected button */
            if ((button_id & 0x10000) != 0)
            {
                button_id &= 0xffff;
                _select_button(gc, button_id);
                GC_TRACE($"_update_selected_button() -> #{button_id} [last enabled]");
                return;
            }

            if (button_id == 0xffff)
            {
                button_id = _find_selected_button_id(gc);
                _select_button(gc, button_id);
            }
        }

        static int _mouse_move(Ref<GRAPHICS_CONTROLLER> gc, UInt16 x, UInt16 y, Ref<GC_NAV_CMDS> cmds)
        {
            Ref<PG_DISPLAY_SET> s = gc.Value.igs;
            Ref<BD_IG_PAGE> page = Ref<BD_IG_PAGE>.Null;
            uint page_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID);
            uint cur_btn_id = Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_SELECTED_BUTTON_ID);
            uint new_btn_id = 0xffff;
            uint ii;

            gc.Value.valid_mouse_position = 0;

            if (gc.Value.ig_drawn == 0)
            {
                GC_TRACE($"_mouse_move(): menu not visible");
                return -1;
            }

            if (gc.Value.button_effect_running != 0)
            {
                GC_ERROR("_mouse_move(): button_effect_running");
                return -1;
            }

            page = _find_page(s.Value.ics.Value.interactive_composition.Ref, page_id);
            if (!page)
            {
                GC_ERROR($"_mouse_move(): unknown page #{page_id} (have {s.Value.ics.Value.interactive_composition.Value.num_pages} pages)");
                return -1;
            }

            for (ii = 0; ii < page.Value.num_bogs; ii++)
            {
                Ref<BD_IG_BOG> bog = page.Value.bog.AtIndex(ii);
                uint valid_id = gc.Value.bog_data[ii].enabled_button;
                Ref<BD_IG_BUTTON> button = _find_button_bog(bog, valid_id);

                if (!button)
                    continue;

                if (x < button.Value.x_pos || y < button.Value.y_pos)
                    continue;

                /* Check for SELECTED state object (button that can be selected) */
                Ref<BD_PG_OBJECT> _object = _find_object_for_button(s, button, ButtonState.BTN_SELECTED, Ref<BOG_DATA>.Null);
                if (!_object)
                    continue;

                if (x >= button.Value.x_pos + _object.Value.width || y >= button.Value.y_pos + _object.Value.height)
                    continue;

                /* mouse is over button */
                gc.Value.valid_mouse_position = 1;

                /* is button already selected? */
                if (button.Value.id == cur_btn_id)
                {
                    return 1;
                }

                new_btn_id = button.Value.id;

                if (cmds)
                {
                    cmds.Value.sound_id_ref = button.Value.selected_sound_id_ref;
                }

                break;
            }

            if (new_btn_id != 0xffff)
            {
                _select_button(gc, new_btn_id);

                _render_page(gc, uint.MaxValue, cmds);

                _reset_user_timeout(gc);
            }

            return (int)gc.Value.valid_mouse_position;
        }

        static int _animate(Ref<GRAPHICS_CONTROLLER> gc, Ref<GC_NAV_CMDS> cmds)
        {
            int result = -1;

            if (gc.Value.ig_open != 0)
            {

                result = 0;

                if (gc.Value.out_effects)
                {
                    Int64 pts = (long)Time.bd_get_scr();
                    Int64 duration = (Int64)gc.Value.out_effects.Value.effect[gc.Value.effect_idx].duration;
                    if (pts >= (gc.Value.next_effect_time + duration))
                    {
                        gc.Value.next_effect_time += duration;
                        gc.Value.effect_idx++;
                        if (gc.Value.effect_idx >= gc.Value.out_effects.Value.num_effects)
                        {
                            gc.Value.out_effects = Ref<BD_IG_EFFECT_SEQUENCE>.Null;
                            gc.Value.effect_idx = 0;
                            _clear_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_IG);
                        }
                        result = _render_page(gc, 0xffff, cmds);
                    }
                }
                else if (gc.Value.in_effects)
                {
                    Int64 pts = (long)Time.bd_get_scr();
                    Int64 duration = (Int64)gc.Value.in_effects.Value.effect[gc.Value.effect_idx].duration;
                    if (pts >= (gc.Value.next_effect_time + duration))
                    {
                        gc.Value.next_effect_time += duration;
                        gc.Value.effect_idx++;
                        if (gc.Value.effect_idx >= gc.Value.in_effects.Value.num_effects)
                        {
                            gc.Value.in_effects = Ref<BD_IG_EFFECT_SEQUENCE>.Null;
                            gc.Value.effect_idx = 0;
                            _clear_osd(gc, (int)bd_overlay_plane_e.BD_OVERLAY_IG);
                        }
                        result = _render_page(gc, 0xffff, cmds);
                    }

                }
                else if (gc.Value.button_animation_running != 0)
                {
                    Int64 pts = (long)Time.bd_get_scr();
                    if (pts >= (gc.Value.next_effect_time + gc.Value.frame_interval))
                    {
                        gc.Value.next_effect_time += gc.Value.frame_interval;
                        result = _render_page(gc, 0xffff, cmds);
                    }
                }
            }

            return result;
        }

        static int _run_timers(Ref<GRAPHICS_CONTROLLER> gc, Ref<GC_NAV_CMDS> cmds)
        {
            int result = -1;

            if (gc.Value.ig_open != 0)
            {

                result = 0;

                if (gc.Value.user_timeout != 0)
                {
                    Int64 pts = (long)Time.bd_get_scr();
                    if (pts > gc.Value.user_timeout)
                    {

                        GC_TRACE("user timeout expired");

                        if (gc.Value.igs.Value.ics.Value.interactive_composition.Value.ui_model != IgDecode.IG_UI_MODEL_POPUP)
                        {

                            if (Register.bd_psr_read(gc.Value.regs, bd_psr_idx.PSR_MENU_PAGE_ID) != 0)
                            {
                                _select_page(gc, 0, 0);
                                result = _render_page(gc, 0xffff, cmds);
                            }

                        }
                        else
                        {
                            gc.Value.popup_visible = 0;
                            result = _render_page(gc, 0xffff, cmds);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// run graphics controller
        /// </summary>
        /// <param name="gc"></param>
        /// <param name="ctrl"></param>
        /// <param name="param"></param>
        /// <param name="cmds"></param>
        /// <returns></returns>
        internal static int gc_run(Ref<GRAPHICS_CONTROLLER> gc, gc_ctrl_e ctrl, UInt32 param, Ref<GC_NAV_CMDS> cmds)
        {
            int result = -1;

            if (cmds)
            {
                cmds.Value.num_nav_cmds = 0;
                cmds.Value.nav_cmds = Ref<MOBJ_CMD>.Null;
                cmds.Value.sound_id_ref = -1;
                cmds.Value.status = GC_STATUS_NONE;
                cmds.Value.page_uo_mask = new();
            }

            if (!gc)
            {
                GC_TRACE("gc_run(): no graphics controller");
                return result;
            }

            gc.Value.mutex.bd_mutex_lock();

            /* always accept reset */
            switch (ctrl)
            {
                case gc_ctrl_e.GC_CTRL_RESET:
                    _gc_reset(gc);

                    gc.Value.mutex.bd_mutex_unlock();
                    return 0;
                case gc_ctrl_e.GC_CTRL_PG_UPDATE:
                    if (gc.Value.pgs && gc.Value.pgs.Value.pcs)
                    {
                        result = _render_pg(gc);
                    }
                    if (gc.Value.tgs && gc.Value.tgs.Value.dialog)
                    {
                        result = _render_textst(gc, param, cmds);
                    }
                    gc.Value.mutex.bd_mutex_unlock();
                    return result;

                case gc_ctrl_e.GC_CTRL_STYLE_SELECT:
                    result = _textst_style_select(gc, (int)param);
                    gc.Value.mutex.bd_mutex_unlock();
                    return result;

                case gc_ctrl_e.GC_CTRL_PG_CHARCODE:
                    if (gc.Value.textst_render)
                    {
                        TextstRender.textst_render_set_char_code(gc.Value.textst_render, (int)param);
                        result = 0;
                    }
                    gc.Value.mutex.bd_mutex_unlock();
                    return result;

                case gc_ctrl_e.GC_CTRL_PG_RESET:
                    _reset_pg(gc);

                    gc.Value.mutex.bd_mutex_unlock();
                    return 0;

                default:
                    break;
            }

            /* other operations require complete display set */
            if (!gc.Value.igs || !gc.Value.igs.Value.ics || gc.Value.igs.Value.complete == 0)
            {
                GC_TRACE("gc_run(): no interactive composition");
                gc.Value.mutex.bd_mutex_unlock();
                return result;
            }

            switch (ctrl)
            {

                case gc_ctrl_e.GC_CTRL_SET_BUTTON_PAGE:
                    _set_button_page(gc, param);
                    break;

                case gc_ctrl_e.GC_CTRL_VK_KEY:
                    if (param != (uint)bd_vk_key_e.BD_VK_POPUP)
                    {
                        result = _user_input(gc, (bd_vk_key_e)param, cmds);
                        break;
                    }
                    /* BD_VK_POPUP => GC_CTRL_POPUP */
                    param = ((gc.Value.popup_visible == 0) ? 1u : 0u);
                    /* fall thru */
                    goto case gc_ctrl_e.GC_CTRL_POPUP;

                case gc_ctrl_e.GC_CTRL_POPUP:
                    if (gc.Value.igs.Value.ics.Value.interactive_composition.Value.ui_model != IgDecode.IG_UI_MODEL_POPUP)
                    {
                        /* not pop-up menu */
                        break;
                    }

                    gc.Value.popup_visible = ((param == 0) ? 0u : 1u);

                    if (gc.Value.popup_visible != 0)
                    {
                        _select_page(gc, 0, 0);
                    }

                    result = _render_page(gc, 0xffff, cmds);
                    break;

                case gc_ctrl_e.GC_CTRL_NOP:
                    result = _animate(gc, cmds);
                    _run_timers(gc, cmds);
                    break;

                case gc_ctrl_e.GC_CTRL_INIT_MENU:
                    _select_page(gc, 0, 0);
                    _render_page(gc, 0xffff, cmds);
                    break;

                case gc_ctrl_e.GC_CTRL_IG_END:
                    _update_selected_button(gc);
                    _render_page(gc, 0xffff, cmds);
                    break;

                case gc_ctrl_e.GC_CTRL_ENABLE_BUTTON:
                    _enable_button(gc, param, 1);
                    break;

                case gc_ctrl_e.GC_CTRL_DISABLE_BUTTON:
                    _enable_button(gc, param, 0);
                    break;

                case gc_ctrl_e.GC_CTRL_MOUSE_MOVE:
                    result = _mouse_move(gc, (ushort)(param >> 16), (ushort)(param & 0xffff), cmds);
                    break;
            }

            if (cmds)
            {
                if (gc.Value.igs.Value.ics.Value.interactive_composition.Value.ui_model == IgDecode.IG_UI_MODEL_POPUP)
                {
                    cmds.Value.status |= GC_STATUS_POPUP;
                }
                if (gc.Value.ig_drawn != 0)
                {
                    cmds.Value.status |= GC_STATUS_MENU_OPEN;
                }
                if (gc.Value.in_effects || gc.Value.out_effects || gc.Value.button_animation_running != 0 || gc.Value.user_timeout != 0)
                {
                    /* do not trigger if unopened pop-up menu has animations */
                    if (gc.Value.ig_open != 0)
                    {
                        cmds.Value.status |= GC_STATUS_ANIMATE;
                        /* user input is still not handled, but user "sees" the menu. */
                        cmds.Value.status |= GC_STATUS_MENU_OPEN;
                    }
                }

                if (gc.Value.ig_open != 0)
                {
                    cmds.Value.page_uo_mask = gc.Value.page_uo_mask;
                }
            }

            gc.Value.mutex.bd_mutex_unlock();

            return result;
        }

    }
}
