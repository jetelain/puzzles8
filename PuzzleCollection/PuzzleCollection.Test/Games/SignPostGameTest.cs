using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using PuzzleCollection.Games;
using PuzzleCollection.Games.SignPost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Test.Games
{
    /// <summary>
    /// Unit tests of <see cref="SignPostGame"/>.
    /// </summary>
    [TestClass]
    public class SignPostGameTest
    {
        /// <summary>
        /// Test SignPostSettings serialization
        /// </summary>
        [TestMethod]
        public void SignPostGame_Settings()
        {
            var game = new SignPostGame();
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
        public void SignPostGame_GenerateNewGameDescription()
        {
            var game = new SignPostGame();
            string aux;
            string result;
            result = game.GenerateNewGameDescription(SignPostSettings.Parse("4x4c"), OriginalRandom.FromTextSeed("238811033586560"), out aux, 0);
            Assert.AreEqual("1cce3gecffchhgbcc16a", result);

            result = game.GenerateNewGameDescription(SignPostSettings.Parse("4x4"), OriginalRandom.FromTextSeed("728688935142131"), out aux, 0);
            Assert.AreEqual("e4egge16abebcag10cb1a11g", result);

            result = game.GenerateNewGameDescription(SignPostSettings.Parse("5x5c"), OriginalRandom.FromTextSeed("896332746558986"), out aux, 0);
            Assert.AreEqual("1ecdefddeggc18bgfgcebahca15ag25a", result);

            result = game.GenerateNewGameDescription(SignPostSettings.Parse("5x5"), OriginalRandom.FromTextSeed("409911568079594"), out aux, 0);
            Assert.AreEqual("ceegf1echdedbc24eebafagbgh25ag", result);

            result = game.GenerateNewGameDescription(SignPostSettings.Parse("6x6c"), OriginalRandom.FromTextSeed("122569235105665"), out aux, 0);
            Assert.AreEqual("1ce33dgefcceefeeaa9dbacag16hghbbhfeaacgb24g36a", result);

            result = game.GenerateNewGameDescription(SignPostSettings.Parse("7x7c"), OriginalRandom.FromTextSeed("635471713097299"), out aux, 0);
            Assert.AreEqual("1cc2cd4dfg31cche32fgeedceg6hg14ebebbgfc25eafgf19geahgagga41cbacb49a", result);
        }
    }
}
