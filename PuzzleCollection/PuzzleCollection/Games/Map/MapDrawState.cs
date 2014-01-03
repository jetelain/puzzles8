using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Map
{
    public sealed class MapDrawState
    {
        internal int tilesize;
        internal ulong[] drawn, todraw;
        internal bool started;
        internal int dragx, dragy;
        //internal bool drag_visible;
        //internal blitter bl;
    }
}
