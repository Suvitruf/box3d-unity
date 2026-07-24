using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Scene-view tool that runs live physics on the selected bodies while the rest of
    /// the scene stays frozen as static collision — settle props into natural rest poses without
    /// entering play mode. <b>Space</b> starts/stops the simulation, <b>left-drag</b> grabs a
    /// simulating body and pulls it with the cursor (release mid-drag to throw), <b>Esc</b> stops.
    /// Positions land on the real Transforms; each run is a single undo step.</summary>
    [EditorTool("Box3D Physics Simulation")]
    public sealed class Box3DPhysicsSimulationTool : EditorTool
    {
        private const string IconPath = "Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DPhysicsSim.png";
        private const float StepTime = 1f / 60f;
        private const int MaxCatchUpSteps = 4;

        private static readonly Color AccentColor = new Color(0.75f, 0.53f, 0.92f, 0.95f); // the world/simulation purple

        // Scene-view panel bounds. Clicks inside it must reach the GUI (the Stop button), so the
        // grab handler leaves them alone.
        private static readonly Rect PanelRect = new Rect(10f, 10f, 260f, 120f);

        private Box3DEditorSimulation _simulation;
        private readonly List<Box3DBody> _running = new List<Box3DBody>();
        private List<Box3DBody> _candidates;
        private double _lastTime;
        private float _accumulator;
        private int _undoGroup;
        private GUIContent _icon;

        public override GUIContent toolbarIcon => _icon ??= new GUIContent(
            AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath),
            "Box3D Physics Simulation — settle the selected bodies with live physics. " +
            "Space starts/stops, left-drag grabs a body, Esc stops.");

        public override bool IsAvailable() => !Application.isPlaying;

        [MenuItem("Tools/Box3D/Physics Simulation", false, 0)]
        private static void ActivateFromMenu() => ToolManager.SetActiveTool<Box3DPhysicsSimulationTool>();

        [MenuItem("Tools/Box3D/Physics Simulation", true)]
        private static bool ValidateActivateFromMenu() => !Application.isPlaying;

        public override void OnActivated()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += DiscardSimulation;
            Undo.undoRedoPerformed += OnUndoRedo;
            Selection.selectionChanged += InvalidateCandidates;
            EditorApplication.hierarchyChanged += InvalidateCandidates;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            InvalidateCandidates();
        }

        public override void OnWillBeDeactivated()
        {
            StopSimulation();
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= DiscardSimulation;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Selection.selectionChanged -= InvalidateCandidates;
            EditorApplication.hierarchyChanged -= InvalidateCandidates;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView) || Application.isPlaying) return;

            HandleKeys(Event.current);
            if (_simulation != null)
            {
                HandleGrab(Event.current);
                DrawGrab();
            }
            DrawPanel();
        }

        private void HandleKeys(Event e)
        {
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.Space)
            {
                if (_simulation != null) StopSimulation();
                else StartSimulation();
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                if (_simulation != null)
                {
                    StopSimulation();
                    e.Use();
                }
            }
        }

        private void HandleGrab(Event e)
        {
            // While simulating the tool owns the left button: clicks grab instead of reselecting.
            int control = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(control);

            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0 && !e.alt && !PanelRect.Contains(e.mousePosition):
                    _simulation.TryGrab(HandleUtility.GUIPointToWorldRay(e.mousePosition));
                    GUIUtility.hotControl = control;
                    e.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == control:
                    _simulation.MoveGrab(HandleUtility.GUIPointToWorldRay(e.mousePosition));
                    e.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == control && e.button == 0:
                    _simulation.Release();
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }
        }

        private void DrawGrab()
        {
            if (Event.current.type != EventType.Repaint || !_simulation.IsGrabbing) return;

            Vector3 from = _simulation.GrabPoint;
            Vector3 to = _simulation.GrabTarget;
            Handles.color = AccentColor;
            Handles.DrawDottedLine(from, to, 4f);
            Handles.SphereHandleCap(0, from, Quaternion.identity,
                HandleUtility.GetHandleSize(from) * 0.1f, EventType.Repaint);
        }

        private void DrawPanel()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(PanelRect);
            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Label("Physics Simulation", EditorStyles.boldLabel);

            if (_simulation != null)
            {
                GUILayout.Label($"Simulating {_simulation.BodyCount} " +
                    (_simulation.BodyCount == 1 ? "body" : "bodies") + " — drag to move them.",
                    EditorStyles.miniLabel);
                if (GUILayout.Button("■ Stop Simulation (Space)")) StopSimulation();
            }
            else
            {
                int count = Candidates().Count;
                GUILayout.Label(count == 0
                    ? "Select objects with a dynamic Box3D body."
                    : $"{count} dynamic " + (count == 1 ? "body" : "bodies") + " in selection.",
                    EditorStyles.miniLabel);
                using (new EditorGUI.DisabledScope(count == 0))
                {
                    if (GUILayout.Button("▶ Start Simulation (Space)")) StartSimulation();
                }
            }

            GUILayout.Label("Unselected shapes stay frozen. Esc stops.", EditorStyles.miniLabel);
            GUILayout.EndVertical();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void StartSimulation()
        {
            InvalidateCandidates(); // Inspector edits (e.g. body type) don't fire the cached events
            List<Box3DBody> bodies = Candidates();
            if (bodies.Count == 0 || _simulation != null) return;

            try
            {
                _simulation = new Box3DEditorSimulation(bodies);
            }
            catch (Exception e)
            {
                // No native library on this editor platform — the tool just can't run here.
                Debug.LogWarning($"[Box3D] Editor physics simulation unavailable: {e.Message}");
                return;
            }

            if (_simulation.BodyCount == 0)
            {
                Debug.LogWarning("[Box3D] Physics simulation: the selected bodies have no shapes to simulate.");
                _simulation.Dispose();
                _simulation = null;
                return;
            }

            _running.Clear();
            _running.AddRange(bodies);

            // One undo step per run: snapshot every transform now, mutate freely while stepping.
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Box3D Physics Simulation");
            _undoGroup = Undo.GetCurrentGroup();
            var transforms = new Transform[bodies.Count];
            for (int i = 0; i < bodies.Count; i++) transforms[i] = bodies[i].transform;
            Undo.RegisterCompleteObjectUndo(transforms, "Box3D Physics Simulation");

            _lastTime = EditorApplication.timeSinceStartup;
            _accumulator = 0f;
            SceneView.RepaintAll();
        }

        private void StopSimulation()
        {
            if (_simulation == null) return;
            _simulation.Dispose();
            _simulation = null;
            GUIUtility.hotControl = 0; // stopping mid-drag must not leave the scene view captured

            // The settled poses were written straight to the Transforms — record them on prefab
            // instances so the overrides serialize, and fold the whole run into one undo step.
            foreach (Box3DBody body in _running)
            {
                if (body && PrefabUtility.IsPartOfPrefabInstance(body.transform))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(body.transform);
                }
            }
            _running.Clear();
            Undo.CollapseUndoOperations(_undoGroup);
            SceneView.RepaintAll();
        }

        // Undo while simulating just restored the pre-run poses — drop the session without
        // re-writing simulated state on top of what the user asked back.
        private void DiscardSimulation()
        {
            if (_simulation == null) return;
            _simulation.Dispose();
            _simulation = null;
            _running.Clear();
            GUIUtility.hotControl = 0;
            SceneView.RepaintAll();
        }

        private void OnEditorUpdate()
        {
            if (_simulation == null) return;

            double now = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Min((float)(now - _lastTime), 0.1f);
            _lastTime = now;
            _accumulator += deltaTime;

            int steps = 0;
            while (_accumulator >= StepTime && steps < MaxCatchUpSteps)
            {
                _simulation.Step(StepTime);
                _accumulator -= StepTime;
                steps++;
            }
            if (steps == MaxCatchUpSteps) _accumulator = 0f; // editor hiccup — drop time, don't chase it

            if (steps > 0)
            {
                _simulation.WriteBack();
                SceneView.RepaintAll();
            }
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) StopSimulation();
        }

        private void OnUndoRedo() => DiscardSimulation();

        private void OnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removing) => DiscardSimulation();

        private void InvalidateCandidates() => _candidates = null;

        // Dynamic, enabled bodies for the current selection: everything under the selected roots,
        // plus the owning body when a child of a prop is selected (clicking a visual mesh still
        // means "this prop"). Deduplicated, ordered parents before children (write-back order for
        // nested bodies).
        private List<Box3DBody> Candidates()
        {
            if (_candidates != null) return _candidates;

            var seen = new HashSet<Box3DBody>();
            var result = new List<Box3DBody>();
            void Add(Box3DBody body)
            {
                if (!body || !body.isActiveAndEnabled || body.BodyType != Box3DBodyType.Dynamic) return;
                if (seen.Add(body)) result.Add(body);
            }

            foreach (GameObject root in Selection.gameObjects)
            {
                Add(root.GetComponentInParent<Box3DBody>());
                foreach (Box3DBody body in root.GetComponentsInChildren<Box3DBody>())
                {
                    Add(body);
                }
            }
            result.Sort((a, b) => Depth(a.transform).CompareTo(Depth(b.transform)));
            return _candidates = result;
        }

        private static int Depth(Transform transform)
        {
            int depth = 0;
            while (transform.parent != null)
            {
                depth++;
                transform = transform.parent;
            }
            return depth;
        }
    }
}
