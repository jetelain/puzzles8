using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleCollection.Games.Untangle
{
    public class UntangleMovePoint : UntanglePoint
    {
        internal readonly int p;

        public UntangleMovePoint(int p, UntanglePoint pData)
            : base(pData)
        {
            this.p = p;
        }
        public UntangleMovePoint(int p, int x, int y, int d)
        {
            this.p = p;
            this.x = x;
            this.y = y;
            this.d = d;
        }
    }
}
