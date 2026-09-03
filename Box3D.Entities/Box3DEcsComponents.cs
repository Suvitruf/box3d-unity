using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Box3D.Entities
{
    /// <summary>Singleton owning the native Box3D world the ECS systems simulate. Created lazily by
    /// <see cref="Box3DWorldSystem"/> when the first <see cref="Box3DBodyDefinition"/> appears.</summary>
    public struct Box3DWorldSingleton : IComponentData
    {
        public World World;

        /// <summary>Fixed step used by <see cref="Box3DStepSystem"/>. Zero or negative means
        /// "use the group's delta time" (the fixed timestep inside FixedStepSimulationSystemGroup).</summary>
        public float TimeStep;

        public int SubSteps;
    }

    /// <summary>Describes the rigid body to create for this entity. Together with
    /// <see cref="Box3DShapeDefinition"/> and <c>LocalTransform</c> it makes
    /// <see cref="Box3DBodyLifecycleSystem"/> create a native body and attach a
    /// <see cref="Box3DBodyRef"/>. Start from one of the factory properties — a default-constructed
    /// value has sleeping disabled and zero gravity scale.</summary>
    public struct Box3DBodyDefinition : IComponentData
    {
        public BodyType Type;
        public float GravityScale;
        public float LinearDamping;
        public float AngularDamping;
        public float3 LinearVelocity;
        public float3 AngularVelocity;
        public bool IsBullet;
        public bool EnableSleep;

        public static Box3DBodyDefinition Dynamic => WithType(BodyType.Dynamic);

        public static Box3DBodyDefinition Kinematic => WithType(BodyType.Kinematic);

        public static Box3DBodyDefinition Static => WithType(BodyType.Static);

        private static Box3DBodyDefinition WithType(BodyType type)
        {
            return new Box3DBodyDefinition { Type = type, GravityScale = 1f, EnableSleep = true, };
        }
    }

    public enum Box3DShapeKind
    {
        Box = 0,
        Sphere = 1,
        Capsule = 2,
    }

    /// <summary>Describes the single collision shape attached to the entity's body at creation.
    /// Start from a factory method — a default-constructed value has zero size and zero friction.</summary>
    public struct Box3DShapeDefinition : IComponentData
    {
        public Box3DShapeKind Kind;

        /// <summary>Box only.</summary>
        public float3 HalfExtents;

        /// <summary>Sphere and capsule.</summary>
        public float Radius;

        /// <summary>Capsule only: half the distance between the hemisphere centers (along local Y).</summary>
        public float HalfHeight;

        /// <summary>Zero or negative falls back to the engine's default density (1000 kg/m³, water-like).</summary>
        public float Density;

        public float Friction;
        public float Restitution;

        public static Box3DShapeDefinition Box(float3 halfExtents)
        {
            return new Box3DShapeDefinition { Kind = Box3DShapeKind.Box, HalfExtents = halfExtents, Friction = 0.6f };
        }

        public static Box3DShapeDefinition Sphere(float radius)
        {
            return new Box3DShapeDefinition { Kind = Box3DShapeKind.Sphere, Radius = radius, Friction = 0.6f };
        }

        public static Box3DShapeDefinition Capsule(float radius, float halfHeight)
        {
            return new Box3DShapeDefinition { Kind = Box3DShapeKind.Capsule, Radius = radius, HalfHeight = halfHeight, Friction = 0.6f };
        }
    }

    /// <summary>The live native body behind an entity. Cleanup component: it survives
    /// <c>DestroyEntity</c>, which is how <see cref="Box3DBodyLifecycleSystem"/> finds and destroys
    /// the native body afterwards.</summary>
    public struct Box3DBodyRef : ICleanupComponentData
    {
        public Body Value;
    }

    /// <summary>Round-trips an <c>Entity</c> through the pointer-sized body user data, so
    /// <see cref="BodyMoveEvent.UserData"/> routes straight back to the entity with no lookup.
    /// Requires 64-bit pointers — true on every shipped platform (win/linux x64, android arm64).</summary>
    public static class Box3DEcsUserData
    {
        public static IntPtr FromEntity(Entity entity)
        {
            return (IntPtr)(((long)entity.Version << 32) | (uint)entity.Index);
        }

        public static Entity ToEntity(IntPtr userData)
        {
            long packed = (long)userData;
            return new Entity { Index = (int)packed, Version = (int)(packed >> 32) };
        }
    }
}
