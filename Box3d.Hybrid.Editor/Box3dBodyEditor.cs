using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Box3d.Hybrid.Editor
{
    /// <summary>Body inspector: the normal fields, plus a live read-out (awake, mass, speed) while
    /// playing so you can watch the simulation state without extra tooling.</summary>
    [CustomEditor(typeof(Box3dBody))]
    public class Box3dBodyEditor : UnityEditor.Editor
    {
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
        }

        // Keep the read-out ticking while playing.
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
