using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Lightup
{
    public sealed class LightupSurrounds
    {
        internal int npoints;
        internal LightupPoint[] points = new LightupPoint[4] { new LightupPoint(), new LightupPoint(), new LightupPoint(), new LightupPoint() }
            ;
    }
}
