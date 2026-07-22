# Component layer (experimental)

> **Young but feature-complete (0.3.x).** A MonoBehaviour layer that lets you author physics in the
> Inspector instead of writing C#, mirroring Unity's Rigidbody/Collider model: bodies, all five
> shape types (sphere, box, capsule, hull, mesh), compound (child) shapes, auto-static colliders,
> seven joint components, and full inspector/gizmo/handle editing. The API is still settling, so
> expect some churn. The pointer-level API in the rest of these docs remains the full-featured
> path. Lives in a separate `Box3D.Hybrid` assembly.

If you know Unity's physics components, you already know these:

| Component | Unity analog | Role |
|---|---|---|
| `Box3DWorld` | the physics scene | Owns the simulation, steps it, syncs Transforms. Auto-created. |
| `Box3DBody` | `Rigidbody` | A physics body: static / kinematic / dynamic. |
| `Box3DSphereShape` | `SphereCollider` | A sphere shape on a body. |
| `Box3DBoxShape` | `BoxCollider` | A box shape on a body. |
| `Box3DCapsuleShape` | `CapsuleCollider` | A capsule shape (radius, height, axis). |
| `Box3DHullShape` | convex `MeshCollider` | Convex hull from a mesh's vertices; works on dynamic bodies. |
| `Box3DMeshShape` | non-convex `MeshCollider` | Triangle mesh from a mesh asset; **static bodies only**. |
| `Box3DWind` | `WindZone` (visual-only in Unity) | Pushes dynamic bodies inside a box volume; optional gusts. |
| `Box3DExplosion` | — | Radial impulse burst with radius + falloff. |
| `Box3DRope` | — | Source 2-style cable: live editor preview, bake static or simulate in game. |

## Quick start

1. **GameObject → Box3D → Sphere** (the menu is also in the Hierarchy **+** button and right-click
   menu). You get a visible sphere with a dynamic **Box3DBody** and **Box3DSphereShape** already
   attached. Move it a few metres up.
2. **GameObject → Box3D → Ground Plane** — a wide static slab whose top face sits at y = 0.
3. Press play. The sphere falls and lands. No `Box3DWorld` needed — one is created automatically.

Building by hand instead: components live under **Add Component → Box3D** (`Shapes/`, `Joints/`,
`Replay/`, `Diagnostics/`). Adding a shape to a GameObject with no `Box3DBody` above it auto-adds
one — set it to **Static** for non-moving geometry. The components drive the Transform, so any
visual on the same GameObject follows.

## Live tuning

During play, Inspector edits apply to the running simulation immediately: world gravity, body
type/damping, shape friction/restitution/density, sphere and capsule size (mass is re-derived),
wind and explosion parameters, and every joint's limits, motor and spring settings. What stays
fixed on a live object: box/hull/mesh geometry, joint axes/anchors/connected bodies (baked into
the joint frames at creation), and the world's worker count.

## Box3DWorld

Optional — placed automatically the first time a body needs it. Add one explicitly to tune:

- **Gravity** — the world gravity vector.
- **Sub Step Count** — solver sub-steps per step (higher = stiffer joints/stacks, slower).
- **Worker Count** — physics threads; 0 = auto (about half the logical cores).

Only one world is used; a second `Box3DWorld` logs a warning. Selecting the world draws a purple
gravity arrow in the Scene view — direction is the gravity vector, length its strength (1 g ≈ 1.5 m).

## Box3DBody

The body type mirrors Unity exactly:

- **Static** — never moves. Floors, walls, level geometry. Cheapest.
- **Kinematic** — moved by *its Transform*, ignores gravity and forces, but still pushes dynamic
  bodies out of the way. Moving platforms, doors, player-driven objects.
- **Dynamic** — moved by the solver: gravity, collisions, forces.

Behaves like a Unity component:

- **Enabling/disabling** the component (or its GameObject) removes it from / returns it to the
  simulation without recreating it. A disabled body is frozen *and non-solid* — other objects pass
  through it.
- **Moving from code**: set `Box3DBody.Position` / `Box3DBody.Rotation` (like `Rigidbody.position`),
  which teleports the body and wakes it. Don't set `transform.position` directly on a dynamic body
  — same advice as Unity. (In the editor, dragging the Transform in the Scene view during play does
  work, as a convenience.)
- **Changing the type at runtime** (`Box3DBody.BodyType = …`, or the Inspector during play) re-types
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
at any nested `Box3DBody`), so several shapes under one body form a single compound body — each
child shape is placed at its Transform relative to the body. And a shape with **no** `Box3DBody` on
itself or an ancestor gets its own static body automatically (a collider without a rigidbody is
static geometry).

## Joints

