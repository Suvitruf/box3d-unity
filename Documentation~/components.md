# Component layer (experimental)

> **Young but feature-complete (0.3.x).** A MonoBehaviour layer that lets you author physics in the
> Inspector instead of writing C#, mirroring Unity's Rigidbody/Collider model: bodies, all five
> shape types (sphere, box, capsule, hull, mesh), compound (child) shapes, auto-static colliders,
> seven joint components, and full inspector/gizmo/handle editing. The API is still settling, so
> expect some churn. The pointer-level API in the rest of these docs remains the full-featured
> path. Lives in a separate `Box3d.Hybrid` assembly.

If you know Unity's physics components, you already know these:

| Component | Unity analog | Role |
|---|---|---|
| `Box3dWorld` | the physics scene | Owns the simulation, steps it, syncs Transforms. Auto-created. |
| `Box3dBody` | `Rigidbody` | A physics body: static / kinematic / dynamic. |
| `Box3dSphereShape` | `SphereCollider` | A sphere shape on a body. |
| `Box3dBoxShape` | `BoxCollider` | A box shape on a body. |
| `Box3dCapsuleShape` | `CapsuleCollider` | A capsule shape (radius, height, axis). |
| `Box3dHullShape` | convex `MeshCollider` | Convex hull from a mesh's vertices; works on dynamic bodies. |
| `Box3dMeshShape` | non-convex `MeshCollider` | Triangle mesh from a mesh asset; **static bodies only**. |

## Quick start

1. Create a GameObject, add **Box3dBody** (leave it Dynamic) and **Box3dSphereShape**. Put it a few
   metres up.
2. Make a floor: another GameObject with **Box3dBody** set to **Static** and **Box3dBoxShape**,
   scaled wide and flat.
3. Press play. The sphere falls and lands. No `Box3dWorld` needed — one is created automatically.

Add a `MeshRenderer` (or start from a primitive) to see the objects; the components drive the
Transform, so any visual on the same GameObject follows.

## Box3dWorld

Optional — placed automatically the first time a body needs it. Add one explicitly to tune:

- **Gravity** — the world gravity vector.
- **Sub Step Count** — solver sub-steps per step (higher = stiffer joints/stacks, slower).
- **Worker Count** — physics threads; 0 = auto (about half the logical cores).

Only one world is used; a second `Box3dWorld` logs a warning.

## Box3dBody

The body type mirrors Unity exactly:

- **Static** — never moves. Floors, walls, level geometry. Cheapest.
- **Kinematic** — moved by *its Transform*, ignores gravity and forces, but still pushes dynamic
  bodies out of the way. Moving platforms, doors, player-driven objects.
- **Dynamic** — moved by the solver: gravity, collisions, forces.

Behaves like a Unity component:

- **Enabling/disabling** the component (or its GameObject) removes it from / returns it to the
  simulation without recreating it. A disabled body is frozen *and non-solid* — other objects pass
  through it.
- **Moving from code**: set `Box3dBody.Position` / `Box3dBody.Rotation` (like `Rigidbody.position`),
  which teleports the body and wakes it. Don't set `transform.position` directly on a dynamic body
  — same advice as Unity. (In the editor, dragging the Transform in the Scene view during play does
  work, as a convenience.)
- **Changing the type at runtime** (`Box3dBody.BodyType = …`, or the Inspector during play) re-types
  the live body.

## Shapes

Add a shape component to the *same GameObject* as the body. Material fields live on the shape:

- **Density** (kg/m³) — determines mass; baked at creation.
- **Friction**, **Restitution** (bounciness) — 0–1; changeable at runtime via
  `SetFriction` / `SetRestitution` or the Inspector during play.
- **Center** — local offset from the body origin (like `Collider.center`).

Restitution notes that match the engine, not the component:

- Two surfaces combine as **max** — a bouncy ball bounces off a non-bouncy floor.
- Bounces only apply above the world's **restitution speed threshold** (~1 m/s), so gently settling
  objects stop bouncing. That's deliberate — it prevents endless micro-jitter.

`transform.lossyScale` is baked into shape dimensions at creation (spheres use the largest axis, as
Unity does).

### Collision layers

Shapes honor the GameObject's **Layer** and Unity's **Layer Collision Matrix**
(Project Settings → Physics): a shape's collision category is its layer, and it collides with the
layers that layer is allowed to in the matrix. Set the layer as you would for a normal Unity
collider. (The layer is read when the shape is created; changing it at runtime needs the body
recreated.)

## Compound shapes & static colliders

Like Unity: a body gathers shape components from its own GameObject **and its children** (stopping
at any nested `Box3dBody`), so several shapes under one body form a single compound body — each
child shape is placed at its Transform relative to the body. And a shape with **no** `Box3dBody` on
itself or an ancestor gets its own static body automatically (a collider without a rigidbody is
static geometry).

## Joints

Add a joint component to a GameObject that has a `Box3dBody`; it constrains that body to a
**Connected Body** (or to the world, if left null — like Unity's null connectedBody). The **Anchor**
is the local pivot point.

| Component | Unity analog | Notes |
|---|---|---|
| `Box3dHingeJoint` | `HingeJoint` | Rotates around **Axis**; optional angle limits + motor. |
| `Box3dBallJoint` | `CharacterJoint` | Ball-and-socket; optional cone (swing) + twist limits. |
| `Box3dSliderJoint` | `ConfigurableJoint` (1 axis) | Slides along **Axis**; optional limits + motor. |
| `Box3dDistanceJoint` | `SpringJoint` | Holds a fixed distance between two anchors; optional spring. |
| `Box3dFixedJoint` | `FixedJoint` | Rigidly welds the bodies at their current pose; optional spring. |
| `Box3dParallelJoint` | — | Spring keeps an axis aligned (stay upright). |
| `Box3dFilterJoint` | — | No constraint; disables collision between the two bodies. |
| `Box3dMotorJoint` | — | Soft, driveable spring toward the rest pose (soft attachment / drag). |
| `Box3dWheelJoint` | `WheelCollider`-ish | Vehicle suspension: spring travel, free spin, optional steering. |

Frames are computed so the joint is satisfied at the pose you built it in — creating it never snaps
the bodies. For the wheel joint, put it on the wheel and set Connected Body to the chassis; the
suspension axis and spin (axle) axis are configurable.

## Tooling components

Drop-in components for diagnostics — all optional, none needed to simulate:

| Component | Does |
|---|---|
| `Box3dStatsHud` | On-screen HUD: FPS, step time + per-phase breakdown, live body/contact/island counts, memory. |
| `Box3dRecorder` | Records the world and checks **determinism** (with a cross-thread option); saves a `.rec`. |
| `Box3dReplayer` | Plays back a `.rec` (or live capture) with a scrub **timeline** and divergence read-out. |

See [debug draw](debug-draw.md) for the overlay and HUD, and
[determinism & replay](determinism-and-replay.md) for the recorder/replayer.

## Current limits

- Mesh shapes are static-only (use a hull shape for dynamic concave-ish objects). Hull and mesh
  shapes read mesh vertices at runtime, so the mesh asset must have **Read/Write enabled**.

For anything beyond this, drop to the code API ([getting started](getting-started.md)) — the
components and the API share the same world and interoperate (`Box3dBody.Body`, `Box3dWorld.World`).
