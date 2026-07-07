#!/usr/bin/env bash
# Builds libbox3d.dylib as a UNIVERSAL binary (x86_64 + arm64). RUN ON A MAC with Xcode tools.
# Output goes to Plugins/macOS/. NOT YET RUN — no Mac available; verify and commit the
# binary + a PluginImporter meta (Editor OSX + Standalone OSXUniversal) when first built.
set -euo pipefail
cd "$(dirname "$0")"

if [ -z "${BOX3D_SRC:-}" ]; then
    for candidate in "$PWD/../../box3d" "$PWD/../../../../box3d"; do
        if [ -f "$candidate/include/box3d/box3d.h" ]; then BOX3D_SRC=$candidate; break; fi
    done
fi
[ -f "${BOX3D_SRC:-}/include/box3d/box3d.h" ] || {
    echo "error: box3d checkout not found — set BOX3D_SRC" >&2; exit 1; }
BUILD=build-macos
OUT=../Plugins/macOS

cmake -S "$BOX3D_SRC" -B "$BUILD" \
  -DCMAKE_BUILD_TYPE=Release \
  -DCMAKE_OSX_ARCHITECTURES="x86_64;arm64" \
  -DCMAKE_OSX_DEPLOYMENT_TARGET=11.0 \
  -DBUILD_SHARED_LIBS=ON \
  -DBOX3D_SAMPLES=OFF \
  -DBOX3D_UNIT_TESTS=OFF \
  -DBOX3D_BENCHMARKS=OFF

cmake --build "$BUILD" -j

mkdir -p "$OUT"
cp "$BUILD"/bin/libbox3d.dylib "$OUT"/libbox3d.dylib 2>/dev/null || cp "$BUILD"/src/libbox3d.dylib "$OUT"/libbox3d.dylib
lipo -info "$OUT"/libbox3d.dylib

# Ad-hoc code signature (identity "-"): no Apple Developer account or certificate needed, and it's
# what lets the arm64 slice load on Apple Silicon (the kernel refuses to load unsigned arm64 code).
# For distribution via git/UPM this is sufficient — quarantine (Gatekeeper) only affects browser
# downloads, and Developer-ID signing + notarization would only matter there.
codesign --force --sign - "$OUT"/libbox3d.dylib
codesign --verify --verbose=2 "$OUT"/libbox3d.dylib

echo "Done: $OUT/libbox3d.dylib (ad-hoc signed)"
