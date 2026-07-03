using NUnit.Framework;
using Unity.Mathematics;

namespace Box3d.Tests
{
    /// <summary>Small-feature pass: wind, conveyor surface material, motion locks.</summary>
    public class FeatureTests
    {
        private const float TimeStep = 1f / 60f;

        [Test]
        public void ApplyWind_AcceleratesBody()
        {
            WorldDef worldDef = WorldDef.Default;
            worldDef.Gravity = float3.zero;
            worldDef.EnableSleep = false;
            World world = World.Create(worldDef);

            BodyDef bodyDef = BodyDef.Default;
            bodyDef.Type = BodyType.Dynamic;
            Body body = world.CreateBody(bodyDef);
            // Wind force is aerodynamic (0.5·ρair·area·v²) — a small sphere has a favorable
            // area/mass ratio; a big water-density ball barely feels an 8 m/s breeze.
            Shape shape = body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.1f });

            for (int i = 0; i < 120; i++)
            {
                shape.ApplyWind(new float3(8f, 0f, 0f), drag: 1f, lift: 0f, maxSpeed: 10f, wake: true);
                world.Step(TimeStep);
            }

            Assert.Greater(body.LinearVelocity.x, 0.2f, "steady wind should accelerate the sphere downwind");
            Assert.AreEqual(0f, body.LinearVelocity.y, 1e-3f, "no lift and no gravity — no vertical drift");

            world.Destroy();
        }

        [Test]
        public void ConveyorMaterial_MovesRestingBox()
        {
            World world = World.Create(WorldDef.Default);

            Body ground = world.CreateBody(BodyDef.Default);
            BoxHull groundHull = BoxHull.Create(10f, 0.5f, 10f);
            ShapeDef beltDef = ShapeDef.Default;
            beltDef.BaseMaterial.TangentVelocity = new float3(2f, 0f, 0f);
            beltDef.BaseMaterial.Friction = 0.9f;
            ground.CreateHullShape(beltDef, in groundHull);

            BodyDef boxDef = BodyDef.Default;
            boxDef.Type = BodyType.Dynamic;
            boxDef.Position = new float3(-4f, 1.2f, 0f);
            Body box = world.CreateBody(boxDef);
            BoxHull boxHull = BoxHull.CreateCube(0.4f);
            box.CreateHullShape(ShapeDef.Default, in boxHull);

            for (int i = 0; i < 300; i++)
            {
                world.Step(TimeStep);
            }

            Assert.Greater(box.Position.x, -3f, "conveyor tangent velocity should carry the box along +x");
            Assert.Greater(box.Position.y, 0.5f, "box should stay on the belt");

            world.Destroy();
        }

        [Test]
        public void MotionLocks_FreezeSelectedAxes()
        {
            World world = World.Create(WorldDef.Default);

            BodyDef lockedDef = BodyDef.Default;
            lockedDef.Type = BodyType.Dynamic;
            lockedDef.Position = new float3(0f, 3f, 0f);
            lockedDef.MotionLocks.LinearY = true;
            lockedDef.MotionLocks.AngularX = true;
            lockedDef.MotionLocks.AngularZ = true;
            Body body = world.CreateBody(lockedDef);
            body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });

            body.SetAngularVelocity(new float3(3f, 2f, 3f));
            for (int i = 0; i < 120; i++)
            {
                world.Step(TimeStep);
            }

            Assert.AreEqual(3f, body.Position.y, 1e-3f, "locked linear Y must ignore gravity entirely");
            float3 angular = body.GetAngularVelocity();
            Assert.AreEqual(0f, angular.x, 1e-3f, "locked angular X must not rotate");
            Assert.AreEqual(0f, angular.z, 1e-3f, "locked angular Z must not rotate");
            Assert.Greater(angular.y, 1f, "unlocked angular Y should keep spinning");

            world.Destroy();
        }
    }
}
