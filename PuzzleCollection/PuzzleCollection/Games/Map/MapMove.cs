using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Map
{
    public sealed class MapMove : MoveBase
    {
        internal bool isSolve;
        internal List<Tuple<bool, int, int>> data;

        internal override string ToId()
        {
            StringBuilder builder = new StringBuilder();
            if (isSolve)
            {
                builder.Append('S');
            }
            foreach (var point in data)
            {
                if (builder.Length > 0)
                {
                    builder.Append(';');
                }
                if (point.Item1)
                {
                    builder.Append('p');
                }
                builder.Append(point.Item2 == -1 ? 'C' : (char)('0' + point.Item2));
                builder.Append(':');
                builder.Append(point.Item3);
            }
            return builder.ToString();
        }

        internal static MapMove Parse(MapSettings settings, string moveString)
        {
            MapMove move = new MapMove();
            move.data = new List<Tuple<bool, int, int>>();
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
                bool isPencil = false;
                if (token.Length > 0 && token[0] == 'p')
                {
                    token = token.Substring(1);
                    isPencil = true;
                }
                var pos = token.Split(':');
                int k;
                if (pos.Length == 2 &&
                    pos[0].Length == 1 && 
                    int.TryParse(pos[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out k) &&
                    k >= 0 && k < settings.n)
                {
                    var c = pos[0][0]; 
                    if ( c == 'C' || (c >= '0' && c <= '3'))
                    {
                        int code = c == 'C' ? -1 : c - '0';
                        move.data.Add(new Tuple<bool, int, int>(isPencil, code, k));
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
            if (move.data.Count == 0)
            {
                return null;
            }
            return move;
        }
    }
}
