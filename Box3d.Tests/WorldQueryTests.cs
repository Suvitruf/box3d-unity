using System;
using NUnit.Framework;
using Unity.Mathematics;

namespace Box3d.Tests
{
    /// <summary>Collector-based world queries: overlap AABB/shape, all-hits ray and shape casts.
    /// First test of managed callbacks invoked from native code.</summary>
    public class WorldQueryTests
    {
        private static World CreateThreeSphereWorld(out Shape left, out Shape middle, out Shape right)
        {
            World world = World.Create(WorldDef.Default);
            left = CreateStaticSphere(world, new float3(-3f, 0f, 0f));
            middle = CreateStaticSphere(world, new float3(0f, 0f, 0f));
            right = CreateStaticSphere(world, new float3(3f, 0f, 0f));
            return world;
        }

        private static Shape CreateStaticSphere(World world, float3 position)
        {
            BodyDef bodyDef = BodyDef.Default;
            bodyDef.Position = position;
            Body body = world.CreateBody(bodyDef);
            return body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });
        }

        [Test]
        public void OverlapAABB_FindsOnlyShapesInBox()
        {
            World world = CreateThreeSphereWorld(out Shape left, out Shape middle, out Shape right);

            // Box covering the middle and right spheres only.
            var aabb = new B3Aabb { LowerBound = new float3(-1f, -1f, -1f), UpperBound = new float3(4f, 1f, 1f) };
            Span<ShapeId> results = stackalloc ShapeId[8];
            int count = world.OverlapAABB(aabb, QueryFilter.Default, results);

            Assert.AreEqual(2, count);
            for (int i = 0; i < count; i++)
            {
                Assert.IsFalse(results[i].Equals(left.Id), "left sphere is outside the AABB");
            }

            world.Destroy();
        }

        [Test]
        public void CastRay_CollectsAllHitsAlongRay()
        {
            World world = CreateThreeSphereWorld(out Shape left, out Shape middle, out Shape right);

            Span<RayHit> hits = stackalloc RayHit[8];
            int count = world.CastRay(new float3(-10f, 0f, 0f), new float3(20f, 0f, 0f), QueryFilter.Default, hits);

            Assert.AreEqual(3, count, "ray through all three spheres should report three hits");
            for (int i = 0; i < count; i++)
            {
                Assert.Greater(hits[i].Fraction, 0f);
                Assert.Less(hits[i].Fraction, 1f);
                Assert.AreEqual(-1f, hits[i].Normal.x, 1e-3f, "normals should face the ray origin");
            }

            world.Destroy();
        }

        [Test]
        public void CastRay_BufferFullStopsEarly()
        {
            World world = CreateThreeSphereWorld(out _, out _, out _);

            Span<RayHit> hits = stackalloc RayHit[2];
            int count = world.CastRay(new float3(-10f, 0f, 0f), new float3(20f, 0f, 0f), QueryFilter.Default, hits);

            Assert.AreEqual(2, count, "collector must stop at buffer capacity without corruption");

            world.Destroy();
        }

        [Test]
        public void OverlapShape_SpherePointProxy()
        {
            World world = CreateThreeSphereWorld(out _, out Shape middle, out _);

            // Single point + radius = sphere proxy, overlapping only the middle sphere.
            Span<float3> proxyPoint = stackalloc float3[] { float3.zero };
            Span<ShapeId> results = stackalloc ShapeId[8];
            int count = world.OverlapShape(new float3(0.6f, 0f, 0f), proxyPoint, 0.2f, QueryFilter.Default, results);

            Assert.AreEqual(1, count);
            Assert.IsTrue(results[0].Equals(middle.Id));

            world.Destroy();
        }

        [Test]
        public void CastShape_SphereSweepHitsAll()
        {
            World world = CreateThreeSphereWorld(out _, out _, out _);

            Span<float3> proxyPoint = stackalloc float3[] { float3.zero };
            Span<RayHit> hits = stackalloc RayHit[8];
            int count = world.CastShape(new float3(-10f, 0f, 0f), proxyPoint, 0.3f,
                new float3(20f, 0f, 0f), QueryFilter.Default, hits);

            Assert.AreEqual(3, count, "swept sphere along the row should hit all three");

            world.Destroy();
        }
    }
}
