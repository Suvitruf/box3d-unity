using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>Base class for shape components, analogous to Unity's Collider. When the GameObject
    /// (or an ancestor) has a <see cref="Box3dBody"/>, the shape attaches to it — including shapes
    /// on child GameObjects (compound colliders). A shape with no body anywhere above it creates
    /// its own static body, mirroring Unity's "collider without a rigidbody is static".
    /// Friction and restitution can be changed at runtime; density is baked at creation.</summary>
    public abstract class Box3dShape : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Density in kg/m³ (mass = density × volume). Baked at creation.")]
        private float Density = 1000f;

        [SerializeField, Range(0f, 1f), Tooltip("Coulomb friction coefficient.")]
        private float Friction = 0.6f;

        [SerializeField, Range(0f, 1f), Tooltip("Bounciness. Only applies above the world's restitution speed threshold (~1 m/s), so gentle settling never bounces.")]
        private float Restitution;

        [SerializeField, Tooltip("Local offset of the shape from the body origin.")]
        protected Vector3 Center = Vector3.zero;

        [SerializeField, Tooltip("Is this shape a sensor (trigger)?")]
        public bool IsSensor;

        [SerializeField, Tooltip("Enable sensor events for trigger-style detection.")]
        private bool EnableSensorEvents = true;

        [SerializeField, Tooltip("Enable contact events for OnCollision-style detection.")]
        private bool EnableContactEvents;

        private Shape _shape;
        private Body _ownBody;
        private GCHandle _selfHandle;

        public ShapeId ShapeId => _shape.Id;
        public Shape Shape => _shape;
        public bool IsSensorShape => IsSensor;

        public float CapsuleRadius { get; protected set; }
        public float CapsuleHeight { get; protected set; }

        public Vector3 ShapeLocalCenter => Center;

        protected float3 LocalCenter => Center;

        public void SetFriction(float value) { Friction = value; if (_shape.IsValid) _shape.SetFriction(value); }
        public void SetRestitution(float value) { Restitution = value; if (_shape.IsValid) _shape.SetRestitution(value); }
        public void SetDensity(float value) { Density = value; if (_shape.IsValid) _shape.SetDensity(value, updateBodyMass: true); }

        private void Awake()
        {
            if (_shape.IsValid) return; // already attached by a body

            Box3dWorld world = Box3dWorld.Instance;
            world.EnsureCreated();
            BodyDef def = BodyDef.Default;
            def.Position = transform.position;
            def.Rotation = transform.rotation;
            _ownBody = world.World.CreateBody(def);
            AttachTo(_ownBody, float3.zero, quaternion.identity, transform.lossyScale);
            Box3dWorld.Instance.RegisterStandaloneShape(this);
        }

        internal void SyncTransform()
        {
            if (!_ownBody.IsValid) return;
            _ownBody.SetTransform(transform.position, transform.rotation);
        }

        private void OnEnable() { if (_ownBody.IsValid) _ownBody.Enable(); }
        private void OnDisable() { if (_ownBody.IsValid) _ownBody.Disable(); }

        private void OnDestroy()
        {
            Box3dWorld.Instance.UnregisterStandaloneShape(this);
            if (_shape.IsValid)
            {
                Box3dWorld.Instance.UnregisterShape(_shape.Id);
            }
            if (_selfHandle.IsAllocated) _selfHandle.Free();

            if (_ownBody.IsValid)
            {
                _ownBody.Destroy();
                ReleaseGeometry();
            }
        }

        protected ShapeDef BuildDef()
        {
            ShapeDef def = ShapeDef.Default;
            def.Density = Density;
            def.BaseMaterial.Friction = Friction;
            def.BaseMaterial.Restitution = Restitution;
            def.Filter.CategoryBits = 1UL << gameObject.layer;
            def.Filter.MaskBits = CollisionMaskForLayer(gameObject.layer);
            def.IsSensor = IsSensor;
            def.EnableSensorEvents = EnableSensorEvents;
            def.EnableContactEvents = EnableContactEvents;

            _selfHandle = GCHandle.Alloc(this);
            def.UserData = GCHandle.ToIntPtr(_selfHandle);
            return def;
        }

        private static ulong CollisionMaskForLayer(int layer)
        {
            ulong mask = 0;
            for (int other = 0; other < 32; other++)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, other)) mask |= 1UL << other;
            }
            return mask;
        }

        internal void AttachTo(Body body, float3 localPosition, quaternion localRotation, float3 scale)
        {
            if (_ownBody.IsValid && _ownBody.Id.ToUInt64() != body.Id.ToUInt64())
            { _ownBody.Destroy(); _ownBody = default; }
            _shape = CreateShape(body, localPosition, localRotation, scale);
            if (_shape.IsValid)
            {
                Box3dWorld.Instance.RegisterShape(_shape.Id, this);
                _shape.UserData = BuildDef().UserData;
            }
        }

        protected abstract Shape CreateShape(Body body, float3 localPosition, quaternion localRotation, float3 scale);

        internal virtual void ReleaseGeometry() { }

        protected float3 ShapeCenter(float3 localPosition, quaternion localRotation, float3 scale)
        {
            return localPosition + math.mul(localRotation, LocalCenter * scale);
        }

        public Vector3 GetClosestPoint(Vector3 point)
        {
            var body = new Body { Id = _shape.GetBody() };
            var bodyPos = body.Position;
            var bodyRot = body.Rotation;
            var localPt = math.mul(math.inverse(bodyRot), (float3)point - bodyPos);
            var localResult = ClosestPointLocal(localPt);
            var worldResult = bodyPos + math.mul(bodyRot, (float3)localResult);
            return worldResult;
        }

        protected virtual Vector3 ClosestPointLocal(float3 localPoint)
        {
            return transform.position;
        }

        private static readonly Color GizmoColor = new Color(0.5f, 0.9f, 0.6f, 0.9f);

        protected void SetGizmoFrame()
        {
            Gizmos.color = GizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        }

        protected float3 ScaledCenter => (float3)Center * (float3)transform.lossyScale;
    }
}
