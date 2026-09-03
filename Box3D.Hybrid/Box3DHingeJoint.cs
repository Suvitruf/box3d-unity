using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A hinge (revolute) joint, analogous to Unity's HingeJoint. Rotates around
    /// <see cref="Axis"/> through the anchor, with optional angle limits and a motor.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DJoint.png")]
    [AddComponentMenu("Box3D/Joints/Hinge Joint")]
    public class Box3DHingeJoint : Box3DJoint
    {
        [SerializeField, Tooltip("Local rotation axis of the hinge.")]
        private Vector3 Axis = Vector3.up;

        [SerializeField, Tooltip("Limit the hinge angle.")]
        private bool UseLimits;

        [SerializeField, Tooltip("Lower angle limit in degrees.")]
        private float MinAngle = -45f;

        [SerializeField, Tooltip("Upper angle limit in degrees.")]
        private float MaxAngle = 45f;

        [SerializeField, Tooltip("Drive the hinge with a motor.")]
        private bool UseMotor;

        [SerializeField, Tooltip("Motor target speed in degrees/second.")]
        private float MotorSpeed = 90f;

        [SerializeField, Min(0f), Tooltip("Maximum motor torque in N·m.")]
        private float MaxMotorTorque = 10f;

        private RevoluteJoint _hinge;

        /// <summary>Sets the local hinge axis. Must be set before the joint is created (Start).</summary>
        public void SetAxis(Vector3 localAxis)
        {
            Axis = localAxis;
        }

        protected override Joint CreateJoint(BodyId bodyA, BodyId bodyB)
        {
            Vector3 worldAxis = transform.TransformDirection(Axis);

            RevoluteJointDef def = RevoluteJointDef.Default;
            SetupBase(ref def.Base, bodyA, bodyB, LocalAxisFrame(worldAxis));

            if (UseLimits)
            {
                def.EnableLimit = true;
                def.LowerAngle = Mathf.Min(MinAngle, MaxAngle) * Mathf.Deg2Rad;
                def.UpperAngle = Mathf.Max(MinAngle, MaxAngle) * Mathf.Deg2Rad;
            }
            if (UseMotor)
            {
                def.EnableMotor = true;
                def.MotorSpeed = MotorSpeed * Mathf.Deg2Rad;
                def.MaxMotorTorque = MaxMotorTorque;
            }

            _hinge = IBox3DWorld.Get(this).PhysicsWorld.CreateRevoluteJoint(def);
            return _hinge;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Push Inspector edits to the live joint during play. Axis, Anchor and Connected Body
            // are baked into the joint frames at creation and stay fixed.
            if (!Application.isPlaying || !_hinge.IsValid) return;
            _hinge.EnableLimit(UseLimits);
            _hinge.SetLimits(Mathf.Min(MinAngle, MaxAngle) * Mathf.Deg2Rad,
                             Mathf.Max(MinAngle, MaxAngle) * Mathf.Deg2Rad);
            _hinge.EnableMotor(UseMotor);
            _hinge.SetMotorSpeed(MotorSpeed * Mathf.Deg2Rad);
            _hinge.SetMaxMotorTorque(MaxMotorTorque);
            WakeBodies();
        }
#endif

        private void OnDrawGizmosSelected()
        {
            Vector3 anchor = WorldAnchor;
            Vector3 axis = transform.TransformDirection(Axis).normalized * 0.5f;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
            Gizmos.DrawLine(anchor - axis, anchor + axis);
            Gizmos.DrawWireSphere(anchor, 0.06f);
        }
    }
}
