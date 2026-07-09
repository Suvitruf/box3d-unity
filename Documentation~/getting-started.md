# Getting started

## Install

Package Manager → *Add package from git URL* → `https://github.com/Suvitruf/box3d-unity.git`.
Unity 6000.0+ is required. `com.unity.mathematics` is pulled in automatically.

Box3d worlds are completely independent from Unity's built-in physics — nothing here touches
PhysX, colliders, or `Rigidbody`. You create a world, populate it, step it, and copy the results
onto your Transforms.

## Your first world

```csharp
using Box3d;
using Unity.Mathematics;
using UnityEngine;

public class MyPhysics : MonoBehaviour
{
    private World _world;
    private Body _ball;

    private void Start()
    {
        _world = World.Create(WorldDef.Default);

        // Ground: a static body with a box hull.
        Body ground = _world.CreateBody(BodyDef.Default);
        BoxHull hull = BoxHull.Create(10f, 0.5f, 10f);
        ground.CreateHullShape(ShapeDef.Default, in hull);

        // A dynamic ball. Defs always start from .Default — never new them up empty.
        BodyDef def = BodyDef.Default;
        def.Type = BodyType.Dynamic;
        def.Position = new float3(0f, 5f, 0f);
        _ball = _world.CreateBody(def);
        _ball.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.5f });
    }

    private void FixedUpdate()
    {
        _world.Step(Time.fixedDeltaTime); // 4 sub-steps by default

        transform.SetPositionAndRotation(_ball.Position, _ball.Rotation);
    }

    private void OnDestroy()
    {
        if (_world.IsValid) _world.Destroy();
    }
}
```

## Syncing many transforms efficiently

Polling every body's transform each frame works, but box3d offers something better: after each
step, `GetBodyMoveEvents()` returns a contiguous array of only the bodies that actually moved,
with their new transforms and your `UserData` attached:

```csharp
bodyDef.UserData = (IntPtr)myIndex; // set at creation

foreach (BodyMoveEvent move in _world.GetBodyMoveEvents())
{
    int index = (int)move.UserData;
    _transforms[index].SetPositionAndRotation(move.Transform.Position, move.Transform.Rotation);
}
```

Sleeping bodies produce no events, so a settled scene costs almost nothing to sync.

## Stepping advice

- Call `Step` from `FixedUpdate` with `Time.fixedDeltaTime`. Box3d is designed for fixed steps.
- The default 4 sub-steps (`world.Step(dt)` = `world.Step(dt, 4)`) suit most games. More sub-steps
  buy constraint stiffness; fewer buy speed.
- For multithreading, set `WorldDef.WorkerCount` before creating the world (a good default is the
  machine's physical core count). See [Callbacks & threading](callbacks-and-threading.md).

## Where to go next

- [Core concepts](concepts.md) — the handle/def/lifetime rules everything else assumes.
- Install the package samples (Package Manager → this package → Samples) for working scenes:
  basic simulation, joints, mouse dragging, a character controller, a drivable vehicle, and
  benchmark scenes — including a [16,290-box pyramid stress test](https://www.youtube.com/watch?v=BtdMbw97Zds)
  (throw spheres to smash it; adjustable worker threads and live step-time metrics).
