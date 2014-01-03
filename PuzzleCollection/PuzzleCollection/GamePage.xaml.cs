using Callisto.Controls;
using PuzzleCollection.Games;
using System;
using System.Collections.Generic;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// Pour en savoir plus sur le modèle d'élément Page Détail de l'élément, consultez la page http://go.microsoft.com/fwlink/?LinkId=234232

namespace PuzzleCollection
{
    /// <summary>
    /// Page affichant les détails d'un élément au sein d'un groupe, offrant la possibilité de
    /// consulter les autres éléments qui appartiennent au même groupe.
    /// </summary>
    public sealed partial class GamePage : PuzzleCollection.Common.LayoutAwarePage
    {
        private GameInfo gameInfo;
        private IGameController controller;

        public GamePage()
        {
            this.InitializeComponent();
            this.Unloaded += GamePage_Unloaded;
            this.Loaded += GamePage_Loaded;
        }

        private void GamePage_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            d2dRectangle.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }

        private void GamePage_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
        }

        /// <summary>
        /// Remplit la page à l'aide du contenu passé lors de la navigation. Tout état enregistré est également
        /// fourni lorsqu'une page est recréée à partir d'une session antérieure.
        /// </summary>
        /// <param name="navigationParameter">Valeur de paramètre passée à
        /// <see cref="Frame.Navigate(Type, Object)"/> lors de la requête initiale de cette page.
        /// </param>
        /// <param name="pageState">Dictionnaire d'état conservé par cette page durant une session
        /// antérieure. Null lors de la première visite de la page.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // Autorise l'état de page enregistré à substituer l'élément initial à afficher
            if (pageState != null && pageState.ContainsKey("GameId"))
            {
                navigationParameter = pageState["GameId"];
            }
            var gameId = (string)navigationParameter;
            gameInfo = GameCollection.GetGameById(gameId);
            controller = gameInfo.Game.CreateController();

            var settings = GameSettingsManager.GetGameSettings(gameId);
            if (!string.IsNullOrEmpty(settings))
            {
                controller.RestoreSettings(settings);
            }


            GameSave save = null;
            if (pageState != null && pageState.ContainsKey("GameSave"))
            {
                save = (GameSave)pageState["GameSave"];
            }
            else if (!GameStateManager.States.TryGetValue(gameId, out save))
            {
                save = null;
            }
            
            if (save != null)
            {
                controller.RestoreGame(save);
            }
            else
            {
                controller.NewGame();
            }
            controller.GameCompleted += GameCompleted;
            this.DefaultViewModel["Title"] = gameInfo.Title;
            this.DefaultViewModel["Controller"] = controller;
            d2dRectangle.SetGameController(controller);
        }

        private async void NewGameWithPrompt(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!controller.HasDoneAnyMove)
            {
                controller.NewGame();
                return;
            }
            MessageDialog dialog = new MessageDialog(Labels.GetString("NewGamePrompt"), Labels.NewGame);
            dialog.Commands.Add(new UICommand(Labels.NewGame, (c) => controller.NewGame()));
            dialog.Commands.Add(new UICommand(Labels.Cancel));
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;
            await dialog.ShowAsync();
        }

        private async void RestartWithPrompt(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (!controller.HasDoneAnyMove)
            {
                return;
            }
            MessageDialog dialog = new MessageDialog(Labels.GetString("RestartPrompt"), Labels.RestartGame);
            dialog.Commands.Add(new UICommand(Labels.RestartGame, (c) => controller.RestartGame()));
            dialog.Commands.Add(new UICommand(Labels.Cancel));
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;
            await dialog.ShowAsync();
        }

        private async void GameCompleted()
        {
            MessageDialog dialog = new MessageDialog(Labels.GetString("GameCompletedPrompt"), Labels.GetString("GameCompletedTilte"));
            dialog.Commands.Add(new UICommand(Labels.NewGame, (c) => controller.NewGame()));
            dialog.Commands.Add(new UICommand(Labels.RestartGame, (c) => controller.RestartGame()));
            dialog.Commands.Add(new UICommand(Labels.Close));
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 2;
            await dialog.ShowAsync();
        }

        /// <summary>
        /// Conserve l'état associé à cette page en cas de suspension de l'application ou de la
        /// suppression de la page du cache de navigation. Les valeurs doivent être conformes aux
        /// exigences en matière de sérialisation de <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">Dictionnaire vide à remplir à l'aide de l'état sérialisable.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            pageState["GameId"] = gameInfo.GameId;
            pageState["GameSave"] = controller.ToGameSave(); 
        }

        protected override void OnNavigatedFrom(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (!controller.IsCompleted)
            {
                GameStateManager.States[gameInfo.GameId] = controller.ToGameSave(); 
            }
            else
            {
                GameStateManager.States.Remove(gameInfo.GameId);
            }
            GameStateManager.SaveAsync();
        }


        private void GameDifficultyShow(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Callisto.Controls.Flyout f = new Callisto.Controls.Flyout();
            f.Placement = PlacementMode.Top;
            f.PlacementTarget = (UIElement)sender; 

            Menu m = new Menu();
            foreach (var preset in controller.GetPresetSettings())
            {
                ToggleMenuItem mi = new ToggleMenuItem();
                mi.Text = preset.Title;
                mi.IsChecked = preset.IsCurrent;
                mi.KeyDown += (miS, miE) =>
                {
                    // XXX: Might be fixed in future Callisto release.
                    if (miE.Key == VirtualKey.Enter)
                    {
                        miE.Handled = true; 
                        f.IsOpen = false;
                        SelectPreset(preset);
                    }
                };
                mi.Tapped += (miS, miE) => 
                {
                    SelectPreset(preset);
                };
                m.Items.Add(mi);
            }
            f.Content = m;
            f.IsOpen = true;
        }

        private void SelectPreset(GamePresetSettings preset)
        {
            controller.SetSettingsFromPreset(preset);
            GameSettingsManager.SetGameSettings(gameInfo.GameId, controller.CurrentSettingsId);
            BottomAppBar.IsOpen = false; 
        }

        private void ShowGameHelp(object sender, RoutedEventArgs e)
        {
            App.ShowHelp(gameInfo.GameId);
        }

        private bool IsCtrlPressed
        {
            get { return (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down; }
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.F1:
                    ShowGameHelp(this, null);
                    args.Handled = true;
                    break;
            }
            if (IsCtrlPressed)
            {
                switch (args.VirtualKey)
                {
                    case VirtualKey.N:
                        NewGameWithPrompt(this, null);
                        args.Handled = true;
                        break;
                    case VirtualKey.Z:
                        if (controller.UndoCommand.CanExecute(null))
                        {
                            controller.UndoCommand.Execute(null);
                        }
                        args.Handled = true;
                        break;
                    case VirtualKey.Y:
                        if (controller.RedoCommand.CanExecute(null))
                        {
                            controller.RedoCommand.Execute(null);
                        }
                        args.Handled = true;
                        break;
                    case VirtualKey.R:
                        RestartWithPrompt(this, null);
                        args.Handled = true;
                        break;
                    case VirtualKey.D:
                        GameDifficultyShow(this, null);
                        args.Handled = true;
                        break;
                }
            }
        }
    }
}
