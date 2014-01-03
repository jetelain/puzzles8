using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Untangle
{
    public class UntangleUI : UIBase
    {
        internal int dragpoint;		       /* point being dragged; -1 if none */
        internal UntanglePoint newpoint = new UntanglePoint();		       /* where it's been dragged to so far */
        internal bool just_dragged;		       /* reset in game_changed_state */
        internal bool just_moved;		       /* _set_ in game_changed_state */
        internal float anim_length;

        internal override bool IsKeyboardCursorVisible
        {
            get { return false; }
        }
    }
}
