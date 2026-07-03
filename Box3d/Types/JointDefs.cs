using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Box3d
{
    // Mirrors of the joint definition structs in box3d types.h. Same rule as all defs: never
    // zero-initialize — start from the Default property (hidden internalValue cookie in the base).

    /// <summary>Mirrors native b3JointDef (112 bytes on x64). Common base embedded in every typed
    /// joint def. Local frames are relative to each body's origin (not center of mass).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct JointDefBase
    {
        public IntPtr UserData;
        public BodyId BodyIdA;
        public BodyId BodyIdB;
        public B3Transform LocalFrameA;
        public B3Transform LocalFrameB;
        public float ForceThreshold;
        public float TorqueThreshold;
        public float ConstraintHertz;
        public float ConstraintDampingRatio;
        public float DrawScale;
        public NativeBool CollideConnected;
        internal int InternalValue;
    }

    /// <summary>Mirrors native b3DistanceJointDef (160 bytes). Segment between two anchor points —
    /// ropes and springs.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DistanceJointDef
    {
        public JointDefBase Base;
        public float Length;
        public NativeBool EnableSpring;
        public float LowerSpringForce;
        public float UpperSpringForce;
        public float Hertz;
        public float DampingRatio;
        public NativeBool EnableLimit;
        public float MinLength;
        public float MaxLength;
        public NativeBool EnableMotor;
        public float MaxMotorForce;
        public float MotorSpeed;

        public static DistanceJointDef Default => UnsafeBindings.b3DefaultDistanceJointDef();
    }

    /// <summary>Mirrors native b3MotorJointDef (168 bytes). Controls relative position and velocity
    /// of two bodies; box3d's drag/mouse-joint replacement.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MotorJointDef
    {
        public JointDefBase Base;
        public float3 LinearVelocity;
        public float MaxVelocityForce;
        public float3 AngularVelocity;
        public float MaxVelocityTorque;
        public float LinearHertz;
        public float LinearDampingRatio;
        public float MaxSpringForce;
        public float AngularHertz;
        public float AngularDampingRatio;
        public float MaxSpringTorque;

        public static MotorJointDef Default => UnsafeBindings.b3DefaultMotorJointDef();
    }

    /// <summary>Mirrors native b3FilterJointDef (112 bytes). Disables collision between two bodies.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FilterJointDef
    {
        public JointDefBase Base;

        public static FilterJointDef Default => UnsafeBindings.b3DefaultFilterJointDef();
    }

    /// <summary>Mirrors native b3ParallelJointDef (128 bytes). Spring-aligns body z-axes — keeps a
    /// body upright.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ParallelJointDef
    {
        public JointDefBase Base;
        public float Hertz;
        public float DampingRatio;
        public float MaxTorque;

        public static ParallelJointDef Default => UnsafeBindings.b3DefaultParallelJointDef();
    }

    /// <summary>Mirrors native b3PrismaticJointDef (152 bytes). Slide along frame A's x-axis,
    /// no relative rotation.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PrismaticJointDef
    {
        public JointDefBase Base;
        public NativeBool EnableSpring;
        public float Hertz;
        public float DampingRatio;
        public float TargetTranslation;
        public NativeBool EnableLimit;
        public float LowerTranslation;
        public float UpperTranslation;
        public NativeBool EnableMotor;
        public float MaxMotorForce;
        public float MotorSpeed;

        public static PrismaticJointDef Default => UnsafeBindings.b3DefaultPrismaticJointDef();
    }

    /// <summary>Mirrors native b3RevoluteJointDef (152 bytes). Hinge around frame z-axis.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RevoluteJointDef
    {
        public JointDefBase Base;
        public float TargetAngle;
        public NativeBool EnableSpring;
        public float Hertz;
        public float DampingRatio;
        public NativeBool EnableLimit;
        public float LowerAngle;
        public float UpperAngle;
        public NativeBool EnableMotor;
        public float MaxMotorTorque;
        public float MotorSpeed;

        public static RevoluteJointDef Default => UnsafeBindings.b3DefaultRevoluteJointDef();
    }

    /// <summary>Mirrors native b3SphericalJointDef (184 bytes). Ball-and-socket with cone/twist
    /// limits — the ragdoll joint.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SphericalJointDef
    {
        public JointDefBase Base;
        public NativeBool EnableSpring;
        public float Hertz;
        public float DampingRatio;
        public quaternion TargetRotation;
        public NativeBool EnableConeLimit;
        public float ConeAngle;
        public NativeBool EnableTwistLimit;
        public float LowerTwistAngle;
        public float UpperTwistAngle;
        public NativeBool EnableMotor;
        public float MaxMotorTorque;
        public float3 MotorVelocity;

        public static SphericalJointDef Default => UnsafeBindings.b3DefaultSphericalJointDef();
    }

    /// <summary>Mirrors native b3WeldJointDef (128 bytes). Rigid (or springy) connection.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WeldJointDef
    {
        public JointDefBase Base;
        public float LinearHertz;
        public float AngularHertz;
        public float LinearDampingRatio;
        public float AngularDampingRatio;

        public static WeldJointDef Default => UnsafeBindings.b3DefaultWeldJointDef();
    }

    /// <summary>Mirrors native b3WheelJointDef (184 bytes). Chassis (A) + wheel (B): suspension
    /// along frame A x-axis, spin around frame B z-axis, optional steering.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WheelJointDef
    {
        public JointDefBase Base;
        public NativeBool EnableSuspensionSpring;
        public float SuspensionHertz;
        public float SuspensionDampingRatio;
        public NativeBool EnableSuspensionLimit;
        public float LowerSuspensionLimit;
        public float UpperSuspensionLimit;
        public NativeBool EnableSpinMotor;
        public float MaxSpinTorque;
        public float SpinSpeed;
        public NativeBool EnableSteering;
        public float SteeringHertz;
        public float SteeringDampingRatio;
        public float TargetSteeringAngle;
        public float MaxSteeringTorque;
        public NativeBool EnableSteeringLimit;
        public float LowerSteeringLimit;
        public float UpperSteeringLimit;

        public static WheelJointDef Default => UnsafeBindings.b3DefaultWheelJointDef();
    }
}
