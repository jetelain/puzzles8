using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Slant
{
    public sealed class SlantDrawState
    {
        internal int tilesize;
        internal bool started;
        internal long[] grid;
        internal long[] todraw;

    }
}
