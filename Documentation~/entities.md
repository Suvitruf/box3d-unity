# ECS / DOTS integration

The `Box3D.Entities` assembly runs Box3D inside Unity's ECS: entities carry blittable body ids as
component data, the simulation is stepped from a system group, and moved bodies are written back to
`LocalTransform` through the engine's move-event array in Burst-compiled code. The native solver
does the physics; Burst removes the managed overhead around it — the per-entity sync loops, and
the step call itself (see [Burst and the native boundary](#burst-and-the-native-boundary) for
exactly where that line runs today).

The assembly is optional: it compiles only when `com.unity.entities` (1.0+) is installed in the
project. Without the package the rest of the wrapper works as before.

The **ECS (experimental)** sample (Package Manager ▸ Samples) is guarded the same way. Without
Entities its assembly is skipped silently and the sample scene reports a missing script on the
sandbox object ("The referenced script (Unknown) on this Behaviour is missing!"); install
`com.unity.entities` and the reference resolves on the next compile.

## How it maps

| ECS side | Physics side |
|---|---|
| `Box3DWorldSingleton` | The native `World`, plus step settings |
| `Box3DBodyDefinition` + `Box3DShapeDefinition` | What to create — body type and one shape |
| `Box3DBodyRef` (cleanup component) | The live native `Body` behind the entity |
| `LocalTransform` | Spawn pose in, simulated pose out |

Box3D owns the simulation state; the ECS components are a mirror. Each entity's `Entity` id rides
in the native body's user data, so a `BodyMoveEvent` routes straight back to its entity without any
lookup table.

Four systems run inside `FixedStepSimulationSystemGroup`, in order:

1. **`Box3DWorldSystem`** — creates the native world (and singleton) lazily when the first body
   definition appears; destroys it with the ECS world.
2. **`Box3DBodyLifecycleSystem`** — creates a native body + shape for every entity that has the
   definition components but no `Box3DBodyRef`; destroys the native body of every entity whose
   definitions are gone. Because `Box3DBodyRef` is a *cleanup* component, plain
   `EntityManager.DestroyEntity` is enough — the native body is freed on the next update.
3. **`Box3DStepSystem`** (Burst) — steps the world with the group's fixed delta time (or the
   singleton's `TimeStep` override).
4. **`Box3DWriteBackSystem`** (Burst) — walks `GetBodyMoveEvents()` and writes position/rotation to
   each moved entity's `LocalTransform`. Scale is left alone; sleeping bodies produce no events and
   cost nothing.

## Spawning bodies from code

```csharp
Entity entity = entityManager.CreateEntity();
entityManager.AddComponentData(entity, Box3DBodyDefinition.Dynamic);
entityManager.AddComponentData(entity, Box3DShapeDefinition.Sphere(0.5f));
entityManager.AddComponentData(entity, LocalTransform.FromPosition(0f, 5f, 0f));
// next fixed update: native body exists, entity falls, LocalTransform follows
```

Start from the factories (`Box3DBodyDefinition.Dynamic/Kinematic/Static`,
`Box3DShapeDefinition.Box/Sphere/Capsule`) — a default-constructed definition has zero gravity
scale and zero friction. The shape kinds map to the same geometry the main API uses: box hull,
sphere, Y-axis capsule.

In a subscene, add the **`Box3DBodyAuthoring`** component instead — its baker emits the same
definition components, and the entity's baked `LocalTransform` becomes the spawn pose.

## Burst and the native boundary

Burst refuses to P/Invoke a function that passes or returns a **struct by value** (error BC1064) —
and box3d's C API passes its id structs by value everywhere. The wrapper solves this in two layers:

- **`World.Step`** routes through a primitive redeclaration of the same entry point (`b3WorldId`
  is 4 bytes — passed identically to a `uint` on every 64-bit ABI the package ships).
- **Everything else Burst needs** goes through the `b3u_*` glue exports — tiny pointer-based
  wrappers compiled into the native library from `Box3D.Native~/glue/` (the box3d source itself is
  untouched). This makes the following **Burst-callable**, from systems and jobs alike:
  - `World.Step`, all four event getters (`GetBodyMoveEvents`, `GetContactEvents`,
    `GetSensorEvents`, `GetJointEvents`);
  - `World.CastRay` (buffer form), `World.OverlapAABB`, and
    `World.CastRayClosest(origin, translation, filter, out result)`;
  - `Body.GetTransform(out …)` / `SetTransform(in …, in …)`, the `LinearVelocity` /
    `AngularVelocity` properties, and `ApplyLinearImpulse(in …)` / `ApplyLinearImpulseToCenter(in …)`.

  The `in`/`out` forms exist because their by-value originals can't cross the Burst boundary;
  everything not listed (shape casts, mover, joints, diagnostics…) still works — from managed code,
  as before. Build `QueryFilter.Default` and the def structs managed-side and pass them into jobs:
  the `b3Default*` factory externs return structs by value and are not Burst-callable.

The four ECS systems run fully under Burst (world creation and body lifecycle stay managed by
design — they're structural, not per-frame-per-entity). The test suite pins all of this with
`[BurstDiscard]` canaries that fail if a job silently falls back to managed.

**Native compatibility:** the glue ships inside the normal plugin binaries (glue version 1;
`World.Create` probes it and logs a clear error if a stale binary is loaded). Rebuilding natives
via the `Box3D.Native~` scripts or CI produces glued binaries automatically. Stay off the
callback-based APIs (custom filtering, pre-solve, debug draw) inside Burst code: they go through
managed delegates.

## Current limits

- **One shape per entity**, primitive kinds only (box / sphere / capsule). Meshes, height fields,
  compounds and joints still go through the main API — grab `Box3DBodyRef.Value` and use it; the
  write-back works for any body whose user data carries the entity id.
- **One-way sync.** ECS → physics happens once, at creation. Moving a kinematic body means driving
  it through the `Body` API (velocities or teleports), not by editing `LocalTransform`.
- **The write-back runs single-threaded** (one Burst loop over moved bodies). Parallelizing it and
  handing box3d's task system to the Unity job scheduler are the natural next steps if profiling
  ever demands them.
- **64-bit only** — the entity id rides in a pointer-sized user-data slot. All shipped native
  platforms are 64-bit.
