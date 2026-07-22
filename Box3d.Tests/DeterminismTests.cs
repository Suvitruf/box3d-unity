using NUnit.Framework;

namespace Box3d.Tests
{
    /// <summary>Determinism / lockstep tests: the same build must reproduce a simulation bit-for-bit.
    /// The scenario is deliberately kept to the low-level API (no UnityEngine types) so the exact same
    /// code can be compiled into a standalone harness and its hash stream compared against Unity's —
    /// the cross-environment / cross-hardware determinism check.</summary>
    public class DeterminismTests
    {
        [Test]
        public void SameBuild_RepeatedRuns_ProduceIdenticalHashStream()
        {
            uint[] runA = DeterminismScenario.Run();
            uint[] runB = DeterminismScenario.Run();

            Assert.AreEqual(runA.Length, runB.Length);
            for (int step = 0; step < runA.Length; step++)
            {
                Assert.AreEqual(runA[step], runB[step],
                    $"determinism broke at step {step}: 0x{runA[step]:X8} vs 0x{runB[step]:X8}");
            }

            // This value is deterministic on THIS build. Compare it to a standalone harness or another
            // machine to check cross-environment determinism — a difference there is the cross-hardware
            // float signal, not a bug in this test. Pin it in an Assert to regression-test a single build.
            TestContext.WriteLine($"Final world-state hash: 0x{runA[^1]:X8}");
        }

        [Test]
        public void HashState_IsStableForUnchangedState()
        {
            World world = World.Create(WorldDef.Default);
            try
            {
                Body ground = world.CreateBody(BodyDef.Default);
                BoxHull hull = BoxHull.Create(1f, 1f, 1f);
                ground.CreateHullShape(ShapeDef.Default, in hull);

                var bodies = new[] { ground };
                Assert.AreEqual(Determinism.HashState(bodies), Determinism.HashState(bodies),
                    "hashing the same state twice must match");
            }
            finally
            {
                world.Destroy();
            }
        }
    }
}
