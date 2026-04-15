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
            Glai.Core.Logger.EnableLog = false;
            Glai.Core.Logger.EnableWarning = false;
        }

        private static void ResetLoggerChannels()
        {
            Glai.Core.Logger.ResetChannels();
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

            var handle = memoryState.PopChunkStackHandle();

            Assert.AreEqual(memoryState.Id, handle.Id);

            memoryState.PushChunkStackHandle(handle);
            memoryState.Dispose();
        }

        [Test]
        public void Chunk_CreateSlot_AssignsSequentialIndices()
        {
            var memoryState = new ECSMemoryState();
            Span<int> sizes = stackalloc int[] { 8, 8 };
            var chunk = new Chunk(new ChunkData
            {
                name = new FixedString128Bytes("ChunkTest"),
                capacityBytes = 64,
                componentCount = 2,
                componentSizes = sizes,
            }, memoryState.persistHandle, memoryState);

            // capacity = 64 / (8+8) = 4 entities
            int first = chunk.CreateSlot(0);
            int second = chunk.CreateSlot(1);
            int third = chunk.CreateSlot(2);
            int fourth = chunk.CreateSlot(3);

            Assert.AreEqual(0, first);
            Assert.AreEqual(1, second);
            Assert.AreEqual(2, third);
            Assert.AreEqual(3, fourth);
            Assert.AreEqual(4, chunk.EntityCount);
            Assert.IsTrue(chunk.IsFull());

            chunk.Dispose(memoryState);
            memoryState.Dispose();
        }

        [Test]
        public void Chunk_RemoveSlot_SwapsLastEntityIntoDead()
        {
            var memoryState = new ECSMemoryState();
            Span<int> sizes = stackalloc int[] { 8, 8 };
            var chunk = new Chunk(new ChunkData
            {
                name = new FixedString128Bytes("ChunkSwapTest"),
                capacityBytes = 64,
                componentCount = 2,
                componentSizes = sizes,
            }, memoryState.persistHandle, memoryState);

            // capacity = 64 / (8+8) = 4 entities
            chunk.CreateSlot(10);
            chunk.CreateSlot(20);
            chunk.CreateSlot(30);

            Assert.AreEqual(3, chunk.EntityCount);

            // Remove slot 0 (entity 10): last entity (30 at slot 2) swaps into slot 0
            int swapped = chunk.RemoveSlot(0);
            Assert.AreEqual(30, swapped);
            Assert.AreEqual(2, chunk.EntityCount);

            // Remove last slot (index 1 is now last): no swap needed
            int swappedNone = chunk.RemoveSlot(1);
            Assert.AreEqual(-1, swappedNone);
            Assert.AreEqual(1, chunk.EntityCount);

            chunk.Dispose(memoryState);
            memoryState.Dispose();
        }

        [Test]
        public void Chunk_RemoveSlot_SwapsComponentData()
        {
            var memoryState = new ECSMemoryState();
            Span<int> sizes = stackalloc int[] { 4, 4 };
            var chunk = new Chunk(new ChunkData
            {
                name = new FixedString128Bytes("ChunkDataSwapTest"),
                capacityBytes = 64,
                componentCount = 2,
                componentSizes = sizes,
            }, memoryState.persistHandle, memoryState);

            // capacity = 64 / (4+4) = 8 entities
            chunk.CreateSlot(0);
            chunk.CreateSlot(1);
            chunk.CreateSlot(2);

            // Write distinct values into component region 0
            chunk.GetComponent<int>(0, 0) = 100;
            chunk.GetComponent<int>(0, 1) = 200;
            chunk.GetComponent<int>(0, 2) = 300;

            // Remove slot 0: slot 2 data (300) should swap into slot 0
            chunk.RemoveSlot(0);

            Assert.AreEqual(300, chunk.GetComponent<int>(0, 0));
            Assert.AreEqual(200, chunk.GetComponent<int>(0, 1));
            Assert.AreEqual(2, chunk.EntityCount);

            chunk.Dispose(memoryState);
            memoryState.Dispose();
        }

        [Test]
        public void Chunk_NewChunk_DataRegionIsZeroInitialized()
        {
            var memoryState = new ECSMemoryState();
            Span<int> sizes = stackalloc int[] { 4, 4 };
            var chunk = new Chunk(new ChunkData
            {
                name = new FixedString128Bytes("ChunkInitTest"),
                capacityBytes = 64,
                componentCount = 2,
                componentSizes = sizes,
            }, memoryState.persistHandle, memoryState);

            Assert.AreEqual(0, chunk.GetComponent<int>(0, 0));
            Assert.AreEqual(0, chunk.GetComponent<int>(1, 0));

            chunk.Dispose(memoryState);
            memoryState.Dispose();
        }
    }
}
