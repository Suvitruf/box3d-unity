using System;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Box3d.Tests
{
    /// <summary>Character mover toolkit: struct layouts, plane gathering, plane solving,
    /// velocity clipping, and mover casts.</summary>
    public class MoverTests
    {
        private static readonly Capsule MoverCapsule = new Capsule
        {
            Center1 = new float3(0f, 0.4f, 0f),
            Center2 = new float3(0f, 1.4f, 0f),
            Radius = 0.4f,
        };

        [Test]
        public void MoverStructSizes_MatchNativeLayout()
        {
            Assert.AreEqual(16, UnsafeUtility.SizeOf<B3Plane>());
            Assert.AreEqual(28, UnsafeUtility.SizeOf<PlaneResult>());
            Assert.AreEqual(28, UnsafeUtility.SizeOf<CollisionPlane>());
            Assert.AreEqual(16, UnsafeUtility.SizeOf<PlaneSolverResult>());
        }

        [Test]
        public void CollideMover_FindsGroundPlane()
        {
            World world = CreateGroundWorld();

            // Capsule slightly overlapping the ground top (y = 0.5).
            Span<CollisionPlane> planes = stackalloc CollisionPlane[8];
            int count = world.CollideMover(new float3(0f, 0.45f, 0f), MoverCapsule, QueryFilter.Default, planes);

            Assert.Greater(count, 0, "overlapping mover should gather at least one plane");
            bool foundUp = false;
            for (int i = 0; i < count; i++)
            {
                if (planes[i].Plane.Normal.y > 0.9f) foundUp = true;
            }
            Assert.IsTrue(foundUp, "ground contact should produce an upward-facing plane");

            world.Destroy();
        }

        [Test]
        public void SolvePlanes_PushesOutAndClipsVelocity()
        {
            World world = CreateGroundWorld();

            Span<CollisionPlane> planes = stackalloc CollisionPlane[8];
            int count = world.CollideMover(new float3(0f, 0.4f, 0f), MoverCapsule, QueryFilter.Default, planes);
            Span<CollisionPlane> active = planes.Slice(0, count);

            // Try to move down into the ground — the solver should refuse the downward motion.
            float3 target = new float3(0.2f, -0.5f, 0f);
            PlaneSolverResult result = Mover.SolvePlanes(target, active);
            Assert.Greater(result.Delta.y, target.y + 0.2f, "solver should cancel most downward motion");
            Assert.Greater(result.IterationCount, 0);

            // Velocity pointing into the ground gets clipped to sliding.
            float3 clipped = Mover.ClipVector(new float3(3f, -5f, 0f), active);
            Assert.GreaterOrEqual(clipped.y, -0.1f, "downward velocity should be clipped by the ground plane");
            Assert.AreEqual(3f, clipped.x, 0.1f, "tangential velocity should be preserved");

            world.Destroy();
        }

        [Test]
        public void CastMover_StopsAtWall()
        {
            World world = CreateGroundWorld();

            // Wall at x = 3: a 0.5-thick box from y 0..4.
            BodyDef wallDef = BodyDef.Default;
            wallDef.Position = new float3(3f, 2f, 0f);
            Body wall = world.CreateBody(wallDef);
            BoxHull wallHull = BoxHull.Create(0.25f, 2f, 3f);
            wall.CreateHullShape(ShapeDef.Default, in wallHull);

            float free = world.CastMover(new float3(0f, 1.2f, 0f), MoverCapsule, new float3(0f, 0f, 10f), QueryFilter.Default);
            Assert.AreEqual(1f, free, 1e-3f, "no obstacle along +z — full translation available");

            float blocked = world.CastMover(new float3(0f, 1.2f, 0f), MoverCapsule, new float3(10f, 0f, 0f), QueryFilter.Default);
            Assert.Less(blocked, 0.3f, "wall at x=3 should stop the 10 m sweep near fraction 0.24");
            Assert.Greater(blocked, 0.1f);

            world.Destroy();
        }

        private static World CreateGroundWorld()
        {
            World world = World.Create(WorldDef.Default);
            Body ground = world.CreateBody(BodyDef.Default);
            BoxHull hull = BoxHull.Create(20f, 0.5f, 20f);
            ground.CreateHullShape(ShapeDef.Default, in hull);
            return world;
        }
    }
}
