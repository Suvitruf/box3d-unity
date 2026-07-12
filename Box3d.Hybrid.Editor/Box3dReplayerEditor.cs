using UnityEditor;
using UnityEngine;

namespace Box3d.Hybrid.Editor
{
    /// <summary>Adds a scrub timeline to <see cref="Box3dReplayer"/>: a frame slider, transport buttons,
    /// and a live divergence read-out while playing.</summary>
    [CustomEditor(typeof(Box3dReplayer))]
    public class Box3dReplayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var replayer = (Box3dReplayer)target;
            if (!Application.isPlaying || !replayer.IsCreated)
            {
                EditorGUILayout.HelpBox("Enter Play mode with a recording loaded to scrub the timeline.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);

            int last = Mathf.Max(0, replayer.FrameCount - 1);
            int frame = EditorGUILayout.IntSlider("Frame", replayer.Frame, 0, last);
            if (frame != replayer.Frame) replayer.SeekFrame(frame);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀")) replayer.SeekFrame(Mathf.Max(0, replayer.Frame - 1));
                if (GUILayout.Button(replayer.IsPlaying ? "❚❚ Pause" : "▶ Play")) replayer.TogglePlay();
                if (GUILayout.Button("▶|")) replayer.StepFrame();
                if (GUILayout.Button("Restart")) replayer.Restart();
            }

            EditorGUILayout.LabelField("Frame", $"{replayer.Frame} / {last}");

            if (replayer.HasDiverged)
                EditorGUILayout.HelpBox($"Replay DIVERGED at frame {replayer.DivergeFrame} — the sim is non-deterministic.", MessageType.Error);
            else
                EditorGUILayout.HelpBox("Deterministic so far.", MessageType.None);

            Repaint(); // keep the read-out live during playback
        }
    }
}
