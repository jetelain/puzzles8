using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Bridges
{
    public class BridgesDrawState
    {
        internal int tilesize;
        internal int w, h;
        internal uint[,] grid;
        internal int[,] lv, lh;
        internal bool started, dragging;
        internal bool show_hints;
    }
}
