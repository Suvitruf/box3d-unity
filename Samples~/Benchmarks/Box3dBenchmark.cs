using System;
using Box3d;
using Unity.Mathematics;
using UnityEngine;

/// <summary>Island-workload benchmark on Box3d. Pick the scenario and scale in the Inspector;
/// layouts come from <see cref="BenchmarkLayout"/> so runs match <see cref="UnityPhysXBenchmark"/>
/// exactly. Sleep is disabled so steady-state solving is measured, not idle sleeping bodies.</summary>
public class Box3dBenchmark : MonoBehaviour
{
    [SerializeField, Tooltip("Workload shape: many medium islands / many tiny islands / no contacts.")]
    private BenchmarkScenario Scenario = BenchmarkScenario.Piles;

    [SerializeField, Tooltip("Piles: piles per side (16 bodies each). Debris/FreeBodies: body count.")]
    private int Scale = 8;

    [SerializeField, Tooltip("Create render primitives (measurement excludes rendering either way).")]
    private bool RenderVisuals = true;

    [SerializeField, Tooltip("Box3d worker threads. 0 = auto (half the logical cores).")]
    private int WorkerCount = 0;

    [SerializeField, Tooltip("Steps skipped before measuring.")]
    private int WarmupSteps = 120;

    [SerializeField, Tooltip("Steps recorded into the perf CSV.")]
    private int MeasureSteps = 600;

    private World _world;
    private Transform[] _visuals;
    private PhysicsPerfRecorder _recorder;
    private readonly System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

    private void Start()
    {
        int workers = WorkerCount > 0 ? WorkerCount : Mathf.Max(1, SystemInfo.processorCount / 2);
        int bodyCount = BenchmarkLayout.BodyCount(Scenario, Scale);
        _recorder = new PhysicsPerfRecorder($"Box3dBench_{Scenario}", WarmupSteps, MeasureSteps,
            $"scenario,{Scenario},bodies,{bodyCount},workers,{workers}");

        WorldDef worldDef = WorldDef.Default;
        worldDef.WorkerCount = (uint)workers;
        worldDef.EnableSleep = false;
        if (Scenario == BenchmarkScenario.FreeBodies) worldDef.Gravity = float3.zero;
        _world = World.Create(worldDef);

        if (Scenario != BenchmarkScenario.FreeBodies) CreateGround();
        SpawnBodies();
    }

    private void CreateGround()
    {
        Body ground = _world.CreateBody(BodyDef.Default);
        BoxHull hull = BoxHull.Create(60f, 0.5f, 60f);
        ground.CreateHullShape(ShapeDef.Default, in hull);

        if (!RenderVisuals) return;
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Ground";
        visual.transform.localScale = new Vector3(120f, 1f, 120f);
        Destroy(visual.GetComponent<Collider>());
    }

    private void SpawnBodies()
    {
        BenchmarkSpawn[] spawns = BenchmarkLayout.Generate(Scenario, Scale);
        _visuals = RenderVisuals ? new Transform[spawns.Length] : null;

        for (int i = 0; i < spawns.Length; i++)
        {
            BenchmarkSpawn spawn = spawns[i];

            BodyDef bodyDef = BodyDef.Default;
            bodyDef.Type = BodyType.Dynamic;
            bodyDef.Position = (float3)spawn.Position;
            bodyDef.LinearVelocity = (float3)spawn.Velocity;
            bodyDef.UserData = (IntPtr)i;
            Body body = _world.CreateBody(bodyDef);

            float half = spawn.Size * 0.5f;
            switch (spawn.ShapeKind)
            {
                case 0:
                    body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = half });
                    break;
                case 1:
                    BoxHull hull = BoxHull.CreateCube(half);
                    body.CreateHullShape(ShapeDef.Default, in hull);
                    break;
                default:
                    float capsuleRadius = spawn.Size * 0.25f;
                    body.CreateCapsuleShape(ShapeDef.Default, new Capsule
                    {
                        Center1 = new float3(0f, -(half - capsuleRadius), 0f),
                        Center2 = new float3(0f, half - capsuleRadius, 0f),
                        Radius = capsuleRadius,
                    });
                    break;
            }

            if (!RenderVisuals) continue;
            _visuals[i] = CreateVisual(spawn);
        }
    }

    private static Transform CreateVisual(BenchmarkSpawn spawn)
    {
        PrimitiveType primitive = spawn.ShapeKind == 0 ? PrimitiveType.Sphere
            : spawn.ShapeKind == 1 ? PrimitiveType.Cube : PrimitiveType.Capsule;
        GameObject visual = GameObject.CreatePrimitive(primitive);
        visual.transform.position = spawn.Position;
        visual.transform.localScale = spawn.ShapeKind == 2
            ? new Vector3(spawn.Size * 0.5f, spawn.Size * 0.5f, spawn.Size * 0.5f)
            : Vector3.one * spawn.Size;
        UnityEngine.Object.Destroy(visual.GetComponent<Collider>());
        return visual.transform;
    }

    private void FixedUpdate()
    {
        _stopwatch.Restart();
        _world.Step(Time.fixedDeltaTime);
        _stopwatch.Stop();
        _recorder.AddSample(_stopwatch.Elapsed.TotalMilliseconds);

        if (_visuals == null) return;
        foreach (BodyMoveEvent moveEvent in _world.GetBodyMoveEvents())
        {
            int index = (int)moveEvent.UserData;
            _visuals[index].SetPositionAndRotation(moveEvent.Transform.Position, moveEvent.Transform.Rotation);
        }
    }

    private void OnGUI()
    {
        string status = _recorder.IsFinished
            ? _recorder.Summary
            : $"Box3d {Scenario}: {_recorder.RollingAverageMs:F3} ms avg — measuring…";
        GUI.Label(new Rect(10f, 10f, 1400f, 30f), status);
    }

    private void OnDestroy()
    {
        if (_world.IsValid) _world.Destroy();
    }
}
