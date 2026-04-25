using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Glai.Gameplay.Tests.EditMode
{
    public class MeshRendererSystemTests
    {
        [Test]
        public void DefaultConfig_DisablesDynamicBoundsUpdates()
        {
            Assert.That(MeshRendererSystemConfig.Default.BoundsUpdateIntervalFrames, Is.EqualTo(0));
        }

        [Test]
        public void CalculateWorldBounds_UsesActualInstancePositions()
        {
            var sourceBounds = new Bounds(new Vector3(1f, 2f, 3f), Vector3.one);

            Bounds bounds = MeshRendererSystem.CalculateWorldBounds(
                new float3(-2f, 1f, 4f),
                new float3(6f, 3f, 8f),
                sourceBounds);

            Assert.That(bounds.min.x, Is.EqualTo(-1.5f));
            Assert.That(bounds.min.y, Is.EqualTo(2.5f));
            Assert.That(bounds.min.z, Is.EqualTo(6.5f));
            Assert.That(bounds.max.x, Is.EqualTo(7.5f));
            Assert.That(bounds.max.y, Is.EqualTo(5.5f));
            Assert.That(bounds.max.z, Is.EqualTo(11.5f));
        }
    }
}
