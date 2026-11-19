#!/usr/bin/env python3
"""Prepare ClangSharp imports for the shipped Box3D libraries and precision modes."""
import re
import sys

path = sys.argv[1]
with open(path) as f:
    text = f.read()

# One extern = DllImport attribute block up to and including the declaration line.
extern_pattern = re.compile(
    r"\n( +\[DllImport\(\"box3d\"[^\n]*\]\n(?: +\[[^\n]*\]\n)* +public static extern [^\n]+\n)")

seen = set()
removed = 0

def dedupe(match):
    global removed
    block = match.group(1)
    name = re.search(r"extern \S+(?: \S+)? (\w+)\(", block).group(1)
    if name in seen:
        removed += 1
        return "\n"
    seen.add(name)
    return match.group(0)

text = extern_pattern.sub(dedupe, text)

# Parsing on Linux resolves uint64_t to 'unsigned long', which ClangSharp maps to UIntPtr —
# semantically wrong (and invalid as const). Replace globally with ulong, EXCEPT the 3 genuine
# size_t functions (b3RecPlayer keyframe budget): size_t is 32-bit on wasm, so those must stay
# UIntPtr (platform word) — wasm-ld reports signature mismatches otherwise.
SIZE_T_FUNCTIONS = ("b3RecPlayer_SetKeyframePolicy", "b3RecPlayer_GetKeyframeBudget",
                    "b3RecPlayer_GetKeyframeBytes")
lines_out = []
for line in text.split("\n"):
    if "UIntPtr" in line and not any(fn in line for fn in SIZE_T_FUNCTIONS):
        line = line.replace("UIntPtr", "ulong")
    lines_out.append(line)
text = "\n".join(lines_out)

# b3InternalAssert is only compiled into DEBUG native builds (core.c guards it with
# !defined(NDEBUG)). Desktop lazy binding never notices, but WebGL links every extern eagerly
# and fails on the missing symbol — so drop the extern entirely.
removed_assert = 0
def drop_internal_assert(match):
    global removed_assert
    if "b3InternalAssert" in match.group(1):
        removed_assert += 1
        return "\n"
    return match.group(0)
text = extern_pattern.sub(drop_internal_assert, text)
print(f"postprocess: removed {removed_assert} debug-only extern(s)")

# These C macros read the current length scale on each use. A readonly field would freeze
# its value the first time the bindings type is initialized.
text = re.sub(r"public static readonly float (B3_\w+) = ([^;\n]*b3GetLengthUnitsPerMeter\(\)[^;\n]*);",
              r"public static float \1 => \2;", text)

# Platform-aware library name: iOS needs DllImport("__Internal") (static linking). The constant
# lives in the hand-written UnsafeBindings.DllName.cs partial.
text = text.replace('[DllImport("box3d"', "[DllImport(Box3DLibrary.Name")

# Precision tripwire: double-precision box3d exports b3CreateWorld AS b3CreateWorldDoublePrecision
# (box3d.h renames it via macro so mismatched clients fail to link on the first call every program
# makes). Mirror that: under BOX3D_DOUBLE the import needs an EntryPoint override.
create_world = re.search(
    r"( +)(\[DllImport\(Box3DLibrary\.Name[^\n]*\]\n(?: +\[[^\n]*\]\n)* +public static extern \w+ b3CreateWorld\()",
    text)
assert create_world, "postprocess: b3CreateWorld extern not found for the precision tripwire"
indent, block = create_world.group(1), create_world.group(2)
attr_end = block.index("]\n") + 1
attr = block[:attr_end]
double_attr = attr.replace("[DllImport(Box3DLibrary.Name",
                           '[DllImport(Box3DLibrary.Name, EntryPoint = "b3CreateWorldDoublePrecision"')
replacement = (f"#if BOX3D_DOUBLE\n{indent}{double_attr}\n"
               f"#else\n{indent}{attr}\n#endif\n{indent}{block[attr_end + 1:]}")
text = text[:create_world.start()] + replacement + text[create_world.end():]
print("postprocess: added BOX3D_DOUBLE EntryPoint override for b3CreateWorld")

with open(path, "w") as f:
    f.write(text)

print(f"postprocess: {len(seen)} externs kept, {removed} duplicates removed")
