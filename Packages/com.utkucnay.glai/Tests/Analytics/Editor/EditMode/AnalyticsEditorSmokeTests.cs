using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Glai.Analytics.Editor;
using NUnit.Framework;
using UnityEngine;
using Glai.Allocator;
using Glai.Core;
using System;
using Glai.Analytics.Memory.Editor;
using Glai.Collections;

namespace Glai.Analytics.Editor.Tests.EditMode
{
    public class AnalyticsEditorSmokeTests
    {
        private sealed class FakeAllocator : IAllocator
        {
            private readonly FixedString128Bytes name;

            public FakeAllocator(
                string allocatorName,
                int count,
                int capacity,
                int handleCount = 0,
                int handleCapacity = 0,
                int? peakCount = null,
                int? peakHandleCount = null)
            {
                name = new FixedString128Bytes(allocatorName);
                Count = count;
                Capacity = capacity;
                HandleCount = handleCount;
                HandleCapacity = handleCapacity;
                PeakCount = peakCount ?? count;
                PeakHandleCount = peakHandleCount ?? handleCount;
            }

            public FixedString128Bytes Name => name;
            public int Count { get; }
            public int PeakCount { get; private set; }
            public int Capacity { get; }
            public int HandleCount { get; }
            public int PeakHandleCount { get; private set; }
            public int HandleCapacity { get; }

            public Handle Allocate<T>() where T : unmanaged
            {
                throw new NotImplementedException();
            }

            public HandleArray AllocateArray<T>(int capacity) where T : unmanaged
            {
                throw new NotImplementedException();
            }

            public void Deallocate(in Handle handle)
            {
                throw new NotImplementedException();
            }

            public void Deallocate(in HandleArray handle)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }

            public T Get<T>(in Handle handle) where T : unmanaged
            {
                throw new NotImplementedException();
            }

            public Span<T> GetArray<T>(in HandleArray handle) where T : unmanaged
            {
                throw new NotImplementedException();
            }

            public void ResetPeaks()
            {
                PeakCount = Count;
                PeakHandleCount = HandleCount;
            }

            public void Set<T>(in Handle handle, T value) where T : unmanaged
            {
                throw new NotImplementedException();
            }

            public void SetArray<T>(in HandleArray handle, in Span<T> values, int offset = 0) where T : unmanaged
            {
                throw new NotImplementedException();
            }

        }

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
        public void FormatBytes_FormatsKilobytesCorrectly()
        {
            MethodInfo method = typeof(MemoryAnalyticsWindow).GetMethod(
                "FormatBytes",
                BindingFlags.NonPublic | BindingFlags.Static);

            string formatted = (string)method.Invoke(null, new object[] { 2048L });

            Assert.AreEqual("2 KB", formatted);
        }

        [Test]
        public void FormatBytes_FormatsValuesLargerThanInt32()
        {
            MethodInfo method = typeof(MemoryAnalyticsWindow).GetMethod(
                "FormatBytes",
                BindingFlags.NonPublic | BindingFlags.Static);

            string formatted = (string)method.Invoke(null, new object[] { 3221225472L });

            Assert.AreEqual("3 GB", formatted);
        }

