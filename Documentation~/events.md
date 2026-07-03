# Events

Box3d does not call you back during the step. Instead, events are buffered and **polled after
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

## When you actually need callbacks

Contact *modification* (disabling specific contacts, custom filtering, custom friction mixing)
can't be done by polling — see [Callbacks & threading](callbacks-and-threading.md) for the
callback API and its worker-thread rules.
