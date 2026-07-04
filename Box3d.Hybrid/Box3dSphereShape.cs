using Unity.Mathematics;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>A sphere shape, analogous to Unity's SphereCollider. Non-uniform scale uses the
    /// largest axis (same limitation as Unity).</summary>
    [AddComponentMenu("Box3d/Box3d Sphere Shape")]
    public class Box3dSphereShape : Box3dShape
    {
        [SerializeField, Min(0f), Tooltip("Sphere radius in local units.")]
        private float Radius = 0.5f;

        protected override Shape CreateShape(Body body, float3 scale)
        {
            float scaledRadius = Radius * math.cmax(math.abs(scale));
            return body.CreateSphereShape(BuildDef(), new Sphere { Center = LocalCenter * scale, Radius = scaledRadius });
        }

        private void OnDrawGizmosSelected()
        {
            SetGizmoFrame();
            float radius = Radius * math.cmax(math.abs((float3)transform.lossyScale));
            Gizmos.DrawWireSphere((Vector3)ScaledCenter, radius);
        }
    }
}
