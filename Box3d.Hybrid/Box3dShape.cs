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
        private Vector3 Center = Vector3.zero;

        private Shape _shape;
        private Body _ownBody; // only set when this shape has no Box3dBody and creates a static one

        protected float3 LocalCenter => Center;

        /// <summary>Sets friction, updating the live shape if it exists.</summary>
        public void SetFriction(float value)
        {
            Friction = value;
            if (_shape.IsValid) _shape.SetFriction(value);
        }

        /// <summary>Sets bounciness, updating the live shape if it exists.</summary>
        public void SetRestitution(float value)
        {
            Restitution = value;
            if (_shape.IsValid) _shape.SetRestitution(value);
        }

        /// <summary>Sets density (kg/m³), updating the live shape and its body's mass if it exists.
        /// Set before the body is created to have it apply at build time.</summary>
        public void SetDensity(float value)
        {
            Density = value;
            if (_shape.IsValid) _shape.SetDensity(value, updateBodyMass: true);
        }

        private void Awake()
        {
            // A body on this GameObject or an ancestor will gather and attach this shape (including
            // as a compound child). Otherwise the shape is an orphan → give it a static body.
            if (GetComponentInParent<Box3dBody>()) return;

            Box3dWorld world = Box3dWorld.Instance;
            BodyDef def = BodyDef.Default; // static by default
            def.Position = transform.position;
            def.Rotation = transform.rotation;
            _ownBody = world.World.CreateBody(def);
            AttachTo(_ownBody, float3.zero, quaternion.identity, transform.lossyScale);
        }

        private void OnDestroy()
        {
            // Only the self-created static body is ours to tear down; body-managed shapes are
            // released by their Box3dBody after it destroys the body.
            if (_ownBody.IsValid)
            {
                _ownBody.Destroy();
                ReleaseGeometry();
            }
        }

        /// <summary>A shape definition seeded from this component's material fields and the
        /// GameObject's Unity collision layer (so Project Settings → Physics → Layer Collision
        /// Matrix is honored).</summary>
        protected ShapeDef BuildDef()
        {
            ShapeDef def = ShapeDef.Default;
            def.Density = Density;
            def.BaseMaterial.Friction = Friction;
            def.BaseMaterial.Restitution = Restitution;
            def.Filter.CategoryBits = 1UL << gameObject.layer;
            def.Filter.MaskBits = CollisionMaskForLayer(gameObject.layer);
            return def;
        }

        // Builds a box3d mask from Unity's layer collision matrix: bit L is set for every layer
        // this layer is allowed to collide with.
        private static ulong CollisionMaskForLayer(int layer)
        {
            ulong mask = 0;
            for (int other = 0; other < 32; other++)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, other)) mask |= 1UL << other;
            }
            return mask;
        }

        /// <summary>Creates the native shape on the given body. <paramref name="localPosition"/> and
        /// <paramref name="localRotation"/> place the shape relative to the body's frame (identity
        /// for a shape on the body's own GameObject); <paramref name="scale"/> is the shape
        /// GameObject's lossy scale, baked into the dimensions.</summary>
        internal void AttachTo(Body body, float3 localPosition, quaternion localRotation, float3 scale)
        {
            _shape = CreateShape(body, localPosition, localRotation, scale);
        }

        protected abstract Shape CreateShape(Body body, float3 localPosition, quaternion localRotation, float3 scale);

        /// <summary>Frees any native geometry this shape owns. Called after the body — and its
        /// shapes — are destroyed, so referenced mesh/heightfield data outlives its shape.</summary>
        internal virtual void ReleaseGeometry() { }

        /// <summary>The shape's local center offset, rotated and offset into the body frame.</summary>
        protected float3 ShapeCenter(float3 localPosition, quaternion localRotation, float3 scale)
        {
            return localPosition + math.mul(localRotation, LocalCenter * scale);
        }

        // The color Unity uses for collider gizmos, so these read as familiar.
        private static readonly Color GizmoColor = new Color(0.5f, 0.9f, 0.6f, 0.9f);

        /// <summary>Sets the gizmo color and a position+rotation frame at the shape's own Transform
        /// (which is where the physics shape ends up, self or compound child).</summary>
        protected void SetGizmoFrame()
        {
            Gizmos.color = GizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        }

        protected float3 ScaledCenter => (float3)Center * (float3)transform.lossyScale;
    }
}
