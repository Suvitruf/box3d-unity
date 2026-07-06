using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>A parallel joint: a spring keeps this body's <see cref="Axis"/> aligned with the
    /// connected body's, leaving position free. Useful to keep something upright.</summary>
    [AddComponentMenu("Box3d/Box3d Parallel Joint")]
    public class Box3dParallelJoint : Box3dJoint
    {
        [SerializeField, Tooltip("Local axis kept aligned with the connected body.")]
        private Vector3 Axis = Vector3.up;

        [SerializeField, Min(0f), Tooltip("Spring stiffness in Hertz.")]
        private float Hertz = 1f;

        [SerializeField, Min(0f), Tooltip("Spring damping ratio.")]
        private float DampingRatio = 1f;

        [SerializeField, Min(0f), Tooltip("Maximum aligning torque in N·m.")]
        private float MaxTorque = 1000f;

        /// <summary>Configures the joint for runtime assembly (before Start).</summary>
        public void Configure(Box3dBody connected, Vector3 axisLocal, float hertz, float dampingRatio)
        {
            SetConnectedBody(connected);
            Axis = axisLocal;
            Hertz = hertz;
            DampingRatio = dampingRatio;
        }

        protected override Joint CreateJoint(BodyId bodyA, BodyId bodyB)
        {
            Vector3 worldAxis = transform.TransformDirection(Axis);

            ParallelJointDef def = ParallelJointDef.Default;
            SetupBase(ref def.Base, bodyA, bodyB, LocalAxisFrame(worldAxis));
            def.Hertz = Hertz;
            def.DampingRatio = DampingRatio;
            def.MaxTorque = MaxTorque;

            return World.World.CreateParallelJoint(def);
        }
    }
}
