using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using EcsWorld = Unity.Entities.World;

namespace Box3D.Entities.Tests
{
    /// <summary>End-to-end tests for the ECS layer: a manually driven ECS world running the Box3D
    /// systems in their production order (world → lifecycle → step → write-back).</summary>
    public class EntitiesIntegrationTests
    {
        private const float TimeStep = 1f / 60f;

        private EcsWorld _ecsWorld;
        private EntityManager _entityManager;
        private SystemHandle _worldSystem;
        private SystemHandle _lifecycleSystem;
        private SystemHandle _stepSystem;
        private SystemHandle _writeBackSystem;

        [SetUp]
        public void SetUp()
        {
            _ecsWorld = new EcsWorld("Box3DEntitiesTests");
            _entityManager = _ecsWorld.EntityManager;
            _worldSystem = _ecsWorld.CreateSystem<Box3DWorldSystem>();
            _lifecycleSystem = _ecsWorld.CreateSystem<Box3DBodyLifecycleSystem>();
            _stepSystem = _ecsWorld.CreateSystem<Box3DStepSystem>();
            _writeBackSystem = _ecsWorld.CreateSystem<Box3DWriteBackSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            _ecsWorld.Dispose(); // Box3DWorldSystem.OnDestroy destroys the native world
        }

        [Test]
        public void Lifecycle_CreatesNativeBody_AndDestroysItWithTheEntity()
        {
            Entity entity = CreateBodyEntity(Box3DBodyDefinition.Dynamic, Box3DShapeDefinition.Sphere(0.5f), new float3(0f, 5f, 0f));
            Tick();

            Body body = _entityManager.GetComponentData<Box3DBodyRef>(entity).Value;
            Assert.IsTrue(body.IsValid, "lifecycle system should create a native body");
            Assert.AreEqual(BodyType.Dynamic, body.Type);
            Assert.AreEqual(5f, ((float3)body.Position).y, 1e-4f, "body should spawn at the entity's LocalTransform");
            Assert.AreEqual(1, body.GetShapeCount());

            _entityManager.DestroyEntity(entity);
            Assert.IsTrue(_entityManager.Exists(entity), "cleanup component should keep the entity alive until the native body is destroyed");

            Tick();
            Assert.IsFalse(body.IsValid, "native body should be destroyed after the entity is gone");
            Assert.IsFalse(_entityManager.Exists(entity), "entity should be fully released once the cleanup component is removed");
        }

        [Test]
        public void Step_FallingBody_LandsOnGround_AndSyncsLocalTransform()
        {
            CreateBodyEntity(Box3DBodyDefinition.Static, Box3DShapeDefinition.Box(new float3(10f, 0.5f, 10f)), float3.zero);
            Entity ball = CreateBodyEntity(Box3DBodyDefinition.Dynamic, Box3DShapeDefinition.Sphere(0.5f), new float3(0f, 3f, 0f));
            Tick();
            SetFixedTimeStep(TimeStep);

            for (int i = 0; i < 240; i++)
            {
                Tick();
            }

            LocalTransform transform = _entityManager.GetComponentData<LocalTransform>(ball);
            Assert.AreEqual(1f, transform.Position.y, 0.1f, "sphere (r=0.5) should rest on the ground box (top y=0.5)");

            Body body = _entityManager.GetComponentData<Box3DBodyRef>(ball).Value;
            float3 bodyPosition = body.Position;
            Assert.AreEqual(bodyPosition.y, transform.Position.y, 1e-3f, "LocalTransform should mirror the native body");
        }

        [Test]
        public void WriteBack_RoutesMoveEvents_ToTheirOwnEntities()
        {
            CreateBodyEntity(Box3DBodyDefinition.Static, Box3DShapeDefinition.Box(new float3(10f, 0.5f, 10f)), float3.zero);
            Entity left = CreateBodyEntity(Box3DBodyDefinition.Dynamic, Box3DShapeDefinition.Sphere(0.5f), new float3(-2f, 3f, 0f));
            Entity right = CreateBodyEntity(Box3DBodyDefinition.Dynamic, Box3DShapeDefinition.Sphere(0.5f), new float3(2f, 3f, 0f));
            Tick();
            SetFixedTimeStep(TimeStep);

            for (int i = 0; i < 60; i++)
            {
                Tick();
            }

            LocalTransform leftTransform = _entityManager.GetComponentData<LocalTransform>(left);
            LocalTransform rightTransform = _entityManager.GetComponentData<LocalTransform>(right);
            Assert.AreEqual(-2f, leftTransform.Position.x, 0.05f, "left sphere's events should land on the left entity");
            Assert.AreEqual(2f, rightTransform.Position.x, 0.05f, "right sphere's events should land on the right entity");
            Assert.Less(leftTransform.Position.y, 3f, "both spheres should have fallen");
            Assert.Less(rightTransform.Position.y, 3f, "both spheres should have fallen");
        }

