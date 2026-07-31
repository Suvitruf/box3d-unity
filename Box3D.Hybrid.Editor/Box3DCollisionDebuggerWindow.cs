using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    /// <summary>"Why isn't this colliding?" — assign two <see cref="Box3DBody"/> objects and, in Play
    /// mode, get a rule-by-rule verdict on whether they collide (body types, enabled, joints, collision
    /// filters), plus sensor and broadphase-proximity notes. Answers the top filtering support question.</summary>
    public class Box3DCollisionDebuggerWindow : EditorWindow
    {
        private Box3DBody _bodyA;
        private Box3DBody _bodyB;

        // Cached diagnosis, refreshed on a throttle: Diagnose P/Invokes O(shapesA × shapesB) and
        // OnGUI runs twice per repaint — running it per pass with a self-Repaint loop burned CPU
        // continuously. 5 Hz keeps the AABB-proximity note live as objects move.
        private const double RefreshInterval = 0.2;
        private List<DiagnosisLine> _lines;
        private bool _canCollide;
        private string _summary;
        private double _nextRefresh;
        private Box3DBody _cachedA;
        private Box3DBody _cachedB;

        [MenuItem("Window/Box3D/Collision Debugger")]
        private static void Open()
        {
            GetWindow<Box3DCollisionDebuggerWindow>("Collision Debugger");
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying || EditorApplication.timeSinceStartup < _nextRefresh) return;
            _nextRefresh = EditorApplication.timeSinceStartup + RefreshInterval;
            _lines = null; // next OnGUI re-diagnoses
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Assign two bodies and enter Play mode to see whether — and why — they collide.", MessageType.None);
            _bodyA = (Box3DBody)EditorGUILayout.ObjectField("Body A", _bodyA, typeof(Box3DBody), true);
            _bodyB = (Box3DBody)EditorGUILayout.ObjectField("Body B", _bodyB, typeof(Box3DBody), true);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to run the check.", MessageType.Info);
                return;
            }
            if (!_bodyA || !_bodyB) return;
            if (!_bodyA.Body.IsValid || !_bodyB.Body.IsValid)
            {
                EditorGUILayout.HelpBox("Bodies aren't created yet.", MessageType.Info);
                return;
            }

            if (_lines == null || _cachedA != _bodyA || _cachedB != _bodyB)
            {
                _cachedA = _bodyA;
                _cachedB = _bodyB;
                _lines = CollisionDiagnostics.Diagnose(_bodyA.Body, _bodyB.Body, out _canCollide, out _summary);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(_summary, _canCollide ? MessageType.Info : MessageType.Warning);
            EditorGUILayout.Space();

            foreach (DiagnosisLine line in _lines)
            {
                string icon = line.Status switch
                {
                    DiagnosisStatus.Pass => "✔", // ✔
                    DiagnosisStatus.Fail => "✘", // ✘
                    _ => "•",                     // •
                };
                EditorGUILayout.LabelField($"{icon}  {line.Label}", EditorStyles.boldLabel);
                if (!string.IsNullOrEmpty(line.Detail))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField(line.Detail, EditorStyles.wordWrappedMiniLabel);
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
