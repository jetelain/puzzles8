using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Map
{
    public sealed class MapSettings : SettingsBase
    {
        internal readonly int w;
        internal readonly int h;
        internal readonly int n;
        internal readonly int diff;

        internal MapSettings(int w, int h, int n, int diff)
        {
            this.w = w; 
            this.h = h; 
            this.n = n; 
            this.diff = diff;
        }

        private static string DifficultyTitle(int diff)
        {
            switch (diff)
            {
                case MapGame.DIFF_EASY: 
                    return Labels.Easy;
                case MapGame.DIFF_NORMAL: 
                    return Labels.Normal;
                case MapGame.DIFF_HARD: 
                    return Labels.Hard;
                case MapGame.DIFF_RECURSE: 
                    return Labels.Unreasonable;
                default: 
                    return Labels.Unknown;
            }
        }

        internal override string ToTitle()
        {
            return string.Format("{0}x{1}, {2} regions, {3}", w, h, n, DifficultyTitle(diff));
        }

        private static readonly char[] map_diffchars = new []{ 'e', 'n', 'h', 'u' };

        internal override string ToId(bool full)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(w);
            builder.Append('x');
            builder.Append(h);
            builder.Append('n');
            builder.Append(n);
            if (full)
            {
                builder.Append('d');
                builder.Append(map_diffchars[diff]);
            }
            return builder.ToString();
        }
        private static readonly Regex SettingsRegex = new Regex("^([0-9]+)x([0-9]+)n([0-9]+)(d(e|n|h|u))?$", RegexOptions.CultureInvariant);

        public static MapSettings Parse(string settingsString)
        {
            var match = SettingsRegex.Match(settingsString);
            if (match.Success)
            {
                if (match.Groups[4].Success)
                {
                    return new MapSettings(
                        w: int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                        h: int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                        n: int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                        diff: Array.IndexOf(map_diffchars, match.Groups[4].Value[1])
                    );
                }
                return new MapSettings(
                    w: int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    h: int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                    n: int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                    diff: 0 // unknown (not full)
                );
            }
            return null;
        }
    }
}
