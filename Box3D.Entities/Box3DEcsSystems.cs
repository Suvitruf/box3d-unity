using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Box3D.Entities
{
    /// <summary>Creates the native Box3D world (and its singleton) lazily when the first
    /// <see cref="Box3DBodyDefinition"/> appears, and destroys it with the ECS world.</summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    public partial struct Box3DWorldSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Box3DBodyDefinition>();
        }

        // Managed on purpose: World.Create wires the managed debug-draw bridge.
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<Box3DWorldSingleton>()) return;
            Entity entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new Box3DWorldSingleton { World = World.Create(WorldDef.Default), TimeStep = 0f, SubSteps = 4, });
        }

        public void OnDestroy(ref SystemState state)
        {
            using EntityQuery query = state.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<Box3DWorldSingleton>());
            if (!query.TryGetSingleton(out Box3DWorldSingleton singleton)) return;
            singleton.World.Destroy();
            state.EntityManager.DestroyEntity(query.GetSingletonEntity());
        }
    }

    /// <summary>Creates a native body (with its shape) for every entity carrying
    /// <see cref="Box3DBodyDefinition"/> + <see cref="Box3DShapeDefinition"/> + <c>LocalTransform</c>,
    /// and destroys the native body of every entity whose definition is gone (usually because the
    /// entity was destroyed, leaving only the <see cref="Box3DBodyRef"/> cleanup component).</summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(Box3DWorldSystem))]
    public partial struct Box3DBodyLifecycleSystem : ISystem
    {
        private EntityQuery _newBodies;
        private EntityQuery _orphanedRefs;

        public void OnCreate(ref SystemState state)
        {
            _newBodies = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Box3DBodyDefinition, Box3DShapeDefinition, LocalTransform>()
                .WithNone<Box3DBodyRef>()
                .Build(ref state);
            _orphanedRefs = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Box3DBodyRef>()
                .WithNone<Box3DBodyDefinition>()
                .Build(ref state);
            state.RequireForUpdate<Box3DWorldSingleton>();
        }

        // Managed on purpose: performs structural changes and calls the native def factories.
        public void OnUpdate(ref SystemState state)
        {
            EntityManager entityManager = state.EntityManager;

            if (!_orphanedRefs.IsEmpty)
            {
                using NativeArray<Box3DBodyRef> refs = _orphanedRefs.ToComponentDataArray<Box3DBodyRef>(Allocator.Temp);
                foreach (Box3DBodyRef bodyRef in refs)
                {
                    Body body = bodyRef.Value;
                    if (body.IsValid) body.Destroy();
                }

                entityManager.RemoveComponent<Box3DBodyRef>(_orphanedRefs);
            }

            if (!_newBodies.IsEmpty)
            {
                World world = SystemAPI.GetSingleton<Box3DWorldSingleton>().World;
                using NativeArray<Entity> entities = _newBodies.ToEntityArray(Allocator.Temp);
                foreach (Entity entity in entities)
                {
                    CreateBody(world, entityManager, entity);
                }
            }
        }

        private static void CreateBody(World world, EntityManager entityManager, Entity entity)
        {
            Box3DBodyDefinition definition = entityManager.GetComponentData<Box3DBodyDefinition>(entity);
            Box3DShapeDefinition shape = entityManager.GetComponentData<Box3DShapeDefinition>(entity);
            LocalTransform transform = entityManager.GetComponentData<LocalTransform>(entity);

            BodyDef def = BodyDef.Default;
            def.Type = definition.Type;
            def.Position = transform.Position;
            def.Rotation = transform.Rotation;
            def.LinearVelocity = definition.LinearVelocity;
            def.AngularVelocity = definition.AngularVelocity;
            def.LinearDamping = definition.LinearDamping;
            def.AngularDamping = definition.AngularDamping;
            def.GravityScale = definition.GravityScale;
            def.IsBullet = definition.IsBullet;
            def.EnableSleep = definition.EnableSleep;
            def.UserData = Box3DEcsUserData.FromEntity(entity);
            Body body = world.CreateBody(in def);

            ShapeDef shapeDef = ShapeDef.Default;
            if (shape.Density > 0f) shapeDef.Density = shape.Density;
            shapeDef.BaseMaterial.Friction = shape.Friction;
            shapeDef.BaseMaterial.Restitution = shape.Restitution;
            switch (shape.Kind)
            {
                case Box3DShapeKind.Sphere:
                    body.CreateSphereShape(in shapeDef, new Sphere { Radius = shape.Radius });
                    break;
                case Box3DShapeKind.Capsule:
                    body.CreateCapsuleShape(in shapeDef, new Capsule { Center1 = new float3(0f, -shape.HalfHeight, 0f), Center2 = new float3(0f, shape.HalfHeight, 0f), Radius = shape.Radius, });
                    break;
                default:
                    BoxHull hull = BoxHull.Create(shape.HalfExtents.x, shape.HalfExtents.y, shape.HalfExtents.z);
                    body.CreateHullShape(in shapeDef, in hull);
                    break;
            }

            entityManager.AddComponentData(entity, new Box3DBodyRef { Value = body });
        }
    }

    /// <summary>Steps the native world once per fixed update. Burst-compiled — the step is a direct
    /// native call.</summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(Box3DBodyLifecycleSystem))]
    [BurstCompile]
    public partial struct Box3DStepSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Box3DWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Box3DWorldSingleton singleton = SystemAPI.GetSingleton<Box3DWorldSingleton>();
            float timeStep = singleton.TimeStep > 0f ? singleton.TimeStep : SystemAPI.Time.DeltaTime;
            if (timeStep <= 0f) return;
            singleton.World.Step(timeStep, singleton.SubSteps);
        }
    }

    /// <summary>Writes the transforms of bodies that moved during the step back to
    /// <c>LocalTransform</c>, via the engine's move-event array — one Burst-compiled pass over only
    /// the bodies that actually moved (the events fetch goes through the native glue exports, so
    /// the whole system runs under Burst).</summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(Box3DStepSystem))]
    [BurstCompile]
    public partial struct Box3DWriteBackSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transforms;

        public void OnCreate(ref SystemState state)
        {
            _transforms = state.GetComponentLookup<LocalTransform>();
            state.RequireForUpdate<Box3DWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            World world = SystemAPI.GetSingleton<Box3DWorldSingleton>().World;
            ReadOnlySpan<BodyMoveEvent> moves = world.GetBodyMoveEvents();
            if (moves.Length == 0) return;

            _transforms.Update(ref state);
            for (int i = 0; i < moves.Length; i++)
            {
                Entity entity = Box3DEcsUserData.ToEntity(moves[i].UserData);
                if (!_transforms.HasComponent(entity)) continue;
                RefRW<LocalTransform> transform = _transforms.GetRefRW(entity);
                transform.ValueRW.Position = moves[i].Transform.Position;
                transform.ValueRW.Rotation = moves[i].Transform.Rotation;
            }
        }
    }
}
