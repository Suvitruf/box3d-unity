using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;

namespace Box3d.Tests
{
    /// <summary>Guards the ABI: every managed mirror must have exactly the size of its native
    /// counterpart (x64, single precision). Sizes are hand-derived from the box3d v0.1.0 headers —
    /// see Docs/box3d-api-notes.md. A failure here means silent memory corruption at runtime.</summary>
    public class StructLayoutTests
    {
        private static void AssertSize<T>(int expected) where T : struct
        {
            Assert.AreEqual(expected, UnsafeUtility.SizeOf<T>(), $"{typeof(T).Name} size mismatch vs native layout");
        }

        [Test]
        public void IdSizes()
        {
            AssertSize<WorldId>(4);
            AssertSize<BodyId>(8);
            AssertSize<ShapeId>(8);
            AssertSize<JointId>(8);
            AssertSize<ContactId>(12);
        }

        [Test]
        public void MathAndGeometrySizes()
        {
            AssertSize<B3Transform>(28);
            AssertSize<B3Aabb>(24);
            AssertSize<B3Matrix3>(36);
            AssertSize<Sphere>(16);
            AssertSize<Capsule>(28);
            AssertSize<HullData>(136);
            AssertSize<BoxHull>(440);
        }

        [Test]
        public void DefSizes()
        {
            AssertSize<Capacity>(20);
            AssertSize<MotionLocks>(6);
            AssertSize<CollisionFilter>(24);
            AssertSize<SurfaceMaterial>(40);
            AssertSize<WorldDef>(144);
            AssertSize<BodyDef>(104);
            AssertSize<ShapeDef>(112);
        }

        [Test]
        public void EventSizes()
        {
            AssertSize<BodyMoveEvent>(48);
        }

        [Test]
        public void NativeBoolIsOneByte()
        {
            AssertSize<NativeBool>(1);
        }
    }
}
