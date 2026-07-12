# Box3d for Unity

Unity bindings for [Box3d](https://github.com/erincatto/box3d) — the 3D physics engine by
Erin Catto, author of Box2D. The full native C API (~580 functions) is exposed through a
Unity-friendly C# layer: no pointers in user code, no per-frame allocations, deterministic
simulation, and excellent multithreaded performance.

![Playground](https://projects.cdn.aapanasik.com/box3d-unity/unity-box3d.gif)

**[▶ Try the live WebGL demo](https://suvitruf.github.io/box3d-unity/)**

This project was inspired by, and owes its architecture to, two projects:

- **[Box3d](https://github.com/erincatto/box3d)** by Erin Catto — the engine itself. This package
  is only a thin, faithful wrapper around it; all the hard physics work is his.
- **[JoltPhysicsUnity](https://github.com/seep/JoltPhysicsUnity)** inspired the way this wrapper is implemented. Its binding-generation pipeline and packaging decisions shaped
  mine (what I deliberately did differently, is covered in the docs).

> **Status: early (0.3.x).** Box3d itself is a young engine (v0.1) with no API stability
> guarantees yet, and this wrapper tracks it. It is usable and well-tested, but expect breaking
> changes on engine updates.

## Features

- **World & bodies** — static/kinematic/dynamic bodies, full body API (velocities, forces, mass,
  damping, per-axis motion locks, bullets/CCD, gravity scale).
- **Shapes** — sphere, capsule, convex hull (with cylinder/cone/rock/point-cloud builders),
  triangle mesh, height field, compound. Clear, documented geometry lifetime rules. Concave objects:
  triangle meshes for static geometry, compounds of convex parts for dynamic bodies
  ([details](Documentation~/shapes-and-geometry.md#concave-objects)).
- **Joints** — all nine box3d joints: distance, motor, filter, parallel, prismatic, revolute,
  spherical (cone/twist limits), weld, wheel (suspension + steering + drive) — each with its full
  spring/limit/motor accessor surface.
- **Events** — polled contact begin/end/hit, sensor enter/exit, joint force-threshold and body-move
  events, delivered as zero-copy spans (the compiler enforces the transient-memory rule).
- **Queries** — closest-hit and all-hits ray casts, shape casts, AABB/shape overlaps, all
  allocation-free via caller-provided buffers.
- **Character mover** — box3d's kinematic capsule toolkit (collide → solve planes → clip velocity)
  with a ready-made sample controller.
- **Callbacks** — custom collision filtering, pre-solve contact veto, friction/restitution mixing
  (with clear worker-thread safety rules).
- **Diagnostics** — debug-draw overlay (shapes, contacts, forces, islands) toggled right on the
  `Box3dWorld` component; a drop-in stats HUD (step time, per-phase profile, live counts); and
  `World.GetProfile()` / `GetCounters()` for programmatic profiling.
- **Determinism & replay** — record a simulation, verify it reproduces bit-identical state (even
  across worker counts), save it, and scrub the replay frame by frame with divergence detection —
  for lockstep/rollback netcode and bug repro. No other Unity physics wrapper ships this.
- **Extras** — explosions, wind, conveyor surface materials.
- **Multithreading** — box3d's internal scheduler, configurable worker count per world.
- **Component layer (experimental)** — author bodies and shapes in the Inspector, mirroring
  Unity's Rigidbody/Collider model (see the docs).

## Performance

Measured against Unity's built-in PhysX with identical seeded scenes (editor, same machine,
box3d running 4 sub-steps vs PhysX defaults — details and CSVs in the docs):

| Scene | PhysX | Box3d (16 workers) | |
|---|---|---|---|
| 10,000 spheres raining | 6.70 ms | **1.84 ms** | ~3.6× faster |
| Destroyed city, ~10,000 bodies (buildings + props) | 10.94 ms | **7.67 ms** | ~1.4× faster |
| 64 piles, 1,024 bodies (sleep off) | 1.15 ms | **0.54 ms** | ~2.1× faster |
| Joint-grid cloth, 930 joints | **0.54 ms** | 0.71 ms | PhysX ahead on single-island loads |

The city scene is a mixed real-world load — a town of running-bond brick buildings and props on
terrain, smashed by a scripted sequence of wrecking balls (identical seeded content on both engines).
There, Box3d's *slowest* step matched PhysX's *median* — tighter frame times through the collapses.

A pure-box3d stress test ships too: a [16,290-box pyramid](https://www.youtube.com/watch?v=BtdMbw97Zds),
one box deep, held upright by contact recycling. It steps in single-digit milliseconds, roughly
halving once the stack settles to sleep — throw spheres to wake and collapse it, and dial the worker
thread count to see how box3d's built-in scheduler scales. Ships in the Benchmarks sample.

Box3d's threading scales with the number of independent simulation islands — piles, debris, and
crowds are its home turf; one giant coupled constraint network is its least favorable shape.

## Installation

### OpenUPM (recommended)

[![openupm](https://img.shields.io/npm/v/com.suvitruf.box3d?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.suvitruf.box3d/)

Via the [openupm-cli](https://openupm.com/):

```
openupm add com.suvitruf.box3d
```

Or add the scoped registry manually — **Edit → Project Settings → Package Manager → Scoped
Registries**, add:

- **Name**: `package.openupm.com`
- **URL**: `https://package.openupm.com`
- **Scope(s)**: `com.suvitruf`

then install **box3d for Unity** from **Window → Package Manager → My Registries**.

### Git URL

Alternatively, in the Package Manager choose **Add package from git URL** and paste:

```
https://github.com/Suvitruf/box3d-unity.git
```

Requires **Unity 6000.0+**. Native binaries are included for **Windows x64**, **Linux x64**,
**Android arm64**, and **WebGL**; macOS/iOS build scripts are provided but binaries are not
shipped yet (see `Documentation~/building-natives.md`).

## Quick start

```csharp
using Box3d;
using Unity.Mathematics;
using UnityEngine;

World world = World.Create(WorldDef.Default);

// Static ground box.
Body ground = world.CreateBody(BodyDef.Default);
BoxHull groundHull = BoxHull.Create(10f, 0.5f, 10f);
ground.CreateHullShape(ShapeDef.Default, in groundHull);

// Dynamic sphere. Defs come from .Default (never zero-initialize them!), then mutate.
BodyDef bodyDef = BodyDef.Default;
bodyDef.Type = BodyType.Dynamic;
bodyDef.Position = new float3(0f, 5f, 0f);
Body body = world.CreateBody(bodyDef);
body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });

// Step from FixedUpdate; sync transforms from move events.
world.Step(Time.fixedDeltaTime);
foreach (BodyMoveEvent move in world.GetBodyMoveEvents())
{
    // move.Transform.Position / .Rotation → your Unity Transform
}

world.Destroy();
```

See `Documentation~/getting-started.md` for the full walkthrough, and install the **samples** from
the Package Manager window: an interactive playground, basic simulation, joints, mouse drag,
character controller, a drivable vehicle, and benchmark scenes comparing against PhysX — including a
[16,290-box pyramid stress test](https://www.youtube.com/watch?v=BtdMbw97Zds) you can smash by
throwing spheres.
The sample scenes assume URP (they render fine elsewhere, minus materials), and the interactive
ones require the Input System package.

## Documentation

- [Getting started](Documentation~/getting-started.md)
- [Component layer (experimental)](Documentation~/components.md) — author physics in the Inspector
- [Core concepts](Documentation~/concepts.md) — ids, defs, lifetimes, the rules that matter
- [Shapes & geometry](Documentation~/shapes-and-geometry.md)
- [Joints](Documentation~/joints.md)
- [Events](Documentation~/events.md)
- [Queries](Documentation~/queries.md)
- [Character mover](Documentation~/character-mover.md)
- [Callbacks & threading](Documentation~/callbacks-and-threading.md)
- [Debug draw](Documentation~/debug-draw.md)
- [Determinism & replay](Documentation~/determinism-and-replay.md) — record, validate, and scrub replays
- [Performance](Documentation~/performance.md)
- [Building the native libraries](Documentation~/building-natives.md)

## License

MIT (see [LICENSE](LICENSE)). Box3d itself is MIT, © Erin Catto.
