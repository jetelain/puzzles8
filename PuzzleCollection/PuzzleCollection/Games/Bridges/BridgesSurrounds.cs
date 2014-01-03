using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleCollection.Games.Bridges
{
    public class BridgesSurrounds
    {
        internal BridgesPoint[] points = new []{new BridgesPoint(), new BridgesPoint(), new BridgesPoint(), new BridgesPoint() };
        internal int npoints, nislands;

        internal BridgesSurrounds Clone()
        {
            return new BridgesSurrounds()
            {
                points = points.Select(p => p.Clone()).ToArray(),
                npoints = npoints,
                nislands = nislands
            };
        }
    }
}
