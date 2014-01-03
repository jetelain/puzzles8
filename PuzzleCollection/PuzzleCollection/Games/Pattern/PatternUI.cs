using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Pattern
{
    public sealed class PatternUI : UIBase
    {
        internal bool dragging;
        internal int drag_start_x;
        internal int drag_start_y;
        internal int drag_end_x;
        internal int drag_end_y;
        internal Buttons drag, release;
        internal int state;
        internal int cur_x, cur_y;
        internal bool cur_visible;

        internal override bool IsKeyboardCursorVisible
        {
            get { return cur_visible; }
        }
    }
}
