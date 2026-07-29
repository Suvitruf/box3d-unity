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

Bake up to 64K spheres/capsules/hulls into one static shape:

```csharp
var spheres = new[] { new CompoundSphereInstance { Sphere = ..., Material = SurfaceMaterial.Default } };
var hulls   = new[] { new CompoundHullInstance { Hull = myHull, Transform = B3Transform.Identity,
                                                 Material = SurfaceMaterial.Default } };
Compound compound = Compound.Create(spheres, hulls: hulls);
myHull.Destroy();  // compounds bake their own copy of hull data
body.CreateCompoundShape(ShapeDef.Default, compound);
```

The `Compound` itself is referenced by its shapes (destroy shapes first), but the *input* hulls
are copied during `Create`.

## Concave objects

Concavity splits by **static vs. dynamic**, as in every rigid-body engine:

- **Static concave geometry works directly.** Use a [triangle mesh](#triangle-meshes-level-geometry)
  (or [height field](#height-fields-terrain)) for level geometry, terrain, or any arbitrary concave
  surface. Mesh shapes only generate contacts against *static* bodies — so this is for the world, not
  for something you push around.
- **Dynamic (moving) concave objects are built from convex pieces.** A moving body collides using
  convex shapes only (sphere / capsule / box / hull), so a moving concave object is a **compound of
  convex parts** — either a baked [`Compound`](#compounds) in the code API, or, in the
  [component layer](components.md), several shape components under one `Box3DBody` (they merge into
  one compound collider, mirroring Unity's own multi-collider Rigidbody).

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
