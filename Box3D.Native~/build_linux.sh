#!/usr/bin/env bash
# Builds libbox3d.so (x64, Release) and copies it into the package.
# Run from WSL/Linux with gcc + cmake installed.
#
# Environment (optional):
#   BOX3D_SRC     box3d checkout; auto-probed next to the repo if unset
#   BOX3D_DOUBLE  set (any value) to build the DOUBLE-precision variant → libbox3d_d.so
#                 (a distinct name so it ships alongside the single lib; the C# BOX3D_DOUBLE
#                 define selects it by name). Unset = single precision (default).
set -euo pipefail
cd "$(dirname "$0")"

if [ -z "${BOX3D_SRC:-}" ]; then
    for candidate in "$PWD/../../box3d" "$PWD/../../../../box3d"; do
        if [ -f "$candidate/include/box3d/box3d.h" ]; then BOX3D_SRC=$candidate; break; fi
    done
fi
[ -f "${BOX3D_SRC:-}/include/box3d/box3d.h" ] || {
    echo "error: box3d checkout not found — set BOX3D_SRC" >&2; exit 1; }

PREC=""; SUFFIX=""; BUILD=build-linux
if [ -n "${BOX3D_DOUBLE:-}" ]; then
    PREC=-DBOX3D_DOUBLE_PRECISION=ON; SUFFIX=_d; BUILD=build-linux-double
fi
OUT=../Plugins/Linux/x86_64

cmake -S "$BOX3D_SRC" -B "$BUILD" \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=ON \
  -DBOX3D_SAMPLES=OFF \
  -DBOX3D_UNIT_TESTS=OFF \
  -DBOX3D_BENCHMARKS=OFF \
  $PREC

cmake --build "$BUILD" -j "$(nproc)"

mkdir -p "$OUT"
cp "$BUILD"/bin/libbox3d.so "$OUT/libbox3d$SUFFIX.so" 2>/dev/null || cp "$BUILD"/src/libbox3d.so "$OUT/libbox3d$SUFFIX.so"

echo "Done: $OUT/libbox3d$SUFFIX.so"
