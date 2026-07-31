using UnityEditor;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Draws the scrub timeline shared by the replay components (frame slider, transport,
    /// divergence read-out). See <see cref="IReplayTimeline"/>.</summary>
    internal static class ReplayTimelineGUI
    {
        // Rebuilt only when the frame counter actually moves — Draw runs twice per repaint, and
        // repaints are continuous during playback.
        private static string _frameLabel;
        private static int _labelFrame = -1;
        private static int _labelLast = -1;

        public static void Draw(UnityEditor.Editor editor, IReplayTimeline replay)
        {
            if (!Application.isPlaying || !replay.IsCreated)
            {
                EditorGUILayout.HelpBox("Enter Play mode with a recording loaded to scrub the timeline.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);

            int last = Mathf.Max(0, replay.FrameCount - 1);
            int frame = EditorGUILayout.IntSlider("Frame", replay.Frame, 0, last);
            if (frame != replay.Frame) replay.SeekFrame(frame);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀")) replay.SeekFrame(Mathf.Max(0, replay.Frame - 1));
                if (GUILayout.Button(replay.IsPlaying ? "❚❚ Pause" : "▶ Play")) replay.TogglePlay();
                if (GUILayout.Button("▶|")) replay.StepFrame();
                if (GUILayout.Button("Restart")) replay.Restart();
            }

            if (_labelFrame != replay.Frame || _labelLast != last)
            {
                _labelFrame = replay.Frame;
                _labelLast = last;
                _frameLabel = $"{replay.Frame} / {last}";
            }
            EditorGUILayout.LabelField("Frame", _frameLabel);

            if (replay.HasDiverged)
                EditorGUILayout.HelpBox($"Replay DIVERGED at frame {replay.DivergeFrame} — the sim is non-deterministic.", MessageType.Error);
            else
                EditorGUILayout.HelpBox("Deterministic so far.", MessageType.None);

            // Repaint only while frames advance on their own; when paused the inspector already
            // repaints on interaction, and an unconditional Repaint here is a busy loop.
            if (replay.IsPlaying) editor.Repaint();
        }
    }
}
