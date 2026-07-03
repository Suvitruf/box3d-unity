namespace Box3d
{
    /// <summary>Global, world-independent Box3d entry points.</summary>
    public static class Box3dApi
    {
        /// <summary>Version of the loaded native library. First call proves the DLL resolves.</summary>
        public static Box3dVersion GetVersion()
        {
            return UnsafeBindings.b3GetVersion();
        }

        /// <summary>True if the native library was built with BOX3D_DOUBLE_PRECISION. This wrapper
        /// requires a single-precision build; managed struct layouts assume it.</summary>
        public static bool IsDoublePrecision => UnsafeBindings.b3IsDoublePrecision();
    }
}
