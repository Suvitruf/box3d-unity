using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A radial impulse burst at this object's position (native <c>World.Explode</c>).
    /// Full impulse inside <see cref="Radius"/>, fading to zero over <see cref="Falloff"/> beyond
    /// it. Trigger from code via <see cref="Explode"/>, the Inspector's Explode button (play mode),
    /// or automatically each time the component is enabled — handy on spawned prefabs. Select the
    /// object to see both radii and the blast rays.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DExplosion.png")]
    [AddComponentMenu("Box3D/Forces/Explosion")]
    public class Box3DExplosion : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Radius in meters that receives the full impulse.")]
        private float Radius = 4f;

        [SerializeField, Min(0f), Tooltip("Extra distance beyond Radius over which the impulse fades to zero.")]
        private float Falloff = 2f;

        [SerializeField, Tooltip("Impulse per m² of exposed surface. Water-density objects weigh hundreds of kg, so useful values are in the thousands.")]
        private float ImpulsePerArea = 3000f;

        [SerializeField, Tooltip("Explode every time this component is enabled — drop an enabled prefab (or toggle the component) to detonate.")]
        private bool ExplodeOnEnable;

        private void OnEnable()
        {
            if (Application.isPlaying && ExplodeOnEnable) Explode();
        }

        /// <summary>Detonates now, at the object's current position. Play mode only.</summary>
        public void Explode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Box3D] Explode() only works in Play mode.", this);
                return;
            }

            ExplosionDef def = ExplosionDef.Default;
            def.Position = transform.position;
            def.Radius = Radius;
            def.Falloff = Falloff;
            def.ImpulsePerArea = ImpulsePerArea;
            IBox3DWorld world = IBox3DWorld.Get(this);
            if (!IBox3DWorld.Validate(world)) return;
            world.PhysicsWorld.Explode(def);
        }

        // Matches the Forces category icon color.
        private static readonly Color GizmoColor = new Color(0.96f, 0.82f, 0.35f, 0.9f);

        private void OnDrawGizmosSelected()
        {
            Vector3 center = transform.position;
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireSphere(center, Radius);

            // Blast rays between the full-impulse core and the outer edge.
            foreach (Vector3 dir in RayDirections)
            {
                Gizmos.DrawLine(center + dir * (Radius * 0.2f), center + dir * (Radius * 0.7f));
            }

            // The faded outer sphere marks where the impulse reaches zero.
            if (Falloff > 0f)
            {
                Gizmos.color = new Color(GizmoColor.r, GizmoColor.g, GizmoColor.b, 0.35f);
                Gizmos.DrawWireSphere(center, Radius + Falloff);
            }
        }

        private static readonly Vector3[] RayDirections =
        {
            Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back,
            new Vector3(1f, 1f, 1f).normalized, new Vector3(-1f, 1f, 1f).normalized,
            new Vector3(1f, -1f, 1f).normalized, new Vector3(1f, 1f, -1f).normalized,
            new Vector3(-1f, -1f, 1f).normalized, new Vector3(-1f, 1f, -1f).normalized,
            new Vector3(1f, -1f, -1f).normalized, new Vector3(-1f, -1f, -1f).normalized,
        };
    }
}
