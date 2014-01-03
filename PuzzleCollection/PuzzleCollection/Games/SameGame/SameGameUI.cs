using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SameGame
{
    public class SameGameUI : UIBase
    {
        internal SameGameSettings @params;
        internal int[,] tiles; /* selected-ness only */
        internal int nselected;
        internal int xsel, ysel;
        internal bool displaysel;

        internal override bool IsKeyboardCursorVisible
        {
            get { return displaysel; }
        }
    }
}
