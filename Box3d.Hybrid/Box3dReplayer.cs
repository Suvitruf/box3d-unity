using System;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>Plays back a recorded simulation (from a <see cref="Box3dRecorder"/> capture or a saved
    /// <c>.rec</c> file) and draws it in the Scene/Game view — the viewer half of the record/replay tools.
    /// Enter Play mode to scrub the timeline (see the component's inspector), and it flags the frame where
    /// a replay diverges from the original (non-determinism). Runs its own replay world; it does not need
    /// a <see cref="Box3dWorld"/> in the scene.</summary>
    [AddComponentMenu("Box3d/Box3d Replayer")]
    public class Box3dReplayer : MonoBehaviour
    {
        [SerializeField, Tooltip("A .rec file to load on Start (empty = load one at runtime via LoadBytes).")]
        private string LoadPath = "";

        [SerializeField, Min(1), Tooltip("Replay at this worker count. Differ it from the recording's to test cross-thread determinism.")]
        private int WorkerCount = 1;

        [SerializeField, Tooltip("What to overlay for the replayed world.")]
        private DebugDrawFlags DebugDraw = DebugDrawFlags.Shapes | DebugDrawFlags.Contacts;

        [SerializeField, Min(1f), Tooltip("Half-extent of the debug-draw region around the origin.")]
        private float DrawRadius = 200f;

        [SerializeField, Tooltip("Start playing as soon as it loads.")]
        private bool PlayOnStart = true;

        [SerializeField, Min(0.01f), Tooltip("Playback speed multiplier.")]
        private float Speed = 1f;

        private ReplayPlayer _player;
        private bool _isPlaying;
        private float _timeStep;
        private float _accum;

        public bool IsCreated => _player.IsCreated;
        public bool IsPlaying => _isPlaying;
        public int Frame => _player.IsCreated ? _player.Frame : 0;
        public int FrameCount => _player.IsCreated ? _player.FrameCount : 0;
        public bool HasDiverged => _player.IsCreated && _player.HasDiverged;
        public int DivergeFrame => _player.IsCreated ? _player.DivergeFrame : -1;

        private void Start()
        {
            if (!string.IsNullOrEmpty(LoadPath)) Load(LoadPath);
        }

        /// <summary>Loads and starts a replay from a saved <c>.rec</c> file.</summary>
        public void Load(string path)
        {
            Recording recording = Recording.LoadFromFile(path);
            if (!recording.IsCreated)
            {
                Debug.LogError($"[Box3dReplayer] couldn't load '{path}'.", this);
                return;
            }
            LoadBytes(recording.GetData());
            recording.Destroy(); // the player copied the bytes
        }

        /// <summary>Loads and starts a replay from recording bytes (e.g. a live capture's
        /// <c>recording.GetData()</c>), no file needed.</summary>
        public void LoadBytes(ReadOnlySpan<byte> data)
        {
            Unload();
            _player = ReplayPlayer.Create(data, WorkerCount);
            if (!_player.IsCreated)
            {
                Debug.LogError("[Box3dReplayer] the recording data was not a valid replay.", this);
                return;
            }
            _player.EnableShapeDrawing();
            ReplayInfo info = _player.GetInfo();
            _timeStep = info.TimeStep;
            _accum = 0f;
            _isPlaying = PlayOnStart;
            Debug.Log($"[Box3dReplayer] loaded: {info.FrameCount} frames, {_player.BodyCount} bodies, " +
                      $"timestep {info.TimeStep * 1000f:F1} ms. Wireframes draw in the Scene view (Game view needs Gizmos on).", this);
        }

        private void Update()
        {
            if (!_player.IsCreated) return;

            if (_isPlaying && _timeStep > 0f)
            {
                _accum += Time.deltaTime * Speed;
                while (_accum >= _timeStep)
                {
                    _accum -= _timeStep;
                    if (!_player.StepFrame()) { _isPlaying = false; break; }
                }
            }

            _player.World.DrawDebug(DebugDraw, DrawRadius);
        }

        [ContextMenu("Play")] public void Play() => _isPlaying = _player.IsCreated;
        [ContextMenu("Pause")] public void Pause() => _isPlaying = false;
        public bool TogglePlay() => _isPlaying = _player.IsCreated && !_isPlaying;

        [ContextMenu("Restart")]
        public void Restart()
        {
            if (!_player.IsCreated) return;
            _player.Restart();
            _accum = 0f;
        }

        /// <summary>Advances one frame (and pauses).</summary>
        public void StepFrame()
        {
            if (!_player.IsCreated) return;
            _isPlaying = false;
            _player.StepFrame();
        }

        /// <summary>Jumps to a frame (and pauses).</summary>
        public void SeekFrame(int frame)
        {
            if (!_player.IsCreated) return;
            _isPlaying = false;
            _player.SeekFrame(frame);
        }

        private void Unload()
        {
            if (_player.IsCreated) _player.Destroy();
            _isPlaying = false;
        }

        private void OnDestroy() => Unload();
    }
}
