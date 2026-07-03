using System;
using Box3d;
using Unity.Mathematics;
using UnityEngine;

/// <summary>Cloth approximation on Box3d: a grid of small sphere bodies stitched with distance
/// joints (structural + shear), dropped so it drapes over a sphere obstacle. Heavy joint-solver
/// stress test, measured with <see cref="PhysicsPerfRecorder"/>. Twin: <see cref="UnityClothSandbox"/>.</summary>
public class Box3dClothSandbox : MonoBehaviour
{
    [SerializeField, Tooltip("Cloth nodes along X.")]
    private int Width = 16;

    [SerializeField, Tooltip("Cloth nodes along Z.")]
    private int Height = 16;

    [SerializeField, Tooltip("Rest distance between neighboring nodes.")]
    private float Spacing = 0.25f;

    [SerializeField, Tooltip("Collision radius of each cloth node.")]
    private float NodeRadius = 0.06f;

    [SerializeField, Tooltip("Add diagonal (shear) joints for shape stability.")]
    private bool ShearJoints = true;

    [SerializeField, Tooltip("Pin the corner nodes of the first row instead of free-dropping.")]
    private bool PinTopRow = false;

    [SerializeField, Tooltip("Box3d worker threads. 0 = auto (half the logical cores).")]
    private int WorkerCount = 0;

    [SerializeField, Tooltip("Steps skipped before measuring.")]
    private int WarmupSteps = 60;

    [SerializeField, Tooltip("Steps recorded into the perf CSV.")]
    private int MeasureSteps = 600;

    private World _world;
    private Body[] _nodes;
    private float3[] _positions;
    private ClothMeshVisual _visual;
    private PhysicsPerfRecorder _recorder;
    private readonly System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

    private void Start()
    {
        int workers = WorkerCount > 0 ? WorkerCount : Mathf.Max(1, SystemInfo.processorCount / 2);
        int jointCount = (Width - 1) * Height + Width * (Height - 1) +
                         (ShearJoints ? 2 * (Width - 1) * (Height - 1) : 0);
        _recorder = new PhysicsPerfRecorder("Box3dCloth", WarmupSteps, MeasureSteps,
            $"nodes,{Width * Height},joints,{jointCount},spacing,{Spacing},workers,{workers}");

        WorldDef worldDef = WorldDef.Default;
        worldDef.WorkerCount = (uint)workers;
        _world = World.Create(worldDef);

        CreateEnvironment();
        CreateClothGrid();
        _visual = new ClothMeshVisual(Width, Height);
    }

    private void CreateEnvironment()
    {
        Body ground = _world.CreateBody(BodyDef.Default);
        BoxHull hull = BoxHull.Create(10f, 0.5f, 10f);
        ground.CreateHullShape(ShapeDef.Default, in hull);

        GameObject groundVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        groundVisual.name = "Ground";
        groundVisual.transform.localScale = new Vector3(20f, 1f, 20f);
        Destroy(groundVisual.GetComponent<Collider>());

        BodyDef obstacleDef = BodyDef.Default;
        obstacleDef.Position = new float3(0f, 2f, 0f);
        Body obstacle = _world.CreateBody(obstacleDef);
        obstacle.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 1f });

        GameObject obstacleVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obstacleVisual.name = "Obstacle";
        obstacleVisual.transform.position = new Vector3(0f, 2f, 0f);
        obstacleVisual.transform.localScale = Vector3.one * 2f;
        Destroy(obstacleVisual.GetComponent<Collider>());
    }

    private void CreateClothGrid()
    {
        _nodes = new Body[Width * Height];
        _positions = new float3[Width * Height];

        float originX = -(Width - 1) * Spacing * 0.5f;
        float originZ = -(Height - 1) * Spacing * 0.5f;
        const float dropHeight = 4f;

        ShapeDef nodeShapeDef = ShapeDef.Default;
        // Negative group: cloth nodes never collide with each other, only with the environment.
        nodeShapeDef.Filter.GroupIndex = -1;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int index = y * Width + x;
                float3 position = new float3(originX + x * Spacing, dropHeight, originZ + y * Spacing);

                BodyDef nodeDef = BodyDef.Default;
                nodeDef.Type = PinTopRow && y == 0 ? BodyType.Static : BodyType.Dynamic;
                nodeDef.Position = position;
                nodeDef.UserData = (IntPtr)index;
                Body node = _world.CreateBody(nodeDef);
                node.CreateSphereShape(nodeShapeDef, new Sphere { Radius = NodeRadius });

                _nodes[index] = node;
                _positions[index] = position;
            }
        }

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (x + 1 < Width) Stitch(y * Width + x, y * Width + x + 1, Spacing);
                if (y + 1 < Height) Stitch(y * Width + x, (y + 1) * Width + x, Spacing);
                if (ShearJoints && x + 1 < Width && y + 1 < Height)
                {
                    float diagonal = Spacing * math.sqrt(2f);
                    Stitch(y * Width + x, (y + 1) * Width + x + 1, diagonal);
                    Stitch(y * Width + x + 1, (y + 1) * Width + x, diagonal);
                }
            }
        }
    }

    private void Stitch(int indexA, int indexB, float length)
    {
        DistanceJointDef def = DistanceJointDef.Default;
        def.Base.BodyIdA = _nodes[indexA].Id;
        def.Base.BodyIdB = _nodes[indexB].Id;
        def.Length = length;
        _world.CreateDistanceJoint(def);
    }

    private void FixedUpdate()
    {
        _stopwatch.Restart();
        _world.Step(Time.fixedDeltaTime);
        _stopwatch.Stop();
        _recorder.AddSample(_stopwatch.Elapsed.TotalMilliseconds);

        foreach (BodyMoveEvent moveEvent in _world.GetBodyMoveEvents())
        {
            _positions[(int)moveEvent.UserData] = moveEvent.Transform.Position;
        }
    }

    private void Update()
    {
        for (int i = 0; i < _positions.Length; i++)
        {
            _visual.SetVertex(i, _positions[i]);
        }
        _visual.Apply();
    }

    private void OnGUI()
    {
        string status = _recorder.IsFinished
            ? _recorder.Summary
            : $"Box3d cloth: {_recorder.RollingAverageMs:F3} ms avg ({Width}×{Height} nodes) — measuring…";
        GUI.Label(new Rect(10f, 10f, 1200f, 30f), status);
    }

    private void OnDestroy()
    {
        if (_world.IsValid) _world.Destroy();
    }
}
