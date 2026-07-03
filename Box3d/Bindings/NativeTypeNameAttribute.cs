using System;
using System.Diagnostics;

namespace Box3d
{
    /// <summary>Preserves the original C type name on generated bindings (emitted by ClangSharp).</summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property |
                    AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue,
        AllowMultiple = false, Inherited = true)]
    [Conditional("DEBUG")]
    internal sealed class NativeTypeNameAttribute : Attribute
    {
        public string Name { get; }

        public NativeTypeNameAttribute(string name)
        {
            Name = name;
        }
    }
}
