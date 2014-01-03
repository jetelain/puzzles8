using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Bridges
{
    public class BridgesMove : MoveBase
    {
        internal readonly List<BridgesMovePoint> points = new List<BridgesMovePoint>();

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
                if (point.type == BridgesMoveType.L)
                {
                    sb.AppendFormat("L{0},{1},{2},{3},{4}",point.x1, point.y1, point.x2, point.y2, point.nl);
                }
                else if (point.type == BridgesMoveType.N)
                {
                    sb.AppendFormat("N{0},{1},{2},{3}", point.x1, point.y1, point.x2, point.y2);
                }
                else if (point.type == BridgesMoveType.M)
                {
                    sb.AppendFormat("M{0},{1}", point.x1, point.y1);
                }
            }
            return sb.ToString();
        }

        private static readonly Regex MovePointRegexL = new Regex("^L([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+)$", RegexOptions.CultureInvariant);
        private static readonly Regex MovePointRegexN = new Regex("^N([0-9]+),([0-9]+),([0-9]+),([0-9]+)$", RegexOptions.CultureInvariant);
        private static readonly Regex MovePointRegexM = new Regex("^M([0-9]+),([0-9]+)$", RegexOptions.CultureInvariant);

        internal static BridgesMove Parse(BridgesSettings settings, string moveStr)
        {
            var move = new BridgesMove();
            if (moveStr.Length > 0 && moveStr[0] == 'S')
            {
                moveStr = moveStr.Substring(1);
                move.isSolve = true;
            }
            var points = moveStr.Split(';');
            foreach (var pointStr in points)
            {
                var match = MovePointRegexL.Match(pointStr);
                if (match.Success)
                {
                    move.points.Add(new BridgesMovePoint(
                            BridgesMoveType.L,
                            int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                            int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                            int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                            int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
                            int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture)
                        ));
                }
                else
                {
                    match = MovePointRegexN.Match(pointStr);
                    if (match.Success)
                    {
                        move.points.Add(new BridgesMovePoint(
                            BridgesMoveType.N,
                            int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                            int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                            int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                            int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
                            0
                        ));

                    }
                    else
                    {
                        match = MovePointRegexM.Match(pointStr);
                        if (match.Success)
                        {
                            move.points.Add(new BridgesMovePoint(
                                BridgesMoveType.M,
                                int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                                int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                                0,
                                0,
                                0
                            ));
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            return move;
        }
    }
}
