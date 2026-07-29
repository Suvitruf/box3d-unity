using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A convex hull shape built from a mesh's vertices, analogous to a convex
    /// MeshCollider. The hull is cloned into the world at creation, so it works on dynamic bodies
    /// (unlike the concave <see cref="Box3DMeshShape"/>).</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DHullShape.png")]
    [AddComponentMenu("Box3D/Shapes/Hull Shape")]
    public class Box3DHullShape : Box3DShape
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

        protected override Shape CreateShape(Body body, float3 localPosition, quaternion localRotation, float3 scale)
        {
            if (!Mesh)
            {
                Debug.LogError($"[Box3D] {name}: Box3DHullShape has no mesh assigned.", this);
                return default;
            }

            Vector3[] vertices = Mesh.vertices;
            if (vertices.Length < 4)
            {
                Debug.LogError($"[Box3D] {name}: mesh '{Mesh.name}' has too few readable vertices " +
                               "(is Read/Write enabled?).", this);
                return default;
            }

            var points = new float3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                points[i] = localPosition + math.mul(localRotation, ((float3)vertices[i] + LocalCenter) * scale);
            }

            Hull hull = Hull.Create(points, MaxVertices);
            Shape shape = body.CreateHullShape(BuildDef(), hull);
            hull.Destroy(); // cloned into the world — safe to free immediately
            return shape;
        }

        // Convex recompute from the dented vertices. Hulls bridge over craters, so this only
        // changes collision where the deformation flattens corners or edges.
        internal override Shape CreateRebuiltShape(Vector3[] vertices, int[] triangles)
        {
            if (vertices.Length < 4) return default;

            var points = new float3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                points[i] = AttachedPosition + math.mul(AttachedRotation, ((float3)vertices[i] + LocalCenter) * AttachedScale);
            }

            Hull hull = Hull.Create(points, MaxVertices);
            if (!hull.IsCreated) return default; // degenerate point cloud — keep the old hull

            Shape shape = AttachedBody.CreateHullShape(BuildDef(), hull);
            hull.Destroy();
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
