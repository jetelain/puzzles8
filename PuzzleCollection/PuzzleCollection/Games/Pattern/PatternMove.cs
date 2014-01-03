using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Pattern
{
    enum PatternMoveType
    {
        Solve,
        Full,
        Empty,
        Undefine
    }

    public sealed class PatternMove : MoveBase
    {
        internal PatternMoveType type;
        internal int x1, y1, x2, y2;
        internal char[] data;

        internal override string ToId()
        {
            StringBuilder builder = new StringBuilder();
            if (type == PatternMoveType.Solve)
            {
                builder.Append('S');
                builder.Append(data);
            }
            else
            {
                builder.Append(type.ToString()[0]);
                builder.AppendFormat("{0},{1},{2},{3}", x1, y1, x2, y2);
            }
            return builder.ToString();
        }

        private static readonly Regex MoveRegex = new Regex("^(F|E|U)([0-9]+),([0-9]+),([0-9]+),([0-9]+)$", RegexOptions.CultureInvariant);

        internal static PatternMove Parse(PatternSettings settings, string move)
        {
            if (move[0] == 'S' && move.Length == settings.w * settings.h + 1)
            {
                return new PatternMove() 
                { 
                    type = PatternMoveType.Solve,  
                    data = move.ToCharArray(1, move.Length - 1) 
                };
            }
            else 
            {
                Match match = MoveRegex.Match(move);
                if (match.Success)
                {
                    PatternMoveType type = move[0] == 'F' ? PatternMoveType.Full : (move[0] == 'E' ? PatternMoveType.Empty : PatternMoveType.Undefine);
                    int x1 = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    int y1 = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                    int x2 = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
                    int y2 = int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);

                    if (x1 >= 0 && x2 >= 0 && x1 + x2 <= settings.w && y1 >= 0 && y2 >= 0 && y1 + y2 <= settings.h)
                    {
                        return new PatternMove() { type = type, x1 = x1, y1 = y1, x2 = x2, y2 = y2 };
                    }
                }
            }
            return null;
        }
    }
}
