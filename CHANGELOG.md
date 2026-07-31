# Changelog

## [0.8.1] — 2026-07-31

Maintenance release: a full performance & lifetime audit of the wrapper, with every finding fixed.

### Fixed — correctness & leaks
- **Destroying a shape component individually now destroys its native shape** (and re-derives the
  body's mass). Previously the collision stayed solid in the simulation — a ghost collider — and
  mesh/terrain geometry leaked. Geometry release is idempotent, so any body/shape teardown order
  is safe, and shapes now free their geometry even when the world was destroyed first.
- **Custom friction/restitution mixers survive unrelated world destroys.** Destroying any world
  used to clear the global mixing callbacks for all worlds, silently reverting live worlds to
  engine-default mixing. Only the registering world's death clears them now.
- `DynamicTree` gained a finalizer backstop — a leaked (undisposed) tree no longer leaks its
  native node pool permanently. `Dispose` remains the correct path.
- `Box3DWater` frees a destroyed terrain's GPU height grid immediately (previously it lingered
  until a new terrain came into view); `Box3DStatsHud` no longer auto-respawns a physics world if
  the world it was watching dies.
- Samples clean up their runtime-created materials and meshes; the Playground checker texture now
  actually shows on spawned objects (it was applied to a material instance that got replaced).
- `Hull`/`TriangleMesh`/`HeightField`/`Compound`/`Recording`/`ReplayPlayer` docs now spell out the
  copy rule: struct copies share one native pointer — `Destroy` exactly once, through one copy.

### Changed — performance
- **`Box3DWaterVolume` caches shape volumes** instead of running `ComputeMassData` (a full inertia
  integral) per submerged shape per step — the biggest runtime win of the audit. Wave sampling in
  the buoyancy loop also stops re-reading the transform per shape.
- **`Determinism.HashState` halves its native calls** (one `GetTransform` + one hash per body);
  the produced hash stream is bit-identical to before.
- Water sim & renderer use pre-resolved shader property ids (no per-call string hashing) and only
  touch the refraction keyword/blend state when it changes.
- Debug draw builds AABB/box corners on the stack — zero GC with Bounds drawing enabled.
- Replayers cap post-hitch catch-up (no unbounded native step bursts); the visual replayer resolves
  body handles once at mapping time and reads one transform per body per frame; rope endpoints,
  wind and waterfall loops each drop redundant per-iteration native/transform calls.
- Editor tooling stops busy-looping: body-inspector contacts snapshot once per frame with the
  scene-view pass gated to repaints, the collision debugger diagnoses on a 5 Hz throttle instead
  of every IMGUI pass, the rope editor throttles mid-drag re-settling (~6 Hz, exact on release),
  and inspectors/gizmos cache serialized properties, scene scans and reflection lookups.
- Stats HUD builds its text once per frame; sample GUIs reuse their `GUIStyle`s.

