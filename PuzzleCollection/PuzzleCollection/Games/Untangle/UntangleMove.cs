using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Untangle
{
    public class UntangleMove : MoveBase
    {
        internal readonly List<UntangleMovePoint> points = new List<UntangleMovePoint>();
        public bool isSolve;

        internal override string ToId()
        {
            StringBuilder sb = new StringBuilder();
            if (isSolve)
            {
                sb.Append('S');
            }
            foreach (var point in points)
            {
                if (sb.Length > 0)
                {
                    sb.Append(';');
                }
                sb.Append('P');
                sb.Append(point.p);
                sb.Append(':');
                sb.Append(point.x);
                sb.Append(',');
                sb.Append(point.y);
                sb.Append('/');
                sb.Append(point.d);
            }
            return sb.ToString();
        }

        private static readonly Regex MovePointRegex = new Regex("^P([0-9]+):([0-9]+),([0-9]+)/([0-9]+)$", RegexOptions.CultureInvariant);

        internal static UntangleMove Parse(UntangleSettings settings, string moveStr)
        {
            var move = new UntangleMove();
            if (moveStr.Length > 0 && moveStr[0] == 'S')
            {
                moveStr = moveStr.Substring(1);
                move.isSolve = true;
            }
            var points = moveStr.Split(';');
            foreach (var pointStr in points)
            {
                var match = MovePointRegex.Match(pointStr);
                if (!match.Success)
                {
                    return null;
                }
                move.points.Add(new UntangleMovePoint(
                        int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                        int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                        int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                        int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture)
                    ));
            }
            return move;
        }
    }
}
