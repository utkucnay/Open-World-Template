using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Glai.Gameplay.Tests.EditMode
{
    public class PackedTransformComponentTests
    {
        [Test]
        public void PackedTransformLayout_StaysGpuCompatible()
        {
            Assert.That(UnsafeUtility.SizeOf<PackedTransformComponent>(), Is.EqualTo(20));
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(15f, 25f, 35f)]
        [TestCase(90f, 0f, 0f)]
        [TestCase(0f, 90f, 0f)]
        [TestCase(0f, 0f, 90f)]
        [TestCase(180f, 0f, 0f)]
        [TestCase(0f, 180f, 0f)]
        [TestCase(0f, 0f, 180f)]
        [TestCase(120f, -45f, 210f)]
        public void Rotation_RoundTripsThroughPacking(float xDegrees, float yDegrees, float zDegrees)
        {
            quaternion expected = quaternion.EulerXYZ(math.radians(new float3(xDegrees, yDegrees, zDegrees)));
            AssertRoundTrip(expected);
        }

        [Test]
        public void Rotation_RoundTripsForQuaternionAndNegatedQuaternion()
        {
            quaternion expected = math.normalize(new quaternion(0.1452f, -0.5821f, 0.2441f, 0.7634f));
            quaternion negated = new quaternion(-expected.value);

            uint2 packed = PackedTransformComponent.PackQuaternion(expected.value);
            uint2 negatedPacked = PackedTransformComponent.PackQuaternion(negated.value);

            Assert.That(negatedPacked, Is.EqualTo(packed));
            AssertAlignment(expected, PackedTransformComponent.UnpackQuaternion(packed));
        }

        [Test]
        public void Rotation_RoundTripsRandomizedNormalizedQuaternions()
        {
            var random = new Unity.Mathematics.Random(0x6E624EB7u);

            for (int i = 0; i < 256; i++)
            {
                float4 value = random.NextFloat4(-1f, 1f);
                if (math.lengthsq(value) < 1e-4f)
                    value = new float4(0f, 0f, 0f, 1f);

                quaternion expected = math.normalize(new quaternion(value));
                AssertRoundTrip(expected);
            }
        }

        static void AssertRoundTrip(quaternion expected)
        {
            uint2 packed = PackedTransformComponent.PackQuaternion(expected.value);
            quaternion unpacked = PackedTransformComponent.UnpackQuaternion(packed);
            AssertAlignment(expected, unpacked);
        }

        static void AssertAlignment(quaternion expected, quaternion unpacked)
        {
            float alignment = math.abs(math.dot(expected.value, unpacked.value));
            Assert.That(alignment, Is.GreaterThan(0.999f));
        }
    }
}
