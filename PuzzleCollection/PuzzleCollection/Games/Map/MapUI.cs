using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Map
{
    public sealed class MapUI : UIBase
    {
        internal int drag_colour;
        internal int drag_pencil;
        internal int dragx, dragy;
        internal bool show_numbers;

        internal int cur_x, cur_y;
        internal bool cur_visible;
        internal Buttons cur_lastmove;
        internal bool cur_moved;

        internal override bool IsKeyboardCursorVisible
        {
            get { return cur_visible; }
        }
    }
}
