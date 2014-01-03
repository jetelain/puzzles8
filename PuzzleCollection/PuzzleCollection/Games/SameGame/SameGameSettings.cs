using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SameGame
{
    public class SameGameSettings : SettingsBase
    {
        internal readonly int w, h, ncols, scoresub;
        internal readonly bool soluble;		       /* choose generation algorithm */

        internal SameGameSettings(int w, int h, int ncols, int scoresub, bool soluble)
        {
            this.w = w;
            this.h = h;
            this.ncols = ncols;
            this.scoresub = scoresub;
            this.soluble = soluble;
        }

        internal override string ToId(bool full)
        {
            return string.Format("{0}x{1}c{2}s{3}{4}", w, h, ncols, scoresub, full && !soluble ? "r" : "");
        }

        private static readonly Regex SettingsRegex = new Regex("^([0-9]+)x([0-9]+)c([0-9]+)s([0-9]+)(r)?$", RegexOptions.CultureInvariant);

        public static SameGameSettings Parse(string settingsString)
        {
            var match = SettingsRegex.Match(settingsString);
            if (match.Success)
            {
                return new SameGameSettings(
                    int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
                    !match.Groups[5].Success
                );
            }
            return null;
        }

        internal override string ToTitle()
        {
            return String.Format("{0}x{1}, {2} colours", w, h, ncols);
        }
    }
}
