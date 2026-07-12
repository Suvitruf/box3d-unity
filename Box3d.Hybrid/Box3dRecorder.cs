using System.IO;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>Records the active <see cref="Box3dWorld"/> and checks its determinism — the tool for
    /// anyone shipping lockstep / rollback netcode or authoritative server physics. Drop it in a scene
    /// with a Box3dWorld: it records from the start, and after <see cref="RecordSteps"/> steps stops and
    /// runs box3d's replay validation (does the sim reproduce bit-identical state?). It can also replay
    /// at a different worker count (cross-thread determinism) and save the capture to a file.
    ///
    /// <para>A "DIVERGED" result means something in the scene is non-deterministic (unseeded randomness,
    /// frame-rate-dependent forces, etc.) — box3d's own stepping is deterministic.</para></summary>
    [AddComponentMenu("Box3d/Box3d Recorder")]
    public class Box3dRecorder : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("Record this many steps, then stop and validate.")]
        private int RecordSteps = 300;

        [SerializeField, Min(1), Tooltip("Replay at this worker count. Differ it from the sim's to test cross-thread determinism.")]
        private int ValidateWorkerCount = 1;

        [SerializeField, Tooltip("Validate automatically when recording stops.")]
        private bool AutoValidateOnStop = true;

        [SerializeField, Tooltip("Also save the capture here on stop (empty = don't save).")]
        private string SavePath = "";

        private Box3dWorld _world;
        private Recording _recording;
        private int _steps;
        private bool _isRecording;

        private void Start()
        {
            _world = Box3dWorld.Instance;
            if (!_world || !_world.World.IsValid) return;

            if (_world.World.GetCounters().BodyCount == 0)
            {
                Debug.LogWarning("[Box3dRecorder] the Box3dWorld has 0 bodies — the recording will be empty. " +
                    "This recorder captures the component Box3dWorld; scenes that build physics via the raw API " +
                    "in their own World (e.g. the sandbox demos) won't be seen. Use a scene built from Box3dBody " +
                    "components, or call World.StartRecording on your own world directly.", this);
            }

            _recording = Recording.Create();
            _world.World.StartRecording(_recording);
            _isRecording = true;
            _steps = 0;
        }

        private void FixedUpdate()
        {
            if (!_isRecording) return;
            _steps++;
            if (_steps >= RecordSteps) Stop();
        }

        /// <summary>Stops recording (and validates / saves per the settings).</summary>
        [ContextMenu("Stop & Validate")]
        public void Stop()
        {
            if (!_isRecording || !_world || !_world.World.IsValid) return;
            _world.World.StopRecording();
            _isRecording = false;
            Debug.Log($"[Box3dRecorder] recorded {_steps} steps, {_recording.Size / 1024} KB.", this);

            if (AutoValidateOnStop) Validate();
            if (!string.IsNullOrEmpty(SavePath)) Save();
        }

        /// <summary>Replays the capture and reports whether the simulation is deterministic.</summary>
        [ContextMenu("Validate Determinism")]
        public void Validate()
        {
            if (!_recording.IsCreated) { Debug.LogWarning("[Box3dRecorder] nothing recorded yet.", this); return; }
            bool ok = _recording.ValidateReplay(ValidateWorkerCount);
            string verdict = ok ? "<color=#88ff88>DETERMINISTIC</color>" : "<color=#ff8888>DIVERGED</color>";
            Debug.Log($"[Box3dRecorder] determinism: {verdict}  (replayed at {ValidateWorkerCount} worker(s))", this);
        }

        /// <summary>Writes the capture to <see cref="SavePath"/> (or persistentDataPath).</summary>
        [ContextMenu("Save Recording")]
        public void Save()
        {
            if (!_recording.IsCreated) return;
            string path = string.IsNullOrEmpty(SavePath)
                ? Path.Combine(Application.persistentDataPath, "box3d.rec")
                : SavePath;
            bool ok = _recording.SaveToFile(path);
            Debug.Log($"[Box3dRecorder] {(ok ? "saved" : "FAILED to save")} → {path}", this);
        }

        private void OnDestroy()
        {
            if (_isRecording && _world && _world.World.IsValid) _world.World.StopRecording();
            if (_recording.IsCreated) _recording.Destroy();
        }
    }
}
