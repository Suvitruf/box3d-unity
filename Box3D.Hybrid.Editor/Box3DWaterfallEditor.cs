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
        private SerializedProperty _waterProp;
        // The "is there any water in the scene?" scan, run once and then only when the hierarchy
        // actually changes — not on every GUI event.
        private bool _sceneHasWater;
        private bool _scanned;

        private void OnEnable()
        {
            _waterProp = serializedObject.FindProperty("Water");
            EditorApplication.hierarchyChanged += InvalidateScan;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= InvalidateScan;
        }

        private void InvalidateScan() => _scanned = false;

        public override void OnInspectorGUI()
        {
            if (!_waterProp.objectReferenceValue && !_scanned)
            {
                _scanned = true;
                _sceneHasWater = Object.FindAnyObjectByType<Box3DWater>(FindObjectsInactive.Include);
            }
            if (!_waterProp.objectReferenceValue && !_sceneHasWater)
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
