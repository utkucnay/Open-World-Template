using Glai.Mathematics;
using NUnit.Framework;

namespace Glai.Mathematics.Tests.EditMode
{
    public class MathematicsSmokeTests
    {
        [SetUp]
        public void SetUp()
        {
            DisableLogAndWarning();
        }

        [TearDown]
        public void TearDown()
        {
            ResetLoggerChannels();
        }

        private static void DisableLogAndWarning()
        {
            Glai.Core.Logger.EnableLog = false;
            Glai.Core.Logger.EnableWarning = false;
        }

        private static void ResetLoggerChannels()
        {
            Glai.Core.Logger.ResetChannels();
        }

        [Test]
        public void ByteHelpers_ConvertUsingBinaryUnits()
        {
            int kb = Math.KB(1);
            int mb = Math.MB(1);
            long gb = Math.GB(1);
            int b = Math.B(7);

            Assert.AreEqual(1024, kb);
            Assert.AreEqual(1024 * 1024, mb);
            Assert.AreEqual(1024L * 1024L * 1024L, gb);
            Assert.AreEqual(7, b);
        }

        [Test]
        public void ByteHelpers_ZeroInput_ReturnsZero()
        {
            int kb = Math.KB(0);
            int mb = Math.MB(0);
            long gb = Math.GB(0);

            Assert.AreEqual(0, kb);
            Assert.AreEqual(0, mb);
            Assert.AreEqual(0L, gb);
        }
    }
}
