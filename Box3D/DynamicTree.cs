using System;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using Unity.Mathematics;

namespace Box3D
{
    /// <summary>A standalone dynamic AABB tree (bounding-volume hierarchy) — box3d's broadphase
    /// spatial index, exposed for your own use: a fast "what's near here?" index over non-physics
    /// data (culling, range checks, trigger/AI volumes) that reuses the engine's tree instead of a
    /// hand-rolled quadtree. Each proxy carries a 64-bit <c>userData</c> you set (an index / entity
    /// id). Owns native memory — call <see cref="Dispose"/> or use <c>using</c>.
    ///
    /// Separate from physics: this tree filters with its own category/mask bits, unrelated to
    /// <see cref="CollisionFilter"/>. To query the PHYSICS world use <see cref="World"/>'s
    /// OverlapAABB / CastRay instead.</summary>
    public sealed unsafe class DynamicTree : IDisposable
    {
        // Native-allocated so the address is stable for the tree's whole life: box3d reallocs the
        // node pool through this pointer and writes the updated struct back in place.
        private b3DynamicTree* _tree;

        /// <param name="proxyCapacity">Initial node-pool size; grows automatically.</param>
        public DynamicTree(int proxyCapacity = 16)
        {
            _tree = (b3DynamicTree*)Marshal.AllocHGlobal(sizeof(b3DynamicTree));
            *_tree = UnsafeBindings.b3DynamicTree_Create(proxyCapacity);
        }

        public bool IsCreated => _tree != null;

        public void Dispose()
        {
            DisposeNative();
            GC.SuppressFinalize(this);
        }

        // Backstop for a leaked instance: frees the native node pool from the finalizer thread
        // (safe — the pool is plain malloc/free memory, untouched by any world). Dispose remains
        // the correct path; the finalizer only limits the damage of forgetting it.
        ~DynamicTree()
        {
            DisposeNative();
        }

        private void DisposeNative()
        {
            if (_tree == null) return;
            UnsafeBindings.b3DynamicTree_Destroy(_tree);
            Marshal.FreeHGlobal((IntPtr)_tree);
            _tree = null;
        }

        // ---- proxies ----

        /// <summary>Inserts a proxy for <paramref name="aabb"/> carrying <paramref name="userData"/>;
        /// returns its id. <paramref name="categoryBits"/> is this tree's own filter tag.</summary>
        public int CreateProxy(B3Aabb aabb, ulong userData, ulong categoryBits = 1)
            => UnsafeBindings.b3DynamicTree_CreateProxy(_tree, aabb, categoryBits, userData);

        public void DestroyProxy(int proxyId) => UnsafeBindings.b3DynamicTree_DestroyProxy(_tree, proxyId);

        /// <summary>Re-fits a proxy to a new AABB (remove + reinsert). Use as objects move.</summary>
        public void MoveProxy(int proxyId, B3Aabb aabb) => UnsafeBindings.b3DynamicTree_MoveProxy(_tree, proxyId, aabb);

        /// <summary>Grows a proxy's fat AABB (and ancestors) — cheaper than Move when the box only expands.</summary>
        public void EnlargeProxy(int proxyId, B3Aabb aabb) => UnsafeBindings.b3DynamicTree_EnlargeProxy(_tree, proxyId, aabb);

        public void SetCategoryBits(int proxyId, ulong categoryBits) => UnsafeBindings.b3DynamicTree_SetCategoryBits(_tree, proxyId, categoryBits);

        public ulong GetCategoryBits(int proxyId) => UnsafeBindings.b3DynamicTree_GetCategoryBits(_tree, proxyId);

        // ---- introspection ----

        public int ProxyCount => UnsafeBindings.b3DynamicTree_GetProxyCount(_tree);

        public int Height => UnsafeBindings.b3DynamicTree_GetHeight(_tree);

        public float AreaRatio => UnsafeBindings.b3DynamicTree_GetAreaRatio(_tree);

        public B3Aabb RootBounds => UnsafeBindings.b3DynamicTree_GetRootBounds(_tree);

