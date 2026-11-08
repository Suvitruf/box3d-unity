using System;
using System.Linq;
using System.Reflection;
using Box3D.Hybrid;
using Box3D.Hybrid.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Box3D.Tests
{
    /// <summary>Every hybrid component inspector supports multi-object editing (public issue #7).
    /// A custom editor without [CanEditMultipleObjects] makes Unity show "Multi-object editing not
    /// supported" for a selection of two or more of that component.</summary>
    public class MultiObjectEditingTests
    {
        [Test]
        public void EveryCustomEditorAllowsMultipleObjects()
        {
            string[] missing = typeof(Box3DBodyEditor).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.GetCustomAttribute<CustomEditor>() != null &&
                            t.GetCustomAttribute<CanEditMultipleObjects>() == null)
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToArray();
            Assert.IsEmpty(missing, "Custom editors without [CanEditMultipleObjects]: " + string.Join(", ", missing));
        }

        [TestCase(typeof(Box3DBody), typeof(Box3DBodyEditor))]
        [TestCase(typeof(Box3DBoxShape), typeof(Box3DBoxShapeEditor))]
        [TestCase(typeof(Box3DSphereShape), typeof(Box3DSphereShapeEditor))]
        [TestCase(typeof(Box3DMeshShape), typeof(Box3DMeshShapeEditor))]
        [TestCase(typeof(Box3DHingeJoint), typeof(Box3DHingeJointEditor))]
        [TestCase(typeof(Box3DDistanceJoint), typeof(Box3DDistanceJointEditor))]
        public void MultiSelectionGetsTheCustomEditor(Type component, Type editor)
        {
            var a = new GameObject("a", component);
            var b = new GameObject("b", component);
            UnityEditor.Editor inspector = null;
            try
            {
                inspector = UnityEditor.Editor.CreateEditor(new Object[] { a.GetComponent(component), b.GetComponent(component) });
                Assert.IsInstanceOf(editor, inspector);
                Assert.AreEqual(2, inspector.targets.Length);
            }
            finally
            {
                if (inspector) Object.DestroyImmediate(inspector);
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }
    }
}
