using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleCollection.Games.Untangle
{
    public class UntangleEdge
    {
        /*
         * This structure is implicitly associated with a particular
         * point set, so all it has to do is to store two point
         * indices. It is required to store them in the order (lower,
         * higher), i.e. a < b always.
         */
        internal int a, b;

        internal static int Compare(UntangleEdge a, UntangleEdge b)
        {
            if (a.a < b.a)
	        return -1;
            else if (a.a > b.a)
	        return +1;
            else if (a.b < b.b)
	        return -1;
            else if (a.b > b.b)
	        return +1;
            return 0;
        }

    }
}