        [Test]
        public void BuildGroups_CreatesHierarchyFromUnderscoreSeparatedNames()
        {
            var window = ScriptableObject.CreateInstance<MemoryAnalyticsWindow>();
            MethodInfo buildGroupsMethod = typeof(MemoryAnalyticsWindow).GetMethod(
                "BuildGroups",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var allocators = new List<IAllocator>
            {
                new FakeAllocator("ECS_Persist", 10, 100),
                new FakeAllocator("ECS_ChunkStack_0", 20, 100),
                new FakeAllocator("Gameplay", 30, 100),
            };
            object[] args = { allocators, 0L, 0L, 0L, 0L, 0L, 0L, 0 };

            try
            {
                var groups = (IList)buildGroupsMethod.Invoke(window, args);
                Assert.AreEqual(2, groups.Count);

                object ecsGroup = GetNodeByName(groups, "ECS");
                IList ecsChildren = GetNodeChildren(ecsGroup);
                object persistNode = GetNodeByName(ecsChildren, "Persist");
                object chunkStackNode = GetNodeByName(ecsChildren, "ChunkStack");
                IList chunkStackChildren = GetNodeChildren(chunkStackNode);
                object leafNode = GetNodeByName(chunkStackChildren, "0");

                Assert.IsNotNull(ecsGroup);
                Assert.IsNotNull(persistNode);
                Assert.IsNotNull(chunkStackNode);
                Assert.IsNotNull(leafNode);
                Assert.IsNotNull(GetNodeByName(groups, "Gameplay"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void BuildGroups_SortsNumericChildrenNaturally()
        {
            var window = ScriptableObject.CreateInstance<MemoryAnalyticsWindow>();
            MethodInfo buildGroupsMethod = typeof(MemoryAnalyticsWindow).GetMethod(
                "BuildGroups",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var allocators = new List<IAllocator>
            {
                new FakeAllocator("ECS_ChunkStack_10", 10, 100),
                new FakeAllocator("ECS_ChunkStack_1", 10, 100),
                new FakeAllocator("ECS_ChunkStack_0", 10, 100),
            };
            object[] args = { allocators, 0L, 0L, 0L, 0L, 0L, 0L, 0 };

            try
            {
                var groups = (IList)buildGroupsMethod.Invoke(window, args);
                object ecsGroup = groups[0];
                object chunkStackNode = GetNodeByName(GetNodeChildren(ecsGroup), "ChunkStack");
                IList chunkChildren = GetNodeChildren(chunkStackNode);

                Assert.AreEqual("0", GetNodeName(chunkChildren[0]));
                Assert.AreEqual("1", GetNodeName(chunkChildren[1]));
                Assert.AreEqual("10", GetNodeName(chunkChildren[2]));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void BuildGroups_AggregatesTotalsAcrossParentHierarchy()
        {
            var window = ScriptableObject.CreateInstance<MemoryAnalyticsWindow>();
            MethodInfo buildGroupsMethod = typeof(MemoryAnalyticsWindow).GetMethod(
                "BuildGroups",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var allocators = new List<IAllocator>
            {
                new FakeAllocator("ECS_Persist", 10, 100, 3, 8, peakCount: 40, peakHandleCount: 6),
                new FakeAllocator("ECS_ChunkStack_0", 20, 100, 5, 16, peakCount: 60, peakHandleCount: 9),
            };
            object[] args = { allocators, 0L, 0L, 0L, 0L, 0L, 0L, 0 };

            try
            {
                var groups = (IList)buildGroupsMethod.Invoke(window, args);
                object ecsGroup = groups[0];
                object chunkStackNode = GetNodeByName(GetNodeChildren(ecsGroup), "ChunkStack");

                long totalPeakAllocated = (long)args[2];
                long totalPeakHandleCount = (long)args[5];
                long groupPeakAllocated = GetLongField(ecsGroup, "TotalPeakAllocated");
                long groupPeakHandleCount = GetLongField(ecsGroup, "TotalPeakHandleCount");
                long childPeakAllocated = GetLongField(chunkStackNode, "TotalPeakAllocated");
                long childPeakHandleCount = GetLongField(chunkStackNode, "TotalPeakHandleCount");

                Assert.AreEqual(100L, totalPeakAllocated);
                Assert.AreEqual(15L, totalPeakHandleCount);
                Assert.AreEqual(100L, groupPeakAllocated);
                Assert.AreEqual(15L, groupPeakHandleCount);
                Assert.AreEqual(60L, childPeakAllocated);
                Assert.AreEqual(9L, childPeakHandleCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void BuildGroups_AggregatesHandleTotals()
        {
            var window = ScriptableObject.CreateInstance<MemoryAnalyticsWindow>();
            MethodInfo buildGroupsMethod = typeof(MemoryAnalyticsWindow).GetMethod(
                "BuildGroups",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var allocators = new List<IAllocator>
            {
                new FakeAllocator("ECS_ChunkStack_0", 10, 100, 3, 8),
                new FakeAllocator("ECS_ChunkStack_1", 20, 100, 5, 16),
            };
            object[] args = { allocators, 0L, 0L, 0L, 0L, 0L, 0L, 0 };

            try
            {
                var groups = (IList)buildGroupsMethod.Invoke(window, args);
                object ecsGroup = groups[0];

                long totalHandleCount = (long)args[4];
                long totalHandleCapacity = (long)args[6];
                long groupHandleCount = GetLongField(ecsGroup, "TotalHandleCount");
                long groupHandleCapacity = GetLongField(ecsGroup, "TotalHandleCapacity");

                Assert.AreEqual(8L, totalHandleCount);
                Assert.AreEqual(24L, totalHandleCapacity);
                Assert.AreEqual(8L, groupHandleCount);
                Assert.AreEqual(24L, groupHandleCapacity);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void GetPageStartIndex_ClampsToLastPage()
        {
            MethodInfo method = typeof(MemoryAnalyticsWindow).GetMethod(
                "GetPageStartIndex",
                BindingFlags.NonPublic | BindingFlags.Static);

            int startIndex = (int)method.Invoke(null, new object[] { 99, 25, 10 });

            Assert.AreEqual(20, startIndex);
        }

        [Test]
        public void GetPageStartIndex_ReturnsZeroForFirstPage()
        {
            MethodInfo method = typeof(MemoryAnalyticsWindow).GetMethod(
                "GetPageStartIndex",
                BindingFlags.NonPublic | BindingFlags.Static);

            int startIndex = (int)method.Invoke(null, new object[] { 0, 25, 10 });

            Assert.AreEqual(0, startIndex);
        }

        private static long GetLongField(object target, string fieldName)
        {
            return (long)target.GetType().GetField(fieldName).GetValue(target);
        }

        private static IList GetNodeChildren(object node)
        {
            return (IList)node.GetType().GetField("Children").GetValue(node);
        }

        private static object GetNodeByName(IList nodes, string nodeName)
        {
            foreach (object node in nodes)
            {
                if (GetNodeName(node) == nodeName)
                {
                    return node;
                }
            }

            return null;
        }

        private static string GetNodeName(object node)
        {
            return (string)node.GetType().GetField("Name").GetValue(node);
        }
    }
}
