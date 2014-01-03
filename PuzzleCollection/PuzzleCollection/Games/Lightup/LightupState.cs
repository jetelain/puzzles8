using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Lightup
{
    public sealed class LightupState : StateBase
    {
            internal int w, h, nlights;
            internal int[,] lights;        /* For black squares, (optionally) the number
                                   of surrounding lights. For non-black squares,
                                   the number of times it's lit. size h*w*/
            internal uint [,]flags;        /* size h*w */
            internal bool completed, used_solve;


            internal override bool IsCompleted
            {
                get { return completed; }
            }

            internal override bool HasCheated
            {
                get { return used_solve; }
            }
    }
}
