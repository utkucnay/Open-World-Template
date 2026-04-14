using System;
using System.Reflection;
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

        private static Assembly TweenCoreAssembly => Assembly.Load("Glai.Tween.Core");

        private static object CreateTween(float from, float to, float duration)
        {
            Type tweenType = TweenCoreAssembly.GetType("Glai.Tween.Core.Tween`1", true).MakeGenericType(typeof(float));
            ConstructorInfo ctor = tweenType.GetConstructor(new[]
            {
                typeof(float),
                typeof(float),
                typeof(float),
                TweenCoreAssembly.GetType("Glai.Tween.Core.TweenTarget", true),
                typeof(Unity.Collections.FixedString128Bytes),
            });

            return ctor.Invoke(new object[]
            {
                from,
                to,
                duration,
                default,
                default(Unity.Collections.FixedString128Bytes),
            });
        }

        [Test]
        public void Tween_GetValue_WithZeroDuration_ReturnsToValue()
        {
            object tween = CreateTween(0f, 5f, 0f);
            Type tweenType = tween.GetType();
            MethodInfo getValue = tweenType.GetMethod("GetValue");
            Func<float, float, float, float> lerp = (a, b, t) => a + ((b - a) * t);

            float value = (float)getValue.Invoke(tween, new object[] { 10f, lerp });

            Assert.AreEqual(5f, value);
        }

        [Test]
        public void Tween_IncreaseTime_ClampsToDuration()
        {
            object tween = CreateTween(0f, 1f, 1f);
            Type tweenType = tween.GetType();
            MethodInfo increaseTime = tweenType.GetMethod("IncreaseTime");
            MethodInfo isComplete = tweenType.GetMethod("IsComplete");
            PropertyInfo currentTime = tweenType.GetProperty("CurrentTime");

            increaseTime.Invoke(tween, new object[] { 0.6f });
            increaseTime.Invoke(tween, new object[] { 0.6f });

            Assert.AreEqual(1f, (float)currentTime.GetValue(tween));
            Assert.IsTrue((bool)isComplete.Invoke(tween, null));
        }

        [Test]
        public void TweenState_PopAndPushArenaHandle_RestoresHandlePool()
        {
            Type tweenStateDataType = TweenCoreAssembly.GetType("Glai.Tween.Core.TweenStateData", true);
            Type tweenStateType = TweenCoreAssembly.GetType("Glai.Tween.Core.TweenState", true);

            object state = Activator.CreateInstance(tweenStateType, new[] { Activator.CreateInstance(tweenStateDataType) });
            MethodInfo pop = tweenStateType.GetMethod("PopArenaHandle");
            MethodInfo push = tweenStateType.GetMethod("PushArenaHandle");
            MethodInfo dispose = tweenStateType.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.Public);

            object handle = pop.Invoke(state, null);
            push.Invoke(state, new[] { handle });
            object second = pop.Invoke(state, null);

            Type memoryStateHandleType = handle.GetType();
            PropertyInfo idProperty = memoryStateHandleType.GetProperty("Id");

            Assert.AreEqual(idProperty.GetValue(handle), idProperty.GetValue(second));

            push.Invoke(state, new[] { second });
            dispose.Invoke(state, null);
        }
    }
}
