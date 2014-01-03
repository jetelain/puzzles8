using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.SignPost
{
    public sealed class SignPostSettings : SettingsBase
    {
        internal readonly int w, h;
        internal readonly bool force_corner_start;

        internal SignPostSettings(int w, int h, bool force_corner_start)
        {
            this.w = w;
            this.h = h;
            this.force_corner_start = force_corner_start;
        }

        internal override string ToTitle()
        {
            return string.Format("{0}x{1}{2}", w, h, force_corner_start ? "" : ", free ends");
        }

        internal override string ToId(bool full)
        {
            if (full)
                return string.Format("{0}x{1}{2}", w, h, force_corner_start ? "c" : "");
            return string.Format("{0}x{1}", w, h);
        }

        private static readonly Regex SettingsRegex = new Regex("^([0-9]+)x([0-9]+)(c)?$", RegexOptions.CultureInvariant);

        public static SignPostSettings Parse(string settingsString)
        {
            var match = SettingsRegex.Match(settingsString);
            if (match.Success)
            {
                int w = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                int h = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                if (w >= 2 && h >= 2 && !(w == 2 && h == 2))
                {
                    return new SignPostSettings(w, h, match.Groups[3].Success);
                }
            }
            return null;
        }
    }
}
