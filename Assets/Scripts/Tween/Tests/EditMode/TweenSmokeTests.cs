using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Glai.Tween.Tests.EditMode
{
    public class TweenSmokeTests
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

        private static Type TweenManagerType => Assembly.Load("Glai.Tween").GetType("Glai.Tween.TweenManager", true);

        private static object CreateAndInitializeManager()
        {
            object manager = Activator.CreateInstance(TweenManagerType, true);
            TweenManagerType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(manager, null);
            return manager;
        }

        private static void DisposeManager(object manager)
        {
            TweenManagerType.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(manager, null);
        }

        [Test]
        public void AddPositionTween_CreatesActiveHandle_AndCanBeToggled()
        {
            object manager = CreateAndInitializeManager();
            var go = new GameObject("TweenTarget");
            try
            {
                MethodInfo addPositionTween = TweenManagerType.GetMethod("AddPositionTween", BindingFlags.Instance | BindingFlags.Public);
                MethodInfo isTweenActive = TweenManagerType.GetMethod("IsTweenActive", BindingFlags.Instance | BindingFlags.Public);
                MethodInfo setTweenActive = TweenManagerType.GetMethod("SetTweenActive", BindingFlags.Instance | BindingFlags.Public);

                Type float3Type = Type.GetType("Unity.Mathematics.float3, Unity.Mathematics", true);
                object from = Activator.CreateInstance(float3Type, new object[] { 0f, 0f, 0f });
                object to = Activator.CreateInstance(float3Type, new object[] { 10f, 0f, 0f });

                object handle = addPositionTween.Invoke(manager, new object[]
                {
                    from,
                    to,
                    1f,
                    go.transform,
                });

                Assert.IsTrue((bool)isTweenActive.Invoke(manager, new[] { handle }));

                setTweenActive.Invoke(manager, new object[] { handle, false });
                Assert.IsFalse((bool)isTweenActive.Invoke(manager, new[] { handle }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                DisposeManager(manager);
            }
        }

        [Test]
        public void SetTweenSpeed_WithRotationHandle_ThrowsNotImplementedException()
        {
            object manager = CreateAndInitializeManager();
            try
            {
                Type tweenTypeEnum = TweenManagerType.Assembly.GetType("Glai.Tween.TweenType", true);
                object rotationValue = Enum.Parse(tweenTypeEnum, "Rotation");

                Type tweenHandleType = TweenManagerType.Assembly.GetType("Glai.Tween.TweenHandle", true);
                ConstructorInfo handleCtor = tweenHandleType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Guid), typeof(int), typeof(int), typeof(int), tweenTypeEnum, typeof(bool), typeof(bool) },
                    null);

                object rotationHandle = handleCtor.Invoke(new object[]
                {
                    Guid.NewGuid(),
                    0,
                    0,
                    0,
                    rotationValue,
                    true,
                    true,
                });

                MethodInfo setTweenSpeed = TweenManagerType.GetMethod("SetTweenSpeed", BindingFlags.Instance | BindingFlags.Public);

                var ex = Assert.Throws<TargetInvocationException>(() =>
                    setTweenSpeed.Invoke(manager, new object[] { rotationHandle, 2f }));

                Assert.IsInstanceOf<NotImplementedException>(ex.InnerException);
            }
            finally
            {
                DisposeManager(manager);
            }
        }
    }
}
