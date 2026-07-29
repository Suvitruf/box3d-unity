# Building the native libraries

The package ships prebuilt Box3D binaries in `Plugins/`; you only need this page to add a
platform, update the pinned Box3D version, or audit what you're running.

All binaries are built from the **pinned Box3D commit** recorded in `Box3D.Native~/VERSION`,
Release configuration, single precision by default (see
[the double-precision variant](#double-precision-variant)), via the scripts in `Box3D.Native~/`. The C API surface
(~580 exported functions) must match the generated bindings — never mix binaries and bindings
from different Box3D commits.

| Platform | Binary | Script | Status |
|---|---|---|---|
| Windows x64 | `Plugins/Windows/x86_64/box3d.dll` | `build_windows.bat` (VS 2022 Build Tools) | shipped |
| Linux x64 | `Plugins/Linux/x86_64/libbox3d.so` | `build_linux.sh` (gcc + cmake) | shipped |
| Android arm64-v8a | `Plugins/Android/arm64-v8a/libbox3d.so` | `build_android.bat` (Windows, Unity's bundled NDK) or `build_android.sh` (Linux/macOS host) | shipped |
| WebGL (wasm static) | `Plugins/WebGL/libbox3d.a` | `build_webgl.sh` (emsdk matching Unity's Emscripten) | shipped |
| macOS universal | `Plugins/macOS/libbox3d.dylib` | `build_macos.sh` (Xcode) | script ready, binary pending |
| iOS arm64 (static) | `Plugins/iOS/libbox3d.a` | `build_ios.sh` (Xcode) | script ready, binary pending |

Each script clones nothing: it needs a Box3D checkout at the pinned commit. All paths are
resolved automatically where possible and overridable via environment variables:

| Variable | Used by | Default |
|---|---|---|
| `BOX3D_SRC` | all scripts | probed: a `box3d/` folder next to this repo |
| `CMAKE` | Windows/Android | `cmake` on PATH, else VS 2022 Build Tools |
| `NINJA` | Android | `ninja` on PATH, else VS 2022 Build Tools |
| `UNITY_EDITOR` | Android (.bat) | newest Unity Hub install that has the Android NDK |
| `ANDROID_NDK` / `ANDROID_ABI` | Android (.sh) | Unity Hub NDK probe / `arm64-v8a` |
| `CLANG_BUILTIN_INCLUDE` | bindgen | newest GCC include dir (for `stdbool.h` etc.) |

Every script configures CMake with `BUILD_SHARED_LIBS=ON` (static for iOS), builds Release, and
copies the result into `Plugins/`. If a script can't find something it exits with a message naming
the variable to set.

## Double-precision variant

Every build script also produces the opt-in [double-precision](double-precision.md) library when
the `BOX3D_DOUBLE` environment variable is set (any value): it adds `-DBOX3D_DOUBLE_PRECISION=ON`
and, on dynamically linked platforms, names the output with a `_d` suffix so it ships **alongside**
the single build (`box3d_d.dll`, `libbox3d_d.so`, `libbox3d_d.dylib`) — the `BOX3D_DOUBLE`
scripting define then selects it by name at runtime. On the statically linked platforms (iOS,
WebGL) the archive keeps its name and is staged under `Box3D.Native~/double-staging/` instead
(outside Unity's asset scope, so it can never be imported and linked by accident) — it is not shipped by default; see the
[iOS / WebGL section](double-precision.md#ios--webgl-static-linking) for how it gets used.

```bash
BOX3D_DOUBLE=1 bash build_linux.sh     # → Plugins/Linux/x86_64/libbox3d_d.so
```

The `build-natives` CI workflow has a matching `precision` input (`single` / `double` / `both`,
default `both`) and builds the same outputs on every platform in one run.

## WebGL specifics

WebGL links plugins statically and runs single-threaded: the C# imports switch to `__Internal`
automatically, and the wrapper forces `WorkerCount = 1` on WebGL players. The Emscripten version
used to build `libbox3d.a` must match the Unity editor's WebGL toolchain (check
`PlaybackEngines/WebGLSupport/BuildTools/Emscripten/.../emscripten-version.txt`); the shipped
archive was built with 3.1.39 for Unity 6000.0.

## iOS specifics

iOS requires static linking. The C# side already handles this: every `DllImport` uses a constant
that compiles to `"__Internal"` on iOS device builds (`Box3D/Bindings/Box3DLibrary.cs`), so only
the `.a` binary and its import settings are needed.

## Updating the Box3D version

1. Check out the new Box3D commit; update `Box3D.Native~/VERSION`.
2. Rebuild **all** platform binaries.
3. Regenerate the bindings: `Box3D.Native~/bindgen/generate.sh` (requires the .NET 8 SDK and the
   ClangSharpPInvokeGenerator tool — see comments in the script). The pipeline regenerates the raw
   externs, the public forwarding methods, and a report of anything not auto-wrapped.
4. Run the test suite — the struct-size and default-value tests are designed to catch ABI drift
   loudly, and the coverage tests catch removed/renamed functions.

## Determinism note

Box3D enforces deterministic math (`-ffp-contract=off`, custom trig). The build scripts preserve
those flags — if you build with different toolchains or flags, cross-platform determinism may be
degraded even though everything still works.
