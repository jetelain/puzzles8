using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Untangle
{
    public class UntangleVertex
    {
        internal int param;
        internal int vindex;

        internal static int Compare(UntangleVertex a, UntangleVertex b)
        {
            if (a.param < b.param)
	        return -1;
            else if (a.param > b.param)
	        return +1;
            else if (a.vindex < b.vindex)
	        return -1;
            else if (a.vindex > b.vindex)
	        return +1;
            return 0;
        }
    }
}