        public int ByteCount => UnsafeBindings.b3DynamicTree_GetByteCount(_tree);

        /// <summary>Rebuilds/re-balances, retaining unchanged subtrees. Returns the number of boxes
        /// sorted. Cheap to call periodically after many moves.</summary>
        public int Rebuild(bool fullBuild = false) => UnsafeBindings.b3DynamicTree_Rebuild(_tree, fullBuild);

        // ---- validation (debug / tests) ----

        /// <summary>Asserts internal invariants (box3d asserts on failure). For tests/debugging.</summary>
        public void Validate() => UnsafeBindings.b3DynamicTree_Validate(_tree);

        public void ValidateNoEnlarged() => UnsafeBindings.b3DynamicTree_ValidateNoEnlarged(_tree);

        // ---- save / load (debug) ----

        /// <summary>Writes the tree to a file for debugging.</summary>
        public void Save(string fileName)
        {
            byte[] p = NullTerminated(fileName);
            fixed (byte* pp = p)
            {
                UnsafeBindings.b3DynamicTree_Save(_tree, (sbyte*)pp);
            }
        }

        /// <summary>Loads a tree written with <see cref="Save"/> (debugging). Caller owns it — Dispose.</summary>
        public static DynamicTree Load(string fileName, float scale = 1f)
        {
            var tree = new DynamicTree(0);
            byte[] p = NullTerminated(fileName);
            fixed (byte* pp = p)
            {
                UnsafeBindings.b3DynamicTree_Destroy(tree._tree); // free the empty pool the ctor made
                *tree._tree = UnsafeBindings.b3DynamicTree_Load((sbyte*)pp, scale);
            }
            return tree;
        }

        private static byte[] NullTerminated(string s)
        {
            int n = Encoding.UTF8.GetByteCount(s);
            var buf = new byte[n + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, buf, 0);
            return buf; // trailing 0 already present (buffer is n+1)
        }

        // ---- queries: collector trampolines (static, rooted, IL2CPP-safe; per-call state via context) ----

        private static readonly b3TreeQueryCallbackFcn QueryDelegate = QueryCollector;
        private static readonly IntPtr QueryPtr = Marshal.GetFunctionPointerForDelegate(QueryDelegate);
        private static readonly b3TreeRayCastCallbackFcn RayDelegate = RayCollector;
        private static readonly IntPtr RayPtr = Marshal.GetFunctionPointerForDelegate(RayDelegate);
        private static readonly b3TreeBoxCastCallbackFcn BoxDelegate = BoxCollector;
        private static readonly IntPtr BoxPtr = Marshal.GetFunctionPointerForDelegate(BoxDelegate);

        private struct CollectorContext
        {
            public DynamicTreeHit* Buffer;
            public int Capacity;
            public int Count;
        }

        [MonoPInvokeCallback(typeof(b3TreeQueryCallbackFcn))]
        private static NativeBool QueryCollector(int proxyId, ulong userData, void* context)
        {
            var ctx = (CollectorContext*)context;
            if (ctx->Count == ctx->Capacity) return false; // buffer full — stop
            ctx->Buffer[ctx->Count++] = new DynamicTreeHit(proxyId, userData);
            return true;
        }

        [MonoPInvokeCallback(typeof(b3TreeRayCastCallbackFcn))]
        private static float RayCollector(b3RayCastInput* input, int proxyId, ulong userData, void* context)
        {
            var ctx = (CollectorContext*)context;
            if (ctx->Count == ctx->Capacity) return 0f; // full — clip the ray here, stop
            ctx->Buffer[ctx->Count++] = new DynamicTreeHit(proxyId, userData);
            return input->maxFraction; // keep the full ray — collect every AABB-crossed proxy
        }

        [MonoPInvokeCallback(typeof(b3TreeBoxCastCallbackFcn))]
        private static float BoxCollector(b3BoxCastInput* input, int proxyId, ulong userData, void* context)
        {
            var ctx = (CollectorContext*)context;
            if (ctx->Count == ctx->Capacity) return 0f;
            ctx->Buffer[ctx->Count++] = new DynamicTreeHit(proxyId, userData);
            return input->maxFraction;
        }

