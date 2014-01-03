using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    public abstract class MoveBase
    {

        internal abstract string ToId();

        public override string ToString()
        {
            return ToId();
        }
    }
}
