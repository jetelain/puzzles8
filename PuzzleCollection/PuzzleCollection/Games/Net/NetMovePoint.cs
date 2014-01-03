using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Net
{
    public enum NetMoveType
    {
        A,
        C,
        F,
        L
    }
    public class NetMovePoint
    {
        internal readonly int x;
        internal readonly int y;
        internal readonly NetMoveType move;

        internal NetMovePoint(NetMoveType move, int x, int y)
        {
            this.move = move;
            this.x = x;
            this.y = y;
        }
    }
}
