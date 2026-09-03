using System.IO;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>Records the active <see cref="Box3DWorld"/> and checks its determinism — the tool for
    /// anyone shipping lockstep / rollback netcode or authoritative server physics. Drop it in a scene
    /// with a Box3DWorld: it records from the start, and after <see cref="RecordSteps"/> steps stops and
    /// runs box3d's replay validation (does the sim reproduce bit-identical state?). It can also replay
    /// at a different worker count (cross-thread determinism) and save the capture to a file.
    ///
    /// <para>A "DIVERGED" result means something in the scene is non-deterministic (unseeded randomness,
    /// frame-rate-dependent forces, etc.) — box3d's own stepping is deterministic.</para></summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DRecorder.png")]
    [AddComponentMenu("Box3D/Replay/Recorder")]
    public class Box3DRecorder : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("Record this many steps, then stop and validate.")]
        private int RecordSteps = 300;

        [SerializeField, Min(1), Tooltip("Replay at this worker count. Differ it from the sim's to test cross-thread determinism.")]
        private int ValidateWorkerCount = 1;

        [SerializeField, Tooltip("Validate automatically when recording stops.")]
        private bool AutoValidateOnStop = true;

        [SerializeField, Tooltip("Also save the capture here on stop (empty = don't save).")]
        private string SavePath = "";

        private IBox3DWorld _world;
        private Recording _recording;
        private int _steps;
        private bool _isRecording;

        private void Start()
        {
            _world = IBox3DWorld.Get(this);
            if (!IBox3DWorld.Validate(_world)) return;
            if (_world.PhysicsWorld.GetCounters().BodyCount == 0)
            {
                Debug.LogWarning("[Box3DRecorder] the Box3DWorld has 0 bodies — the recording will be empty. " +
                    "This recorder captures the component Box3DWorld; scenes that build physics via the raw API " +
                    "in their own World (e.g. the sandbox demos) won't be seen. Use a scene built from Box3DBody " +
                    "components, or call World.StartRecording on your own world directly.", this);
            }

            _recording = Recording.Create();
            _world.PhysicsWorld.StartRecording(_recording);
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
            if (!_isRecording || !_world.IsValid) return;
            _world.PhysicsWorld.StopRecording();
            _isRecording = false;
            Debug.Log($"[Box3DRecorder] recorded {_steps} steps, {_recording.Size / 1024} KB.", this);

            if (AutoValidateOnStop) Validate();
            if (!string.IsNullOrEmpty(SavePath)) Save();
        }

        /// <summary>Replays the capture and reports whether the simulation is deterministic.</summary>
        [ContextMenu("Validate Determinism")]
        public void Validate()
        {
            if (!_recording.IsCreated) { Debug.LogWarning("[Box3DRecorder] nothing recorded yet.", this); return; }
            bool ok = _recording.ValidateReplay(ValidateWorkerCount);
            string verdict = ok ? "<color=#88ff88>DETERMINISTIC</color>" : "<color=#ff8888>DIVERGED</color>";
            Debug.Log($"[Box3DRecorder] determinism: {verdict}  (replayed at {ValidateWorkerCount} worker(s))", this);
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
            Debug.Log($"[Box3DRecorder] {(ok ? "saved" : "FAILED to save")} → {path}", this);
        }

        private void OnDestroy()
        {
            if (_isRecording && _world.IsValid) _world.PhysicsWorld.StopRecording();
            if (_recording.IsCreated) _recording.Destroy();
        }
    }
}
