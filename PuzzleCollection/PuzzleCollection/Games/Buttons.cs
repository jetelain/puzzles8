using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    [Flags]
    public  enum Buttons
    {NONE=0,
        // Pressed
        LEFT_BUTTON = 0x0200,
        MIDDLE_BUTTON = LEFT_BUTTON + 1,
        RIGHT_BUTTON = LEFT_BUTTON + 2,

        // Moved Pressed
        LEFT_DRAG = LEFT_BUTTON + 3,
        MIDDLE_DRAG = LEFT_BUTTON + 4,
        RIGHT_DRAG = LEFT_BUTTON + 5,

        // Released
        LEFT_RELEASE = LEFT_BUTTON + 6,
        MIDDLE_RELEASE = LEFT_BUTTON + 7,
        RIGHT_RELEASE = LEFT_BUTTON + 8,

        CURSOR_UP = LEFT_BUTTON + 9,
        CURSOR_DOWN = LEFT_BUTTON + 10,
        CURSOR_LEFT = LEFT_BUTTON + 11,
        CURSOR_RIGHT = LEFT_BUTTON + 12,
        CURSOR_SELECT = LEFT_BUTTON + 13,
        CURSOR_SELECT2 = LEFT_BUTTON + 14,
    
        /* made smaller because of 'limited range of datatype' errors. */
        MOD_CTRL       = 0x1000,
        MOD_SHFT       = 0x2000,
        MOD_NUM_KEYPAD = 0x4000,
        MOD_MASK       = 0x7000 /* mask for all modifiers */


    }
}
