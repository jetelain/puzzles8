using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using PuzzleCollection.Games;
using PuzzleCollection.Games.SameGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Test.Games
{
    /// <summary>
    /// Unit tests of <see cref="SameGameGame"/>.
    /// </summary>
    [TestClass]
    public class SameGameGameTest
    {
        /// <summary>
        /// Test SameGameSettings serialization
        /// </summary>
        [TestMethod]
        public void SameGameGame_Settings()
        {
            var game = new SameGameGame();
            // Ensures that ParseSettings is correct
            foreach (var settings in game.PresetsSettings)
            {
                Assert.AreEqual(settings.Id, game.ParseSettings(settings.Id).Id);
            }
        }

        /// <summary>
        /// Check generation using a specific seed (results collected from reference c implementation)
        /// </summary>
        [TestMethod]
        public void SameGameGame_GenerateNewGameDescription()
        {
            var game = new SameGameGame();
            string aux;
            string result;

            result = game.GenerateNewGameDescription(SameGameSettings.Parse("5x5c3s2"), OriginalRandom.FromTextSeed("730224562078661"), out aux, 0);
            Assert.AreEqual("3,1,2,2,1,2,1,1,3,1,2,2,3,2,3,1,3,1,2,1,1,2,3,3,1", result);

            result = game.GenerateNewGameDescription(SameGameSettings.Parse("10x5c3s2"), OriginalRandom.FromTextSeed("637779874876585"), out aux, 0);
            Assert.AreEqual("1,1,2,1,3,3,1,2,3,2,2,3,2,3,1,1,3,1,3,2,2,3,1,3,2,2,1,3,1,3,3,1,3,2,1,3,3,2,2,3,3,1,3,2,3,2,1,3,1,1", result);

            result = game.GenerateNewGameDescription(SameGameSettings.Parse("10x10c3s2"), OriginalRandom.FromTextSeed("163974258744376"), out aux, 0);
            Assert.AreEqual("2,2,2,3,1,3,2,1,3,2,2,3,2,3,2,3,2,1,3,2,1,3,1,1,1,1,1,3,2,3,1,2,3,1,2,3,1,3,2,3,3,1,2,3,2,3,2,1,1,1,2,1,2,1,3,2,2,3,2,2,1,2,3,1,2,3,3,1,3,1,1,1,1,3,1,1,2,3,3,2,3,2,3,1,2,3,2,1,2,3,2,1,3,2,3,1,3,1,2,3", result);

            result = game.GenerateNewGameDescription(SameGameSettings.Parse("15x10c4s2"), OriginalRandom.FromTextSeed("285987464958328"), out aux, 0);
            Assert.AreEqual("1,4,3,4,4,1,1,1,2,4,1,3,3,2,1,4,2,3,1,2,2,3,4,2,2,2,3,1,3,1,2,4,4,1,4,1,3,4,4,2,2,2,4,1,4,4,2,2,3,1,2,1,1,2,3,4,2,1,2,4,3,4,4,3,4,3,3,2,1,1,1,4,4,2,1,3,1,4,4,2,3,2,4,2,4,3,3,1,4,1,2,3,1,4,4,2,3,2,1,3,2,2,4,2,3,1,4,1,2,1,4,1,1,3,4,2,4,2,4,3,1,3,4,2,4,1,3,4,2,3,1,3,4,1,4,3,2,4,3,1,4,4,3,2,4,1,3,2,2,4", result);

            result = game.GenerateNewGameDescription(SameGameSettings.Parse("20x15c4s2"), OriginalRandom.FromTextSeed("915767191012700"), out aux, 0);
            Assert.AreEqual("3,2,2,1,4,3,3,2,3,1,1,3,1,1,4,2,3,4,4,2,3,3,3,1,2,4,4,2,1,1,3,4,4,2,2,2,2,3,4,3,1,2,4,2,2,4,3,1,4,4,3,4,3,2,3,1,3,2,2,3,4,2,3,1,4,3,4,2,4,3,4,1,3,3,1,4,2,3,1,1,2,1,3,2,2,3,4,2,1,4,4,2,1,2,3,4,1,2,3,1,4,2,2,2,2,1,3,4,3,4,3,4,2,2,2,1,1,4,3,2,1,3,3,1,1,3,2,3,2,1,3,4,1,1,1,4,3,1,1,2,4,3,3,4,3,1,2,3,1,2,1,2,4,2,3,2,3,3,4,4,1,2,2,4,3,1,4,2,3,4,3,1,4,1,1,2,4,3,1,2,4,4,4,3,1,4,3,4,3,4,2,3,3,4,3,4,4,2,1,3,3,2,1,3,2,1,3,2,4,2,2,3,4,2,4,3,2,3,3,3,4,1,1,4,3,4,1,4,1,1,1,4,3,3,2,1,1,4,2,1,2,4,3,1,2,1,1,2,4,3,1,4,1,4,1,3,2,2,1,4,3,4,2,2,3,2,4,3,4,2,4,2,4,3,2,1,3,4,1,3,3,1,3,1,4,3,4,2,1,2,1,2,4,1,2,3,2,4,2,1", result);
        }
    }
}
