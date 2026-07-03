using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Box3d
{
    // Mirrors of the definition structs in box3d types.h. Every def carries a hidden validation
    // field set by its native b3Default…() factory — never zero-initialize these in C#; always
    // start from the Default property, then mutate fields.

    /// <summary>Mirrors native b3Capacity (20 bytes). Optional initial world capacities.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Capacity
    {
        public int StaticShapeCount;
        public int DynamicShapeCount;
        public int StaticBodyCount;
        public int DynamicBodyCount;
        public int ContactCount;
    }

    /// <summary>Mirrors native b3MotionLocks (6 bytes). Per-axis translation/rotation locks.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MotionLocks
    {
        public NativeBool LinearX;
        public NativeBool LinearY;
        public NativeBool LinearZ;
        public NativeBool AngularX;
        public NativeBool AngularY;
        public NativeBool AngularZ;
    }

    /// <summary>Mirrors native b3Filter (24 bytes). Collision category/mask/group filtering.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CollisionFilter
    {
        public ulong CategoryBits;
        public ulong MaskBits;
        public int GroupIndex;

        public static CollisionFilter Default => UnsafeBindings.b3DefaultFilter();
    }

    /// <summary>Mirrors native b3SurfaceMaterial (40 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SurfaceMaterial
    {
        public float Friction;
        public float Restitution;
        public float RollingResistance;
        public float3 TangentVelocity;
        public ulong UserMaterialId;
        public uint CustomColor;

        public static SurfaceMaterial Default => UnsafeBindings.b3DefaultSurfaceMaterial();
    }

    /// <summary>Mirrors native b3WorldDef (144 bytes on x64). Create via <see cref="Default"/> only.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WorldDef
    {
        public float3 Gravity;
        public float RestitutionThreshold;
        public float HitEventThreshold;
        public float ContactHertz;
        public float ContactDampingRatio;
        public float ContactSpeed;
        public float MaximumLinearSpeed;
        public IntPtr FrictionCallback;
        public IntPtr RestitutionCallback;
        public NativeBool EnableSleep;
        public NativeBool EnableContinuous;
        public uint WorkerCount;
        public IntPtr EnqueueTask;
        public IntPtr FinishTask;
        public IntPtr UserTaskContext;
        public IntPtr UserData;
        public IntPtr CreateDebugShape;
        public IntPtr DestroyDebugShape;
        public IntPtr UserDebugShapeContext;
        public Capacity Capacity;
        internal int InternalValue;

        public static WorldDef Default => UnsafeBindings.b3DefaultWorldDef();
    }

    /// <summary>Mirrors native b3BodyDef (104 bytes on x64). Create via <see cref="Default"/> only.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BodyDef
    {
        public BodyType Type;
        public float3 Position;
        public quaternion Rotation;
        public float3 LinearVelocity;
        public float3 AngularVelocity;
        public float LinearDamping;
        public float AngularDamping;
        public float GravityScale;
        public float SleepThreshold;
        public IntPtr Name;
        public IntPtr UserData;
        public MotionLocks MotionLocks;
        public NativeBool EnableSleep;
        public NativeBool IsAwake;
        public NativeBool IsBullet;
        public NativeBool IsEnabled;
        public NativeBool AllowFastRotation;
        public NativeBool EnableContactRecycling;
        internal int InternalValue;

        public static BodyDef Default => UnsafeBindings.b3DefaultBodyDef();
    }

    /// <summary>Mirrors native b3ShapeDef (112 bytes on x64). Create via <see cref="Default"/> only.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ShapeDef
    {
        public IntPtr UserData;
        public IntPtr Materials;
        public int MaterialCount;
        public SurfaceMaterial BaseMaterial;
        public float Density;
        public float ExplosionScale;
        public CollisionFilter Filter;
        public NativeBool EnableCustomFiltering;
        public NativeBool IsSensor;
        public NativeBool EnableSensorEvents;
        public NativeBool EnableContactEvents;
        public NativeBool EnableHitEvents;
        public NativeBool EnablePreSolveEvents;
        public NativeBool InvokeContactCreation;
        public NativeBool UpdateBodyMass;
        internal int InternalValue;

        public static ShapeDef Default => UnsafeBindings.b3DefaultShapeDef();
    }
}
