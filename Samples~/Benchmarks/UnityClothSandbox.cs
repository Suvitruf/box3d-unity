using System.Diagnostics;
using UnityEngine;

/// <summary>PhysX twin of <see cref="Box3dClothSandbox"/>: the same node grid stitched with
/// linear-limited ConfigurableJoints, draped over the same sphere obstacle, measured the same way.</summary>
public class UnityClothSandbox : MonoBehaviour
{
    private const int ClothLayer = 4; // built-in "Water" layer, repurposed to disable self-collision

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

    [SerializeField, Tooltip("Steps skipped before measuring.")]
    private int WarmupSteps = 60;

    [SerializeField, Tooltip("Steps recorded into the perf CSV.")]
    private int MeasureSteps = 600;

    private Rigidbody[] _nodes;
    private ClothMeshVisual _visual;
    private PhysicsPerfRecorder _recorder;
    private SimulationMode _previousSimulationMode;
    private readonly Stopwatch _stopwatch = new Stopwatch();

    private void OnEnable()
    {
        _previousSimulationMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        Physics.IgnoreLayerCollision(ClothLayer, ClothLayer, true);
    }

    private void OnDisable()
    {
        Physics.simulationMode = _previousSimulationMode;
        Physics.IgnoreLayerCollision(ClothLayer, ClothLayer, false);
    }

    private void Start()
    {
        int jointCount = (Width - 1) * Height + Width * (Height - 1) +
                         (ShearJoints ? 2 * (Width - 1) * (Height - 1) : 0);
        _recorder = new PhysicsPerfRecorder("UnityPhysXCloth", WarmupSteps, MeasureSteps,
            $"nodes,{Width * Height},joints,{jointCount},spacing,{Spacing}," +
            $"jobWorkers,{Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount}");

        CreateEnvironment();
        CreateClothGrid();
        _visual = new ClothMeshVisual(Width, Height);
    }

    private void CreateEnvironment()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(20f, 1f, 20f);

        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obstacle.name = "Obstacle";
        obstacle.transform.position = new Vector3(0f, 2f, 0f);
        obstacle.transform.localScale = Vector3.one * 2f;
    }

    private void CreateClothGrid()
    {
        _nodes = new Rigidbody[Width * Height];

        float originX = -(Width - 1) * Spacing * 0.5f;
        float originZ = -(Height - 1) * Spacing * 0.5f;
        const float dropHeight = 4f;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int index = y * Width + x;
                var node = new GameObject($"Node {index}");
                node.layer = ClothLayer;
                node.transform.position = new Vector3(originX + x * Spacing, dropHeight, originZ + y * Spacing);

                SphereCollider collider = node.AddComponent<SphereCollider>();
                collider.radius = NodeRadius;

                Rigidbody body = node.AddComponent<Rigidbody>();
                body.mass = 0.05f;
                body.solverIterations = 8;
                body.isKinematic = PinTopRow && y == 0;
                _nodes[index] = body;
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
                    float diagonal = Spacing * Mathf.Sqrt(2f);
                    Stitch(y * Width + x, (y + 1) * Width + x + 1, diagonal);
                    Stitch(y * Width + x + 1, (y + 1) * Width + x, diagonal);
                }
            }
        }
    }

    private void Stitch(int indexA, int indexB, float length)
    {
        ConfigurableJoint joint = _nodes[indexA].gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = _nodes[indexB];
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;
        joint.linearLimit = new SoftJointLimit { limit = length };
        joint.anchor = Vector3.zero;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = Vector3.zero;
        joint.enableCollision = false;
    }

    private void FixedUpdate()
    {
        _stopwatch.Restart();
        Physics.Simulate(Time.fixedDeltaTime);
        _stopwatch.Stop();
        _recorder.AddSample(_stopwatch.Elapsed.TotalMilliseconds);
    }

    private void Update()
    {
        for (int i = 0; i < _nodes.Length; i++)
        {
            _visual.SetVertex(i, _nodes[i].transform.position);
        }
        _visual.Apply();
    }

    private void OnGUI()
    {
        string status = _recorder.IsFinished
            ? _recorder.Summary
            : $"Unity PhysX cloth: {_recorder.RollingAverageMs:F3} ms avg ({Width}×{Height} nodes) — measuring…";
        GUI.Label(new Rect(10f, 10f, 1200f, 30f), status);
    }
}
