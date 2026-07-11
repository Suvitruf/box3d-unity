using System;
using System.Runtime.InteropServices;
using AOT;
using Unity.Mathematics;

namespace Box3d
{
    public unsafe partial struct World
    {
        // Collector trampolines: static, rooted for the process lifetime, IL2CPP-safe via
        // MonoPInvokeCallback. Per-call state travels through the native context pointer as a
        // stack-allocated context struct — no allocation per query.

        private static readonly b3OverlapResultFcn OverlapCollectorDelegate = OverlapCollector;
        private static readonly IntPtr OverlapCollectorPtr = Marshal.GetFunctionPointerForDelegate(OverlapCollectorDelegate);
        private static readonly b3CastResultFcn CastCollectorDelegate = CastCollector;
        private static readonly IntPtr CastCollectorPtr = Marshal.GetFunctionPointerForDelegate(CastCollectorDelegate);

        private unsafe struct ShapeCollectorContext
        {
            public ShapeId* Buffer;
            public int Capacity;
            public int Count;
        }

        private unsafe struct RayCollectorContext
        {
            public RayHit* Buffer;
            public int Capacity;
            public int Count;
        }

        [MonoPInvokeCallback(typeof(b3OverlapResultFcn))]
        private static unsafe NativeBool OverlapCollector(ShapeId shapeId, void* context)
        {
            var ctx = (ShapeCollectorContext*)context;
            if (ctx->Count == ctx->Capacity) return false; // buffer full — stop the query
            ctx->Buffer[ctx->Count] = shapeId;
            ctx->Count++;
            return true;
        }

        [MonoPInvokeCallback(typeof(b3CastResultFcn))]
        private static unsafe float CastCollector(ShapeId shapeId, float3 point, float3 normal,
            float fraction, ulong userMaterialId, int triangleIndex, int childIndex, void* context)
        {
            var ctx = (RayCollectorContext*)context;
            if (ctx->Count == ctx->Capacity) return 0f; // buffer full — terminate
            ctx->Buffer[ctx->Count] = new RayHit
            {
                ShapeId = shapeId,
                Point = point,
                Normal = normal,
                Fraction = fraction,
                UserMaterialId = userMaterialId,
                TriangleIndex = triangleIndex,
                ChildIndex = childIndex,
            };
            ctx->Count++;
            return 1f; // keep the full ray — collect every hit
        }

        /// <summary>Finds shapes whose bounds overlap the AABB. Fills the buffer, returns the
        /// count (query stops early if the buffer fills up).</summary>
        public int OverlapAABB(B3Aabb aabb, QueryFilter filter, Span<ShapeId> results)
            => OverlapAABB(aabb, filter, results, out _);

        /// <summary>As <see cref="OverlapAABB(B3Aabb, QueryFilter, Span{ShapeId})"/>, also reporting the
        /// broadphase-tree work the query did (<see cref="TreeStats"/>) — useful for profiling queries.</summary>
        public unsafe int OverlapAABB(B3Aabb aabb, QueryFilter filter, Span<ShapeId> results, out TreeStats stats)
        {
            fixed (ShapeId* buffer = results)
            {
                var ctx = new ShapeCollectorContext { Buffer = buffer, Capacity = results.Length };
                b3TreeStats s = UnsafeBindings.b3World_OverlapAABB(Id, aabb, filter, OverlapCollectorPtr, &ctx);
                stats = new TreeStats { NodeVisits = s.nodeVisits, LeafVisits = s.leafVisits };
                return ctx.Count;
            }
        }

        /// <summary>Finds shapes overlapping a convex point-cloud proxy (a sphere when one point,
        /// a capsule when two, a hull otherwise) placed at origin. Fills the buffer, returns the count.</summary>
        public int OverlapShape(float3 origin, ReadOnlySpan<float3> proxyPoints, float proxyRadius,
            QueryFilter filter, Span<ShapeId> results)
            => OverlapShape(origin, proxyPoints, proxyRadius, filter, results, out _);

        /// <summary>As the other <c>OverlapShape</c>, also reporting broadphase-tree work.</summary>
        public unsafe int OverlapShape(float3 origin, ReadOnlySpan<float3> proxyPoints, float proxyRadius,
            QueryFilter filter, Span<ShapeId> results, out TreeStats stats)
        {
            if (proxyPoints.IsEmpty) throw new ArgumentException("proxy needs at least one point", nameof(proxyPoints));
            fixed (float3* points = proxyPoints)
            fixed (ShapeId* buffer = results)
            {
                var proxy = new b3ShapeProxy { points = points, count = proxyPoints.Length, radius = proxyRadius };
                var ctx = new ShapeCollectorContext { Buffer = buffer, Capacity = results.Length };
                b3TreeStats s = UnsafeBindings.b3World_OverlapShape(Id, origin, &proxy, filter, OverlapCollectorPtr, &ctx);
                stats = new TreeStats { NodeVisits = s.nodeVisits, LeafVisits = s.leafVisits };
                return ctx.Count;
            }
        }

        /// <summary>Collects every hit along the ray (unordered). Fills the buffer, returns the
        /// count. For just the nearest hit use <see cref="CastRayClosest"/>.</summary>
        public int CastRay(float3 origin, float3 translation, QueryFilter filter, Span<RayHit> hits)
            => CastRay(origin, translation, filter, hits, out _);

        /// <summary>As the other <c>CastRay</c>, also reporting broadphase-tree work.</summary>
        public unsafe int CastRay(float3 origin, float3 translation, QueryFilter filter, Span<RayHit> hits, out TreeStats stats)
        {
            fixed (RayHit* buffer = hits)
            {
                var ctx = new RayCollectorContext { Buffer = buffer, Capacity = hits.Length };
                b3TreeStats s = UnsafeBindings.b3World_CastRay(Id, origin, translation, filter, CastCollectorPtr, &ctx);
                stats = new TreeStats { NodeVisits = s.nodeVisits, LeafVisits = s.leafVisits };
                return ctx.Count;
            }
        }

        /// <summary>Sweeps a convex point-cloud proxy from origin along translation, collecting
        /// every hit (unordered). Fills the buffer, returns the count.</summary>
        public int CastShape(float3 origin, ReadOnlySpan<float3> proxyPoints, float proxyRadius,
            float3 translation, QueryFilter filter, Span<RayHit> hits)
            => CastShape(origin, proxyPoints, proxyRadius, translation, filter, hits, out _);

        /// <summary>As the other <c>CastShape</c>, also reporting broadphase-tree work.</summary>
        public unsafe int CastShape(float3 origin, ReadOnlySpan<float3> proxyPoints, float proxyRadius,
            float3 translation, QueryFilter filter, Span<RayHit> hits, out TreeStats stats)
        {
            if (proxyPoints.IsEmpty) throw new ArgumentException("proxy needs at least one point", nameof(proxyPoints));
            fixed (float3* points = proxyPoints)
            fixed (RayHit* buffer = hits)
            {
                var proxy = new b3ShapeProxy { points = points, count = proxyPoints.Length, radius = proxyRadius };
                var ctx = new RayCollectorContext { Buffer = buffer, Capacity = hits.Length };
                b3TreeStats s = UnsafeBindings.b3World_CastShape(Id, origin, &proxy, translation, filter, CastCollectorPtr, &ctx);
                stats = new TreeStats { NodeVisits = s.nodeVisits, LeafVisits = s.leafVisits };
                return ctx.Count;
            }
        }
    }
}
