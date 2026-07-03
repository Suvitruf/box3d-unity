#!/usr/bin/env bash
# Regenerates the raw Box3d bindings + public API forwarders (run from WSL/Linux).
#
# Environment (all optional):
#   BOX3D_SRC              box3d checkout; auto-probed next to the repo if unset
#   CLANG_BUILTIN_INCLUDE  dir providing stdbool.h etc.; newest GCC include dir if unset
#
# Requires: ~/.dotnet SDK + ClangSharpPInvokeGenerator global tool + python3.
set -euo pipefail
cd "$(dirname "$0")"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$HOME/.dotnet/tools:$PATH"

# Locate the box3d checkout: env, sibling of the public repo, or sibling of the host project.
if [ -z "${BOX3D_SRC:-}" ]; then
    for candidate in "$PWD/../../../box3d" "$PWD/../../../../../box3d"; do
        if [ -f "$candidate/include/box3d/box3d.h" ]; then BOX3D_SRC=$candidate; break; fi
    done
fi
[ -f "${BOX3D_SRC:-}/include/box3d/box3d.h" ] || {
    echo "error: box3d checkout not found — set BOX3D_SRC" >&2; exit 1; }

# libclang needs freestanding headers (stdbool.h, stdint.h, ...); borrow GCC's.
if [ -z "${CLANG_BUILTIN_INCLUDE:-}" ]; then
    CLANG_BUILTIN_INCLUDE=$(ls -d /usr/lib/gcc/x86_64-linux-gnu/*/include 2>/dev/null | sort -V | tail -1)
fi
[ -f "${CLANG_BUILTIN_INCLUDE:-}/stdbool.h" ] || {
    echo "error: no stdbool.h — install gcc or set CLANG_BUILTIN_INCLUDE" >&2; exit 1; }

rm -rf generated
mkdir -p generated

# The generator exits non-zero on benign warnings (function-like macros) — don't abort on it,
# but do fail if no output was produced.
ClangSharpPInvokeGenerator @box3d.rsp \
    --file-directory "$BOX3D_SRC/include" \
    --include-directory "$BOX3D_SRC/include" \
    --include-directory "$CLANG_BUILTIN_INCLUDE" \
    || true
test -s generated/UnsafeBindings.g.cs

python3 postprocess.py generated/UnsafeBindings.g.cs
python3 generate_api.py

# Deploy into the module.
cp generated/UnsafeBindings.g.cs ../../Box3d/Bindings/UnsafeBindings.g.cs
rm -f ../../Box3d/Generated/*.g.cs
mkdir -p ../../Box3d/Generated
cp generated/api/*.g.cs ../../Box3d/Generated/

echo "Generated:"
wc -l generated/UnsafeBindings.g.cs generated/api/*.g.cs | tail -3
