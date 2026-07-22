using Unity.Mathematics;

namespace Box3d
{
    /// <summary>A fixed, randomness-free physics scenario for determinism testing. The same math runs in
    /// every environment (the native library does it), so its per-step state-hash stream must match across
    /// repeated runs on one build, and — if the platform's float math agrees — across platforms too.
    ///
    /// <para>Shared by the determinism unit test and the cross-platform harness so both check the exact
    /// same simulation. It uses only the low-level API (no <c>UnityEngine</c> types), fixed timestep, and a
    /// single worker, so nothing frame-rate- or thread-count-dependent leaks in.</para></summary>
    public static class DeterminismScenario
    {
        public const float TimeStep = 1f / 60f;
        public const int SubStepCount = 4;
        public const int StepCount = 120;
        public const int BoxCount = 10;

        /// <summary>Runs the scenario and returns the per-step world-state hash (position + rotation of
        /// every body). Deterministic on a given build; compare streams to check determinism.</summary>
        public static uint[] Run()
        {
            WorldDef worldDef = WorldDef.Default;
            worldDef.Gravity = new float3(0f, -10f, 0f);
            worldDef.WorkerCount = 1; // single-threaded baseline: removes the constraint-graph variable
            World world = World.Create(worldDef);
            try
            {
                var bodies = new Body[BoxCount + 1];

                Body ground = world.CreateBody(BodyDef.Default);
                BoxHull groundHull = BoxHull.Create(10f, 0.5f, 10f);
                ground.CreateHullShape(ShapeDef.Default, in groundHull);
                bodies[0] = ground;

                BoxHull boxHull = BoxHull.CreateCube(0.5f);
                for (int i = 0; i < BoxCount; i++)
                {
                    BodyDef def = BodyDef.Default;
                    def.Type = BodyType.Dynamic;
                    def.Position = new float3(0.05f * i, 1.0f + 1.05f * i, 0f); // slight lean → non-trivial settling
                    Body box = world.CreateBody(def);
                    box.CreateHullShape(ShapeDef.Default, in boxHull);
                    bodies[i + 1] = box;
                }

                var hashes = new uint[StepCount];
                for (int step = 0; step < StepCount; step++)
                {
                    world.Step(TimeStep, SubStepCount);
                    hashes[step] = Determinism.HashState(bodies);
                }
                return hashes;
            }
            finally
            {
                world.Destroy();
            }
        }
    }
}
