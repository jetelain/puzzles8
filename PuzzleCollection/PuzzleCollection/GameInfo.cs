using PuzzleCollection.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleCollection
{
    class GameInfo
    {
        public string Title { get; set; }

        public IGame Game { get; set; }

        public string GameId { get; set; }

        public string Subtitle { get; set; }

        public string Image { get; set; }

        internal static GameInfo Create<T>()
            where T : IGame, new()
        {
            string id = typeof(T).Name;
            if (id.EndsWith("Game")) id = id.Substring(0, id.Length - 4);
            return new GameInfo()
            {
                GameId = id,
                Game = new T(),
                Title = Labels.GetString(id + "Title"),
                Subtitle = Labels.GetString(id + "Description"),
                Image = "ms-appx:///Assets/" + id + "Preview.png"
            };
        }

    }
}
