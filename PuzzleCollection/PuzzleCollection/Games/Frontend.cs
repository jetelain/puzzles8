using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    public class Frontend
    {
        internal void frontend_default_colour(float[] ret, int p)
        {
            ret[p] = 1.0f;
            ret[p+1] = 1.0f;
            ret[p+2] = 1.0f;
        }
    }
}
