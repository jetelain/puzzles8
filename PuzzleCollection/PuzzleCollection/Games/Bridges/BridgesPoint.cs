using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleCollection.Games.Bridges
{
    public class BridgesPoint
    {
        internal int x, y, dx, dy, off;

        internal BridgesPoint Clone()
        {
            return new BridgesPoint() { x = x, y=y, dx = dx, dy = dy, off = off};
        }
    }
}
