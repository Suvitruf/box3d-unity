# Changelog

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
