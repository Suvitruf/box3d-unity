namespace Box3D
{
    public static unsafe partial class UnsafeBindings
    {
        /// <summary>Box3D's position sanity limit in the current length units.</summary>
#if BOX3D_DOUBLE
        public static float B3_HUGE => 1e9f * b3GetLengthUnitsPerMeter();
#else
        public static float B3_HUGE => 1e5f * b3GetLengthUnitsPerMeter();
#endif
    }
}
