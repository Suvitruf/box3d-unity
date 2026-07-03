using System.Diagnostics;
using UnityEngine;

/// <summary>PhysX twin of <see cref="Box3dSandbox"/>: identical sphere rain (same seed, counts,
/// sizes) simulated by Unity's built-in physics. Switches PhysX to script-driven simulation so the
/// step cost can be measured the same way Box3d's is.</summary>
public class UnityPhysicsSandbox : MonoBehaviour
{
    [SerializeField, Tooltip("Number of spheres to drop.")]
    private int SphereCount = 100;

    [SerializeField, Tooltip("Sphere radius in meters.")]
    private float SphereRadius = 0.5f;

    [SerializeField, Tooltip("Spawn area half-extent on X/Z.")]
    private float SpawnRadius = 5f;

    [SerializeField, Tooltip("Height range spheres spawn in.")]
    private Vector2 SpawnHeight = new Vector2(5f, 25f);

    [SerializeField, Tooltip("Random seed so runs are comparable across physics engines.")]
    private int Seed = 12345;

    [SerializeField, Tooltip("Steps skipped before measuring (spawn/JIT noise).")]
    private int WarmupSteps = 60;

    [SerializeField, Tooltip("Steps recorded into the perf CSV.")]
    private int MeasureSteps = 600;

    private SimulationMode _previousSimulationMode;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private PhysicsPerfRecorder _recorder;

    private void OnEnable()
    {
        _previousSimulationMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
    }

    private void OnDisable()
    {
        Physics.simulationMode = _previousSimulationMode;
    }

    private void Start()
    {
        Random.InitState(Seed);

        _recorder = new PhysicsPerfRecorder("UnityPhysX", WarmupSteps, MeasureSteps,
            $"spheres,{SphereCount},radius,{SphereRadius},seed,{Seed},timestep,{Time.fixedDeltaTime}," +
            $"jobWorkers,{Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount}");

        CreateGround();
        CreateSpheres();
    }

    private void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(20f, 1f, 20f);
    }

    private void CreateSpheres()
    {
        for (int i = 0; i < SphereCount; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(-SpawnRadius, SpawnRadius),
                Random.Range(SpawnHeight.x, SpawnHeight.y),
                Random.Range(-SpawnRadius, SpawnRadius));

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"Sphere {i}";
            sphere.transform.localScale = Vector3.one * (SphereRadius * 2f);
            sphere.transform.position = position;
            sphere.AddComponent<Rigidbody>();
        }
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
            : $"Unity PhysX step: {_recorder.RollingAverageMs:F3} ms avg ({SphereCount} spheres) — measuring…";
        GUI.Label(new Rect(10f, 10f, 1200f, 30f), status);
    }
}
