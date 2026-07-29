namespace Box3D
{
    /// <summary>Native library name used by every DllImport (patched into the generated bindings
    /// by bindgen/postprocess.py). iOS and WebGL link plugins statically, so the imports must
    /// target "__Internal"; everywhere else the dynamic library resolves by name
    /// (box3d.dll / libbox3d.so / libbox3d.dylib).</summary>
    internal static class Box3DLibrary
    {
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        public const string Name = "__Internal"; // statically linked — precision is fixed by which archive was linked into the player
#elif BOX3D_DOUBLE
        public const string Name = "box3d_d"; // double-precision variant, shipped alongside the single build
#else
        public const string Name = "box3d";
#endif
    }
}
