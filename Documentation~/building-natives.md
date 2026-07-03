# Building the native libraries

The package ships prebuilt Box3d binaries in `Plugins/`; you only need this page to add a
platform, update the pinned Box3d version, or audit what you're running.

All binaries are built from the **pinned Box3d commit** recorded in `Box3d.Native~/VERSION`,
Release configuration, single precision, via the scripts in `Box3d.Native~/`. The C API surface
(~580 exported functions) must match the generated bindings — never mix binaries and bindings
from different Box3d commits.

| Platform | Binary | Script | Status |
|---|---|---|---|
| Windows x64 | `Plugins/Windows/x86_64/box3d.dll` | `build_windows.bat` (VS 2022 Build Tools) | shipped |
| Linux x64 | `Plugins/Linux/x86_64/libbox3d.so` | `build_linux.sh` (gcc + cmake) | shipped |
| Android arm64-v8a | `Plugins/Android/arm64-v8a/libbox3d.so` | `build_android.bat` (Windows, Unity's bundled NDK) or `build_android.sh` (Linux/macOS host) | shipped |
| macOS universal | `Plugins/macOS/libbox3d.dylib` | `build_macos.sh` (Xcode) | script ready, binary pending |
| iOS arm64 (static) | `Plugins/iOS/libbox3d.a` | `build_ios.sh` (Xcode) | script ready, binary pending |

Each script clones nothing: it needs a Box3d checkout at the pinned commit. All paths are
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

## iOS specifics

iOS requires static linking. The C# side already handles this: every `DllImport` uses a constant
that compiles to `"__Internal"` on iOS device builds (`Box3d/Bindings/Box3dLibrary.cs`), so only
the `.a` binary and its import settings are needed.

## Updating the Box3d version

1. Check out the new Box3d commit; update `Box3d.Native~/VERSION`.
2. Rebuild **all** platform binaries.
3. Regenerate the bindings: `Box3d.Native~/bindgen/generate.sh` (requires the .NET 8 SDK and the
   ClangSharpPInvokeGenerator tool — see comments in the script). The pipeline regenerates the raw
   externs, the public forwarding methods, and a report of anything not auto-wrapped.
4. Run the test suite — the struct-size and default-value tests are designed to catch ABI drift
   loudly, and the coverage tests catch removed/renamed functions.

## Determinism note

Box3d enforces deterministic math (`-ffp-contract=off`, custom trig). The build scripts preserve
those flags — if you build with different toolchains or flags, cross-platform determinism may be
degraded even though everything still works.