Add a joint component to a GameObject that has a `Box3DBody`; it constrains that body to a
**Connected Body** (or to the world, if left null — like Unity's null connectedBody). The **Anchor**
is the local pivot point.

| Component | Unity analog | Notes |
|---|---|---|
| `Box3DHingeJoint` | `HingeJoint` | Rotates around **Axis**; optional angle limits + motor. |
| `Box3DBallJoint` | `CharacterJoint` | Ball-and-socket; optional cone (swing) + twist limits. |
| `Box3DSliderJoint` | `ConfigurableJoint` (1 axis) | Slides along **Axis**; optional limits + motor. |
| `Box3DDistanceJoint` | `SpringJoint` | Holds a fixed distance between two anchors; optional spring. |
| `Box3DFixedJoint` | `FixedJoint` | Rigidly welds the bodies at their current pose; optional spring. |
| `Box3DParallelJoint` | — | Spring keeps an axis aligned (stay upright). |
| `Box3DFilterJoint` | — | No constraint; disables collision between the two bodies. |
| `Box3DMotorJoint` | — | Soft, driveable spring toward the rest pose (soft attachment / drag). |
| `Box3DWheelJoint` | `WheelCollider`-ish | Vehicle suspension: spring travel, free spin, optional steering. |

Frames are computed so the joint is satisfied at the pose you built it in — creating it never snaps
the bodies. For the wheel joint, put it on the wheel and set Connected Body to the chassis; the
suspension axis and spin (axle) axis are configurable.

## Forces

Scene-authorable force fields — select one to see its gizmos:

- **`Box3DWind`** — a box volume that pushes every dynamic body inside along the object's forward
  (+Z) axis each step; rotate the object to aim it. **Strength** is newtons (negative blows
  backward); **Ignore Mass** applies it as acceleration so light and heavy bodies drift equally;
  **Gust Amplitude** / **Gust Frequency** add Perlin-noise gusting. Gizmos show the zone and a grid
  of arrows whose length follows the live gust strength in play mode.
- **`Box3DExplosion`** — a radial impulse burst (native `World.Explode`) at the object's position:
  full **Impulse Per Area** inside **Radius**, fading to zero over **Falloff** beyond it. Trigger
  with `Explode()` from code, the Inspector's **Explode** button, or **Explode On Enable** for
  spawned prefabs. Gizmos show both radii and the blast rays.

Both live under **Add Component → Box3D → Forces** and **GameObject → Box3D**.

## Rope

`Box3DRope` is a Source 2-style cable (**GameObject → Box3D → Rope**). Put it on the start object,
point **End Point** at the far end (or drag the Scene-view handle when it's empty), set
**Segments** / **Slack** / **Radius** — the Scene view always shows the settled hang while you
edit, and **▶ Simulate in Editor** animates it live. Then choose:

- **Dynamic** (default): at runtime the rope becomes capsule segment bodies linked by ball joints.
  Ends attach to any `Box3DBody` found at the endpoints — the rope swings with them and tugs on
  them — otherwise they pin to the world. The rope collides with the scene, but not with the
  bodies it's attached to (the anchors sit at their surface, and contacts there would fight the
  joints); enable **Collide With Attached** if you want it to drape over them.
- **Baked** — press **Bake Current Shape**: the curve freezes (no simulation in game) with
  optional static capsule collision, cheap enough to scatter everywhere like Source 2's static
  cables. A rope set to Baked without ever baking settles once at startup. **Make Dynamic** reverts.

Rendering goes through the rope's LineRenderer (style its material/width freely); the simulation
drives its points every frame.

## Tooling components

Drop-in components for diagnostics — all optional, none needed to simulate:

| Component | Does |
|---|---|
| `Box3DStatsHud` | On-screen HUD: FPS, step time + per-phase breakdown, live body/contact/island counts, memory. |
| `Box3DRecorder` | Records the world and checks **determinism** (with a cross-thread option); saves a `.rec`. |
| `Box3DReplayer` | Plays back a `.rec` (or live capture) as **wireframes** with a scrub **timeline** and divergence read-out. |
| `Box3DVisualReplayer` | Plays a `.rec` back on the scene's **real GameObjects** (same scene), mapped by body name. |

See [debug draw](debug-draw.md) for the overlay and HUD, and
[determinism & replay](determinism-and-replay.md) for the recorder/replayer.

## Current limits

- Mesh shapes are static-only (use a hull shape for dynamic concave-ish objects). Hull and mesh
  shapes read mesh vertices at runtime, so the mesh asset must have **Read/Write enabled**.

For anything beyond this, drop to the code API ([getting started](getting-started.md)) — the
components and the API share the same world and interoperate (`Box3DBody.Body`, `Box3DWorld.World`).
