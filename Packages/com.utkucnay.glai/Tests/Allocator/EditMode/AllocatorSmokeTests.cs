using System;
using System.Text.RegularExpressions;
using Glai.Collections;
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
    }
}