        /// <summary>Collects proxies whose fat AABB overlaps <paramref name="aabb"/>. Fills the
        /// buffer, returns the count (stops early if full). Filter with category/mask bits.</summary>
        public int Query(B3Aabb aabb, Span<DynamicTreeHit> results,
            ulong maskBits = ulong.MaxValue, bool requireAllBits = false)
            => Query(aabb, results, out _, maskBits, requireAllBits);

        /// <summary>As <see cref="Query(B3Aabb,Span{DynamicTreeHit},ulong,bool)"/>, also reporting
        /// nodes/leaves visited (<see cref="TreeStats"/>).</summary>
        public int Query(B3Aabb aabb, Span<DynamicTreeHit> results, out TreeStats stats,
            ulong maskBits = ulong.MaxValue, bool requireAllBits = false)
        {
            fixed (DynamicTreeHit* buffer = results)
            {
                var ctx = new CollectorContext { Buffer = buffer, Capacity = results.Length };
                b3TreeStats s = UnsafeBindings.b3DynamicTree_Query(_tree, aabb, maskBits, requireAllBits, QueryPtr, &ctx);
                stats = new TreeStats { NodeVisits = s.nodeVisits, LeafVisits = s.leafVisits };
                return ctx.Count;
            }
        }

        /// <summary>Collects proxies whose AABB the ray crosses (broadphase only — the tree knows
        /// boxes, not shapes, so do your own narrow-phase against each hit's <c>UserData</c>). Fills
        /// the buffer, returns the count.</summary>
        public int RayCast(float3 origin, float3 translation, Span<DynamicTreeHit> results, out TreeStats stats,
            float maxFraction = 1f, ulong maskBits = ulong.MaxValue, bool requireAllBits = false)
        {
            var input = new b3RayCastInput { origin = origin, translation = translation, maxFraction = maxFraction };
            fixed (DynamicTreeHit* buffer = results)
            {
                var ctx = new CollectorContext { Buffer = buffer, Capacity = results.Length };
                b3TreeStats s = UnsafeBindings.b3DynamicTree_RayCast(_tree, &input, maskBits, requireAllBits, RayPtr, &ctx);
                stats = new TreeStats { NodeVisits = s.nodeVisits, LeafVisits = s.leafVisits };
                return ctx.Count;
            }
        }

        public int RayCast(float3 origin, float3 translation, Span<DynamicTreeHit> results,
            float maxFraction = 1f, ulong maskBits = ulong.MaxValue, bool requireAllBits = false)
            => RayCast(origin, translation, results, out _, maxFraction, maskBits, requireAllBits);

        /// <summary>Sweeps an AABB along <paramref name="translation"/>, collecting proxies it
        /// crosses (broadphase only). Fills the buffer, returns the count.</summary>
        public int BoxCast(B3Aabb box, float3 translation, Span<DynamicTreeHit> results, out TreeStats stats,
            float maxFraction = 1f, ulong maskBits = ulong.MaxValue, bool requireAllBits = false)
        {
            var input = new b3BoxCastInput { box = box, translation = translation, maxFraction = maxFraction };
            fixed (DynamicTreeHit* buffer = results)
            {
                var ctx = new CollectorContext { Buffer = buffer, Capacity = results.Length };
                b3TreeStats s = UnsafeBindings.b3DynamicTree_BoxCast(_tree, &input, maskBits, requireAllBits, BoxPtr, &ctx);
                stats = new TreeStats { NodeVisits = s.nodeVisits, LeafVisits = s.leafVisits };
                return ctx.Count;
            }
        }

        public int BoxCast(B3Aabb box, float3 translation, Span<DynamicTreeHit> results,
            float maxFraction = 1f, ulong maskBits = ulong.MaxValue, bool requireAllBits = false)
            => BoxCast(box, translation, results, out _, maxFraction, maskBits, requireAllBits);
    }
}
