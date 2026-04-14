using System;
using System.Reflection;
using Glai.Module;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Glai.Tween.Tests.EditMode
{
    public class TweenSmokeTests
    {
        [SetUp]
        public void SetUp()
        {
            DisableLogAndWarning();
            if (ModuleManager.Instance == null)
            {
                var go = new GameObject("ModuleManager_Test");
                var moduleManager = go.AddComponent<ModuleManager>();
                MethodInfo awakeMethod = typeof(ModuleManager).GetMethod("AwakeTest", BindingFlags.Instance | BindingFlags.NonPublic);
                awakeMethod.Invoke(moduleManager, null);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (ModuleManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(ModuleManager.Instance.gameObject);
            }

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
        public void AddPositionTween_CreatesHandle_AndAcceptsSpeedUpdate()
        {
            var go = new GameObject("TweenTarget");
            try
            {
                TweenHandle handle = go.transform.DoMove(new float3(0f, 0f, 0f), new float3(10f, 0f, 0f), 1f);

                Assert.AreNotEqual(Guid.Empty, handle.Id);
                Assert.DoesNotThrow(() => Tween.SetTweenSpeed(handle, 2f));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception was thrown during test execution: {ex.StackTrace}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SetTweenSpeed_WithRotationHandle_ThrowsNotImplementedException()
        {
            var tweenAssembly = typeof(TweenHandle).Assembly;
            Type tweenTypeEnum = tweenAssembly.GetType("Glai.Tween.TweenType", true);
            object rotationValue = Enum.Parse(tweenTypeEnum, "Rotation");

            ConstructorInfo handleCtor = typeof(TweenHandle).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(Guid), typeof(int), typeof(int), typeof(int), tweenTypeEnum, typeof(bool), typeof(bool) },
                null);
            Assert.IsNotNull(handleCtor);

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

            Assert.Throws<NotImplementedException>(() => Tween.SetTweenSpeed((TweenHandle)rotationHandle, 2f));
        }
    }
}
