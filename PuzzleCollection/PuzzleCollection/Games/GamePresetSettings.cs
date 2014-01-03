using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games
{
    public class GamePresetSettings
    {
        internal string Title { get; set; }

        internal SettingsBase Settings { get; set; }

        internal bool IsCurrent { get; set; }
    }
}
