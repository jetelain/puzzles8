using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Net
{
    public class NetUI  : UIBase
    {
        internal int org_x, org_y; /* origin */
        internal int cx, cy;       /* source tile (game coordinates) */
        internal int cur_x, cur_y;
        internal bool cur_visible;
        internal Random rs; /* used for jumbling */
#if USE_DRAGGING
    int dragtilex, dragtiley, dragstartx, dragstarty, dragged;
#endif
        internal override bool IsKeyboardCursorVisible
        {
            get { return cur_visible; }
        }
    }
}
