using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Box3D
{
    /// <summary>A WORLD transform — box3d's <c>b3WorldTransform</c>: a world position
    /// (<see cref="B3Pos"/> — double precision under <c>BOX3D_DOUBLE</c>) plus an always-float
    /// rotation quaternion. Distinct from <see cref="B3Transform"/>, which is a RELATIVE transform
    /// (shape-local frames, joint frames) and stays float in both modes. In single precision the
    /// two are layout-identical (28 bytes) and convert implicitly; in double mode this is 40 bytes
    /// and narrowing to <see cref="B3Transform"/> is an explicit, lossy operation.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct B3WorldTransform
    {
        public B3Pos Position;
        public quaternion Rotation;

        public static B3WorldTransform Identity => new B3WorldTransform { Position = B3Pos.Zero, Rotation = quaternion.identity };

#if !BOX3D_DOUBLE
        public static implicit operator B3Transform(B3WorldTransform t)
            => new B3Transform { Position = t.Position, Rotation = t.Rotation };
        public static implicit operator B3WorldTransform(B3Transform t)
            => new B3WorldTransform { Position = t.Position, Rotation = t.Rotation };

        /// <summary>As a float <see cref="B3Transform"/> (no-op in single precision, narrowing in double).</summary>
        public B3Transform ToB3Transform() => this;
#else
        // Widening (lossless) — implicit.
        public static implicit operator B3WorldTransform(B3Transform t)
            => new B3WorldTransform { Position = t.Position, Rotation = t.Rotation };

        // Narrowing (LOSSY translation far from the origin) — explicit on purpose.
        public static explicit operator B3Transform(B3WorldTransform t) => t.ToB3Transform();

        /// <summary>Narrows the translation to a float <see cref="B3Transform"/> — LOSSY far from
        /// the origin. For Unity Transform sync, convert relative to a render origin instead.</summary>
        public B3Transform ToB3Transform()
            => new B3Transform { Position = Position.ToFloat3(), Rotation = Rotation };
#endif
    }
}
