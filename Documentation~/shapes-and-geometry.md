# Shapes & geometry

Six shape types, created on a body via `body.Create*Shape(shapeDef, geometry)`. All creation goes
through a `ShapeDef` (from `ShapeDef.Default`) carrying density, friction/restitution material,
collision filter, and the event opt-in flags.

| Shape | Geometry | Dynamic? | Lifetime |
|---|---|---|---|
| Sphere | `Sphere { Center, Radius }` | yes | value, no management |
| Capsule | `Capsule { Center1, Center2, Radius }` | yes | value, no management |
| Convex hull | `BoxHull` or `Hull` | yes | **cloned** at creation |
| Triangle mesh | `TriangleMesh` | static only | **referenced** — must outlive shapes |
| Height field | `HeightField` | static only | **referenced** — must outlive shapes |
| Compound | `Compound` | static only | **referenced** — must outlive shapes |

There is no primitive box: boxes are convex hulls (`BoxHull.Create(hx, hy, hz)` — a stack-friendly
value type, cheap and idiomatic for the common case).

## Convex hulls

```csharp
// Common case — a box:
BoxHull box = BoxHull.Create(1f, 0.5f, 2f);          // half-extents
body.CreateHullShape(ShapeDef.Default, in box);

// From a point cloud (up to 64 vertices kept by default):
Hull hull = Hull.Create(points);                      // ReadOnlySpan<float3>
body.CreateHullShape(ShapeDef.Default, hull);
hull.Destroy();                                       // safe immediately: hulls are CLONED

// Generators: Hull.CreateCylinder / CreateCone / CreateRock
```

## Triangle meshes (level geometry)

```csharp
TriangleMesh mesh = TriangleMesh.Create(vertices, triangles);  // spans; indices 3 per triangle
Shape shape = ground.CreateMeshShape(ShapeDef.Default, mesh);  // optional float3 scale overload
// ... on teardown, order matters:
world.Destroy();   // or destroy the shapes
mesh.Destroy();    // THEN free the referenced data
```

`TriangleMesh.Create` rejects out-of-range indices and non-finite vertices with an argument
exception. A mesh with no usable triangles (including after welding), or with collision bounds
outside Box3D's supported range, returns an uncreated owner (`IsCreated == false`). Check that
result before attaching it to a body. These checks also avoid a mesh failure-path leak in older
native plugins.

Engine generators exist for quick setups: `TriangleMesh.CreateBox`, `TriangleMesh.CreateGrid`.
Meshes are one-sided and static-only; per-triangle materials are supported via
`ShapeDef.Materials`.

## Height fields (terrain)

```csharp
HeightField field = HeightField.Create(heights, countX, countZ, scale, minHeight, maxHeight);
ground.CreateHeightFieldShape(ShapeDef.Default, field);
```

Heights are row-major `countX × countZ` (`index = z * countX + x`; +X spans columns, +Z rows, from
a corner origin at the body position). `scale` is (cell size X, height multiplier, cell size Z) and
the default counter-clockwise winding faces the surface up. The min/max heights define the
quantization range — use identical values on adjacent fields that must line up seamlessly. Optional
per-quad material indices (`(countX-1) × (countZ-1)`, same ordering) flow into hit events, and
`HeightField.HoleMaterial` (0xFF) carves a quad out of the collision. Same referenced-lifetime rule
as meshes.

In the component layer, `Box3DTerrainShape` builds all of this straight from a Unity `Terrain` —
heightmap, size, and painted holes — see [Components](components.md).

## Compounds

Bake up to 64K spheres, capsules, hulls, and triangle meshes into one static shape:

```csharp
var spheres = new[] { new CompoundSphereInstance { Sphere = ..., Material = SurfaceMaterial.Default } };
var hulls   = new[] { new CompoundHullInstance { Hull = myHull, Transform = B3Transform.Identity,
                                                 Material = SurfaceMaterial.Default } };
var meshes  = new[] { new CompoundMeshInstance { Mesh = myMesh, Transform = B3Transform.Identity,
                                                 Scale = new float3(1f, 1f, 1f), // required; negative mirrors
                                                 Material = SurfaceMaterial.Default } };
Compound compound = Compound.Create(spheres, hulls: hulls, meshes: meshes);
myHull.Destroy();  // compounds bake their own copy of hull and mesh data
myMesh.Destroy();
body.CreateCompoundShape(ShapeDef.Default, compound);
```

The `Compound` itself is referenced by its shapes (destroy shapes first), but the *input* hulls and
meshes are copied during `Create`. Instances that share one `TriangleMesh` share a single baked copy,
so a forest built from one tree mesh costs one mesh plus a transform per tree. A mesh instance
carries one material, and its `Scale` must be set (zero is rejected).

### Serialization & streaming

A baked compound is one flat, self-contained blob: child shapes, materials, hull and mesh copies,
and the culling tree. That blob *is* the streaming format. Bake tiles offline, store the bytes, and
load them at run time without re-baking: loading validates the blob (an engine-version stamp and
the length) and fixes up a single internal pointer. Nothing is copied or rebuilt.

