using System;
using System.Runtime.InteropServices;
using AOT;
using Unity.Mathematics;

namespace Box3D
{
    public unsafe partial struct World
    {
        private static readonly b3PlaneResultFcn PlaneCollectorDelegate = PlaneCollector;
        private static readonly IntPtr PlaneCollectorPtr = Marshal.GetFunctionPointerForDelegate(PlaneCollectorDelegate);

        private unsafe struct PlaneCollectorContext
        {
            public CollisionPlane* Buffer;
            public MoverHit* Hits;
            public float3 Origin;
            public int Capacity;
            public int Count;
            public float PushLimit;
        }

        [MonoPInvokeCallback(typeof(b3PlaneResultFcn))]
        private static NativeBool PlaneCollector(ShapeId shapeId, PlaneResult* planes, int planeCount, void* context)
        {
            var ctx = (PlaneCollectorContext*)context;
            for (int i = 0; i < planeCount; i++)
            {
                if (ctx->Count == ctx->Capacity) return false;
                ctx->Buffer[ctx->Count] = new CollisionPlane
                {
                    Plane = planes[i].Plane,
                    PushLimit = ctx->PushLimit,
                    Push = 0f,
                    ClipVelocity = true,
                };
                if (ctx->Hits != null)
                {
                    ctx->Hits[ctx->Count] = new MoverHit
                    {
                        ShapeId = shapeId,
                        // The native point is relative to the mover origin; report world space.
                        Point = ctx->Origin + planes[i].Point,
                    };
                }
                ctx->Count++;
            }
            return true;
        }

        /// <summary>Collides a capsule mover with the world and assembles rigid collision planes
        /// ready for <see cref="Mover.SolvePlanes"/>. Fills the buffer, returns the count.</summary>
        public int CollideMover(float3 origin, in Capsule mover, QueryFilter filter,
            Span<CollisionPlane> planes, float pushLimit = float.MaxValue)
        {
            return CollideMover(origin, in mover, filter, planes, Span<MoverHit>.Empty, pushLimit);
        }

        /// <summary>Same as <see cref="CollideMover(float3, in Capsule, QueryFilter, Span{CollisionPlane}, float)"/>,
        /// but also records a <see cref="MoverHit"/> (source shape + closest point on it) per plane,
        /// index-aligned with the planes buffer. Pass an empty hits span to skip recording.</summary>
        public int CollideMover(float3 origin, in Capsule mover, QueryFilter filter,
            Span<CollisionPlane> planes, Span<MoverHit> hits, float pushLimit = float.MaxValue)
        {
            if (!hits.IsEmpty && hits.Length < planes.Length)
                throw new ArgumentException("hits buffer must be at least as large as the planes buffer", nameof(hits));

            Capsule localMover = mover;
            fixed (CollisionPlane* buffer = planes)
            fixed (MoverHit* hitBuffer = hits)
            {
                var ctx = new PlaneCollectorContext
                {
                    Buffer = buffer,
                    Hits = hitBuffer,
                    Origin = origin,
                    Capacity = planes.Length,
                    PushLimit = pushLimit,
                };
                UnsafeBindings.b3World_CollideMover(Id, origin, &localMover, filter, PlaneCollectorPtr, &ctx);
                return ctx.Count;
            }
        }

        /// <summary>Sweeps a capsule mover along a translation. Returns the fraction [0,1] of the
        /// translation that can be taken before hitting something (1 = free path).</summary>
        public float CastMover(float3 origin, in Capsule mover, float3 translation, QueryFilter filter)
        {
            Capsule localMover = mover;
            return UnsafeBindings.b3World_CastMover(Id, origin, &localMover, translation, filter, IntPtr.Zero, null);
        }
    }

    /// <summary>World-independent character mover solver (box3d's kinematic capsule toolkit).
    /// Typical loop each step: <see cref="World.CollideMover"/> to gather planes →
    /// <see cref="SolvePlanes"/> to get a corrected movement delta → apply it →
    /// <see cref="ClipVector"/> to remove velocity pointing into surfaces.</summary>
    public static unsafe class Mover
    {
        /// <summary>Solves the target movement delta against the collision planes. Writes the
        /// computed push back into each plane. Returns the corrected delta.</summary>
        public static PlaneSolverResult SolvePlanes(float3 targetDelta, Span<CollisionPlane> planes)
        {
            fixed (CollisionPlane* p = planes)
            {
                return UnsafeBindings.b3SolvePlanes(targetDelta, p, planes.Length);
            }
        }

        /// <summary>Removes components of a vector pointing into the clip planes (velocity
        /// projection for sliding along surfaces).</summary>
        public static float3 ClipVector(float3 vector, ReadOnlySpan<CollisionPlane> planes)
        {
            fixed (CollisionPlane* p = planes)
            {
                return UnsafeBindings.b3ClipVector(vector, p, planes.Length);
            }
        }
    }
}
