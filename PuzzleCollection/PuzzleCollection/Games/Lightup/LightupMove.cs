using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Lightup
{
    public sealed class LightupMove : MoveBase
    {
        internal bool isSolve;
        internal readonly List<LightupPoint> points = new List<LightupPoint>();

        internal override string ToId()
        {
            StringBuilder builder = new StringBuilder();
            if (isSolve)
            {
                builder.Append('S');
            } 
            foreach (var point in points)
            {
                if (builder.Length > 0)
                {
                    builder.Append(';');
                }
                builder.Append(point.f == LightupGame.F_LIGHT ? 'L' : 'I');
                builder.Append(point.x); 
                builder.Append(',');
                builder.Append(point.y);
            }
            return builder.ToString();
        }

        internal static LightupMove Parse(LightupSettings settings, string moveString)
        {
            LightupMove move = new LightupMove();
            var tokens = moveString.Split(';');
            var index = 0;
            if (tokens[0] == "S")
            {
                move.isSolve = true;
                index++;
            }
            for (; index < tokens.Length; index++)
            {
                var token = tokens[index];
                if (token.Length > 0 && (token[0] == 'L' || token[0] == 'I'))
                {
                    uint flag = token[0] == 'L' ? LightupGame.F_LIGHT : LightupGame.F_IMPOSSIBLE;
                    var pos = token.Substring(1).Split(',');
                    int x, y;
                    if (pos.Length == 2 &&
                        int.TryParse(pos[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out x) &&
                        int.TryParse(pos[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out y) &&
                        x >= 0 && x < settings.w && y >= 0 && y < settings.h)
                    {
                        move.points.Add(new LightupPoint() { f = flag, x = x, y = y });
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
