using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using PuzzleCollection.Games;
using PuzzleCollection.Games.Bridges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Test.Games
{
    /// <summary>
    /// Unit tests of <see cref="BridgesGame"/>.
    /// </summary>
    [TestClass]
    public class BridgesGameTest
    {
        /// <summary>
        /// Test BridgesSettings serialization
        /// </summary>
        [TestMethod]
        public void BridgesGame_Settings()
        {
            var game = new BridgesGame();
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
        public void BridgesGame_GenerateNewGameDescription()
        {
            var game = new BridgesGame();
            string aux;
            string result;

            // Easy
            result = game.GenerateNewGameDescription(BridgesSettings.Parse("7x7i30e10m2d0"), OriginalRandom.FromTextSeed("419574874642567"), out aux, 0);
            Assert.AreEqual("b3a3a42h5a2a3g5a4b3h3b3a4a", result);

            result = game.GenerateNewGameDescription(BridgesSettings.Parse("10x10i30e10m2d0"), OriginalRandom.FromTextSeed("761743026123477"), out aux, 0);
            Assert.AreEqual("2a2a6a5b1j3c7a6b2l2c5a3u4a7a6a4a1k3a2a2d3", result);

            result = game.GenerateNewGameDescription(BridgesSettings.Parse("15x15i30e10m2d0"), OriginalRandom.FromTextSeed("222023191273591"), out aux, 0);
            Assert.AreEqual("a2g5d3j4a3p1zp3a3y6c4y3b1b2b1b2a7d4j1b2a3h6b1a2o3a4f5c3a", result);

            // Medium / Normal
            result = game.GenerateNewGameDescription(BridgesSettings.Parse("7x7i30e10m2d1"), OriginalRandom.FromTextSeed("694920918046735"), out aux, 0);
            Assert.AreEqual("3a2c2a2a5a2k2d4c2a2g2d2", result);

            result = game.GenerateNewGameDescription(BridgesSettings.Parse("10x10i30e10m2d1"), OriginalRandom.FromTextSeed("738570285034166"), out aux, 0);
            Assert.AreEqual("1h2j3d3b2k5h4a1c3b1a2l3a6c6k3a4c3a4", result);

            result = game.GenerateNewGameDescription(BridgesSettings.Parse("15x15i30e10m2d1"), OriginalRandom.FromTextSeed("655474761047265"), out aux, 0);
            Assert.AreEqual("a2b2e4b4l2b2e1b3a3b4b6b6f5zzc1e3w4b2b2w4e3a2i1e3a1i2a4c3b3c3b3", result);
            
            // Hard
            result = game.GenerateNewGameDescription(BridgesSettings.Parse("7x7i30e10m2d2"), OriginalRandom.FromTextSeed("489867197411041"), out aux, 0);
            Assert.AreEqual("2b2b2a1c1i3a6a5n13b2a2a", result);

            result = game.GenerateNewGameDescription(BridgesSettings.Parse("10x10i30e10m2d2"), OriginalRandom.FromTextSeed("230148393268095"), out aux, 0);
            Assert.AreEqual("3g2c2a1a3b2j5a7c4f1d2c3a4d4e3b2m1b2c22c4a3a2a", result);

            result = game.GenerateNewGameDescription(BridgesSettings.Parse("15x15i30e10m2d2"), OriginalRandom.FromTextSeed("353822714775262"), out aux, 0);
            Assert.AreEqual("b3a4a4a3a3c21d1e1f1c1a2e4a4b5b3g2d1j3h3b5u2a2c3a5b1m4a7a6c2b1h3a4a5c2e2o2c4a4a3e2a1g3b4d5e3", result);
        }
    }
}
