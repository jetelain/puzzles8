using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Pattern
{
    public sealed class PatternDrawState
    {
        internal bool started;
        internal int w, h;
        internal int tilesize;
        internal byte[] visible, numcolours;
        internal int cur_x, cur_y;
    }
}
