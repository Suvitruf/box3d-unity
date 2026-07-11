namespace Box3d
{
    public partial struct World
    {
        /// <summary>Per-phase timing breakdown of the last <see cref="Step"/> (milliseconds). Use it to
        /// find which phase dominates the step cost. Native b3World_GetProfile.</summary>
        public unsafe Profile GetProfile()
        {
            // b3Profile is 23 sequential floats, layout-identical to Profile — reinterpret the copy.
            b3Profile p = UnsafeBindings.b3World_GetProfile(Id);
            return *(Profile*)&p;
        }

        /// <summary>Live body/shape/contact/joint/island counts and allocator + broadphase stats after
        /// the last step. Native b3World_GetCounters (scalar fields).</summary>
        public unsafe Counters GetCounters()
        {
            b3Counters c = UnsafeBindings.b3World_GetCounters(Id);
            return new Counters
            {
                BodyCount = c.bodyCount,
                ShapeCount = c.shapeCount,
                ContactCount = c.contactCount,
                JointCount = c.jointCount,
                IslandCount = c.islandCount,
                StackUsed = c.stackUsed,
                ArenaCapacity = c.arenaCapacity,
                StaticTreeHeight = c.staticTreeHeight,
                TreeHeight = c.treeHeight,
                SatCallCount = c.satCallCount,
                SatCacheHitCount = c.satCacheHitCount,
                ByteCount = c.byteCount,
                TaskCount = c.taskCount,
                AwakeContactCount = c.awakeContactCount,
                RecycledContactCount = c.recycledContactCount,
                DistanceIterations = c.distanceIterations,
                PushBackIterations = c.pushBackIterations,
                RootIterations = c.rootIterations,
            };
        }
    }
}
