using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;

namespace Box3D.Tests
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

        // Structs carrying a world position (B3Pos / B3WorldTransform) change size under
        // BOX3D_DOUBLE. Both branches are hand-verified against a native sizeof dump compiled
        // from the pinned headers in the matching mode (see Docs/double-precision-plan.md).
        [Test]
        public void PositionCarryingSizes()
        {
#if BOX3D_DOUBLE
            AssertSize<B3Pos>(24);
            AssertSize<B3WorldTransform>(40);
            AssertSize<BodyDef>(120);
            AssertSize<BodyMoveEvent>(64);
            AssertSize<RayResult>(80);
            AssertSize<ExplosionDef>(48);
            AssertSize<ContactHitEvent>(88);
#else
            AssertSize<B3Pos>(12);
            AssertSize<B3WorldTransform>(28);
            AssertSize<BodyDef>(104);
            AssertSize<BodyMoveEvent>(48);
            AssertSize<RayResult>(64);
            AssertSize<ExplosionDef>(32);
            AssertSize<ContactHitEvent>(72);
#endif
        }

        [Test]
        public void DefSizes()
        {
            AssertSize<Capacity>(20);
            AssertSize<MotionLocks>(6);
            AssertSize<CollisionFilter>(24);
            AssertSize<SurfaceMaterial>(40);
            AssertSize<WorldDef>(144);
            AssertSize<ShapeDef>(112);
        }

        [Test]
        public void NativeBoolIsOneByte()
        {
            AssertSize<NativeBool>(1);
        }
    }
}
