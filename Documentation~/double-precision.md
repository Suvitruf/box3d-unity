# Double precision (large worlds)

By default the package runs Box3D in **single precision** — the right choice for almost every
game. Box3D also supports an opt-in **double-precision** mode for *large worlds*: float positions
lose accuracy past roughly 16 km from the origin, while double positions stay accurate essentially
anywhere. It is a **hybrid** mode, not a wholesale switch: only world **positions** widen to double
(`b3Pos`, and the translation part of world transforms). Velocities, rotations, forces, AABBs and
local geometry stay float in both modes.

Enable it only if you actually have a far-from-origin problem — it costs a little performance and,
on its own, does nothing for rendering (see [the render side](#the-render-side-floating-origin)
below).

## Enabling it

Two things must change together, and the package verifies they match:

1. **Define `BOX3D_DOUBLE`** in *Project Settings ▸ Player ▸ Scripting Define Symbols* (per build
   target). This switches the managed struct layouts AND makes every binding import the
   double-precision library by name.
2. **Provide the double-precision native library** next to the single one, named with a `_d`
   suffix:

   | Platform | Double library |
   |---|---|
   | Windows x64 | `Plugins/Windows/x86_64/box3d_d.dll` |
   | Linux x64 | `Plugins/Linux/x86_64/libbox3d_d.so` |
   | macOS | `Plugins/macOS/libbox3d_d.dylib` |
   | Android arm64 | `Plugins/Android/arm64-v8a/libbox3d_d.so` |

   Give each file PluginImporter settings mirroring its single-precision sibling (same platforms,
   CPU x86_64/ARM64). Both variants can ship side by side — a native library only loads when first
   called, so the unused one is inert. iOS and WebGL work differently — see
   [the iOS / WebGL section](#ios--webgl-static-linking).

3. **Restart the editor** after switching the define (either direction). A native library, once
   loaded, stays in the editor process until it exits — it survives domain reloads and cannot be
   unloaded. So after a mid-session switch the *previous* variant is still resident: you can end up
   with both libraries loaded at once, each with its own global state (log/assert hooks, worker
   threads, world slots), and worlds or callbacks created before the switch belong to the other
   library. It may appear to work; treat it as undefined. Also note that a loaded DLL is locked on
   Windows — updating the library *files* requires the editor closed regardless of the define.

Build the `_d` libraries with the same scripts as the single ones by setting `BOX3D_DOUBLE=1`
(see [building the natives](building-natives.md#double-precision-variant)), or take them from the
`build-natives` CI workflow's artifacts (`precision: both`).

**Mismatch protection.** A managed/native precision mismatch would corrupt memory, so it is guarded
three ways: Box3D renames `b3CreateWorld` in double builds (a deliberate tripwire — the wrong
pairing fails on the first world creation instead of corrupting), the runtime asserts
`b3IsDoublePrecision()` against the define at init and logs a clear error, and the
`NativeBuild_PrecisionMatchesManagedDefine` test fails loudly in CI.

## What changes in the API

Positions are carried by two types whose layout follows the define:

- **`B3Pos`** — a world position. Single: three floats, interchangeable with `float3`. Double:
  three doubles, interchangeable with `double3`.
- **`B3WorldTransform`** — a world transform: `B3Pos` position + always-float quaternion rotation.
  Distinct from `B3Transform`, which is a *relative* transform (shape/joint frames) and stays float
  in both modes.

Conversions follow one rule: **into** the wide types is implicit (lossless widening from
`float3`/`Vector3`), **out** is an explicit cast in double mode (`(float3)`, `(Vector3)`,
`.ToFloat3()`, `.ToB3Transform()`) because narrowing loses precision far from the origin. In single
mode everything converts implicitly both ways, so code written with the casts compiles identically
under either define.

Where these appear: `Body.Position` / `Body.Transform`, `BodyDef.Position`, `BodyMoveEvent`,
`RayResult.Point`, `ContactHitEvent.Point`, `ExplosionDef.Position`, apply-force/impulse points,
and the debug-draw callbacks.

Behavioral differences in double mode:

- The world-size sanity limit (`B3_HUGE`) widens from 1e5 to 1e9 length units — e.g. the distance
  joint's default `MaxLength` becomes 1e9.
- **Recordings (`.rec`) are precision-specific** — positions are serialized at native width, so a
  recording made in one precision won't replay in the other. Re-record after switching.
- **Determinism hashes are precision-specific** — `Determinism.HashState` hashes the full-width
  position bits, so compare hashes only between builds of the same precision.

## The render side (floating origin)

Unity's `Transform` stores float positions, so the component layer necessarily narrows when it
drives GameObjects. Near the origin that's lossless in practice; the double-precision benefit only
materializes far from the origin, **paired with a floating-origin / camera-relative strategy** —
keep physics at absolute double coordinates (Box3D needs no rebasing) and render relative to a
moving origin. Without that, enabling double precision changes nothing visible. The low-level API
(`Body.Position` as `double3`) gives you the exact positions to build such a scheme on.

## iOS / WebGL (static linking)

iOS and WebGL link the native code **into the player at build time** (`__Internal`) — there is no
separate library file to select by name, and the single- and double-precision archives export the
same symbols, so they can never both be linked into one build. Consequences:

- **The package ships single precision only on these platforms.** The `BOX3D_DOUBLE` define alone
  is not enough there — it would make the C# expect double layouts while the linked archive is
  single (the init guard and the `b3CreateWorld` tripwire will catch this immediately).
- **To use double precision on iOS or WebGL you must replace the linked archive**, which requires a
  *mutable* package: embed the package (Package Manager ▸ Embed, or reference it via a local
  `file:` path) so it moves into `Packages/` and becomes editable. A registry/git install lives in
  the read-only package cache and cannot be modified.
- Then build the double archive (`BOX3D_DOUBLE=1 build_ios.sh` / `build_webgl.sh` — it lands in
  `Box3D.Native~/double-staging/iOS|WebGL/`, outside Unity's asset scope), **replace** `Plugins/iOS/libbox3d.a`
  (or `Plugins/WebGL/libbox3d.a`) with it, set the define for that build target, and build the
  player. Precision on these platforms is fixed per player build.

Given that large-world games are rare on mobile and web, treat double precision there as an
advanced, embed-required path; on the dynamic platforms above it works out of the box.
