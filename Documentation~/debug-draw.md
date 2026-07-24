# Debug draw

One call renders box3d's own view of the world as editor lines:

```csharp
private void Update()
{
    _world.DrawDebug(DebugDrawFlags.Shapes | DebugDrawFlags.Joints);
}
```

Lines are drawn with `Debug.DrawLine`, which means they appear in the **Scene view** always, and
in the **Game view only when the Gizmos toggle** (top-right of the Game view) is enabled — check
that toggle before concluding nothing is drawn.

![box3d debug-draw overlay](https://projects.cdn.aapanasik.com/box3d-unity/box3d-world-debug.gif)

## Component layer: one-click overlay

With the component layer you don't need any code. The `Box3DWorld` component has a **Debug Draw**
field (a `DebugDrawFlags` mask; `None` = off) plus a **Debug Draw Radius**. Pick the flags in the
Inspector and it overlays them in the Scene view every frame while playing — e.g.
`Shapes | Contacts | ContactNormals | Islands` to watch collisions and the solver's threading
islands. (Same Game-view Gizmos caveat applies.)

## Flags

| Flag | Shows |
|---|---|
| `Shapes` | shape wireframes (spheres, capsules, convex hulls) |
| `Joints` / `JointExtras` | joint connections and frames / extra joint detail |
| `Bounds` | fat AABBs — also the way to visualize mesh/heightfield/compound shapes |
| `Mass` | center of mass and mass info for dynamic bodies |
| `Contacts` / `ContactNormals` / `ContactForces` / `FrictionForces` | contact points and vectors |
| `Islands` | island bounding boxes — great for understanding threading behavior |
| `GraphColors` | constraint-graph coloring (solver internals) |

`DebugDrawFlags.Default` is `Shapes | Joints`. A second parameter limits the draw radius
(default 100 m around the origin).

## Stats & profiling

Numbers to go with the geometry:

- **`Box3DStatsHud`** — a drop-in component: add it to any GameObject in a scene with a `Box3DWorld`
  and it overlays FPS, step time, awake/total body count, live shape/contact/joint/island counts,
  memory, and a **per-phase step-time breakdown** (broadphase / narrowphase / solve / continuous /
  sleep / sensors). The classic testbed HUD; call `Toggle()` from your own input to bind a key.
- **`World.GetProfile()`** → a `Profile` struct: per-phase step timings in milliseconds. Use it to see
  which phase dominates the step cost.
- **`World.GetCounters()`** → a `Counters` struct: live body/shape/contact/joint/island counts and
  allocator + broadphase stats (tree heights, byte count). Plus `World.GetAwakeBodyCount()`.
- **`TreeStats`** — every ray/overlap/cast has an `out TreeStats` overload reporting how many
  broadphase nodes the query visited (its spatial cost).

## Collision debugger ("why isn't this colliding?")

**Window ▸ Box3D ▸ Collision Debugger** answers the most common physics question. Assign two
`Box3DBody` objects, enter Play mode, and it gives a rule-by-rule verdict — the same rules the engine
uses:

- **body types** — a static/kinematic pair never collides (at least one must be dynamic);
- **enabled** — disabled bodies don't collide;
- **joints** — a joint between them with *Collide Connected* off suppresses it;
- **collision filters** — category/mask both ways, and group-index overrides, with the exact bits that
  fail spelled out;
- **sensor** and **AABB proximity** notes (whether they're even near each other right now).

The rule logic lives in `CollisionDiagnostics` if you want to run it from your own code.

## Limitations

- Triangle meshes, height fields, and compounds don't draw their full wireframe (a terrain would
  be tens of thousands of lines) — use `Bounds` for those, and your own render mesh is usually the
  ground truth anyway.
- World-space debug *text* (body names) is not bridged.
- Drawing wireframes for a world with many shapes is line-heavy; it's a debugging tool, not a
  renderer. Keep it behind a toggle, as the samples do.
