using UnityEditor;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Deformable inspector: the normal fields plus setup validation (mesh assigned,
    /// Read/Write enabled, a Box3DBody to receive impacts) and play-mode Test Dent / Reset
    /// buttons for tuning dents without staging collisions.</summary>
    [CustomEditor(typeof(Box3DDeformable))]
    [CanEditMultipleObjects]
    public class Box3DDeformableEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var deformable = (Box3DDeformable)target;
            var filter = deformable.GetComponent<MeshFilter>();
            Mesh mesh = filter ? filter.sharedMesh : null;
            if (!mesh)
            {
                EditorGUILayout.HelpBox("Assign a mesh to the MeshFilter — there is nothing to deform.",
                    MessageType.Error);
            }
            else if (!mesh.isReadable)
            {
                EditorGUILayout.HelpBox($"Mesh '{mesh.name}' is not readable. Enable Read/Write in its " +
                    "model import settings to deform it.", MessageType.Error);
            }
            if (!deformable.GetComponentInParent<Box3DBody>(true))
            {
                EditorGUILayout.HelpBox("Impacts arrive through a Box3DBody — add one (plus a Box3D shape) " +
                    "on this object or a parent. Static bodies work for walls and floors.", MessageType.Warning);
            }
            if (serializedObject.FindProperty("UpdateCollision").boolValue)
            {
                if (!deformable.GetComponent<Box3DHullShape>() && !deformable.GetComponent<Box3DMeshShape>())
                {
                    EditorGUILayout.HelpBox("Update Collision needs a Hull or Mesh shape on this GameObject — " +
                        "box, sphere and capsule collision can't take dents.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("Hulls stay convex, so dents register only where they flatten " +
                        "corners or edges; mesh shapes (static) take true craters. Rebuilding collision makes " +
                        "recorded replays diverge.", MessageType.Info);
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button(Application.isPlaying ? "Test Dent" : "Test Dent (enter Play mode)"))
                {
                    foreach (Object t in targets)
                    {
                        ((Box3DDeformable)t).TestDent();
                    }
                }
                if (GUILayout.Button("Reset Deformation"))
                {
                    foreach (Object t in targets)
                    {
                        ((Box3DDeformable)t).ResetDeformation();
                    }
                }
            }
        }
    }
}
