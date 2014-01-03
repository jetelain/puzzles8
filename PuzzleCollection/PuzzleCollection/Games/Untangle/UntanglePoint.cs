using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleCollection.Games.Untangle
{
    public class UntanglePoint
    {
        /*
         * Points are stored using rational coordinates, with the same
         * denominator for both coordinates.
         */
        internal long x, y, d;

        internal UntanglePoint()
        {

        }

        internal UntanglePoint(UntanglePoint p)
        {
            this.x = p.x;
            this.y = p.y;
            this.d = p.d;
        }
    }
}
