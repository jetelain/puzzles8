using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Net
{
    public class NetState : StateBase
    {
        internal int width, height;
        internal bool wrapping, completed;
        internal int last_rotate_x, last_rotate_y, last_rotate_dir;
        internal bool used_solve;
        internal byte[,] tiles;
        internal byte[,] barriers;

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
