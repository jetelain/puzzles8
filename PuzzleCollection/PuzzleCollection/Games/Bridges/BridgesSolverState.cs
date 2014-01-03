using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleCollection.Games.Bridges
{
    public class BridgesSolverState
    {
        internal int[] dsf, comptspaces;
        internal int[] tmpdsf, tmpcompspaces;
        internal int refcount;
    }
}
