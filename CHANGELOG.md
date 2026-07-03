# Changelog

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
- Samples: basic simulation, joints, mouse drag, character controller, vehicle, PhysX benchmarks.
- 60+ edit-mode tests: ABI/layout guards, native-defaults round-trips, behavioral simulation tests.
