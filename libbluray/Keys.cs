using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray
{
    /// <summary>
    /// Key codes
    /// </summary>
    public enum bd_vk_key_e
    {
        /// <summary>
        /// no key pressed
        /// </summary>
        BD_VK_NONE = 0xffff,  

        /* numeric key events */
        
        /// <summary>
        /// "1"
        /// </summary>
        BD_VK_0 = 0, 

        /// <summary>
        /// "2"
        /// </summary>
        BD_VK_1 = 1, 
        
        /// <summary>
        /// "3"
        /// </summary>
        BD_VK_2 = 2, 

        /// <summary>
        /// "4"
        /// </summary>
        BD_VK_3 = 3, 

        /// <summary>
        /// "5"
        /// </summary>
        BD_VK_4 = 4,  

        /// <summary>
        /// "6"
        /// </summary>
        BD_VK_5 = 5,  

        /// <summary>
        /// "7"
        /// </summary>
        BD_VK_6 = 6,  

        /// <summary>
        /// "8"
        /// </summary>
        BD_VK_7 = 7,  
        
        /// <summary>
        /// "9"
        /// </summary>
        BD_VK_8 = 8, 
        
        /// <summary>
        /// "0"
        /// </summary>
        BD_VK_9 = 9,

        /* */
        /// <summary>
        /// Open disc root menu
        /// </summary>
        BD_VK_ROOT_MENU = 10,

        /// <summary>
        /// Toggle popup menu
        /// </summary>
        BD_VK_POPUP = 11,

        /* interactive key events */
        /// <summary>
        /// Arrow up
        /// </summary>
        BD_VK_UP = 12,

        /// <summary>
        ///  Arrow down
        /// </summary>
        BD_VK_DOWN = 13,

        /// <summary>
        /// Arrow left
        /// </summary>
        BD_VK_LEFT = 14,

        /// <summary>
        /// Arrow right
        /// </summary>
        BD_VK_RIGHT = 15,

        /// <summary>
        /// Select
        /// </summary>
        BD_VK_ENTER = 16,

        /// <summary>
        /// Mouse click. Translated to BD_VK_ENTER if mouse is over a valid button.
        /// </summary>
        BD_VK_MOUSE_ACTIVATE = 17,

        /// <summary>
        /// Color key "Red"
        /// </summary>
        BD_VK_RED = 403,

        /// <summary>
        /// Color key "Green"
        /// </summary>
        BD_VK_GREEN = 404,

        /// <summary>
        /// Color key "Yellow"
        /// </summary>
        BD_VK_YELLOW = 405,

        /// <summary>
        /// Color key "Blue"
        /// </summary>
        BD_VK_BLUE = 406, 
    }

    public static class Keys
    {
        /*
         * Application may optionally provide KEY_PRESSED, KEY_TYPED and KEY_RELEASED events.
         * These masks are OR'd with the key code when calling bd_user_input().
         */

        /// <summary>
        /// Key was pressed down
        /// </summary>
        public const uint BD_VK_KEY_PRESSED = 0x80000000;

        /// <summary>
        /// Key was typed
        /// </summary>
        public const uint BD_VK_KEY_TYPED = 0x40000000;

        /// <summary>
        /// Key was released
        /// </summary>
        public const uint BD_VK_KEY_RELEASED = 0x20000000;
    }
}
