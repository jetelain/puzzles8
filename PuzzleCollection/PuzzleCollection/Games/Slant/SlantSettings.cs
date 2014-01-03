using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Slant
{
    public sealed class SlantSettings : SettingsBase
    {
        internal readonly int w, h, diff;

        internal SlantSettings(int w, int h, int diff)
        {
            this.w = w;
            this.h = h;
            this.diff = diff;
        }

        internal override string ToTitle()
        {
            return string.Format("{0}x{1} {2}", w, h, diff == 0 ? Labels.Easy : Labels.Hard);
        }

        internal override string ToId(bool full)
        {
            if (full)
            {
                return string.Format("{0}x{1}d{2}", w, h, diff == 0 ? "e" : "h");
            }
            return string.Format("{0}x{1}", w, h);
        }

        private readonly static Regex SettingsRegex = new Regex("^([0-9]+)x([0-9]+)(de|dh)?$");

        public static SlantSettings Parse(string settingsString)
        {
            var match = SettingsRegex.Match(settingsString);
            if (match.Success)
            {
                var w = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                var h = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                var diff = 0;
                if (match.Groups[3].Success && match.Groups[3].Value == "dh")
                {
                    diff = 1;
                }
                if (w >= 2 && h >= 2)
                {
                    return new SlantSettings(w, h, diff);
                }
            }
            return null;
        }
    }
}
