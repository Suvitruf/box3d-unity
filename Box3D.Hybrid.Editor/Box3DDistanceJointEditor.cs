using UnityEditor;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    [CustomEditor(typeof(Box3DDistanceJoint))]
    public class Box3DDistanceJointEditor : Box3DJointEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBase();
            Field("ConnectedAnchor");
            Field("Length");
            ConditionalField("UseSpring", "Hertz", "DampingRatio");
            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI(); // the anchor on this body

            // The connected anchor: on the connected body if any, otherwise a world point.
            var joint = (Box3DDistanceJoint)target;
            SerializedProperty connected = Property("ConnectedBody");
            var connectedBody = connected.objectReferenceValue as Box3DBody;
            if (connectedBody)
            {
                DrawAnchorHandle("ConnectedAnchor", connectedBody.transform, new Color(1f, 0.6f, 0.2f));
            }
            else
            {
                SerializedProperty anchor = Property("ConnectedAnchor");
                EditorGUI.BeginChangeCheck();
                Handles.color = new Color(1f, 0.6f, 0.2f);
                float size = HandleUtility.GetHandleSize(anchor.vector3Value) * 0.12f;
                Vector3 moved = Handles.FreeMoveHandle(anchor.vector3Value, size, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Move Connected Anchor");
                    anchor.vector3Value = moved;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
