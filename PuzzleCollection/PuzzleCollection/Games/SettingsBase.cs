using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    public abstract class SettingsBase
    {
        internal abstract string ToTitle();

        internal abstract string ToId(bool full);

        public string Id
        {
            get { return ToId(true); }
        }

        public override string ToString()
        {
            return ToId(true);
        }
    }
}
