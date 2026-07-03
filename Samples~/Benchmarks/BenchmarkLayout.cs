using UnityEngine;

/// <summary>Island-workload benchmark scenarios.</summary>
public enum BenchmarkScenario
{
    /// <summary>Grid of separate stacks — many medium contact islands under persistent load.</summary>
    Piles,

    /// <summary>Thousands of small bodies resting on the ground — many tiny contact islands.</summary>
    Debris,

    /// <summary>Bodies drifting in zero gravity, never touching — pure integration + broadphase.</summary>
    FreeBodies,
}

/// <summary>One spawned body: 0 = sphere, 1 = box, 2 = capsule.</summary>
public struct BenchmarkSpawn
{
    public int ShapeKind;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Size;
}

/// <summary>Generates identical seeded spawn layouts for the Box3d and PhysX benchmark twins.
/// Both engines MUST consume the same array so runs are directly comparable.</summary>
public static class BenchmarkLayout
{
    public const int Seed = 4242;

    public static BenchmarkSpawn[] Generate(BenchmarkScenario scenario, int scale)
    {
        Random.InitState(Seed);
        switch (scenario)
        {
            case BenchmarkScenario.Piles: return GeneratePiles(pilesPerSide: scale);
            case BenchmarkScenario.Debris: return GenerateDebris(count: scale);
            default: return GenerateFreeBodies(count: scale);
        }
    }

    /// <summary>Human-readable body count for a scenario/scale, for CSV metadata.</summary>
    public static int BodyCount(BenchmarkScenario scenario, int scale)
    {
        return scenario == BenchmarkScenario.Piles ? scale * scale * BodiesPerPile : scale;
    }

    private const int BodiesPerPile = 16;

    private static BenchmarkSpawn[] GeneratePiles(int pilesPerSide)
    {
        const float pileSpacing = 6f;
        const float size = 0.5f;
        var spawns = new BenchmarkSpawn[pilesPerSide * pilesPerSide * BodiesPerPile];

        int index = 0;
        float origin = -(pilesPerSide - 1) * pileSpacing * 0.5f;
        for (int px = 0; px < pilesPerSide; px++)
        {
            for (int pz = 0; pz < pilesPerSide; pz++)
            {
                for (int i = 0; i < BodiesPerPile; i++)
                {
                    spawns[index++] = new BenchmarkSpawn
                    {
                        ShapeKind = i % 2,
                        Position = new Vector3(
                            origin + px * pileSpacing + Random.Range(-0.03f, 0.03f),
                            0.55f + i * (size + 0.05f),
                            origin + pz * pileSpacing + Random.Range(-0.03f, 0.03f)),
                        Velocity = Vector3.zero,
                        Size = size,
                    };
                }
            }
        }
        return spawns;
    }

    private static BenchmarkSpawn[] GenerateDebris(int count)
    {
        const float area = 40f;
        var spawns = new BenchmarkSpawn[count];
        for (int i = 0; i < count; i++)
        {
            spawns[i] = new BenchmarkSpawn
            {
                ShapeKind = i % 3,
                Position = new Vector3(
                    Random.Range(-area, area),
                    Random.Range(0.5f, 3f),
                    Random.Range(-area, area)),
                Velocity = Vector3.zero,
                Size = Random.Range(0.15f, 0.45f),
            };
        }
        return spawns;
    }

    private static BenchmarkSpawn[] GenerateFreeBodies(int count)
    {
        const float volume = 60f;
        var spawns = new BenchmarkSpawn[count];
        for (int i = 0; i < count; i++)
        {
            spawns[i] = new BenchmarkSpawn
            {
                ShapeKind = i % 3,
                Position = new Vector3(
                    Random.Range(-volume, volume),
                    Random.Range(20f, 20f + volume),
                    Random.Range(-volume, volume)),
                Velocity = new Vector3(
                    Random.Range(-5f, 5f),
                    Random.Range(-5f, 5f),
                    Random.Range(-5f, 5f)),
                Size = 0.4f,
            };
        }
        return spawns;
    }
}
