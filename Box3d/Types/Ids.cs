using System;
using System.Runtime.InteropServices;

namespace Box3d
{
    // Mirrors of the opaque id handles in box3d id.h. Treated as opaque; passed by value.
    // Null = zero-initialized. Generation-validated by the engine (b3*_IsValid).

    /// <summary>Mirrors native b3WorldId (4 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WorldId : IEquatable<WorldId>
    {
        internal ushort Index1;
        internal ushort Generation;

        public bool IsNull => Index1 == 0;

        public uint ToUInt32()
        {
            return ((uint)Index1 << 16) | Generation;
        }

        public static WorldId FromUInt32(uint value)
        {
            return new WorldId { Index1 = (ushort)(value >> 16), Generation = (ushort)value };
        }

        public bool Equals(WorldId other)
        {
            return Index1 == other.Index1 && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)ToUInt32();
        }
    }

    /// <summary>Mirrors native b3BodyId (8 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BodyId : IEquatable<BodyId>
    {
        internal int Index1;
        internal ushort World0;
        internal ushort Generation;

        public bool IsNull => Index1 == 0;

        public ulong ToUInt64()
        {
            return ((ulong)(uint)Index1 << 32) | ((ulong)World0 << 16) | Generation;
        }

        public static BodyId FromUInt64(ulong value)
        {
            return new BodyId { Index1 = (int)(value >> 32), World0 = (ushort)(value >> 16), Generation = (ushort)value };
        }

        public bool Equals(BodyId other)
        {
            return Index1 == other.Index1 && World0 == other.World0 && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is BodyId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ToUInt64().GetHashCode();
        }
    }

    /// <summary>Mirrors native b3ShapeId (8 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ShapeId : IEquatable<ShapeId>
    {
        internal int Index1;
        internal ushort World0;
        internal ushort Generation;

        public bool IsNull => Index1 == 0;

        public ulong ToUInt64()
        {
            return ((ulong)(uint)Index1 << 32) | ((ulong)World0 << 16) | Generation;
        }

        public static ShapeId FromUInt64(ulong value)
        {
            return new ShapeId { Index1 = (int)(value >> 32), World0 = (ushort)(value >> 16), Generation = (ushort)value };
        }

        public bool Equals(ShapeId other)
        {
            return Index1 == other.Index1 && World0 == other.World0 && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is ShapeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ToUInt64().GetHashCode();
        }
    }

    /// <summary>Mirrors native b3JointId (8 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct JointId : IEquatable<JointId>
    {
        internal int Index1;
        internal ushort World0;
        internal ushort Generation;

        public bool IsNull => Index1 == 0;

        public ulong ToUInt64()
        {
            return ((ulong)(uint)Index1 << 32) | ((ulong)World0 << 16) | Generation;
        }

        public static JointId FromUInt64(ulong value)
        {
            return new JointId { Index1 = (int)(value >> 32), World0 = (ushort)(value >> 16), Generation = (ushort)value };
        }

        public bool Equals(JointId other)
        {
            return Index1 == other.Index1 && World0 == other.World0 && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is JointId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ToUInt64().GetHashCode();
        }
    }

    /// <summary>Mirrors native b3ContactId (12 bytes).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ContactId : IEquatable<ContactId>
    {
        internal int Index1;
        internal ushort World0;
        internal short Padding;
        internal uint Generation;

        public bool IsNull => Index1 == 0;

        public bool Equals(ContactId other)
        {
            return Index1 == other.Index1 && World0 == other.World0 && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is ContactId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Index1 ^ (World0 << 16) ^ (int)Generation;
        }
    }
}
