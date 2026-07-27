# Queries

All world queries are allocation-free: you pass a buffer (usually `stackalloc`), the query fills
it and returns the count. Every query takes a `QueryFilter` (from `QueryFilter.Default`) matched
against shape filters.

## Ray casts

```csharp
// Closest hit — the common case:
RayResult hit = world.CastRayClosest(origin, translation, QueryFilter.Default);
if (hit.Hit)
{
    // hit.ShapeId, hit.Point, hit.Normal, hit.Fraction, hit.TriangleIndex, hit.UserMaterialId
    Body hitBody = new Body { Id = new Shape { Id = hit.ShapeId }.GetBody() };
}

// All hits along the ray (unordered):
Span<RayHit> hits = stackalloc RayHit[16];
int count = world.CastRay(origin, translation, QueryFilter.Default, hits);
```

Note `translation` is the full ray vector (direction × distance), not a normalized direction.

## Overlaps

```csharp
// Everything whose bounds overlap a box:
Span<ShapeId> results = stackalloc ShapeId[64];
int count = world.OverlapAABB(aabb, QueryFilter.Default, results);

// Overlap an arbitrary convex proxy: 1 point + radius = sphere, 2 = capsule, N = hull.
Span<float3> point = stackalloc float3[] { float3.zero };
count = world.OverlapShape(center, point, radius, QueryFilter.Default, results);
```

## Shape casts (sweeps)

```csharp
// Sweep a sphere and collect every hit:
Span<float3> proxy = stackalloc float3[] { float3.zero };
Span<RayHit> hits = stackalloc RayHit[16];
int count = world.CastShape(origin, proxy, 0.5f, translation, QueryFilter.Default, hits);
```

Useful for projectiles that are too fast for contact detection and too thick for a ray.

## Buffer sizing

If the buffer fills, the query stops early and returns the buffer length — results are valid but
incomplete. Size generously (these are stack bytes, not allocations) or re-run with a bigger
buffer when `count == buffer.Length`.

## Filtering

`QueryFilter.CategoryBits`/`MaskBits` work like shape filters: a shape is considered when
`(queryCategory & shapeMask) != 0 && (shapeCategory & queryMask) != 0`. The default filter matches
everything.

## Dynamic tree (standalone spatial index)

> 🎥 [Watch the showcase](https://www.youtube.com/watch?v=awPUUEsWGAg)

Everything above queries the **physics** world. `DynamicTree` exposes box3d's broadphase AABB tree
on its own, so you can build a fast "what's near here?" index over **your own non-physics data** —
frustum/interest culling, "which enemies are in range", trigger/AI volumes, spatial audio — reusing
the engine's cache-friendly tree instead of rolling a quadtree. It's independent of the physics
world and uses its own category/mask bits (not `CollisionFilter`).

Each proxy carries a 64-bit `userData` you set — typically an index or entity id you map back to
your own objects. It owns native memory, so dispose it (`using` or `Dispose`).

```csharp
using var tree = new DynamicTree();

// Insert proxies (fat AABBs) tagged with your own ids.
for (int i = 0; i < enemies.Count; i++)
    tree.CreateProxy(enemies[i].Bounds, userData: (ulong)i);

// "Who's near the player?" — buffer-fill, no allocation.
Span<DynamicTreeHit> hits = stackalloc DynamicTreeHit[32];
int n = tree.Query(playerAabb, hits);
for (int i = 0; i < n; i++)
    Consider(enemies[(int)hits[i].UserData]);

// As things move: re-fit (or EnlargeProxy when the box only grows), Rebuild occasionally.
tree.MoveProxy(proxyId, enemies[proxyId].Bounds);
```

`Query` reports proxies overlapping an AABB. `RayCast`/`BoxCast` sweep a ray or box and report the
proxies crossed — but the tree only knows **boxes, not shapes**, so these are a *broadphase* pass:
they hand you candidates (by `UserData`); run your own precise test against each. All three fill a
buffer and return the count (stopping early if it fills), with an `out TreeStats` overload for
profiling. `QueryClosest` (nearest-proxy refinement) is not yet wrapped.
