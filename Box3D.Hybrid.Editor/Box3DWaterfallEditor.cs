using UnityEditor;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Waterfall inspector: the normal fields plus a one-click fix when the scene has no
    /// water to pour into, and a tap toggle for testing the flow live in play mode.</summary>
    [CustomEditor(typeof(Box3DWaterfall))]
    [CanEditMultipleObjects]
    public class Box3DWaterfallEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            SerializedProperty waterProp = serializedObject.FindProperty("Water");
            if (waterProp.objectReferenceValue == null &&
                !Object.FindAnyObjectByType<Box3DWater>(FindObjectsInactive.Include))
            {
                EditorGUILayout.HelpBox("There is no Box3DWater in the scene to pour into.", MessageType.Warning);
                if (GUILayout.Button("Create Water"))
                {
                    var go = new GameObject("Water", typeof(Box3DWater));
                    Undo.RegisterCreatedObjectUndo(go, "Create Box3D Water");
                }
                EditorGUILayout.Space();
            }

            DrawDefaultInspector();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                var waterfall = (Box3DWaterfall)target;
                string label = !Application.isPlaying ? "Toggle Flow (enter Play mode)"
                    : waterfall.IsFlowing ? "Stop Flow" : "Start Flow";
                if (GUILayout.Button(label))
                {
                    foreach (Object t in targets)
                    {
                        var fall = (Box3DWaterfall)t;
                        fall.IsFlowing = !fall.IsFlowing;
                    }
                }
            }
        }
    }
}
