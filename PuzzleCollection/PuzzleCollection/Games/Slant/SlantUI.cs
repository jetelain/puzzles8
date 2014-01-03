using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Slant
{
    public sealed class SlantUI : UIBase
    {
        internal int cur_x, cur_y;
        internal bool cur_visible;

        internal override bool IsKeyboardCursorVisible
        {
            get { return cur_visible; }
        }
    }
}
