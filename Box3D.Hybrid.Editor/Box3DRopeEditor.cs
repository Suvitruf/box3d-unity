using System;
using UnityEditor;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Rope inspector, modeled on Source 2's Hammer cable workflow. The Scene view always
    /// shows the true drape while editing: the preview runs a real Box3D simulation in a throwaway
    /// world with the scene frozen as static collision, so the rope hangs over geometry exactly as
    /// it will in play mode — and **Bake Current Shape** captures that. **Simulate in Editor**
    /// animates the same simulation live (drag an endpoint and the rope follows); **Make Dynamic**
    /// reverts a bake. When End Point is empty the far end gets a draggable handle.</summary>
    [CustomEditor(typeof(Box3DRope))]
    public class Box3DRopeEditor : UnityEditor.Editor
    {
        private Box3DRopePreview _simulation;
        private Vector3[] _nodes;
        private bool _simulating;
        private Vector3 _lastStart;
        private Vector3 _lastEnd;

        private Box3DRope Rope => (Box3DRope)target;

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            Undo.undoRedoPerformed += RefreshPreview;
            RefreshPreview();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            Undo.undoRedoPerformed -= RefreshPreview;
            StopSimulation();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                // Parameters changed: a running simulation restarts with them; a static preview
                // just recomputes.
                if (_simulating) StopSimulation(keepSimulating: true);
                else RefreshPreview();
            }

            if (Application.isPlaying) return;
            EditorGUILayout.Space();

            if (Rope.CurrentMode == Box3DRope.RopeMode.Dynamic)
            {
                bool simulate = GUILayout.Toggle(_simulating,
                    _simulating ? "■ Stop Editor Simulation" : "▶ Simulate in Editor", GUI.skin.button);
                if (simulate != _simulating)
                {
                    _simulating = simulate;
                    if (!_simulating)
                    {
                        StopSimulation();
                        RefreshPreview();
                    }
                }
                if (GUILayout.Button("Bake Current Shape"))
                {
                    Undo.RecordObject(Rope, "Bake Box3D Rope");
                    Rope.Bake(_nodes ?? Rope.ComputeSettledPoints());
                    EditorUtility.SetDirty(Rope);
                    _simulating = false;
                    StopSimulation();
                    RefreshPreview();
                }
                EditorGUILayout.HelpBox("Dynamic: simulated in game as capsule segments + ball joints. " +
                    "The preview collides with the scene's Box3D shapes (frozen at their current pose). " +
                    "Bake to freeze the current hang into a static cable instead.", MessageType.None);
            }
            else
            {
                if (GUILayout.Button("Make Dynamic (clear bake)"))
                {
                    Undo.RecordObject(Rope, "Un-bake Box3D Rope");
                    Rope.ClearBake();
                    EditorUtility.SetDirty(Rope);
                    RefreshPreview();
                }
                EditorGUILayout.HelpBox("Baked: a frozen curve — no simulation in game, optional static collision.",
                    MessageType.None);
            }
        }

        private void OnSceneGUI()
        {
            if (Application.isPlaying) return;

            // A draggable far-end handle when no End Point transform is assigned.
            SerializedProperty endPoint = serializedObject.FindProperty("EndPoint");
            if (!endPoint.objectReferenceValue)
            {
                SerializedProperty offset = serializedObject.FindProperty("EndOffset");
                Vector3 world = Rope.transform.TransformPoint(offset.vector3Value);
                EditorGUI.BeginChangeCheck();
                world = Handles.PositionHandle(world, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    offset.vector3Value = Rope.transform.InverseTransformPoint(world);
                    serializedObject.ApplyModifiedProperties();
                }
            }

            // Re-drape when either endpoint is moved (the running simulation follows by itself).
            if (!_simulating && (_lastStart != Rope.StartWorld || _lastEnd != Rope.EndWorld))
            {
                RefreshPreview();
            }
        }

        private void OnEditorUpdate()
        {
            if (!_simulating || Application.isPlaying || !Rope) return;

            try
            {
                _simulation ??= new Box3DRopePreview(Rope);
                _simulation.Step(1f / 60f);
                _simulation.Step(1f / 60f);
                _nodes = _simulation.Nodes;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Box3D] Rope editor simulation unavailable: {e.Message}", Rope);
                _simulating = false;
                StopSimulation();
                return;
            }
            Rope.ApplyToLine(_nodes);
            SceneView.RepaintAll();
        }

        // keepSimulating: drop only the world (it rebuilds next tick with fresh parameters).
        private void StopSimulation(bool keepSimulating = false)
        {
            _simulation?.Dispose();
            _simulation = null;
            if (!keepSimulating) _simulating = false;
        }

        private void RefreshPreview()
        {
            // While the live editor simulation runs it owns the preview world — a second
            // Box3DRopePreview here would detach scene geometry twice and leak the first copy.
            if (_simulating || !Rope || Application.isPlaying) return;
            _lastStart = Rope.StartWorld;
            _lastEnd = Rope.EndWorld;

            if (Rope.CurrentMode == Box3DRope.RopeMode.Baked && Rope.HasBake)
            {
                Rope.ApplyToLine(Rope.BakedToWorld());
                return;
            }

            try
            {
                // The real simulation, scene collision included — what play mode will produce.
                using var simulation = new Box3DRopePreview(Rope);
                _nodes = (Vector3[])simulation.Settle().Clone();
            }
            catch (Exception e)
            {
                // No native library (e.g. unsupported editor platform): fall back to the
                // collision-less verlet hang so the preview still works.
                Debug.LogWarning($"[Box3D] Rope preview fell back to verlet (native sim unavailable): {e.Message}", Rope);
                _nodes = Rope.ComputeSettledPoints();
            }
            Rope.ApplyToLine(_nodes);
        }
    }
}
