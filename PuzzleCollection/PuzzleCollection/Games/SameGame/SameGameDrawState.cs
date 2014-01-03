using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SameGame
{
    public class SameGameDrawState
    {
        internal bool started;
        internal int bgcolour;
        internal int tileinner, tilegap;
        internal int[,] tiles; /* contains colour and SELECTED. */
    }
}
