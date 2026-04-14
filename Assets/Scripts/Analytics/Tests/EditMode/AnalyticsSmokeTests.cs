using System;
using System.Linq;
using Glai.Allocator.Core;
using Glai.Analytics;
using NUnit.Framework;
using Unity.Collections;

namespace Glai.Analytics.Tests.EditMode
{
    public class AnalyticsSmokeTests
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

        private sealed class FakeAllocator : IAllocatorBase
        {
            public FixedString128Bytes Name => new FixedString128Bytes("AnalyticsFake");
            public int Count => 32;
            public int Capacity => 128;
            public void Dispose()
            {
            }
        }

        [Test]
        public void RegisterAndUnregisterAllocator_UpdatesCollection()
        {
            var allocator = new FakeAllocator();

            MemoryAnalytics.RegisterAllocator(allocator);
            bool registered = MemoryAnalytics.GetCollections().Contains(allocator);
            MemoryAnalytics.UnregisterAllocator(allocator);
            bool removed = !MemoryAnalytics.GetCollections().Contains(allocator);

            Assert.IsTrue(registered);
            Assert.IsTrue(removed);
        }

        [Test]
        public void GetCollections_ReturnsNonNullCollection()
        {
            Assert.IsNotNull(MemoryAnalytics.GetCollections());
        }
    }
}
