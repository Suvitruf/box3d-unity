using System;
using Unity.Mathematics;

namespace Box3D
{
    /// <summary>Deterministic hashing for lockstep / cross-environment determinism testing. Wraps box3d's
    /// own djb2 hash (<c>b3Hash</c>, provided by the engine "for determinism testing"), so the hash
    /// algorithm is bit-identical everywhere — only the float <em>values</em> it hashes can differ across
    /// builds or hardware, which is exactly what such a test is looking for.
    ///
    /// <para>Typical use: build a fixed scenario, and each step hash the same ordered set of bodies to get
    /// a per-step hash stream. Two runs of the same build must produce the same stream (lockstep); compare
    /// streams from Unity vs a standalone build/another machine to check cross-environment determinism.</para></summary>
    public static class Determinism
    {
        /// <summary>box3d's djb2 seed (<c>B3_HASH_INIT</c>). Start a hash chain from here.</summary>
        public const uint HashSeed = 5381;

        /// <summary>Hashes raw bytes with box3d's djb2, chaining from <paramref name="hash"/> so you can
        /// build a running hash across many calls.</summary>
        public static unsafe uint Hash(uint hash, ReadOnlySpan<byte> data)
        {
            if (data.Length == 0) return hash;
            fixed (byte* p = data)
            {
                return UnsafeBindings.b3Hash(hash, p, data.Length);
            }
        }

        /// <summary>Hashes raw bytes with box3d's djb2 from the default seed.</summary>
        public static uint Hash(ReadOnlySpan<byte> data) => Hash(HashSeed, data);

        /// <summary>A determinism hash over the bodies' positions and rotations (raw float bits), in the
        /// order given. The order must be identical across the runs you compare — pass the same fixed body
        /// list each time. Chains from <paramref name="hash"/> so you can fold in more state if needed.</summary>
        public static unsafe uint HashState(ReadOnlySpan<Body> bodies, uint hash = HashSeed)
        {
            float* buffer = stackalloc float[7]; // px py pz | qx qy qz qw
            for (int i = 0; i < bodies.Length; i++)
            {
                float3 position = bodies[i].Position;
                quaternion rotation = bodies[i].Rotation;
                buffer[0] = position.x;
                buffer[1] = position.y;
                buffer[2] = position.z;
                buffer[3] = rotation.value.x;
                buffer[4] = rotation.value.y;
                buffer[5] = rotation.value.z;
                buffer[6] = rotation.value.w;
                hash = UnsafeBindings.b3Hash(hash, (byte*)buffer, 7 * sizeof(float));
            }
            return hash;
        }
    }
}
