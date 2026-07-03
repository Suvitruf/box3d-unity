using NUnit.Framework;
using Unity.Mathematics;

namespace Box3d.Tests
{
    /// <summary>Worker-thread callback registration and behavior. Tests run with the default
    /// single worker, so callbacks fire on the stepping thread — behavior is identical, threading
    /// rules are documented on the delegates.</summary>
    public class CallbackTests
    {
        private const float TimeStep = 1f / 60f;

        private static int _filterCalls;
        private static int _preSolveCalls;
        private static int _frictionCalls;
        private static int _restitutionCalls;

        [Test]
        public void CustomFilter_ReturningFalse_PreventsCollision()
        {
            WorldDef worldDef = WorldDef.Default;
            worldDef.Gravity = float3.zero;
            World world = World.Create(worldDef);

            _filterCalls = 0;
            world.SetCustomFilterCallback((shapeA, shapeB) =>
            {
                _filterCalls++;
                return false; // never collide
            });

            ShapeDef shapeDef = ShapeDef.Default;
            shapeDef.EnableCustomFiltering = true;
            Body bodyA = CreateDynamicSphere(world, float3.zero, shapeDef);
            Body bodyB = CreateDynamicSphere(world, new float3(0.2f, 0f, 0f), shapeDef);

            for (int i = 0; i < 60; i++)
            {
                world.Step(TimeStep);
            }

            Assert.Greater(_filterCalls, 0, "custom filter should have been consulted");
            Assert.Less(math.distance(bodyA.Position, bodyB.Position), 0.3f,
                "filtered-out pair must not be pushed apart");

            world.Destroy();
        }

        [Test]
        public void PreSolve_ReturningFalse_DisablesContact()
        {
            World world = World.Create(WorldDef.Default);

            Body ground = world.CreateBody(BodyDef.Default);
            BoxHull hull = BoxHull.Create(10f, 0.5f, 10f);
            ground.CreateHullShape(ShapeDef.Default, in hull);

            _preSolveCalls = 0;
            world.SetPreSolveCallback((shapeA, shapeB, point, normal) =>
            {
                _preSolveCalls++;
                return false; // ghost through everything
            });

            BodyDef sphereDef = BodyDef.Default;
            sphereDef.Type = BodyType.Dynamic;
            sphereDef.Position = new float3(0f, 2f, 0f);
            Body sphere = world.CreateBody(sphereDef);
            ShapeDef shapeDef = ShapeDef.Default;
            shapeDef.EnablePreSolveEvents = true;
            sphere.CreateSphereShape(shapeDef, new Sphere { Radius = 0.5f });

            for (int i = 0; i < 300; i++)
            {
                world.Step(TimeStep);
            }

            Assert.Greater(_preSolveCalls, 0, "pre-solve callback should have been invoked");
            Assert.Less(sphere.Position.y, -2f, "disabled contacts should let the sphere fall through the ground");

            world.Destroy();
        }

        [Test]
        public void MaterialMixCallbacks_AreInvokedWithMaterialIds()
        {
            WorldDef worldDef = WorldDef.Default;
            worldDef.EnableSleep = false;
            World world = World.Create(worldDef);

            _frictionCalls = 0;
            _restitutionCalls = 0;
            ulong seenFrictionId = 0;
            world.SetFrictionCallback((a, idA, b, idB) =>
            {
                _frictionCalls++;
                seenFrictionId = math.max(idA, idB);
                return math.sqrt(a * b);
            });
            world.SetRestitutionCallback((a, idA, b, idB) =>
            {
                _restitutionCalls++;
                return math.max(a, b);
            });

            Body ground = world.CreateBody(BodyDef.Default);
            BoxHull hull = BoxHull.Create(10f, 0.5f, 10f);
            ground.CreateHullShape(ShapeDef.Default, in hull);

            BodyDef sphereDef = BodyDef.Default;
            sphereDef.Type = BodyType.Dynamic;
            sphereDef.Position = new float3(0f, 2f, 0f);
            sphereDef.LinearVelocity = new float3(2f, 0f, 0f);
            Body sphere = world.CreateBody(sphereDef);
            ShapeDef shapeDef = ShapeDef.Default;
            shapeDef.BaseMaterial.Restitution = 0.5f;
            shapeDef.BaseMaterial.UserMaterialId = 77;
            sphere.CreateSphereShape(shapeDef, new Sphere { Radius = 0.5f });

            for (int i = 0; i < 180; i++)
            {
                world.Step(TimeStep);
            }

            Assert.Greater(_frictionCalls, 0, "friction mixer should be invoked for the rolling contact");
            Assert.Greater(_restitutionCalls, 0, "restitution mixer should be invoked for the bouncing contact");
            Assert.AreEqual(77ul, seenFrictionId, "user material id should flow through to the mixer");

            world.SetFrictionCallback(null);
            world.SetRestitutionCallback(null);
            world.Destroy();
        }

        private static Body CreateDynamicSphere(World world, float3 position, in ShapeDef shapeDef)
        {
            BodyDef def = BodyDef.Default;
            def.Type = BodyType.Dynamic;
            def.Position = position;
            Body body = world.CreateBody(def);
            body.CreateSphereShape(shapeDef, new Sphere { Radius = 0.5f });
            return body;
        }
    }
}
