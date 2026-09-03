using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A parallel joint: a spring keeps this body's <see cref="Axis"/> aligned with the
    /// connected body's, leaving position free. Useful to keep something upright.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DJoint.png")]
    [AddComponentMenu("Box3D/Joints/Parallel Joint")]
    public class Box3DParallelJoint : Box3DJoint
    {
        [SerializeField, Tooltip("Local axis kept aligned with the connected body.")]
        private Vector3 Axis = Vector3.up;

        [SerializeField, Min(0f), Tooltip("Spring stiffness in Hertz.")]
        private float Hertz = 1f;

        [SerializeField, Min(0f), Tooltip("Spring damping ratio.")]
        private float DampingRatio = 1f;

        [SerializeField, Min(0f), Tooltip("Maximum aligning torque in N·m.")]
        private float MaxTorque = 1000f;

        private ParallelJoint _parallel;

        /// <summary>Configures the joint for runtime assembly (before Start).</summary>
        public void Configure(Box3DBody connected, Vector3 axisLocal, float hertz, float dampingRatio)
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

            _parallel = IBox3DWorld.Get(this).PhysicsWorld.CreateParallelJoint(def);
            return _parallel;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Push Inspector edits to the live joint during play. Axis and Connected Body are
            // baked into the joint frames at creation and stay fixed.
            if (!Application.isPlaying || !_parallel.IsValid) return;
            _parallel.SetSpringHertz(Hertz);
            _parallel.SetSpringDampingRatio(DampingRatio);
            _parallel.SetMaxTorque(MaxTorque);
            WakeBodies();
        }
#endif
    }
}
