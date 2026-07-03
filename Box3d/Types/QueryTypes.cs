using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Box3d
{
    /// <summary>Mirrors native b3QueryFilter (32 bytes). Filters queries (ray/shape casts,
    /// overlaps) against shape filters.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct QueryFilter
    {
        public ulong CategoryBits;
        public ulong MaskBits;
        public ulong Id;
        public IntPtr Name;

        public static QueryFilter Default => UnsafeBindings.b3DefaultQueryFilter();
    }

    /// <summary>Mirrors native b3RayResult (64 bytes). Result of a closest-hit ray cast.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RayResult
    {
        public ShapeId ShapeId;
        public float3 Point;
        public float3 Normal;
        public ulong UserMaterialId;
        public float Fraction;
        public int TriangleIndex;
        public int ChildIndex;
        public int NodeVisits;
        public int LeafVisits;
        public NativeBool Hit;
    }

    /// <summary>One hit from an all-hits ray or shape cast (wrapper-defined container, filled by
    /// the query collectors — not a native mirror).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RayHit
    {
        public ShapeId ShapeId;
        public float3 Point;
        public float3 Normal;
        public float Fraction;
        public ulong UserMaterialId;
        public int TriangleIndex;
        public int ChildIndex;
    }

    /// <summary>Mirrors native b3MassData (52 bytes): mass, local center, rotational inertia
    /// about the center.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MassData
    {
        public float Mass;
        public float3 Center;
        public B3Matrix3 Inertia;
    }

    /// <summary>Mirrors native b3ExplosionDef (32 bytes). Create via <see cref="Default"/>.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ExplosionDef
    {
        public ulong MaskBits;
        public float3 Position;
        public float Radius;
        public float Falloff;
        public float ImpulsePerArea;

        public static ExplosionDef Default => UnsafeBindings.b3DefaultExplosionDef();
    }
}
