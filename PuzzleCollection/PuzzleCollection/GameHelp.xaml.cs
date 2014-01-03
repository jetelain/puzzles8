using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Pour en savoir plus sur le modèle d'élément Contrôle utilisateur, consultez la page http://go.microsoft.com/fwlink/?LinkId=234236

namespace PuzzleCollection
{
    public sealed class GameHelpViewModel
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Goal { get; set; }
        public string MouseCommands { get; set; }
        public string TouchCommands { get; set; }
        public string Credits { get; set; }
        public string SourceUri { get; set; }
    }

    public sealed partial class GameHelp : UserControl
    {
        internal GameHelp(GameInfo info)
        {
            this.InitializeComponent();
            this.DataContext = new GameHelpViewModel()
            {
                Title = info.Title,
                Subtitle = info.Subtitle,
                Goal = Labels.GetString("Help" + info.GameId + "Goal"),
                MouseCommands = Labels.GetString("Help" + info.GameId + "MouseCommands"),
                TouchCommands = Labels.GetString("Help" + info.GameId + "TouchCommands"),
                Credits = Labels.GetString("Help" + info.GameId + "Credits"),
                SourceUri = "http://www.chiark.greenend.org.uk/~sgtatham/puzzles/doc/"+info.GameId.ToLowerInvariant()+".html"
            };
        }
    }
}
