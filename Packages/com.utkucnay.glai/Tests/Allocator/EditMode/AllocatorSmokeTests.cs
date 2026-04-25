using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Glai.Collections;
using Glai.Core;
using NUnit.Framework;
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
            Glai.Core.Logger.EnableLog = false;
            Glai.Core.Logger.EnableWarning = false;
        }

        private static void ResetLoggerChannels()
        {
            Glai.Core.Logger.ResetChannels();
        }

        private sealed class TestMemoryState : MemoryState
        {
            public MemoryStateHandle Add(IAllocator allocator)
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
        public void Arena_AllocateAndAllocateArray_HonorRequestedAlignment()
        {
            var arena = new Arena(new ArenaData
            {
                name = new FixedString128Bytes("ArenaAlignmentTest"),
                capacityBytes = 256,
                maxHandles = 8,
            });

            AssertAllocatorAlignment(arena, 32);
            arena.Dispose();
        }

        [Test]
        public void Stack_AllocateAndAllocateArray_HonorRequestedAlignment()
        {
            var stack = new Stack(new StackData
            {
                name = new FixedString128Bytes("StackAlignmentTest"),
                capacityBytes = 256,
                maxHandles = 8,
            });

            AssertAllocatorAlignment(stack, 32);
            stack.Dispose();
        }

        [Test]
        public void Persist_AllocateAndAllocateArray_HonorRequestedAlignment()
        {
            var persist = new Persist(new PersistData
            {
                name = new FixedString128Bytes("PersistAlignmentTest"),
                capacityBytes = 256,
                maxHandles = 8,
            });

            AssertAllocatorAlignment(persist, 32);
            persist.Dispose();
        }

        [Test]
        public void Allocators_WhenAlignmentIsNotPowerOfTwo_Throw()
        {
            var arena = new Arena(new ArenaData
            {
                name = new FixedString128Bytes("ArenaInvalidAlignment"),
                capacityBytes = 128,
                maxHandles = 4,
            });

            var stack = new Stack(new StackData
            {
                name = new FixedString128Bytes("StackInvalidAlignment"),
                capacityBytes = 128,
                maxHandles = 4,
            });

            var persist = new Persist(new PersistData
            {
                name = new FixedString128Bytes("PersistInvalidAlignment"),
                capacityBytes = 128,
                maxHandles = 4,
            });

            Assert.Throws<ArgumentException>(() => arena.Allocate<byte>(3));
            Assert.Throws<ArgumentException>(() => stack.Allocate<byte>(3));
            Assert.Throws<ArgumentException>(() => persist.Allocate<byte>(3));

            arena.Dispose();
            stack.Dispose();
            persist.Dispose();
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

        [Test]
        public void Arena_PeaksPersistAcrossClearUntilReset()
        {
            var arena = new Arena(new ArenaData
            {
                name = new FixedString128Bytes("ArenaPeakTest"),
                capacityBytes = 128,
                maxHandles = 8,
            });

            arena.Allocate<int>();
            int peakAfterAllocate = arena.PeakCount;
            int peakHandlesAfterAllocate = arena.PeakHandleCount;

            arena.Clear();

            Assert.AreEqual(0, arena.Count);
            Assert.AreEqual(0, arena.HandleCount);
            Assert.AreEqual(peakAfterAllocate, arena.PeakCount);
            Assert.AreEqual(peakHandlesAfterAllocate, arena.PeakHandleCount);

            arena.ResetPeaks();

            Assert.AreEqual(0, arena.PeakCount);
            Assert.AreEqual(0, arena.PeakHandleCount);
            arena.Dispose();
        }

        [Test]
        public unsafe void MemoryPool_WhenBlockFreed_ReusesSameAddressForMatchingAllocation()
        {
            var memoryPool = new MemoryPool(512);
            Assert.AreEqual(512, memoryPool.Capacity);
            Assert.AreEqual(0, memoryPool.UsedBytes);

            byte* first = memoryPool.Allocate(32, 16);
            Assert.AreEqual(0, ((long)first) % 16);
            Assert.Greater(memoryPool.UsedBytes, 0);

            byte* second = memoryPool.Allocate(64, 32);
            Assert.AreEqual(0, ((long)second) % 32);
            int usedAfterSecondAllocation = memoryPool.UsedBytes;

            memoryPool.Deallocate(first);
            Assert.Less(memoryPool.UsedBytes, usedAfterSecondAllocation);

            byte* reused = memoryPool.Allocate(24, 16);

            Assert.AreEqual((IntPtr)first, (IntPtr)reused);
            Assert.Greater(memoryPool.FreeBytes, 0);
            memoryPool.Dispose();
        }

        [Test]
        public void Persist_Dispose_ReturnsPooledMemoryForReuse()
        {
            var first = new Persist(new PersistData
            {
                name = new FixedString128Bytes("PersistReuseA"),
                capacityBytes = 128,
                maxHandles = 8,
            });

            IntPtr firstDataPtr = GetPrivateIntPtr(first, "dataPtr");
            first.Dispose();

            var second = new Persist(new PersistData
            {
                name = new FixedString128Bytes("PersistReuseB"),
                capacityBytes = 128,
                maxHandles = 8,
            });

            IntPtr secondDataPtr = GetPrivateIntPtr(second, "dataPtr");

            Assert.AreEqual(firstDataPtr, secondDataPtr);
            second.Dispose();
        }

        [Test]
        public void Stack_Deallocate_RewindsAllocationCursor()
        {
            var stack = new Stack(new StackData
            {
                name = new FixedString128Bytes("StackDeallocateTest"),
                capacityBytes = 256,
                maxHandles = 8,
            });

            var first = stack.Allocate<int>();
            var second = stack.Allocate<int>();
            var third = stack.Allocate<int>();
            int countBeforeDeallocate = stack.Count;

            stack.Deallocate(second);

            Assert.AreEqual(second.Index, stack.HandleCount);
            Assert.Less(stack.Count, countBeforeDeallocate);

            var replacement = stack.Allocate<int>();

            Assert.AreEqual(second.Index, replacement.Index);
            Assert.AreEqual(second.ArrayIndex, replacement.ArrayIndex);
            Assert.AreNotEqual(third.Generation, replacement.Generation);
            stack.Dispose();
        }

        [Test]
        public void Stack_DeallocateAlignedAllocation_RewindsToPreAlignmentCursor()
        {
            var stack = new Stack(new StackData
            {
                name = new FixedString128Bytes("StackAlignedRewindTest"),
                capacityBytes = 256,
                maxHandles = 8,
            });

            stack.Allocate<byte>();
            int countAfterFirstAllocation = stack.Count;
            var aligned = stack.Allocate<int>(32);

            stack.Deallocate(aligned);

            Assert.AreEqual(countAfterFirstAllocation, stack.Count);
            stack.Dispose();
        }

        [Test]
        public void Arena_Deallocate_DoesNotReclaimAllocationSpace()
        {
            var arena = new Arena(new ArenaData
            {
                name = new FixedString128Bytes("ArenaDeallocateTest"),
                capacityBytes = 256,
                maxHandles = 8,
            });

            var first = arena.Allocate<int>();
            var second = arena.Allocate<int>();
            int countBeforeDeallocate = arena.Count;

            arena.Deallocate(first);

            Assert.AreEqual(countBeforeDeallocate, arena.Count);

            var third = arena.Allocate<int>();

            Assert.Greater(third.ArrayIndex, second.ArrayIndex);
            arena.Dispose();
        }

        [Test]
        public void Persist_Deallocate_LogsWarningAndDoesNotChangeUsage()
        {
            Glai.Core.Logger.EnableWarning = true;

            var persist = new Persist(new PersistData
            {
                name = new FixedString128Bytes("PersistDeallocateWarning"),
                capacityBytes = 128,
                maxHandles = 8,
            });

            var handle = persist.Allocate<int>();
            int countBeforeDeallocate = persist.Count;
            int handlesBeforeDeallocate = persist.HandleCount;

            LogAssert.Expect(LogType.Warning, new Regex("Persist allocator doesn't support deallocation\\."));
            persist.Deallocate(handle);

            Assert.AreEqual(countBeforeDeallocate, persist.Count);
            Assert.AreEqual(handlesBeforeDeallocate, persist.HandleCount);
            persist.Dispose();
        }

        [Test]
        public void Allocators_WhenOutOfMemory_ThrowInvalidOperationException()
        {
            var arena = new Arena(new ArenaData
            {
                name = new FixedString128Bytes("ArenaOutOfMemory"),
                capacityBytes = sizeof(int),
                maxHandles = 4,
            });

            var stack = new Stack(new StackData
            {
                name = new FixedString128Bytes("StackOutOfMemory"),
                capacityBytes = sizeof(int),
                maxHandles = 4,
            });

            var persist = new Persist(new PersistData
            {
                name = new FixedString128Bytes("PersistOutOfMemory"),
                capacityBytes = sizeof(int),
                maxHandles = 4,
            });

            arena.Allocate<int>();
            stack.Allocate<int>();
            persist.Allocate<int>();

            Assert.Throws<InvalidOperationException>(() => arena.Allocate<byte>());
            Assert.Throws<InvalidOperationException>(() => stack.Allocate<byte>());
            Assert.Throws<InvalidOperationException>(() => persist.Allocate<byte>());

            arena.Dispose();
            stack.Dispose();
            persist.Dispose();
        }

        [Test]
        public void Allocators_WhenHandleCapacityReached_ThrowInvalidOperationException()
        {
            var arena = new Arena(new ArenaData
            {
                name = new FixedString128Bytes("ArenaHandleLimit"),
                capacityBytes = 128,
                maxHandles = 1,
            });

            var stack = new Stack(new StackData
            {
                name = new FixedString128Bytes("StackHandleLimit"),
                capacityBytes = 128,
                maxHandles = 1,
            });

            var persist = new Persist(new PersistData
            {
                name = new FixedString128Bytes("PersistHandleLimit"),
                capacityBytes = 128,
                maxHandles = 1,
            });

            arena.Allocate<int>();
            stack.Allocate<int>();
            persist.Allocate<int>();

            Assert.Throws<InvalidOperationException>(() => arena.Allocate<int>());
            Assert.Throws<InvalidOperationException>(() => stack.Allocate<int>());
            Assert.Throws<InvalidOperationException>(() => persist.Allocate<int>());

            arena.Dispose();
            stack.Dispose();
            persist.Dispose();
        }

        [Test]
        public void Allocators_SetArrayAndGetArray_RoundTripValues()
        {
            var arena = new Arena(new ArenaData
            {
                name = new FixedString128Bytes("ArenaArrayRoundTrip"),
                capacityBytes = 256,
                maxHandles = 8,
            });

            var stack = new Stack(new StackData
            {
                name = new FixedString128Bytes("StackArrayRoundTrip"),
                capacityBytes = 256,
                maxHandles = 8,
            });

            var persist = new Persist(new PersistData
            {
                name = new FixedString128Bytes("PersistArrayRoundTrip"),
                capacityBytes = 256,
                maxHandles = 8,
            });

            AssertArrayRoundTrip(arena);
            AssertArrayRoundTrip(stack);
            AssertArrayRoundTrip(persist);

            arena.Dispose();
            stack.Dispose();
            persist.Dispose();
        }

        [Test]
        public void Arena_Dispose_OnlyPublishesUnregisterOnce()
        {
            int unregisterCount = 0;

            void OnUnregister(Glai.Core.Object obj)
            {
                unregisterCount++;
            }

            EventBus.Subscribe(IAllocator.UnregisterEvent, OnUnregister);

            try
            {
                var arena = new Arena(new ArenaData
                {
                    name = new FixedString128Bytes("ArenaDisposeIdempotent"),
                    capacityBytes = 128,
                    maxHandles = 8,
                });

                arena.Dispose();
                arena.Dispose();

                Assert.AreEqual(1, unregisterCount);
            }
            finally
            {
                EventBus.Unsubscribe(IAllocator.UnregisterEvent, OnUnregister);
            }
        }

        private static IntPtr GetPrivateIntPtr(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected field '{fieldName}' on {instance.GetType().Name}.");
            return (IntPtr)field.GetValue(instance);
        }

        private static void AssertAllocatorAlignment(IAllocator allocator, int alignment)
        {
            var first = allocator.Allocate<byte>();
            var second = allocator.Allocate<int>(alignment);
            var third = allocator.AllocateArray<byte>(17, alignment);

            IntPtr dataPtr = GetPrivateIntPtr(allocator, "dataPtr");

            Assert.AreEqual(0, (dataPtr.ToInt64() + second.ArrayIndex) % alignment);
            Assert.AreEqual(0, (dataPtr.ToInt64() + third.ArrayIndex) % alignment);
            Assert.GreaterOrEqual(second.ArrayIndex, first.ArrayIndex + sizeof(byte));
            Assert.GreaterOrEqual(third.ArrayIndex, second.ArrayIndex + sizeof(int));
        }

        private static void AssertArrayRoundTrip(IAllocator allocator)
        {
            var handle = allocator.AllocateArray<int>(4);
            int[] values = { 3, 5, 8, 13 };
            allocator.SetArray(handle, values.AsSpan());

            Span<int> stored = allocator.GetArray<int>(handle);

            Assert.AreEqual(values, stored.ToArray());
        }
    }
}
