using Unity.Mathematics;
using UnityEngine;

namespace Box3D
{
    /// <summary>A world position — box3d's <c>b3Pos</c>. Single precision (three floats, identical
    /// to <c>float3</c>) by default; with the <c>BOX3D_DOUBLE</c> scripting define it widens to
    /// three doubles for large worlds (pair with the double-precision native library — see
    /// Documentation~/building-natives.md). Only world POSITIONS are affected; velocities,
    /// rotations and local geometry stay float in both modes.
    ///
    /// In single mode it converts implicitly both ways with <c>float3</c>/<c>Vector3</c>. In double
    /// mode conversions INTO <c>B3Pos</c> stay implicit (widening), but conversions OUT are
    /// explicit casts — they narrow to float and lose precision far from the origin.</summary>
    public struct B3Pos
    {
#if !BOX3D_DOUBLE
        public float x, y, z;

        public B3Pos(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static unsafe implicit operator float3(B3Pos p) => *(float3*)&p;
        public static unsafe implicit operator B3Pos(float3 v) => *(B3Pos*)&v;
        public static implicit operator Vector3(B3Pos p) => new Vector3(p.x, p.y, p.z);
        public static implicit operator B3Pos(Vector3 v) => new B3Pos(v.x, v.y, v.z);

        /// <summary>The position as <c>float3</c> (no-op in single precision, narrowing in double).</summary>
        public float3 ToFloat3() => this;
#else
        public double x, y, z;

        public B3Pos(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static unsafe implicit operator double3(B3Pos p) => *(double3*)&p;
        public static unsafe implicit operator B3Pos(double3 v) => *(B3Pos*)&v;

        // Widening (lossless) — implicit.
        public static implicit operator B3Pos(float3 v) => new B3Pos(v.x, v.y, v.z);
        public static implicit operator B3Pos(Vector3 v) => new B3Pos(v.x, v.y, v.z);

        // Narrowing (LOSSY far from the origin) — explicit on purpose.
        public static explicit operator float3(B3Pos p) => new float3((float)p.x, (float)p.y, (float)p.z);
        public static explicit operator Vector3(B3Pos p) => new Vector3((float)p.x, (float)p.y, (float)p.z);

        /// <summary>The position narrowed to <c>float3</c> — LOSSY far from the origin. For Unity
        /// Transform sync, convert relative to a render origin instead of narrowing absolutes.</summary>
        public float3 ToFloat3() => new float3((float)x, (float)y, (float)z);
#endif

        public static B3Pos Zero => default;

        public override string ToString() => $"({x}, {y}, {z})";
    }
}
