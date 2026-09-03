using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A motor joint: a soft, driveable link that pulls this body toward its rest pose
    /// relative to the connected body with linear and angular springs. Unlike the rigid fixed joint,
    /// its stiffness and maximum force are tunable — useful for soft attachments, platforms, and
    /// dragging bodies from code (the classic "mouse joint").</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DJoint.png")]
    [AddComponentMenu("Box3D/Joints/Motor Joint")]
    public class Box3DMotorJoint : Box3DJoint
    {
        [SerializeField, Min(0f), Tooltip("Linear spring stiffness in Hertz.")]
        private float LinearHertz = 5f;

        [SerializeField, Min(0f), Tooltip("Linear spring damping ratio.")]
        private float LinearDampingRatio = 0.7f;

        [SerializeField, Min(0f), Tooltip("Maximum linear force in newtons.")]
        private float MaxForce = 1000f;

        [SerializeField, Min(0f), Tooltip("Angular spring stiffness in Hertz.")]
        private float AngularHertz = 5f;

        [SerializeField, Min(0f), Tooltip("Angular spring damping ratio.")]
        private float AngularDampingRatio = 0.7f;

        [SerializeField, Min(0f), Tooltip("Maximum torque in N·m.")]
        private float MaxTorque = 1000f;

        private MotorJoint _motor;

        protected override Joint CreateJoint(BodyId bodyA, BodyId bodyB)
        {
            MotorJointDef def = MotorJointDef.Default;
            // Coincident frames at the anchor → the current relative pose is the spring's rest state.
            SetupBase(ref def.Base, bodyA, bodyB, quaternion.identity);
            def.LinearHertz = LinearHertz;
            def.LinearDampingRatio = LinearDampingRatio;
            def.MaxSpringForce = MaxForce;
            def.AngularHertz = AngularHertz;
            def.AngularDampingRatio = AngularDampingRatio;
            def.MaxSpringTorque = MaxTorque;

            _motor = IBox3DWorld.Get(this).PhysicsWorld.CreateMotorJoint(def);
            return _motor;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Push Inspector edits to the live joint during play. The rest pose is captured at
            // creation and stays fixed; code-driven velocities are untouched.
            if (!Application.isPlaying || !_motor.IsValid) return;
            _motor.SetLinearHertz(LinearHertz);
            _motor.SetLinearDampingRatio(LinearDampingRatio);
            _motor.SetMaxSpringForce(MaxForce);
            _motor.SetAngularHertz(AngularHertz);
            _motor.SetAngularDampingRatio(AngularDampingRatio);
            _motor.SetMaxSpringTorque(MaxTorque);
            WakeBodies();
        }
#endif

        /// <summary>Drives the joint with a relative linear velocity (world space).</summary>
        public void SetLinearVelocity(Vector3 velocity)
        {
            if (_motor.IsValid) _motor.SetLinearVelocity(velocity);
        }
    }
}
