#!/usr/bin/env bash
# Builds libbox3d.so for Android on a Linux or macOS host. (On Windows/WSL use build_android.bat,
# which drives Unity's bundled Windows NDK.)
#
# Environment (all optional):
#   ANDROID_NDK  NDK root (ANDROID_NDK_ROOT / ANDROID_NDK_HOME also honored);
#                probed from Unity Hub installs if unset
#   ANDROID_ABI  target ABI, default arm64-v8a (also: armeabi-v7a, x86_64)
#   BOX3D_SRC    box3d checkout; auto-probed next to the repo if unset
set -euo pipefail
cd "$(dirname "$0")"

if [ -z "${BOX3D_SRC:-}" ]; then
    for candidate in "$PWD/../../box3d" "$PWD/../../../../box3d"; do
        if [ -f "$candidate/include/box3d/box3d.h" ]; then BOX3D_SRC=$candidate; break; fi
    done
fi
[ -f "${BOX3D_SRC:-}/include/box3d/box3d.h" ] || {
    echo "error: box3d checkout not found — set BOX3D_SRC" >&2; exit 1; }

# NDK: explicit env first, then Unity Hub installs (Linux and macOS layouts), newest wins.
NDK="${ANDROID_NDK:-${ANDROID_NDK_ROOT:-${ANDROID_NDK_HOME:-}}}"
if [ -z "$NDK" ]; then
    for candidate in \
        "$HOME/Unity/Hub/Editor"/*/Editor/Data/PlaybackEngines/AndroidPlayer/NDK \
        "/Applications/Unity/Hub/Editor"/*/Unity.app/Contents/PlaybackEngines/AndroidPlayer/NDK; do
        [ -f "$candidate/source.properties" ] && NDK=$candidate
    done
fi
[ -f "${NDK:-}/source.properties" ] || {
    echo "error: Android NDK not found — set ANDROID_NDK" >&2; exit 1; }

ABI="${ANDROID_ABI:-arm64-v8a}"
BUILD="build-android-$ABI"
OUT="../Plugins/Android/$ABI"

GENERATOR_ARGS=()
command -v ninja >/dev/null && GENERATOR_ARGS=(-G Ninja)

cmake -S "$BOX3D_SRC" -B "$BUILD" "${GENERATOR_ARGS[@]}" \
  -DCMAKE_TOOLCHAIN_FILE="$NDK/build/cmake/android.toolchain.cmake" \
  -DANDROID_ABI="$ABI" \
  -DANDROID_PLATFORM=android-23 \
  -DCMAKE_BUILD_TYPE=Release \
  -DCMAKE_SHARED_LINKER_FLAGS="-Wl,-z,max-page-size=16384" \
  -DBUILD_SHARED_LIBS=ON \
  -DBOX3D_SAMPLES=OFF \
  -DBOX3D_UNIT_TESTS=OFF \
  -DBOX3D_BENCHMARKS=OFF

cmake --build "$BUILD" -j

mkdir -p "$OUT"
cp "$BUILD"/bin/libbox3d.so "$OUT"/libbox3d.so 2>/dev/null || cp "$BUILD"/src/libbox3d.so "$OUT"/libbox3d.so

STRIP=$(ls "$NDK"/toolchains/llvm/prebuilt/*/bin/llvm-strip 2>/dev/null | head -1)
[ -n "$STRIP" ] && "$STRIP" --strip-unneeded "$OUT"/libbox3d.so

echo "Done: $OUT/libbox3d.so"
