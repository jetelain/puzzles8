using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Bridges
{
    public class BridgesSettings : SettingsBase
    {
        internal readonly int w, h, maxb;
        internal readonly int islands, expansion;     /* %age of island squares, %age chance of expansion */
        internal readonly bool allowloops;
        internal readonly int difficulty;

        internal BridgesSettings(int w, int h, int maxb, int islands, int expansion, bool allowloops, int difficulty)
        {
            this.w = w; 
            this.h = h;
            this.maxb = maxb;
            this.islands = islands;
            this.expansion = expansion;
            this.allowloops = allowloops;
            this.difficulty = difficulty;
        }

        internal override string ToId(bool full)
        {
            if (full) {
                return string.Format("{0}x{1}i{2}e{3}m{4}{5}d{6}",
                        w, h, islands, expansion,
                        maxb, allowloops ? "" : "L",
                        difficulty);
            } else {
                return string.Format("{0}x{1}m{2}{3}", w, h,
                        maxb, allowloops ? "" : "L");
            }
        }

        internal override string ToTitle()
        {
            return string.Format("{0}x{1} {2}", w, h,
                    difficulty == 0 ? Labels.Easy :
                    difficulty == 1 ? Labels.Normal : Labels.Hard);
        }

        //{0}x{1}i{2}e{3}m{4}{5}d{6}
        private static readonly Regex SettingsRegex = new Regex("^([0-9]+)x([0-9]+)(i([0-9]+))?(e([0-9]+))?m([0-9]+)(L)?(d([0-9]+))?$", RegexOptions.CultureInvariant);

        public static BridgesSettings Parse(string settingsString)
        {
            var match = SettingsRegex.Match(settingsString);
            if (match.Success)
            {
                int w = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                int  h = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                int maxb= int.Parse(match.Groups[7].Value, CultureInfo.InvariantCulture);
                int islands=0, expansion=0;     /* %age of island squares, %age chance of expansion */
                bool allowloops = !match.Groups[8].Success;
                int difficulty=0;
                if (match.Groups[3].Success)
                {
                    islands = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
                }
                if (match.Groups[5].Success)
                {
                    expansion = int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture);
                }
                if (match.Groups[9].Success)
                {
                    difficulty = int.Parse(match.Groups[10].Value, CultureInfo.InvariantCulture);
                } 
                return new BridgesSettings(w, h, maxb, islands, expansion, allowloops, difficulty);
            }
            return null;
        }
    }
}
