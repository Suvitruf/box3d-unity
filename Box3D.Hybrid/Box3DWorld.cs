using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>Scene-level owner of a Box3D simulation world. Steps from FixedUpdate, pushes
    /// kinematic bodies before the step and syncs moved bodies back after. Auto-created on demand
    /// (like Unity's single physics world); place one explicitly to tune gravity, sub-steps, or
    /// worker count.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DWorld.png")]
    [AddComponentMenu("Box3D/World")]
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class Box3DWorld : MonoBehaviour
    {
        [SerializeField, Tooltip("Gravity vector applied to every dynamic body.")]
        private Vector3 Gravity = new Vector3(0f, -9.81f, 0f);

        [SerializeField, Min(1), Tooltip("Solver sub-steps per step. Higher = stiffer, slower.")]
        private int SubStepCount = 4;

        [SerializeField, Min(0), Tooltip("Box3D worker threads. 0 = auto (about half the logical cores).")]
        private int WorkerCount = 0;

        [SerializeField, Tooltip("Overlay physics debug geometry in the Scene view each frame (None = off). " +
            "Enable Contacts / Islands / Forces / Bounds to see the solver's view of the world. " +
            "For the Game view, turn on its Gizmos toggle.")]
        private DebugDrawFlags DebugDraw = DebugDrawFlags.None;

        [SerializeField, Min(1f), Tooltip("Half-size of the box around the origin that debug drawing covers.")]
        private float DebugDrawRadius = 200f;

        // Only kinematic bodies need per-frame attention (they follow their Transform). Dynamic
        // bodies sync back through move events — which report only bodies that actually moved — so
        // they never appear here. Bodies map back to their component through a GCHandle in userData,
        // so no all-bodies list is kept.
        private readonly List<Box3DBody> _kinematicBodies = new List<Box3DBody>();

        private World _world;
        private Body _anchor;
        private static Box3DWorld _instance;

        /// <summary>The underlying Box3D world (valid after this component is enabled).</summary>
        public World World => _world;

        /// <summary>When true, the world stops stepping (bodies stay put). The visual replayer sets this
        /// so live physics doesn't fight the replayed transforms.</summary>
        public bool Paused { get; set; }

        /// <summary>The configured gravity vector (readable without a live world — the rope's
        /// editor preview settles under the same gravity the simulation will use).</summary>
        public Vector3 GravityVector => Gravity;

        /// <summary>A shared static body at the origin, used as the fixed endpoint for joints whose
        /// connected body is null (like Unity's null connectedBody = attach to the world).</summary>
        public Body WorldAnchor
        {
            get
            {
                if (!_anchor.IsValid)
                {
                    EnsureCreated();
                    _anchor = _world.CreateBody(BodyDef.Default); // static at origin
                }
                return _anchor;
            }
        }

        /// <summary>The active world component, creating one if the scene has none.</summary>
        public static Box3DWorld Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindAnyObjectByType<Box3DWorld>();
                    if (!_instance)
                    {
                        _instance = new GameObject("Box3D World").AddComponent<Box3DWorld>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance && _instance != this)
            {
                Debug.LogWarning("[Box3D] Multiple Box3DWorld components — only the first is used.", this);
            }
            _instance = this;
            EnsureCreated();
        }

        private void EnsureCreated()
        {
            if (_world.IsValid) return;

            WorldDef def = WorldDef.Default;
            def.Gravity = Gravity;
            def.WorkerCount = (uint)(WorkerCount > 0 ? WorkerCount : Mathf.Max(1, SystemInfo.processorCount / 2));
            _world = World.Create(def);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Push Inspector edits to the live world during play. SubStepCount and DebugDraw are
            // read every frame anyway; WorkerCount is baked at world creation.
            if (!Application.isPlaying || !_world.IsValid) return;
            _world.SetGravity(Gravity);
        }
