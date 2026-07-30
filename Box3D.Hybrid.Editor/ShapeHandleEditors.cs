using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Scene-view editing for the sphere shape: drag the radius like a Unity collider.
    /// Handles are drawn in a position+rotation frame with the lossy scale baked into dimensions,
    /// matching how the physics shape is built.</summary>
    [CustomEditor(typeof(Box3DSphereShape))]
    public class Box3DSphereShapeEditor : UnityEditor.Editor
    {
        private readonly SphereBoundsHandle _handle = new SphereBoundsHandle();
        // Cached once — OnSceneGUI runs per scene-view event, and FindProperty re-walks the path.
        private SerializedProperty _radius;
        private SerializedProperty _center;

        private void OnEnable()
        {
            _radius = serializedObject.FindProperty("Radius");
            _center = serializedObject.FindProperty("Center");
        }

        private void OnSceneGUI()
        {
            Transform t = ((Box3DSphereShape)target).transform;
            SerializedProperty radius = _radius;
            SerializedProperty center = _center;

            float scale = Mathf.Max(0.0001f, ShapeHandleMath.MaxAbs(t.lossyScale));
            using (new Handles.DrawingScope(Matrix4x4.TRS(t.position, t.rotation, Vector3.one)))
            {
                _handle.center = Vector3.Scale(center.vector3Value, t.lossyScale);
                _handle.radius = radius.floatValue * scale;

                EditorGUI.BeginChangeCheck();
                _handle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Edit Sphere Shape");
                    radius.floatValue = _handle.radius / scale;
                    center.vector3Value = ShapeHandleMath.Unscale(_handle.center, t.lossyScale);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }

    /// <summary>Scene-view editing for the box shape: drag the faces to resize.</summary>
    [CustomEditor(typeof(Box3DBoxShape))]
    public class Box3DBoxShapeEditor : UnityEditor.Editor
    {
        private readonly BoxBoundsHandle _handle = new BoxBoundsHandle();
        // Cached once — OnSceneGUI runs per scene-view event, and FindProperty re-walks the path.
        private SerializedProperty _size;
        private SerializedProperty _center;

        private void OnEnable()
        {
            _size = serializedObject.FindProperty("Size");
            _center = serializedObject.FindProperty("Center");
        }

        private void OnSceneGUI()
        {
            Transform t = ((Box3DBoxShape)target).transform;
            SerializedProperty size = _size;
            SerializedProperty center = _center;

            Vector3 scale = ShapeHandleMath.Abs(t.lossyScale);
            using (new Handles.DrawingScope(Matrix4x4.TRS(t.position, t.rotation, Vector3.one)))
            {
                _handle.center = Vector3.Scale(center.vector3Value, t.lossyScale);
                _handle.size = Vector3.Scale(size.vector3Value, scale);

                EditorGUI.BeginChangeCheck();
                _handle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Edit Box Shape");
                    size.vector3Value = ShapeHandleMath.Unscale(_handle.size, scale);
                    center.vector3Value = ShapeHandleMath.Unscale(_handle.center, t.lossyScale);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }

    /// <summary>Scene-view editing for the capsule shape: drag radius and height. Radius scales by
    /// the perpendicular axes and height by the capsule axis, matching the physics shape.</summary>
    [CustomEditor(typeof(Box3DCapsuleShape))]
    public class Box3DCapsuleShapeEditor : UnityEditor.Editor
    {
        private readonly CapsuleBoundsHandle _handle = new CapsuleBoundsHandle();
        // Cached once — OnSceneGUI runs per scene-view event, and FindProperty re-walks the path.
        private SerializedProperty _radius;
        private SerializedProperty _height;
        private SerializedProperty _center;
        private SerializedProperty _direction;

        private void OnEnable()
        {
            _radius = serializedObject.FindProperty("Radius");
            _height = serializedObject.FindProperty("Height");
            _center = serializedObject.FindProperty("Center");
            _direction = serializedObject.FindProperty("Direction");
        }

        private void OnSceneGUI()
        {
            Transform t = ((Box3DCapsuleShape)target).transform;
            SerializedProperty radius = _radius;
            SerializedProperty height = _height;
            SerializedProperty center = _center;
            int direction = _direction.enumValueIndex; // 0=X, 1=Y, 2=Z

            Vector3 absScale = ShapeHandleMath.Abs(t.lossyScale);
            float axisScale = Mathf.Max(0.0001f, absScale[direction]);
            float radialScale = Mathf.Max(0.0001f, ShapeHandleMath.MaxExcept(absScale, direction));

            _handle.heightAxis = (CapsuleBoundsHandle.HeightAxis)direction;
            using (new Handles.DrawingScope(Matrix4x4.TRS(t.position, t.rotation, Vector3.one)))
            {
                _handle.center = Vector3.Scale(center.vector3Value, t.lossyScale);
                _handle.radius = radius.floatValue * radialScale;
                _handle.height = height.floatValue * axisScale;

                EditorGUI.BeginChangeCheck();
                _handle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Edit Capsule Shape");
                    radius.floatValue = _handle.radius / radialScale;
                    height.floatValue = _handle.height / axisScale;
                    center.vector3Value = ShapeHandleMath.Unscale(_handle.center, t.lossyScale);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }

    internal static class ShapeHandleMath
    {
        public static float MaxAbs(Vector3 v)
        {
            return Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        public static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        // The largest component of v excluding the given axis index.
        public static float MaxExcept(Vector3 v, int axis)
        {
            float max = 0f;
            for (int i = 0; i < 3; i++)
            {
                if (i != axis) max = Mathf.Max(max, v[i]);
            }
            return max;
        }

        // Divides component-wise, treating a zero scale as 1 to avoid NaN.
        public static Vector3 Unscale(Vector3 v, Vector3 scale)
        {
            return new Vector3(
                Mathf.Approximately(scale.x, 0f) ? v.x : v.x / scale.x,
                Mathf.Approximately(scale.y, 0f) ? v.y : v.y / scale.y,
                Mathf.Approximately(scale.z, 0f) ? v.z : v.z / scale.z);
        }
    }
}
