using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Pattern
{
    public sealed class PatternSettings : SettingsBase
    {
        internal readonly int w;

        internal readonly int h;

        internal PatternSettings(int w, int h)
        {
            this.w = w;
            this.h = h;
        }

        internal override string ToTitle()
        {
            return string.Format("{0}x{1}", w, h);
        }

        internal override string ToId(bool full)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}x{1}", w, h);
        }

        private static readonly Regex SettingsRegex = new Regex("^([0-9]+)x([0-9]+)$", RegexOptions.CultureInvariant);

        public static PatternSettings Parse(string settingsString)
        {
            var match = SettingsRegex.Match(settingsString);
            if (match.Success)
            {
                return new PatternSettings(
                    int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
                );
            }
            return null;
        }
    }
}
