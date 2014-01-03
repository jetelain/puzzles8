using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using PuzzleCollection.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Test.Util
{
    [TestClass]
    public class OriginalRandomTest
    {
        /// <summary>
        /// Check that generated numbers are identical to reference c implementation for the same seed.
        /// </summary>
        [TestMethod]
        public void OriginalRandom_Next()
        {
            var random = OriginalRandom.FromTextSeed("12300");
            Assert.AreEqual(2064, random.Next(10000));
            Assert.AreEqual(1680, random.Next(10000));
            Assert.AreEqual(2916, random.Next(10000));
            Assert.AreEqual(6889, random.Next(10000));
            Assert.AreEqual(6850, random.Next(10000));
            Assert.AreEqual(1440, random.Next(10000));
            Assert.AreEqual(8846019, random.Next(10000000));
            Assert.AreEqual(5789344, random.Next(10000000));
            Assert.AreEqual(3881974, random.Next(10000000));

            random = OriginalRandom.FromTextSeed("507896411361192");
            Assert.AreEqual(8177, random.Next(10000));
            Assert.AreEqual(8750, random.Next(10000));
            Assert.AreEqual(9217, random.Next(10000));
            Assert.AreEqual(6001, random.Next(10000));
            Assert.AreEqual(7782, random.Next(10000));
            Assert.AreEqual(3458, random.Next(10000));
            Assert.AreEqual(2206941, random.Next(10000000));
            Assert.AreEqual(6960008, random.Next(10000000));
            Assert.AreEqual(8294780, random.Next(10000000));
        }
    }
}
