using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A triangle mesh shape from a mesh asset, analogous to a non-convex MeshCollider.
    /// STATIC BODIES ONLY (box3d mesh shapes don't move). The mesh data is referenced by the shape,
    /// so this component keeps it alive until the body is destroyed.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DMeshShape.png")]
    [AddComponentMenu("Box3D/Shapes/Mesh Shape")]
    public class Box3DMeshShape : Box3DShape
    {
        [SerializeField, Tooltip("Mesh to collide against. Must be Read/Write enabled.")]
        private Mesh Mesh;

        private TriangleMesh _mesh;

        /// <summary>Sets the source mesh. Must be set before the body creates the shape (Awake).</summary>
        public void SetMesh(Mesh mesh)
        {
            Mesh = mesh;
        }

        protected override Shape CreateShape(Body body, float3 localPosition, quaternion localRotation, float3 scale)
        {
            if (!Mesh)
            {
                Debug.LogError($"[Box3D] {name}: Box3DMeshShape has no mesh assigned.", this);
                return default;
            }
            if (body.GetBodyType() != Box3D.BodyType.Static)
            {
                Debug.LogWarning($"[Box3D] {name}: mesh shapes only work on Static bodies; " +
                                 "use Box3DHullShape (convex) for dynamic bodies.", this);
            }

            Vector3[] vertices = Mesh.vertices;
            int[] triangles = Mesh.triangles;
            if (vertices.Length < 3 || triangles.Length < 3)
            {
                Debug.LogError($"[Box3D] {name}: mesh '{Mesh.name}' has no readable geometry " +
                               "(is Read/Write enabled?).", this);
                return default;
            }

            // Bake the local transform and scale into the vertices (mesh creation takes no
            // transform), then create with unit scale.
            var points = new float3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                points[i] = localPosition + math.mul(localRotation, ((float3)vertices[i] + LocalCenter) * scale);
            }

            _mesh = TriangleMesh.Create(points, triangles);
            return body.CreateMeshShape(BuildDef(), _mesh);
        }

        // True crater collision from the dented vertices, reusing the visual mesh's topology.
        // The old TriangleMesh is referenced by the old shape, so it is stashed here and freed
        // in ReleaseRebuiltGeometry once the base has destroyed that shape.
        private TriangleMesh _preRebuildMesh;

        internal override Shape CreateRebuiltShape(Vector3[] vertices, int[] triangles)
        {
            if (vertices.Length < 3 || triangles == null || triangles.Length < 3) return default;

            var points = new float3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                points[i] = AttachedPosition + math.mul(AttachedRotation, ((float3)vertices[i] + LocalCenter) * AttachedScale);
            }

            TriangleMesh rebuilt = TriangleMesh.Create(points, triangles);
            if (!rebuilt.IsCreated) return default;

            Shape shape = AttachedBody.CreateMeshShape(BuildDef(), rebuilt);
            if (!shape.IsValid)
            {
                rebuilt.Destroy();
                return default;
            }
            _preRebuildMesh = _mesh;
            _mesh = rebuilt;
            return shape;
        }

        internal override void ReleaseRebuiltGeometry()
        {
            if (_preRebuildMesh.IsCreated) _preRebuildMesh.Destroy();
            _preRebuildMesh = default;
        }

        internal override void ReleaseGeometry()
        {
            if (_mesh.IsCreated) _mesh.Destroy();
        }

#if UNITY_EDITOR
        // The detached preview shape stored its TriangleMesh in _mesh like a live one — free it
        // the same way once the preview world is gone.
        internal override void ReleaseDetachedGeometry() => ReleaseGeometry();
#endif

        private void OnDrawGizmosSelected()
        {
            if (!Mesh) return;
            Gizmos.color = new Color(0.5f, 0.9f, 0.6f, 0.9f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(Mesh, (Vector3)LocalCenter);
        }
    }
}
