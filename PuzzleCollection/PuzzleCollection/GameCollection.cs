using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection
{
    internal static class GameCollection
    {
        private static volatile List<GameInfo> games;

        private static List<GameInfo> BuildGamesList()
        {
            List<GameInfo> list = new List<GameInfo>();
            list.Add(GameInfo.Create<PuzzleCollection.Games.Pattern.PatternGame>());
            list.Add(GameInfo.Create<PuzzleCollection.Games.Map.MapGame>());
            list.Add(GameInfo.Create<PuzzleCollection.Games.Lightup.LightupGame>());
            list.Add(GameInfo.Create<PuzzleCollection.Games.Net.NetGame>());
            list.Add(GameInfo.Create<PuzzleCollection.Games.Untangle.UntangleGame>());
            list.Add(GameInfo.Create<PuzzleCollection.Games.Bridges.BridgesGame>());
            list.Add(GameInfo.Create<PuzzleCollection.Games.SameGame.SameGameGame>());
            list.Add(GameInfo.Create<PuzzleCollection.Games.SignPost.SignPostGame>());
            list.Add(GameInfo.Create<PuzzleCollection.Games.Slant.SlantGame>());
            list.Sort((a, b) => a.Title.CompareTo(b.Title));
            return list;
        }

        internal static List<GameInfo> GamesList
        {
            get
            {
                if (games == null)
                {
                    games = BuildGamesList();
                }
                return games;
            }
        }

        internal static GameInfo GetGameById(string gameId)
        {
            return GamesList.First(g => g.GameId == gameId);
        }
    }
}