        [Test]
        public void WriteBack_TouchesOnlyPositionAndRotation_ScaleSurvives()
        {
            CreateBodyEntity(Box3DBodyDefinition.Static, Box3DShapeDefinition.Box(new float3(10f, 0.5f, 10f)), float3.zero);
            Entity ball = CreateBodyEntity(Box3DBodyDefinition.Dynamic, Box3DShapeDefinition.Sphere(0.5f), new float3(0f, 3f, 0f), 2f);
            Tick();
            SetFixedTimeStep(TimeStep);

            for (int i = 0; i < 30; i++)
            {
                Tick();
            }

            LocalTransform transform = _entityManager.GetComponentData<LocalTransform>(ball);
            Assert.Less(transform.Position.y, 3f, "sphere should have fallen");
            Assert.AreEqual(2f, transform.Scale, "write-back must not clobber the entity's scale");
        }

        [Test]
        public void StaticBody_ProducesNoMoveEvents_TransformUntouched()
        {
            Entity ground = CreateBodyEntity(Box3DBodyDefinition.Static, Box3DShapeDefinition.Box(new float3(10f, 0.5f, 10f)), new float3(0f, -1f, 0f));
            Entity ball = CreateBodyEntity(Box3DBodyDefinition.Dynamic, Box3DShapeDefinition.Sphere(0.5f), new float3(0f, 3f, 0f));
            Tick();
            SetFixedTimeStep(TimeStep);

            for (int i = 0; i < 30; i++)
            {
                Tick();
            }

            LocalTransform groundTransform = _entityManager.GetComponentData<LocalTransform>(ground);
            Assert.AreEqual(-1f, groundTransform.Position.y, 0f, "static body must not be moved by write-back");
            Assert.Less(_entityManager.GetComponentData<LocalTransform>(ball).Position.y, 3f, "the dynamic body was simulating meanwhile");
        }

        [Test]
        public void BurstCompiledJob_StepsNativeWorld()
        {
            CreateBodyEntity(Box3DBodyDefinition.Static, Box3DShapeDefinition.Box(new float3(10f, 0.5f, 10f)), float3.zero);
            Entity ball = CreateBodyEntity(Box3DBodyDefinition.Dynamic, Box3DShapeDefinition.Sphere(0.5f), new float3(0f, 3f, 0f));
            Tick();
            Body body = _entityManager.GetComponentData<Box3DBodyRef>(ball).Value;

            using var ranManaged = new NativeReference<bool>(Allocator.TempJob);
            new StepJob
            {
                PhysicsWorld = GetPhysicsWorld(),
                TimeStep = TimeStep,
                Steps = 60,
                RanManaged = ranManaged,
            }.Run();

            Assert.IsFalse(ranManaged.Value, "job fell back to managed — Burst failed to compile the P/Invoke call");
            Assert.Less(((float3)body.Position).y, 2.5f, "Burst-driven steps should have made the sphere fall");
        }

        [Test]
        public void BurstCompiledJob_CastsRayIntoNativeWorld()
        {
            CreateBodyEntity(Box3DBodyDefinition.Static, Box3DShapeDefinition.Box(new float3(10f, 0.5f, 10f)), float3.zero);
            Tick();

            using var result = new NativeReference<RayResult>(Allocator.TempJob);
            using var ranManaged = new NativeReference<bool>(Allocator.TempJob);
            new RaycastJob
            {
                PhysicsWorld = GetPhysicsWorld(),
                Filter = QueryFilter.Default, // filled managed-side: the Default factory extern is not Burst-callable
                Result = result,
                RanManaged = ranManaged,
            }.Run();

            Assert.IsFalse(ranManaged.Value, "job fell back to managed — Burst failed to compile the glue P/Invoke");
            Assert.IsTrue((bool)result.Value.Hit, "ray straight down should hit the ground box");
            Assert.AreEqual(0.5f, ((float3)result.Value.Point).y, 1e-3f, "hit should be on the box top");
        }

