using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Box3D
{
    /// <summary>Mirrors native b3Plane (16 bytes). Separation = dot(normal, point) - offset.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct B3Plane
    {
        public float3 Normal;
        public float Offset;
    }

    /// <summary>Mirrors native b3PlaneResult (28 bytes): the separating plane between a mover and
    /// a shape, plus the closest point on the shape. Both are relative to the mover origin (float in
    /// both precision modes); <see cref="MoverHit"/> carries the point as a world position.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneResult
    {
        public B3Plane Plane;
        public float3 Point;
    }

    /// <summary>Mirrors native b3CollisionPlane (28 bytes). Input/output of the plane solver:
    /// PushLimit = float.MaxValue for rigid planes (lower = soft); Push is written by the solver;
    /// ClipVelocity should be false for soft planes.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CollisionPlane
    {
        public B3Plane Plane;
        public float PushLimit;
        public float Push;
        public NativeBool ClipVelocity;
    }

    /// <summary>Mirrors native b3PlaneSolverResult (16 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneSolverResult
    {
        public float3 Delta;
        public int IterationCount;
    }

    /// <summary>Per-plane collision details gathered by
    /// <see cref="World.CollideMover(B3Pos, in Capsule, QueryFilter, System.Span{CollisionPlane}, System.Span{MoverHit}, float)"/>,
    /// index-aligned with the CollisionPlane buffer: the shape that produced the plane and the
    /// closest point on that shape as a world position (may not be unique for deep overlaps). Use
    /// for debug drawing, pushing dynamic bodies, or per-surface plane tuning.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MoverHit
    {
        public ShapeId ShapeId;
        public B3Pos Point;
    }
}
