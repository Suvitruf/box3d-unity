# Determinism testing

For lockstep / rollback netcode and authoritative servers you need the simulation to be **reproducible**:
the same inputs must produce the same result. This page covers how to test that — with a state hash you
can compare across runs, builds, and machines.

## The layers (set your expectations)

All the physics math runs inside the **native box3d library**; the C# wrapper only marshals floats across
the boundary and adds no floating-point nondeterminism of its own. So:

| Scope | Deterministic? |
|---|---|
| Same build, same machine, repeated runs | **Yes** — box3d steps deterministically by design. |
| Unity editor vs a standalone build/console on the same OS+CPU | **Yes** — the same native library does the math. |
| Different CPU architecture (x64 vs arm64) or a different compiler | **Not guaranteed** — single-precision float can diverge (FMA contraction, transcendental functions, reordering). |

The tools here let you **measure** each layer. The last one is a property of how box3d is built for a
platform, not something the wrapper can force — but you can detect it, which is the point.

## The state hash

`Determinism.HashState(bodies)` hashes every body's position + rotation (raw float bits) with box3d's own
`b3Hash` (a djb2 the engine ships "for determinism testing"). The hash algorithm is bit-identical
everywhere; only the float values differ if the sim diverges — exactly what you want to catch.

```csharp
// bodies: a FIXED, identically-ordered list across every run you compare.
uint hash = Determinism.HashState(bodies);          // one checksum
// or build a per-step stream:
for (int step = 0; step < steps; step++)
{
    world.Step(1f / 60f, 4);
    stream[step] = Determinism.HashState(bodies);   // catches WHERE a run diverges, not just that it did
}
```

`Determinism.Hash(seed, bytes)` is the raw primitive if you want to fold in more state (velocities, your
own game state, …). The order of `bodies` must match across the runs you compare — pass the same list.

## A determinism unit test

`Box3D.Tests/DeterminismTests.cs` is a ready template: it builds a fixed, randomness-free scenario (a
leaning box stack), steps it, and asserts two runs produce an identical per-step hash stream. It logs the
final hash so you can compare it elsewhere. Copy it and swap in your own scenario.

Keep the scenario on the **low-level API** (no `UnityEngine` types) and it stays portable — the same code
can run in a non-Unity harness.

## Checking across platforms (Editor / Android / WebGL)

`Box3DDeterminismHarness` runs the shared `DeterminismScenario` and shows its hash signature on screen —
so you can build the *same* scene to different targets and compare by eye.

1. New empty scene → add a GameObject with **Box3D Determinism Harness**. (No camera or lights needed;
   it draws with IMGUI.)
2. Build and run it on each target you care about — **Editor** (x64/Mono), **Android** (arm64/IL2CPP),
   **WebGL** (WASM). Each shows platform, backend, and four hashes: checkpoints at 25/50/75 % and the
   **final**.
3. Compare:
   - **All four match across platforms** → the physics reproduced bit-for-bit there. Lockstep across
     those devices is safe.
   - **They differ** → single-precision float diverged between those native builds. The first checkpoint
     that differs tells you how early it happens. This is box3d's per-platform build, not the wrapper;
     it's where you'd invest in strict-FP build flags if you need cross-device lockstep.

The signature is also `Debug.Log`ged, so you can read it via `adb logcat` (Android) or the browser
console (WebGL), and there's a **Copy signature** button. The harness and the unit test run the *same*
`DeterminismScenario`, so a value you see on device is the same simulation the test pins on your build.

> Tip: two different phones with the same CPU architecture and the same app build should match. The
> interesting comparison is *across* architectures (desktop x64 vs mobile arm64 vs WASM).

## Related

- [Determinism & replay](determinism-and-replay.md) — record a run and let box3d validate it reproduces
  (`ValidateReplay`), including across worker counts, plus scrubbable replay.
