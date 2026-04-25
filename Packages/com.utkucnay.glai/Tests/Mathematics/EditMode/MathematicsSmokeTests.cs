using Glai.Mathematics;
using NUnit.Framework;
using Unity.Mathematics;

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
            int helperKb = ByteSizeHelper.KB(2);
            int helperMb = ByteSizeHelper.MB(3);
            long helperGb = ByteSizeHelper.GB(4);

            Assert.AreEqual(1024, kb);
            Assert.AreEqual(1024 * 1024, mb);
            Assert.AreEqual(1024L * 1024L * 1024L, gb);
            Assert.AreEqual(7, b);
            Assert.AreEqual(2 * 1024, helperKb);
            Assert.AreEqual(3 * 1024 * 1024, helperMb);
            Assert.AreEqual(4L * 1024L * 1024L * 1024L, helperGb);
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

        [Test]
        public void MathWrapper_ForwardsCommonUnityMathematicsCalls()
        {
            float clamped = Glai.math.clamp(12f, 0f, 10f);
            float3 interpolated = Glai.math.lerp(new float3(0f, 2f, 4f), new float3(10f, 12f, 14f), 0.5f);
            float3 normalized = Glai.math.normalizesafe(new float3(0f, 3f, 4f));
            float dot = Glai.math.dot(new float3(1f, 2f, 3f), new float3(4f, 5f, 6f));

            Assert.AreEqual(10f, clamped);
            Assert.AreEqual(new float3(5f, 7f, 9f), interpolated);
            Assert.AreEqual(0f, normalized.x, 0.0001f);
            Assert.AreEqual(0.6f, normalized.y, 0.0001f);
            Assert.AreEqual(0.8f, normalized.z, 0.0001f);
            Assert.AreEqual(32f, dot);
        }
    }
}
