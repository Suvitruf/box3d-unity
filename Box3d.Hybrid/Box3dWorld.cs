using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Box3d.Hybrid
{
    public interface IBox3dSensorHandler { Box3dShape SensorShape { get; } void OnBox3dSensorEnter(Box3dShape other); void OnBox3dSensorExit(Box3dShape other); }

    public static class Box3dQuery
    {
        public const int MaxHits = 64;

        public static bool Raycast(Vector3 origin, Vector3 direction, out Box3dRaycastHit hit, float maxDistance, int layerMask)
        {
            hit = default;
            var world = Box3dWorld.Instance.World;
            var filter = QueryFilterForMask(layerMask);
            RayResult result = world.CastRayClosest(origin, direction * maxDistance, filter);
            if (!result.Hit) return false;
            hit = new Box3dRaycastHit
            {
                point = result.Point,
                normal = result.Normal,
                fraction = result.Fraction,
                shapeId = result.ShapeId,
                hit = true,
            };
            return true;
        }

        public static int RaycastAll(Vector3 origin, Vector3 direction, Box3dRaycastHit[] results, float maxDistance, int layerMask)
        {
            var world = Box3dWorld.Instance.World;
            var filter = QueryFilterForMask(layerMask);
            Span<RayHit> hits = stackalloc RayHit[MaxHits];
            int count = world.CastRay(origin, direction * maxDistance, filter, hits);
            int resultCount = Mathf.Min(count, results.Length);
            for (int i = 0; i < resultCount; i++)
            {
                results[i] = new Box3dRaycastHit
                {
                    point = hits[i].Point,
                    normal = hits[i].Normal,
                    fraction = hits[i].Fraction,
                    shapeId = hits[i].ShapeId,
                    hit = true,
                };
            }
            return resultCount;
        }

        public static int OverlapSphere(Vector3 position, float radius, Box3dRaycastHit[] results, int layerMask)
        {
            var world = Box3dWorld.Instance.World;
            var filter = QueryFilterForMask(layerMask);
            Span<ShapeId> shapeIds = stackalloc ShapeId[MaxHits];
            Span<float3> proxy = stackalloc float3[1];
            proxy[0] = float3.zero;
            int count = world.OverlapShape(position, proxy, radius, filter, shapeIds);
            int resultCount = Mathf.Min(count, results.Length);
            for (int i = 0; i < resultCount; i++)
            {
                results[i] = new Box3dRaycastHit { shapeId = shapeIds[i], hit = true };
            }
            return resultCount;
        }

        internal static QueryFilter QueryFilterForMask(int layerMask)
        {
            return new QueryFilter { CategoryBits = ulong.MaxValue, MaskBits = (ulong)layerMask };
        }

        public static Box3dShape ShapeIdToComponent(ShapeId shapeId)
        {
            return Box3dWorld.Instance.GetShapeComponent(shapeId);
        }
    }

    public struct Box3dRaycastHit
    {
        public Vector3 point;
        public Vector3 normal;
        public float fraction;
        public ShapeId shapeId;
        public bool hit;

        public Box3dShape ShapeComponent => hit ? Box3dWorld.Instance.GetShapeComponent(shapeId) : null;
        public T GetComponentInParent<T>() => ShapeComponent ? ShapeComponent.GetComponentInParent<T>() : default;
    }

    /// <summary>Scene-level owner of a Box3d simulation world. Step() must be called externally
    /// (by ManualPhysicsController.OnAnimatorMove) at a fixed tick rate. Syncs kinematic and
    /// dynamic bodies before the step writes moved bodies back after.</summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class Box3dWorld : MonoBehaviour
    {
        [SerializeField, Tooltip("Gravity vector applied to every dynamic body.")]
        private Vector3 Gravity = new Vector3(0f, -9.81f, 0f);

        [SerializeField, Min(1), Tooltip("Solver sub-steps per step. Higher = stiffer, slower.")]
        private int SubStepCount = 4;

        [SerializeField, Min(1), Tooltip("Box3d worker threads. 1 = deterministic, single-threaded. Use 1 for rollback netcode.")]
        private int WorkerCount = 1;

        /// <summary>Set to true during rollback replay to prevent LateUpdate from pushing Transform changes to the body.</summary>
        public static bool IsReplaying;

        [SerializeField, Tooltip("Overlay physics debug geometry in the Scene view each frame (None = off). " +
            "Enable Contacts / Islands / Forces / Bounds to see the solver's view of the world. " +
            "For the Game view, turn on its Gizmos toggle.")]
        private DebugDrawFlags DebugDraw = DebugDrawFlags.None;

        [SerializeField, Min(1f), Tooltip("Half-size of the box around the origin that debug drawing covers.")]
        private float DebugDrawRadius = 200f;

        private readonly List<Box3dBody> _kinematicBodies = new List<Box3dBody>();
        private readonly List<Box3dBody> _dynamicBodies = new List<Box3dBody>();
        private readonly List<Box3dShape> _standaloneShapes = new List<Box3dShape>();
        private readonly List<IBox3dSensorHandler> _sensorHandlers = new List<IBox3dSensorHandler>();
        private readonly Dictionary<ulong, Box3dShape> _shapeMap = new Dictionary<ulong, Box3dShape>();

        private World _world;
        private Body _anchor;
        private Vector3 _lastGravity;
        private static Box3dWorld _instance;

        public World World => _world;

        public int WorldSubStepCount => SubStepCount;

        public Body WorldAnchor
        {
            get
            {
                if (!_anchor.IsValid)
                {
                    EnsureCreated();
                    _anchor = _world.CreateBody(BodyDef.Default);
                }
                return _anchor;
            }
        }

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

        public void EnsureCreated()
        {
            if (_world.IsValid) return;

            WorldDef def = WorldDef.Default;
            def.Gravity = Gravity;
            def.WorkerCount = (uint)WorkerCount;
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

        internal void RegisterStandaloneShape(Box3dShape shape) { if (!_standaloneShapes.Contains(shape)) _standaloneShapes.Add(shape); }
        internal void UnregisterStandaloneShape(Box3dShape shape) { _standaloneShapes.Remove(shape); }

        internal void AddDynamic(Box3dBody body)
        {
            if (!_dynamicBodies.Contains(body)) _dynamicBodies.Add(body);
        }

        internal void RemoveDynamic(Box3dBody body)
        {
            _dynamicBodies.Remove(body);
        }

        public void UnsyncBody(Box3dBody body) { _kinematicBodies.Remove(body); _dynamicBodies.Remove(body); }

        public int CollectDynamicBodies(ref Box3dBody[] buffer)
        {
            if (buffer == null || buffer.Length < _dynamicBodies.Count)
                buffer = new Box3dBody[_dynamicBodies.Count];
            _dynamicBodies.CopyTo(buffer);
            return _dynamicBodies.Count;
        }

        internal void RegisterShape(ShapeId shapeId, Box3dShape shape)
        {
            _shapeMap[shapeId.ToUInt64()] = shape;
        }

        internal void UnregisterShape(ShapeId shapeId)
        {
            _shapeMap.Remove(shapeId.ToUInt64());
        }

        public Box3dShape GetShapeComponent(ShapeId shapeId)
        {
            _shapeMap.TryGetValue(shapeId.ToUInt64(), out var shape);
            return shape;
        }

        public void RegisterSensorHandler(IBox3dSensorHandler h) { if (!_sensorHandlers.Contains(h)) _sensorHandlers.Add(h); }
        public void UnregisterSensorHandler(IBox3dSensorHandler h) { _sensorHandlers.Remove(h); }

        public void Step(float deltaTime)
        {
            if (!_world.IsValid) return;

            if (_lastGravity != Gravity)
            {
                _lastGravity = Gravity;
                _world.SetGravity(Gravity);
            }

            for (int i = 0; i < _kinematicBodies.Count; i++)
            {
                Box3dBody body = _kinematicBodies[i];
                if (body) body.SyncFromTransform();
                if (body) body.PushKinematic(deltaTime);
            }

            for (int i = 0; i < _standaloneShapes.Count; i++)
            {
                Box3dShape shape = _standaloneShapes[i];
                if (shape) shape.SyncTransform();
            }

            for (int i = 0; i < _dynamicBodies.Count; i++)
            {
                Box3dBody body = _dynamicBodies[i];
                if (body) body.SyncFromTransform();
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

            if (_sensorHandlers.Count > 0)
            {
                SensorEvents events = _world.GetSensorEvents();
                foreach (var begin in events.Begin)
                {
                    var a = GetShapeComponent(begin.SensorShapeId); var b = GetShapeComponent(begin.VisitorShapeId);
                    if (!a || !b || !a.enabled || !b.enabled) continue;
                    foreach (var h in _sensorHandlers) { if (h.SensorShape == a) h.OnBox3dSensorEnter(b); else if (h.SensorShape == b) h.OnBox3dSensorEnter(a); }
                }
                foreach (var end in events.End)
                {
                    var a = GetShapeComponent(end.SensorShapeId); var b = GetShapeComponent(end.VisitorShapeId);
                    if (!a || !b || !a.enabled || !b.enabled) continue;
                    foreach (var h in _sensorHandlers) { if (h.SensorShape == a) h.OnBox3dSensorExit(b); else if (h.SensorShape == b) h.OnBox3dSensorExit(a); }
                }
            }
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

        private void OnDestroy()
        {
            if (_world.IsValid) _world.Destroy();
            if (_instance == this) _instance = null;
        }
    }
}
