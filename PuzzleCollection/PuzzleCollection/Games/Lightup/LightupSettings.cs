using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Lightup
{
    public sealed class LightupSettings : SettingsBase
    {
        internal readonly int w;
        internal readonly int h;
        internal int blackpc;        /* %age of black squares */
        internal readonly int symm;
        internal readonly int difficulty;     /* 0 to DIFFCOUNT */

        internal LightupSettings(int w, int h, int blackpc, int symm, int difficulty)
        {
            if (symm == 5)
            {
                // First version had a bug, SYMM_ROT4 was defined as 5 but it was intend to be 4
                symm = 4;
            }
            this.w = w; 
            this.h = h;
            this.blackpc = blackpc;
            this.symm = symm;
            this.difficulty = difficulty;
        }

        internal LightupSettings Clone()
        {
            return new LightupSettings(
                w : w,
                h : h,
                blackpc : blackpc,
                symm : symm,
                difficulty : difficulty
            );
        }
        private static string DifficultyTitle(int diff)
        {
            switch (diff)
            {
                case 0:
                    return Labels.Easy;
                case 1:
                    return Labels.Tricky;
                case 2:
                    return Labels.Hard;
                default:
                    return Labels.Unknown;
            }
        }
        internal override string ToTitle()
        {
            return string.Format("{0}x{1}, {2}", w, h, DifficultyTitle(difficulty));
        }

        internal override string ToId(bool full)
        {
            if (full) 
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}x{1}b{2}s{3}d{4}", w, h, blackpc, symm, difficulty);
            } 
            return string.Format(CultureInfo.InvariantCulture, "{0}x{1}", w, h);
        }

        private static readonly Regex SettingsRegex = new Regex("^([0-9]+)x([0-9]+)(b([0-9]+)s([0-9]+)d([0-9]+))?$", RegexOptions.CultureInvariant);
        
        public static LightupSettings Parse(string settingsString)
        {
            var match = SettingsRegex.Match(settingsString);
            if (match.Success)
            {
                if (match.Groups[3].Success)
                {
                    return new LightupSettings(
                    
                        w : int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                        h : int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),

                        blackpc : int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
                        symm : int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture),
                        difficulty : int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture)
                    );
                }
                return new LightupSettings(
                    w : int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    h : int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                    blackpc: 0, // unknown (not full)
                    symm: 0, // unknown (not full)
                    difficulty: 0 // unknown (not full)
                );
            }
            return null;
        }
    }
}
