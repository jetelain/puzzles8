using PuzzleCollection.Games.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleCollection.Games.Untangle
{
    public class UntangleGraph
    {
        internal int refcount;		       /* for deallocation */
        internal Tree234<UntangleEdge> edges;		       /* stores `edge' structures */
    }
}
