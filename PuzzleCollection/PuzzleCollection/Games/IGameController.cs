using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;

namespace PuzzleCollection.Games
{
    public interface IGameController 
    {
        void PrepareColors(Drawing dr);

        void Draw(Drawing dr);

        bool MouseEvent(Drawing dr, int x, int y, Buttons buttons, bool isTouchOrStylus);

        ICommand UndoCommand { get; }

        ICommand RedoCommand { get; }

        void DrawSurfaceSizeChanged(int pixelWidth, int pixelHeight);

        IEnumerable<GamePresetSettings> GetPresetSettings();

        event Action Redraw;

        event Action GameCompleted;

        void SetSettingsFromPreset(GamePresetSettings preset);

        void NewGame();

        void RestartGame();

        bool HasDoneAnyMove { get; }

        void RestoreGame(GameSave save);

        GameSave ToGameSave();

        bool IsCompleted { get; }

        void RestoreSettings(string settingsId);

        string CurrentSettingsId { get; }

        Size ActualSize { get; }

        bool IsKeyboardCursorVisible { get; set; }

        bool HoldToRightMouse { get; }
    }
}
