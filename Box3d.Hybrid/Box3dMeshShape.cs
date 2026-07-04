using Unity.Mathematics;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>A triangle mesh shape from a mesh asset, analogous to a non-convex MeshCollider.
    /// STATIC BODIES ONLY (box3d mesh shapes don't move). The mesh data is referenced by the shape,
    /// so this component keeps it alive until the body is destroyed.</summary>
    [AddComponentMenu("Box3d/Box3d Mesh Shape")]
    public class Box3dMeshShape : Box3dShape
    {
        [SerializeField, Tooltip("Mesh to collide against. Must be Read/Write enabled.")]
        private Mesh Mesh;

        private TriangleMesh _mesh;

        /// <summary>Sets the source mesh. Must be set before the body creates the shape (Awake).</summary>
        public void SetMesh(Mesh mesh)
        {
            Mesh = mesh;
        }

        protected override Shape CreateShape(Body body, float3 scale)
        {
            if (!Mesh)
            {
                Debug.LogError($"[Box3d] {name}: Box3dMeshShape has no mesh assigned.", this);
                return default;
            }
            if (body.GetBodyType() != Box3d.BodyType.Static)
            {
                Debug.LogWarning($"[Box3d] {name}: mesh shapes only work on Static bodies; " +
                                 "use Box3dHullShape (convex) for dynamic bodies.", this);
            }

            Vector3[] vertices = Mesh.vertices;
            int[] triangles = Mesh.triangles;
            if (vertices.Length < 3 || triangles.Length < 3)
            {
                Debug.LogError($"[Box3d] {name}: mesh '{Mesh.name}' has no readable geometry " +
                               "(is Read/Write enabled?).", this);
                return default;
            }

            var points = new float3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                points[i] = (float3)vertices[i] + LocalCenter;
            }

            _mesh = TriangleMesh.Create(points, triangles);
            return body.CreateMeshShape(BuildDef(), _mesh, scale);
        }

        internal override void ReleaseGeometry()
        {
            if (_mesh.IsCreated) _mesh.Destroy();
        }

        private void OnDrawGizmosSelected()
        {
            if (!Mesh) return;
            Gizmos.color = new Color(0.5f, 0.9f, 0.6f, 0.9f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(Mesh, (Vector3)LocalCenter);
        }
    }
}
