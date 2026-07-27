using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>One physics impact delivered to <see cref="IBox3DHitReceiver"/> components — the
    /// component-layer view of a native contact hit event. Fires only when the approach speed
    /// exceeds the world's hit threshold (~1 m/s), so resting and sliding contact never spams.</summary>
    public struct Box3DHit
    {
        /// <summary>World-space impact point.</summary>
        public Vector3 Point;

        /// <summary>Unit impact direction, pointing into the receiving body's surface —
        /// deformation pushes along it.</summary>
        public Vector3 Direction;

        /// <summary>How fast the surfaces were approaching, in m/s.</summary>
        public float ApproachSpeed;

        /// <summary>The other body, when it has a <see cref="Box3DBody"/> component (null for
        /// plain static geometry).</summary>
        public Box3DBody OtherBody;
    }

    /// <summary>Implement on a MonoBehaviour under a <see cref="Box3DBody"/> to receive impacts on
    /// the body's shapes. The body detects receivers when it wakes and opts its shapes into native
    /// hit events, so add receivers before play (or before the body is spawned); disabled receivers
    /// are skipped at dispatch.</summary>
    public interface IBox3DHitReceiver
    {
        /// <summary>Called after the physics step for each qualifying impact.</summary>
        void OnBox3DHit(in Box3DHit hit);
    }
}
