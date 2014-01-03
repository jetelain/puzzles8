using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Bridges
{
    public class BridgesState : StateBase
    {
        internal int w, h, maxb;
        internal bool completed, solved, allowloops;
        internal uint[,] grid, scratch;
        internal readonly List<BridgesIsland> islands = new List<BridgesIsland>();
        //internal int n_islands, n_islands_alloc;
        internal BridgesSettings @params; /* used by the aux solver. */
        internal sbyte[,] possv, possh, lines, maxv, maxh;
        internal BridgesIsland[,] gridi;
        internal BridgesSolverState solver; /* refcounted */

        internal override bool IsCompleted
        {
            get { return completed; }
        }

        internal override bool HasCheated
        {
            get { return solved; }
        }
    }
}
