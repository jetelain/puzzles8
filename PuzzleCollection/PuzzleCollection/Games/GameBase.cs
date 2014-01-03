using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace PuzzleCollection.Games
{
    public abstract class GameBase<TSettings, TState, TMove, TDrawState, TUI> : IGame
        where TSettings : SettingsBase
        where TState : StateBase
        where TMove : MoveBase
        where TDrawState : class
        where TUI : UIBase
    {
        public abstract TSettings DefaultSettings { get; }

        public abstract IEnumerable<TSettings> PresetsSettings { get; }

        /// <summary>
        /// Parse string representation of settings
        /// </summary>
        /// <param name="settingsString"></param>
        /// <returns>parsed settings or null if invalid</returns>
        public abstract TSettings ParseSettings(string settingsString);

        public abstract string GenerateNewGameDescription(
            TSettings @params,
            Random rs,
            out string aux,
            int interactive);

        public abstract TState CreateNewGameFromDescription(
            TSettings @params,
            string desc);

        public abstract TState ExecuteMove(
            TState from, 
            TMove move);

        /// <summary>
        /// Parse string representation of a move
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="moveString"></param>
        /// <returns>Parsed move, or null if invalid</returns>
        public abstract TMove ParseMove(TSettings settings, string moveString);

        public abstract TMove CreateSolveGameMove(
            TState state, 
            TState currstate,
            TMove ai, 
            out string error);

        public abstract TDrawState CreateDrawState(Drawing dr, TState state);

        public abstract void ComputeSize(TSettings @params, int tilesize,
                              out int x, out int y);

        public abstract void SetTileSize(Drawing dr, TDrawState ds,
                           TSettings @params, int tilesize);

        public abstract float[] GetColours(Frontend fe, out int ncolours);

        public abstract void Redraw(Drawing dr, TDrawState ds,
                                TState oldstate, TState state,
                                int dir, TUI ui,
                                float animtime, float flashtime);

        public abstract TUI CreateUI(TState state);

        public abstract TMove InterpretMove(TState state, TUI ui,
                            TDrawState ds,
                            int x, int y, Buttons button, bool isTouchOrStylus);

        public IGameController CreateController()
        {
            return new GameController<TSettings, TState, TMove, TDrawState, TUI>(this);
        }

        public virtual float CompletedFlashDuration(TSettings settings)
        {

                return 0.30f;
            
        }

        public virtual float AnimDuration
        {
            get
            {
                return 0.0f;
            }
        }

        internal abstract void SetKeyboardCursorVisible(TUI ui, int tileSize, bool value);

        public virtual void GameStateChanged(TUI ui, TState oldstate, TState newstate)
        {

        }

        internal virtual bool HoldToRightMouse
        {
            get { return false; }
        }
    }
}
