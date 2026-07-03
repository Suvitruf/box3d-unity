using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace Box3d.Tests
{
    /// <summary>Guards the generated public API layer: every extern that is eligible for
    /// forwarding (mapped prefix, id-first, publicly-visible types only) must be reachable as a
    /// member on its public struct — either generated or hand-written. Skipped externs are the
    /// documented remainder (Docs/api-design.md).</summary>
    public class GeneratedApiTests
    {
        private static readonly (string Prefix, Type Struct, Type IdType)[] PrefixMap =
        {
            ("b3World_", typeof(World), typeof(WorldId)),
            ("b3Body_", typeof(Body), typeof(BodyId)),
            ("b3Shape_", typeof(Shape), typeof(ShapeId)),
            ("b3Joint_", typeof(Joint), typeof(JointId)),
            ("b3DistanceJoint_", typeof(DistanceJoint), typeof(JointId)),
            ("b3MotorJoint_", typeof(MotorJoint), typeof(JointId)),
            ("b3ParallelJoint_", typeof(ParallelJoint), typeof(JointId)),
            ("b3PrismaticJoint_", typeof(PrismaticJoint), typeof(JointId)),
            ("b3RevoluteJoint_", typeof(RevoluteJoint), typeof(JointId)),
            ("b3SphericalJoint_", typeof(SphericalJoint), typeof(JointId)),
            ("b3WeldJoint_", typeof(WeldJoint), typeof(JointId)),
            ("b3WheelJoint_", typeof(WheelJoint), typeof(JointId)),
            ("b3Contact_", typeof(Contact), typeof(ContactId)),
        };

        [Test]
        public void EveryEligibleExtern_IsReachableOnItsPublicStruct()
        {
            Type bindings = typeof(World).Assembly.GetType("Box3d.UnsafeBindings", throwOnError: true);
            var externs = bindings
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<DllImportAttribute>() != null);

            var missing = new List<string>();
            int eligible = 0;

            foreach (MethodInfo method in externs)
            {
                var entry = PrefixMap.FirstOrDefault(p => method.Name.StartsWith(p.Prefix, StringComparison.Ordinal));
                if (entry.Prefix == null) continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != entry.IdType) continue;
                if (!IsPubliclyUsable(method.ReturnType)) continue;
                if (parameters.Skip(1).Any(p => !IsPubliclyUsable(p.ParameterType))) continue;

                eligible++;
                string member = method.Name.Substring(entry.Prefix.Length);
                if (member == "GetType") member = $"Get{entry.Struct.Name}Type";

                if (entry.Struct.GetMember(member, BindingFlags.Public | BindingFlags.Instance).Length == 0)
                {
                    missing.Add($"{method.Name} → {entry.Struct.Name}.{member}");
                }
            }

            Assert.Greater(eligible, 280, "eligible extern surface unexpectedly small — generator logic drifted?");
            Assert.IsEmpty(missing,
                "externs eligible for forwarding but unreachable on the public API:\n" + string.Join("\n", missing));
        }

        private static bool IsPubliclyUsable(Type type)
        {
            if (type == typeof(void)) return true;
            if (type.IsPointer) return false;
            if (type == typeof(NativeBool)) return true;
            return type.IsVisible && (type.IsPrimitive || type.IsEnum || type.IsValueType);
        }
    }
}
