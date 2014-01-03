using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SignPost
{
    public sealed class SignPostMove : MoveBase
    {
        public string SolveData;
        public SignPostMoveType Type;
        public int sx;
        public int sy;
        public int ex;
        public int ey;

        internal override string ToId()
        {
            if (Type == SignPostMoveType.Solve)
            {
                return "S" + SolveData;
            }
            if (Type == SignPostMoveType.Link)
            {
                return string.Format("L{0},{1}-{2},{3}", sx, sy, ex, ey);
            }
            if (Type == SignPostMoveType.MarkX)
            {
                return string.Format("X{0},{1}", sx, sy);
            }
            if (Type == SignPostMoveType.MarkC)
            {
                return string.Format("C{0},{1}", sx, sy);
            }
            return null;
        }
        private static readonly Regex RegexL = new Regex("^L([0-9]+),([0-9]+)-([0-9]+),([0-9]+)$", RegexOptions.CultureInvariant);
        private static readonly Regex RegexX = new Regex("^X([0-9]+),([0-9]+)$", RegexOptions.CultureInvariant);
        private static readonly Regex RegexC = new Regex("^C([0-9]+),([0-9]+)$", RegexOptions.CultureInvariant);


        public static SignPostMove Parse(SignPostSettings settings, string moveStr)
        {
            if (moveStr.Length < 1)
            {
                return null;
            }
            if (moveStr[0] == 'S')
            {
                return new SignPostMove() { Type = SignPostMoveType.Solve, SolveData = moveStr.Substring(1) };
            }
            var match = RegexL.Match(moveStr);
            if (match.Success)
            {
                return new SignPostMove()
                {
                    Type = SignPostMoveType.Link,
                    sx = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    sy = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                    ex = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                    ey = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
                };
            }
            match = RegexX.Match(moveStr);
            if (match.Success)
            {
                return new SignPostMove()
                {
                    Type = SignPostMoveType.MarkX,
                    sx = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    sy = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                };
            }
            match = RegexC.Match(moveStr);
            if (match.Success)
            {
                return new SignPostMove()
                {
                    Type = SignPostMoveType.MarkC,
                    sx = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    sy = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                };
            }
            return null;
        }
    }
}