#endif

        internal void AddKinematic(Box3DBody body)
        {
            if (!_kinematicBodies.Contains(body)) _kinematicBodies.Add(body);
        }

        internal void RemoveKinematic(Box3DBody body)
        {
            _kinematicBodies.Remove(body);
        }

        private void FixedUpdate()
        {
            if (Paused || !_world.IsValid) return;

            float deltaTime = Time.fixedDeltaTime;
            for (int i = 0; i < _kinematicBodies.Count; i++)
            {
                Box3DBody body = _kinematicBodies[i];
                if (body) body.PushKinematic(deltaTime);
            }

            _world.Step(deltaTime, SubStepCount);

            foreach (BodyMoveEvent moveEvent in _world.GetBodyMoveEvents())
            {
                if (moveEvent.UserData == IntPtr.Zero) continue;
                if (GCHandle.FromIntPtr(moveEvent.UserData).Target is Box3DBody body)
                {
                    body.ApplyMoveEvent(moveEvent.Transform);
                }
            }

            // Impacts, delivered after the move sync so receivers see settled transforms. The
            // native normal points from shape A to shape B: it is the impact direction into B,
            // its negation the impact direction into A.
            foreach (ContactHitEvent hitEvent in _world.GetContactEvents().Hit)
            {
                Box3DBody bodyA = BodyFromShape(hitEvent.ShapeIdA);
                Box3DBody bodyB = BodyFromShape(hitEvent.ShapeIdB);
                if (bodyA && bodyA.WantsHits)
                {
                    bodyA.DispatchHit(new Box3DHit { Point = (Vector3)hitEvent.Point, Direction = -hitEvent.Normal,
                        ApproachSpeed = hitEvent.ApproachSpeed, OtherBody = bodyB });
                }
                if (bodyB && bodyB.WantsHits)
                {
                    bodyB.DispatchHit(new Box3DHit { Point = (Vector3)hitEvent.Point, Direction = hitEvent.Normal,
                        ApproachSpeed = hitEvent.ApproachSpeed, OtherBody = bodyA });
                }
            }
        }

        // Maps a hit event's shape back to its component through the body GCHandle in shape userData
        // (zero for shapes that created their own static body, or preview shapes).
        private static Box3DBody BodyFromShape(ShapeId shapeId)
        {
            var shape = new Shape { Id = shapeId };
            if (!shape.IsValid) return null;
            IntPtr userData = shape.UserData;
            return userData == IntPtr.Zero ? null : GCHandle.FromIntPtr(userData).Target as Box3DBody;
        }

        private void LateUpdate()
        {
            // Debug overlay: box3d emits its geometry through the debug-draw bridge as Scene-view lines.
            // Drawn after the step + transform sync so it reflects the current pose.
            if (DebugDraw != DebugDrawFlags.None && _world.IsValid)
            {
                _world.DrawDebug(DebugDraw, DebugDrawRadius);
            }
        }

        // Matches the world component's purple icon so the arrow reads as "the world's gravity".
        private static readonly Color GravityGizmoColor = new Color(0.75f, 0.53f, 0.92f, 0.95f);

        private void OnDrawGizmosSelected()
        {
            float magnitude = Gravity.magnitude;
            if (magnitude < 1e-4f) return; // zero gravity — nothing to point at

            Vector3 origin = transform.position;
            Vector3 dir = Gravity / magnitude;
            // Shaft length tracks strength (1 g ≈ 1.5 m), clamped so extreme values stay readable.
            float length = Mathf.Clamp(1.5f * magnitude / 9.81f, 0.4f, 4f);
            Vector3 tip = origin + dir * length;

            // A basis perpendicular to the arrow for the head fins (gravity is usually straight
            // down, where Vector3.up is degenerate — fall back to right).
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(dir, Vector3.right);
            side.Normalize();
            Vector3 side2 = Vector3.Cross(dir, side);

            Gizmos.color = GravityGizmoColor;
            Gizmos.DrawLine(origin, tip);
            float head = Mathf.Min(0.3f * length, 0.4f);
            foreach (Vector3 fin in new[] { side, -side, side2, -side2 })
            {
                Gizmos.DrawLine(tip, tip - dir * head + fin * (head * 0.5f));
            }
        }

        private void OnDestroy()
        {
            if (_world.IsValid) _world.Destroy();
            if (_instance == this) _instance = null;
        }
    }
}
