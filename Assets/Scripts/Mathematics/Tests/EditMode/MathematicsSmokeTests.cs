using System;
using System.Reflection;
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
            var loggerType = Type.GetType("Glai.Core.Logger, Glai.Core");
            if (loggerType == null) return;

            loggerType.GetProperty("EnableLog")?.SetValue(null, false);
            loggerType.GetProperty("EnableWarning")?.SetValue(null, false);
        }

        private static void ResetLoggerChannels()
        {
            var loggerType = Type.GetType("Glai.Core.Logger, Glai.Core");
            if (loggerType == null) return;

            loggerType.GetMethod("ResetChannels")?.Invoke(null, null);
        }

        private static MethodInfo ResolveMathMethod(string name)
        {
            var type = Assembly.Load("Glai.Mathematics").GetType("Glai.Mathematics.Math", true);
            return type.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
        }

        [Test]
        public void ByteHelpers_ConvertUsingBinaryUnits()
        {
            int kb = (int)ResolveMathMethod("KB").Invoke(null, new object[] { 1 });
            int mb = (int)ResolveMathMethod("MB").Invoke(null, new object[] { 1 });
            long gb = (long)ResolveMathMethod("GB").Invoke(null, new object[] { 1 });
            int b = (int)ResolveMathMethod("B").Invoke(null, new object[] { 7 });

            Assert.AreEqual(1024, kb);
            Assert.AreEqual(1024 * 1024, mb);
            Assert.AreEqual(1024L * 1024L * 1024L, gb);
            Assert.AreEqual(7, b);
        }

        [Test]
        public void ByteHelpers_ZeroInput_ReturnsZero()
        {
            int kb = (int)ResolveMathMethod("KB").Invoke(null, new object[] { 0 });
            int mb = (int)ResolveMathMethod("MB").Invoke(null, new object[] { 0 });
            long gb = (long)ResolveMathMethod("GB").Invoke(null, new object[] { 0 });

            Assert.AreEqual(0, kb);
            Assert.AreEqual(0, mb);
            Assert.AreEqual(0L, gb);
        }
    }
}
