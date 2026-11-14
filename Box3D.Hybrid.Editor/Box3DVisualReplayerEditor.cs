using UnityEditor;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Adds a scrub timeline to <see cref="Box3DVisualReplayer"/>.</summary>
    [CustomEditor(typeof(Box3DVisualReplayer))]
    [CanEditMultipleObjects]
    public class Box3DVisualReplayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("The timeline is available with a single replayer selected.", MessageType.None);
                return;
            }
            ReplayTimelineGUI.Draw(this, (Box3DVisualReplayer)target);
        }
    }
}
