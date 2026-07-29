using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A height-field shape built from a Unity Terrain, analogous to TerrainCollider.
    /// STATIC BODIES ONLY. The height-field data is referenced by the shape, so this component
    /// keeps it alive until the body is destroyed.
    /// The field's corner sits at the body origin (box3d height fields carry no local transform),
    /// so the body must be at the terrain's position — automatic when the terrain has no
    /// <see cref="Box3DBody"/> above it, or when the body lives on the Terrain GameObject itself.
    /// Like Unity's terrain renderer, transform rotation and scale are ignored.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DMeshShape.png")]
    [AddComponentMenu("Box3D/Shapes/Terrain Shape")]
    public class Box3DTerrainShape : Box3DShape
    {
        [SerializeField, Tooltip("Terrain to collide against. Defaults to the Terrain on this GameObject.")]
        private Terrain Terrain;

        [SerializeField, Min(1), Tooltip("Heightmap downsampling: 1 = every sample, 2/4/8… = coarser, cheaper " +
            "collision. Rounded to a power of two (heightmap resolutions are 2^n + 1).")]
        private int SampleStride = 1;

        [SerializeField, Tooltip("Carve painted terrain holes out of the collision. With a stride above 1, " +
            "a coarse quad is carved only when every heightmap cell it covers is a hole.")]
        private bool ApplyHoles = true;

        private HeightField _field;

        // Live height-field shapes by id, so systems that meet one through a physics query can
        // get back to the component and its sampled grid (Box3DWater mirrors it to the GPU for
        // exact terrain collision instead of the bounding-box approximation).
        private static readonly Dictionary<ShapeId, Box3DTerrainShape> Registry =
            new Dictionary<ShapeId, Box3DTerrainShape>();

        private float[] _heights;   // normalized [0, 1] samples, row-major z * SampleCountX + x
        private int _countX;
        private int _countZ;
        private float3 _fieldScale;
        private float3 _fieldOrigin;
        private ShapeId _registeredId;

        /// <summary>Maps a height-field shape id from a query back to its live component, or false
        /// for fields that were created straight through the API.</summary>
        internal static bool TryGetLiveField(ShapeId id, out Box3DTerrainShape terrain)
        {
            if (Registry.TryGetValue(id, out terrain) && terrain && terrain._heights != null) return true;
            terrain = null;
            return false;
        }

        /// <summary>The collision height grid (normalized [0, 1], row-major z * SampleCountX + x),
        /// as sampled from the terrain — stride and holes applied. Null until the shape exists.</summary>
        internal float[] SampledHeights => _heights;

        internal int SampleCountX => _countX;
        internal int SampleCountZ => _countZ;

        /// <summary>World size of one grid cell in x/z; y is the world height the normalized
        /// samples scale by.</summary>
        internal float3 FieldScale => _fieldScale;

        /// <summary>World corner the grid spans +X/+Z from (the body origin).</summary>
        internal float3 FieldOrigin => _fieldOrigin;

        /// <summary>Sets the source terrain. Must be set before the body creates the shape (Awake).</summary>
        public void SetTerrain(Terrain terrain)
        {
            Terrain = terrain;
        }

        protected override Shape CreateShape(Body body, float3 localPosition, quaternion localRotation, float3 scale)
        {
            Terrain terrain = Terrain ? Terrain : GetComponent<Terrain>();
            if (!terrain || !terrain.terrainData)
            {
                Debug.LogError($"[Box3D] {name}: Box3DTerrainShape has no Terrain (assign one or add the " +
                               "component to a Terrain GameObject).", this);
                return default;
            }
            if (body.GetBodyType() != Box3D.BodyType.Static)
            {
                Debug.LogWarning($"[Box3D] {name}: terrain shapes only work on Static bodies.", this);
            }

            // Height fields carry no shape-local transform, so a non-identity frame (a compound
            // child offset from its body) would land the collision in the wrong place. Refuse
            // rather than collide somewhere the terrain isn't.
            bool identityFrame = math.lengthsq(localPosition) < 1e-8f &&
                                 math.abs(math.dot(localRotation, quaternion.identity)) > 0.99999f;
            if (!identityFrame)
            {
                Debug.LogError($"[Box3D] {name}: terrain shapes must sit at their body's origin — put the " +
                               "Box3DBody on the Terrain GameObject (or give the terrain no body at all).", this);
                return default;
            }
            if (math.lengthsq(LocalCenter) > 0f)
            {
                Debug.LogWarning($"[Box3D] {name}: Center is ignored on terrain shapes.", this);
            }

            TerrainData data = terrain.terrainData;
            int res = data.heightmapResolution;
            int stride = Mathf.Clamp(Mathf.ClosestPowerOfTwo(SampleStride), 1, math.max(1, res - 1));
            int nx = (res - 1) / stride + 1;
            int nz = nx;

            // Unity heights come normalized [z, x] with x fastest — exactly box3d's
            // z * countX + x order. Kept normalized; data.size.y rides in scale.y, so the
            // 16-bit quantization spans the terrain's own height range.
            float[,] raw = data.GetHeights(0, 0, res, res);
            var heights = new float[nx * nz];
            for (int z = 0; z < nz; z++)
            {
                int rz = z * stride;
                for (int x = 0; x < nx; x++)
                {
                    heights[z * nx + x] = raw[rz, x * stride];
                }
            }

            float3 fieldScale = new float3(
                data.size.x / (nx - 1),
                data.size.y,
                data.size.z / (nz - 1));

            _field = HeightField.Create(heights, nx, nz, fieldScale,
                globalMinimumHeight: 0f, globalMaximumHeight: 1f,
                clockwiseWinding: false, // counter-clockwise = upward-facing surface
                materialIndices: ApplyHoles ? BuildHoleQuads(data, nx, nz, stride) : null);
            Shape shape = body.CreateHeightFieldShape(BuildDef(), _field);
            if (shape.IsValid)
            {
                _heights = heights;
                _countX = nx;
                _countZ = nz;
                _fieldScale = fieldScale;
                _fieldOrigin = body.GetTransform().Position;
                _registeredId = shape.Id;
                Registry[_registeredId] = this;
            }
            return shape;
        }

        // Per-quad material indices carving painted holes, or null when the terrain has none.
        private static byte[] BuildHoleQuads(TerrainData data, int nx, int nz, int stride)
        {
            int hr = data.holesResolution; // heightmapResolution - 1: one sample per quad
            bool[,] surface = data.GetHoles(0, 0, hr, hr); // [z, x], false = hole
            byte[] quads = null;
            for (int qz = 0; qz < nz - 1; qz++)
            {
                for (int qx = 0; qx < nx - 1; qx++)
                {
                    // A coarse quad becomes a hole only if every fine cell under it is one —
                    // collision is kept wherever any ground is still rendered.
                    bool allHoles = true;
                    int bx = math.min(qx * stride, hr - 1), bz = math.min(qz * stride, hr - 1);
                    int sx = math.min(stride, hr - bx), sz = math.min(stride, hr - bz);
                    for (int z = 0; z < sz && allHoles; z++)
                    {
                        for (int x = 0; x < sx; x++)
                        {
                            if (surface[bz + z, bx + x]) { allHoles = false; break; }
                        }
                    }
                    if (!allHoles) continue;

                    quads ??= new byte[(nx - 1) * (nz - 1)];
                    quads[qz * (nx - 1) + qx] = HeightField.HoleMaterial;
                }
            }
            return quads;
        }

        internal override void ReleaseGeometry()
        {
            Registry.Remove(_registeredId);
            _registeredId = default;
            _heights = null;
            if (_field.IsCreated) _field.Destroy();
        }

#if UNITY_EDITOR
        // Detached preview shapes stash their HeightField in _field like a live one — free it
        // the same way once the preview world is gone.
        internal override void ReleaseDetachedGeometry() => ReleaseGeometry();
#endif

        private void OnDrawGizmosSelected()
        {
            Terrain terrain = Terrain ? Terrain : GetComponent<Terrain>();
            if (!terrain || !terrain.terrainData) return;

            TerrainData data = terrain.terrainData;
            Gizmos.color = new Color(0.5f, 0.9f, 0.6f, 0.9f);
            Gizmos.matrix = Matrix4x4.Translate(terrain.GetPosition());

            // A coarse height-following grid — the full field would be millions of lines.
            const int Lines = 16;
            Vector3 size = data.size;
            for (int i = 0; i <= Lines; i++)
            {
                float t = (float)i / Lines;
                Vector3 prevX = default, prevZ = default;
                for (int j = 0; j <= Lines; j++)
                {
                    float s = (float)j / Lines;
                    var alongX = new Vector3(s * size.x, data.GetInterpolatedHeight(s, t), t * size.z);
                    var alongZ = new Vector3(t * size.x, data.GetInterpolatedHeight(t, s), s * size.z);
                    if (j > 0)
                    {
                        Gizmos.DrawLine(prevX, alongX);
                        Gizmos.DrawLine(prevZ, alongZ);
                    }
                    prevX = alongX;
                    prevZ = alongZ;
                }
            }
        }
    }
}
