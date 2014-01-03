using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SameGame
{
    public class SameGameMove : MoveBase
    {
        internal override string ToId()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append('M');
            builder.Append(string.Join(",", select.Select(i => i.ToString(CultureInfo.InvariantCulture))));
            return builder.ToString();
        }

        internal List<int> select = new List<int>();

        internal static SameGameMove Parse(SameGameSettings settings, string moveString)
        {
            if (moveString.Length > 0 && moveString[0] == 'M')
            {
                int n = settings.w * settings.h;
                var move = new SameGameMove();
                foreach (var iStr in moveString.Substring(1).Split(','))
                {
                    int i;
                    if (int.TryParse(iStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out i) &&  i >= 0 && i< n)
                    {
                        move.select.Add(i);
                    }
                    else
                    {
                        return null;
                    }
                }
                return move;
            }
            return null;
        }
    }
}
