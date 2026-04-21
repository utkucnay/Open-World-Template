using System;
using Glai.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using CoreLogger = Glai.Core.Logger;

namespace Glai.Core.Tests.EditMode
{
    public class CoreSmokeTests
    {
        private sealed class DummyObject : Glai.Core.Object
        {
        }

        [SetUp]
        public void SetUp()
        {
            CoreLogger.EnableLog = false;
            CoreLogger.EnableWarning = false;
        }

        [TearDown]
        public void TearDown()
        {
            CoreLogger.ResetChannels();
        }

        [Test]
        public void TypeId_ForDifferentTypes_AreUniqueAndStable()
        {
            int intIdFirst = TypeId<int>.Id;
            int intIdSecond = TypeId<int>.Id;
            int floatId = TypeId<float>.Id;

            Assert.AreEqual(intIdFirst, intIdSecond);
            Assert.AreNotEqual(intIdFirst, floatId);
        }

        [Test]
        public void Handle_IsValid_RespectsIdAndGeneration()
        {
            Guid id = Guid.NewGuid();
            var valid = new Handle(id, 1, 16, 3);
            var same = new Handle(id, 1, 16, 3);
            var wrongGeneration = new Handle(id, 1, 16, 4);

            Assert.IsTrue(valid.IsValid(same));
            Assert.IsFalse(valid.IsValid(wrongGeneration));
        }

        [Test]
        public void HandleArray_IsValid_DelegatesToUnderlyingHandle()
        {
            Guid id = Guid.NewGuid();
            var arrayHandle = new HandleArray(id, 2, 32, 5, 1);
            var valid = new Handle(id, 2, 32, 1);
            var invalid = new Handle(id, 2, 32, 2);

            Assert.IsTrue(arrayHandle.IsValid(valid));
            Assert.IsFalse(arrayHandle.IsValid(invalid));
        }

        [Test]
        public void Object_Dispose_IsIdempotent()
        {
            var obj = new DummyObject();

            obj.Dispose();
            Assert.IsTrue(obj.Disposed);
            Assert.DoesNotThrow(() => obj.Dispose());
        }

        [Test]
        public void Logger_DisabledLog_DoesNotEmitMessage()
        {
            CoreLogger.EnableLog = false;

            CoreLogger.Log("HiddenLog");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Logger_DisabledWarning_DoesNotEmitWarning()
        {
            CoreLogger.EnableWarning = false;

            CoreLogger.LogWarning("HiddenWarning");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Logger_EnabledWarning_EmitsWarning()
        {
            CoreLogger.EnableWarning = true;
            LogAssert.Expect(LogType.Warning, "VisibleWarning");

            CoreLogger.LogWarning("VisibleWarning");
        }
    }
}
