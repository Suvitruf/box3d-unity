using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Box3d.Tests
{
    /// <summary>Event struct layout guards + behavioral tests proving events flow end-to-end
    /// (including the opt-in flags on ShapeDef, which are all false by default).</summary>
    public class EventTests
    {
        private const float TimeStep = 1f / 60f;

        [Test]
        public void EventStructSizes_MatchNativeLayout()
        {
            Assert.AreEqual(28, UnsafeUtility.SizeOf<ContactBeginTouchEvent>());
            Assert.AreEqual(28, UnsafeUtility.SizeOf<ContactEndTouchEvent>());
            Assert.AreEqual(72, UnsafeUtility.SizeOf<ContactHitEvent>());
            Assert.AreEqual(16, UnsafeUtility.SizeOf<SensorBeginTouchEvent>());
            Assert.AreEqual(16, UnsafeUtility.SizeOf<SensorEndTouchEvent>());
            Assert.AreEqual(16, UnsafeUtility.SizeOf<JointEvent>());
        }

        [Test]
        public void RawContainerSizes_MatchNativeLayout()
        {
            Assert.AreEqual(40, UnsafeUtility.SizeOf<ContactEventsRaw>());
            Assert.AreEqual(24, UnsafeUtility.SizeOf<SensorEventsRaw>());
            Assert.AreEqual(16, UnsafeUtility.SizeOf<JointEventsRaw>());
            Assert.AreEqual(16, UnsafeUtility.SizeOf<BodyEventsRaw>());
        }

        [Test]
        public void ContactBeginAndHitEvents_FireOnImpact()
        {
            World world = World.Create(WorldDef.Default);

            Body ground = world.CreateBody(BodyDef.Default);
            BoxHull groundHull = BoxHull.Create(10f, 0.5f, 10f);
            ShapeDef groundShapeDef = ShapeDef.Default;
            groundShapeDef.EnableContactEvents = true;
            Shape groundShape = ground.CreateHullShape(groundShapeDef, in groundHull);

            BodyDef sphereDef = BodyDef.Default;
            sphereDef.Type = BodyType.Dynamic;
            sphereDef.Position = new float3(0f, 5f, 0f);
            Body sphereBody = world.CreateBody(sphereDef);
            ShapeDef sphereShapeDef = ShapeDef.Default;
            sphereShapeDef.EnableContactEvents = true;
            sphereShapeDef.EnableHitEvents = true;
            Shape sphereShape = sphereBody.CreateSphereShape(sphereShapeDef, new Sphere { Radius = 0.5f });

            bool sawBegin = false;
            bool sawHit = false;
            float hitSpeed = 0f;

            for (int i = 0; i < 120 && !(sawBegin && sawHit); i++)
            {
                world.Step(TimeStep);
                ContactEvents events = world.GetContactEvents();

                foreach (ContactBeginTouchEvent begin in events.Begin)
                {
                    bool pairMatches =
                        (begin.ShapeIdA.Equals(groundShape.Id) && begin.ShapeIdB.Equals(sphereShape.Id)) ||
                        (begin.ShapeIdA.Equals(sphereShape.Id) && begin.ShapeIdB.Equals(groundShape.Id));
                    if (pairMatches) sawBegin = true;
                }

                foreach (ContactHitEvent hit in events.Hit)
                {
                    sawHit = true;
                    hitSpeed = hit.ApproachSpeed;
                }
            }

            Assert.IsTrue(sawBegin, "contact begin event should fire for the sphere/ground pair");
            Assert.IsTrue(sawHit, "hit event should fire — impact speed is well above the 1 m/s threshold");
            Assert.Greater(hitSpeed, 1f);

            world.Destroy();
        }

        [Test]
        public void SensorEvents_FireOnEnterAndExit()
        {
            World world = World.Create(WorldDef.Default);

            // Sensor volume: a static 1×1×1 box at the origin.
            Body sensorBody = world.CreateBody(BodyDef.Default);
            BoxHull sensorHull = BoxHull.Create(0.5f, 0.5f, 0.5f);
            ShapeDef sensorShapeDef = ShapeDef.Default;
            sensorShapeDef.IsSensor = true;
            sensorShapeDef.EnableSensorEvents = true;
            Shape sensorShape = sensorBody.CreateHullShape(sensorShapeDef, in sensorHull);

            // Visitor: a small sphere falling straight through the volume.
            BodyDef visitorDef = BodyDef.Default;
            visitorDef.Type = BodyType.Dynamic;
            visitorDef.Position = new float3(0f, 3f, 0f);
            Body visitor = world.CreateBody(visitorDef);
            ShapeDef visitorShapeDef = ShapeDef.Default;
            visitorShapeDef.EnableSensorEvents = true;
            Shape visitorShape = visitor.CreateSphereShape(visitorShapeDef, new Sphere { Radius = 0.1f });

            bool sawBegin = false;
            bool sawEnd = false;

            for (int i = 0; i < 300 && !sawEnd; i++)
            {
                world.Step(TimeStep);
                SensorEvents events = world.GetSensorEvents();

                foreach (SensorBeginTouchEvent begin in events.Begin)
                {
                    if (begin.SensorShapeId.Equals(sensorShape.Id) && begin.VisitorShapeId.Equals(visitorShape.Id))
                    {
                        sawBegin = true;
                    }
                }

                foreach (SensorEndTouchEvent end in events.End)
                {
                    if (end.SensorShapeId.Equals(sensorShape.Id)) sawEnd = true;
                }
            }

            Assert.IsTrue(sawBegin, "sensor begin event should fire when the sphere enters the volume");
            Assert.IsTrue(sawEnd, "sensor end event should fire when the sphere falls out of the volume");

            world.Destroy();
        }

        [Test]
        public void JointEvents_EmptyWithoutThresholdExceeded()
        {
            World world = World.Create(WorldDef.Default);
            world.Step(TimeStep);
            Assert.AreEqual(0, world.GetJointEvents().Length);
            world.Destroy();
        }
    }
}
