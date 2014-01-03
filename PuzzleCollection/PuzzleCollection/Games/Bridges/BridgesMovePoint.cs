using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleCollection.Games.Bridges
{
    public class BridgesMovePoint
    {
        internal readonly BridgesMoveType type;
        internal readonly int x1;
        internal readonly int y1;
        internal readonly int x2;
        internal readonly int y2;
        internal readonly int nl;

        public BridgesMovePoint(BridgesMoveType type, int x1, int y1, int x2, int y2, int nl)
        {
            this.type = type;
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
            this.nl = nl;
        }
    }
}
