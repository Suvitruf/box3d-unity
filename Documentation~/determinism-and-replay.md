# Determinism & replay

box3d steps deterministically and can **record** a simulation, **validate** that it reproduces
bit-identical state, and **replay** it frame by frame. This is the tooling for lockstep / rollback
netcode, authoritative server physics, and reproducing "it only happens sometimes" bugs — and no
other Unity physics wrapper ships it.

![box3d replay scrubbing](https://projects.cdn.aapanasik.com/box3d-unity/box3d-replay.gif)

## What a recording captures

A recording is the **physics** of a run: collision shapes, body transforms, contacts, and a
per-step state hash. It is *not* your Unity scene — no meshes, materials, or prefabs. So a replay
draws the world as debug **wireframes** (through the same draw bridge as [debug draw](debug-draw.md)),
not your rendered objects. That's expected: this is a physics-state viewer, not cinematic playback.

Recordings are **snapshot-seeded** — the full world state is captured at `StartRecording`, so the
replay stands up the same bodies even if you start recording after the world is populated.

## Determinism check (record → validate)

The headline: prove your simulation is reproducible.

- **`Box3dRecorder`** — drop it on a GameObject in a scene with a `Box3dWorld`. It records from the
  start, and after **Record Steps** stops and runs box3d's replay validation, logging
  **`DETERMINISTIC`** or **`DIVERGED`**. A `DIVERGED` result means something in your scene is
  non-deterministic (unseeded randomness, frame-rate-dependent forces) — box3d's own stepping is not.
  - **Cross-thread test:** set the `Box3dWorld`'s **Worker Count** to 4 and the recorder's **Validate
    Worker Count** to 1 (or vice-versa). Still `DETERMINISTIC` means box3d reproduces identically
    regardless of thread count — the guarantee networked games need.
  - Context menu (⋮): **Stop & Validate**, **Validate Determinism**, **Save Recording**.

> The recorder captures the **component** `Box3dWorld`. If a scene builds physics via the raw API in
> its own `World`, the recorder warns that the component world is empty — record that world directly
> (below) instead.

## Replay & scrub (record → replay)

- **`Box3dReplayer`** — plays back a `.rec` file (or live capture bytes) in its own replay world,
  debug-drawn. In Play mode the component's Inspector shows a **Timeline**: a frame slider,
  transport buttons (◀ / play-pause / step / restart), and a live **`DIVERGED at frame N`** read-out.
  It needs no `Box3dWorld` or bodies in the scene — it stands up its own world from the recording.

Wireframes render in the **Scene view** always, and in the **Game view only with Gizmos** enabled
(same caveat as debug draw).

## Doing it from code (raw API)

The components wrap a small low-level API you can drive directly — useful for raw-API worlds or
headless validation:

```csharp
World world = World.Create(worldDef);
// ... create bodies ...

var recording = Recording.Create();       // sized buffer; grows up to its cap
world.StartRecording(recording);          // snapshot-seeded from here

for (int i = 0; i < 300; i++) world.Step(1f / 60f, 4);

world.StopRecording();
bool deterministic = recording.ValidateReplay(workerCount: 1);   // the determinism check
recording.SaveToFile("run.rec");

// Later — replay it:
ReplayPlayer player = ReplayPlayer.Create(recording.GetData(), workerCount: 1);
player.EnableShapeDrawing();               // wire the draw bridge so shapes draw (main thread)
player.SeekFrame(120);                     // jump anywhere
player.World.DrawDebug(DebugDrawFlags.Shapes | DebugDrawFlags.Contacts);
if (player.HasDiverged) Debug.Log($"diverged at frame {player.DivergeFrame}");

player.Destroy();                          // both own native memory
recording.Destroy();
```

`ReplayPlayer` exposes `StepFrame` / `SeekFrame` / `Restart`, `Frame` / `FrameCount` / `IsAtEnd`,
`HasDiverged` / `DivergeFrame`, `GetInfo()` (frame count, timestep, sub-steps, worker count, bounds),
`SetWorkerCount`, per-body access (`BodyCount` / `GetBody(i)`), and the replayed `World`.

## When to reach for it

- **Networked physics** — verify lockstep/rollback determinism, including across worker counts.
- **Bug repro** — capture a run that misbehaves and scrub to the exact frame it goes wrong.
- **Regression tests** — `ValidateReplay` in a test asserts a scene stays deterministic.
