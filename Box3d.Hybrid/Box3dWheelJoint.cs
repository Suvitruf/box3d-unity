using Unity.Mathematics;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>A wheel joint, analogous to a vehicle suspension: attaches this body (the wheel) to
    /// the connected body (the chassis) with a spring along the suspension axis, free spin about the
    /// axle, and optional steering. Put it on the wheel and set Connected Body to the chassis.</summary>
    [AddComponentMenu("Box3d/Box3d Wheel Joint")]
    public class Box3dWheelJoint : Box3dJoint
    {
        [SerializeField, Tooltip("Suspension travel axis in the wheel's local space (usually up).")]
        private Vector3 SuspensionAxis = Vector3.up;

        [SerializeField, Tooltip("Wheel spin axis (the axle) in the wheel's local space.")]
        private Vector3 SpinAxis = Vector3.right;

        [SerializeField, Min(0f), Tooltip("Suspension spring stiffness in Hertz.")]
        private float SuspensionHertz = 4f;

        [SerializeField, Min(0f), Tooltip("Suspension damping ratio.")]
        private float SuspensionDamping = 0.7f;

        [SerializeField, Min(0f), Tooltip("Suspension travel distance (± meters).")]
        private float SuspensionTravel = 0.2f;

        [SerializeField, Tooltip("Drive the wheel with a spin motor.")]
        private bool DriveWheel;

        [SerializeField, Tooltip("Motor target spin speed (rad/s).")]
        private float SpinSpeed = 20f;

        [SerializeField, Min(0f), Tooltip("Maximum drive torque in N·m.")]
        private float MaxSpinTorque = 20f;

        [SerializeField, Tooltip("Enable steering (rotate the wheel about the suspension axis).")]
        private bool Steerable;

        [SerializeField, Min(0f), Tooltip("Steering stiffness in Hertz.")]
        private float SteeringHertz = 10f;

        [SerializeField, Min(0f), Tooltip("Steering damping ratio.")]
        private float SteeringDamping = 0.7f;

        [SerializeField, Min(0f), Tooltip("Maximum steering torque in N·m.")]
        private float MaxSteeringTorque = 5f;

        [SerializeField, Range(0f, 80f), Tooltip("Maximum steering angle in degrees.")]
        private float MaxSteerAngle = 30f;

        private WheelJoint _wheel;

        /// <summary>Configures the wheel for runtime assembly (before the joint is created in Start):
        /// connects it to the chassis, sets the axle direction, and enables drive/steering.</summary>
        public void Configure(Box3dBody chassis, Vector3 spinAxisLocal, bool drive, bool steerable)
        {
            SetConnectedBody(chassis);
            SpinAxis = spinAxisLocal;
            DriveWheel = drive;
            Steerable = steerable;
        }

        protected override Joint CreateJoint(BodyId bodyA, BodyId bodyB)
        {
            // bodyA = chassis (connected / world), bodyB = this wheel.
            quaternion frame = BuildFrame(transform.TransformDirection(SuspensionAxis), transform.TransformDirection(SpinAxis));
            Vector3 center = transform.position;

            WheelJointDef def = WheelJointDef.Default;
            def.Base.BodyIdA = bodyA;
            def.Base.BodyIdB = bodyB;
            def.Base.CollideConnected = Collides;
            def.Base.LocalFrameB = new B3Transform
            {
                Position = float3.zero, // the wheel's center
                Rotation = math.mul(math.inverse((quaternion)transform.rotation), frame),
            };
            def.Base.LocalFrameA = Connected
                ? new B3Transform
                {
                    Position = ToBodyLocal(Connected.transform, center),
                    Rotation = math.mul(math.inverse((quaternion)Connected.transform.rotation), frame),
                }
                : new B3Transform { Position = center, Rotation = frame };

            def.EnableSuspensionSpring = true;
            def.SuspensionHertz = SuspensionHertz;
            def.SuspensionDampingRatio = SuspensionDamping;
            def.EnableSuspensionLimit = true;
            def.LowerSuspensionLimit = -SuspensionTravel;
            def.UpperSuspensionLimit = SuspensionTravel;

            def.EnableSpinMotor = DriveWheel;
            def.MaxSpinTorque = MaxSpinTorque;
            def.SpinSpeed = SpinSpeed;

            def.EnableSteering = Steerable;
            def.SteeringHertz = SteeringHertz;
            def.SteeringDampingRatio = SteeringDamping;
            def.MaxSteeringTorque = MaxSteeringTorque;
            def.EnableSteeringLimit = Steerable;
            def.LowerSteeringLimit = -math.radians(MaxSteerAngle);
            def.UpperSteeringLimit = math.radians(MaxSteerAngle);

            _wheel = World.World.CreateWheelJoint(def);
            return _wheel;
        }

        /// <summary>Sets the drive motor's target spin speed (rad/s). Wakes the bodies when non-zero
        /// so a settled car starts moving.</summary>
        public void SetSpinSpeed(float radiansPerSecond)
        {
            if (!_wheel.IsValid) return;
            _wheel.SetSpinMotorSpeed(radiansPerSecond);
            WakeBodies();
        }

        /// <summary>Sets the target steering angle (degrees).</summary>
        public void SetSteerAngle(float degrees)
        {
            if (!_wheel.IsValid) return;
            _wheel.SetTargetSteeringAngle(math.radians(degrees));
            if (degrees != 0f) WakeBodies();
        }

        // Frame with box3d's convention: x-axis = suspension travel, z-axis = spin/steer axis.
        // Built via LookRotation (unambiguous) so the frame's x really is the suspension axis:
        // LookRotation(forward, up) makes local z = forward and local x = cross(up, forward), so
        // passing forward = axle and up = cross(axle, suspension) yields x = suspension, z = axle.
        private static quaternion BuildFrame(Vector3 suspension, Vector3 spin)
        {
            Vector3 x = suspension.normalized;                       // frame x = suspension
            Vector3 z = spin.normalized;
            z = (z - Vector3.Dot(z, x) * x).normalized;              // frame z = axle, ⊥ suspension
            if (z.sqrMagnitude < 1e-6f) z = Vector3.forward;
            return Quaternion.LookRotation(z, Vector3.Cross(z, x));
        }
    }
}
