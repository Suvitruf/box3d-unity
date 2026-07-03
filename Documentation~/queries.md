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
