using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Slant
{
    public sealed class SlantMove : MoveBase
    {
        public bool IsSolve;
        internal List<SlantMovePoint> Points = new List<SlantMovePoint>();

        internal override string ToId()
        {
            StringBuilder ret = new StringBuilder();
            if (IsSolve)
            {
                ret.Append('S');
            }
            foreach (var point in Points)
            {
                if (ret.Length > 0)
                {
                    ret.Append(';');
                }
                ret.Append((point.n == -1 ? '\\' : point.n == +1 ? '/' : 'C'));
                ret.Append(point.x);
                ret.Append(',');
                ret.Append(point.y);
            }
            return ret.ToString();
        }

        private static readonly Regex MovePoint = new Regex("^(/|\\\\|C)([0-9]+),([0-9]+)$", RegexOptions.CultureInvariant);

        internal static SlantMove Parse(SlantSettings settings, string moveString)
        {
            var move = new SlantMove();
            if (moveString.Length > 0 && moveString[0] == 'S')
            {
                move.IsSolve = true;
                moveString = moveString.Substring(1);
            }
            var tokens = moveString.Split(';');
            foreach (var token in tokens)
            {
                var match = MovePoint.Match(token);
                if (!match.Success)
                {
                    return null;
                }
                var x = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                var y = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                sbyte n = 0;
                if (match.Groups[1].Value == "\\")
                {
                    n = -1;
                }
                else if (match.Groups[1].Value == "/")
                {
                    n = 1;
                }
                move.Points.Add(new SlantMovePoint() { x=x,y=y,n=n});
            }
            return move;
        }


    }
}
