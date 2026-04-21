using System;
using Glai.Allocator;
using Glai.Tween.Core;
using NUnit.Framework;

namespace Glai.Tween.Core.Tests.EditMode
{
    public class TweenCoreSmokeTests
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
        public void Tween_GetValue_WithZeroDuration_ReturnsToValue()
        {
            var tween = new Tween<float>(0f, 5f, 0f, default);
            Func<float, float, float, float> lerp = (a, b, t) => a + ((b - a) * t);

            float value = tween.GetValue(10f, lerp);

            Assert.AreEqual(5f, value);
        }

        [Test]
        public void Tween_IncreaseTime_ClampsToDuration()
        {
            var tween = new Tween<float>(0f, 1f, 1f, default);

            tween.IncreaseTime(0.6f);
            tween.IncreaseTime(0.6f);

            Assert.AreEqual(1f, tween.CurrentTime);
            Assert.IsTrue(tween.IsComplete());
        }

        [Test]
        public void TweenState_PopAndPushArenaHandle_RestoresHandlePool()
        {
            var state = new TweenState(default);

            MemoryStateHandle handle = state.PopArenaHandle();
            state.PushArenaHandle(handle);
            MemoryStateHandle second = state.PopArenaHandle();

            Assert.AreEqual(handle.Id, second.Id);

            state.PushArenaHandle(second);
            state.Dispose();
        }
    }
}