```csharp
// Bake time (editor / build pipeline):
using (var file = File.Create(path)) compound.WriteTo(file);   // straight from native memory
byte[] blob = compound.ToArray();                              // or one managed copy, e.g. for an asset

// Run time — pick the entry point that matches where the bytes live:
Compound tile = Compound.ReadFrom(stream);               // new native buffer, owned by the result
Compound tile = Compound.FromBytes(textAsset.bytes);     // managed bytes: one copy into a native buffer
Compound tile = Compound.FromNativeMemory(nativeArray);  // caller-owned native memory: zero-copy

body.CreateCompoundShape(ShapeDef.Default, tile);
// ... when the region unloads:
shape.Destroy();
tile.Destroy();   // frees ReadFrom/FromBytes buffers; after FromNativeMemory it only forgets yours
```

| Entry point | Copies | Buffer owner | Notes |
|---|---|---|---|
| `WriteTo(Stream)` | 0 (see below) | — | the compound stays usable afterwards |
| `WriteTo(Span<byte>)`, `ToArray()` | 1 | — | for assets or your own containers |
| `ReadFrom(Stream)` | 0 (see below) | the result | sized from the blob header; data after the blob is left alone |
| `FromBytes(ReadOnlySpan<byte>)` | 1 | the result | TextAsset / Addressables payloads |
| `FromNativeMemory(NativeArray<byte>)` | 0 | you | 8-byte aligned, unmoved until the compound and every shape using it are destroyed; one buffer may back several static bodies |

- A blob baked by another engine version (or corrupt data) yields a non-created compound
  (`IsCreated == false`) rather than an exception; a stream that ends early throws
  `EndOfStreamException`. Check `IsCreated` in streaming code and rebake stale caches.
- Serializing temporarily unlinks the compound's tree and relinks it before returning, so do not
  call `WriteTo` while a world that references the compound is stepping (the usual rule for every
  call into the engine).
- **Stream I/O under Unity's Mono.** `MemoryStream` and `UnmanagedMemoryStream` move bytes directly
  to and from native memory. `FileStream` in Unity's class library has no `Span` overloads, so the
  base `Stream` stages each call through a pooled buffer. The wrapper reads and writes in 256 KB
  chunks, which keeps that fallback inside the shared pool (no garbage), but it is one extra memcpy
  per chunk. For true zero-copy disk streaming, read the file into a `NativeArray<byte>` with
  Unity's `AsyncReadManager` and hand it to `FromNativeMemory`:

```csharp
// Zero-copy, asynchronous tile load (unsafe context; Unity.IO.LowLevel.Unsafe + Unity.Collections).
var bytes = new NativeArray<byte>((int)new FileInfo(path).Length, Allocator.Persistent);
var command = new ReadCommand { Offset = 0, Size = bytes.Length, Buffer = bytes.GetUnsafePtr() };
ReadHandle read = AsyncReadManager.Read(path, &command, 1);
// ... later, once read.Status == ReadStatus.Complete:
read.Dispose();
Compound tile = Compound.FromNativeMemory(bytes);   // bytes must outlive tile and its shapes
```

## Concave objects

Concavity splits by **static vs. dynamic**, as in every rigid-body engine:

- **Static concave geometry works directly.** Use a [triangle mesh](#triangle-meshes-level-geometry)
  (or [height field](#height-fields-terrain)) for level geometry, terrain, or any arbitrary concave
  surface. Mesh shapes only generate contacts against *static* bodies — so this is for the world, not
  for something you push around.
- **Dynamic (moving) concave objects are built from convex pieces.** A moving body collides using
  convex shapes only (sphere / capsule / box / hull), so a moving concave object is **several convex
  shapes on one body**: in the code API, several `Create*Shape` calls on the same `Body`; in the
  [component layer](components.md), several shape components under one `Box3DBody` (they gather
  into one multi-shape body, mirroring Unity's own multi-collider Rigidbody). The baked
  [`Compound`](#compounds) is not for this — it is static-only, built for large amounts of level
  geometry.

There is no built-in convex decomposition: author the convex pieces yourself, or precompute them with
a tool such as [V-HACD](https://github.com/kmammou/v-hacd) and add one hull per piece.

## Materials, filters, and flags on ShapeDef

- `BaseMaterial` — friction, restitution, rolling resistance, `TangentVelocity` (conveyor belts!),
  and a `UserMaterialId` that flows through hit events and mixing callbacks.
- `Filter` — category/mask bits and group index (negative group = members never collide; the
  package cloth sample uses this to disable self-collision).
- Event opt-ins — `EnableContactEvents`, `EnableHitEvents`, `EnableSensorEvents`,
  `EnablePreSolveEvents`, `EnableCustomFiltering`: **all false by default.** If events aren't
  arriving, this is why. See [Events](events.md).

## Runtime changes

`shape.SetSphere(...)` / `SetCapsule(...)` replace geometry in place. Mass is recomputed on shape
creation/destruction unless you opt out (`Destroy(updateBodyMass: false)`), or manage it manually
via `body.SetMassData`.
