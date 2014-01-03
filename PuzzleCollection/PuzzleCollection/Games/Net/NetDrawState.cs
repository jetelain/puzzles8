using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Net
{
    public class NetDrawState
    {
        internal bool started;
        internal int width, height;
        internal int org_x, org_y;
        internal int tilesize;
        internal byte[,] visible;
    }
}
