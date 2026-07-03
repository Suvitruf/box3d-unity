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

Heights are row-major `countX × countZ`. The min/max heights define the quantization range — use
identical values on adjacent fields that must line up seamlessly. Same referenced-lifetime rule as
meshes.

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
