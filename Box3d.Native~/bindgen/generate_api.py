#!/usr/bin/env python3
"""Generates public forwarding methods from the generated externs.

Parses generated/UnsafeBindings.g.cs, and for every extern named b3{Type}_{Method} whose first
parameter is the type's id and whose signature uses only public blittable types, emits
    public {Ret} {Method}(...) => UnsafeBindings.b3{Type}_{Method}(Id, ...);
into api/{Type}.g.cs as a partial struct. Everything else lands in api/skipped-externs.txt with a
reason, for hand-wrapping or explicit deferral (see Docs/api-design.md).

Hand-written members win: any method whose name is already declared on the hand-written partial
is skipped.
"""
import os
import re
import sys
from collections import defaultdict

BINDINGS = "generated/UnsafeBindings.g.cs"
MODULE_ROOT = "../.."
API_DIR = "generated/api"

# b3 prefix -> (public struct, id type)
PREFIX_MAP = {
    "b3World_": ("World", "WorldId"),
    "b3Body_": ("Body", "BodyId"),
    "b3Shape_": ("Shape", "ShapeId"),
    "b3Joint_": ("Joint", "JointId"),
    "b3DistanceJoint_": ("DistanceJoint", "JointId"),
    "b3MotorJoint_": ("MotorJoint", "JointId"),
    "b3ParallelJoint_": ("ParallelJoint", "JointId"),
    "b3PrismaticJoint_": ("PrismaticJoint", "JointId"),
    "b3RevoluteJoint_": ("RevoluteJoint", "JointId"),
    "b3SphericalJoint_": ("SphericalJoint", "JointId"),
    "b3WeldJoint_": ("WeldJoint", "JointId"),
    "b3WheelJoint_": ("WheelJoint", "JointId"),
    "b3Contact_": ("Contact", "ContactId"),
}

# Types allowed in generated public signatures (blittable + already public).
ALLOWED = {
    "void", "float", "int", "uint", "ulong", "long", "ushort", "short", "byte", "IntPtr",
    "NativeBool",
    "Unity.Mathematics.float2", "Unity.Mathematics.float3", "Unity.Mathematics.float4",
    "Unity.Mathematics.quaternion",
    "WorldId", "BodyId", "ShapeId", "JointId", "ContactId",
    "B3Transform", "B3Aabb", "B3Matrix3",
    "Sphere", "Capsule", "BoxHull", "Box3dVersion",
    "BodyType", "ShapeType",
    "WorldDef", "BodyDef", "ShapeDef", "CollisionFilter", "SurfaceMaterial", "Capacity",
    "MotionLocks", "QueryFilter", "MassData", "RayResult", "ExplosionDef", "B3Plane", "PlaneResult", "CollisionPlane", "PlaneSolverResult",
}

# Avoid hiding System.Object members.
RENAMES = {"GetType": lambda struct: f"Get{struct}Type"}

NATIVE_ATTR = re.compile(r"\[NativeTypeName\(\"[^\"]*\"\)\] ?")
EXTERN = re.compile(r"^\s*(?:public|internal) static extern ([\w\.\*]+) (\w+)\((.*)\);\s*$")
HAND_MEMBER = re.compile(r"public [^\n=(]*?(\w+)\s*(?:\(|=>|\{|;)")
HAND_STRUCT = re.compile(r"partial struct (\w+)")


def parse_hand_written_members():
    """struct name -> set of member names declared in hand-written (non-generated) sources."""
    members = defaultdict(set)
    src_dir = os.path.join(MODULE_ROOT, "Box3d")
    for name in os.listdir(src_dir):
        if not name.endswith(".cs"):
            continue
        with open(os.path.join(src_dir, name)) as f:
            text = f.read()
        # Split the file into struct-scoped chunks so members attribute to the right struct.
        parts = HAND_STRUCT.split(text)
        for i in range(1, len(parts), 2):
            struct, body = parts[i], parts[i + 1]
            for match in HAND_MEMBER.finditer(body):
                members[struct].add(match.group(1))
    return members


def main():
    with open(BINDINGS) as f:
        lines = f.readlines()

    hand_written = parse_hand_written_members()
    forwarders = defaultdict(list)
    skipped = []
    generated_count = 0

    for line in lines:
        line = NATIVE_ATTR.sub("", line)
        match = EXTERN.match(line)
        if not match:
            continue
        ret, name, params_text = match.groups()

        prefix = next((p for p in PREFIX_MAP if name.startswith(p)), None)
        if prefix is None:
            if name.startswith("b3") and "_" in name:
                skipped.append((name, "no mapped prefix"))
            continue
        struct, id_type = PREFIX_MAP[prefix]
        method = name[len(prefix):]

        params = [p.strip() for p in params_text.split(",")] if params_text.strip() else []
        if not params or params[0].split()[0] != id_type:
            skipped.append((name, f"first param is not {id_type}"))
            continue

        rest = []
        reject = None
        for param in params[1:]:
            type_name, param_name = param.rsplit(" ", 1)
            if "*" in type_name:
                reject = f"pointer param ({type_name})"
                break
            if type_name not in ALLOWED:
                reject = f"non-public type ({type_name})"
                break
            rest.append((type_name.replace("NativeBool", "bool"), param_name))
        if reject is None and ("*" in ret or (ret not in ALLOWED)):
            reject = f"non-public return ({ret})"
        if reject is not None:
            skipped.append((name, reject))
            continue

        if method in hand_written.get(struct, ()):  # hand-written wins
            continue
        if method in RENAMES:
            method = RENAMES[method](struct)

        public_ret = ret.replace("NativeBool", "bool")
        sig = ", ".join(f"{t} {n}" for t, n in rest)
        args = ", ".join(["Id"] + [n for _, n in rest])
        forwarders[struct].append(
            f"        public {public_ret} {method}({sig}) => UnsafeBindings.{name}({args});")
        generated_count += 1

    os.makedirs(API_DIR, exist_ok=True)
    for struct, methods in sorted(forwarders.items()):
        with open(os.path.join(API_DIR, f"{struct}.g.cs"), "w") as f:
            f.write("// <auto-generated/> Produced by bindgen/generate_api.py — do not edit.\n")
            f.write("// Regenerate with generate.sh. Hand-written members belong in the other partial.\n\n")
            f.write("using System;\n\n")
            f.write("namespace Box3d\n{\n")
            f.write(f"    public partial struct {struct}\n    {{\n")
            f.write("\n".join(methods))
            f.write("\n    }\n}\n")

    with open(os.path.join(API_DIR, "skipped-externs.txt"), "w") as f:
        f.write("# Externs not forwarded by generate_api.py — hand-wrap or defer (Docs/api-design.md).\n")
        for name, reason in sorted(skipped):
            f.write(f"{name}: {reason}\n")

    print(f"generate_api: {generated_count} forwarders across {len(forwarders)} structs, "
          f"{len(skipped)} skipped")


if __name__ == "__main__":
    os.chdir(os.path.dirname(os.path.abspath(sys.argv[0])))
    main()
