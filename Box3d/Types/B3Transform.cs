using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Box3d
{
    /// <summary>Mirrors native b3Transform / b3WorldTransform (single precision, 28 bytes):
    /// position + rotation. b3Quat memory order is x,y,z,w — identical to Unity.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct B3Transform
    {
        public float3 Position;
        public quaternion Rotation;

        public static B3Transform Identity => new B3Transform { Position = float3.zero, Rotation = quaternion.identity };
    }
}
