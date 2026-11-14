using UnityEditor;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Adds a scrub timeline to <see cref="Box3DReplayer"/>.</summary>
    [CustomEditor(typeof(Box3DReplayer))]
    [CanEditMultipleObjects]
    public class Box3DReplayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("The timeline is available with a single replayer selected.", MessageType.None);
                return;
            }
            ReplayTimelineGUI.Draw(this, (Box3DReplayer)target);
        }
    }
}
