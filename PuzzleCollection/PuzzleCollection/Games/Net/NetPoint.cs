using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Net
{
    public class NetPoint
    {
        internal int x, y, direction;

        public NetPoint()
        {
        }

        internal NetPoint Clone()
        {
            return new NetPoint() { x = x, y = y, direction =  direction };
        }
    }
}
