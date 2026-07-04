using Unity.Mathematics;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>A box shape, analogous to Unity's BoxCollider.</summary>
    [AddComponentMenu("Box3d/Box3d Box Shape")]
    public class Box3dBoxShape : Box3dShape
    {
        [SerializeField, Tooltip("Full box size in local units (like Unity's BoxCollider.size).")]
        private Vector3 Size = Vector3.one;

        protected override Shape CreateShape(Body body, float3 scale)
        {
            float3 halfExtents = (float3)Size * 0.5f * math.abs(scale);
            float3 center = LocalCenter * scale;
            BoxHull hull = BoxHull.CreateOffset(halfExtents.x, halfExtents.y, halfExtents.z, center);
            return body.CreateHullShape(BuildDef(), in hull);
        }

        private void OnDrawGizmosSelected()
        {
            SetGizmoFrame();
            float3 size = (float3)Size * math.abs((float3)transform.lossyScale);
            Gizmos.DrawWireCube((Vector3)ScaledCenter, (Vector3)size);
        }
    }
}
