using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>Scene-level owner of a Box3d simulation world. Steps from FixedUpdate, pushes
    /// kinematic bodies before the step and syncs moved bodies back after. Auto-created on demand
    /// (like Unity's single physics world); place one explicitly to tune gravity, sub-steps, or
    /// worker count.</summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class Box3dWorld : MonoBehaviour
    {
        [SerializeField, Tooltip("Gravity vector applied to every dynamic body.")]
        private Vector3 Gravity = new Vector3(0f, -9.81f, 0f);

        [SerializeField, Min(1), Tooltip("Solver sub-steps per step. Higher = stiffer, slower.")]
        private int SubStepCount = 4;

        [SerializeField, Min(0), Tooltip("Box3d worker threads. 0 = auto (about half the logical cores).")]
        private int WorkerCount = 0;

        // Only kinematic bodies need per-frame attention (they follow their Transform). Dynamic
        // bodies sync back through move events — which report only bodies that actually moved — so
        // they never appear here. Bodies map back to their component through a GCHandle in userData,
        // so no all-bodies list is kept.
        private readonly List<Box3dBody> _kinematicBodies = new List<Box3dBody>();

        private World _world;
        private Body _anchor;
        private static Box3dWorld _instance;

        /// <summary>The underlying Box3d world (valid after this component is enabled).</summary>
        public World World => _world;

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
        public static Box3dWorld Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindObjectOfType<Box3dWorld>();
                    if (!_instance)
                    {
                        _instance = new GameObject("Box3d World").AddComponent<Box3dWorld>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance && _instance != this)
            {
                Debug.LogWarning("[Box3d] Multiple Box3dWorld components — only the first is used.", this);
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

        internal void AddKinematic(Box3dBody body)
        {
            if (!_kinematicBodies.Contains(body)) _kinematicBodies.Add(body);
        }

        internal void RemoveKinematic(Box3dBody body)
        {
            _kinematicBodies.Remove(body);
        }

        private void FixedUpdate()
        {
            if (!_world.IsValid) return;

            float deltaTime = Time.fixedDeltaTime;
            for (int i = 0; i < _kinematicBodies.Count; i++)
            {
                Box3dBody body = _kinematicBodies[i];
                if (body) body.PushKinematic(deltaTime);
            }

            _world.Step(deltaTime, SubStepCount);

            foreach (BodyMoveEvent moveEvent in _world.GetBodyMoveEvents())
            {
                if (moveEvent.UserData == IntPtr.Zero) continue;
                if (GCHandle.FromIntPtr(moveEvent.UserData).Target is Box3dBody body)
                {
                    body.ApplyMoveEvent(moveEvent.Transform);
                }
            }
        }

        private void OnDestroy()
        {
            if (_world.IsValid) _world.Destroy();
            if (_instance == this) _instance = null;
        }
    }
}
