using PuzzleCollection.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace PuzzleCollection.Games
{

    class GameController<TSettings, TState, TMove, TDrawState, TUI> : IGameController
        where TSettings : SettingsBase
        where TState : StateBase
        where TMove : MoveBase
        where TDrawState : class
        where TUI : UIBase
    {
        class GameControllerState
        {
            internal readonly TState state;
            internal readonly TMove move;

            internal GameControllerState(TState state)
            {
                this.state = state;
            }

            internal GameControllerState(TMove move, TState state)
            {
                this.state = state;
                this.move = move;
            }
        }


        private readonly GameBase<TSettings, TState, TMove, TDrawState, TUI> game;
        private readonly ActionCommand undoCommand;
        private readonly ActionCommand redoCommand;
        private readonly List<GameControllerState> history = new List<GameControllerState>();
        private readonly List<GameControllerState> redoList = new List<GameControllerState>();
        private readonly DispatcherTimer timer;

        TState oldState;
        TState currentState;
        TState initialState;
        TSettings settings;
        TUI ui;
        int tileSize = 32;
        int pixelWidth;
        int pixelHeight;
        string gameDescription;

        Stopwatch flashWatch = new Stopwatch();
        int flashDuration = -1;

        Stopwatch animWatch = new Stopwatch();
        int animDuration = -1;

        internal GameController(GameBase<TSettings, TState, TMove, TDrawState, TUI> game)
        {
            this.game = game;
            settings = game.DefaultSettings;
            undoCommand = new ActionCommand(UndoMove, false);
            redoCommand = new ActionCommand(RedoMove, false);
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(5);
            timer.Tick += TimerTick;
        }

        private void RedoMove()
        {
            if (redoList.Count > 0)
            {
                var previousState = currentState;
                var controllerState = redoList[redoList.Count - 1];
                currentState = controllerState.state;
                redoList.RemoveAt(redoList.Count - 1);
                history.Add(controllerState);

                undoCommand.CanExecuteCommand = true;
                redoCommand.CanExecuteCommand = redoList.Count > 0;

                game.GameStateChanged(ui, previousState, currentState);
                AskRedraw();
            }
        }

        private void UndoMove()
        {
            if (history.Count > 1)
            {
                var previousState = currentState;
                redoList.Add(history[history.Count - 1]);
                currentState = history[history.Count - 2].state;
                history.RemoveAt(history.Count - 1);

                undoCommand.CanExecuteCommand = history.Count > 1;
                redoCommand.CanExecuteCommand = true;

                game.GameStateChanged(ui, previousState, currentState);
                AskRedraw();
            }
        }

        public void RestartGame()
        {
            history.Clear();
            history.Add(new GameControllerState(initialState));
            currentState = initialState;
            redoList.Clear();
            ui = game.CreateUI(initialState);
            undoCommand.CanExecuteCommand = false;
            redoCommand.CanExecuteCommand = false;
            AskRedraw();
        }

        public void PrepareColors(Drawing dr)
        {
            int count;
            var values = game.GetColours(new Frontend(), out count);
            for (int i = 0; i < count; ++i)
            {
                dr.AddColor(new Color(values[i * 3], values[i * 3 + 1], values[i * 3 + 2]));
            }
        }

        public void Draw(Drawing dr)
        {
            float flashTime = 0.0f;
            float animTime = 0.0f;
            if (flashDuration != -1)
            {
                flashTime = flashWatch.ElapsedMilliseconds / 1000.0f;
                //Debug.WriteLine("Draw with flashTime={0}", flashTime);
            }
            if (animDuration != -1)
            {
                animTime = animWatch.ElapsedMilliseconds / 1000.0f;
                //Debug.WriteLine("Draw with animTime={0}", animTime);
            }
            var drawState = game.CreateDrawState(dr, currentState);
            game.SetTileSize(dr, drawState, settings, tileSize);
            game.Redraw(dr, drawState, oldState, currentState, (animDuration != -1 ? 1 : 0), ui, animTime, flashTime);
        }


        public bool MouseEvent(Drawing dr, int x, int y, Buttons buttons, bool isTouchOrStylus)
        {
            if (flashDuration != -1)
            {
                return false; // while an animation is running, events are ignored
            }
            if (IsCompleted)
            {
                return false; // Game is completed, no more move is allowed
            }
            
            var drawState = game.CreateDrawState(dr, currentState);
            game.SetTileSize(dr, drawState, settings, tileSize);

            var move = game.InterpretMove(currentState, ui,
                            drawState,
                            x, y, buttons, isTouchOrStylus);
            if (move != null)
            {
                string moveId = move.ToId();
                Debug.WriteLine("ExecuteMove Move='{0}'", moveId);
                // quick test of ParseMove method
                Debug.Assert(game.ParseMove(settings, moveId).ToId() == moveId, "Error in ToId() or Parse()");

                var previousState = currentState;
                currentState = game.ExecuteMove(currentState, move);
                history.Add(new GameControllerState(move, currentState));
                undoCommand.CanExecuteCommand = true;
                if (redoList.Count > 0)
                {
                    redoCommand.CanExecuteCommand = false;
                    redoList.Clear();
                }
                OnMoveDone(previousState, currentState);
            }
            AskRedraw();
            return (move != null);
        }

        private void OnMoveDone(TState previousState, TState currentState)
        {
            game.GameStateChanged(ui, previousState, currentState);
            //Debug.WriteLine("OnMoveDone");
            if (!previousState.IsCompleted && currentState.IsCompleted && !currentState.HasCheated)
            {
                flashDuration = (int)(game.CompletedFlashDuration(settings) * 1000);
                flashWatch.Restart();
                timer.Start();
            }
            else if (game.AnimDuration != 0.0f)
            {
                oldState = previousState;
                animDuration = (int)(game.AnimDuration * 1000);
                animWatch.Restart();
                timer.Start();
            }
        }


        private void TimerTick(object sender, object e)
        {
            //Debug.WriteLine("[TimerTick]");
            if (animDuration != -1)
            {
                //Debug.WriteLine("Anim Elapsed={0}", animWatch.ElapsedMilliseconds);
                if (animWatch.ElapsedMilliseconds > animDuration)
                {
                    animDuration = -1;
                    timer.Stop();
                    animWatch.Stop();
                    oldState = null;
                }
                AskRedraw();
            }
            else if (flashDuration != -1)
            {
                //Debug.WriteLine("Flash Elapsed={0}", flashWatch.ElapsedMilliseconds);
                if (flashWatch.ElapsedMilliseconds > flashDuration)
                {
                    flashDuration = -1;
                    timer.Stop();
                    flashWatch.Stop();
                    AskRedraw();
                    OnGameCompleted();
                    return;
                }
                AskRedraw();
            }
        }

        private void OnGameCompleted()
        {
            if (GameCompleted != null)
            {
                GameCompleted();
            }
        }

        private void ClearState()
        {
            flashDuration = -1;
            animDuration = -1;
            if (timer.IsEnabled)
            {
                timer.Stop();
            }
            history.Clear();
            redoList.Clear();
            undoCommand.CanExecuteCommand = false;
            redoCommand.CanExecuteCommand = false;
        }

        public void NewGame()
        {
            Debug.WriteLine("[NewGame]");
            ClearState();
            
            string aux;
            Debug.WriteLine("GenerateNewGameDescription Settings='{0}'", settings.ToId(true));
            gameDescription = game.GenerateNewGameDescription(settings, new Random(), out aux, 0);
            Debug.WriteLine("CreateNewGameFromDescription Settings='{0}' Description='{1}'", settings.ToId(true), gameDescription);
            initialState = currentState = game.CreateNewGameFromDescription(settings, gameDescription);
            ui = game.CreateUI(initialState);
            history.Add(new GameControllerState(initialState));
            AskRedraw();
        }

        public ICommand UndoCommand
        {
            get { return undoCommand; }
        }

        public ICommand RedoCommand
        {
            get { return redoCommand; }
        }



        public void DrawSurfaceSizeChanged(int pixelWidth, int pixelHeight)
        {
            this.pixelHeight = pixelHeight;
            this.pixelWidth = pixelWidth;
            ComputeTileSize();
        }

        private void ComputeTileSize()
        {
            if (pixelWidth == 0 && pixelHeight == 0)
            {
                return;
            }
            int max = 1, min = 1;
            int width, height;
	        do 
            {
	            max *= 2;
	            game.ComputeSize(settings, max, out width, out height);
	        } 
            while (width <= pixelWidth && height <= pixelHeight);

            /*
             * Now binary-search between min and max. We're looking for a
             * boundary rather than a value: the point at which tile sizes
             * stop fitting within the given dimensions. Thus, we stop when
             * max and min differ by exactly 1.
             */
            while (max - min > 1)
            {
	            int mid = (max + min) / 2;
                game.ComputeSize(settings, mid, out width, out height);
                if (width <= pixelWidth && height <= pixelHeight)
                {
                    min = mid;
                }
                else
                {
                    max = mid;
                }
            }
            tileSize = min;
        }

        public void AskRedraw()
        {
            if (Redraw != null)
            {
                Redraw();
            }
        }

        public IEnumerable<GamePresetSettings> GetPresetSettings()
        {
            return game.PresetsSettings.Select(s => new GamePresetSettings() { Title = s.ToTitle(), Settings = s, IsCurrent = s == settings });
        }


        public event Action Redraw;
        public event Action GameCompleted;

        public bool IsCompleted
        {
            get { return currentState.IsCompleted; } 
        }

        public bool HasDoneAnyMove
        {
            get { return history.Count > 1; }
        }

        public void SetSettingsFromPreset(GamePresetSettings preset)
        {
            settings = (TSettings)preset.Settings;
            ComputeTileSize();
            NewGame();
        }

        private TSettings GetSettings(string id)
        {
            // If settings exists in presets, use the preset instance (to easily distinguish presets of custom settings)
            var settings = game.PresetsSettings.FirstOrDefault(s => s.Id == id);
            if (settings != null)
            {
                return settings;
            }
            return game.ParseSettings(id);
        }

        public void RestoreGame(GameSave save)
        {
            ClearState();

            settings = GetSettings(save.Settings);
            if (settings == null)
            {
                Debug.WriteLine("ERROR: Settings in GameSave are corrupted. Start a new game with default settings.");
                settings = game.DefaultSettings;
                NewGame();
                return;
            }
            gameDescription = save.Description;
            initialState = currentState = game.CreateNewGameFromDescription(settings, gameDescription);
            ui = game.CreateUI(initialState);
            history.Add(new GameControllerState(initialState));

            foreach (var moveString in save.HistoryMoves)
            {
                var move = game.ParseMove(settings, moveString);
                currentState = game.ExecuteMove(currentState, move);
                history.Add(new GameControllerState(move, currentState));
            }

            var redoState = currentState;
            foreach (var moveString in save.RedoListMoves)
            {
                var move = game.ParseMove(settings, moveString);
                redoState = game.ExecuteMove(redoState, move);
                redoList.Insert(0, new GameControllerState(move, redoState));
            }

            undoCommand.CanExecuteCommand = history.Count > 1;
            redoCommand.CanExecuteCommand = redoList.Count > 0;
            AskRedraw();
        }

        public GameSave ToGameSave()
        {
            var save = new GameSave();
            save.Settings = settings.Id;
            save.Description = gameDescription;
            save.HistoryMoves = history.Where(s => s.move != null).Select(s => s.move.ToId()).ToArray();
            save.RedoListMoves = redoList.Select(s => s.move.ToId()).Reverse().ToArray();
            return save;
        }

        public void RestoreSettings(string settingsId)
        {
            if (currentState != null)
            {
                throw new InvalidOperationException("A game was already started or restored");
            }
            settings = GetSettings(settingsId);
            if (settings == null)
            {
                Debug.WriteLine("ERROR: Invalid settingsId '{0}'.", settings);
                settings = game.DefaultSettings;
            }
        }

        public string CurrentSettingsId
        {
            get { return settings.Id; }
        }


        public Windows.Foundation.Size ActualSize
        {
            get 
            {
                int width, height;
                game.ComputeSize(settings, tileSize, out width, out height);
                return new Windows.Foundation.Size(width, height);
            }
        }


        public bool IsKeyboardCursorVisible
        {
            get
            {
                return ui != null && ui.IsKeyboardCursorVisible;
            }
            set
            {
                if (ui != null && ui.IsKeyboardCursorVisible != value)
                {
                    game.SetKeyboardCursorVisible(ui, tileSize, value);
                    AskRedraw();
                }
            }
        }


        public bool HoldToRightMouse
        {
            get
            {
                return game.HoldToRightMouse;
            }
        }
    }
}
