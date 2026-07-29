using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>Dents this GameObject's mesh where physics impacts land: vertices near the impact
    /// point are pushed along the impact direction, deepest at the point and fading to zero at
    /// <see cref="Radius"/>, scaled by how hard the hit was. Works next to any Box3D shape — the
    /// deformation is visual only, so the collision geometry (and determinism) is untouched. The
    /// mesh is deformed on a private copy; the shared asset is never modified. Needs a readable
    /// mesh (Read/Write enabled) and a <see cref="Box3DBody"/> on this object or a parent —
    /// Static bodies work, so walls and floors can dent too. Dents are permanent unless
    /// <see cref="RecoverySpeed"/> heals them back.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DDeformable.png")]
    [AddComponentMenu("Box3D/Deformable")]
    [RequireComponent(typeof(MeshFilter))]
    [DisallowMultipleComponent]
    public class Box3DDeformable : MonoBehaviour, IBox3DHitReceiver
    {
        [SerializeField, Min(0.01f), Tooltip("Radius in meters around the impact point that gets dented.")]
        private float Radius = 0.3f;

        [SerializeField, Min(0f), Tooltip("Dent depth in meters per m/s of impact speed (0.02 dents 10 cm on a 5 m/s hit).")]
        private float Strength = 0.02f;

        [SerializeField, Min(0f), Tooltip("Hard cap on how far any vertex can move from its original position, in meters — repeated hits pile up only this far.")]
        private float MaxDepth = 0.2f;

        [SerializeField, Min(0f), Tooltip("Impacts slower than this m/s don't dent. (The world already skips hit events below its own ~1 m/s threshold.)")]
        private float MinImpactSpeed = 1.5f;

        [SerializeField, Min(0f), Tooltip("Dents heal back at this speed in m/s (0 = permanent).")]
        private float RecoverySpeed;

        [SerializeField, Tooltip("Rebuild hull/mesh collision on this GameObject from the dented mesh. Hulls stay convex — dents register only where they flatten corners or edges; mesh shapes (static) take true craters. Box, sphere and capsule shapes can't change. Makes recorded replays diverge.")]
        private bool UpdateCollision;

        [SerializeField, Min(0f), Tooltip("Minimum seconds between collision rebuilds — impacts burst in clusters, one rebuild covers them all.")]
        private float RebuildCooldown = 0.25f;

        private Mesh _mesh;
        private Vector3[] _original;
        private Vector3[] _vertices;
        private bool _dirty;
        private bool _deformed;
        private Box3DShape[] _rebuildTargets;
        private int[] _triangles;
        private float _lastRebuildTime;
        private bool _collisionDirty;
        private bool _warnedNoRebuildTarget;

        /// <summary>True while any vertex is displaced from the original mesh.</summary>
        public bool IsDeformed => _deformed;

        private void Awake()
        {
            var filter = GetComponent<MeshFilter>();
            Mesh source = filter ? filter.sharedMesh : null;
            if (!source)
            {
                Debug.LogError($"[Box3D] {name}: Box3DDeformable needs a MeshFilter with a mesh.", this);
                enabled = false;
                return;
            }
            if (!source.isReadable)
            {
                Debug.LogError($"[Box3D] {name}: mesh '{source.name}' is not readable — enable " +
                    "Read/Write in its import settings to deform it.", this);
                enabled = false;
                return;
            }

            // Deform a private copy; the shared mesh asset must survive untouched.
            _mesh = Instantiate(source);
            _mesh.name = source.name + " (deforming)";
            _mesh.MarkDynamic();
            filter.sharedMesh = _mesh;
            _original = source.vertices;
            _vertices = _mesh.vertices;
            _rebuildTargets = GetComponents<Box3DShape>();
        }

        private void OnDestroy()
        {
            if (_mesh) Destroy(_mesh);
        }

#if UNITY_EDITOR
        // Same hierarchy-aware auto-body as shapes: impacts only reach receivers under a Box3DBody.
        private void Reset()
        {
            if (!GetComponentInParent<Box3DBody>(true))
            {
                UnityEditor.Undo.AddComponent<Box3DBody>(gameObject);
            }
        }
#endif

        void IBox3DHitReceiver.OnBox3DHit(in Box3DHit hit)
        {
            if (hit.ApproachSpeed < MinImpactSpeed) return;
            Dent(hit.Point, hit.Direction, hit.ApproachSpeed);
        }

        /// <summary>Dents the mesh as if hit at <paramref name="point"/> (world space) along
        /// <paramref name="direction"/> at <paramref name="speed"/> m/s — for scripted damage
        /// (and the Inspector's Test Dent button).</summary>
        public void Dent(Vector3 point, Vector3 direction, float speed)
        {
            if (_mesh == null) return;
            float depth = Mathf.Min(Strength * speed, MaxDepth);
            if (depth <= 0f) return;
            Vector3 push = direction.normalized * depth;

            // Work in world space so Radius and depth stay meters under any (even non-uniform) scale.
            Matrix4x4 toWorld = transform.localToWorldMatrix;
            Matrix4x4 toLocal = transform.worldToLocalMatrix;
            float radiusSq = Radius * Radius;
            float maxDepthSq = MaxDepth * MaxDepth;

            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector3 world = toWorld.MultiplyPoint3x4(_vertices[i]);
                float distSq = (world - point).sqrMagnitude;
                if (distSq >= radiusSq) continue;

                // Smooth dome falloff: full depth at the impact point, zero at the rim. The total
                // offset from the original vertex is clamped so pile-up stops at MaxDepth.
                Vector3 originalWorld = toWorld.MultiplyPoint3x4(_original[i]);
                Vector3 offset = world + push * (1f - distSq / radiusSq) - originalWorld;
                if (offset.sqrMagnitude > maxDepthSq) offset = offset.normalized * MaxDepth;
                _vertices[i] = toLocal.MultiplyPoint3x4(originalWorld + offset);
                _dirty = true;
            }
            _deformed |= _dirty;
        }

        /// <summary>Dents a random point on the surface, aimed at the mesh center — the
        /// Inspector's Test Dent button. Play mode only (the deforming mesh copy exists then).</summary>
        public void TestDent()
        {
            if (_mesh == null || _original.Length == 0) return;
            Vector3 point = transform.localToWorldMatrix.MultiplyPoint3x4(_original[Random.Range(0, _original.Length)]);
            Vector3 direction = transform.localToWorldMatrix.MultiplyPoint3x4(_mesh.bounds.center) - point;
            if (direction.sqrMagnitude < 1e-6f) direction = Vector3.down;
            Dent(point, direction, Mathf.Max(MinImpactSpeed * 2f, 3f));
        }

        /// <summary>Restores the undeformed mesh immediately.</summary>
        public void ResetDeformation()
        {
            if (_mesh == null) return;
            System.Array.Copy(_original, _vertices, _original.Length);
            _deformed = false;
            Apply();
        }

        private void Update()
        {
            if (_deformed && RecoverySpeed > 0f) Recover(Time.deltaTime);
            if (_dirty) Apply();
            if (_collisionDirty && Time.time - _lastRebuildTime >= RebuildCooldown) RebuildCollision();
        }

        private void Recover(float deltaTime)
        {
            // RecoverySpeed is world m/s; approximate local units under the object's average scale.
            Vector3 s = transform.lossyScale;
            float scale = (Mathf.Abs(s.x) + Mathf.Abs(s.y) + Mathf.Abs(s.z)) / 3f;
            float step = RecoverySpeed * deltaTime / Mathf.Max(scale, 1e-4f);

            bool settled = true;
            for (int i = 0; i < _vertices.Length; i++)
            {
                if (_vertices[i] == _original[i]) continue;
                _vertices[i] = Vector3.MoveTowards(_vertices[i], _original[i], step);
                if (_vertices[i] != _original[i]) settled = false;
                _dirty = true;
            }
            if (settled) _deformed = false;
        }

        private void Apply()
        {
            _mesh.vertices = _vertices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _dirty = false;
            if (UpdateCollision) _collisionDirty = true;
        }

        // Pushes the dented vertices into this GameObject's hull/mesh shapes (see UpdateCollision).
        // Runs from Update, outside the physics step, at most once per RebuildCooldown.
        private void RebuildCollision()
        {
            _collisionDirty = false;
            _lastRebuildTime = Time.time;
            _triangles ??= _mesh.triangles;

            bool rebuilt = false;
            foreach (Box3DShape shape in _rebuildTargets)
            {
                if (shape) rebuilt |= shape.TryRebuildGeometry(_vertices, _triangles);
            }
            if (!rebuilt && !_warnedNoRebuildTarget)
            {
                _warnedNoRebuildTarget = true;
                Debug.LogWarning($"[Box3D] {name}: Update Collision couldn't rebuild any collision " +
                    "shape on this GameObject — it needs a Hull or Mesh shape here (box/sphere/capsule " +
                    "collision can't take dents).", this);
            }
        }

        // Matches the deformable icon color.
        private static readonly Color GizmoColor = new Color(0.95f, 0.54f, 0.38f, 0.9f);

        private void OnDrawGizmosSelected()
        {
            // The dent footprint, drawn at the object center for scale — the actual dent forms
            // around wherever an impact lands.
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
