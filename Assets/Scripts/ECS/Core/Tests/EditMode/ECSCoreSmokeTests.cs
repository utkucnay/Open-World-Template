using System;
using Glai.ECS.Core;
using NUnit.Framework;
using Unity.Collections;

namespace Glai.ECS.Core.Tests.EditMode
{
    public class ECSCoreSmokeTests
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

        [Test]
        public void ECSMemoryState_ConstructsPersistHandleForSameMemoryState()
        {
            var memoryState = new ECSMemoryState();

            Assert.AreEqual(memoryState.Id, memoryState.persistHandle.Id);
            memoryState.Dispose();
        }

        [Test]
        public void ECSMemoryState_PopAndPushStackHandle_RoundTripsHandle()
        {
            var memoryState = new ECSMemoryState();

            var handle = memoryState.PopStackHandle();

            Assert.AreEqual(memoryState.Id, handle.Id);

            memoryState.PushStackHandle(handle);
            memoryState.Dispose();
        }

        [Test]
        public void Chunk_CreateAndRemoveIndex_ReusesFreedIndex()
        {
            var memoryState = new ECSMemoryState();
            var chunk = new Chunk(new ChunkData
            {
                name = new FixedString128Bytes("ChunkTest"),
                capacityBytes = 64,
                maxComponentSize = 8,
                componentCount = 2,
            }, memoryState.persistHandle, memoryState);

            int first = chunk.CreateComponentIndex();
            int second = chunk.CreateComponentIndex();
            chunk.RemoveComponentIndex(first);
            int reused = chunk.CreateComponentIndex();
            int third = chunk.CreateComponentIndex();
            int fourth = chunk.CreateComponentIndex();

            Assert.AreEqual(0, first);
            Assert.AreEqual(1, second);
            Assert.AreEqual(first, reused);
            Assert.AreEqual(2, third);
            Assert.AreEqual(3, fourth);
            Assert.IsTrue(chunk.IsFull());

            chunk.Dispose(memoryState);
            memoryState.Dispose();
        }

        [Test]
        public void Chunk_NewChunk_DataRegionIsZeroInitialized()
        {
            var memoryState = new ECSMemoryState();
            var chunk = new Chunk(new ChunkData
            {
                name = new FixedString128Bytes("ChunkInitTest"),
                capacityBytes = 64,
                maxComponentSize = 4,
                componentCount = 2,
            }, memoryState.persistHandle, memoryState);

            Assert.AreEqual(0, chunk.GetComponent<int>(0, 0));
            Assert.AreEqual(0, chunk.GetComponent<int>(1, 0));

            chunk.Dispose(memoryState);
            memoryState.Dispose();
        }
    }
}
