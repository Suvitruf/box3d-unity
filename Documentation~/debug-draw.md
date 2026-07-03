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

## Limitations

- Triangle meshes, height fields, and compounds don't draw their full wireframe (a terrain would
  be tens of thousands of lines) — use `Bounds` for those, and your own render mesh is usually the
  ground truth anyway.
- World-space debug *text* (body names) is not bridged.
- Drawing wireframes for a world with many shapes is line-heavy; it's a debugging tool, not a
  renderer. Keep it behind a toggle, as the samples do.
