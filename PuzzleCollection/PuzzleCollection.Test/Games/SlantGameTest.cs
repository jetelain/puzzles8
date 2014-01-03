using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using PuzzleCollection.Games;
using PuzzleCollection.Games.Slant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Test.Games
{
    /// <summary>
    /// Unit tests of <see cref="SlantGame"/>.
    /// </summary>
    [TestClass]
    public class SlantGameTest
    {
        /// <summary>
        /// Test SlantSettings serialization
        /// </summary>
        [TestMethod]
        public void SlantGame_Settings()
        {
            var game = new SlantGame();
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
        public void SlantGame_GenerateNewGameDescription()
        {
            var game = new SlantGame();
            string aux;
            string result;
            result = game.GenerateNewGameDescription(SlantSettings.Parse("5x5de"), OriginalRandom.FromTextSeed("635826315844811"), out aux, 0);
            Assert.AreEqual("a1b2a1b3a2b23b11c2122a1a0e", result);

            result = game.GenerateNewGameDescription(SlantSettings.Parse("5x5dh"), OriginalRandom.FromTextSeed("142071390180577"), out aux, 0);
            Assert.AreEqual("a1a1c11a21a2b3b32d1122g", result);

            result = game.GenerateNewGameDescription(SlantSettings.Parse("8x8de"), OriginalRandom.FromTextSeed("379450086684295"), out aux, 0);
            Assert.AreEqual("c12d2a1b3a4a0c1a4d3f211d1b2a3222b2b21c2b3c42b2b1d", result);

            result = game.GenerateNewGameDescription(SlantSettings.Parse("8x8dh"), OriginalRandom.FromTextSeed("554613413128852"), out aux, 0);
            Assert.AreEqual("b1a1d0b2a31a11a31a122c222a23a13a32a1b122a1b2a1b1a322c312c1a11c11a", result);

            result = game.GenerateNewGameDescription(SlantSettings.Parse("12x10de"), OriginalRandom.FromTextSeed("425060843341517"), out aux, 0);
            Assert.AreEqual("a120c11j2d2002b12a13g3122b23a1221c2d20b1a32a2b3b02a1a23b2a22a12a3c2a3a1b23a2a222c222b223b21g2a2c1", result);

            result = game.GenerateNewGameDescription(SlantSettings.Parse("12x10dh"), OriginalRandom.FromTextSeed("478445348963833"), out aux, 0);
            Assert.AreEqual("c11a11b1c22a13a122a1b3a2c2d1122a21a2c11a1231d3a2c2a3b332a3a13b123b1a11a3c3a21b3e13b1a21b11a1222a21a1b1j", result);
        }
    }
}
