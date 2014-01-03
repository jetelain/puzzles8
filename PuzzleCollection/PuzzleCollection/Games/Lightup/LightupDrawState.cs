using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Lightup
{
    public sealed class LightupDrawState
    {
        internal int tilesize, crad;
        internal int w, h;
        internal uint[,] flags;         /* width * height */
        internal bool started;
    }
}
