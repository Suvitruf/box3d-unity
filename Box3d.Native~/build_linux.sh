#!/usr/bin/env bash
# Builds libbox3d.so (x64, Release, single precision) and copies it into the package.
# Run from WSL/Linux with gcc + cmake installed.
#
# Environment (optional): BOX3D_SRC — box3d checkout; auto-probed next to the repo if unset.
set -euo pipefail
cd "$(dirname "$0")"

if [ -z "${BOX3D_SRC:-}" ]; then
    for candidate in "$PWD/../../box3d" "$PWD/../../../../box3d"; do
        if [ -f "$candidate/include/box3d/box3d.h" ]; then BOX3D_SRC=$candidate; break; fi
    done
fi
[ -f "${BOX3D_SRC:-}/include/box3d/box3d.h" ] || {
    echo "error: box3d checkout not found — set BOX3D_SRC" >&2; exit 1; }

BUILD=build-linux
OUT=../Plugins/Linux/x86_64

cmake -S "$BOX3D_SRC" -B "$BUILD" \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=ON \
  -DBOX3D_SAMPLES=OFF \
  -DBOX3D_UNIT_TESTS=OFF \
  -DBOX3D_BENCHMARKS=OFF

cmake --build "$BUILD" -j "$(nproc)"

mkdir -p "$OUT"
cp "$BUILD"/bin/libbox3d.so "$OUT"/libbox3d.so 2>/dev/null || cp "$BUILD"/src/libbox3d.so "$OUT"/libbox3d.so

echo "Done: $OUT/libbox3d.so"
