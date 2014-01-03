using Callisto.Controls;
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

namespace PuzzleCollection
{
    public sealed partial class HelpPane : UserControl
    {
        private readonly Callisto.Controls.SettingsFlyout owner;
        private readonly UIElement InitialContent;

        public HelpPane(Callisto.Controls.SettingsFlyout owner)
        {
            this.InitializeComponent();
            this.DataContext = new Dictionary<String, Object>() { { "Games", GameCollection.GamesList } };
            this.owner = owner;
            owner.BackClicked += BackClicked;
            this.InitialContent = this.Content;
        }

        private void BackClicked(object sender, BackClickedEventArgs e)
        {
            if (Content != InitialContent)
            {
                Content = InitialContent;
                e.Cancel = true;
            }
        }

        public void ShowGameHelp(string gameId)
        {
            GameHelp helpContent = new GameHelp(GameCollection.GetGameById(gameId));
            Content = helpContent;
        }

 
        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ShowGameHelp(((GameInfo)(e.ClickedItem)).GameId);
        }

    }
}
