namespace Box3d
{
    public partial struct World
    {
        // Joint factories. Defs must originate from their Default property (internalValue rule).

        public unsafe DistanceJoint CreateDistanceJoint(in DistanceJointDef def)
        {
            DistanceJointDef local = def;
            return new DistanceJoint { Id = UnsafeBindings.b3CreateDistanceJoint(Id, &local) };
        }

        public unsafe MotorJoint CreateMotorJoint(in MotorJointDef def)
        {
            MotorJointDef local = def;
            return new MotorJoint { Id = UnsafeBindings.b3CreateMotorJoint(Id, &local) };
        }

        /// <summary>Creates a filter joint: no constraint, just disables collision between the two
        /// bodies. Returned as a plain <see cref="Joint"/> (it has no typed accessors).</summary>
        public unsafe Joint CreateFilterJoint(in FilterJointDef def)
        {
            FilterJointDef local = def;
            return new Joint { Id = UnsafeBindings.b3CreateFilterJoint(Id, &local) };
        }

        public unsafe ParallelJoint CreateParallelJoint(in ParallelJointDef def)
        {
            ParallelJointDef local = def;
            return new ParallelJoint { Id = UnsafeBindings.b3CreateParallelJoint(Id, &local) };
        }

        public unsafe PrismaticJoint CreatePrismaticJoint(in PrismaticJointDef def)
        {
            PrismaticJointDef local = def;
            return new PrismaticJoint { Id = UnsafeBindings.b3CreatePrismaticJoint(Id, &local) };
        }

        public unsafe RevoluteJoint CreateRevoluteJoint(in RevoluteJointDef def)
        {
            RevoluteJointDef local = def;
            return new RevoluteJoint { Id = UnsafeBindings.b3CreateRevoluteJoint(Id, &local) };
        }

        public unsafe SphericalJoint CreateSphericalJoint(in SphericalJointDef def)
        {
            SphericalJointDef local = def;
            return new SphericalJoint { Id = UnsafeBindings.b3CreateSphericalJoint(Id, &local) };
        }

        public unsafe WeldJoint CreateWeldJoint(in WeldJointDef def)
        {
            WeldJointDef local = def;
            return new WeldJoint { Id = UnsafeBindings.b3CreateWeldJoint(Id, &local) };
        }

        public unsafe WheelJoint CreateWheelJoint(in WheelJointDef def)
        {
            WheelJointDef local = def;
            return new WheelJoint { Id = UnsafeBindings.b3CreateWheelJoint(Id, &local) };
        }
    }
}
