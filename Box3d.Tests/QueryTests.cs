using System;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Box3d.Tests
{
    /// <summary>Step-5 surface: query/mass/explosion type layouts, closest-hit ray casts, mass
    /// computation, userData round-trips, and body introspection.</summary>
    public class QueryTests
    {
        [Test]
        public void QueryStructSizes_MatchNativeLayout()
        {
            Assert.AreEqual(32, UnsafeUtility.SizeOf<QueryFilter>());
            Assert.AreEqual(64, UnsafeUtility.SizeOf<RayResult>());
            Assert.AreEqual(52, UnsafeUtility.SizeOf<MassData>());
            Assert.AreEqual(32, UnsafeUtility.SizeOf<ExplosionDef>());
        }

        [Test]
        public void CastRayClosest_HitsSphereAtExpectedPoint()
        {
            World world = World.Create(WorldDef.Default);

            Body body = world.CreateBody(BodyDef.Default);
            body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 1f });

            RayResult result = world.CastRayClosest(
                new float3(-5f, 0f, 0f), new float3(10f, 0f, 0f), QueryFilter.Default);

            Assert.IsTrue(result.Hit, "ray straight through the sphere must hit");
            Assert.AreEqual(-1f, result.Point.x, 1e-3f, "hit point should be on the sphere surface");
            Assert.AreEqual(0.4f, result.Fraction, 1e-3f);
            Assert.AreEqual(-1f, result.Normal.x, 1e-3f, "normal should face the ray origin");

            RayResult miss = world.CastRayClosest(
                new float3(-5f, 5f, 0f), new float3(10f, 0f, 0f), QueryFilter.Default);
            Assert.IsFalse(miss.Hit);

            world.Destroy();
        }

        [Test]
        public void MassData_MatchesAnalyticSphereMass()
        {
            World world = World.Create(WorldDef.Default);

            BodyDef bodyDef = BodyDef.Default;
            bodyDef.Type = BodyType.Dynamic;
            Body body = world.CreateBody(bodyDef);
            body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });

            // Density 1000 (water, the default) → m = 4/3·π·r³·ρ ≈ 523.6 kg.
            MassData massData = body.GetMassData();
            Assert.AreEqual(4f / 3f * math.PI * 0.125f * 1000f, massData.Mass, 0.5f);
            Assert.AreEqual(0f, massData.Center.x, 1e-4f);

            world.Destroy();
        }

        [Test]
        public void UserData_RoundTripsOnAllObjects()
        {
            World world = World.Create(WorldDef.Default);
            world.UserData = (IntPtr)111;
            Assert.AreEqual((IntPtr)111, world.UserData);

            Body bodyA = world.CreateBody(BodyDef.Default);
            BodyDef dynamicDef = BodyDef.Default;
            dynamicDef.Type = BodyType.Dynamic;
            dynamicDef.Position = new float3(0f, 1f, 0f);
            Body bodyB = world.CreateBody(dynamicDef);
            bodyB.UserData = (IntPtr)222;
            Assert.AreEqual((IntPtr)222, bodyB.UserData);

            Shape shape = bodyB.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });
            shape.UserData = (IntPtr)333;
            Assert.AreEqual((IntPtr)333, shape.UserData);

            DistanceJointDef jointDef = DistanceJointDef.Default;
            jointDef.Base.BodyIdA = bodyA.Id;
            jointDef.Base.BodyIdB = bodyB.Id;
            Joint joint = world.CreateDistanceJoint(jointDef);
            joint.UserData = (IntPtr)444;
            Assert.AreEqual((IntPtr)444, joint.UserData);

            world.Destroy();
        }

        [Test]
        public void Explode_PushesBodyInZeroGravity()
        {
            WorldDef worldDef = WorldDef.Default;
            worldDef.Gravity = float3.zero;
            World world = World.Create(worldDef);

            BodyDef bodyDef = BodyDef.Default;
            bodyDef.Type = BodyType.Dynamic;
            bodyDef.Position = new float3(2f, 0f, 0f);
            Body body = world.CreateBody(bodyDef);
            body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });

            ExplosionDef explosion = ExplosionDef.Default;
            explosion.Position = float3.zero;
            explosion.Radius = 5f;
            explosion.ImpulsePerArea = 100f;
            world.Explode(explosion);
            world.Step(1f / 60f);

            Assert.Greater(body.LinearVelocity.x, 0f, "explosion at origin should push the body along +x");

            world.Destroy();
        }

        [Test]
        public void GetShapesAndJoints_FillBuffers()
        {
            World world = World.Create(WorldDef.Default);

            BodyDef bodyDef = BodyDef.Default;
            bodyDef.Type = BodyType.Dynamic;
            bodyDef.Position = new float3(0f, 1f, 0f);
            Body body = world.CreateBody(bodyDef);
            Shape s1 = body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });
            Shape s2 = body.CreateSphereShape(ShapeDef.Default, new Sphere { Center = new float3(0f, 1f, 0f), Radius = 0.25f });

            Span<ShapeId> shapes = stackalloc ShapeId[8];
            int count = body.GetShapes(shapes);
            Assert.AreEqual(2, count);
            Assert.IsTrue(shapes[0].Equals(s1.Id) || shapes[1].Equals(s1.Id));
            Assert.IsTrue(shapes[0].Equals(s2.Id) || shapes[1].Equals(s2.Id));

            Body anchor = world.CreateBody(BodyDef.Default);
            DistanceJointDef jointDef = DistanceJointDef.Default;
            jointDef.Base.BodyIdA = anchor.Id;
            jointDef.Base.BodyIdB = body.Id;
            Joint joint = world.CreateDistanceJoint(jointDef);

            Span<JointId> joints = stackalloc JointId[4];
            Assert.AreEqual(1, body.GetJoints(joints));
            Assert.IsTrue(joints[0].Equals(joint.Id));

            world.Destroy();
        }
    }
}
