using System;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Box3D.Tests
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
            Type bindings = typeof(World).Assembly.GetType("Box3D.b3CompoundSphereDef", throwOnError: true);
            Assert.AreEqual(UnsafeUtility.SizeOf(bindings), UnsafeUtility.SizeOf<CompoundSphereInstance>());
            Assert.AreEqual(
                UnsafeUtility.SizeOf(typeof(World).Assembly.GetType("Box3D.b3CompoundCapsuleDef", throwOnError: true)),
                UnsafeUtility.SizeOf<CompoundCapsuleInstance>());
            Assert.AreEqual(
                UnsafeUtility.SizeOf(typeof(World).Assembly.GetType("Box3D.b3CompoundHullDef", throwOnError: true)),
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
        public void MeshFromRawTriangleSoup_SupportsFallingSphere()
        {
            // Exercises the hand-marshalled b3MeshDef path (pinned vertex/index spans), unlike the
            // engine-generated CreateBox/CreateGrid variants. Quad winding matches the terrain
            // generator (upward faces): (0,2,1)(1,2,3) over an x-fastest vertex grid.
            var vertices = new float3[]
            {
                new float3(-5f, 0f, -5f), new float3(5f, 0f, -5f),
                new float3(-5f, 0f, 5f), new float3(5f, 0f, 5f),
            };
            var triangles = new[] { 0, 2, 1, 1, 2, 3 };

            TriangleMesh mesh = TriangleMesh.Create(vertices, triangles);
            Assert.IsTrue(mesh.IsCreated);

            World world = World.Create(WorldDef.Default);
            Body ground = world.CreateBody(BodyDef.Default);
            Assert.IsTrue(ground.CreateMeshShape(ShapeDef.Default, mesh).IsValid);

            BodyDef sphereDef = BodyDef.Default;
            sphereDef.Type = BodyType.Dynamic;
            sphereDef.Position = new float3(0f, 3f, 0f);
            Body sphere = world.CreateBody(sphereDef);
            sphere.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });

            for (int i = 0; i < 300; i++)
            {
                world.Step(1f / 60f);
            }

            Assert.AreEqual(0.5f, sphere.Position.y, 0.05f, "sphere should rest exactly on the y=0 quad");
            Assert.IsFalse(sphere.IsAwake);

            world.Destroy();
            mesh.Destroy();
        }

        [Test]
        public void HeightFieldFromRawHeights_SupportsFallingSphere()
        {
            // Exercises the hand-marshalled b3HeightFieldDef path (pinned heights span): a flat
            // 5×5 field at height 2 within a [0,4] quantization range.
            var heights = new float[25];
            for (int i = 0; i < heights.Length; i++) heights[i] = 2f;

            HeightField field = HeightField.Create(heights, 5, 5, new float3(1f, 1f, 1f),
                globalMinimumHeight: 0f, globalMaximumHeight: 4f);
            Assert.IsTrue(field.IsCreated);

            World world = World.Create(WorldDef.Default);
            Body ground = world.CreateBody(BodyDef.Default);
            Assert.IsTrue(ground.CreateHeightFieldShape(ShapeDef.Default, field).IsValid);

            BodyDef sphereDef = BodyDef.Default;
            sphereDef.Type = BodyType.Dynamic;
            sphereDef.Position = new float3(1f, 5f, 1f);
            Body sphere = world.CreateBody(sphereDef);
            sphere.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.3f });

            for (int i = 0; i < 300; i++)
            {
                world.Step(1f / 60f);
            }

            Assert.IsFalse(sphere.IsAwake, "sphere should come to rest on the height field");
            Assert.Greater(sphere.Position.y, 1.5f, "sphere should rest near the field's height of 2");

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

        [Test]
        public void CompoundShape_BakesMeshInstances()
        {
            int baseline = UnsafeBindings.b3GetByteCount();

            // A unit box mesh instanced twice (the second one stretched 3× in Y) plus a sphere. The
            // mesh is destroyed right after baking to prove the compound keeps its own copy.
            TriangleMesh mesh = TriangleMesh.CreateBox(float3.zero, new float3(0.5f, 0.5f, 0.5f));
            var meshes = new[]
            {
                new CompoundMeshInstance
                {
                    Mesh = mesh,
                    Transform = B3Transform.Identity,
                    Scale = new float3(1f, 1f, 1f),
                    Material = SurfaceMaterial.Default,
                },
                new CompoundMeshInstance
                {
                    Mesh = mesh,
                    Transform = new B3Transform { Position = new float3(4f, 0f, 0f), Rotation = quaternion.identity },
                    Scale = new float3(1f, 3f, 1f),
                    Material = SurfaceMaterial.Default,
                },
            };
            var spheres = new[]
            {
                new CompoundSphereInstance
                {
                    Sphere = new Sphere { Center = new float3(-4f, 0f, 0f), Radius = 0.5f },
                    Material = SurfaceMaterial.Default,
                },
            };

            Compound compound = Compound.Create(spheres, meshes: meshes);
            Assert.IsTrue(compound.IsCreated);
            Assert.AreEqual(3, compound.ChildCount);
            mesh.Destroy();

            World world = World.Create(WorldDef.Default);
            Body body = world.CreateBody(BodyDef.Default);
            Assert.IsTrue(body.CreateCompoundShape(ShapeDef.Default, compound).IsValid);
            AssertRayHitsTopAt(world, x: -4f, expectedY: 0.5f);
            AssertRayHitsTopAt(world, x: 0f, expectedY: 0.5f);
            AssertRayHitsTopAt(world, x: 4f, expectedY: 1.5f);
            world.Destroy();

            compound.Destroy();
            Assert.AreEqual(baseline, UnsafeBindings.b3GetByteCount(), "compound memory should return to baseline");
        }

        [Test]
        public void CompoundCreate_RejectsUnsetMeshScale()
        {
            TriangleMesh mesh = TriangleMesh.CreateBox(float3.zero, new float3(0.5f, 0.5f, 0.5f));
            var meshes = new[] { new CompoundMeshInstance { Mesh = mesh, Transform = B3Transform.Identity } };
            Assert.Throws<ArgumentException>(() => Compound.Create(meshes: meshes));
            mesh.Destroy();
        }

        [Test]
        public void Compound_RoundTripsThroughBytes()
        {
            Compound baked = BakeSampleCompound();
            int byteCount = baked.ByteCount;
            int childCount = baked.ChildCount;
            Assert.Greater(byteCount, 0);
            Assert.AreEqual(3, childCount);

            byte[] bytes = baked.ToArray();
            Assert.AreEqual(byteCount, bytes.Length);
            AssertSampleCompoundCollides(baked); // serializing must leave the original usable
            baked.Destroy();

            Compound restored = Compound.FromBytes(bytes);
            Assert.IsTrue(restored.IsCreated);
            Assert.AreEqual(byteCount, restored.ByteCount);
            Assert.AreEqual(childCount, restored.ChildCount);
            AssertSampleCompoundCollides(restored);
            restored.Destroy();
            Assert.IsFalse(restored.IsCreated);
        }

        [Test]
        public void Compound_RoundTripsThroughStreams()
        {
            Compound baked = BakeSampleCompound();
            int byteCount = baked.ByteCount;
            var stream = new MemoryStream();
            baked.WriteTo(stream);
            Assert.AreEqual(byteCount, stream.Length);
            AssertSampleCompoundCollides(baked);
            stream.WriteByte(0xAB); // trailing data after the blob belongs to the caller
            baked.Destroy();

            stream.Position = 0;
            Compound restored = Compound.ReadFrom(new ChunkedStream(stream, chunk: 7)); // forces partial reads
            Assert.IsTrue(restored.IsCreated);
            Assert.AreEqual(byteCount, restored.ByteCount);
            Assert.AreEqual(byteCount, stream.Position, "ReadFrom must stop right after the blob");
            AssertSampleCompoundCollides(restored);
            restored.Destroy();
        }

        [Test]
        public void Compound_FromNativeMemory_WrapsBufferInPlace()
        {
            Compound baked = BakeSampleCompound();
            var buffer = new NativeArray<byte>(baked.ByteCount + 16, Allocator.Persistent); // slack after the blob
            Assert.AreEqual(baked.ByteCount, baked.WriteTo(buffer.AsSpan()));
            baked.Destroy();

            Compound wrapped = Compound.FromNativeMemory(buffer);
            Assert.IsTrue(wrapped.IsCreated);
            AssertSampleCompoundCollides(wrapped);
            Compound instanced = Compound.FromNativeMemory(buffer); // same buffer again: the fixup is idempotent
            Assert.IsTrue(instanced.IsCreated);
            AssertSampleCompoundCollides(instanced);

            wrapped.Destroy();
            instanced.Destroy();
            Assert.IsTrue(buffer.IsCreated, "Destroy must not free caller-owned memory");
            buffer.Dispose();
        }

        [Test]
        public void Compound_RejectsForeignOrCorruptBytes()
        {
            Compound baked = BakeSampleCompound();
            byte[] bytes = baked.ToArray();
            baked.Destroy();

            byte[] wrongVersion = (byte[])bytes.Clone();
            wrongVersion[0] ^= 1;
            Assert.IsFalse(Compound.FromBytes(wrongVersion).IsCreated);
            Assert.IsFalse(Compound.ReadFrom(new MemoryStream(wrongVersion)).IsCreated);
            var wrongVersionNative = new NativeArray<byte>(wrongVersion, Allocator.Temp);
            Assert.IsFalse(Compound.FromNativeMemory(wrongVersionNative).IsCreated);
            wrongVersionNative.Dispose();

            Assert.IsFalse(Compound.FromBytes(new ReadOnlySpan<byte>(bytes, 0, bytes.Length - 1)).IsCreated, "declared length exceeds the data");
            Assert.Throws<ArgumentException>(() => Compound.FromBytes(new byte[8]));

            var truncated = new MemoryStream(bytes, 0, bytes.Length - 1);
            Assert.Throws<EndOfStreamException>(() => Compound.ReadFrom(truncated)); // seekable: length check up front
            Assert.Throws<EndOfStreamException>(
                () => Compound.ReadFrom(new ChunkedStream(new MemoryStream(bytes, 0, bytes.Length - 1), chunk: 64))); // non-seekable: read loop
        }

        /// <summary>One child of each baked kind, all with their top face at y = 0.5: a sphere at
        /// x = -4, a box hull at x = 0, and a box mesh at x = 4. The hull and mesh are destroyed right
        /// away — the compound bakes its own copies.</summary>
        private static Compound BakeSampleCompound()
        {
            var corners = new float3[8];
            for (int i = 0; i < 8; i++)
            {
                corners[i] = new float3((i & 1) == 0 ? -0.5f : 0.5f, (i & 2) == 0 ? -0.5f : 0.5f, (i & 4) == 0 ? -0.5f : 0.5f);
            }
            Hull hull = Hull.Create(corners);
            TriangleMesh mesh = TriangleMesh.CreateBox(float3.zero, new float3(0.5f, 0.5f, 0.5f));

            var spheres = new[]
            {
                new CompoundSphereInstance
                {
                    Sphere = new Sphere { Center = new float3(-4f, 0f, 0f), Radius = 0.5f },
                    Material = SurfaceMaterial.Default,
                },
            };
            var hulls = new[]
            {
                new CompoundHullInstance { Hull = hull, Transform = B3Transform.Identity, Material = SurfaceMaterial.Default },
            };
            var meshes = new[]
            {
                new CompoundMeshInstance
                {
                    Mesh = mesh,
                    Transform = new B3Transform { Position = new float3(4f, 0f, 0f), Rotation = quaternion.identity },
                    Scale = new float3(1f, 1f, 1f),
                    Material = SurfaceMaterial.Default,
                },
            };
            Compound compound = Compound.Create(spheres, hulls: hulls, meshes: meshes);
            Assert.IsTrue(compound.IsCreated);
            hull.Destroy();
            mesh.Destroy();
            return compound;
        }

        /// <summary>Attaches the sample compound to a fresh static body and ray casts each child.</summary>
        private static void AssertSampleCompoundCollides(Compound compound)
        {
            World world = World.Create(WorldDef.Default);
            Body body = world.CreateBody(BodyDef.Default);
            Assert.IsTrue(body.CreateCompoundShape(ShapeDef.Default, compound).IsValid);
            AssertRayHitsTopAt(world, x: -4f, expectedY: 0.5f);
            AssertRayHitsTopAt(world, x: 0f, expectedY: 0.5f);
            AssertRayHitsTopAt(world, x: 4f, expectedY: 0.5f);
            world.Destroy();
        }

        private static void AssertRayHitsTopAt(World world, float x, float expectedY)
        {
            RayResult hit = world.CastRayClosest(new float3(x, 5f, 0f), new float3(0f, -10f, 0f), QueryFilter.Default);
            Assert.IsTrue(hit.Hit, $"ray at x = {x} should hit");
            Assert.AreEqual(expectedY, hit.Point.y, 1e-3f, $"hit height at x = {x}");
        }

        /// <summary>Hands out at most <c>chunk</c> bytes per Read and cannot seek, so a loader must
        /// loop over partial reads and cannot rely on the stream length.</summary>
        private sealed class ChunkedStream : Stream
        {
            private readonly Stream _inner;
            private readonly int _chunk;

            public ChunkedStream(Stream inner, int chunk)
            {
                _inner = inner;
                _chunk = chunk;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _inner.Read(buffer, offset, Math.Min(count, _chunk));
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
