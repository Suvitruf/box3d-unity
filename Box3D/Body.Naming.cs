using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Box3D
{
    public partial struct Body
    {
        /// <summary>Sets this body's name. Names are stored in recordings and shown in debug output, and
        /// are how the visual replayer maps a replayed body back to a scene object — so keep them unique
        /// if you rely on that. box3d stores a short fixed-length name, so long names are truncated.</summary>
        public unsafe void SetName(string name)
        {
            name ??= "";
            byte[] buf = new byte[Encoding.UTF8.GetByteCount(name) + 1];
            Encoding.UTF8.GetBytes(name, 0, name.Length, buf, 0); // trailing 0 already present
            fixed (byte* p = buf)
            {
                UnsafeBindings.b3Body_SetName(Id, (sbyte*)p);
            }
        }

        /// <summary>This body's name, or "" if unset. Reflects box3d's truncation.</summary>
        public unsafe string GetName()
        {
            sbyte* p = UnsafeBindings.b3Body_GetName(Id);
            return p == null ? "" : Marshal.PtrToStringUTF8((IntPtr)p) ?? "";
        }
    }
}
