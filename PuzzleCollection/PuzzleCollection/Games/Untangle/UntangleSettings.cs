using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Untangle
{
    public class UntangleSettings : SettingsBase
    {
        internal readonly int n;			       /* number of points */

        internal UntangleSettings(int n)
        {
            this.n = n;
        }

        internal override string ToTitle()
        {
            return n.ToString();
        }

        internal override string ToId(bool full)
        {
            return n.ToString(CultureInfo.InvariantCulture);
        }

        public static UntangleSettings Parse(string settingsStr)
        {
            int askedN;
            if (int.TryParse(settingsStr, out askedN) && askedN >= 4)
            {
                return new UntangleSettings(askedN);
            }
            return null;
        }
    }
}
