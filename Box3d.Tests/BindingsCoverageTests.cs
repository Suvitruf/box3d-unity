using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace Box3d.Tests
{
    /// <summary>Guards the generated bindings layer: the extern surface matches the native export
    /// count, key functions from every header made it through, and every interop struct is
    /// blittable (pinnable) so DllImport marshalling never falls back to conversion.</summary>
    public class BindingsCoverageTests
    {
        private static Type UnsafeBindingsType => typeof(World).Assembly.GetType("Box3d.UnsafeBindings", throwOnError: true);

        private static MethodInfo[] Externs => UnsafeBindingsType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<DllImportAttribute>() != null)
            .ToArray();

        [Test]
        public void ExternCount_MatchesGeneratedSurface()
        {
            // 580 native exports minus 2 duplicate header declarations. Update when box3d is bumped.
            Assert.AreEqual(578, Externs.Length);
        }

        [Test]
        public void KeyFunctions_ExistAcrossAllHeaders()
        {
            var names = Externs.Select(m => m.Name).ToHashSet();

            // base.h / constants.h
            Assert.IsTrue(names.Contains("b3SetAllocator"));
            Assert.IsTrue(names.Contains("b3SetLengthUnitsPerMeter"));
            // math_functions.h
            Assert.IsTrue(names.Contains("b3ComputeCosSin"));
            // box3d.h — world/body/shape/joint/query surface
            Assert.IsTrue(names.Contains("b3World_CastRayClosest"));
            Assert.IsTrue(names.Contains("b3World_OverlapAABB"));
            Assert.IsTrue(names.Contains("b3World_Explode"));
            Assert.IsTrue(names.Contains("b3CreateRevoluteJoint"));
            Assert.IsTrue(names.Contains("b3CreateSphericalJoint"));
            Assert.IsTrue(names.Contains("b3World_GetContactEvents"));
            Assert.IsTrue(names.Contains("b3World_GetSensorEvents"));
            // collision.h — geometry builders, casts, character mover
            Assert.IsTrue(names.Contains("b3CreateMesh"));
            Assert.IsTrue(names.Contains("b3CreateHeightField"));
            Assert.IsTrue(names.Contains("b3ShapeDistance"));
            Assert.IsTrue(names.Contains("b3SolvePlanes"));
        }

        [Test]
        public void AllInteropStructs_AreBlittable()
        {
            var structTypes = typeof(World).Assembly.GetTypes()
                .Where(t => t.IsValueType && !t.IsEnum && !t.IsPrimitive && t.Namespace == "Box3d")
                .ToArray();

            Assert.Greater(structTypes.Length, 80, "expected the full generated struct surface");

            foreach (Type type in structTypes)
            {
                object instance = Activator.CreateInstance(type);
                GCHandle handle = default;
                try
                {
                    handle = GCHandle.Alloc(instance, GCHandleType.Pinned);
                }
                catch (ArgumentException)
                {
                    Assert.Fail($"{type.Name} is not blittable — it would be marshalled by conversion, breaking the ABI");
                }
                finally
                {
                    if (handle.IsAllocated) handle.Free();
                }
            }
        }

        [Test]
        public void ExternSignatures_UseNoClassTypes()
        {
            foreach (MethodInfo method in Externs)
            {
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    Type type = parameter.ParameterType;
                    Assert.IsFalse(type.IsClass && !type.IsPointer,
                        $"{method.Name}: parameter '{parameter.Name}' is a reference type ({type.Name})");
                }

                Type returnType = method.ReturnType;
                Assert.IsFalse(returnType.IsClass && !returnType.IsPointer && returnType != typeof(void),
                    $"{method.Name}: returns a reference type ({returnType.Name})");
            }
        }
    }
}
