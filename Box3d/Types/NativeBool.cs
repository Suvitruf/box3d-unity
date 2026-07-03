using System.Runtime.InteropServices;

namespace Box3d
{
    /// <summary>One-byte bool matching C99 bool across the interop boundary. C# bool would make
    /// containing structs non-blittable.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeBool
    {
        private byte _value;

        public static implicit operator bool(NativeBool b)
        {
            return b._value != 0;
        }

        public static implicit operator NativeBool(bool b)
        {
            return new NativeBool { _value = b ? (byte)1 : (byte)0 };
        }

        public override string ToString()
        {
            return ((bool)this).ToString();
        }
    }
}
