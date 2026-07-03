using NUnit.Framework;
using Unity.Mathematics;

namespace Box3d.Tests
{
    /// <summary>One behavioral test per joint type: proves each joint actually constrains motion
    /// the way its def promises, end-to-end through the generated API.</summary>
    public class JointBehaviorTests
    {
        private const float TimeStep = 1f / 60f;

        private static World CreateZeroGravityWorld()
        {
            WorldDef def = WorldDef.Default;
            def.Gravity = float3.zero;
            return World.Create(def);
        }

        private static Body CreateDynamicSphere(World world, float3 position, float radius = 0.5f)
        {
            BodyDef def = BodyDef.Default;
            def.Type = BodyType.Dynamic;
            def.Position = position;
            Body body = world.CreateBody(def);
            body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = radius });
            return body;
        }

        [Test]
        public void PrismaticJoint_MotorSlidesAndLimitClamps()
        {
            World world = CreateZeroGravityWorld();
            Body anchor = world.CreateBody(BodyDef.Default);
            Body slider = CreateDynamicSphere(world, float3.zero);

            PrismaticJointDef def = PrismaticJointDef.Default;
            def.Base.BodyIdA = anchor.Id;
            def.Base.BodyIdB = slider.Id;
            PrismaticJoint joint = world.CreatePrismaticJoint(def);

            joint.EnableMotor(true);
            joint.SetMaxMotorForce(10000f);
            joint.SetMotorSpeed(1f);
            joint.EnableLimit(true);
            joint.SetLimits(0f, 0.5f);

            for (int i = 0; i < 120; i++)
            {
                world.Step(TimeStep);
            }

            Assert.AreEqual(0.5f, joint.GetTranslation(), 0.02f, "motor should drive to the upper limit and stop");
            Assert.AreEqual(0.5f, slider.Position.x, 0.02f, "slider should move along frame A's x-axis");
            Assert.AreEqual(0f, slider.Position.y, 1e-3f, "prismatic joint must not allow off-axis drift");

            world.Destroy();
        }

        [Test]
        public void SphericalJoint_ConeLimitCapsSwing()
        {
            World world = World.Create(WorldDef.Default);
            Body anchor = world.CreateBody(BodyDef.Default);

            Body pendulum = CreateDynamicSphere(world, new float3(0f, -1f, 0f), 0.2f);

            SphericalJointDef def = SphericalJointDef.Default;
            def.Base.BodyIdA = anchor.Id;
            def.Base.BodyIdB = pendulum.Id;
            def.Base.LocalFrameB = new B3Transform { Position = new float3(0f, 1f, 0f), Rotation = quaternion.identity };
            SphericalJoint joint = world.CreateSphericalJoint(def);

            const float coneLimit = 0.25f;
            joint.EnableConeLimit(true);
            joint.SetConeLimit(coneLimit);

            // Push along z: the swing rotates about the x-axis, tilting frame B's z-axis and
            // opening the cone. (An x push would rotate AROUND the cone axis — twist, cone angle 0.)
            pendulum.LinearVelocity = new float3(0f, 0f, 5f);

            float maxObservedAngle = 0f;
            for (int i = 0; i < 240; i++)
            {
                world.Step(TimeStep);
                maxObservedAngle = math.max(maxObservedAngle, joint.GetConeAngle());
            }

            Assert.Greater(maxObservedAngle, 0.1f, "pendulum should actually swing");
            // Impulse-based limits overshoot transiently when hit at speed (~0.05 rad here at
            // 5 m/s) and are corrected over the following steps — allow that, but a broken limit
            // (free swing would exceed 1 rad) still fails clearly.
            Assert.LessOrEqual(maxObservedAngle, coneLimit + 0.1f, "cone limit should cap the swing angle");

            world.Destroy();
        }

        [Test]
        public void WeldJoint_KeepsBodiesRigid()
        {
            World world = World.Create(WorldDef.Default);

            Body ground = world.CreateBody(BodyDef.Default);
            BoxHull groundHull = BoxHull.Create(10f, 0.5f, 10f);
            ground.CreateHullShape(ShapeDef.Default, in groundHull);

            Body bodyA = CreateDynamicSphere(world, new float3(0f, 4f, 0f), 0.3f);
            Body bodyB = CreateDynamicSphere(world, new float3(1f, 4f, 0f), 0.3f);
            float initialDistance = math.distance(bodyA.Position, bodyB.Position);

            WeldJointDef def = WeldJointDef.Default;
            def.Base.BodyIdA = bodyA.Id;
            def.Base.BodyIdB = bodyB.Id;
            def.Base.LocalFrameB = new B3Transform { Position = new float3(-1f, 0f, 0f), Rotation = quaternion.identity };
            world.CreateWeldJoint(def);

            for (int i = 0; i < 300; i++)
            {
                world.Step(TimeStep);
            }

            Assert.AreEqual(initialDistance, math.distance(bodyA.Position, bodyB.Position), 0.03f,
                "welded pair should stay rigid through the fall and landing");
            Assert.Greater(bodyA.Position.y, 0.5f, "welded pair should rest on the ground");

            world.Destroy();
        }

        [Test]
        public void WheelJoint_SuspensionHoldsWheelWithinLimits()
        {
            World world = World.Create(WorldDef.Default);
            Body chassis = world.CreateBody(BodyDef.Default);

            Body wheel = CreateDynamicSphere(world, float3.zero, 0.3f);

            // Suspension translates along frame A's x-axis; point it down (-y) so gravity loads it.
            quaternion xDown = quaternion.RotateZ(-math.PI / 2f);
            WheelJointDef def = WheelJointDef.Default;
            def.Base.BodyIdA = chassis.Id;
            def.Base.BodyIdB = wheel.Id;
            def.Base.LocalFrameA = new B3Transform { Position = float3.zero, Rotation = xDown };
            def.Base.LocalFrameB = new B3Transform { Position = float3.zero, Rotation = xDown };
            WheelJoint joint = world.CreateWheelJoint(def);

            joint.EnableSuspensionLimit(true);
            joint.SetSuspensionLimits(-0.4f, 0.4f);

            for (int i = 0; i < 300; i++)
            {
                world.Step(TimeStep);
            }

            Assert.Greater(wheel.Position.y, -0.5f, "suspension + limit should hold the wheel, not let it fall");
            Assert.Less(wheel.Position.y, -0.05f, "gravity should sag the suspension below the chassis");
            Assert.AreEqual(0f, wheel.Position.z, 1e-3f, "wheel must not drift off the suspension axis");

            world.Destroy();
        }

        [Test]
        public void MotorJoint_SpringDrivesBodyToMovedTarget()
        {
            World world = CreateZeroGravityWorld();
            Body anchor = world.CreateBody(BodyDef.Default);
            Body body = CreateDynamicSphere(world, float3.zero);

            MotorJointDef def = MotorJointDef.Default;
            def.Base.BodyIdA = anchor.Id;
            def.Base.BodyIdB = body.Id;
            def.LinearHertz = 5f;
            def.LinearDampingRatio = 1f;
            def.MaxSpringForce = 100000f;
            MotorJoint joint = world.CreateMotorJoint(def);

            // Drag pattern: move the target frame, the spring pulls the body after it.
            Joint common = joint;
            common.SetLocalFrameA(new B3Transform { Position = new float3(2f, 0f, 0f), Rotation = quaternion.identity });

            for (int i = 0; i < 300; i++)
            {
                world.Step(TimeStep);
            }

            Assert.AreEqual(2f, body.Position.x, 0.05f, "spring should settle the body at the moved target");

            world.Destroy();
        }

        [Test]
        public void ParallelJoint_RightsTiltedBody()
        {
            World world = CreateZeroGravityWorld();
            Body anchor = world.CreateBody(BodyDef.Default);
            Body body = CreateDynamicSphere(world, new float3(0f, 2f, 0f));

            ParallelJointDef def = ParallelJointDef.Default;
            def.Base.BodyIdA = anchor.Id;
            def.Base.BodyIdB = body.Id;
            def.Hertz = 5f;
            def.DampingRatio = 1f;
            world.CreateParallelJoint(def);

            // Tilt the body, then let the spring right it.
            body.SetAngularVelocity(new float3(3f, 0f, 3f));
            for (int i = 0; i < 30; i++)
            {
                world.Step(TimeStep);
            }

            for (int i = 0; i < 400; i++)
            {
                world.Step(TimeStep);
            }

            float3 bodyZ = math.mul(body.Rotation, new float3(0f, 0f, 1f));
            Assert.Greater(math.dot(bodyZ, new float3(0f, 0f, 1f)), 0.95f,
                "parallel joint spring should re-align the body's z-axis with the anchor's");

            world.Destroy();
        }

        [Test]
        public void FilterJoint_DisablesCollisionBetweenBodies()
        {
            World world = CreateZeroGravityWorld();

            Body bodyA = CreateDynamicSphere(world, float3.zero);
            Body bodyB = CreateDynamicSphere(world, new float3(0.2f, 0f, 0f));

            FilterJointDef def = FilterJointDef.Default;
            def.Base.BodyIdA = bodyA.Id;
            def.Base.BodyIdB = bodyB.Id;
            world.CreateFilterJoint(def);

            for (int i = 0; i < 60; i++)
            {
                world.Step(TimeStep);
            }

            Assert.Less(math.distance(bodyA.Position, bodyB.Position), 0.3f,
                "deeply overlapping spheres must not be pushed apart when filter-jointed");

            world.Destroy();
        }
    }
}
