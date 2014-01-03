using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SignPost
{
    public sealed class SignPostUI : UIBase
    {
        internal int cx, cy;
        internal bool cshow;

        internal bool dragging;
        internal bool drag_is_from;
        internal int sx, sy;         /* grid coords of start cell */
        internal int dx, dy;         /* pixel coords of drag posn */

        public SignPostUI()
        {
        }
        public SignPostUI(SignPostUI src)
        {
            cx = src.cx;
            cy = src.cy;
            cshow = src.cshow;
            dragging = src.dragging;
            drag_is_from = src.drag_is_from;
            sx = src.sx;
            sy = src.sy;
            dx = src.dx;
            dy = src.dy;

        }

        internal override bool IsKeyboardCursorVisible
        {
            get { return cshow; }
        }
    }
}
