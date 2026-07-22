using NUnit.Framework;
using Unity.Mathematics;

namespace Box3D.Tests
{
    /// <summary>End-to-end simulation smoke tests: the whole interop slice has to work for these
    /// to pass — defs, world/body/shape creation, stepping, transform getters, events.</summary>
    public class SimulationTests
    {
        private const float TimeStep = 1f / 60f;

        [Test]
        public void FallingSphere_RestsOnGroundHull()
        {
            World world = World.Create(WorldDef.Default);
            Assert.IsTrue(world.IsValid);

            // Static ground: a 20×1×20 box whose top face sits at y = 0.5.
            Body ground = world.CreateBody(BodyDef.Default);
            BoxHull groundHull = BoxHull.Create(10f, 0.5f, 10f);
            ground.CreateHullShape(ShapeDef.Default, in groundHull);

            // Dynamic unit-radius sphere dropped from y = 5.
            BodyDef sphereBodyDef = BodyDef.Default;
            sphereBodyDef.Type = BodyType.Dynamic;
            sphereBodyDef.Position = new float3(0f, 5f, 0f);
            Body sphereBody = world.CreateBody(sphereBodyDef);
            Shape sphereShape = sphereBody.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });
            Assert.IsTrue(sphereShape.IsValid);

            for (int i = 0; i < 300; i++)
            {
                world.Step(TimeStep);
            }

            // Resting height = ground top (0.5) + sphere radius (0.5), within contact slop.
            float3 position = sphereBody.Position;
            Assert.AreEqual(1f, position.y, 0.02f, "sphere should rest on the ground");
            Assert.AreEqual(0f, position.x, 1e-3f);
            Assert.AreEqual(0f, position.z, 1e-3f);
            Assert.IsFalse(sphereBody.IsAwake, "sphere should have fallen asleep at rest");

            world.Destroy();
            Assert.IsFalse(world.IsValid);
        }

        [Test]
        public void BodyMoveEvents_ReportMovingBody()
        {
            World world = World.Create(WorldDef.Default);

            BodyDef bodyDef = BodyDef.Default;
            bodyDef.Type = BodyType.Dynamic;
            bodyDef.Position = new float3(0f, 10f, 0f);
            bodyDef.UserData = (System.IntPtr)42;
            Body body = world.CreateBody(bodyDef);
            body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });

            world.Step(TimeStep);

            var events = world.GetBodyMoveEvents();
            Assert.AreEqual(1, events.Length, "one falling body should produce one move event");
            Assert.IsTrue(events[0].BodyId.Equals(body.Id));
            Assert.AreEqual((System.IntPtr)42, events[0].UserData, "userData should round-trip");
            Assert.Less(events[0].Transform.Position.y, 10f, "body should have fallen");

            world.Destroy();
        }

        [Test]
        public void DestroyedBody_IdGoesStale()
        {
            World world = World.Create(WorldDef.Default);

            Body body = world.CreateBody(BodyDef.Default);
            BodyId idBefore = body.Id;
            Assert.IsTrue(UnsafeIsValid(idBefore));

            body.Destroy();
            Assert.IsFalse(UnsafeIsValid(idBefore), "stale id must fail validation");

            world.Destroy();
        }

        private static bool UnsafeIsValid(BodyId id)
        {
            return new Body { Id = id }.IsValid;
        }

        [Test]
        public void Ids_ResolveBackToWrappers()
        {
            World world = World.Create(WorldDef.Default);
            World other = World.Create(WorldDef.Default);

            BodyDef bodyDef = BodyDef.Default;
            bodyDef.Type = BodyType.Dynamic;
            Body body = world.CreateBody(bodyDef);
            Shape shape = body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });

            // Unchecked wrapping: an id (as events deliver them) round-trips to a working wrapper.
            Assert.IsTrue(new Body(body.Id).IsValid);
            Assert.AreEqual(body, new Shape(shape.Id).GetBody());

            // Validated resolution: owning world succeeds, another world rejects the same id.
            Assert.IsTrue(world.TryGetBody(body.Id, out Body resolvedBody));
            Assert.AreEqual(body, resolvedBody);
            Assert.IsTrue(world.TryGetShape(shape.Id, out Shape resolvedShape));
            Assert.AreEqual(shape, resolvedShape);
            Assert.IsFalse(other.TryGetBody(body.Id, out _), "a foreign world must not resolve the id");

            // Stale ids fail both paths after destroy.
            BodyId stale = body.Id;
            body.Destroy();
            Assert.IsFalse(new Body(stale).IsValid);
            Assert.IsFalse(world.TryGetBody(stale, out _));

            other.Destroy();
            world.Destroy();
        }
    }
}
