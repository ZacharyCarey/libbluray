using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libbluray
{
    public enum bd_vk_key_e
    {
        BD_VK_NONE = 0xffff,  /**< no key pressed */

        /* numeric key events */
        BD_VK_0 = 0,   /**< "1" */
        BD_VK_1 = 1,   /**< "2" */
        BD_VK_2 = 2,   /**< "3" */
        BD_VK_3 = 3,   /**< "4" */
        BD_VK_4 = 4,   /**< "5" */
        BD_VK_5 = 5,   /**< "6" */
        BD_VK_6 = 6,   /**< "7" */
        BD_VK_7 = 7,   /**< "8" */
        BD_VK_8 = 8,   /**< "9" */
        BD_VK_9 = 9,   /**< "0" */

        /* */
        BD_VK_ROOT_MENU = 10,  /**< Open disc root menu */
        BD_VK_POPUP = 11,  /**< Toggle popup menu */

        /* interactive key events */
        BD_VK_UP = 12,  /**< Arrow up */
        BD_VK_DOWN = 13,  /**< Arrow down */
        BD_VK_LEFT = 14,  /**< Arrow left */
        BD_VK_RIGHT = 15,  /**< Arrow right */
        BD_VK_ENTER = 16,  /**< Select */

        /** Mouse click. Translated to BD_VK_ENTER if mouse is over a valid button. */
        BD_VK_MOUSE_ACTIVATE = 17,

        BD_VK_RED = 403, /**< Color key "Red" */
        BD_VK_GREEN = 404, /**< Color key "Green" */
        BD_VK_YELLOW = 405, /**< Color key "Yellow" */
        BD_VK_BLUE = 406, /**< Color key "Blue" */
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
