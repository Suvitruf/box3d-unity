using Unity.Mathematics;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>Base class for shape components, analogous to Unity's Collider. Attaches to the
    /// <see cref="Box3dBody"/> on the same GameObject when that body is created. Friction and
    /// restitution can be changed at runtime; density is baked at creation (it changes mass).</summary>
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

        internal void AttachTo(Body body, float3 scale)
        {
            _shape = CreateShape(body, scale);
        }

        /// <summary>Creates the native shape on the given body. <paramref name="scale"/> is the
        /// owning GameObject's lossy scale, to bake into the shape dimensions.</summary>
        protected abstract Shape CreateShape(Body body, float3 scale);

        /// <summary>Frees any native geometry this shape owns. Called by the body AFTER the body
        /// (and its shapes) are destroyed, so referenced mesh/heightfield data outlives its shape.
        /// Default: nothing (spheres/boxes/hulls are self-contained or cloned).</summary>
        internal virtual void ReleaseGeometry() { }

        // The color Unity uses for collider gizmos, so these read as familiar.
        private static readonly Color GizmoColor = new Color(0.5f, 0.9f, 0.6f, 0.9f);

        /// <summary>Sets the gizmo color and a position+rotation frame (no scale — subclasses bake
        /// the lossy scale into their dimensions to match the physics shape exactly).</summary>
        protected void SetGizmoFrame()
        {
            Gizmos.color = GizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        }

        protected float3 ScaledCenter => (float3)Center * (float3)transform.lossyScale;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying || !_shape.IsValid) return;
            _shape.SetFriction(Friction);
            _shape.SetRestitution(Restitution);
        }
#endif
    }
}
