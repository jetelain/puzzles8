using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Net
{
    public class NetMove : MoveBase
    {
        internal bool isSolve;
        internal bool isJumble;
        internal readonly List<NetMovePoint> points = new List<NetMovePoint>();

        internal override string ToId()
        {
            StringBuilder builder = new StringBuilder();
            if (isSolve)
            {
                builder.Append('S');
            }
            else if (isJumble)
            {
                builder.Append('J');
            }
            foreach (var point in points)
            {
                if (builder.Length > 0)
                {
                    builder.Append(';');
                }
                builder.Append(point.move.ToString()); // One letter
                builder.Append(point.x);
                builder.Append(',');
                builder.Append(point.y);
            }
            return builder.ToString();
        }

        internal static NetMove Parse(NetSettings settings, string moveString)
        {
            NetMove move = new NetMove();
            var tokens = moveString.Split(';');
            var index = 0;
            if (tokens[0] == "S")
            {
                move.isSolve = true;
                index++;
            }
            else if (tokens[0] == "J")
            {
                move.isJumble = true;
                index++;
            }
            for (; index < tokens.Length; index++)
            {
                var token = tokens[index];
                NetMoveType type;
                if (token.Length > 0 && Enum.TryParse<NetMoveType>(token[0].ToString(), out type))
                {
                    var pos = token.Substring(1).Split(',');
                    int x, y;
                    if (pos.Length == 2 &&
                        int.TryParse(pos[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out x) &&
                        int.TryParse(pos[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out y) &&
                        x >= 0 && x < settings.width && y >= 0 && y < settings.height)
                    {
                        move.points.Add(new NetMovePoint(type, x, y));
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            if (move.points.Count == 0)
            {
                return null;
            }
            return move;
        }
    }
}
