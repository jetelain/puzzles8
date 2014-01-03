using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Untangle
{
    public class UntangleState : StateBase
    {
        internal UntangleSettings @params;
        internal int w, h;			       /* extent of coordinate system only */
        internal UntanglePoint[] pts;
        internal UntangleGraph graph;
        internal bool completed, cheated, just_solved;

        internal override bool IsCompleted
        {
            get { return completed; }
        }

        internal override bool HasCheated
        {
            get { return cheated; }
        }
    }
}
