using System;
using Glai.Allocator.Core;
using NUnit.Framework;
using Unity.Collections;

namespace Glai.Allocator.Core.Tests.EditMode
{
    public class AllocatorCoreSmokeTests
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

        private sealed class FakeAllocator : IAllocatorBase
        {
            public FixedString128Bytes Name { get; } = new FixedString128Bytes("FakeAllocator");
            public int Count { get; private set; } = 8;
            public int Capacity { get; private set; } = 64;
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                Count = 0;
                IsDisposed = true;
            }
        }

        [Test]
        public void AllocatorBase_ExposesNameCountAndCapacity()
        {
            IAllocatorBase allocator = new FakeAllocator();

            Assert.AreEqual("FakeAllocator", allocator.Name.ToString());
            Assert.AreEqual(8, allocator.Count);
            Assert.AreEqual(64, allocator.Capacity);
        }

        [Test]
        public void AllocatorBase_Dispose_CanBeCalledMultipleTimes()
        {
            var allocator = new FakeAllocator();

            allocator.Dispose();
            Assert.IsTrue(allocator.IsDisposed);
            Assert.DoesNotThrow(() => allocator.Dispose());
            Assert.AreEqual(0, allocator.Count);
        }
    }
}
