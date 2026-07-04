using Unity.Mathematics;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>A convex hull shape built from a mesh's vertices, analogous to a convex
    /// MeshCollider. The hull is cloned into the world at creation, so it works on dynamic bodies
    /// (unlike the concave <see cref="Box3dMeshShape"/>).</summary>
    [AddComponentMenu("Box3d/Box3d Hull Shape")]
    public class Box3dHullShape : Box3dShape
    {
        [SerializeField, Tooltip("Mesh whose vertices define the convex hull. Must be Read/Write enabled.")]
        private Mesh Mesh;

        [SerializeField, Range(4, 64), Tooltip("Maximum hull vertices kept from the mesh.")]
        private int MaxVertices = 64;

        /// <summary>Sets the source mesh. Must be set before the body creates the shape (Awake).</summary>
        public void SetMesh(Mesh mesh)
        {
            Mesh = mesh;
        }

        protected override Shape CreateShape(Body body, float3 scale)
        {
            if (!Mesh)
            {
                Debug.LogError($"[Box3d] {name}: Box3dHullShape has no mesh assigned.", this);
                return default;
            }

            Vector3[] vertices = Mesh.vertices;
            if (vertices.Length < 4)
            {
                Debug.LogError($"[Box3d] {name}: mesh '{Mesh.name}' has too few readable vertices " +
                               "(is Read/Write enabled?).", this);
                return default;
            }

            var points = new float3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                points[i] = ((float3)vertices[i] + LocalCenter) * scale;
            }

            Hull hull = Hull.Create(points, MaxVertices);
            Shape shape = body.CreateHullShape(BuildDef(), hull);
            hull.Destroy(); // cloned into the world — safe to free immediately
            return shape;
        }

        private void OnDrawGizmosSelected()
        {
            // Approximate: draws the source mesh, not the exact convex hull (close enough to place).
            if (!Mesh) return;
            Gizmos.color = new Color(0.5f, 0.9f, 0.6f, 0.9f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(Mesh, (Vector3)LocalCenter);
        }
    }
}
