namespace Box3d
{
    // Typed joint wrappers. All wrap the same JointId (box3d has no joint-type hierarchy in its
    // handles); each gets its b3{X}Joint_* surface via generated forwarders, plus implicit widening
    // to Joint for the common b3Joint_* surface.

    /// <summary>Distance joint: keeps two anchor points at a distance, optionally spring/limited.</summary>
    public partial struct DistanceJoint
    {
        public JointId Id;

        public static implicit operator Joint(DistanceJoint joint)
        {
            return new Joint { Id = joint.Id };
        }
    }

    /// <summary>Motor joint: drives a body toward a target transform with force/torque limits.
    /// Box3d's replacement for a mouse/drag joint.</summary>
    public partial struct MotorJoint
    {
        public JointId Id;

        public static implicit operator Joint(MotorJoint joint)
        {
            return new Joint { Id = joint.Id };
        }
    }

    /// <summary>Parallel joint: keeps two body frames rotationally aligned.</summary>
    public partial struct ParallelJoint
    {
        public JointId Id;

        public static implicit operator Joint(ParallelJoint joint)
        {
            return new Joint { Id = joint.Id };
        }
    }

    /// <summary>Prismatic joint: translation along one axis, rotation locked.</summary>
    public partial struct PrismaticJoint
    {
        public JointId Id;

        public static implicit operator Joint(PrismaticJoint joint)
        {
            return new Joint { Id = joint.Id };
        }
    }

    /// <summary>Revolute joint: rotation around one axis (hinge).</summary>
    public partial struct RevoluteJoint
    {
        public JointId Id;

        public static implicit operator Joint(RevoluteJoint joint)
        {
            return new Joint { Id = joint.Id };
        }
    }

    /// <summary>Spherical joint: ball-and-socket with cone/twist limits. The ragdoll joint.</summary>
    public partial struct SphericalJoint
    {
        public JointId Id;

        public static implicit operator Joint(SphericalJoint joint)
        {
            return new Joint { Id = joint.Id };
        }
    }

    /// <summary>Weld joint: rigidly connects two bodies, optionally springy.</summary>
    public partial struct WeldJoint
    {
        public JointId Id;

        public static implicit operator Joint(WeldJoint joint)
        {
            return new Joint { Id = joint.Id };
        }
    }

    /// <summary>Wheel joint: suspension spring along one axis plus drive motor around another.</summary>
    public partial struct WheelJoint
    {
        public JointId Id;

        public static implicit operator Joint(WheelJoint joint)
        {
            return new Joint { Id = joint.Id };
        }
    }
}
