using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using PuzzleCollection.Games;
using PuzzleCollection.Games.Untangle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Test.Games
{
    /// <summary>
    /// Unit tests of <see cref="UntangleGame"/>.
    /// </summary>
    [TestClass]
    public class UntangleGameTest
    {
        /// <summary>
        /// Test UntangleSettings serialization
        /// </summary>
        [TestMethod]
        public void UntangleGame_Settings()
        {
            var game = new UntangleGame();
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
        public void UntangleGame_GenerateNewGameDescription()
        {
            var game = new UntangleGame();
            string aux;
            string result;

            result = game.GenerateNewGameDescription(UntangleSettings.Parse("6"), OriginalRandom.FromTextSeed("654665470699953"), out aux, 0);
            Assert.AreEqual("0-1,0-2,0-3,0-4,1-2,1-5,2-4,2-5,3-4,3-5,4-5", result);

            result = game.GenerateNewGameDescription(UntangleSettings.Parse("10"), OriginalRandom.FromTextSeed("532198935691559"), out aux, 0);
            Assert.AreEqual("0-3,0-5,0-7,0-9,1-4,1-6,1-8,2-4,2-6,2-7,2-9,3-5,3-8,3-9,4-6,5-7,5-8,6-8,7-9", result);

            result = game.GenerateNewGameDescription(UntangleSettings.Parse("15"), OriginalRandom.FromTextSeed("597929675619262"), out aux, 0);
            Assert.AreEqual("0-6,0-11,0-13,0-14,1-3,1-4,1-7,1-8,2-6,2-9,2-12,2-13,3-8,3-10,4-7,5-7,5-11,5-12,5-14,6-11,6-13,7-8,8-10,9-10,9-12,9-13,10-12,11-14", result);

            result = game.GenerateNewGameDescription(UntangleSettings.Parse("20"), OriginalRandom.FromTextSeed("879023985584531"), out aux, 0);
            Assert.AreEqual("0-8,0-11,0-15,0-19,1-5,1-7,1-9,1-18,2-6,2-7,2-10,2-17,3-9,3-10,3-18,4-8,4-14,4-15,5-16,5-18,6-11,6-14,6-17,7-11,7-19,8-11,8-14,9-10,9-18,12-13,12-15,12-16,12-19,13-16,14-17,16-19", result);

            result = game.GenerateNewGameDescription(UntangleSettings.Parse("25"), OriginalRandom.FromTextSeed("673044682295223"), out aux, 0);
            Assert.AreEqual("0-5,0-8,0-15,0-23,1-2,1-6,2-6,2-16,3-8,3-12,3-13,3-21,4-9,4-19,4-22,5-6,5-7,5-18,6-18,7-14,7-15,8-12,8-23,9-11,9-19,10-11,10-16,10-18,10-22,11-16,11-19,12-15,12-23,13-20,13-21,13-24,14-17,14-20,14-24,15-20,16-18,17-21,17-22,17-24,19-22,20-24", result);
        }
    }
}
