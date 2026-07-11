using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>Motion type of a <see cref="Box3dBody"/>, mirroring Unity's static collider /
    /// kinematic / dynamic Rigidbody distinction.</summary>
    public enum Box3dBodyType
    {
        Static,
        Kinematic,
        Dynamic,
    }

    /// <summary>A physics body, analogous to Unity's Rigidbody. The native body is created once
    /// (Awake) and destroyed once (OnDestroy); enabling/disabling toggles it in the simulation
    /// without recreating it. Dynamic bodies drive their Transform (synced from body-move events);
    /// kinematic bodies follow it. To move a body from code use <see cref="Position"/> /
    /// <see cref="Rotation"/> (like Unity's Rigidbody.position), not transform.position directly.</summary>
    [AddComponentMenu("Box3d/Box3d Body")]
    [DisallowMultipleComponent]
    public class Box3dBody : MonoBehaviour
    {
        [SerializeField, Tooltip("Static: never moves. Kinematic: moved by its Transform. Dynamic: moved by the solver.")]
        private Box3dBodyType Type = Box3dBodyType.Dynamic;

        [SerializeField, Min(0f), Tooltip("Linear velocity damping.")]
        private float LinearDamping;

        [SerializeField, Min(0f), Tooltip("Angular velocity damping.")]
        private float AngularDamping = 0.05f;

        [SerializeField, Tooltip("Start awake, or asleep until disturbed.")]
        private bool StartAwake = true;

        [SerializeField, Tooltip("Allow fast rotation (e.g. spinning wheels) without box3d clamping angular velocity.")]
        private bool AllowFastRotation;

        [Header("Motion Locks (like Rigidbody.constraints)")]
        [SerializeField] private bool FreezePositionX;
        [SerializeField] private bool FreezePositionY;
        [SerializeField] private bool FreezePositionZ;
        [SerializeField] private bool FreezeRotationX;
        [SerializeField] private bool FreezeRotationY;
        [SerializeField] private bool FreezeRotationZ;

        private Box3dWorld _world;
        private Body _body;
        private Box3dShape[] _shapes;
        private GCHandle _handle;

        /// <summary>The underlying Box3d body (valid between Awake and OnDestroy).</summary>
        public Body Body => _body;

        /// <summary>Motion type. Setting it at runtime re-types the live body (like toggling
        /// Rigidbody.isKinematic).</summary>
        public Box3dBodyType BodyType
        {
            get => Type;
            set
            {
                if (Type == value) return;
                bool wasKinematic = Type == Box3dBodyType.Kinematic;
                Type = value;
                if (!_body.IsValid) return;

                _body.SetType(ToNative(value));
                bool isKinematic = value == Box3dBodyType.Kinematic;
                bool wasDynamic = !wasKinematic;
                bool isDynamic = !isKinematic;
                if (isKinematic && !wasKinematic) _world.AddKinematic(this);
                else if (!isKinematic && wasKinematic) _world.RemoveKinematic(this);
                if (isDynamic && !wasDynamic) _world.AddDynamic(this);
                else if (!isDynamic && wasDynamic) _world.RemoveDynamic(this);
            }
        }

        /// <summary>World position. Setting it teleports the body (and wakes a dynamic one) — the
        /// correct way to move a body from code.</summary>
        public Vector3 Position
        {
            get => transform.position;
            set => Warp(value, transform.rotation);
        }

        /// <summary>World rotation. Setting it teleports the body (and wakes a dynamic one).</summary>
        public Quaternion Rotation
        {
            get => transform.rotation;
            set => Warp(transform.position, value);
        }

        /// <summary>Enables fast rotation (spinning wheels). Set before the body is created (Awake).</summary>
        public void SetAllowFastRotation(bool value)
        {
            AllowFastRotation = value;
        }

        private void Awake()
        {
            _world = Box3dWorld.Instance;

            BodyDef def = BodyDef.Default;
            def.Type = ToNative(Type);
            def.Position = transform.position;
            def.Rotation = transform.rotation;
            def.LinearDamping = LinearDamping;
            def.AngularDamping = AngularDamping;
            def.IsAwake = StartAwake;
            def.IsEnabled = isActiveAndEnabled;
            def.AllowFastRotation = AllowFastRotation;

            _handle = GCHandle.Alloc(this);
            def.UserData = GCHandle.ToIntPtr(_handle);
            _body = _world.World.CreateBody(def);

            ApplyMotionLocks();

            var shapes = new System.Collections.Generic.List<Box3dShape>();
            GatherShapes(transform, shapes, isRoot: true);
            _shapes = shapes.ToArray();

            quaternion bodyInverse = math.inverse((quaternion)transform.rotation);
            foreach (Box3dShape shape in _shapes)
            {
                Transform shapeTransform = shape.transform;
                float3 localPosition = transform.InverseTransformPoint(shapeTransform.position);
                quaternion localRotation = math.mul(bodyInverse, shapeTransform.rotation);
                shape.AttachTo(_body, localPosition, localRotation, shapeTransform.lossyScale);
            }

            if (Type == Box3dBodyType.Kinematic) _world.AddKinematic(this);
            if (Type == Box3dBodyType.Dynamic) _world.AddDynamic(this);
            transform.hasChanged = false;
        }

        private void OnEnable()
        {
            if (!_body.IsValid) return;
            _body.SetTransform(transform.position, transform.rotation);
            _body.Enable();
            transform.hasChanged = false;
        }

        private void OnDisable()
        {
            if (_body.IsValid) _body.Disable();
        }

        private void OnDestroy()
        {
            if (Type == Box3dBodyType.Kinematic && _world) _world.RemoveKinematic(this);
            if (Type == Box3dBodyType.Dynamic && _world) _world.RemoveDynamic(this);
            if (_body.IsValid) _body.Destroy();
            // Free referenced geometry (meshes) only after the body — and its shapes — are gone.
            if (_shapes != null)
            {
                foreach (Box3dShape shape in _shapes)
                {
                    if (shape) shape.ReleaseGeometry();
                }
            }
            if (_handle.IsAllocated) _handle.Free();
        }

        // Collects shape components on this GameObject and descendants, stopping at any nested
        // Box3dBody (that subtree belongs to the other body). Unity's compound-collider gathering.
        private static void GatherShapes(Transform node, System.Collections.Generic.List<Box3dShape> result, bool isRoot)
        {
            if (!isRoot && node.GetComponent<Box3dBody>()) return;

            node.GetComponents(TempShapes);
            result.AddRange(TempShapes);

            for (int i = 0; i < node.childCount; i++)
            {
                GatherShapes(node.GetChild(i), result, isRoot: false);
            }
        }

        private static readonly System.Collections.Generic.List<Box3dShape> TempShapes =
            new System.Collections.Generic.List<Box3dShape>();

        /// <summary>Called by the world before each step to sync a Dynamic body's Transform changes to the native body.</summary>
        internal void SyncFromTransform()
        {
            if (!_body.IsValid) return;
            _body.SetTransform(transform.position, transform.rotation);
            _body.SetAwake(true);
        }

        /// <summary>Called by the world (kinematic list only) before each step.</summary>
        internal void PushKinematic(float deltaTime)
        {
            if (!isActiveAndEnabled || !_body.IsValid) return;
            var target = new B3Transform { Position = transform.position, Rotation = transform.rotation };
            _body.SetTargetTransform(target, deltaTime, wake: true);
        }

        /// <summary>Called by the world after each step to write a body-move event to the Transform.</summary>
        internal void ApplyMoveEvent(B3Transform moved)
        {
            transform.SetPositionAndRotation(moved.Position, moved.Rotation);
        }

        private void Warp(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            if (_body.IsValid)
            {
                _body.SetTransform(position, rotation);
                if (Type == Box3dBodyType.Dynamic) _body.SetAwake(true);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // The field already holds the new value here, so push it straight to the body and
            // refresh kinematic-list membership (Add/Remove are idempotent).
            if (!Application.isPlaying || !_body.IsValid) return;
            _body.SetType(ToNative(Type));
            _body.SetLinearDamping(LinearDamping);
            _body.SetAngularDamping(AngularDamping);
            ApplyMotionLocks();
            if (Type == Box3dBodyType.Kinematic) _world.AddKinematic(this);
            else _world.RemoveKinematic(this);
        }
#endif

        private void ApplyMotionLocks()
        {
            if (!_body.IsValid) return;
            _body.SetMotionLocks(new MotionLocks
            {
                LinearX = FreezePositionX,
                LinearY = FreezePositionY,
                LinearZ = FreezePositionZ,
                AngularX = FreezeRotationX,
                AngularY = FreezeRotationY,
                AngularZ = FreezeRotationZ,
            });
        }

        private static Box3d.BodyType ToNative(Box3dBodyType type)
        {
            switch (type)
            {
                case Box3dBodyType.Static: return Box3d.BodyType.Static;
                case Box3dBodyType.Kinematic: return Box3d.BodyType.Kinematic;
                default: return Box3d.BodyType.Dynamic;
            }
        }
    }
}
