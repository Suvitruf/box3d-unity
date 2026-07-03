using System.Diagnostics;
using UnityEngine;

/// <summary>PhysX twin of <see cref="Box3dBenchmark"/>: identical scenarios and layouts from
/// <see cref="BenchmarkLayout"/>, script-driven simulation, per-body sleep disabled.</summary>
public class UnityPhysXBenchmark : MonoBehaviour
{
    [SerializeField, Tooltip("Workload shape: many medium islands / many tiny islands / no contacts.")]
    private BenchmarkScenario Scenario = BenchmarkScenario.Piles;

    [SerializeField, Tooltip("Piles: piles per side (16 bodies each). Debris/FreeBodies: body count.")]
    private int Scale = 8;

    [SerializeField, Tooltip("Create render primitives (measurement excludes rendering either way).")]
    private bool RenderVisuals = true;

    [SerializeField, Tooltip("Steps skipped before measuring.")]
    private int WarmupSteps = 120;

    [SerializeField, Tooltip("Steps recorded into the perf CSV.")]
    private int MeasureSteps = 600;

    private PhysicsPerfRecorder _recorder;
    private SimulationMode _previousSimulationMode;
    private Vector3 _previousGravity;
    private readonly Stopwatch _stopwatch = new Stopwatch();

    private void OnEnable()
    {
        _previousSimulationMode = Physics.simulationMode;
        _previousGravity = Physics.gravity;
        Physics.simulationMode = SimulationMode.Script;
    }

    private void OnDisable()
    {
        Physics.simulationMode = _previousSimulationMode;
        Physics.gravity = _previousGravity;
    }

    private void Start()
    {
        int bodyCount = BenchmarkLayout.BodyCount(Scenario, Scale);
        _recorder = new PhysicsPerfRecorder($"UnityPhysXBench_{Scenario}", WarmupSteps, MeasureSteps,
            $"scenario,{Scenario},bodies,{bodyCount}," +
            $"jobWorkers,{Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount}");

        if (Scenario == BenchmarkScenario.FreeBodies) Physics.gravity = Vector3.zero;
        else CreateGround();
        SpawnBodies();
    }

    private void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(120f, 1f, 120f);
        if (!RenderVisuals) ground.GetComponent<MeshRenderer>().enabled = false;
    }

    private void SpawnBodies()
    {
        BenchmarkSpawn[] spawns = BenchmarkLayout.Generate(Scenario, Scale);

        for (int i = 0; i < spawns.Length; i++)
        {
            BenchmarkSpawn spawn = spawns[i];
            float half = spawn.Size * 0.5f;

            var node = new GameObject($"Body {i}");
            node.transform.position = spawn.Position;

            switch (spawn.ShapeKind)
            {
                case 0:
                    node.AddComponent<SphereCollider>().radius = half;
                    break;
                case 1:
                    node.AddComponent<BoxCollider>().size = Vector3.one * spawn.Size;
                    break;
                default:
                    CapsuleCollider capsule = node.AddComponent<CapsuleCollider>();
                    capsule.radius = spawn.Size * 0.25f;
                    capsule.height = spawn.Size;
                    break;
            }

            Rigidbody body = node.AddComponent<Rigidbody>();
            body.linearVelocity = spawn.Velocity;
            body.sleepThreshold = 0f; // benchmark steady-state solving, not sleeping

            if (RenderVisuals) CreateVisual(spawn, node.transform);
        }
    }

    private static void CreateVisual(BenchmarkSpawn spawn, Transform parent)
    {
        PrimitiveType primitive = spawn.ShapeKind == 0 ? PrimitiveType.Sphere
            : spawn.ShapeKind == 1 ? PrimitiveType.Cube : PrimitiveType.Capsule;
        GameObject visual = GameObject.CreatePrimitive(primitive);
        Object.Destroy(visual.GetComponent<Collider>());
        visual.transform.SetParent(parent, worldPositionStays: false);
        visual.transform.localScale = spawn.ShapeKind == 2
            ? new Vector3(spawn.Size * 0.5f, spawn.Size * 0.5f, spawn.Size * 0.5f)
            : Vector3.one * spawn.Size;
    }

    private void FixedUpdate()
    {
        _stopwatch.Restart();
        Physics.Simulate(Time.fixedDeltaTime);
        _stopwatch.Stop();
        _recorder.AddSample(_stopwatch.Elapsed.TotalMilliseconds);
    }

    private void OnGUI()
    {
        string status = _recorder.IsFinished
            ? _recorder.Summary
            : $"PhysX {Scenario}: {_recorder.RollingAverageMs:F3} ms avg — measuring…";
        GUI.Label(new Rect(10f, 10f, 1400f, 30f), status);
    }
}
