using System;
using Glai.Allocator;
using Glai.Allocator.Core;
using Glai.Collection;
using NUnit.Framework;
using Unity.Collections;

namespace Glai.Collection.Tests.EditMode
{
    public class CollectionSmokeTests
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
            public MemoryStateHandle Add(IAllocatorBase allocator)
            {
                return AddAllocator(allocator);
            }
        }

        [Test]
        public void FixedArray_SetSwapFind_WorksAsExpected()
        {
            var memoryState = new TestMemoryState();
            var arenaHandle = memoryState.Add(new Arena(new ArenaData
            {
                name = new FixedString128Bytes("ArrayArena"),
                capacityBytes = 512,
                maxHandles = 16,
            }));

            var array = new FixedArray<int>(3, arenaHandle, memoryState);
            array[0] = 10;
            array[1] = 20;
            array[2] = 30;
            array.Swap(0, 2);

            Assert.AreEqual(30, array[0]);
            Assert.AreEqual(0, array.FindIndex(30));

            array.Dispose(memoryState);
            memoryState.Dispose();
        }

        [Test]
        public void FixedList_AddRemoveAt_UsesSwapBackRemoval()
        {
            var memoryState = new TestMemoryState();
            var arenaHandle = memoryState.Add(new Arena(new ArenaData
            {
                name = new FixedString128Bytes("ListArena"),
                capacityBytes = 512,
                maxHandles = 16,
            }));

            var list = new FixedList<int>(4, arenaHandle, memoryState);
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.RemoveAt(1);

            Assert.AreEqual(2, list.Count);
            Assert.IsTrue(list.Contains(1));
            Assert.IsTrue(list.Contains(3));

            list.Dispose(memoryState);
            memoryState.Dispose();
        }

        [Test]
        public void FixedStack_PushPopPeek_FollowsLifo()
        {
            var memoryState = new TestMemoryState();
            var arenaHandle = memoryState.Add(new Arena(new ArenaData
            {
                name = new FixedString128Bytes("StackArena"),
                capacityBytes = 512,
                maxHandles = 16,
            }));

            var stack = new FixedStack<int>(4, arenaHandle, memoryState);
            stack.Push(10);
            stack.Push(20);

            Assert.AreEqual(20, stack.Peek());
            Assert.AreEqual(20, stack.Pop());
            Assert.AreEqual(10, stack.Pop());

            stack.Dispose(memoryState);
            memoryState.Dispose();
        }

        [Test]
        public void FixedQueue_EnqueueDequeue_WrapsAroundCorrectly()
        {
            var memoryState = new TestMemoryState();
            var arenaHandle = memoryState.Add(new Arena(new ArenaData
            {
                name = new FixedString128Bytes("QueueArena"),
                capacityBytes = 512,
                maxHandles = 16,
            }));

            var queue = new FixedQueue<int>(3, arenaHandle, memoryState);
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);
            Assert.AreEqual(1, queue.Dequeue());

            queue.Enqueue(4);

            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(3, queue.Dequeue());
            Assert.AreEqual(4, queue.Dequeue());

            queue.Dispose(memoryState);
            memoryState.Dispose();
        }

        [Test]
        public void FixedDictionary_AddUpdateRemove_TracksKeysCorrectly()
        {
            var memoryState = new TestMemoryState();
            var arenaHandle = memoryState.Add(new Arena(new ArenaData
            {
                name = new FixedString128Bytes("DictionaryArena"),
                capacityBytes = 2048,
                maxHandles = 32,
            }));

            var dict = new FixedDictionary<int, int>(8, arenaHandle, memoryState);
            dict.Add(7, 10);
            dict.Add(7, 20);

            Assert.IsTrue(dict.ContainsKey(7));
            Assert.AreEqual(20, dict[7]);

            dict.Remove(7);
            Assert.IsFalse(dict.ContainsKey(7));

            dict.Dispose(memoryState);
            memoryState.Dispose();
        }
    }
}
