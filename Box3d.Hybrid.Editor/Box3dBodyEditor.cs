using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Box3d.Hybrid.Editor
{
    /// <summary>Body inspector: the normal fields, a live read-out (awake, mass, speed) while playing,
    /// and a Contact Inspector — the body's live contacts (other body, points, separation, impulse)
    /// listed in the Inspector and drawn in the Scene view (points + normals, colored by impulse).</summary>
    [CustomEditor(typeof(Box3dBody))]
    public class Box3dBodyEditor : UnityEditor.Editor
    {
        // Impulse (N·s) that maps to a full-red / long-normal contact; contacts above this saturate.
        private const float ImpulseScale = 3f;

        private static bool _showContacts = true;
        private static bool _drawInScene = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            var component = (Box3dBody)target;
            Body body = component.Body;
            if (!body.IsValid) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Awake", body.IsAwake);
                EditorGUILayout.FloatField("Mass", body.GetMass());
                EditorGUILayout.FloatField("Speed", math.length(body.LinearVelocity));
            }

            ContactData[] contacts = body.GetContacts();
            EditorGUILayout.Space();
            _showContacts = EditorGUILayout.Foldout(_showContacts, $"Contacts ({contacts.Length})", true);
            if (!_showContacts) return;

            EditorGUI.indentLevel++;
            _drawInScene = EditorGUILayout.Toggle("Draw in Scene", _drawInScene);

            if (contacts.Length == 0)
            {
                EditorGUILayout.LabelField("(not touching anything)");
            }
            else
            {
                Box3dBody[] all = FindObjectsByType<Box3dBody>(FindObjectsSortMode.None);
                int shown = 0;
                foreach (ContactData c in contacts)
                {
                    if (shown++ >= 16) { EditorGUILayout.LabelField($"… and {contacts.Length - 16} more"); break; }
                    Summarize(c, out int points, out float peakImpulse, out float minSeparation);
                    string other = OtherBodyName(c, body, all);
                    EditorGUILayout.LabelField($"vs {other}",
                        $"{points} pt   impulse {peakImpulse:F2}   sep {minSeparation:F3}");
                }
            }
            EditorGUI.indentLevel--;
        }

        private void OnSceneGUI()
        {
            if (!Application.isPlaying || !_showContacts || !_drawInScene) return;

            var component = (Box3dBody)target;
            Body body = component.Body;
            if (!body.IsValid) return;

            foreach (ContactData c in body.GetContacts())
            {
                // Anchor A is relative to shape A's body's center of mass (box3d picks the A/B order).
                var bodyA = new Body { Id = c.ShapeA.GetBody() };
                if (!bodyA.IsValid) continue;
                float3 comA = bodyA.GetWorldCenterOfMass();

                foreach (Manifold m in c.Manifolds)
                {
                    var normal = (Vector3)m.Normal;
                    for (int i = 0; i < m.PointCount; i++)
                    {
                        ManifoldPoint p = m[i];
                        var world = (Vector3)(comA + p.AnchorA);
                        float t = Mathf.Clamp01(p.TotalNormalImpulse / ImpulseScale);
                        Handles.color = Color.Lerp(Color.yellow, Color.red, t);

                        float size = HandleUtility.GetHandleSize(world);
                        Handles.SphereHandleCap(0, world, Quaternion.identity, size * 0.07f, EventType.Repaint);
                        Handles.DrawLine(world, world + normal * (size * (0.4f + t)));
                    }
                }
            }
        }

        private static void Summarize(ContactData c, out int points, out float peakImpulse, out float minSeparation)
        {
            points = 0;
            peakImpulse = 0f;
            minSeparation = float.MaxValue;
            foreach (Manifold m in c.Manifolds)
            {
                for (int i = 0; i < m.PointCount; i++)
                {
                    ManifoldPoint p = m[i];
                    points++;
                    peakImpulse = Mathf.Max(peakImpulse, p.TotalNormalImpulse);
                    minSeparation = Mathf.Min(minSeparation, p.Separation);
                }
            }
            if (points == 0) minSeparation = 0f;
        }

        // Resolve the name of the OTHER body in the contact by matching ids against scene components
        // (safe — no userData/GCHandle dereference). Raw-API bodies won't be found → "static/other".
        private static string OtherBodyName(ContactData c, Body self, Box3dBody[] all)
        {
            Body a = new Body { Id = c.ShapeA.GetBody() };
            Body b = new Body { Id = c.ShapeB.GetBody() };
            Body other = a == self ? b : a;
            foreach (Box3dBody component in all)
            {
                if (component && component.Body == other) return component.name;
            }
            return "static/other";
        }

        // Keep the read-out and Scene drawing ticking while playing.
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
