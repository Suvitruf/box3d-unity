using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Box3d.Tests
{
    /// <summary>Joint def layout guards + defaults round-trip + end-to-end joint simulation.</summary>
    public class JointTests
    {
        [Test]
        public void JointDefSizes_MatchNativeLayout()
        {
            Assert.AreEqual(112, UnsafeUtility.SizeOf<JointDefBase>());
            Assert.AreEqual(160, UnsafeUtility.SizeOf<DistanceJointDef>());
            Assert.AreEqual(168, UnsafeUtility.SizeOf<MotorJointDef>());
            Assert.AreEqual(112, UnsafeUtility.SizeOf<FilterJointDef>());
            Assert.AreEqual(128, UnsafeUtility.SizeOf<ParallelJointDef>());
            Assert.AreEqual(152, UnsafeUtility.SizeOf<PrismaticJointDef>());
            Assert.AreEqual(152, UnsafeUtility.SizeOf<RevoluteJointDef>());
            Assert.AreEqual(184, UnsafeUtility.SizeOf<SphericalJointDef>());
            Assert.AreEqual(128, UnsafeUtility.SizeOf<WeldJointDef>());
            Assert.AreEqual(184, UnsafeUtility.SizeOf<WheelJointDef>());
        }

        [Test]
        public void JointDefDefaults_MatchNative()
        {
            // Base defaults (via revolute): identity frames, FLT_MAX thresholds, hertz 60,
            // damping 2, drawScale 1, cookie set. Values from box3d src/joint.c.
            RevoluteJointDef revolute = RevoluteJointDef.Default;
            Assert.AreEqual(1f, revolute.Base.LocalFrameA.Rotation.value.w);
            Assert.AreEqual(1f, revolute.Base.LocalFrameB.Rotation.value.w);
            Assert.AreEqual(float.MaxValue, revolute.Base.ForceThreshold);
            Assert.AreEqual(float.MaxValue, revolute.Base.TorqueThreshold);
            Assert.AreEqual(60f, revolute.Base.ConstraintHertz);
            Assert.AreEqual(2f, revolute.Base.ConstraintDampingRatio);
            Assert.AreEqual(1f, revolute.Base.DrawScale);
            Assert.AreNotEqual(0, revolute.Base.InternalValue);
            Assert.IsFalse(revolute.EnableLimit);

            DistanceJointDef distance = DistanceJointDef.Default;
            Assert.AreEqual(1f, distance.Length);
            Assert.AreEqual(float.MinValue, distance.LowerSpringForce);
            Assert.AreEqual(float.MaxValue, distance.UpperSpringForce);
            Assert.AreEqual(100000f, distance.MaxLength, 1f);

            WheelJointDef wheel = WheelJointDef.Default;
            Assert.IsTrue(wheel.EnableSuspensionSpring);
            Assert.AreEqual(1f, wheel.SuspensionHertz);
            Assert.AreEqual(0.7f, wheel.SuspensionDampingRatio, 1e-6f);
            Assert.AreEqual(1f, wheel.SteeringHertz);
            Assert.IsFalse(wheel.EnableSteering);

            SphericalJointDef spherical = SphericalJointDef.Default;
            Assert.AreEqual(1f, spherical.TargetRotation.value.w, "target rotation should be identity");

            ParallelJointDef parallel = ParallelJointDef.Default;
            Assert.AreEqual(1f, parallel.Hertz);
            Assert.AreEqual(float.MaxValue, parallel.MaxTorque);
        }

        [Test]
        public void DistanceJoint_HoldsBodiesAtRestLength()
        {
            World world = World.Create(WorldDef.Default);

            Body anchor = world.CreateBody(BodyDef.Default);

            BodyDef hangingDef = BodyDef.Default;
            hangingDef.Type = BodyType.Dynamic;
            hangingDef.Position = new float3(0f, -1f, 0f);
            Body hanging = world.CreateBody(hangingDef);
            hanging.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.1f });

            DistanceJointDef jointDef = DistanceJointDef.Default;
            jointDef.Base.BodyIdA = anchor.Id;
            jointDef.Base.BodyIdB = hanging.Id;
            jointDef.Length = 1f;
            DistanceJoint joint = world.CreateDistanceJoint(jointDef);
            Assert.IsTrue(((Joint)joint).IsValid);

            for (int i = 0; i < 300; i++)
            {
                world.Step(1f / 60f);
            }

            float distance = math.distance(anchor.Position, hanging.Position);
            Assert.AreEqual(1f, distance, 0.01f, "hanging body should stay at the joint rest length");

            world.Destroy();
        }

        [Test]
        public void TypedJointAccessors_RoundTrip()
        {
            World world = World.Create(WorldDef.Default);

            Body bodyA = world.CreateBody(BodyDef.Default);
            BodyDef dynamicDef = BodyDef.Default;
            dynamicDef.Type = BodyType.Dynamic;
            dynamicDef.Position = new float3(1f, 0f, 0f);
            Body bodyB = world.CreateBody(dynamicDef);
            bodyB.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.1f });

            RevoluteJointDef jointDef = RevoluteJointDef.Default;
            jointDef.Base.BodyIdA = bodyA.Id;
            jointDef.Base.BodyIdB = bodyB.Id;
            RevoluteJoint revolute = world.CreateRevoluteJoint(jointDef);

            revolute.SetSpringHertz(5f);
            Assert.AreEqual(5f, revolute.GetSpringHertz());
            revolute.EnableLimit(true);
            Assert.IsTrue(revolute.IsLimitEnabled());

            Joint asJoint = revolute;
            Assert.IsTrue(asJoint.IsValid);
            asJoint.WakeBodies();

            world.Destroy();
        }
    }
}
