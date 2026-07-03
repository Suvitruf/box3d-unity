using NUnit.Framework;
using Unity.Mathematics;

namespace Box3d.Tests
{
    /// <summary>Debug-draw bridge: native draw callbacks must reach the managed trampolines.</summary>
    public class DebugDrawTests
    {
        [Test]
        public void DrawDebug_InvokesBridgeCallbacks()
        {
            World world = World.Create(WorldDef.Default);

            BodyDef bodyDef = BodyDef.Default;
            bodyDef.Type = BodyType.Dynamic;
            bodyDef.Position = new float3(0f, 1f, 0f);
            Body body = world.CreateBody(bodyDef);
            body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });
            body.CreateCapsuleShape(ShapeDef.Default, new Capsule
            {
                Center1 = new float3(0f, 0.5f, 0f),
                Center2 = new float3(0f, 1f, 0f),
                Radius = 0.2f,
            });
            world.Step(1f / 60f);

            world.DrawDebug(DebugDrawFlags.Shapes | DebugDrawFlags.Bounds | DebugDrawFlags.Mass);

            Assert.Greater(DebugDrawBridge.DrawCallCount, 0,
                "drawing a world with shapes should invoke the managed draw trampolines");

            world.Destroy();
        }
    }
}
