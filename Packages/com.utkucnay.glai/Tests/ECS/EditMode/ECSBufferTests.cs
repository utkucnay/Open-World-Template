using System;
using Glai.ECS;
using Glai.ECS.Core;
using NUnit.Framework;
using Unity.Burst;

namespace Glai.ECS.Tests.EditMode
{
    // -------------------------------------------------------------------------
    // Buffer component types
    // -------------------------------------------------------------------------

    [FixedBuffer(capacity: 8)]
    internal struct Item : IBufferComponent
    {
        public int Id;
        public int Count;
    }

    [FixedBuffer(capacity: 4)]
    internal struct Skill : IBufferComponent
    {
        public int SkillId;
    }

    // Used only for the missing-attribute error test.
    internal struct NoAttributeBuffer : IBufferComponent
    {
        public int Value;
    }

    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    internal struct BufferScalarQuerySystem
    {
        public void Execute(BufferRW<Item> items)
        {
            items.TryAdd(new Item { Id = 7, Count = 1 });
        }
    }

    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    internal struct BufferReadOnlyScalarQuerySystem
    {
        public void Execute(BufferR<Item> items, RefRW<Position> position)
        {
            position.Value.Value = items.Length == 0 ? -1 : items[0].Id;
        }
    }

    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    internal struct BufferSseEntityIdQuerySystem
    {
        public void ExecuteSSE(EntityId4 entityIds, BufferRW4<Item> items)
        {
            for (int i = 0; i < EntityId4.Length; i++)
                items[i].TryAdd(new Item { Id = entityIds[i], Count = i });
        }
    }

    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    internal struct BufferReadOnlySseQuerySystem
    {
        public void ExecuteSSE(BufferR4<Item> items, RefRW4<Position> positions)
        {
            for (int i = 0; i < BufferR4<Item>.Length; i++)
                positions[i].Value = items[i].Length == 0 ? -1 : items[i][0].Id;
        }
    }

    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    internal struct BufferAvxEntityIdQuerySystem
    {
        public void ExecuteAVX(EntityId8 entityIds, BufferRW8<Item> items)
        {
            for (int i = 0; i < EntityId8.Length; i++)
                items[i].TryAdd(new Item { Id = entityIds[i], Count = i });
        }
    }

    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    internal struct BufferReadOnlyAvxQuerySystem
    {
        public void ExecuteAVX(BufferR8<Item> items, RefRW8<Position> positions)
        {
            for (int i = 0; i < BufferR8<Item>.Length; i++)
                positions[i].Value = items[i].Length == 0 ? -1 : items[i][0].Id;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    public class ECSBufferTests
    {
        [SetUp]
        public void SetUp()
        {
            Glai.Core.Logger.EnableLog     = false;
            Glai.Core.Logger.EnableWarning = false;
        }

        [TearDown]
        public void TearDown()
        {
            Glai.Core.Logger.ResetChannels();
        }

        // -- BufferTypeInfo ---------------------------------------------------

        [Test]
        public void BufferTypeInfo_CapacityMatchesAttribute()
        {
            Assert.AreEqual(8, BufferTypeInfo<Item>.Capacity);
            Assert.AreEqual(4, BufferTypeInfo<Skill>.Capacity);
        }

        [Test]
        public unsafe void BufferTypeInfo_SlotSize_IsHeaderPlusCapacityTimesElementSize()
        {
            // Item: sizeof(Item) = 8 (two ints), HeaderSize = 4, SlotSize = 4 + 8*8 = 68
            int expectedItemSlot = BufferTypeInfo<Item>.HeaderSize + 8 * sizeof(Item);
            Assert.AreEqual(expectedItemSlot, BufferTypeInfo<Item>.SlotSize);

            // Skill: sizeof(Skill) = 4, HeaderSize = 4, SlotSize = 4 + 4*4 = 20
            int expectedSkillSlot = BufferTypeInfo<Skill>.HeaderSize + 4 * sizeof(Skill);
            Assert.AreEqual(expectedSkillSlot, BufferTypeInfo<Skill>.SlotSize);
        }

        [Test]
        public void BufferTypeInfo_MissingAttribute_ThrowsTypeInitializationException()
        {
            Assert.Throws<TypeInitializationException>(() =>
            {
                var _ = BufferTypeInfo<NoAttributeBuffer>.Capacity;
            });
        }

        // -- CreateArchetype --------------------------------------------------

        [Test]
        public void EntityManager_CreateArchetype_BufferOnly_Succeeds()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);

            Assert.IsTrue(manager.IsValid(entity));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_CreateArchetype_ComponentAndBuffer_Succeeds()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>(), ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);

            Assert.IsTrue(manager.IsValid(entity));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_CreateArchetype_TwoBuffers_Succeeds()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>(), ArchetypeType.Buffer<Skill>() });
            var entity = manager.CreateEntity(arch);

            Assert.IsTrue(manager.IsValid(entity));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_CreateArchetype_WithDuplicateBufferType_Throws()
        {
            var manager = new EntityManager();
            manager.Initialize();

            Assert.Throws<InvalidOperationException>(() => manager.CreateArchetype(stackalloc ArchetypeType[]
            {
                ArchetypeType.Buffer<Item>(),
                ArchetypeType.Buffer<Item>()
            }));

            manager.Dispose();
        }

        // -- GetBuffer zero-init ----------------------------------------------

        [Test]
        public void EntityManager_GetBuffer_NewEntity_LengthIsZero()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);

            var buf = manager.GetBufferRW<Item>(entity);

            Assert.AreEqual(0, buf.Length);
            Assert.AreEqual(8, buf.Capacity);
            manager.Dispose();
        }

        // -- Add / TryAdd -----------------------------------------------------

        [Test]
        public void EntityManager_GetBuffer_Add_IncreasesLengthAndPersists()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);

            manager.GetBufferRW<Item>(entity).Add(new Item { Id = 7, Count = 3 });
            manager.GetBufferRW<Item>(entity).Add(new Item { Id = 12, Count = 1 });

            var buf = manager.GetBufferRW<Item>(entity);
            Assert.AreEqual(2, buf.Length);
            Assert.AreEqual(7,  buf[0].Id);
            Assert.AreEqual(3,  buf[0].Count);
            Assert.AreEqual(12, buf[1].Id);
            Assert.AreEqual(1,  buf[1].Count);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_GetBuffer_Add_WhenFull_Throws()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);

            var buf = manager.GetBufferRW<Item>(entity);
            for (int i = 0; i < buf.Capacity; i++)
                buf.Add(new Item { Id = i });

            Assert.Throws<InvalidOperationException>(() =>
                manager.GetBufferRW<Item>(entity).Add(new Item { Id = 99 }));

            manager.Dispose();
        }

        [Test]
        public void EntityManager_GetBuffer_TryAdd_WhenFull_ReturnsFalse()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);

            var buf = manager.GetBufferRW<Item>(entity);
            for (int i = 0; i < buf.Capacity; i++)
                buf.TryAdd(new Item { Id = i });

            bool result = manager.GetBufferRW<Item>(entity).TryAdd(new Item { Id = 99 });
            Assert.IsFalse(result);
            manager.Dispose();
        }

        // -- RemoveAt ---------------------------------------------------------

        [Test]
        public void EntityManager_GetBuffer_RemoveAt_SwapsLastAndDecrementsLength()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);

            manager.GetBufferRW<Item>(entity).Add(new Item { Id = 1 });
            manager.GetBufferRW<Item>(entity).Add(new Item { Id = 2 });
            manager.GetBufferRW<Item>(entity).Add(new Item { Id = 3 });

            manager.GetBufferRW<Item>(entity).RemoveAt(0); // removes Id=1, last (Id=3) swaps in

            var buf = manager.GetBufferRW<Item>(entity);
            Assert.AreEqual(2,  buf.Length);
            Assert.AreEqual(3,  buf[0].Id); // Id=3 swapped to index 0
            Assert.AreEqual(2,  buf[1].Id);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_GetBuffer_RemoveAt_OutOfRange_Throws()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);
            manager.GetBufferRW<Item>(entity).Add(new Item { Id = 1 });

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                manager.GetBufferRW<Item>(entity).RemoveAt(5));

            manager.Dispose();
        }

        // -- Clear ------------------------------------------------------------

        [Test]
        public void EntityManager_GetBuffer_Clear_ResetsLengthToZero()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);

            var buf = manager.GetBufferRW<Item>(entity);
            buf.Add(new Item { Id = 1 });
            buf.Add(new Item { Id = 2 });
            manager.GetBufferRW<Item>(entity).Clear();

            Assert.AreEqual(0, manager.GetBufferRW<Item>(entity).Length);
            manager.Dispose();
        }

        // -- Indexer bounds ---------------------------------------------------

        [Test]
        public void EntityManager_GetBuffer_Indexer_OutOfRange_Throws()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var buf = manager.GetBufferRW<Item>(entity);
                var _ = buf[0];
            });

            manager.Dispose();
        }

        // -- Multi-entity isolation -------------------------------------------

        [Test]
        public void EntityManager_GetBuffer_MultipleEntities_DoNotShareStorage()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var e1 = manager.CreateEntity(arch);
            var e2 = manager.CreateEntity(arch);

            manager.GetBufferRW<Item>(e1).Add(new Item { Id = 10 });
            manager.GetBufferRW<Item>(e2).Add(new Item { Id = 20 });
            manager.GetBufferRW<Item>(e2).Add(new Item { Id = 21 });

            Assert.AreEqual(1,  manager.GetBufferRW<Item>(e1).Length);
            Assert.AreEqual(10, manager.GetBufferRW<Item>(e1)[0].Id);
            Assert.AreEqual(2,  manager.GetBufferRW<Item>(e2).Length);
            Assert.AreEqual(20, manager.GetBufferRW<Item>(e2)[0].Id);
            manager.Dispose();
        }

        // -- Query jobs --------------------------------------------------------

        [Test]
        public void EntityManager_Run_WithBufferParameter_AppendsToBuffers()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var first = manager.CreateEntity(arch);
            var second = manager.CreateEntity(arch);

            var query = manager.Query().WithAllBuffer<Item>();
            var system = new BufferScalarQuerySystem();
            var handle = manager.Run(query, ref system);
            handle.Complete();

            Assert.AreEqual(1, manager.GetBufferRW<Item>(first).Length);
            Assert.AreEqual(7, manager.GetBufferRW<Item>(first)[0].Id);
            Assert.AreEqual(1, manager.GetBufferRW<Item>(second).Length);
            Assert.AreEqual(7, manager.GetBufferRW<Item>(second)[0].Id);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Run_WithReadOnlyBufferParameter_ReadsBuffers()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>(), ArchetypeType.Component<Position>() });
            var first = manager.CreateEntity(arch);
            var second = manager.CreateEntity(arch);
            manager.GetBufferRW<Item>(first).Add(new Item { Id = 10 });
            manager.GetBufferRW<Item>(second).Add(new Item { Id = 20 });

            var query = manager.Query().WithAll<Position>().WithAllBuffer<Item>();
            var system = new BufferReadOnlyScalarQuerySystem();
            var handle = manager.Run(query, ref system);
            handle.Complete();

            Assert.AreEqual(10, manager.GetComponent<Position>(first).Value);
            Assert.AreEqual(20, manager.GetComponent<Position>(second).Value);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Run_WithSseBufferAndEntityIds_UsesPaddedTailBatch()
        {
            if (!Unity.Burst.Intrinsics.X86.Sse4_2.IsSse42Supported)
                Assert.Ignore("SSE4.2 is not supported on this CPU.");

            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entities = new Entity[7];
            for (int i = 0; i < entities.Length; i++)
                entities[i] = manager.CreateEntity(arch);

            var query = manager.Query().WithAllBuffer<Item>();
            var system = new BufferSseEntityIdQuerySystem();
            var handle = manager.Run(query, ref system);
            handle.Complete();

            for (int i = 0; i < entities.Length; i++)
            {
                var buffer = manager.GetBufferRW<Item>(entities[i]);
                Assert.AreEqual(1, buffer.Length);
                Assert.AreEqual(entities[i].Id, buffer[0].Id);
            }

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Run_WithSseReadOnlyBuffer_ReadsPaddedTailBatch()
        {
            if (!Unity.Burst.Intrinsics.X86.Sse4_2.IsSse42Supported)
                Assert.Ignore("SSE4.2 is not supported on this CPU.");

            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>(), ArchetypeType.Component<Position>() });
            var entities = new Entity[7];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = manager.CreateEntity(arch);
                manager.GetBufferRW<Item>(entities[i]).Add(new Item { Id = 100 + i });
            }

            var query = manager.Query().WithAll<Position>().WithAllBuffer<Item>();
            var system = new BufferReadOnlySseQuerySystem();
            var handle = manager.Run(query, ref system);
            handle.Complete();

            for (int i = 0; i < entities.Length; i++)
                Assert.AreEqual(100 + i, manager.GetComponent<Position>(entities[i]).Value);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Run_WithAvxBufferAndEntityIds_UsesPaddedTailBatch()
        {
            if (!Unity.Burst.Intrinsics.X86.Avx2.IsAvx2Supported)
                Assert.Ignore("AVX2 is not supported on this CPU.");

            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entities = new Entity[11];
            for (int i = 0; i < entities.Length; i++)
                entities[i] = manager.CreateEntity(arch);

            var query = manager.Query().WithAllBuffer<Item>();
            var system = new BufferAvxEntityIdQuerySystem();
            var handle = manager.Run(query, ref system);
            handle.Complete();

            for (int i = 0; i < entities.Length; i++)
            {
                var buffer = manager.GetBufferRW<Item>(entities[i]);
                Assert.AreEqual(1, buffer.Length);
                Assert.AreEqual(entities[i].Id, buffer[0].Id);
            }

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Run_WithAvxReadOnlyBuffer_ReadsPaddedTailBatch()
        {
            if (!Unity.Burst.Intrinsics.X86.Avx2.IsAvx2Supported)
                Assert.Ignore("AVX2 is not supported on this CPU.");

            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>(), ArchetypeType.Component<Position>() });
            var entities = new Entity[11];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = manager.CreateEntity(arch);
                manager.GetBufferRW<Item>(entities[i]).Add(new Item { Id = 200 + i });
            }

            var query = manager.Query().WithAll<Position>().WithAllBuffer<Item>();
            var system = new BufferReadOnlyAvxQuerySystem();
            var handle = manager.Run(query, ref system);
            handle.Complete();

            for (int i = 0; i < entities.Length; i++)
                Assert.AreEqual(200 + i, manager.GetComponent<Position>(entities[i]).Value);

            handle.Dispose();
            manager.Dispose();
        }

        // -- DestroyEntity ----------------------------------------------------

        [Test]
        public void EntityManager_DestroyEntity_WithBuffer_InvalidatesHandle()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);
            manager.GetBufferRW<Item>(entity).Add(new Item { Id = 5 });

            manager.DestroyEntity(entity);

            Assert.IsFalse(manager.IsValid(entity));
            Assert.Throws<InvalidOperationException>(() =>
                manager.GetBufferRW<Item>(entity));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_ReusedEntitySlot_BufferLengthIsResetToZero()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);
            manager.GetBufferRW<Item>(entity).Add(new Item { Id = 5 });

            manager.DestroyEntity(entity);

            var recycled = manager.CreateEntity(arch);

            Assert.AreEqual(0, manager.GetBufferRW<Item>(recycled).Length);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_ReusedSwapBackSlot_BufferLengthIsResetToZero()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var first = manager.CreateEntity(arch);
            var second = manager.CreateEntity(arch);

            manager.GetBufferRW<Item>(first).Add(new Item { Id = 1 });
            manager.GetBufferRW<Item>(second).Add(new Item { Id = 2 });
            manager.GetBufferRW<Item>(second).Add(new Item { Id = 3 });

            manager.DestroyEntity(first);

            var recycled = manager.CreateEntity(arch);

            Assert.AreEqual(0, manager.GetBufferRW<Item>(recycled).Length);
            Assert.AreEqual(2, manager.GetBufferRW<Item>(second).Length);
            manager.Dispose();
        }

    }
}
