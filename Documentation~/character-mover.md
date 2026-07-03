# Character mover

Box3d ships a *toolkit* for kinematic character controllers, not a turnkey component: you own the
character's position and velocity; the toolkit tells you where the world pushes back. This is
deliberate — every game tunes step height, slopes, and jumping differently.

The pieces:

- `world.CollideMover(origin, capsule, filter, planes)` — finds all surfaces the capsule touches
  and fills a buffer of `CollisionPlane`s, ready for the solver.
- `Mover.SolvePlanes(targetDelta, planes)` — given your intended movement, returns the corrected
  movement that respects every plane (and how hard each plane pushed).
- `Mover.ClipVector(velocity, planes)` — removes velocity components pointing into surfaces, so
  the character slides along walls instead of sticking.
- `world.CastMover(origin, capsule, translation, filter)` — swept capsule; returns the fraction of
  the translation that is free. Useful for pre-checks (ledges, teleports).

## The canonical loop

```csharp
private float3 _position;
private float3 _velocity;
private bool _grounded;

private static readonly Capsule Mover = new Capsule
{
    Center1 = new float3(0f, 0.4f, 0f),   // capsule spans the character's height
    Center2 = new float3(0f, 1.4f, 0f),
    Radius = 0.4f,
};

private void StepCharacter(float dt)
{
    // 1. Gather contact planes at the current position.
    Span<CollisionPlane> planes = stackalloc CollisionPlane[16];
    int count = world.CollideMover(_position, Mover, QueryFilter.Default, planes);
    Span<CollisionPlane> active = planes.Slice(0, count);

    // 2. Groundedness falls out of the plane normals.
    _grounded = false;
    for (int i = 0; i < count; i++)
        if (active[i].Plane.Normal.y > 0.7f) _grounded = true;

    // 3. Integrate input + gravity into a desired move, let the solver correct it.
    _velocity.y -= gravity * dt;
    PlaneSolverResult solved = Box3d.Mover.SolvePlanes(_velocity * dt, active);
    _position += solved.Delta;

    // 4. Clip velocity so we slide along surfaces instead of accumulating into them.
    _velocity = Box3d.Mover.ClipVector(_velocity, active);
}
```

Run it from `FixedUpdate`, set `_velocity.x/z` from input, add `_velocity.y = jumpSpeed` when
grounded and jump is pressed — that's a working controller. The **Character Mover** sample is
exactly this plus stairs, a ramp, and obstacles.

## Notes

- The mover is not a body: it doesn't push dynamic objects. If you need that, apply impulses to
  bodies you detect via the gathered planes' shapes, or give the character a kinematic body too.
- Soft platforms: `CollideMover` takes a `pushLimit` (default rigid); lower values make surfaces
  squishy, and such planes should have `ClipVelocity` off.
- Step climbing quality comes from capsule shape + plane solving; genuinely discrete steps work
  better with a `CastMover` step-up probe. Tune for your game.
