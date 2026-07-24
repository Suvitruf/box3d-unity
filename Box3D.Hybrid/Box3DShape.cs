using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>Base class for shape components, analogous to Unity's Collider. When the GameObject
    /// (or an ancestor) has a <see cref="Box3DBody"/>, the shape attaches to it — including shapes
    /// on child GameObjects (compound colliders). A shape with no body anywhere above it creates
    /// its own static body, mirroring Unity's "collider without a rigidbody is static".
    /// Adding a shape in the editor auto-adds a <see cref="Box3DBody"/> when the hierarchy has
    /// none (set its type to Static for non-moving geometry).
    /// Friction and restitution can be changed at runtime; density is baked at creation.</summary>
    public abstract class Box3DShape : MonoBehaviour
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
        private Body _ownBody; // only set when this shape has no Box3DBody and creates a static one

        // The attach frame from AttachTo, kept so Inspector edits can rebuild geometry in place.
        private float3 _attachPosition;
        private quaternion _attachRotation = quaternion.identity;
        private float3 _attachScale = new float3(1f);

        protected float3 LocalCenter => Center;

        /// <summary>The live native shape (valid between Awake and OnDestroy).</summary>
        protected Shape LiveShape => _shape;

        protected float3 AttachedPosition => _attachPosition;
        protected quaternion AttachedRotation => _attachRotation;
        protected float3 AttachedScale => _attachScale;

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

#if UNITY_EDITOR
        // Runs when the component is added in the editor (and on context-menu Reset). Like
        // RequireComponent, but hierarchy-aware: a compound shape on a child of a body must NOT
        // get its own body (a nested Box3DBody splits the compound), so only orphan shapes get one.
        // For static geometry, set the auto-added body's type to Static.
        private void Reset()
        {
            if (!GetComponentInParent<Box3DBody>(true))
            {
                UnityEditor.Undo.AddComponent<Box3DBody>(gameObject);
            }
        }
#endif

        private void Awake()
        {
            // A body on this GameObject or an ancestor will gather and attach this shape (including
            // as a compound child). Otherwise the shape is an orphan → give it a static body.
            if (GetComponentInParent<Box3DBody>()) return;

            Box3DWorld world = Box3DWorld.Instance;
            BodyDef def = BodyDef.Default; // static by default
            def.Position = transform.position;
            def.Rotation = transform.rotation;
            _ownBody = world.World.CreateBody(def);
            AttachTo(_ownBody, float3.zero, quaternion.identity, transform.lossyScale);
        }

        private void OnDestroy()
        {
            // Only the self-created static body is ours to tear down; body-managed shapes are
            // released by their Box3DBody after it destroys the body.
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
        internal static ulong CollisionMaskForLayer(int layer)
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
            _attachPosition = localPosition;
            _attachRotation = localRotation;
            _attachScale = scale;
            _shape = CreateShape(body, localPosition, localRotation, scale);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Push Inspector edits to the live shape during play. SetDensity with updateBodyMass
            // re-derives mass from the current geometry, so size edits update mass too.
            if (!Application.isPlaying || !_shape.IsValid) return;
            _shape.SetFriction(Friction);
            _shape.SetRestitution(Restitution);
            UpdateLiveGeometry();
            _shape.SetDensity(Density, updateBodyMass: true);
        }
#endif

        /// <summary>Pushes edited geometry to the live native shape where the engine supports
        /// in-place replacement (sphere and capsule). Other shapes keep their creation geometry.</summary>
        protected virtual void UpdateLiveGeometry() { }

#if UNITY_EDITOR
        /// <summary>Creates this shape on a body in a throwaway preview world (rope editor
        /// preview), leaving component state alone. The body must already sit at this shape's
        /// transform pose.</summary>
        internal Shape CreateDetachedShape(Body body)
        {
            return CreateShape(body, float3.zero, quaternion.identity, transform.lossyScale);
        }

        /// <summary>Compound variant for preview bodies that own several shapes (editor physics
        /// simulation): places the shape at a local frame within the body instead of assuming the
        /// body sits at the shape's own transform.</summary>
        internal Shape CreateDetachedShape(Body body, float3 localPosition, quaternion localRotation)
        {
            return CreateShape(body, localPosition, localRotation, transform.lossyScale);
        }

        /// <summary>Frees native geometry a detached preview shape allocated (mesh shapes).
        /// Called after the preview world is destroyed.</summary>
        internal virtual void ReleaseDetachedGeometry() { }
#endif

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