### Docs
- Performance page: notes the one timing asymmetry in the PhysX comparison (PhysX's transform
  write-back is inside its timed call; Box3D's move-event sync is outside).

## [0.8.0] — 2026-07-29

### Added — water
- **`Box3DWater`** — GPU particle water: a position-based fluid (density constraint + XSPH
  viscosity + cohesion) simulated in compute shaders, colliding with the Box3D world every step.
  Spheres, capsules, boxes and terrain height fields collide **exactly**; other complex shapes
  contribute the top face of their bounding box while it is plate-like (floors, road decks) so
  sprawling meshes never dam or crush the fluid. **Two-way coupling** pushes dynamic bodies back
  (props float, bob and get carried) with submersion-scaled drag. Includes whitewater/foam
  tracking and a per-particle buffer API for custom renderers.
- **`Box3DWaterRenderer`** — screen-space liquid surface for URP: depth-aware blurred surface,
  thickness-based absorption, refraction (opaque texture), reflection, foam and spray streaks,
  soft shore blending. Needs a URP camera with Depth Texture.
- **`Box3DWaterfall`** — emits a particle stream into a `Box3DWater` from an aimable lip:
  waterfalls, taps, fountains. Recycles the water's oldest particles on a fixed budget.
- **`Box3DWind` → water** — **Water Influence** lets a wind zone grip the water surface (and its
  foam/spray), fading to nothing in the bulk.
- **`Box3DWaterVolume`** — analytic buoyancy volume for pool-style game water: Archimedes buoyancy
  at the center of submersion (floaters right themselves), depth-scaled drag, currents, fill
  level, deterministic sine waves (`SampleSurfaceY`), entry slap, and `BodyEntered`/`BodyExited`
  events for splashes.
- **Samples**: *Physics Water* (particle water pool with a waterfall, wind and floating props) and
  *Water Pool* (buoyancy volume test bed) — both registered in the Package Manager.

### Added — shapes & tooling
- **`Box3DTerrainShape`** — a height-field shape built straight from a Unity Terrain (like
  TerrainCollider): heightmap downsampling via **Sample Stride**, painted terrain holes carved out
  of collision via **Apply Holes**. Static bodies only.
- **`Box3DDeformable`** — dents the rendered mesh where impacts land (radius, strength, max depth),
  with optional healing over time and an opt-in collision rebuild from the dented vertices.
- **Box3D Physics Simulation** Scene-view tool — run live physics on the selected bodies in edit
  mode while the rest of the scene stays put: drag props with the mouse, settle them, keep or
  cancel the poses.

### Fixed
- `Samples~`/`Documentation~` folders are no longer hidden from git by a global `*~` ignore
  pattern (repo-level `!*~/` un-ignore).
- GameObject menu: the Water Volume and Water entries no longer collide (duplicate method).

## [0.7.2] — 2026-07-28

### Added — opt-in double precision (large worlds)
- **`BOX3D_DOUBLE`** scripting define enables Box3D's double-precision mode: world **positions**
  widen to double (accurate far beyond float's ~16 km limit) while velocities, rotations and local
  geometry stay float. Single precision remains the default and is unchanged. Full guide:
  [double precision](Documentation~/double-precision.md).
- **New real types `B3Pos` and `B3WorldTransform`** carry world positions/transforms and follow the
  define: conversions *into* them are implicit (widening), *out* are explicit casts in double mode
  (lossy narrowing). In single precision they are layout- and source-compatible with the previous
  `float3`/`B3Transform` API.
- **Double native libraries ship alongside the single ones** (`box3d_d.dll`, `libbox3d_d.so`,
  `libbox3d_d.dylib`, Android `libbox3d_d.so`) — the define selects the right one by name at
  runtime. iOS/WebGL link statically and stay single precision unless you embed the package and
  swap the archive (see the guide's iOS/WebGL section).
- **Triple mismatch protection**: Box3D's `b3CreateWorld` precision tripwire, a two-way runtime
  assert at init (`b3IsDoublePrecision()` vs the define), and a test that fails loudly in CI.
- Build tooling: every `Box3D.Native~` build script takes `BOX3D_DOUBLE=1`; the CI workflow gained a
  `precision` input (`single` / `double` / `both`).

### Changed
- Position-carrying APIs (`Body.Position` / `Body.Transform`, `BodyDef.Position`,
  `BodyMoveEvent.Transform`, `RayResult.Point`, `ContactHitEvent.Point`, `ExplosionDef.Position`,
  apply-at-point forces/impulses, debug-draw callbacks) now use `B3Pos` / `B3WorldTransform`.
  Source-compatible in single precision via implicit conversions.
- `Determinism.HashState` hashes positions at native width (single-precision hash values are
  unchanged). Hashes and **recordings (`.rec`) are precision-specific** — a recording made in one
  precision won't replay in the other; the replayers now say so when a load fails.

## [0.7.1] — 2026-07-27

### Added
- **`DynamicTree`** — box3d's broadphase AABB tree exposed as a standalone spatial index for your own
  (non-physics) data: a fast "what's near here?" over thousands of moving objects — AI perception,
  interest management, trigger volumes, culling — without spinning up colliders. Insert proxies
  (each with a 64-bit `userData`), `MoveProxy` them as things move, and query by region (`Query`),
  ray (`RayCast`) or swept box (`BoxCast`) — buffer-fill, allocation-free, with `out TreeStats`
  overloads. Introspection (`ProxyCount` / `Height` / `RootBounds` / `ByteCount` / `Rebuild`) and
  `Validate`. `IDisposable` (owns native memory). See
  [Dynamic tree](Documentation~/queries.md#dynamic-tree-standalone-spatial-index) and the
  [showcase video](https://www.youtube.com/watch?v=awPUUEsWGAg). (`QueryClosest` is not yet wrapped.)

## [0.7.0] — 2026-07-22

### Changed — naming: Box3d → Box3D
- Everything named `Box3d…` is now `Box3D…` (capital D): assemblies (`Box3D.Runtime`, `Box3D.Hybrid`,
  `Box3D.Hybrid.Editor`, `Box3D.Tests`), namespaces (`Box3D`, `Box3D.Hybrid`), every component class
  (`Box3DBody`, `Box3DBoxShape`, …) and every file. **Breaking for code** — update `using Box3d…`
  and `Box3d…` type references to `Box3D…`. **Scenes and prefabs are unaffected**: scripts are
  referenced by GUID and every `.meta` was preserved through the rename. The package id
  (`com.suvitruf.box3d`) and the native library (`box3d`) stay lowercase by requirement.

### Added — scene authoring for designers
- **GameObject → Box3D creation menu** (also in the Hierarchy **+** button and right-click menu):
  World, Box, Sphere, Capsule, Empty Body, Static Box and Ground Plane. Shape items create a Unity
  primitive for visuals (its PhysX collider removed) with a `Box3DBody` + matching Box3D shape —
  primitive dimensions match the shape defaults, so what you see is what simulates.
- **Shapes auto-add a body**: adding a shape component to a GameObject with no `Box3DBody` on it or
  any ancestor adds one automatically (like `RequireComponent`, but hierarchy-aware — compound child
  shapes under a body don't get their own). Set the body to **Static** for non-moving geometry.
- **Component icons**: every component now has a category-colored icon (green shapes, orange joints,
  blue body, purple world, red replay, teal diagnostics) in the Add Component menu, Inspector,
  Project window and Hierarchy.
- **Add Component menu categories**: components are grouped under `Box3D/` — `Shapes/`, `Joints/`,
  `Replay/`, `Diagnostics/`, with `Body` and `World` at the top level.
- **Gravity gizmo**: selecting a `Box3DWorld` draws a purple gravity arrow in the Scene view —
  direction is the gravity vector, length its strength (1 g ≈ 1.5 m, clamped for readability).
- **Force components** (Add Component → `Box3D/Forces`, GameObject → Box3D): **`Box3DWind`** — a
  box volume pushing dynamic bodies along its forward axis with optional Perlin gusts, visualized
  as a zone + arrow grid that tracks live gust strength; **`Box3DExplosion`** — a radial impulse
  burst (native `World.Explode`) with radius/falloff gizmos, an Inspector **Explode** button and
  **Explode On Enable** for spawned prefabs.
- **`Box3DRope`** — Source 2-style cables (`Box3D/Rope`, GameObject → Box3D → Rope): the Scene view
  shows the true drape live while editing — the preview runs a real Box3D simulation in a throwaway
  world with the scene's shapes frozen as static collision (draggable far-end handle, an animated
  editor **Simulate** toggle), then **Bake** freezes the curve into a static
  cable with optional static collision — or leave it **Dynamic** and it builds capsule segments +
  ball joints at runtime, attaching to any `Box3DBody` at its endpoints, spawning taut and sagging
  into place so it drapes onto scene objects instead of spawning through them. Segments are continuous
  (bullet) bodies honoring the layer collision matrix, so the rope reacts to everything it sweeps
  past; it ignores collision with its attached bodies by default (filter joints; **Collide With
  Attached** re-enables it). Renders through a LineRenderer whose width follows the rope Radius.

### Added — event id → wrapper resolution ([#2](https://github.com/Suvitruf/box3d-unity/issues/2))
- **`new Body(id)` / `new Shape(id)` / `new Joint(id)`** — the documented way back into the wrapper
  API from the raw ids that move/contact/sensor/joint events deliver. No parallel lookup table
  needed: the wrappers are thin value types over the ids, so wrapping an id *is* the resolution.
- **`World.TryGetBody` / `TryGetShape` / `TryGetJoint`** — the validated form: false for stale ids
  and for ids belonging to a different world. New "[Resolving event ids](Documentation~/events.md)"
  section in the events doc.

### Fixed
- **Inspector edits now apply live during play** across the component layer: `Box3DWorld` gravity,
  shape friction/restitution/density and sphere/capsule size (mass is re-derived), and every
  joint's limits, motor and spring parameters — `OnValidate` pushes them to the native objects,
  matching `Box3DBody` which already did this. Creation-baked state still can't change on a live
  object: box/hull/mesh geometry, joint axes/anchors/connected bodies, and the world's worker count.

## [0.6.2] — 2026-07-22

### Added
- **Determinism state hashing** — `Determinism.HashState(bodies)` / `Determinism.Hash(bytes)` wrap
  box3d's own state hash, giving lockstep/rollback games a per-step checksum to exchange and compare.
  Ships with an example lockstep test and a new doc:
  [determinism testing](Documentation~/determinism-testing.md).
- **`Box3DDeterminismHarness`** — a cross-platform determinism probe: build the same seeded scene to
  Editor (x64/Mono), Android (arm64/IL2CPP) and WebGL (WASM), and compare the on-screen hash
  signature (platform/backend + checkpoints at 25/50/75%/final) to see whether box3d's floating-point
  results match across platforms.
- **Collision Debugger** (Window ▸ Box3D ▸ Collision Debugger) — assign two `Box3DBody` and get a
  rule-by-rule verdict of why they aren't colliding: body types, enabled state, joint
  Collide Connected, category/mask/group filters, sensors, and broadphase-AABB proximity — mirroring
  box3d's own collision rules. The logic is reusable at runtime via `CollisionDiagnostics.Diagnose`.
- New managed accessors: `Shape.GetFilter` / `SetFilter` / `IsSensor` / `GetAABB` / `GetBody`,
  `Body.Type` / `IsEnabled` / `GetShapeCount` / `GetJointCount`,
  `Joint.BodyA` / `BodyB` / `CollideConnected`.

### Changed
- `Shape.GetBody()` now returns a `Body` (previously the raw `BodyId`).
- `Body.IsEnabled` is now a property (previously a generated `IsEnabled()` method).

## [0.6.1] — 2026-07-13

### Added
- **`Box3DVisualReplayer`** — plays a recording back on the scene's **real GameObjects** (meshes and
  materials), not wireframes. It pauses live physics and drives each recorded body onto its scene
  object, mapped by **body name**, with the same scrub timeline as `Box3DReplayer`. Use it in the
  scene the recording was made in; unmatched bodies (e.g. the joint world anchor) are skipped.
- **`Body.SetName()` / `GetName()`** — and `Box3DBody` now names its native body after the GameObject,
  so names appear in recordings/debug output and drive the visual replayer's mapping.

## [0.6.0] — 2026-07-12

### Added — determinism & replay
- **Record / validate / replay** — capture a simulation and prove it reproduces bit-identical state,
  the tooling for lockstep/rollback netcode, authoritative server physics, and reproducing
  intermittent bugs. No other Unity physics wrapper ships this.
- **`Box3DRecorder`**: a drop-in component that records the world and reports **`DETERMINISTIC`** /
  **`DIVERGED`** — including a cross-thread option (replay at a different worker count) to verify
  box3d reproduces identically regardless of thread count. Warns if the recorded world is empty.
- **`Box3DReplayer`** + scrub timeline: plays back a `.rec` file (or live capture) in its own replay
  world, debug-drawn, with an Inspector timeline (frame slider, transport, live divergence read-out).
- **Low-level API**: `Recording` (Create / Save / Load / GetData / **ValidateReplay**),
  `World.StartRecording` / `StopRecording`, and `ReplayPlayer` (StepFrame / SeekFrame / Restart,
  HasDiverged / DivergeFrame, GetInfo, per-body access, the replayed `World`, and `EnableShapeDrawing`).
- Documentation: [determinism & replay](Documentation~/determinism-and-replay.md).

## [0.5.0] — 2026-07-11

### Added — diagnostics & debug tooling
- **Debug-draw overlay on `Box3DWorld`**: a `DebugDrawFlags` mask in the Inspector overlays collision
  shapes, joints, contacts, normals/forces, AABBs, mass, islands and graph colors into the Scene view —
  no code needed.
- **`Box3DStatsHud`**: a drop-in on-screen overlay — FPS, step time, awake/total body count, live
  shape/contact/joint/island counts and memory, and a per-phase step-time breakdown.
- **`World.GetProfile()` / `World.GetCounters()`**: public `Profile` (per-phase step timings) and
  `Counters` (live counts + allocator/broadphase stats) for programmatic profiling. Plus
  `GetAwakeBodyCount()`.
- **`out TreeStats` query overloads** on ray/overlap/shape casts — the broadphase nodes a query visited
  (its spatial cost), previously discarded.

## [0.4.2] — 2026-07-10

### Fixed
- Component joints now suppress collision between overlapping connected bodies. box3d applies
  `CollideConnected` only when *creating* a contact, not one that already exists — so with the
  component layer's deferred activation a stale wheel-inside-chassis contact could persist and crush
  the bodies apart with huge force, pinning driven wheels. Joints now clear it on creation. This makes
  motor-driven component vehicles work with normal (overlapping) wheel placement.

## [0.4.1] — 2026-07-09

### Added
- Pyramid stress-test scene in the Benchmarks sample — Erin Catto's 16,290-box pyramid, one box deep,
  held stable by contact recycling. Throw spheres to smash it, adjust the worker-thread count, and
  watch live step/FPS/object metrics (CSV export). ([demo](https://www.youtube.com/watch?v=BtdMbw97Zds))
- `Box3DWheelJoint.Native` — accessor to the underlying native wheel joint.

## [0.4.0] — 2026-07-08

### Added
- Contact manifold data — query live contacts via `Body.GetContacts()`, `Shape.GetContacts()` and
  `Contact.GetData()`: contact points, world normal, separation, and solver impulses
  (`TotalNormalImpulse` for impact strength, `NormalVelocity`, per-triangle index, and a
  new-vs-resting `Persisted` flag). New `ContactData` / `Manifold` / `ManifoldPoint` types; the
  transient native manifold pointer is copied into managed memory so results are safe to keep.

## [0.3.2] — 2026-07-07

### Added
- macOS (universal, minimum macOS 11.0) and iOS (arm64 static, minimum iOS 13.0) native binaries —
  the package now ships all six platforms.

### Fixed
- Android native library is now 16 KB page-aligned, as required by Android 15 and Google Play.

### Documentation
- Added a "Concave objects" section (static triangle meshes vs. dynamic compounds of convex shapes).

## [0.3.1] — 2026-07-06

### Added
- Motor and wheel joint components, completing all nine joint types as components.
- `Box3DShape.SetDensity`, `Box3DBody.AllowFastRotation`, joint `WakeBodies`, and runtime
  `Configure` helpers on the wheel and parallel joints.

### Fixed
- Joint anchors on scaled GameObjects: world→body-local conversion no longer divides by lossyScale
  (box3d bodies are unscaled), so joints on a scaled body anchor in the right place.

## [0.3.0] — 2026-07-05

### Added — component layer (author physics in the Inspector)
- **Bodies & shapes**: `Box3DWorld`, `Box3DBody` (static/kinematic/dynamic, enable-disable, live
  type/material edits), and all five shape types — sphere, box, capsule, convex hull, triangle mesh.
- **Compound & static colliders**: bodies gather child shapes into one compound body; a shape with
  no body becomes static automatically.
- **Collision layers**: shapes honor the GameObject layer and Unity's Layer Collision Matrix.
- **Joints** (seven of nine as components): hinge, ball, slider, distance, fixed, parallel, filter.
  Motor and wheel remain code-API.
- **Editor experience**: shape gizmos; draggable Scene-view handles for shape sizes and joint
  anchors; joint inspectors that hide unused fields; a live body read-out during play; and a
  mesh Read/Write warning.

## [0.2.1] — 2026-07-04

### Added
- Component layer: capsule, hull (convex, from a mesh), and mesh (static, from a mesh) shape
  components, completing all five shape types.
- Shapes honor the GameObject's Unity layer and the Layer Collision Matrix.

## [0.2.0] — 2026-07-04

### Added
- **Experimental component layer** (`Box3D.Hybrid`): author physics in the Inspector with
  `Box3DWorld`, `Box3DBody`, `Box3DSphereShape`, and `Box3DBoxShape`, mirroring Unity's
  Rigidbody/Collider model — static/kinematic/dynamic bodies, enable/disable, live type and
  material edits, runtime `Position`/`Rotation`. Sphere and box shapes only for now.
- WebGL native binary (static wasm), joining Windows, Linux, and Android.
- Components sample scene + documentation.

### Fixed
- Native-safety guards (double-destroy, geometry argument checks, debug-draw exception barriers).
- Magenta materials in player builds; Linux editor plugin; non-URP / missing-Input-System sample imports.

### Changed
- API consistency: equality operators and `IsValid` on all wrappers; `Body.AngularVelocity`.

## [0.1.0] — 2026-07-03

First public release. Wraps Box3D v0.1.0 (commit 29bf523).

### Added
- Full C API bindings (578 functions) generated from the Box3D headers, with a public C# layer:
  `World`/`Body`/`Shape`/`Joint` + typed joints as value handles over generation-validated ids.
- All shape types: sphere, capsule, convex hull (+ builders), triangle mesh, height field, compound.
- All nine joint types with complete accessor surfaces and creation defs.
- Polled events (body move, contact begin/end/hit, sensor, joint) as zero-copy spans.
- Allocation-free queries: ray casts (closest/all), shape casts, AABB/shape overlaps.
- Character mover toolkit (`CollideMover`/`CastMover`/`SolvePlanes`/`ClipVector`).
- Custom filter / pre-solve / material-mixing callbacks with worker-thread safety handling.
- Debug-draw bridge (shapes, joints, contacts, islands → Scene view lines).
- Explosions, wind, conveyor materials, per-axis motion locks, multithreading (worker count).
- Native binaries: Windows x64, Linux x64, Android arm64-v8a. macOS/iOS build scripts included.
- Samples: interactive playground, basic simulation, joints, mouse drag, character controller,
  vehicle, PhysX benchmarks.
- 60+ edit-mode tests: ABI/layout guards, native-defaults round-trips, behavioral simulation tests.
