using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SameGame
{
    public class SameGameState : StateBase
    {
        internal SameGameSettings @params;
        internal int n;
        internal int[,] tiles; /* colour only */
        internal int score;
        internal bool complete, impossible;

        internal sbyte[,] dx;
        internal sbyte[,] dy; 

        internal override bool IsCompleted
        {
            get { return complete; }
        }

        internal override bool HasCheated
        {
            get { return false; }
        }
    }
}
