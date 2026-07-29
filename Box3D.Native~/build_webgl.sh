#!/usr/bin/env bash
# Builds libbox3d.a as a WebAssembly STATIC library for Unity WebGL.
#
# WebGL has no dynamic plugins: the archive is linked into the player build, and the C# side
# imports "__Internal" on WebGL (Box3D/Bindings/Box3DLibrary.cs). The Emscripten version MUST
# match the Unity editor's (Editor/Data/PlaybackEngines/WebGLSupport/BuildTools/Emscripten/...
# emscripten-version.txt) or linking may fail — built and verified with 3.1.39 (Unity 6000.0).
#
# WebGL players are single-threaded: the wrapper forces workerCount = 1 there.
#
# Environment (optional):
#   BOX3D_SRC     box3d checkout; auto-probed next to the repo if unset
#   EMSDK_ENV     path to emsdk_env.sh to source if emcmake is not already on PATH
#   BOX3D_DOUBLE  set (any value) to build the DOUBLE-precision variant. Like iOS, WebGL links
#                 statically as "__Internal", so the double archive keeps the name libbox3d.a and is
#                 staged under Box3D.Native~/double-staging/WebGL/ — NOT shipped by default. To use double on WebGL,
#                 EMBED the package, replace Plugins/WebGL/libbox3d.a with this one, and set the
#                 BOX3D_DOUBLE C# define. See Docs/double-precision-plan.md.
set -euo pipefail
cd "$(dirname "$0")"

if ! command -v emcmake >/dev/null; then
    [ -n "${EMSDK_ENV:-}" ] && source "$EMSDK_ENV"
fi
command -v emcmake >/dev/null || { echo "error: emcmake not found — install emsdk or set EMSDK_ENV" >&2; exit 1; }

if [ -z "${BOX3D_SRC:-}" ]; then
    for candidate in "$PWD/../../box3d" "$PWD/../../../../box3d"; do
        if [ -f "$candidate/include/box3d/box3d.h" ]; then BOX3D_SRC=$candidate; break; fi
    done
fi
[ -f "${BOX3D_SRC:-}/include/box3d/box3d.h" ] || {
    echo "error: box3d checkout not found — set BOX3D_SRC" >&2; exit 1; }

PREC=""; BUILD=build-webgl; OUT=../Plugins/WebGL
if [ -n "${BOX3D_DOUBLE:-}" ]; then
    PREC=-DBOX3D_DOUBLE_PRECISION=ON; BUILD=build-webgl-double; OUT=double-staging/WebGL
fi

emcmake cmake -S "$BOX3D_SRC" -B "$BUILD" \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=OFF \
  -DBOX3D_DISABLE_SIMD=ON \
  -DBOX3D_SAMPLES=OFF \
  -DBOX3D_UNIT_TESTS=OFF \
  -DBOX3D_BENCHMARKS=OFF \
  $PREC

cmake --build "$BUILD" -j "$(nproc)"

mkdir -p "$OUT"
ARCHIVE=$(find "$BUILD" -name "libbox3d.a" | head -1)
[ -n "$ARCHIVE" ] || { echo "error: libbox3d.a not produced" >&2; exit 1; }
cp "$ARCHIVE" "$OUT"/libbox3d.a

echo "Done: $OUT/libbox3d.a ($(du -h "$OUT"/libbox3d.a | cut -f1))"
