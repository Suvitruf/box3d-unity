using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A buoyancy volume — game water. Dynamic bodies inside the box zone get an
    /// Archimedes buoyancy force (water density × displaced volume, applied at the center of the
    /// submerged region so tilted bodies right themselves), plus linear/angular drag scaled by how
    /// deep they sit, and an optional flow current. Bodies float or sink by their own shape
    /// density: lighter than <see cref="Density"/> floats, denser sinks.
    ///
    /// Optional extras, all Inspector-configurable: deterministic sine <b>waves</b> that bob
    /// floaters (<see cref="SampleSurfaceY"/> exposes the same surface to visuals), an
    /// <b>entry slap</b> that sheds vertical speed the moment a body hits the surface (water
    /// resists sudden entry much harder than steady motion), and <see cref="BodyEntered"/> /
    /// <see cref="BodyExited"/> events for splashes and SFX.
    ///
    /// The zone is world-axis-aligned (a water surface is horizontal — the transform's rotation is
    /// ignored). <see cref="FillLevel"/> sets how much of the zone holds water, so a pool can fill
    /// or drain at runtime; settled floaters are left free to fall asleep, and a moving surface
    /// wakes them (waves count as a moving surface, so they keep floaters awake). Submersion is
    /// estimated from each shape's AABB — right for boxes, close enough everywhere else for
    /// gameplay.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DWaterVolume.png")]
    [AddComponentMenu("Box3D/Forces/Water Volume")]
    [DisallowMultipleComponent]
    public class Box3DWaterVolume : MonoBehaviour
    {
        [Tooltip("Water volume in local units, centered on this object (world-axis-aligned — rotation is ignored; scale applies). The top face is the surface at Fill = 1.")]
        public Vector3 ZoneSize = new Vector3(10f, 4f, 10f);

        [SerializeField, Range(0f, 1f), Tooltip("How much of the zone holds water, bottom-up: 0 = empty, 1 = full. Raise over time to fill a pool.")]
        private float Fill = 1f;

        [SerializeField, Min(1f), Tooltip("Water density in kg/m³ (1000 = water). Shapes denser than this sink, lighter float.")]
        private float Density = 1000f;

        [SerializeField, Min(0f), Tooltip("Linear drag coefficient — how quickly the water slows submerged movement.")]
        private float LinearDrag = 3f;

        [SerializeField, Min(0f), Tooltip("Angular drag coefficient — how quickly the water slows submerged spin.")]
        private float AngularDrag = 1f;

        [SerializeField, Tooltip("Flow velocity in m/s (world space) — a river current pushing submerged bodies.")]
        private Vector3 Current;

        [SerializeField, Range(0f, 1f), Tooltip("Fraction of a body's vertical speed removed the moment it first hits the surface — water resists sudden entry (belly-flop physics). 0 disables.")]
        private float EntrySlap = 0.4f;

        [SerializeField, Tooltip("Animate the surface with deterministic sine waves. Floaters bob and drift; visuals can sample the same surface via SampleSurfaceY. Keeps floaters awake.")]
        private bool EnableWaves;

        [SerializeField, Min(0f), Tooltip("Wave height in meters (peak offset from the flat surface).")]
        private float WaveAmplitude = 0.15f;

        [SerializeField, Min(0.1f), Tooltip("Length of the primary wave in meters (smaller = choppier).")]
        private float WaveLength = 3f;

        [SerializeField, Min(0f), Tooltip("Wave animation speed multiplier.")]
        private float WaveSpeed = 1f;

        /// <summary>A body's first contact with the water: (body, world entry point, speed m/s).
        /// Fired during FixedUpdate — hook splashes, ripples and SFX here.</summary>
        public event System.Action<Body, Vector3, float> BodyEntered;

        /// <summary>A body leaving the water entirely (thrown out, drained past it, or destroyed —
        /// check <c>body.IsValid</c>).</summary>
        public event System.Action<Body> BodyExited;

        private static readonly Color ZoneGizmoColor = new Color(0.25f, 0.6f, 0.9f, 0.9f);
        private static readonly Color SurfaceGizmoColor = new Color(0.25f, 0.6f, 0.9f, 0.25f);

        private Box3DWorld _world;
        private float _lastSurfaceY = float.NaN;
        private bool _warnedTruncated;

        // Broadphase results; a zone overlapping more shapes than this gets truncated for a step.
        private readonly ShapeId[] _overlap = new ShapeId[512];

        // Submersion tracking for events + entry slap (swapped each step — no allocation).
        private HashSet<Body> _submerged = new HashSet<Body>();
        private HashSet<Body> _submergedPrevious = new HashSet<Body>();

        /// <summary>How much of the zone holds water (0 = empty, 1 = full). Setting it moves the
        /// surface; bodies in the zone wake automatically on the next step.</summary>
        public float FillLevel
        {
            get => Fill;
            set => Fill = Mathf.Clamp01(value);
        }

        /// <summary>The flat water surface height in world space (bottom of the zone + fill),
        /// ignoring waves. For the wavy surface at a specific spot use <see cref="SampleSurfaceY"/>.</summary>
        public float SurfaceY
        {
            get
            {
                float halfY = ZoneSize.y * 0.5f * Mathf.Abs(transform.lossyScale.y);
                return transform.position.y - halfY + Fill * halfY * 2f;
            }
        }

        /// <summary>Current wave height in meters (0 when waves are off) — how far the surface can
        /// deviate from <see cref="SurfaceY"/>. Handy for sizing visuals under the surface.</summary>
        public float WaveHeight => EnableWaves ? WaveAmplitude : 0f;

        /// <summary>The water surface height at a world-space (x, z) — the flat fill level plus the
        /// wave offset when waves are enabled. Deterministic in fixed time; the physics samples this
        /// same function, so visuals built on it match what bodies feel. (Inside FixedUpdate,
        /// <c>Time.time</c> equals the fixed-step time, so both contexts agree.)</summary>
        public float SampleSurfaceY(float x, float z)
        {
            float surface = SurfaceY;
            if (!EnableWaves || WaveAmplitude <= 0f) return surface;

            float t = Time.time * WaveSpeed;
            float k = 2f * math.PI / WaveLength;
            // Three fixed-direction components at related frequencies — cheap, loopless, and
            // irregular enough to not read as a single marching sine.
            float wave = math.sin(k * (x * 0.86f + z * 0.5f) + t) * 0.6f
                       + math.sin(k * 1.7f * (z * 0.93f - x * 0.35f) + t * 1.31f) * 0.3f
                       + math.sin(k * 2.9f * (x * 0.6f - z * 0.8f) + t * 1.73f) * 0.1f;
            surface += wave * WaveAmplitude;

            // Never above the zone or below its floor.
            float halfY = ZoneSize.y * 0.5f * Mathf.Abs(transform.lossyScale.y);
            return Mathf.Clamp(surface, transform.position.y - halfY, transform.position.y + halfY);
        }

        private void Awake()
        {
            _world = Box3DWorld.Instance;
        }

        // Runs after Box3DWorld's step (it uses DefaultExecutionOrder(-100)), so forces land on the
        // next step — a constant one-step latency, the same every frame.
        private void FixedUpdate()
        {
            if (!_world || _world.Paused || !_world.World.IsValid) return;
            if (Fill <= 0f)
            {
                FlushSubmerged();
                return;
            }

            float3 half = (float3)ZoneSize * 0.5f * math.abs((float3)transform.lossyScale);
            float3 center = transform.position;
            float surfaceY = SurfaceY;
            bool waves = EnableWaves && WaveAmplitude > 0f;

            // A moving surface (filling/draining, or waves) must wake floaters; a still surface
            // leaves them free to sleep at equilibrium.
            bool wake = waves || !Mathf.Approximately(surfaceY, _lastSurfaceY);
            _lastSurfaceY = surfaceY;

            // The submerged region: zone footprint up to the surface (plus wave crests).
            var water = new B3Aabb
            {
                LowerBound = new float3(center.x - half.x, center.y - half.y, center.z - half.z),
                UpperBound = new float3(center.x + half.x, surfaceY + (waves ? WaveAmplitude : 0f), center.z + half.z),
            };

            int count = _world.World.OverlapAABB(water, QueryFilter.Default, _overlap);
            if (count == _overlap.Length && !_warnedTruncated)
            {
                _warnedTruncated = true;
                Debug.LogWarning($"[Box3DWaterVolume] more than {_overlap.Length} shapes in the water — extras get no buoyancy this step.", this);
            }

            float3 gravity = (float3)_world.GravityVector;
            float3 current = (float3)Current;

            for (int i = 0; i < count; i++)
            {
                var shape = new Shape { Id = _overlap[i] };
                if (shape.IsSensor()) continue; // sensors are non-solid — they displace nothing
                Body body = shape.GetBody();
                if (body.GetBodyType() != BodyType.Dynamic) continue;

                // Submerged fraction from the shape's AABB clipped against the water region, with
                // the surface sampled at the shape's own (x, z) so waves lift and drop it locally.
                B3Aabb bounds = shape.GetAABB();
                float3 boundsCenter = (bounds.LowerBound + bounds.UpperBound) * 0.5f;
                float localSurface = waves ? SampleSurfaceY(boundsCenter.x, boundsCenter.z) : surfaceY;

                float3 lo = math.max(bounds.LowerBound, water.LowerBound);
                float3 hi = math.min(bounds.UpperBound, new float3(water.UpperBound.x, localSurface, water.UpperBound.z));
                if (lo.x >= hi.x || lo.y >= hi.y || lo.z >= hi.z) continue; // broadphase-fat AABB, not actually in

                float3 size = bounds.UpperBound - bounds.LowerBound;
                float boundsVolume = size.x * size.y * size.z;
                if (boundsVolume <= 1e-9f) continue;
                float3 clipped = hi - lo;
                float fraction = clipped.x * clipped.y * clipped.z / boundsVolume;

                // True shape volume from its own mass data, so a sphere doesn't displace like its
                // bounding box; the AABB only supplies the submerged fraction.
                float shapeDensity = shape.GetDensity();
                if (shapeDensity <= 0f) continue;
                float displaced = shape.ComputeMassData().Mass / shapeDensity * fraction;
                if (displaced <= 0f) continue;

                float3 centerOfBuoyancy = (lo + hi) * 0.5f;

                // First-contact handling: entry slap + event. (Multi-shape bodies enter once.)
                if (_submerged.Add(body) && !_submergedPrevious.Contains(body))
                {
                    OnBodyEntered(body, centerOfBuoyancy);
                }

                // Archimedes: weight of the displaced water, opposing gravity, at the submerged
                // region's center — an off-center push that uprights tilted floaters.
                body.ApplyForce(-gravity * (Density * displaced), centerOfBuoyancy, wake);

                // Drag against the water (relative to the current), also at the center of buoyancy
                // so a half-submerged body gets braked where the water actually touches it.
                float3 velocity = body.GetWorldPointVelocity(centerOfBuoyancy) - current;
                body.ApplyForce(-velocity * (LinearDrag * Density * displaced), centerOfBuoyancy, wake);
                body.ApplyTorque(-body.GetAngularVelocity() * (AngularDrag * Density * displaced), wake);
            }

            // Anything wet last step but not this step has left the water.
            foreach (Body body in _submergedPrevious)
            {
                if (!_submerged.Contains(body)) InvokeExited(body);
            }
            (_submerged, _submergedPrevious) = (_submergedPrevious, _submerged);
            _submerged.Clear();
        }

        private void OnBodyEntered(Body body, float3 point)
        {
            float3 velocity = body.GetLinearVelocity();
            if (EntrySlap > 0f && velocity.y < 0f)
            {
                // Shed a fraction of the vertical speed as a one-shot impulse — the surface "slap".
                body.ApplyLinearImpulseToCenter(new float3(0f, -velocity.y * body.GetMass() * EntrySlap, 0f), wake: true);
            }
            // A throwing subscriber must not abort the water step mid-loop (bodies after it would
            // silently get no buoyancy) — same barrier discipline as the native callback trampolines.
            try
            {
                BodyEntered?.Invoke(body, (Vector3)(float3)point, math.length(velocity));
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void InvokeExited(Body body)
        {
            try
            {
                BodyExited?.Invoke(body);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        // Water gone (drained/disabled): everything still tracked as wet has exited.
        private void FlushSubmerged()
        {
            foreach (Body body in _submergedPrevious) InvokeExited(body);
            _submergedPrevious.Clear();
            _submerged.Clear();
        }

        private void OnDisable()
        {
            FlushSubmerged();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 half = Vector3.Scale(ZoneSize * 0.5f, new Vector3(
                Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z)));
            Vector3 center = transform.position;

            Gizmos.color = ZoneGizmoColor;
            Gizmos.DrawWireCube(center, half * 2f);

            float filledHeight = Fill * half.y * 2f;
            if (filledHeight <= 0f) return;
            Vector3 waterCenter = new Vector3(center.x, center.y - half.y + filledHeight * 0.5f, center.z);
            var waterSize = new Vector3(half.x * 2f, filledHeight, half.z * 2f);
            Gizmos.color = SurfaceGizmoColor;
            Gizmos.DrawCube(waterCenter, waterSize);
            Gizmos.color = ZoneGizmoColor;
            Gizmos.DrawWireCube(waterCenter, waterSize);
        }
    }
}
