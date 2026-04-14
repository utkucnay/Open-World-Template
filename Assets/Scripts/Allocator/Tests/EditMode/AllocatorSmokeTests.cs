using System;
using System.Text.RegularExpressions;
using Glai.Allocator;
using Glai.Allocator.Core;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Glai.Allocator.Tests.EditMode
{
    public class AllocatorSmokeTests
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

        private sealed class TestMemoryState : MemoryState
        {
            public MemoryStateHandle Add(IAllocatorBase allocator)
            {
                return AddAllocator(allocator);
            }
        }

        [Test]
        public void Persist_WhenSetThenGet_ReturnsStoredValue()
        {
            var persist = new Persist(new PersistData
            {
                name = new FixedString128Bytes("PersistTest"),
                capacityBytes = 128,
                maxHandles = 8,
            });

            var handle = persist.Allocate<int>();
            persist.Set(handle, 42);

            Assert.AreEqual(42, persist.Get<int>(handle));
            persist.Dispose();
        }

        [Test]
        public void Stack_WhenDeallocatedTwice_ThrowsInvalidOperationException()
        {
            var stack = new Stack(new StackData
            {
                name = new FixedString128Bytes("StackTest"),
                capacityBytes = 128,
                maxHandles = 8,
            });

            var handle = stack.Allocate<int>();
            stack.Deallocate(handle);

            Assert.Throws<InvalidOperationException>(() => stack.Deallocate(handle));
            stack.Dispose();
        }

        [Test]
        public void MemoryState_GetWithForeignHandle_ReturnsNullAllocator()
        {
            var memoryState = new TestMemoryState();
            var handle = memoryState.Add(new Persist(new PersistData
            {
                name = new FixedString128Bytes("StatePersist"),
                capacityBytes = 128,
                maxHandles = 8,
            }));

            var foreign = new MemoryStateHandle(Guid.NewGuid(), handle.ArrayIndex);
            LogAssert.Expect(LogType.Error, new Regex("MemoryStateHandle with Id .* does not belong to this MemoryState"));
            var allocator = memoryState.Get<IAllocator>(foreign);

            Assert.IsNull(allocator);
            memoryState.Dispose();
        }
    }
}
