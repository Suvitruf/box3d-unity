# Events

Box3D does not call you back during the step. Instead, events are buffered and **polled after
`Step`** — simpler to reason about, trivially thread-safe, and cache-friendly.

```csharp
world.Step(dt);

foreach (BodyMoveEvent move in world.GetBodyMoveEvents()) { ... }

ContactEvents contacts = world.GetContactEvents();
foreach (ContactBeginTouchEvent begin in contacts.Begin) { ... }
foreach (ContactEndTouchEvent end in contacts.End) { ... }
foreach (ContactHitEvent hit in contacts.Hit) { ... }      // impacts above the speed threshold

SensorEvents sensors = world.GetSensorEvents();
foreach (SensorBeginTouchEvent enter in sensors.Begin) { ... }
foreach (SensorEndTouchEvent exit in sensors.End) { ... }

foreach (JointEvent strained in world.GetJointEvents()) { ... }
```

## The two rules

**1. Events are opt-in per shape, and off by default.** Set the flags on `ShapeDef` before
creating the shape:

| Flag | Enables |
|---|---|
| `EnableContactEvents` | begin/end touch events |
| `EnableHitEvents` | hit events (impact speed above `WorldDef.HitEventThreshold`, default 1 m/s) |
| `EnableSensorEvents` | sensor events — needed on **both** the sensor and the visiting shape |
| `IsSensor` | makes the shape a sensor (overlaps, never collides) |

"Why am I getting no events?" is almost always a missing flag.

**2. Event memory is transient** — valid only until the next `Step` or world mutation. The
contact/sensor bundles are `ref struct`s of spans, so the compiler stops you from storing them;
copy out anything you need to keep. Ids inside *end*-touch events may reference already-destroyed
shapes — validate with `IsValid` before use.

## Resolving event ids back to wrappers

Events carry raw ids (`BodyId`, `ShapeId`, `JointId`), not wrapper objects. You never need your own
id→object lookup table — the wrappers are thin value types over the ids, so wrapping an id **is**
the resolution:

```csharp
foreach (ContactBeginTouchEvent begin in contacts.Begin)
{
    var shape = new Shape(begin.ShapeIdA);   // cheap, no validation
    Body body = shape.GetBody();             // and onward through the normal API
    object payload = MyLookup(body.UserData);
}
```

Ids fresh out of the current world's event stream are safe to wrap directly. For ids you stored
across steps (or received from elsewhere), use the validated form — false for stale ids and for
ids belonging to a different world:

```csharp
if (world.TryGetBody(savedId, out Body body))
    body.ApplyForceToCenter(force, wake: true);
```

`TryGetShape` and `TryGetJoint` work the same way, and every wrapper also exposes `IsValid` for a
spot check (ids are generation-validated, so a stale id fails cleanly rather than crashing).

## Body move events

The efficient transform-sync channel. Only bodies that moved appear; each event carries the new
transform, your `Body` user data (set `BodyDef.UserData`, e.g. an index into your own array), and
a `FellAsleep` flag so you can also idle your game object when the body sleeps.

## Hit events

`ContactHitEvent` includes the impact point, normal, approach speed, and both shapes' user
material ids — enough for impact sounds and effects without any per-contact callback:

```csharp
foreach (ContactHitEvent hit in contacts.Hit)
{
    PlayImpactSound(hit.Point, hit.ApproachSpeed, hit.UserMaterialIdA, hit.UserMaterialIdB);
}
```

Tune the world-wide threshold with `WorldDef.HitEventThreshold`.

## Contact manifolds (querying contacts)

Hit events are cheap fire-and-forget notifications. When you need the *full* detail of what a body
or shape is currently touching — every contact point, the normal, penetration depth, and solver
impulses — poll for it. No opt-in is needed (unlike the event stream):

```csharp
foreach (ContactData contact in body.GetContacts())
{
    // AnchorA is relative to shape A's body's center of mass — and box3d, not you, chooses which
    // shape is A (it may be the OTHER body). Resolve A's body to turn an anchor into a world point:
    var bodyA = new Body { Id = contact.ShapeA.GetBody() };
    float3 comA = bodyA.GetWorldCenterOfMass();
    foreach (Manifold m in contact.Manifolds)
    {
        for (int i = 0; i < m.PointCount; i++)
        {
            ManifoldPoint p = m[i];
            float3 worldPoint = comA + p.AnchorA;
            if (p.TotalNormalImpulse > hardHitThreshold && !p.Persisted)
                SpawnImpactEffect(worldPoint, m.Normal);   // a NEW hard contact this step
        }
    }
}
```

Available on `Contact.GetData()` (a single tracked contact), `Body.GetContacts()`, and
`Shape.GetContacts()`; `GetContactCapacity()` gives an upper bound if you want to pre-size.
`Body.GetContacts()` returns only *touching* contacts; `Shape.GetContacts()` can also include
*speculative* points that are near but separated (positive `Separation`) — filter on
`Separation <= 0` if you only want ones in contact.

What each field is good for:

| Field | Use |
|---|---|
| `Manifold.Normal` | surface orientation (world, points A → B) — orient decals/sparks |
| `ManifoldPoint.AnchorA` / `AnchorB` | contact location relative to shape A's / shape B's body center of mass; world point = `contact.ShapeA.GetBody()`'s COM + `AnchorA` (box3d picks the A/B order) |
| `TotalNormalImpulse` | how hard the point was pushed — impact strength / damage |
| `NormalVelocity` | pre-solve approach speed (negative = closing) |
| `Separation` | penetration depth (negative = overlapping) |
| `TriangleIndex` | which triangle, when a shape is a mesh/height field — per-surface material |
| `Persisted` | `false` = a fresh impact this step; `true` = ongoing resting contact |
| `FeatureId` | stable id to track one point across steps |

**Lifetime.** In the raw C API, `b3ContactData` reaches its manifolds through a pointer into internal
engine memory that *"may become invalid — do not store this pointer."* This wrapper copies the
manifold data into managed memory the moment you call the accessor, so the `ContactData` you get back
is a safe, self-contained snapshot you can keep. It reflects the state at the **last `World.Step`** —
call again after stepping for fresh values. (Each call allocates the returned arrays, so query when
you need the detail rather than every frame for every body.)

## When you actually need callbacks

Contact *modification* (disabling specific contacts, custom filtering, custom friction mixing)
can't be done by polling — see [Callbacks & threading](callbacks-and-threading.md) for the
callback API and its worker-thread rules.
