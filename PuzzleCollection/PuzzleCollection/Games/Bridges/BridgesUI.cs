using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Bridges
{
    public class BridgesUI : UIBase
    {
        internal int dragx_src, dragy_src;   /* source; -1 means no drag */
        internal int dragx_dst, dragy_dst;   /* src's closest orth island. */
        internal uint todraw;
        internal bool dragging, drag_is_noline;
        internal int nlines;

        internal int cur_x, cur_y;
        internal bool cur_visible;      /* cursor position */
        internal bool show_hints;

        internal override bool IsKeyboardCursorVisible
        {
            get { return cur_visible; }
        }
    }
}
