using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Net
{
    public class NetSettings : SettingsBase
    {
        internal int width;
        internal int height;
        internal bool wrapping;
        internal bool unique;
        internal float barrier_probability;

        public NetSettings(int width, int height, bool wrapping, bool unique, float barrier_probability)
        {
            this.width = width;
            this.height = height;
            this.wrapping = wrapping;
            this.unique = unique;
            this.barrier_probability = barrier_probability;
        }

        internal override string ToTitle()
        {
            if (wrapping)
            {
                return string.Format("{0}x{1} wrapping", width, height);
            }
            return string.Format("{0}x{1}",width,height);
        }
        internal override string ToId(bool full)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(width);
            builder.Append('x');
            builder.Append(height);
            if (wrapping)
            {
                builder.Append('w');
            }
            if ( full )
            {
                if (barrier_probability != 0)
                {
                    builder.Append('b').Append(barrier_probability.ToString(CultureInfo.InvariantCulture));
                }
                if (!unique)
                {
                    builder.Append('a');
                }
            }
            return builder.ToString();
        }

        private static readonly Regex SettingsRegex = new Regex("^([0-9]+)x([0-9]+)(w)?(b([0-9\\.]+))?(a)?$", RegexOptions.CultureInvariant);

        internal static NetSettings Parse(string settingsString)
        {
            var match = SettingsRegex.Match(settingsString);
            if (match.Success)
            {
                float proba = 0.0f;
                if (match.Groups[4].Success)
                {
                    proba = float.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);
                }
                return new NetSettings(
                    width: int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    height: int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                    wrapping: match.Groups[3].Success,
                    barrier_probability: proba,
                    unique: !match.Groups[6].Success
                );
            }
            return null;
        }
    }
}
