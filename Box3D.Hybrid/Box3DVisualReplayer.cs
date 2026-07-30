using System;
using System.Collections.Generic;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>Plays a recording back on the scene's <em>real</em> GameObjects — meshes, materials and
    /// all — instead of wireframes. It pauses live physics and drives each recorded body onto the
    /// matching scene object, mapped by body name (set from the GameObject name when the body is
    /// created). Use it in the <em>same scene</em> the recording was made in; for a scene-less wireframe
    /// view use <see cref="Box3DReplayer"/> instead.
    ///
    /// <para>Names should be unique for exact mapping — same-named bodies are paired best-effort.</para></summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DVisualReplayer.png")]
    [AddComponentMenu("Box3D/Replay/Visual Replayer")]
    public class Box3DVisualReplayer : MonoBehaviour, IReplayTimeline
    {
        [SerializeField, Tooltip("A .rec file to load on Start (empty = load one at runtime via LoadBytes).")]
        private string LoadPath = "";

        [SerializeField, Min(1), Tooltip("Replay at this worker count.")]
        private int WorkerCount = 1;

        [SerializeField, Tooltip("Start playing as soon as it loads.")]
        private bool PlayOnStart = true;

        [SerializeField, Min(0.01f), Tooltip("Playback speed multiplier.")]
        private float Speed = 1f;

        // After an editor/frame hitch, catching up the full backlog in one Update could mean
        // hundreds of native steps — cap it and drop the rest.
        private const int MaxCatchUpSteps = 8;

        private ReplayPlayer _player;
        private Box3DWorld _world;
        private Transform[] _targets; // replay body index -> scene transform (null = unmapped / hole)
        private Body[] _replayBodies; // replay body handles, resolved once at mapping time
        private bool _isPlaying;
        private bool _pendingLoad;
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
            if (string.IsNullOrEmpty(LoadPath)) return;

            // Pause live physics right away so the scene doesn't simulate before the replay takes over,
            // then defer the load one frame: objects built in other components' Start() (like a spawned
            // car) don't exist yet here, but they will by the first Update.
            _world = Box3DWorld.Instance;
            if (_world) _world.Paused = true;
            _pendingLoad = true;
        }

        /// <summary>Loads and starts a replay from a saved <c>.rec</c> file.</summary>
        public void Load(string path)
        {
            Recording recording = Recording.LoadFromFile(path);
            if (!recording.IsCreated)
            {
                Debug.LogError($"[Box3DVisualReplayer] couldn't load '{path}'.", this);
                return;
            }
            LoadBytes(recording.GetData());
            recording.Destroy();
        }

        /// <summary>Loads and starts a replay from recording bytes (e.g. a live capture's
        /// <c>recording.GetData()</c>).</summary>
        public void LoadBytes(ReadOnlySpan<byte> data)
        {
            Unload();
            _player = ReplayPlayer.Create(data, WorkerCount);
            if (!_player.IsCreated)
            {
                Debug.LogError("[Box3DVisualReplayer] the recording data was not a valid replay. Recordings are precision-specific: a .rec written by a single-precision build cannot replay in a BOX3D_DOUBLE build (positions are serialized at native width) and vice versa — re-record with the current build if the precision changed.", this);
                return;
            }
            _timeStep = _player.GetInfo().TimeStep;
            BuildMapping();
            _accum = 0f;
            _isPlaying = PlayOnStart;
            ApplyFrame(); // show frame 0 right away, even before pressing play
        }

        private void BuildMapping()
        {
            // Pause live physics so it doesn't fight the replayed transforms.
            _world = Box3DWorld.Instance;
            if (_world) _world.Paused = true;

            // Group scene objects by their (recorded) body name — read from the live body so it matches
            // the recording's truncation exactly.
            var byName = new Dictionary<string, List<Transform>>();
            foreach (Box3DBody body in FindObjectsByType<Box3DBody>(FindObjectsSortMode.None))
            {
                if (!body.Body.IsValid) continue;
                string name = body.Body.GetName();
                if (!byName.TryGetValue(name, out List<Transform> list))
                {
                    list = new List<Transform>();
                    byName[name] = list;
                }
                list.Add(body.transform);
            }

            int count = _player.BodyCount;
            _targets = new Transform[count];
            _replayBodies = new Body[count];
            var cursor = new Dictionary<string, int>();
            int mapped = 0, duplicates = 0;
            for (int i = 0; i < count; i++)
            {
                Body replayBody = _replayBodies[i] = _player.GetBody(i);
                if (!replayBody.IsValid) continue; // hole left by a destroyed body
                string name = replayBody.GetName();
                if (!byName.TryGetValue(name, out List<Transform> list)) continue;

                int c = cursor.TryGetValue(name, out int v) ? v : 0;
                if (c < list.Count)
                {
                    _targets[i] = list[c];
                    mapped++;
                }
                cursor[name] = c + 1;
                if (list.Count > 1) duplicates++;
            }

            Debug.Log($"[Box3DVisualReplayer] mapped {mapped}/{count} replay bodies to scene objects by name.", this);
            if (duplicates > 0)
            {
                Debug.LogWarning($"[Box3DVisualReplayer] {duplicates} replay bodies share a name — those are paired " +
                                 "best-effort. Give bodies unique names for exact playback.", this);
            }
            if (mapped == 0)
            {
                var replayNames = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    Body rb = _player.GetBody(i);
                    replayNames.Add(rb.IsValid ? $"'{rb.GetName()}'" : "(hole)");
                }
                Debug.LogWarning($"[Box3DVisualReplayer] no matches. Recording body names: [{string.Join(", ", replayNames)}]. " +
                                 $"Scene Box3DBody names: [{string.Join(", ", byName.Keys)}]. If the recording names are all " +
                                 "empty, it predates body naming — re-record with the current build.", this);
            }
        }

        private void Update()
        {
            if (_pendingLoad)
            {
                _pendingLoad = false;
                Load(LoadPath); // deferred from Start so the scene's bodies exist for mapping
            }

            if (!_player.IsCreated || !_isPlaying || _timeStep <= 0f) return;

            _accum += Time.deltaTime * Speed;
            bool advanced = false;
            int steps = 0;
            while (_accum >= _timeStep && steps < MaxCatchUpSteps)
            {
                _accum -= _timeStep;
                if (!_player.StepFrame()) { _isPlaying = false; break; }
                advanced = true;
                steps++;
            }
            if (steps == MaxCatchUpSteps) _accum = 0f; // hitch — drop the backlog, don't chase it
            if (advanced) ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (_targets == null) return;
            for (int i = 0; i < _targets.Length; i++)
            {
                Transform target = _targets[i];
                if (!target) continue;
                // Cached handle + a single GetTransform: half the native calls of re-resolving
                // the body and reading Position/Rotation separately every frame.
                Body replayBody = _replayBodies[i];
                if (!replayBody.IsValid) continue;
                B3Transform tf = replayBody.GetTransform().ToB3Transform();
                target.SetPositionAndRotation((Vector3)tf.Position, tf.Rotation);
            }
        }

        public void SeekFrame(int frame)
        {
            if (!_player.IsCreated) return;
            _isPlaying = false;
            _player.SeekFrame(frame);
            ApplyFrame();
        }

        public void StepFrame()
        {
            if (!_player.IsCreated) return;
            _isPlaying = false;
            _player.StepFrame();
            ApplyFrame();
        }

        public bool TogglePlay() => _isPlaying = _player.IsCreated && !_isPlaying;

        public void Restart()
        {
            if (!_player.IsCreated) return;
            _player.Restart();
            _accum = 0f;
            ApplyFrame();
        }

        private void Unload()
        {
            if (_player.IsCreated) _player.Destroy();
            if (_world) _world.Paused = false;
            _isPlaying = false;
            _targets = null;
        }

        private void OnDestroy() => Unload();
    }
}
