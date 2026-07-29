#!/usr/bin/env bash
# Builds libbox3d.a as a STATIC library for iOS (arm64). RUN ON A MAC with Xcode.
# iOS plugins link statically — the C# side already imports "__Internal" on iOS
# (see Box3D/Bindings/Box3DLibrary.cs). Output goes to Plugins/iOS/.
# NOT YET RUN — no Mac available; verify + add a PluginImporter meta (iOS only) when first built.
#
# Environment (optional):
#   BOX3D_SRC     box3d checkout; auto-probed next to the repo if unset
#   BOX3D_DOUBLE  set (any value) to build the DOUBLE-precision variant. Static linking can't select
#                 by name (both archives export the same symbols → duplicate-symbol link error), so
#                 the double archive keeps the name libbox3d.a and is staged under Box3D.Native~/double-staging/iOS/ (outside Unity's asset scope, so it can never be auto-imported and linked by accident)
#                 — NOT shipped by default. To use double on iOS, EMBED the package (make it mutable),
#                 replace Plugins/iOS/libbox3d.a with this one, and set the BOX3D_DOUBLE C# define.
#                 See Docs/double-precision-plan.md.
set -euo pipefail
cd "$(dirname "$0")"

if [ -z "${BOX3D_SRC:-}" ]; then
    for candidate in "$PWD/../../box3d" "$PWD/../../../../box3d"; do
        if [ -f "$candidate/include/box3d/box3d.h" ]; then BOX3D_SRC=$candidate; break; fi
    done
fi
[ -f "${BOX3D_SRC:-}/include/box3d/box3d.h" ] || {
    echo "error: box3d checkout not found — set BOX3D_SRC" >&2; exit 1; }
PREC=""; BUILD=build-ios; OUT=../Plugins/iOS
if [ -n "${BOX3D_DOUBLE:-}" ]; then
    PREC=-DBOX3D_DOUBLE_PRECISION=ON; BUILD=build-ios-double; OUT=double-staging/iOS
fi

cmake -S "$BOX3D_SRC" -B "$BUILD" -G Xcode \
  -DCMAKE_SYSTEM_NAME=iOS \
  -DCMAKE_OSX_DEPLOYMENT_TARGET=13.0 \
  -DCMAKE_OSX_ARCHITECTURES=arm64 \
  -DBUILD_SHARED_LIBS=OFF \
  -DBOX3D_SAMPLES=OFF \
  -DBOX3D_UNIT_TESTS=OFF \
  -DBOX3D_BENCHMARKS=OFF \
  $PREC

cmake --build "$BUILD" --config Release -j

mkdir -p "$OUT"
find "$BUILD" -name "libbox3d.a" -path "*Release*" -exec cp {} "$OUT"/libbox3d.a \;

echo "Done: $OUT/libbox3d.a"
