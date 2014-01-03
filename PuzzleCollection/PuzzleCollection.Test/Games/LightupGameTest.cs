using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using PuzzleCollection.Games;
using PuzzleCollection.Games.Lightup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Test.Games
{
    /// <summary>
    /// Unit tests of <see cref="LightupGame"/>.
    /// </summary>
    [TestClass]
    public class LightupGameTest
    {
        /// <summary>
        /// Test LightupSettings serialization
        /// </summary>
        [TestMethod]
        public void LightupGame_Settings()
        {
            var game = new LightupGame();
            // Ensures that ParseSettings is correct
            foreach (var settings in game.PresetsSettings)
            {
                Assert.AreEqual(settings.Id, game.ParseSettings(settings.Id).Id);
            }
            // Ensures that settings produced by the first version are correctly understood
            Assert.AreEqual("7x7b20s4d0", game.ParseSettings("7x7b20s5d0").Id);
            Assert.AreEqual("7x7b20s4d1", game.ParseSettings("7x7b20s5d1").Id);
            Assert.AreEqual("7x7b20s4d2", game.ParseSettings("7x7b20s5d2").Id);
        }

        /// <summary>
        /// Test LightupSettings presets settings
        /// </summary>
        [TestMethod]
        public void LightupGame_PresetsSettings()
        {
            var game = new LightupGame();
            Assert.AreEqual("7x7b20s4d0", game.PresetsSettings.ElementAt(0).Id);
            Assert.AreEqual("7x7b20s4d1", game.PresetsSettings.ElementAt(1).Id);
            Assert.AreEqual("7x7b20s4d2", game.PresetsSettings.ElementAt(2).Id);
            Assert.AreEqual("10x10b20s2d0", game.PresetsSettings.ElementAt(3).Id);
            Assert.AreEqual("10x10b20s2d1", game.PresetsSettings.ElementAt(4).Id);
            Assert.AreEqual("10x10b20s2d2", game.PresetsSettings.ElementAt(5).Id);
            Assert.AreEqual("14x14b20s2d0", game.PresetsSettings.ElementAt(6).Id);
            Assert.AreEqual("14x14b20s2d1", game.PresetsSettings.ElementAt(7).Id);
            Assert.AreEqual("14x14b20s2d2", game.PresetsSettings.ElementAt(8).Id);
        }

        /// <summary>
        /// Check generation using a specific seed (results collected from reference c implementation)
        /// </summary>
        [TestMethod]
        public void LightupGame_GenerateNewGameDescription()
        {
            var game = new LightupGame();
            string aux;
            string result;

            result = game.GenerateNewGameDescription(LightupSettings.Parse("7x7b20s4d0"), OriginalRandom.FromTextSeed("204786253910817"), out aux, 0);
            Assert.AreEqual("c3aBa1m1e0mBa0aBc", result);

            result = game.GenerateNewGameDescription(LightupSettings.Parse("7x7b20s4d1"), OriginalRandom.FromTextSeed("492052127714434"), out aux, 0);
            Assert.AreEqual("e1a0b1k1c1k0b3a1e", result);

            result = game.GenerateNewGameDescription(LightupSettings.Parse("7x7b20s4d2"), OriginalRandom.FromTextSeed("476652939696640"), out aux, 0);
            Assert.AreEqual("1a2cBbBi10g1BiBb1c1a1", result);

            result = game.GenerateNewGameDescription(LightupSettings.Parse("10x10b20s2d0"), OriginalRandom.FromTextSeed("755341380087152"), out aux, 0);
            Assert.AreEqual("aBd0f1g0fB1h0cBd12b21dBcBhBBfBg1f1d2a", result);

            result = game.GenerateNewGameDescription(LightupSettings.Parse("10x10b20s2d1"), OriginalRandom.FromTextSeed("355272709153989"), out aux, 0);
            Assert.AreEqual("a1cBb0e1c0f1k3f2aBa2b3a0a2fBkBf0c2eBb2c0a", result);

            result = game.GenerateNewGameDescription(LightupSettings.Parse("10x10b20s2d2"), OriginalRandom.FromTextSeed("994177797307151"), out aux, 0);
            Assert.AreEqual("i011a2b1bBc0d0l1c2a10b1Ba1cBl1d1c0b1b0aB10i", result);

            result = game.GenerateNewGameDescription(LightupSettings.Parse("12x12b20s2d0"), OriginalRandom.FromTextSeed("223198613952275"), out aux, 0);
            Assert.AreEqual("cBe1b1a0a21cBlBBbBcBc2f3a2a3h2a3bB1bBaBhBaBaBf0cBc0b01l2c1BaBa0b0e2c", result);

            result = game.GenerateNewGameDescription(LightupSettings.Parse("12x12b20s2d1"), OriginalRandom.FromTextSeed("642002680977718"), out aux, 0);
            Assert.AreEqual("BBc0c1d1i2d10e1b0g1eBB1gBaBbB0dBBb1aBgBB1eBg3bBe2Bd1iBd0c1cBB", result);

            result = game.GenerateNewGameDescription(LightupSettings.Parse("14x14b20s2d2"), OriginalRandom.FromTextSeed("646903133235996"), out aux, 0);
            Assert.AreEqual("Bs3bBBBc0cBaBa1bBa1Bb2cBpBbBe0BB2b3eBbBc1d2cBb2e1bBBB1eBb1pBc1b0BaBb0a1a3c1c1BBbBsB", result);
        }
    }
}
