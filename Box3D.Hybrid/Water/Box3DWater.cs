using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Box3D.Hybrid
{
    /// <summary>Particle-based water simulated on the GPU and rendered as a screen-space liquid
    /// surface. The box volume fills with fluid particles on play; they collide with every Box3D
    /// shape near the water bounds (spheres, capsules, boxes and <see cref="Box3DTerrainShape"/>
    /// terrains exactly, other complex shapes as their bounding box) and push dynamic bodies back,
    /// so props float, bob and get carried by the flow. Needs compute-shader support and a URP
    /// camera with Depth Texture on (enable Opaque Texture too for refraction).</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DWater.png")]
    [AddComponentMenu("Box3D/Water")]
    [DefaultExecutionOrder(-50)] // after Box3DWorld (-100): fresh transforms in, forces land next step
    public class Box3DWater : MonoBehaviour
    {
        [Header("Fluid")]
        [SerializeField, Range(0.04f, 0.5f), Tooltip("Particle radius in meters. Smaller particles = finer, more detailed water that costs more to simulate. Editing this in play mode re-seeds the water at the new size.")]
        private float ParticleRadius = 0.12f;

        [SerializeField, Range(1024, 65536), Tooltip("Particle pool size. The volume fill and any splash emitters share this budget; oldest particles are recycled when it runs out.")]
        private int MaxParticles = 16384;

        [SerializeField, Range(0f, 1f), Tooltip("How syrupy the water feels. 0 = lively and splashy, 1 = thick and slow.")]
        private float Viscosity = 0.08f;

        [SerializeField, Range(0f, 0.1f), Tooltip("Surface-tension feel: how much particles cling together into droplets and sheets.")]
        private float Cohesion = 0.02f;

        [SerializeField, Range(0f, 2f), Tooltip("Multiplier on the world's gravity for the fluid only.")]
        private float GravityScale = 1f;

        [Header("Volume")]
        [SerializeField, Tooltip("Fill the volume with water when play starts.")]
        private bool FillOnStart = true;

        [SerializeField, Tooltip("Water fill region in local units, centered on this object (rotates with it).")]
        private Vector3 VolumeSize = new Vector3(4f, 2f, 4f);

        [SerializeField, Tooltip("Simulation bounds around this object's position (world-axis-aligned). With Contain on these are solid walls; otherwise water escaping below them is removed.")]
        private Vector3 BoundsSize = new Vector3(8f, 6f, 8f);

        [SerializeField, Tooltip("Keep the water inside the bounds like a tank. Turn off for open water that drains away (spilled particles are recycled by new splashes).")]
        private bool Contain = true;

        [Header("Interaction")]
        [SerializeField, Tooltip("Layers of Box3D shapes the water collides with.")]
        private LayerMask CollideWith = ~0;

        [SerializeField, Tooltip("Push dynamic bodies back: objects float, bob and get shoved by waves. Costs one small GPU readback per step.")]
        private bool TwoWayCoupling = true;

        [SerializeField, Range(0f, 1f), Tooltip("Strength of the push the water gives dynamic bodies. Raise it if props sink, lower it if they get launched.")]
        private float CouplingStrength = 0.2f;

        [SerializeField, Range(0f, 10f), Tooltip("Water resistance on submerged bodies — higher makes them settle into a calm float sooner.")]
        private float BodyDrag = 2f;

        [SerializeField, Tooltip("Let water land on the top of mesh, compound and component-less height-field shapes' bounding boxes (exact support: spheres, capsules, boxes, box hulls and Box3DTerrainShape terrains). Only the top face acts — the box of a sprawling mesh is far bigger than its geometry, and solid sides would dam water at invisible walls.")]
        private bool ApproximateComplexShapes = true;

        [Header("Solver")]
        [SerializeField, Range(1, 8), Tooltip("Density-constraint iterations per substep. More = stiffer, less compressible water.")]
        private int SolverIterations = 3;

        [SerializeField, Range(1, 4), Tooltip("Simulation substeps per physics step. More = stabler fast splashes, linearly more GPU cost.")]
        private int Substeps = 2;

        [SerializeField, Range(0f, 1f), Tooltip("Extra velocity damping per second. A little keeps a tank from sloshing forever.")]
        private float Damping = 0.05f;

        [Header("Rendering")]
        [SerializeField, Tooltip("Draw the liquid surface. Turn off to use the particle buffer with your own renderer.")]
        private bool RenderSurface = true;

        [SerializeField, Tooltip("Water color — what white light fades toward as it travels through the fluid.")]
        private Color WaterColor = new Color(0.09f, 0.34f, 0.42f, 1f);

        [SerializeField, Range(0f, 6f), Tooltip("How quickly the color saturates with depth. Low = clear shallows, high = murky.")]
        private float Absorption = 1.2f;

        [SerializeField, Range(0f, 2f), Tooltip("How strongly the surface bends the scene behind it. Needs Opaque Texture in the URP asset.")]
        private float Refraction = 0.6f;

        [SerializeField, Range(0f, 1f), Tooltip("Sky/reflection-probe mirror strength at grazing angles.")]
        private float Reflection = 0.9f;

        [SerializeField, Range(0f, 2f), Tooltip("Surface smoothing. 0 shows the raw particle blobs; higher melts them into one continuous sheet.")]
        private float Smoothing = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("Whitewater strength. Fast, aerated water (splashes, impacts, waterfalls) churns into foam, plus a foam rim where objects pierce the surface.")]
        private float Foam = 0.5f;

        [SerializeField, Range(0f, 2f), Tooltip("How much fast, foamy droplets stretch into streaks along their motion. 0 keeps every droplet round; higher makes whitewater read as flying spray.")]
        private float FoamStretch = 1f;

        [SerializeField, Range(0.01f, 0.5f), Tooltip("How softly the water blends into objects and shores it touches, in meters. Small = crisp waterline, large = misty fade.")]
        private float ShoreBlend = 0.12f;

        [SerializeField, Range(1f, 3f), Tooltip("Visual size of each particle relative to its physical radius. Larger overlaps neighbours into a closed surface.")]
        private float ParticleRenderScale = 1.8f;

        [SerializeField, Range(0.3f, 1f), Tooltip("Resolution of the fluid buffers relative to the camera. Lower is faster and slightly softer.")]
        private float RenderScale = 1f;

        // ------------------------------------------------------------------ solver constants
        internal const int HashSize = 131072;      // must match Box3DWaterSim.compute
        private const int Threads = 128;
        private const int MaxColliders = 256;
        private const int MaxBodies = 128;
        private const int MaxWinds = 8;            // wind volumes per step, 4 float4s each — must match _WindData in the compute
        private const float ImpulseScale = 256f;   // fixed-point scale used by the compute kernels
        private const float RestDensity = 1000f;

        private Box3DWorld _world;
        private ComputeShader _compute;
        private Box3DWaterRenderer _renderer;

        private ComputeBuffer _positions;
        private ComputeBuffer _velocities;
        private ComputeBuffer _predicted;
        private ComputeBuffer _scratch;
        private ComputeBuffer _lambdas;
        private ComputeBuffer _densities;
        private ComputeBuffer _gridHead;
        private ComputeBuffer _gridNext;
        private ComputeBuffer _colliders;
        private ComputeBuffer _bodyImpulses;
        private ComputeBuffer _terrainHeights;

        private int _kClearGrid, _kPredict, _kBuildGrid, _kLambda, _kDelta, _kApply, _kUpdateVel, _kFinalize;

        private float _lastRadius;  // detects Inspector radius edits mid-play (see OnValidate)
        private int _capacity;      // buffer size, frozen at creation (MaxParticles edits mid-play wait for re-enable)
        private int _activeRange;   // highest particle slot in use + 1 (dispatch range)
        private int _spawnCursor;   // ring cursor for recycling the pool
        private float4[] _spawnPositions;
        private float4[] _spawnVelocities;
        private readonly int[] _impulseClear = new int[MaxBodies * 8];

        // Collider mirroring (layout must match WaterCollider in the compute shader).
        private struct GpuCollider
        {
            public float4 Rot, Pos, Data, VelLin, VelAng, BodyPos;
            public int Type, BodyIndex, Aux0, Aux1; // aux: terrain heights offset / samples per row
        }

        private readonly ShapeId[] _overlap = new ShapeId[1024];
        private readonly GpuCollider[] _colliderStage = new GpuCollider[MaxColliders];

        // Terrain height grids the water has met, packed back to back into _terrainHeights;
        // the value is each grid's first slot in the buffer.
        private readonly Dictionary<Box3DTerrainShape, int> _terrainSlots = new Dictionary<Box3DTerrainShape, int>();
        private readonly Dictionary<Body, int> _bodySlots = new Dictionary<Body, int>();
        private Body[] _bodyStage = new Body[MaxBodies];
        private int _colliderCount;
        private int _bodyCount;

        private sealed class PendingReadback
        {
            public AsyncGPUReadbackRequest Request;
            public readonly Body[] Bodies = new Body[MaxBodies];
            public int BodyCount;
            public float DeltaTime;
        }

        private readonly Queue<PendingReadback> _pending = new Queue<PendingReadback>();
        private readonly Stack<PendingReadback> _readbackPool = new Stack<PendingReadback>();

        /// <summary>The live particle buffer (float4: xyz world position, w = 1 alive / 0 dead),
        /// for custom renderers or VFX Graph binding. Null until the simulation starts.</summary>
        public ComputeBuffer ParticleBuffer => _positions;

        /// <summary>The particle slots currently in use (upper bound on alive particles).</summary>
        public int ActiveParticleRange => _activeRange;

        /// <summary>The live velocity buffer (float4: xyz velocity, w = foam amount in [0, 1]),
        /// for custom renderers. Null until the simulation starts.</summary>
        public ComputeBuffer VelocityBuffer => _velocities;

        /// <summary>Radius of one fluid particle in meters.</summary>
        public float Radius => ParticleRadius;

        private float SmoothingRadius => ParticleRadius * 4f;
        private float ParticleMass => RestDensity * 8f * ParticleRadius * ParticleRadius * ParticleRadius;

        internal float RenderRadius => ParticleRadius * ParticleRenderScale;
        internal float SurfaceSmoothing => Smoothing;
        internal float RenderResolutionScale => RenderScale;
        internal Color SurfaceColor => WaterColor;
        internal float SurfaceAbsorption => Absorption;
        internal float SurfaceRefraction => Refraction;
        internal float SurfaceReflection => Reflection;
        internal float SurfaceFoam => Foam;
        internal float SurfaceFoamStretch => FoamStretch;
        internal float SurfaceShoreBlend => ShoreBlend;
        internal float SmoothingWorldRadius => SmoothingRadius;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogWarning("Box3DWater: compute shaders are not supported on this device; water is disabled.", this);
                enabled = false;
                return;
            }

            _world = Box3DWorld.Instance;
            ComputeShader source = Resources.Load<ComputeShader>("Box3D/Box3DWaterSim");
            if (!source)
            {
                Debug.LogWarning("Box3DWater: simulation compute shader failed to load from package resources; water is disabled.", this);
                enabled = false;
                return;
            }
            _compute = Instantiate(source); // per-instance copy: uniform state must not leak between waters

            _kClearGrid = _compute.FindKernel("ClearGrid");
            _kPredict = _compute.FindKernel("Predict");
            _kBuildGrid = _compute.FindKernel("BuildGrid");
            _kLambda = _compute.FindKernel("SolveLambda");
            _kDelta = _compute.FindKernel("SolveDelta");
            _kApply = _compute.FindKernel("ApplyDelta");
            _kUpdateVel = _compute.FindKernel("UpdateVelocity");
            _kFinalize = _compute.FindKernel("Finalize");

            CreateBuffers();
            _lastRadius = ParticleRadius;
            if (FillOnStart) Fill();

            if (RenderSurface)
            {
                _renderer = new Box3DWaterRenderer(this);
                _renderer.Enable();
            }
        }

        private void OnDisable()
        {
            _renderer?.Dispose();
            _renderer = null;
            ReleaseBuffers();
            if (_compute) Destroy(_compute);
            _compute = null;
            _pending.Clear();
        }

        private void CreateBuffers()
        {
            _capacity = MaxParticles;
            _positions = new ComputeBuffer(_capacity, 16);
            _velocities = new ComputeBuffer(_capacity, 16);
            _predicted = new ComputeBuffer(_capacity, 16);
            _scratch = new ComputeBuffer(_capacity, 16);
            _lambdas = new ComputeBuffer(_capacity, 4);
            _densities = new ComputeBuffer(_capacity, 4);
            _gridHead = new ComputeBuffer(HashSize, 4);
            _gridNext = new ComputeBuffer(_capacity, 4);
            _colliders = new ComputeBuffer(MaxColliders, 112);
            _bodyImpulses = new ComputeBuffer(MaxBodies * 8, 4);
            BindTerrainHeights(1); // placeholder until a terrain is met — the kernel needs a valid binding

            _spawnPositions = new float4[_capacity];
            _spawnVelocities = new float4[_capacity];
            _positions.SetData(_spawnPositions); // all slots dead
            _velocities.SetData(_spawnVelocities);

            foreach (int kernel in new[] { _kClearGrid, _kPredict, _kBuildGrid, _kLambda, _kDelta, _kApply, _kUpdateVel, _kFinalize })
            {
                _compute.SetBuffer(kernel, "_Positions", _positions);
                _compute.SetBuffer(kernel, "_Velocities", _velocities);
                _compute.SetBuffer(kernel, "_Predicted", _predicted);
                _compute.SetBuffer(kernel, "_Scratch", _scratch);
                _compute.SetBuffer(kernel, "_Lambdas", _lambdas);
                _compute.SetBuffer(kernel, "_Densities", _densities);
                _compute.SetBuffer(kernel, "_GridHead", _gridHead);
                _compute.SetBuffer(kernel, "_GridNext", _gridNext);
                _compute.SetBuffer(kernel, "_Colliders", _colliders);
                _compute.SetBuffer(kernel, "_BodyImpulses", _bodyImpulses);
            }
        }

        private void ReleaseBuffers()
        {
            _positions?.Release(); _positions = null;
            _velocities?.Release(); _velocities = null;
            _predicted?.Release(); _predicted = null;
            _scratch?.Release(); _scratch = null;
            _lambdas?.Release(); _lambdas = null;
            _densities?.Release(); _densities = null;
            _gridHead?.Release(); _gridHead = null;
            _gridNext?.Release(); _gridNext = null;
            _colliders?.Release(); _colliders = null;
            _bodyImpulses?.Release(); _bodyImpulses = null;
            _terrainHeights?.Release(); _terrainHeights = null;
            _terrainSlots.Clear();
            _activeRange = 0;
            _spawnCursor = 0;
        }

        // (Re)creates the terrain heights buffer at the given float count and binds it to the
        // collision kernel (ApplyDelta is the only one that resolves colliders).
        private void BindTerrainHeights(int count)
        {
            _terrainHeights?.Release();
            _terrainHeights = new ComputeBuffer(count, 4);
            _compute.SetBuffer(_kApply, "_TerrainHeights", _terrainHeights);
        }

        // The first slot of this terrain's grid in the heights buffer, uploading it on first
        // sight. Terrains are static and rare, so the repack runs about once per terrain.
        private int TerrainHeightsOffset(Box3DTerrainShape terrain)
        {
            if (_terrainSlots.TryGetValue(terrain, out int known)) return known;

            var terrains = new List<Box3DTerrainShape>();
            foreach (Box3DTerrainShape t in _terrainSlots.Keys)
            {
                if (t && t.SampledHeights != null) terrains.Add(t); // drop destroyed terrains
            }
            terrains.Add(terrain);
            _terrainSlots.Clear();

            int total = 0;
            foreach (Box3DTerrainShape t in terrains) total += t.SampledHeights.Length;
            BindTerrainHeights(total);

            int cursor = 0;
            foreach (Box3DTerrainShape t in terrains)
            {
                _terrainSlots[t] = cursor;
                _terrainHeights.SetData(t.SampledHeights, 0, cursor, t.SampledHeights.Length);
                cursor += t.SampledHeights.Length;
            }
            return _terrainSlots[terrain];
        }

        // ------------------------------------------------------------------ emission

        /// <summary>Clears the pool and refills the volume box with resting water.</summary>
        public void Fill()
        {
            if (_positions == null) return;
            Clear();

            float spacing = ParticleRadius * 2f;
            var half = (float3)VolumeSize * 0.5f;
            var counts = (int3)math.max(new float3(1f), math.floor((float3)VolumeSize / spacing));
            var rng = new Unity.Mathematics.Random(0x9e3779b9);

            int n = 0;
            for (int x = 0; x < counts.x && n < _capacity; x++)
            for (int y = 0; y < counts.y && n < _capacity; y++)
            for (int z = 0; z < counts.z && n < _capacity; z++)
            {
                float3 local = -half + spacing * 0.5f + new float3(x, y, z) * spacing
                               + rng.NextFloat3(-0.02f, 0.02f) * spacing;
                float3 world = transform.TransformPoint((Vector3)local);
                _spawnPositions[n] = new float4(world, 1f);
                _spawnVelocities[n] = float4.zero;
                n++;
            }

            _positions.SetData(_spawnPositions, 0, 0, n);
            _velocities.SetData(_spawnVelocities, 0, 0, n);
            _activeRange = n;
            _spawnCursor = n % _capacity;
        }

        /// <summary>Injects a ball of moving water — a splash, a jet burst, rain. Recycles the
        /// oldest particles when the pool is full.</summary>
        public void SpawnParticles(Vector3 center, float radius, Vector3 velocity, int count)
        {
            if (_positions == null || count <= 0) return;
            count = Mathf.Min(count, _capacity);
            var rng = new Unity.Mathematics.Random((uint)(Time.frameCount * 2654435761u + 1u));

            for (int i = 0; i < count; i++)
            {
                float3 p = (float3)center + rng.NextFloat3Direction() * (rng.NextFloat() * radius);
                _spawnPositions[i] = new float4(p, 1f);
                _spawnVelocities[i] = new float4((float3)velocity, 0f);
            }
            SpawnParticles(_spawnPositions, _spawnVelocities, count);
        }

        /// <summary>Batched raw spawn for custom emitters (see <see cref="Box3DWaterfall"/>):
        /// writes the first <paramref name="count"/> entries into the pool, recycling the oldest
        /// particles when it is full. Positions are float4(world xyz, 1) — the w = 1 marks the
        /// particle alive; velocities are float4(xyz, 0).</summary>
        public void SpawnParticles(float4[] positions, float4[] velocities, int count)
        {
            if (_positions == null || positions == null || velocities == null || count <= 0) return;
            count = Mathf.Min(count, Mathf.Min(_capacity, Mathf.Min(positions.Length, velocities.Length)));

            int written = 0;
            int highestTouched = _activeRange;
            while (written < count)
            {
                int run = Mathf.Min(count - written, _capacity - _spawnCursor);
                _positions.SetData(positions, written, _spawnCursor, run);
                _velocities.SetData(velocities, written, _spawnCursor, run);
                highestTouched = Mathf.Max(highestTouched, _spawnCursor + run);
                _spawnCursor = (_spawnCursor + run) % _capacity;
                written += run;
            }

            _activeRange = Mathf.Min(_capacity, highestTouched);
        }

        /// <summary>Removes all water.</summary>
        public void Clear()
        {
            if (_positions == null) return;
            System.Array.Clear(_spawnPositions, 0, _spawnPositions.Length);
            _positions.SetData(_spawnPositions);
            _activeRange = 0;
            _spawnCursor = 0;
        }

        // ------------------------------------------------------------------ wind

        private readonly Vector4[] _windData = new Vector4[MaxWinds * 4];
        private int _windCount;
        private double _windQueueTime = -1.0; // fixed step the queue belongs to (stale entries reset)

        /// <summary>Queues an oriented wind box for the next fluid step: particles inside get up to
        /// <paramref name="acceleration"/> (m/s²) — full strength on surface, spray and foam, fading
        /// to nothing in the bulk. The queue holds one physics step; call every FixedUpdate while the
        /// wind blows (<see cref="Box3DWind"/> does this for its zone). At most 8 winds per step.</summary>
        public void AddWind(Vector3 center, Quaternion rotation, Vector3 halfExtents, Vector3 acceleration)
        {
            // Winds queue after this water's step (Box3DWater runs early at -50) and are consumed
            // by the next one; a queue left over from an older step has already been consumed.
            double now = Time.fixedTimeAsDouble;
            if (_windQueueTime != now)
            {
                _windQueueTime = now;
                _windCount = 0;
            }

            if (_windCount >= MaxWinds) return;
            int slot = _windCount * 4;
            _windData[slot + 0] = center;
            _windData[slot + 1] = new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
            _windData[slot + 2] = halfExtents;
            _windData[slot + 3] = acceleration;
            _windCount++;
        }

        // ------------------------------------------------------------------ stepping

        private void FixedUpdate()
        {
            if (_positions == null || !_world || _world.Paused || !_world.World.IsValid) return;

            ApplyFinishedReadbacks();
            if (_activeRange == 0) return;

            GatherColliders();

            float dt = Time.fixedDeltaTime / Substeps;
            float h = SmoothingRadius;
            float h2 = h * h;
            float poly6 = 315f / (64f * Mathf.PI * Mathf.Pow(h, 9f));
            float deltaQ = 0.3f * h;
            float wDeltaQ = poly6 * Mathf.Pow(h2 - deltaQ * deltaQ, 3f);

            _compute.SetInt("_ParticleCount", _activeRange);
            _compute.SetInt("_ColliderCount", _colliderCount);
            _compute.SetFloat("_DeltaTime", dt);
            _compute.SetFloat("_InvDeltaTime", 1f / dt);
            _compute.SetVector("_Gravity", (Vector3)_world.GravityVector * GravityScale);
            _compute.SetFloat("_ParticleRadius", ParticleRadius);
            _compute.SetFloat("_SmoothingRadius", h);
            _compute.SetFloat("_RestDensity", RestDensity);
            _compute.SetFloat("_ParticleMass", ParticleMass);
            _compute.SetFloat("_Poly6", poly6);
            _compute.SetFloat("_SpikyGrad", -45f / (Mathf.PI * Mathf.Pow(h, 6f)));
            _compute.SetFloat("_CohesionK", Cohesion);
            _compute.SetFloat("_InvWDeltaQ", 1f / wDeltaQ);
            _compute.SetFloat("_Viscosity", Viscosity);
            _compute.SetFloat("_Damping", Mathf.Exp(-Damping * dt));
            Vector3 boundsCenter = transform.position;
            _compute.SetVector("_BoundsMin", boundsCenter - BoundsSize * 0.5f);
            _compute.SetVector("_BoundsMax", boundsCenter + BoundsSize * 0.5f);
            _compute.SetInt("_Contain", Contain ? 1 : 0);
            _compute.SetFloat("_CouplingScale", TwoWayCoupling ? CouplingStrength : 0f);
            _compute.SetFloat("_MaxSpeed", h / dt);
            _compute.SetFloat("_FoamDecay", Mathf.Exp(-1.1f * dt));
            _compute.SetInt("_WindCount", _windCount);
            if (_windCount > 0) _compute.SetVectorArray("_WindData", _windData);
            _windCount = 0; // consumed — a wind that stopped blowing must not linger

            _bodyImpulses.SetData(_impulseClear);

            int groups = (_activeRange + Threads - 1) / Threads;
            int gridGroups = HashSize / Threads;

            for (int step = 0; step < Substeps; step++)
            {
                _compute.Dispatch(_kPredict, groups, 1, 1);
                _compute.Dispatch(_kClearGrid, gridGroups, 1, 1);
                _compute.Dispatch(_kBuildGrid, groups, 1, 1);

                for (int it = 0; it < SolverIterations; it++)
                {
                    _compute.Dispatch(_kLambda, groups, 1, 1);
                    _compute.Dispatch(_kDelta, groups, 1, 1);
                    _compute.Dispatch(_kApply, groups, 1, 1);
                }

                _compute.Dispatch(_kUpdateVel, groups, 1, 1);
                _compute.Dispatch(_kFinalize, groups, 1, 1);
            }

            if (TwoWayCoupling && _bodyCount > 0) RequestReadback();
        }

        private void GatherColliders()
        {
            var filter = QueryFilter.Default;
            filter.CategoryBits = 1UL << gameObject.layer;
            ulong mask = 0;
            for (int l = 0; l < 32; l++)
            {
                if ((CollideWith.value & (1 << l)) != 0) mask |= 1UL << l;
            }
            filter.MaskBits = mask;

            float margin = SmoothingRadius * 2f;
            float3 center = transform.position;
            var aabb = new B3Aabb
            {
                LowerBound = center - (float3)(BoundsSize * 0.5f) - margin,
                UpperBound = center + (float3)(BoundsSize * 0.5f) + margin,
            };

            int shapeCount = _world.World.OverlapAABB(aabb, filter, _overlap);
            _colliderCount = 0;
            _bodyCount = 0;
            _bodySlots.Clear();

            for (int i = 0; i < shapeCount && _colliderCount < MaxColliders; i++)
            {
                var shape = new Shape(_overlap[i]);
                Body body = shape.GetBody();
                B3Transform tf = body.GetTransform();
                var col = new GpuCollider { Rot = new float4(tf.Rotation.value) };

                switch (shape.GetShapeType())
                {
                    case ShapeType.Sphere:
                    {
                        Sphere s = shape.GetSphere();
                        col.Type = 0;
                        col.Pos = new float4(tf.Position + math.rotate(tf.Rotation, s.Center), 0f);
                        col.Data = new float4(s.Radius, 0f, 0f, 0f);
                        break;
                    }
                    case ShapeType.Capsule:
                    {
                        Capsule c = shape.GetCapsule();
                        col.Type = 1;
                        col.Pos = new float4(tf.Position + math.rotate(tf.Rotation, c.Center1), 0f);
                        col.Data = new float4(tf.Position + math.rotate(tf.Rotation, c.Center2), c.Radius);
                        break;
                    }
                    case ShapeType.Hull:
                    {
                        B3Aabb local = shape.GetHullLocalBounds();
                        float3 mid = (local.LowerBound + local.UpperBound) * 0.5f;
                        col.Type = 2;
                        col.Pos = new float4(tf.Position + math.rotate(tf.Rotation, mid), 0f);
                        col.Data = new float4((local.UpperBound - local.LowerBound) * 0.5f, 0f);
                        break;
                    }
                    case ShapeType.HeightField:
                    {
                        // Terrain collides exactly: the component's sampled grid rides on the GPU
                        // and is bilinearly sampled per particle. Fields with no live
                        // Box3DTerrainShape (created straight through the API) have no grid to
                        // sample and keep the bounding-box treatment below.
                        if (!Box3DTerrainShape.TryGetLiveField(_overlap[i], out Box3DTerrainShape terrain)) goto default;
                        float3 fieldScale = terrain.FieldScale;
                        col.Type = 3;
                        col.Rot = new float4(0f, 0f, 0f, 1f);
                        col.Pos = new float4(terrain.FieldOrigin, 0f);
                        col.Data = new float4(fieldScale, terrain.SampleCountZ);
                        col.Aux0 = TerrainHeightsOffset(terrain);
                        col.Aux1 = terrain.SampleCountX;
                        break;
                    }
                    default:
                    {
                        if (!ApproximateComplexShapes) continue;
                        B3Aabb world = shape.GetAABB();
                        col.Type = 2;
                        col.Rot = new float4(0f, 0f, 0f, 1f);
                        col.Pos = new float4((world.LowerBound + world.UpperBound) * 0.5f, 0f);
                        // w = 1: a world-AABB approximation — only its top face acts on the
                        // water (see the box branch in Box3DWaterSim.compute).
                        col.Data = new float4((world.UpperBound - world.LowerBound) * 0.5f, 1f);
                        break;
                    }
                }

                col.BodyIndex = -1;
                if (TwoWayCoupling && body.GetBodyType() == BodyType.Dynamic)
                {
                    if (!_bodySlots.TryGetValue(body, out int slot))
                    {
                        if (_bodyCount < MaxBodies)
                        {
                            slot = _bodyCount;
                            _bodySlots.Add(body, slot);
                            _bodyStage[_bodyCount++] = body;
                        }
                        else
                        {
                            slot = -1;
                        }
                    }
                    col.BodyIndex = slot;
                    col.VelLin = new float4(body.LinearVelocity, 0f);
                    col.VelAng = new float4(body.AngularVelocity, 0f);
                    col.BodyPos = new float4(body.Position, 0f);
                }

                _colliderStage[_colliderCount++] = col;
            }

            _colliders.SetData(_colliderStage, 0, 0, math.max(_colliderCount, 1));
        }

        // ------------------------------------------------------------------ two-way coupling

        private void RequestReadback()
        {
            PendingReadback pending = _readbackPool.Count > 0 ? _readbackPool.Pop() : new PendingReadback();
            System.Array.Copy(_bodyStage, pending.Bodies, _bodyCount);
            pending.BodyCount = _bodyCount;
            pending.DeltaTime = Time.fixedDeltaTime;
            pending.Request = AsyncGPUReadback.Request(_bodyImpulses);
            _pending.Enqueue(pending);
        }

        private void ApplyFinishedReadbacks()
        {
            while (_pending.Count > 0 && _pending.Peek().Request.done)
            {
                PendingReadback pending = _pending.Dequeue();
                if (!pending.Request.hasError)
                {
                    var data = pending.Request.GetData<int>();
                    for (int i = 0; i < pending.BodyCount; i++)
                    {
                        Body body = pending.Bodies[i];
                        if (!body.IsValid) continue;

                        int baseIndex = i * 8;
                        int contacts = data[baseIndex + 6];
                        if (contacts == 0) continue;

                        var impulse = new float3(data[baseIndex + 0], data[baseIndex + 1], data[baseIndex + 2]) / ImpulseScale;
                        var torque = new float3(data[baseIndex + 3], data[baseIndex + 4], data[baseIndex + 5]) / ImpulseScale;
                        body.ApplyLinearImpulseToCenter(impulse, wake: true);
                        body.ApplyAngularImpulse(torque * 0.5f, wake: true);

                        // Water resistance, ramping up with how deeply the body sits in the fluid.
                        float submerged = Mathf.Clamp01(contacts / 48f);
                        float drag = BodyDrag * submerged * pending.DeltaTime;
                        body.ApplyLinearImpulseToCenter(-body.LinearVelocity * (body.GetMass() * drag), wake: false);
                        body.ApplyAngularImpulse(-body.AngularVelocity * (body.GetMass() * drag * 0.2f), wake: false);
                    }
                }
                _readbackPool.Push(pending);
            }
        }

#if UNITY_EDITOR
        // The radius rescales the fluid's whole rest configuration (kernel reach, mass, particle
        // spacing), so existing particles would violently re-relax to the new parameters. When it
        // is tuned mid-play, re-seed calm water at the new spacing instead of letting it detonate.
        private void OnValidate()
        {
            if (!Application.isPlaying || _positions == null) return;
            if (Mathf.Approximately(ParticleRadius, _lastRadius)) return;

            _lastRadius = ParticleRadius;
            if (FillOnStart) Fill();
            else Clear(); // hand-emitted water can't be re-seeded meaningfully — respawn it
        }
#endif

        // ------------------------------------------------------------------ gizmos

        private static readonly Color VolumeColor = new Color(0.35f, 0.75f, 0.95f, 0.9f);
        private static readonly Color BoundsColor = new Color(0.35f, 0.55f, 0.95f, 0.5f);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = VolumeColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, VolumeSize);
            Gizmos.color = new Color(VolumeColor.r, VolumeColor.g, VolumeColor.b, 0.1f);
            Gizmos.DrawCube(Vector3.zero, VolumeSize);

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = BoundsColor;
            Gizmos.DrawWireCube(transform.position, BoundsSize);
        }
    }
}
