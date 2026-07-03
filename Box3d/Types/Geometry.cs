using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Box3d
{
    /// <summary>Mirrors native b3Sphere (16 bytes): a sphere with a local-space offset.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Sphere
    {
        public float3 Center;
        public float Radius;
    }

    /// <summary>Mirrors native b3Capsule (28 bytes): two hemisphere centers and a radius.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Capsule
    {
        public float3 Center1;
        public float3 Center2;
        public float Radius;
    }

    /// <summary>Mirrors native b3AABB (24 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct B3Aabb
    {
        public float3 LowerBound;
        public float3 UpperBound;
    }

    /// <summary>Mirrors native b3Matrix3 (36 bytes, column-major).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct B3Matrix3
    {
        public float3 Cx;
        public float3 Cy;
        public float3 Cz;
    }

    /// <summary>Header of native b3HullData (136 bytes). Real hull data hangs off the end of this
    /// struct in native memory — never copy a b3HullData you don't own; pass by reference only.
    /// Fields are engine-internal; exposed read-only where useful.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct HullData
    {
        internal ulong Version;
        internal int ByteCount;
        internal uint Hash;
        internal B3Aabb Aabb;
        internal float SurfaceArea;
        internal float Volume;
        internal float InnerRadius;
        internal float3 Center;
        internal B3Matrix3 CentralInertia;
        internal int VertexCount;
        internal int VertexOffset;
        internal int PointOffset;
        internal int EdgeCount;
        internal int EdgeOffset;
        internal int FaceCount;
        internal int FaceOffset;
        internal int PlaneOffset;
        internal int Padding;
    }

    /// <summary>Mirrors native b3BoxHull (440 bytes): a self-contained box hull returned by value
    /// from b3MakeBoxHull. Safe to keep on the stack — hull shape creation clones the data.
    /// The trailing blob holds the vertex/point/edge/face/plane arrays the header offsets index into.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BoxHull
    {
        internal HullData Base;
        internal fixed byte Data[304];

        /// <summary>A box hull with the given half-extents, centered at the local origin.</summary>
        public static BoxHull Create(float halfX, float halfY, float halfZ)
        {
            return UnsafeBindings.b3MakeBoxHull(halfX, halfY, halfZ);
        }

        /// <summary>A cube hull with the given half-width, centered at the local origin.</summary>
        public static BoxHull CreateCube(float halfWidth)
        {
            return UnsafeBindings.b3MakeCubeHull(halfWidth);
        }
    }
}
