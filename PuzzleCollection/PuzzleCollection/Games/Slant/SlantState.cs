using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Slant
{
    public sealed class SlantState : StateBase
    {
    internal SlantSettings p;
    internal SlantClues clues;
    internal sbyte[] soln;
    internal byte[] errors;
    internal bool completed;
    internal bool used_solve;		       /* used to suppress completion flash */

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
