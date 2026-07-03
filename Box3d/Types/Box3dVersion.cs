using System.Runtime.InteropServices;

namespace Box3d
{
    /// <summary>Mirrors native b3Version (base.h). SemVer version of the loaded Box3d library.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Box3dVersion
    {
        public int Major;
        public int Minor;
        public int Revision;

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Revision}";
        }
    }
}
