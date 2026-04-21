using System;
using Glai.ECS;
using Glai.ECS.Core;
using NUnit.Framework;

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

    // -------------------------------------------------------------------------
    // System structs for RunNonBurst (non-nested so source gen can see them)
    // -------------------------------------------------------------------------

    [QueryJob(QueryExecution.MainThread), NonBurstQuery]
    internal struct SumItemIdsSystem
    {
        public int Total;
        public void Execute(Buffer<Item> b1)
        {
            for (int i = 0; i < b1.Length; i++)
                Total += b1[i].Id;
        }
    }

    [QueryJob(QueryExecution.MainThread), NonBurstQuery]
    internal struct StoreItemCountInPositionSystem
    {
        public void Execute(Buffer<Item> b1, Ref<Position> c1)
        {
            c1.Value.Value = b1.Length;
        }
    }

    [QueryJob(QueryExecution.MainThread), NonBurstQuery]
    internal struct SumBothBuffersSystem
    {
        public int ItemTotal;
        public int SkillTotal;
        public void Execute(Buffer<Item> b1, Buffer<Skill> b2)
        {
            for (int i = 0; i < b1.Length; i++) ItemTotal  += b1[i].Id;
            for (int i = 0; i < b2.Length; i++) SkillTotal += b2[i].SkillId;
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

            var buf = manager.GetBuffer<Item>(entity);

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

            manager.GetBuffer<Item>(entity).Add(new Item { Id = 7, Count = 3 });
            manager.GetBuffer<Item>(entity).Add(new Item { Id = 12, Count = 1 });

            var buf = manager.GetBuffer<Item>(entity);
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

            var buf = manager.GetBuffer<Item>(entity);
            for (int i = 0; i < buf.Capacity; i++)
                buf.Add(new Item { Id = i });

            Assert.Throws<InvalidOperationException>(() =>
                manager.GetBuffer<Item>(entity).Add(new Item { Id = 99 }));

            manager.Dispose();
        }

        [Test]
        public void EntityManager_GetBuffer_TryAdd_WhenFull_ReturnsFalse()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);

            var buf = manager.GetBuffer<Item>(entity);
            for (int i = 0; i < buf.Capacity; i++)
                buf.TryAdd(new Item { Id = i });

            bool result = manager.GetBuffer<Item>(entity).TryAdd(new Item { Id = 99 });
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

            manager.GetBuffer<Item>(entity).Add(new Item { Id = 1 });
            manager.GetBuffer<Item>(entity).Add(new Item { Id = 2 });
            manager.GetBuffer<Item>(entity).Add(new Item { Id = 3 });

            manager.GetBuffer<Item>(entity).RemoveAt(0); // removes Id=1, last (Id=3) swaps in

            var buf = manager.GetBuffer<Item>(entity);
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
            manager.GetBuffer<Item>(entity).Add(new Item { Id = 1 });

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                manager.GetBuffer<Item>(entity).RemoveAt(5));

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

            var buf = manager.GetBuffer<Item>(entity);
            buf.Add(new Item { Id = 1 });
            buf.Add(new Item { Id = 2 });
            manager.GetBuffer<Item>(entity).Clear();

            Assert.AreEqual(0, manager.GetBuffer<Item>(entity).Length);
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
                var buf = manager.GetBuffer<Item>(entity);
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

            manager.GetBuffer<Item>(e1).Add(new Item { Id = 10 });
            manager.GetBuffer<Item>(e2).Add(new Item { Id = 20 });
            manager.GetBuffer<Item>(e2).Add(new Item { Id = 21 });

            Assert.AreEqual(1,  manager.GetBuffer<Item>(e1).Length);
            Assert.AreEqual(10, manager.GetBuffer<Item>(e1)[0].Id);
            Assert.AreEqual(2,  manager.GetBuffer<Item>(e2).Length);
            Assert.AreEqual(20, manager.GetBuffer<Item>(e2)[0].Id);
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
            manager.GetBuffer<Item>(entity).Add(new Item { Id = 5 });

            manager.DestroyEntity(entity);

            Assert.IsFalse(manager.IsValid(entity));
            Assert.Throws<InvalidOperationException>(() =>
                manager.GetBuffer<Item>(entity));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_ReusedEntitySlot_BufferLengthIsResetToZero()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var entity = manager.CreateEntity(arch);
            manager.GetBuffer<Item>(entity).Add(new Item { Id = 5 });

            manager.DestroyEntity(entity);

            var recycled = manager.CreateEntity(arch);

            Assert.AreEqual(0, manager.GetBuffer<Item>(recycled).Length);
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

            manager.GetBuffer<Item>(first).Add(new Item { Id = 1 });
            manager.GetBuffer<Item>(second).Add(new Item { Id = 2 });
            manager.GetBuffer<Item>(second).Add(new Item { Id = 3 });

            manager.DestroyEntity(first);

            var recycled = manager.CreateEntity(arch);

            Assert.AreEqual(0, manager.GetBuffer<Item>(recycled).Length);
            Assert.AreEqual(2, manager.GetBuffer<Item>(second).Length);
            manager.Dispose();
        }

        // -- RunNonBurst buffer jobs ------------------------------------------

        [Test]
        public void EntityManager_RunNonBurst_Buffer_SingleBuffer_SumsItems()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });
            var e1 = manager.CreateEntity(arch);
            var e2 = manager.CreateEntity(arch);

            manager.GetBuffer<Item>(e1).Add(new Item { Id = 3 });
            manager.GetBuffer<Item>(e1).Add(new Item { Id = 4 });
            manager.GetBuffer<Item>(e2).Add(new Item { Id = 10 });

            var query = manager.Query();
            var system = new SumItemIdsSystem();
            var handle = manager.RunNonBurst(query, ref system);
            handle.Complete();

            Assert.AreEqual(17, system.Total);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_RunNonBurst_BufferAndComponent_OnlyMatchingArchetype()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archBoth = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>(), ArchetypeType.Buffer<Item>() });
            int archBufOnly = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>() });

            var matching = manager.CreateEntity(archBoth);
            var nonMatching = manager.CreateEntity(archBufOnly);

            manager.GetBuffer<Item>(matching).Add(new Item { Id = 1 });
            manager.GetBuffer<Item>(matching).Add(new Item { Id = 1 });
            manager.GetBuffer<Item>(nonMatching).Add(new Item { Id = 1 });

            manager.GetComponentRef<Position>(matching).Value = 0;

            var query = manager.Query().WithAll<Position>();
            var system = new StoreItemCountInPositionSystem();
            var handle = manager.RunNonBurst(query, ref system);
            handle.Complete();

            Assert.AreEqual(2, manager.GetComponent<Position>(matching).Value);

            handle.Dispose();
            manager.Dispose();
        }

        // -- RunNonBurst two buffers ------------------------------------------

        [Test]
        public void EntityManager_RunNonBurst_TwoBuffers_SumsBoth()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int arch = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Buffer<Item>(), ArchetypeType.Buffer<Skill>() });

            var entity = manager.CreateEntity(arch);
            manager.GetBuffer<Item>(entity).Add(new Item  { Id = 5 });
            manager.GetBuffer<Item>(entity).Add(new Item  { Id = 6 });
            manager.GetBuffer<Skill>(entity).Add(new Skill { SkillId = 100 });

            var query = manager.Query();
            var system = new SumBothBuffersSystem();
            var handle = manager.RunNonBurst(query, ref system);
            handle.Complete();

            Assert.AreEqual(11,  system.ItemTotal);
            Assert.AreEqual(100, system.SkillTotal);

            handle.Dispose();
            manager.Dispose();
        }
    }
}
