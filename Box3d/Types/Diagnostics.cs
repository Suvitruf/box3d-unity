using System.Runtime.InteropServices;

namespace Box3d
{
    /// <summary>Per-phase timing breakdown of the last <see cref="World.Step"/>, in milliseconds.
    /// Mirrors native b3Profile; get it via <see cref="World.GetProfile"/>. The sub-phases roughly sum
    /// to <see cref="Step"/> — use it to see which phase (broadphase, narrowphase, solve, …) dominates.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Profile
    {
        /// <summary>Total step time.</summary>
        public float Step;

        /// <summary>Broadphase pair finding.</summary>
        public float Pairs;

        /// <summary>Narrowphase collision (manifold generation).</summary>
        public float Collide;

        /// <summary>Total constraint solve (the sub-phases below break this down).</summary>
        public float Solve;

        public float SolverSetup;
        public float Constraints;
        public float PrepareConstraints;
        public float IntegrateVelocities;
        public float WarmStart;
        public float SolveImpulses;
        public float IntegratePositions;
        public float RelaxImpulses;
        public float ApplyRestitution;
        public float StoreImpulses;
        public float SplitIslands;
        public float Transforms;
        public float SensorHits;
        public float JointEvents;
        public float HitEvents;

        /// <summary>Broadphase tree refit.</summary>
        public float Refit;

        /// <summary>Continuous collision (bullets / CCD).</summary>
        public float Bullets;

        public float SleepIslands;
        public float Sensors;
    }

    /// <summary>Live world counts and allocator/broadphase stats after the last step. Mirrors the scalar
    /// fields of native b3Counters; get it via <see cref="World.GetCounters"/>. (The advanced
    /// constraint-graph color and manifold-histogram arrays are not surfaced.)</summary>
    public struct Counters
    {
        public int BodyCount;
        public int ShapeCount;
        public int ContactCount;
        public int JointCount;
        public int IslandCount;

        /// <summary>Bytes of the arena/stack allocator currently in use.</summary>
        public int StackUsed;

        /// <summary>Arena allocator capacity.</summary>
        public int ArenaCapacity;

        public int StaticTreeHeight;
        public int TreeHeight;

        /// <summary>Separating-axis narrowphase call count and cache hits.</summary>
        public int SatCallCount;
        public int SatCacheHitCount;

        /// <summary>Total bytes allocated by the world.</summary>
        public int ByteCount;

        public int TaskCount;
        public int AwakeContactCount;
        public int RecycledContactCount;

        /// <summary>Peak CCD/TOI iteration counts this step.</summary>
        public int DistanceIterations;
        public int PushBackIterations;
        public int RootIterations;
    }

    /// <summary>Broadphase-tree work done by a spatial query — how many internal and leaf nodes it
    /// visited. Lower is a tighter query. Filled by the <c>out TreeStats</c> query overloads.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TreeStats
    {
        public int NodeVisits;
        public int LeafVisits;
    }
}