        [Test]
        public void BurstCompiledJob_DrivesBodyVelocityAndImpulse()
        {
            Entity ball = CreateBodyEntity(Box3DBodyDefinition.Dynamic, Box3DShapeDefinition.Sphere(0.5f), new float3(0f, 50f, 0f));
            Tick();
            Body body = _entityManager.GetComponentData<Box3DBodyRef>(ball).Value;

            using var ranManaged = new NativeReference<bool>(Allocator.TempJob);
            using var finalPosition = new NativeReference<float3>(Allocator.TempJob);
            new VelocityDriveJob
            {
                PhysicsWorld = GetPhysicsWorld(),
                Body = body,
                TimeStep = TimeStep,
                RanManaged = ranManaged,
                FinalPosition = finalPosition,
            }.Run();

            Assert.IsFalse(ranManaged.Value, "job fell back to managed — Burst failed to compile the glue P/Invoke");
            Assert.Greater(finalPosition.Value.x, 2f, "velocity set from Burst should have carried the body along +X");
            Assert.Greater(finalPosition.Value.z, 0.5f, "impulse applied from Burst should have pushed the body along +Z");
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct StepJob : IJob
        {
            public World PhysicsWorld;
            public float TimeStep;
            public int Steps;
            public NativeReference<bool> RanManaged;

            public void Execute()
            {
                MarkManagedFallback(RanManaged);
                for (int i = 0; i < Steps; i++)
                {
                    PhysicsWorld.Step(TimeStep);
                }
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct RaycastJob : IJob
        {
            public World PhysicsWorld;

            // QueryFilter carries a debug-name pointer; it's built managed-side and read-only here.
            [NativeDisableUnsafePtrRestriction] public QueryFilter Filter;
            public NativeReference<RayResult> Result;
            public NativeReference<bool> RanManaged;

            public void Execute()
            {
                MarkManagedFallback(RanManaged);
                PhysicsWorld.CastRayClosest(new B3Pos(0f, 5f, 0f), new float3(0f, -10f, 0f), Filter, out RayResult result);
                Result.Value = result;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct VelocityDriveJob : IJob
        {
            public World PhysicsWorld;
            public Body Body;
            public float TimeStep;
            public NativeReference<bool> RanManaged;
            public NativeReference<float3> FinalPosition;

            public void Execute()
            {
                MarkManagedFallback(RanManaged);
                Body body = Body;
                body.LinearVelocity = new float3(5f, 0f, 0f);
                // Default density is 1000 kg/m³ — the r=0.5 sphere weighs ~524 kg, so size the
                // impulse for a clearly visible push (~2 m/s).
                float3 impulse = new float3(0f, 0f, 1000f);
                body.ApplyLinearImpulseToCenter(in impulse, true);
                for (int i = 0; i < 60; i++)
                {
                    PhysicsWorld.Step(TimeStep);
                }
                body.GetTransform(out B3WorldTransform transform);
                FinalPosition.Value = transform.Position;
            }
        }

        [BurstDiscard]
        private static void MarkManagedFallback(NativeReference<bool> flag)
        {
            flag.Value = true;
        }

        private void Tick()
        {
            _worldSystem.Update(_ecsWorld.Unmanaged);
            _lifecycleSystem.Update(_ecsWorld.Unmanaged);
            _stepSystem.Update(_ecsWorld.Unmanaged);
            _writeBackSystem.Update(_ecsWorld.Unmanaged);
        }

        private Entity CreateBodyEntity(Box3DBodyDefinition body, Box3DShapeDefinition shape, float3 position, float scale = 1f)
        {
            Entity entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(entity, body);
            _entityManager.AddComponentData(entity, shape);
            _entityManager.AddComponentData(entity, LocalTransform.FromPositionRotationScale(position, quaternion.identity, scale));
            return entity;
        }

        private void SetFixedTimeStep(float timeStep)
        {
            using EntityQuery query = _entityManager.CreateEntityQuery(typeof(Box3DWorldSingleton));
            Entity singletonEntity = query.GetSingletonEntity();
            Box3DWorldSingleton singleton = _entityManager.GetComponentData<Box3DWorldSingleton>(singletonEntity);
            singleton.TimeStep = timeStep;
            _entityManager.SetComponentData(singletonEntity, singleton);
        }

        private World GetPhysicsWorld()
        {
            using EntityQuery query = _entityManager.CreateEntityQuery(typeof(Box3DWorldSingleton));
            return query.GetSingleton<Box3DWorldSingleton>().World;
        }
    }
}
