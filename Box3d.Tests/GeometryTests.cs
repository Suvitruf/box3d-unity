using System;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Box3d.Tests
{
    /// <summary>Geometry owner tests: layout guards for compound instance structs, native memory
    /// round-trips (create/destroy without leaks), and simulation against each geometry type.</summary>
    public class GeometryTests
    {
        private const float TimeStep = 1f / 60f;

        [Test]
        public void CompoundInstanceStructs_MatchGeneratedNativeLayout()
        {
            // Public instances are pointer-cast onto the generated native defs — sizes must match.
            Type bindings = typeof(World).Assembly.GetType("Box3d.b3CompoundSphereDef", throwOnError: true);
            Assert.AreEqual(UnsafeUtility.SizeOf(bindings), UnsafeUtility.SizeOf<CompoundSphereInstance>());
            Assert.AreEqual(
                UnsafeUtility.SizeOf(typeof(World).Assembly.GetType("Box3d.b3CompoundCapsuleDef", throwOnError: true)),
                UnsafeUtility.SizeOf<CompoundCapsuleInstance>());
            Assert.AreEqual(
                UnsafeUtility.SizeOf(typeof(World).Assembly.GetType("Box3d.b3CompoundHullDef", throwOnError: true)),
                UnsafeUtility.SizeOf<CompoundHullInstance>());
        }

        [Test]
        public void GeometryCreateDestroy_DoesNotLeak()
        {
            int baseline = UnsafeBindings.b3GetByteCount();

            Hull hull = Hull.CreateCylinder(2f, 0.5f);
            TriangleMesh mesh = TriangleMesh.CreateBox(float3.zero, new float3(1f, 1f, 1f));
            HeightField field = HeightField.CreateGrid(8, 8, new float3(1f, 1f, 1f));
            Assert.IsTrue(hull.IsCreated);
            Assert.IsTrue(mesh.IsCreated);
            Assert.IsTrue(field.IsCreated);
            Assert.Greater(UnsafeBindings.b3GetByteCount(), baseline);

            hull.Destroy();
            mesh.Destroy();
            field.Destroy();
            Assert.IsFalse(hull.IsCreated);
            Assert.AreEqual(baseline, UnsafeBindings.b3GetByteCount(), "geometry memory should return to baseline");
        }

        [Test]
        public void HullFromPoints_SimulatesAndSurvivesEarlyDestroy()
        {
            // Octahedron point cloud.
            var points = new float3[]
            {
                new float3(1f, 0f, 0f), new float3(-1f, 0f, 0f),
                new float3(0f, 1f, 0f), new float3(0f, -1f, 0f),
                new float3(0f, 0f, 1f), new float3(0f, 0f, -1f),
            };

            World world = World.Create(WorldDef.Default);
            Body ground = world.CreateBody(BodyDef.Default);
            BoxHull groundHull = BoxHull.Create(10f, 0.5f, 10f);
            ground.CreateHullShape(ShapeDef.Default, in groundHull);

            BodyDef bodyDef = BodyDef.Default;
            bodyDef.Type = BodyType.Dynamic;
            bodyDef.Position = new float3(0f, 3f, 0f);
            Body body = world.CreateBody(bodyDef);

            Hull hull = Hull.Create(points);
            Assert.IsTrue(hull.IsCreated);
            Shape shape = body.CreateHullShape(ShapeDef.Default, hull);
            Assert.IsTrue(shape.IsValid);
            hull.Destroy(); // hulls are cloned — destroying immediately must be safe

            for (int i = 0; i < 300; i++)
            {
                world.Step(TimeStep);
            }

            Assert.Greater(body.Position.y, 0.5f, "octahedron should rest on the ground, not fall through");
            Assert.IsFalse(body.IsAwake);

            world.Destroy();
        }

        [Test]
        public void MeshGround_SupportsFallingSphere()
        {
            World world = World.Create(WorldDef.Default);

            TriangleMesh mesh = TriangleMesh.CreateBox(float3.zero, new float3(5f, 0.5f, 5f));
            Body ground = world.CreateBody(BodyDef.Default);
            Shape meshShape = ground.CreateMeshShape(ShapeDef.Default, mesh);
            Assert.IsTrue(meshShape.IsValid);

            BodyDef sphereDef = BodyDef.Default;
            sphereDef.Type = BodyType.Dynamic;
            sphereDef.Position = new float3(0f, 3f, 0f);
            Body sphere = world.CreateBody(sphereDef);
            sphere.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });

            for (int i = 0; i < 300; i++)
            {
                world.Step(TimeStep);
            }

            Assert.Greater(sphere.Position.y, 0.5f, "sphere should rest on the box mesh, not fall through");
            Assert.IsFalse(sphere.IsAwake);

            // Destroy order: world (shapes) first, then the referenced mesh data.
            world.Destroy();
            mesh.Destroy();
        }

        [Test]
        public void HeightFieldGround_CatchesFallingSphere()
        {
            World world = World.Create(WorldDef.Default);

            // 21×21 so the drop point at (5, 5) lands inside the field whether the grid spans
            // 0..20 or is centered on the origin.
            HeightField field = HeightField.CreateGrid(21, 21, new float3(1f, 1f, 1f));
            Body ground = world.CreateBody(BodyDef.Default);
            Shape fieldShape = ground.CreateHeightFieldShape(ShapeDef.Default, field);
            Assert.IsTrue(fieldShape.IsValid);

            BodyDef sphereDef = BodyDef.Default;
            sphereDef.Type = BodyType.Dynamic;
            sphereDef.Position = new float3(5f, 3f, 5f);
            Body sphere = world.CreateBody(sphereDef);
            sphere.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.3f });

            for (int i = 0; i < 300; i++)
            {
                world.Step(TimeStep);
            }

            Assert.IsFalse(sphere.IsAwake, "sphere should come to rest on the height field");
            Assert.Greater(sphere.Position.y, -1f, "sphere should not have fallen through the height field");

            world.Destroy();
            field.Destroy();
        }

        [Test]
        public void CompoundShape_BuildsFromInstances()
        {
            Hull hull = Hull.CreateCylinder(1f, 0.4f);

            var spheres = new[]
            {
                new CompoundSphereInstance
                {
                    Sphere = new Sphere { Center = new float3(-2f, 0.5f, 0f), Radius = 0.5f },
                    Material = SurfaceMaterial.Default,
                },
                new CompoundSphereInstance
                {
                    Sphere = new Sphere { Center = new float3(2f, 0.5f, 0f), Radius = 0.5f },
                    Material = SurfaceMaterial.Default,
                },
            };
            var hulls = new[]
            {
                new CompoundHullInstance
                {
                    Hull = hull,
                    Transform = B3Transform.Identity,
                    Material = SurfaceMaterial.Default,
                },
            };

            Compound compound = Compound.Create(spheres, hulls: hulls);
            Assert.IsTrue(compound.IsCreated);
            hull.Destroy(); // compound bakes its own copy at creation

            World world = World.Create(WorldDef.Default);
            Body body = world.CreateBody(BodyDef.Default);
            Shape shape = body.CreateCompoundShape(ShapeDef.Default, compound);
            Assert.IsTrue(shape.IsValid);
            world.Step(TimeStep);

            world.Destroy();
            compound.Destroy();
        }
    }
}
