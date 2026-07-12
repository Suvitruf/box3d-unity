# Changelog

## [0.6.0] — 2026-07-12

### Added — determinism & replay
- **Record / validate / replay** — capture a simulation and prove it reproduces bit-identical state,
  the tooling for lockstep/rollback netcode, authoritative server physics, and reproducing
  intermittent bugs. No other Unity physics wrapper ships this.
- **`Box3dRecorder`**: a drop-in component that records the world and reports **`DETERMINISTIC`** /
  **`DIVERGED`** — including a cross-thread option (replay at a different worker count) to verify
  box3d reproduces identically regardless of thread count. Warns if the recorded world is empty.
- **`Box3dReplayer`** + scrub timeline: plays back a `.rec` file (or live capture) in its own replay
  world, debug-drawn, with an Inspector timeline (frame slider, transport, live divergence read-out).
- **Low-level API**: `Recording` (Create / Save / Load / GetData / **ValidateReplay**),
  `World.StartRecording` / `StopRecording`, and `ReplayPlayer` (StepFrame / SeekFrame / Restart,
  HasDiverged / DivergeFrame, GetInfo, per-body access, the replayed `World`, and `EnableShapeDrawing`).
- Documentation: [determinism & replay](Documentation~/determinism-and-replay.md).

## [0.5.0] — 2026-07-11

### Added — diagnostics & debug tooling
- **Debug-draw overlay on `Box3dWorld`**: a `DebugDrawFlags` mask in the Inspector overlays collision
  shapes, joints, contacts, normals/forces, AABBs, mass, islands and graph colors into the Scene view —
  no code needed.
- **`Box3dStatsHud`**: a drop-in on-screen overlay — FPS, step time, awake/total body count, live
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
- `Box3dWheelJoint.Native` — accessor to the underlying native wheel joint.

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
- `Box3dShape.SetDensity`, `Box3dBody.AllowFastRotation`, joint `WakeBodies`, and runtime
  `Configure` helpers on the wheel and parallel joints.

### Fixed
- Joint anchors on scaled GameObjects: world→body-local conversion no longer divides by lossyScale
  (box3d bodies are unscaled), so joints on a scaled body anchor in the right place.

## [0.3.0] — 2026-07-05

### Added — component layer (author physics in the Inspector)
- **Bodies & shapes**: `Box3dWorld`, `Box3dBody` (static/kinematic/dynamic, enable-disable, live
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
- **Experimental component layer** (`Box3d.Hybrid`): author physics in the Inspector with
  `Box3dWorld`, `Box3dBody`, `Box3dSphereShape`, and `Box3dBoxShape`, mirroring Unity's
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

First public release. Wraps Box3d v0.1.0 (commit 29bf523).

### Added
- Full C API bindings (578 functions) generated from the Box3d headers, with a public C# layer:
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
