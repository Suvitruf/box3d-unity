using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A continuous water emitter: pours a stream of fluid particles from a rectangular
    /// lip along this object's forward (+Z) axis — rotate the object to aim it. Point it out of a
    /// cliff for a waterfall, straight up for a fountain, or down for a spout; gravity shapes the
    /// stream after launch (the scene-view arc previews where it lands). Pours into the scene's
    /// <see cref="Box3DWater"/> (or an explicitly assigned one), recycling that water's oldest
    /// particles once its pool is full — so a waterfall runs forever on a fixed particle budget.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DWaterfall.png")]
    [AddComponentMenu("Box3D/Waterfall")]
    [DefaultExecutionOrder(-51)] // just before Box3DWater (-50): spawned particles simulate this step
    public class Box3DWaterfall : MonoBehaviour
    {
        [SerializeField, Tooltip("The water this emitter pours into. Leave empty to use the scene's water.")]
        private Box3DWater Water;

        [SerializeField, Min(0f), Tooltip("Particles emitted per second. The steady-state stream holds FlowRate × flight-time particles — budget the water's Max Particles accordingly, or the stream recycles the oldest water (e.g. the pool it feeds).")]
        private float FlowRate = 600f;

        [SerializeField, Min(0f), Tooltip("Launch speed in m/s along this object's forward (+Z) axis.")]
        private float Speed = 4f;

        [SerializeField, Tooltip("The pouring lip in local units: width (X) by thickness (Y). The stream leaves this rectangle along +Z.")]
        private Vector2 LipSize = new Vector2(2f, 0.3f);

        [SerializeField, Range(0f, 1f), Tooltip("Random launch variation as a fraction of Speed — a little breaks the sheet into natural ropes and spray.")]
        private float Spread = 0.08f;

        [SerializeField, Tooltip("Start pouring as soon as the emitter is enabled. Toggle at runtime with IsFlowing.")]
        private bool FlowOnEnable = true;

        private const int MaxPerStep = 1024;

        private readonly float4[] _positions = new float4[MaxPerStep];
        private readonly float4[] _velocities = new float4[MaxPerStep];
        private Unity.Mathematics.Random _rng;
        private float _carry; // fractional particles owed from previous steps
        private bool _flowing;
        private bool _warnedNoWater;

        /// <summary>Whether the emitter is currently pouring. Set it to open/close the tap.</summary>
        public bool IsFlowing
        {
            get => _flowing;
            set => _flowing = value;
        }

        /// <summary>The water being poured into (auto-found on enable when not assigned).</summary>
        public Box3DWater Target => Water;

        private void OnEnable()
        {
            _rng = new Unity.Mathematics.Random(0x6E624EB7u);
            _carry = 0f;
            _flowing = FlowOnEnable;
            if (Application.isPlaying && !Water)
            {
                Water = FindAnyObjectByType<Box3DWater>();
            }
        }

        private void FixedUpdate()
        {
            if (!_flowing || FlowRate <= 0f) return;
            if (!Water || Water.ParticleBuffer == null)
            {
                if (!_warnedNoWater)
                {
                    _warnedNoWater = true;
                    Debug.LogWarning("Box3DWaterfall: no running Box3DWater to pour into — add one (GameObject → Box3D → Water) or assign the Water field.", this);
                }
                return;
            }
            _warnedNoWater = false;

            float dt = Time.fixedDeltaTime;
            _carry += FlowRate * dt;
            int count = (int)_carry;
            if (count == 0) return;
            _carry -= count;
            count = Mathf.Min(count, MaxPerStep);

            float3 forward = transform.forward;
            Matrix4x4 localToWorld = transform.localToWorldMatrix; // hoisted out of the per-particle loop
            for (int i = 0; i < count; i++)
            {
                float2 lip = (_rng.NextFloat2() - 0.5f) * (float2)LipSize;
                float3 velocity = forward * Speed + _rng.NextFloat3Direction() * (Speed * Spread);
                float3 position = (float3)localToWorld.MultiplyPoint3x4(new Vector3(lip.x, lip.y, 0f));

                // Scatter each particle along its first-step travel so consecutive steps join
                // into a continuous sheet instead of pulsing in bands.
                position += velocity * (_rng.NextFloat() * dt);

                _positions[i] = new float4(position, 1f);
                _velocities[i] = new float4(velocity, 0f);
            }

            Water.SpawnParticles(_positions, _velocities, count);
        }

        // ------------------------------------------------------------------ gizmos

        // Matches the water volume color.
        private static readonly Color GizmoColor = new Color(0.35f, 0.75f, 0.95f, 0.9f);

        // Cached scene search for the gravity preview: gizmos repaint continuously while
        // selected, and a full scene scan per repaint adds up. Re-searched at most twice a
        // second while no world exists.
        private Box3DWorld _gizmoWorld;
        private float _nextGizmoSearch;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(LipSize.x, LipSize.y, 0f));

            // Ballistic preview of where the stream goes: the lip center and both width edges,
            // integrated with the scene's gravity (the same curve the particles will fly).
            Gizmos.matrix = Matrix4x4.identity;
            if (!_gizmoWorld && Time.realtimeSinceStartup >= _nextGizmoSearch)
            {
                _nextGizmoSearch = Time.realtimeSinceStartup + 0.5f;
                _gizmoWorld = FindAnyObjectByType<Box3DWorld>();
            }
            Vector3 gravity = _gizmoWorld ? _gizmoWorld.GravityVector : Physics.gravity;

            for (int edge = -1; edge <= 1; edge++)
            {
                Vector3 p = transform.TransformPoint(new Vector3(edge * LipSize.x * 0.5f, 0f, 0f));
                Vector3 v = transform.forward * Speed;
                const float previewDt = 0.06f;
                for (int i = 0; i < 24; i++)
                {
                    Vector3 next = p + v * previewDt;
                    Gizmos.DrawLine(p, next);
                    v += gravity * previewDt;
                    p = next;
                }
            }
        }
    }
}
