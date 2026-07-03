#!/usr/bin/env python3
"""Post-processes ClangSharp output:
1. Removes duplicate extern declarations (identical re-declarations across box3d headers).
2. Makes the UnsafeBindings class internal (raw layer is not public API; also avoids CS0050
   for externs returning internal types like BodyEventsRaw).
"""
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

text = text.replace("public static unsafe partial class UnsafeBindings",
                    "internal static unsafe partial class UnsafeBindings")
text = text.replace("public static partial class UnsafeBindings",
                    "internal static unsafe partial class UnsafeBindings")

# Parsing on Linux resolves uint64_t to 'unsigned long', which ClangSharp maps to UIntPtr —
# semantically wrong (and invalid as const). Replace globally with ulong. Caveat: the 3 genuine
# size_t functions (b3RecPlayer keyframe budget) also become ulong; identical ABI on all 64-bit
# targets, revisit if a 32-bit platform is ever shipped.
text = text.replace("UIntPtr", "ulong")

# Platform-aware library name: iOS needs DllImport("__Internal") (static linking). The constant
# lives in the hand-written UnsafeBindings.DllName.cs partial.
text = text.replace('[DllImport("box3d"', "[DllImport(Box3dLibrary.Name")

with open(path, "w") as f:
    f.write(text)

print(f"postprocess: {len(seen)} externs kept, {removed} duplicates removed")
