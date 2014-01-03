using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using PuzzleCollection.Games;
using PuzzleCollection.Games.Pattern;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Test.Games
{
    /// <summary>
    /// Unit tests of <see cref="PatternGame"/>.
    /// </summary>
    [TestClass]
    public class PatternGameTest
    {
        /// <summary>
        /// Test PatternSettings serialization
        /// </summary>
        [TestMethod]
        public void PatternGame_Settings()
        {
            var game = new PatternGame();
            // Ensures that ParseSettings is correct
            foreach (var settings in game.PresetsSettings)
            {
                Assert.AreEqual(settings.Id, game.ParseSettings(settings.Id).Id);
            }
        }

        /// <summary>
        /// Test pattern presets settings
        /// </summary>
        [TestMethod]
        public void PatternGame_PresetsSettings()
        {
            var game = new PatternGame();
            Assert.AreEqual("10x10", game.PresetsSettings.ElementAt(0).Id);
            Assert.AreEqual("15x15", game.PresetsSettings.ElementAt(1).Id);
            Assert.AreEqual("20x20", game.PresetsSettings.ElementAt(2).Id);
        }

        /// <summary>
        /// Check generation using a specific seed (results collected from reference c implementation)
        /// </summary>
        [TestMethod]
        public void PatternGame_GenerateNewGameDescription()
        {
            var game = new PatternGame();
            string aux;
            string result;

            result = game.GenerateNewGameDescription(PatternSettings.Parse("10x10"), OriginalRandom.FromTextSeed("700216523594170"), out aux, 0);
            Assert.AreEqual("2/2.1/1.3/3/2.1/1.5.1/3.3/3.4/3.4/3.5/2.1.2/3.2/5/3/4/2.1/1.3/2.4/3.4/8", result);

            result = game.GenerateNewGameDescription(PatternSettings.Parse("15x15"), OriginalRandom.FromTextSeed("640257404256696"), out aux, 0);
            Assert.AreEqual("2.7/1.10/5.1.1/5.4/5.3.3/9.4/12/1.10/2.1/1.1.1/1.1.1/2.3/2.3/3.3/3.2/2.2.1.6/1.2.4/3.2/7/10.1/4.4.3/8.6/3.5.3/2.4/2.2/2.2.1/1.1.3/7/4/5", result);

            result = game.GenerateNewGameDescription(PatternSettings.Parse("20x20"), OriginalRandom.FromTextSeed("459464529829905"), out aux, 0);
            Assert.AreEqual("2.5.1.1/2.3.4.1/4.3.2/9.3/5.5.3.1/5.3.1.5/3.1.1.1.3/4.2/3.1.3/4.1.4.1/3.4.1/3.7/1.2.3.2/1.1.4.2/6.2.1/2.6.1/3.6.2/10.2.2/1.8.1.1/1.4.3.4/3.3.3.1.5/3.3.3.3/1.3.5.4/4.1.3.3/4.1.3/1.1.7/1.1.5.5/1.1.1.1.7/8.8/5.13/5.9/5.3/2.2.1/1.3.2/1.3/4.2/4.3.1/2.5.3.1/3.1.1/1.3.1.3", result);

        }
    }
}
