using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using PuzzleCollection.Games;
using PuzzleCollection.Games.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Test.Games
{
    /// <summary>
    /// Unit tests of <see cref="MapGame"/>.
    /// </summary>
    [TestClass]
    public class MapGameTest
    {
        /// <summary>
        /// Test MapSettings serialization
        /// </summary>
        [TestMethod]
        public void MapGame_Settings()
        {
            var game = new MapGame();
            // Ensures that ParseSettings is correct
            foreach (var settings in game.PresetsSettings)
            {
                Assert.AreEqual(settings.Id, game.ParseSettings(settings.Id).Id);
            }
        }

        /// <summary>
        /// Test LightupSettings presets settings
        /// </summary>
        [TestMethod]
        public void MapGame_PresetsSettings()
        {
            var game = new MapGame();
            Assert.AreEqual("20x15n30de", game.PresetsSettings.ElementAt(0).Id);
            Assert.AreEqual("20x15n30dn", game.PresetsSettings.ElementAt(1).Id);
            Assert.AreEqual("20x15n30dh", game.PresetsSettings.ElementAt(2).Id);
            Assert.AreEqual("20x15n30du", game.PresetsSettings.ElementAt(3).Id);
            Assert.AreEqual("30x25n75dn", game.PresetsSettings.ElementAt(4).Id);
            Assert.AreEqual("30x25n75dh", game.PresetsSettings.ElementAt(5).Id);
        }

        /// <summary>
        /// Check generation using a specific seed (results collected from reference c implementation)
        /// </summary>
        [TestMethod]
        public void MapGame_GenerateNewGameDescription()
        {
            var game = new MapGame();
            string aux;
            string result;

            result = game.GenerateNewGameDescription(MapSettings.Parse("20x15n30de"), OriginalRandom.FromTextSeed("750963468150504"), out aux, 0);
            Assert.AreEqual("hbhaabchdbjeaaaadaaaicfaaccbbabcfaabbaabgabbbbaabdecbaadaceaeacaeegbadddaabbbaiaabdcbbfabcaazbbfcfajgjcbddbfalabaaadacbeaeecaaceefabccbbbfahabcabbbcabaccbcbbdaabcbdabadbbbaaaacabbbdacadjaibc,0a1a10a12e311c21a0b00a2", result);

            result = game.GenerateNewGameDescription(MapSettings.Parse("20x15n30dn"), OriginalRandom.FromTextSeed("533451592341698"), out aux, 0);
            Assert.AreEqual("dalbgbbbebcbaaffhcbabafcabacaakbfbhabcacbdebbbfdadacacbadaebbcbbaabafcbcaaacoadbbbgakaaabcabgbbbkcebabbabagbbaafdbbagadcaaaabaabcbbeaebaaadchahaiabaaaahfaicaeaabadacbaabbjbcaaagaaccdaceafanaaad,2a31a2b0k33a2b03a2", result);

            result = game.GenerateNewGameDescription(MapSettings.Parse("20x15n30dh"), OriginalRandom.FromTextSeed("639474500391106"), out aux, 0);
            Assert.AreEqual("abccbakedahbcaaedaeicbdbaababaaajbbbdccababakbabaceaabaaecaacaabaaabbaceabcbacbahabbdadbfaaadebadabadaeaeaabjaaaaaaabaaaaaaadaaadadbibababbbaacbhaaaaccadabahbedbbeadaccdjaaabcahababaaajheecaaafbaaacdacbcabcdeabfabakah,302a1b3d2b2c1e23b0", result);

            result = game.GenerateNewGameDescription(MapSettings.Parse("20x15n30du"), OriginalRandom.FromTextSeed("709797814473903"), out aux, 0);
            Assert.AreEqual("jdbaaalabbaaeafaacbaeagbbbabbecbaadaaacccbbaaaaahfbdddbcaadagaaacabhddcaaapacaabbeababbaaabbaacaabdabbibafgehadbbabcaddaobhakaaaaclabajacababbaaabcbacccbadababacbeabaabacaaababbbehfabbcababaabbacbaabaaaaababacacebaaaeaj,b3a3a3e11e3d02a1b", result);

            result = game.GenerateNewGameDescription(MapSettings.Parse("30x25n75dn"), OriginalRandom.FromTextSeed("530367413210280"), out aux, 0);
            Assert.AreEqual("caaaeamaladaadaaeaaafccadabaiaacbdaadabaeadeabaaadaaecbagaabaabbefgadcacagkaaadacaeababbaaaaeacbaafbabbabccaaababcachbbjdaaakbcbebbabahaaaaddaeacdeabbbapabbbdcbaacfbdbcaafaaaaaecaacaccbaeaecmbkabagaebaccaacahbadaaacacgjhacaccabadaaeafaacckaabdabbkccaabbaaajabblbbaeccadbbdaaebdcccbacadaaabbbcbbaabaeaabbegagagbcdbaaagaaeaaaagcaeaacacaccaagbdbbcbadbbbadiebbaabbfacbbaabcdacbacbaaaabcdacgaacbcaaaacdaeadaaabbabdcbafaaebdcaadsbfbfbablaabdaaabdbacbedfcjbaabadabajaacbabaabbciadcaabacbcacafajcbceaacbdbccadaaacbdbccoacaeaeamadagcf,31g2a01a21c12c20e3a0h333b2f0a1a1f0b23a23a2", result);

            result = game.GenerateNewGameDescription(MapSettings.Parse("30x25n75dh"), OriginalRandom.FromTextSeed("746431354954289"), out aux, 0);
            Assert.AreEqual("dafacababdibfabaiacaacaaabcbabaabbaaacaaccgabaadabaadbcchaidcbcafabbbbdaadeacbabbbaeaadcababdbjcccbagaaacafbbabacbabbaedbacaeadfbcfhcbaaeacbbbbbaaadfaacbaccbacbfbaacbdccebcaacabbbbcabaaagbdbbabadbaaaahbabbaababaaaadgcaaagcaaahaadbabhcbaaagabbhcaabagbbacafabbabaccaacacaabcbcbbcabafbcadecbadabaacaeaaaiaeakabaaaabebaaaadaabaadcdaaabacbaadacadacbfbbbhcbbaaaeaaaaaakabadababdbcdaaaababaagabbcdeabaeaabbacabaaadaafdabcacaabaecaaaababceadbabaadababaabcapaaahhdbdabdhadabbaaaadafhbdacadcbacfabcbaabdaadeaaadbcaaafcbacafbbacacbdacaababdabadbaabcaadbcaedbaabbabaababbadaabbccaeacccbabeccafbbaeahaaaabeadabebacaeba,02a2032f22b2c1c3a23e0a1f20b21g32b22b1d1a2310", result);
        }
    }
}
