using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SignPost
{
    public sealed class SignPostDrawState
    {
        internal int tilesize;
        internal bool started, solved;
        internal int w, h, n;
        internal int[] nums, dirp;
        internal uint[] f;
        internal double angle_offset;
        internal bool dragging;
        internal int dx, dy;
    }
}
