using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A prismatic (slider) joint: the body slides along <see cref="Axis"/> with no
    /// relative rotation. Optional translation limits and a motor.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DJoint.png")]
    [AddComponentMenu("Box3D/Joints/Slider Joint")]
    public class Box3DSliderJoint : Box3DJoint
    {
        [SerializeField, Tooltip("Local axis the body slides along.")]
        private Vector3 Axis = Vector3.right;

        [SerializeField, Tooltip("Limit the travel.")]
        private bool UseLimits;

        [SerializeField, Tooltip("Lower travel limit in meters.")]
        private float MinTranslation = -0.5f;

        [SerializeField, Tooltip("Upper travel limit in meters.")]
        private float MaxTranslation = 0.5f;

        [SerializeField, Tooltip("Drive the slider with a motor.")]
        private bool UseMotor;

        [SerializeField, Tooltip("Motor target speed in m/s.")]
        private float MotorSpeed = 1f;

        [SerializeField, Min(0f), Tooltip("Maximum motor force in newtons.")]
        private float MaxMotorForce = 100f;

        private PrismaticJoint _slider;

        protected override Joint CreateJoint(BodyId bodyA, BodyId bodyB)
        {
            Vector3 worldAxis = transform.TransformDirection(Axis);

            PrismaticJointDef def = PrismaticJointDef.Default;
            SetupBase(ref def.Base, bodyA, bodyB, LocalSlideFrame(worldAxis));

            if (UseLimits)
            {
                def.EnableLimit = true;
                def.LowerTranslation = Mathf.Min(MinTranslation, MaxTranslation);
                def.UpperTranslation = Mathf.Max(MinTranslation, MaxTranslation);
            }
            if (UseMotor)
            {
                def.EnableMotor = true;
                def.MotorSpeed = MotorSpeed;
                def.MaxMotorForce = MaxMotorForce;
            }

            _slider = IBox3DWorld.Get(this).PhysicsWorld.CreatePrismaticJoint(def);
            return _slider;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Push Inspector edits to the live joint during play. Axis, Anchor and Connected Body
            // are baked into the joint frames at creation and stay fixed.
            if (!Application.isPlaying || !_slider.IsValid) return;
            _slider.EnableLimit(UseLimits);
            _slider.SetLimits(Mathf.Min(MinTranslation, MaxTranslation),
                              Mathf.Max(MinTranslation, MaxTranslation));
            _slider.EnableMotor(UseMotor);
            _slider.SetMotorSpeed(MotorSpeed);
            _slider.SetMaxMotorForce(MaxMotorForce);
            WakeBodies();
        }
#endif
    }
}
