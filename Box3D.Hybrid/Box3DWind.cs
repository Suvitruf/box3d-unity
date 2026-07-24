using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A directional wind volume. Every physics step it pushes the dynamic bodies inside
    /// its box zone along the GameObject's forward (+Z) axis — rotate the object to aim the wind.
    /// Strength can gust over time with Perlin noise. Select the object to see the zone and a grid
    /// of arrows showing direction and (while playing) the live gust strength.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DWind.png")]
    [AddComponentMenu("Box3D/Forces/Wind")]
    public class Box3DWind : MonoBehaviour
    {
        [SerializeField, Tooltip("Steady force in newtons on each dynamic body in the zone, along this object's forward (+Z) axis. Negative blows backward.")]
        private float Strength = 20f;

        [SerializeField, Tooltip("Apply as acceleration instead of force, so light and heavy bodies drift equally (like gravity).")]
        private bool IgnoreMass;

        [SerializeField, Range(0f, 1f), Tooltip("Gust variation as a fraction of Strength: 0 = steady, 1 = swings between 0 and 2× Strength.")]
        private float GustAmplitude = 0.25f;

        [SerializeField, Min(0f), Tooltip("Gust speed in cycles per second.")]
        private float GustFrequency = 0.5f;

        [SerializeField, Tooltip("Wind volume in local units, centered on this object (follows the transform's rotation and scale). Make it large to cover the whole scene.")]
        private Vector3 ZoneSize = new Vector3(10f, 10f, 10f);

        private Box3DWorld _world;
        private float _currentStrength;

        // Broadphase results; a zone overlapping more shapes than this gets truncated for a step.
        private readonly ShapeId[] _overlap = new ShapeId[512];
        private readonly HashSet<Body> _bodies = new HashSet<Body>();

        private void Awake()
        {
            _world = Box3DWorld.Instance;
            _currentStrength = Strength;
        }

        // Runs after Box3DWorld's step (it uses DefaultExecutionOrder(-100)), so forces land on the
        // next step — a constant one-step latency, the same every frame.
        private void FixedUpdate()
        {
            if (!_world || _world.Paused || !_world.World.IsValid) return;

            _currentStrength = Strength;
            if (GustAmplitude > 0f && GustFrequency > 0f)
            {
                float noise = Mathf.PerlinNoise(Time.fixedTime * GustFrequency, 0.37f) * 2f - 1f;
                _currentStrength = Strength * (1f + GustAmplitude * noise);
            }
            if (Mathf.Abs(_currentStrength) < 1e-5f) return;

            // Broadphase pass over the zone's world AABB, then an exact test that the body origin
            // is inside the oriented zone.
            float3 half = (float3)ZoneSize * 0.5f * math.abs((float3)transform.lossyScale);
            var rot = new float3x3((quaternion)transform.rotation);
            float3 worldExtents = math.mul(new float3x3(math.abs(rot.c0), math.abs(rot.c1), math.abs(rot.c2)), half);
            var aabb = new B3Aabb
            {
                LowerBound = (float3)transform.position - worldExtents,
                UpperBound = (float3)transform.position + worldExtents,
            };

            int count = _world.World.OverlapAABB(aabb, QueryFilter.Default, _overlap);
            _bodies.Clear();
            for (int i = 0; i < count; i++)
            {
                _bodies.Add(new Shape { Id = _overlap[i] }.GetBody());
            }

            float3 force = (float3)transform.forward * _currentStrength;
            foreach (Body body in _bodies)
            {
                if (body.GetBodyType() != BodyType.Dynamic) continue;

                Vector3 local = transform.InverseTransformPoint((Vector3)body.Position);
                if (Mathf.Abs(local.x) > ZoneSize.x * 0.5f ||
                    Mathf.Abs(local.y) > ZoneSize.y * 0.5f ||
                    Mathf.Abs(local.z) > ZoneSize.z * 0.5f) continue;

                body.ApplyForceToCenter(IgnoreMass ? force * body.GetMass() : force, wake: true);
            }
        }

        // Matches the Forces category icon color.
        private static readonly Color GizmoColor = new Color(0.96f, 0.82f, 0.35f, 0.9f);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, ZoneSize);

            // A 3×3 grid of arrows on the zone's mid plane, blowing along local +Z. While playing
            // the length follows the live gust strength; a negative Strength flips the arrows.
            float factor = !Application.isPlaying || Mathf.Abs(Strength) < 1e-5f
                ? 1f
                : _currentStrength / Strength;
            float len = Mathf.Min(ZoneSize.z * 0.35f, 2f) * factor * Mathf.Sign(Strength);
            if (Mathf.Abs(len) < 1e-4f) return;

            float head = Mathf.Abs(len) * 0.3f;
            for (int ix = -1; ix <= 1; ix++)
            {
                for (int iy = -1; iy <= 1; iy++)
                {
                    var start = new Vector3(ix * ZoneSize.x * 0.3f, iy * ZoneSize.y * 0.3f, -len * 0.5f);
                    Vector3 end = start + new Vector3(0f, 0f, len);
                    Gizmos.DrawLine(start, end);
                    float back = Mathf.Sign(len) * head;
                    Gizmos.DrawLine(end, end + new Vector3(head * 0.5f, 0f, -back));
                    Gizmos.DrawLine(end, end + new Vector3(-head * 0.5f, 0f, -back));
                    Gizmos.DrawLine(end, end + new Vector3(0f, head * 0.5f, -back));
                    Gizmos.DrawLine(end, end + new Vector3(0f, -head * 0.5f, -back));
                }
            }
        }
    }
}
