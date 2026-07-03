namespace Box3d
{
    /// <summary>Native library name used by every DllImport (patched into the generated bindings
    /// by bindgen/postprocess.py). iOS links plugins statically, so the imports must target
    /// "__Internal"; everywhere else the dynamic library resolves by name
    /// (box3d.dll / libbox3d.so / libbox3d.dylib).</summary>
    internal static class Box3dLibrary
    {
#if UNITY_IOS && !UNITY_EDITOR
        public const string Name = "__Internal";
#else
        public const string Name = "box3d";
#endif
    }
}
