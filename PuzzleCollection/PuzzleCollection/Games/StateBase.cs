using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    public abstract class StateBase
    {
        internal abstract bool IsCompleted { get; }
        internal abstract bool HasCheated { get; }
    }
}
