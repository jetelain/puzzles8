using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SignPost
{
    public sealed class SignPostState : StateBase
    {
        internal int w, h, n;
        internal bool completed, used_solve, impossible;
        internal int[] dirs;                  /* direction enums, size n */
        internal int[] nums;                  /* numbers, size n */
        internal uint[] flags;        /* flags, size n */
        internal int[] next, prev;           /* links to other cell indexes, size n (-1 absent) */
        internal int[] dsf;                   /* connects regions with a dsf. */
        internal int[] numsi;                 /* for each number, which index is it in? (-1 absent) */


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
