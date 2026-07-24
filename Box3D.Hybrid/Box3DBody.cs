using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>Motion type of a <see cref="Box3DBody"/>, mirroring Unity's static collider /
    /// kinematic / dynamic Rigidbody distinction.</summary>
    public enum Box3DBodyType
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
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DBody.png")]
    [AddComponentMenu("Box3D/Body")]
    [DisallowMultipleComponent]
    public class Box3DBody : MonoBehaviour
    {
        [SerializeField, Tooltip("Static: never moves. Kinematic: moved by its Transform. Dynamic: moved by the solver.")]
        private Box3DBodyType Type = Box3DBodyType.Dynamic;

        [SerializeField, Min(0f), Tooltip("Linear velocity damping.")]
        private float LinearDamping;

        [SerializeField, Min(0f), Tooltip("Angular velocity damping.")]
        private float AngularDamping = 0.05f;

        [SerializeField, Tooltip("Start awake, or asleep until disturbed.")]
        private bool StartAwake = true;

        [SerializeField, Tooltip("Allow fast rotation (e.g. spinning wheels) without box3d clamping angular velocity.")]
        private bool AllowFastRotation;

        private Box3DWorld _world;
        private Body _body;
        private Box3DShape[] _shapes;
        // A handle to this component lives in the native body's userData, so a body-move event
        // dereferences straight back here — no managed-side lookup list.
        private GCHandle _handle;

        /// <summary>The underlying Box3D body (valid between Awake and OnDestroy).</summary>
        public Body Body => _body;

        /// <summary>Motion type. Setting it at runtime re-types the live body (like toggling
        /// Rigidbody.isKinematic).</summary>
        public Box3DBodyType BodyType
        {
            get => Type;
            set
            {
                if (Type == value) return;
                bool wasKinematic = Type == Box3DBodyType.Kinematic;
                Type = value;
                if (!_body.IsValid) return;

                _body.SetType(ToNative(value));
                bool isKinematic = value == Box3DBodyType.Kinematic;
                if (isKinematic && !wasKinematic) _world.AddKinematic(this);
                else if (!isKinematic && wasKinematic) _world.RemoveKinematic(this);
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

        // Serialized damping, readable without a live body — the editor physics simulation builds
        // its preview bodies with the same damping the runtime will use.
        internal float LinearDampingValue => LinearDamping;
        internal float AngularDampingValue => AngularDamping;

        private void Awake()
        {
            _world = Box3DWorld.Instance;

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
            _body.SetName(gameObject.name); // recorded — lets the visual replayer map replay bodies to scene objects

            var shapes = new System.Collections.Generic.List<Box3DShape>();
            GatherShapes(transform, shapes, isRoot: true);
            _shapes = shapes.ToArray();

            quaternion bodyInverse = math.inverse((quaternion)transform.rotation);
            foreach (Box3DShape shape in _shapes)
            {
                Transform shapeTransform = shape.transform;
                float3 localPosition = transform.InverseTransformPoint(shapeTransform.position);
                quaternion localRotation = math.mul(bodyInverse, shapeTransform.rotation);
                shape.AttachTo(_body, localPosition, localRotation, shapeTransform.lossyScale);
            }

            if (Type == Box3DBodyType.Kinematic) _world.AddKinematic(this);
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
            if (Type == Box3DBodyType.Kinematic && _world) _world.RemoveKinematic(this);
            if (_body.IsValid) _body.Destroy();
            // Free referenced geometry (meshes) only after the body — and its shapes — are gone.
            if (_shapes != null)
            {
                foreach (Box3DShape shape in _shapes)
                {
                    if (shape) shape.ReleaseGeometry();
                }
            }
            if (_handle.IsAllocated) _handle.Free();
        }

        // Collects shape components on this GameObject and descendants, stopping at any nested
        // Box3DBody (that subtree belongs to the other body). Unity's compound-collider gathering.
        // Internal so the editor physics simulation builds its preview bodies from the same rules.
        internal static void GatherShapes(Transform node, System.Collections.Generic.List<Box3DShape> result, bool isRoot)
        {
            if (!isRoot && node.GetComponent<Box3DBody>()) return;

            node.GetComponents(TempShapes);
            result.AddRange(TempShapes);

            for (int i = 0; i < node.childCount; i++)
            {
                GatherShapes(node.GetChild(i), result, isRoot: false);
            }
        }

        private static readonly System.Collections.Generic.List<Box3DShape> TempShapes =
            new System.Collections.Generic.List<Box3DShape>();

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
            transform.hasChanged = false; // our own write must not read back as a user move
        }

        private void Warp(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            if (_body.IsValid)
            {
                _body.SetTransform(position, rotation);
                if (Type == Box3DBodyType.Dynamic) _body.SetAwake(true);
            }
            transform.hasChanged = false;
        }

#if UNITY_EDITOR
        private void Update()
        {
            // Editor authoring convenience: pick up Scene-view Transform drags during play for
            // non-kinematic bodies (kinematic ones already follow the Transform). Cheap bool check,
            // editor-only — shipped builds never poll.
            if (!Application.isPlaying || Type == Box3DBodyType.Kinematic || !_body.IsValid) return;
            if (!transform.hasChanged) return;

            transform.hasChanged = false;
            _body.SetTransform(transform.position, transform.rotation);
            if (Type == Box3DBodyType.Dynamic) _body.SetAwake(true);
        }
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            // The field already holds the new value here, so push it straight to the body and
            // refresh kinematic-list membership (Add/Remove are idempotent).
            if (!Application.isPlaying || !_body.IsValid) return;
            _body.SetType(ToNative(Type));
            _body.SetLinearDamping(LinearDamping);
            _body.SetAngularDamping(AngularDamping);
            if (Type == Box3DBodyType.Kinematic) _world.AddKinematic(this);
            else _world.RemoveKinematic(this);
        }
#endif

        private static Box3D.BodyType ToNative(Box3DBodyType type)
        {
            switch (type)
            {
                case Box3DBodyType.Static: return Box3D.BodyType.Static;
                case Box3DBodyType.Kinematic: return Box3D.BodyType.Kinematic;
                default: return Box3D.BodyType.Dynamic;
            }
        }
    }
}
