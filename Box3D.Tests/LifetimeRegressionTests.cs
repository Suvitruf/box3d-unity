using System;
using NUnit.Framework;
using Unity.Mathematics;

namespace Box3D.Tests
{
    public class LifetimeRegressionTests
    {
        [Test]
        public void StaleWorldDestroy_PreservesReplacementAndCallbacks()
        {
            World world = World.Create(WorldDef.Default);
            World stale = world;
            world.Destroy();
            World replacement = World.Create(WorldDef.Default);
            try
            {
                Assert.AreEqual(stale.Id.Index1, replacement.Id.Index1);
                int calls = 0;
                replacement.SetCustomFilterCallback((a, b) => { calls++; return false; });
                stale.Destroy();
                Assert.IsTrue(replacement.IsValid);
                Assert.IsTrue(stale.Id.IsNull);
                Assert.IsTrue(DebugDrawBridge.IsBridgeOwned(replacement.Id));
                BodyDef bodyDef = BodyDef.Default;
                bodyDef.Type = BodyType.Dynamic;
                ShapeDef shapeDef = ShapeDef.Default;
                shapeDef.EnableCustomFiltering = true;
                for (int i = 0; i < 2; i++)
                {
                    Body body = replacement.CreateBody(bodyDef);
                    body.CreateSphereShape(shapeDef, new Sphere { Radius = 1f });
                }
                replacement.Step(1f / 60f);
                Assert.Greater(calls, 0, "stale destroy must not clear the new world's callback slot");
            }
            finally { replacement.Destroy(); }
        }

        [Test]
        public void StaleBodyShapeAndJointDestroy_PreserveReusedSlots()
        {
            World world = World.Create(WorldDef.Default);
            try
            {
                Body first = world.CreateBody(BodyDef.Default);
                Body staleBody = first;
                first.Destroy();
                Body body = world.CreateBody(BodyDef.Default);
                staleBody.Destroy();
                Assert.IsTrue(body.IsValid);

                Shape firstShape = body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 1f });
                Shape staleShape = firstShape;
                firstShape.Destroy();
                Shape shape = body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 1f });
                staleShape.Destroy();
                Assert.IsTrue(shape.IsValid);

                BodyDef def = BodyDef.Default;
                def.Type = BodyType.Dynamic;
                Body other = world.CreateBody(def);
                FilterJointDef jointDef = FilterJointDef.Default;
                jointDef.Base.BodyIdA = body.Id;
                jointDef.Base.BodyIdB = other.Id;
                Joint firstJoint = world.CreateFilterJoint(jointDef);
                Joint staleJoint = firstJoint;
                firstJoint.Destroy();
                Joint joint = world.CreateFilterJoint(jointDef);
                staleJoint.Destroy();
                Assert.IsTrue(joint.IsValid);
            }
            finally { world.Destroy(); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DegenerateMeshCreation_DoesNotLeak(bool weld)
        {
            // Nonzero area before welding; welding collapses all three vertices.
            float3[] vertices = weld
                ? new[] { float3.zero, new float3(0.001f, 0f, 0f), new float3(0f, 0.001f, 0f) }
                : new[] { float3.zero, float3.zero, float3.zero };
            int[] indices = { 0, 1, 2 };
            int baseline = UnsafeBindings.b3GetByteCount();
            for (int i = 0; i < 100; i++)
            {
                TriangleMesh mesh = TriangleMesh.Create(vertices, indices, weldVertices: weld);
                try { Assert.IsFalse(mesh.IsCreated); }
                finally { mesh.Destroy(); }
            }
            Assert.AreEqual(baseline, UnsafeBindings.b3GetByteCount());
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void MeshRejectsOutOfRangeIndices(int invalidIndex)
        {
            float3[] vertices = { float3.zero, new float3(1f, 0f, 0f), new float3(0f, 1f, 0f) };
            Assert.Throws<ArgumentOutOfRangeException>(() => TriangleMesh.Create(vertices, new[] { 0, 1, invalidIndex }));
        }

        [Test]
        public void MeshBounds_FollowNativePrecision()
        {
            float3[] vertices = { new float3(1e6f, 0f, 0f), new float3(1e6f, 1f, 0f), new float3(1e6f, 0f, 1f) };
            int baseline = UnsafeBindings.b3GetByteCount();
            TriangleMesh mesh = TriangleMesh.Create(vertices, new[] { 0, 1, 2 });
            try
            {
#if BOX3D_DOUBLE
                Assert.IsTrue(mesh.IsCreated);
#else
                Assert.IsFalse(mesh.IsCreated);
#endif
            }
            finally { mesh.Destroy(); }
            Assert.AreEqual(baseline, UnsafeBindings.b3GetByteCount());
        }

        [Test]
        public void MeshRejectsNonFiniteVertices()
        {
            float3[] vertices = { float3.zero, new float3(float.NaN, 0f, 0f), new float3(0f, 1f, 0f) };
            Assert.Throws<ArgumentException>(() => TriangleMesh.Create(vertices, new[] { 0, 1, 2 }));
        }

        [Test]
        public void WeldedMesh_PreservesUsableTriangles()
        {
            float3[] vertices = { float3.zero, new float3(1f, 0f, 0f), new float3(0f, 1f, 0f), new float3(0.001f) };
            TriangleMesh mesh = TriangleMesh.Create(vertices, new[] { 3, 1, 2 }, weldVertices: true);
            try { Assert.IsTrue(mesh.IsCreated); }
            finally { mesh.Destroy(); }
        }
    }
}
