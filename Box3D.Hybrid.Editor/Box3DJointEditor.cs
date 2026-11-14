using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Shared drawing helpers for joint inspectors: the common base fields, a conditional
    /// group whose sub-fields appear only when a toggle is on, and a draggable Scene-view handle for
    /// the anchor. Inspector fields go through <c>serializedObject</c>, which spans a multi-object
    /// selection and shows mixed values by itself; Scene handles go through the current target's
    /// own SerializedObject (<see cref="SceneTarget"/>), because Unity calls OnSceneGUI once per
    /// selected joint and a handle must move only that joint's anchor.</summary>
    public abstract class Box3DJointEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, SerializedProperty> _properties =
            new Dictionary<string, SerializedProperty>();
        private readonly TargetSerializedObjects _sceneTargets = new TargetSerializedObjects();

        protected virtual void OnDisable()
        {
            _sceneTargets.Dispose();
        }

        /// <summary>serializedObject.FindProperty with a per-editor cache — the inspector redraws
        /// several times per frame, and FindProperty re-walks the property path each call.</summary>
        protected SerializedProperty Property(string name)
        {
            if (!_properties.TryGetValue(name, out SerializedProperty property))
            {
                property = serializedObject.FindProperty(name);
                _properties[name] = property;
            }
            return property;
        }

        /// <summary>The SerializedObject of the joint OnSceneGUI is currently drawing, refreshed.
        /// Apply handle edits through it, never through <c>serializedObject</c>.</summary>
        protected SerializedObject SceneTarget => _sceneTargets.Update(target);

        /// <summary>A property of <see cref="SceneTarget"/>, cached per joint.</summary>
        protected SerializedProperty SceneProperty(string name)
        {
            return _sceneTargets.Property(target, name);
        }

        /// <summary>Draws a draggable handle for a local-anchor property on a body's Transform.</summary>
        protected void DrawAnchorHandle(string anchorProperty, Transform body, Color color)
        {
            SerializedObject joint = SceneTarget;
            SerializedProperty anchor = SceneProperty(anchorProperty);
            Vector3 world = body.TransformPoint(anchor.vector3Value);

            Handles.color = color;
            float size = HandleUtility.GetHandleSize(world) * 0.12f;
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Move Joint Anchor");
                anchor.vector3Value = body.InverseTransformPoint(moved);
                joint.ApplyModifiedProperties();
            }
        }

        protected virtual void OnSceneGUI()
        {
            DrawAnchorHandle("Anchor", ((Box3DJoint)target).transform, Color.yellow);
        }

        /// <summary>Draws the connected body, anchor, and collide-connected fields.</summary>
        protected void DrawBase()
        {
            Field("ConnectedBody");
            Field("Anchor");
            Field("CollideConnected");
        }

        protected void Field(string propertyName)
        {
            EditorGUILayout.PropertyField(Property(propertyName));
        }

        /// <summary>Draws a bool toggle and, only when it is on, its indented sub-fields.</summary>
        protected void ConditionalField(string toggle, params string[] subFields)
        {
            SerializedProperty toggleProperty = Property(toggle);
            EditorGUILayout.PropertyField(toggleProperty);
            // Keep the sub-fields reachable when the selected joints disagree on the toggle.
            if (!toggleProperty.boolValue && !toggleProperty.hasMultipleDifferentValues) return;

            EditorGUI.indentLevel++;
            foreach (string subField in subFields) Field(subField);
            EditorGUI.indentLevel--;
        }
    }
}
