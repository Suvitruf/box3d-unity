using System.Collections.Generic;
using System.Diagnostics;
using Box3d;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Debug = UnityEngine.Debug;

/// <summary>Erin Catto's box3d pyramid stress test in Unity: a triangular pyramid of unit boxes,
/// one box deep, kept stable by box3d's contact recycling (persistent contact ids → warm starting).
/// Default 180 rows = 180·181/2 = 16,290 boxes, matching the original demo.
///
/// Physics runs on the raw box3d API (no per-box GameObject); the boxes are drawn with GPU instancing
/// so RENDERING never becomes the bottleneck and the reported step time is pure physics. Live metrics
/// on-screen (avg / peak step ms, FPS, body count); a full run also writes a CSV via
/// PhysicsPerfRecorder. Attach to a GameObject in an empty scene and press Play.</summary>
public class PyramidBenchmark : MonoBehaviour
{
    [SerializeField, Tooltip("Rows in the pyramid. N rows = N·(N+1)/2 boxes (180 → 16,290).")]
    private int Rows = 180;

    [SerializeField, Tooltip("Box edge length.")]
    private float BoxSize = 1f;

    [SerializeField, Tooltip("Extra horizontal gap between boxes (0 = flush).")]
    private float Gap = 0.02f;

    [SerializeField, Tooltip("Warm-up steps excluded from the CSV.")]
    private int WarmupSteps = 120;

    [SerializeField, Tooltip("Measured steps written to the CSV.")]
    private int MeasureSteps = 600;

    [SerializeField, Min(0), Tooltip("Physics worker threads (box3d's built-in scheduler). 0 = auto (~half the logical cores). 1 = single-threaded.")]
    private int WorkerThreads = 0;

    [Header("Throwing (left-click)")]
    [SerializeField, Tooltip("Launch speed of thrown spheres (m/s).")]
    private float ShootSpeed = 90f;

    [SerializeField, Tooltip("Radius of thrown spheres.")]
    private float SphereRadius = 2.5f;

    [SerializeField, Tooltip("Density of thrown spheres — heavier smashes harder.")]
    private float SphereDensity = 4000f;

    [SerializeField, Min(1), Tooltip("Max live thrown spheres; the oldest is removed past this.")]
    private int MaxSpheres = 40;

    private World _world;
    private readonly StepStats _stepStats = new StepStats(120);
    private PhysicsPerfRecorder _recorder;
    private readonly Stopwatch _stopwatch = new Stopwatch();

    private int _bodyCount;
    private int _workers;
    private int _stepCount;
    private Mesh _cubeMesh;
    private Material _material;
    private Material _sphereMaterial;
    private Matrix4x4[][] _batches;   // GPU-instancing batches (≤1023 matrices each)
    private int[] _batchCounts;
    private const int BatchSize = 1023;

    // Thrown spheres are few — plain GameObjects synced to their bodies (no instancing needed).
    private readonly List<(Body body, Transform visual)> _spheres = new List<(Body, Transform)>();

    private void Start()
    {
        WorldDef worldDef = WorldDef.Default;
        worldDef.WorkerCount = (uint)(WorkerThreads > 0 ? WorkerThreads : Mathf.Max(1, SystemInfo.processorCount / 2));
        _workers = (int)worldDef.WorkerCount;
        _world = World.Create(worldDef);
        BuildRenderResources();
        BuildPyramid();
        SetupCameraAndLight();

        _recorder = new PhysicsPerfRecorder("box3d", WarmupSteps, MeasureSteps,
            $"pyramid {Rows} rows, {_bodyCount} boxes");
        Debug.Log($"[Pyramid] {Rows} rows, {_bodyCount} boxes created.");
    }

    private void BuildPyramid()
    {
        float step = BoxSize + Gap;
        float half = BoxSize * 0.5f;

        // Static ground: center at y = -0.5 so its top face sits at y = 0.
        BodyDef groundDef = BodyDef.Default;
        groundDef.Position = new float3(0f, -0.5f, 0f);
        Body ground = _world.CreateBody(groundDef);
        float baseHalfWidth = Rows * step * 0.5f + BoxSize;
        BoxHull groundHull = BoxHull.Create(baseHalfWidth, 0.5f, BoxSize);
        ground.CreateHullShape(ShapeDef.Default, in groundHull);

        BoxHull boxHull = BoxHull.Create(half, half, half);

        var matrices = new List<Matrix4x4>(Rows * (Rows + 1) / 2);
        int index = 0;
        // Level L=0 is the base (Rows boxes); L=Rows-1 is the apex (1 box).
        for (int level = 0; level < Rows; level++)
        {
            int countInRow = Rows - level;
            float rowLeft = -(countInRow - 1) * step * 0.5f; // centered; each level offset by step/2
            float y = level * BoxSize + half;                 // flush vertical stack, base on the ground
            for (int c = 0; c < countInRow; c++)
            {
                var pos = new float3(rowLeft + c * step, y, 0f);

                BodyDef def = BodyDef.Default;
                def.Type = BodyType.Dynamic;
                def.Position = pos;
                def.UserData = (System.IntPtr)index; // move events map back to the instance index
                Body body = _world.CreateBody(def);
                body.CreateHullShape(ShapeDef.Default, in boxHull);

                matrices.Add(Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * BoxSize));
                index++;
            }
        }

        _bodyCount = index;
        BuildBatches(matrices);
    }

    // Slice the initial transforms into fixed instancing batches; move events update them in place.
    private void BuildBatches(List<Matrix4x4> matrices)
    {
        int batchTotal = (_bodyCount + BatchSize - 1) / BatchSize;
        _batches = new Matrix4x4[batchTotal][];
        _batchCounts = new int[batchTotal];
        for (int b = 0; b < batchTotal; b++)
        {
            int start = b * BatchSize;
            int size = Mathf.Min(BatchSize, _bodyCount - start);
            _batches[b] = new Matrix4x4[size];
            _batchCounts[b] = size;
            for (int i = 0; i < size; i++) _batches[b][i] = matrices[start + i];
        }
    }

    private void FixedUpdate()
    {
        if (!_world.IsValid) return;

        _stopwatch.Restart();
        _world.Step(Time.fixedDeltaTime);
        _stopwatch.Stop();

        double ms = _stopwatch.Elapsed.TotalMilliseconds;
        _stepStats.Add(ms);
        _recorder.AddSample(ms);
        _stepCount++;

        // Update only the boxes that moved this step (settled/sleeping boxes emit nothing).
        foreach (BodyMoveEvent move in _world.GetBodyMoveEvents())
        {
            int index = (int)move.UserData;
            var m = Matrix4x4.TRS((Vector3)move.Transform.Position, (Quaternion)move.Transform.Rotation,
                Vector3.one * BoxSize);
            _batches[index / BatchSize][index % BatchSize] = m;
        }
    }

    private void Update()
    {
        // Shadows off: 16k instanced shadow-casters would dominate the frame and aren't the metric.
        for (int b = 0; b < _batches.Length; b++)
        {
            Graphics.DrawMeshInstanced(_cubeMesh, 0, _material, _batches[b], _batchCounts[b],
                null, ShadowCastingMode.Off, false);
        }

        foreach ((Body body, Transform visual) in _spheres)
        {
            B3Transform t = body.Transform;
            visual.SetPositionAndRotation(t.Position, t.Rotation);
        }

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ShootSphere();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    // Launch a heavy bullet sphere from the camera through the cursor into the pyramid.
    private void ShootSphere()
    {
        Camera cam = Camera.main;
        if (!cam || !_world.IsValid) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        float3 start = ray.origin + ray.direction * 3f; // clear of the near plane

        BodyDef def = BodyDef.Default;
        def.Type = BodyType.Dynamic;
        def.Position = start;
        def.LinearVelocity = (float3)(ray.direction * ShootSpeed);
        def.IsBullet = true; // CCD — a fast ball must not tunnel through the one-deep wall
        Body body = _world.CreateBody(def);

        ShapeDef shapeDef = ShapeDef.Default;
        shapeDef.Density = SphereDensity;
        body.CreateSphereShape(shapeDef, new Sphere { Radius = SphereRadius });

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Thrown sphere";
        Destroy(go.GetComponent<Collider>());
        go.transform.localScale = Vector3.one * (SphereRadius * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = _sphereMaterial;
        _spheres.Add((body, go.transform));

        if (_spheres.Count > MaxSpheres)
        {
            (Body oldBody, Transform oldVisual) = _spheres[0];
            _spheres.RemoveAt(0);
            if (oldBody.IsValid) oldBody.Destroy();
            if (oldVisual) Destroy(oldVisual.gameObject);
        }
    }
#endif

    private void BuildRenderResources()
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _cubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        if (!shader) shader = Shader.Find("Sprites/Default");
        _material = new Material(shader) { enableInstancing = true };
        var tint = new Color(0.45f, 0.6f, 0.85f);
        _material.color = tint;                       // Standard / built-in
        if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", tint); // URP/Lit

        _sphereMaterial = new Material(shader);
        var red = new Color(0.85f, 0.25f, 0.2f);
        _sphereMaterial.color = red;
        if (_sphereMaterial.HasProperty("_BaseColor")) _sphereMaterial.SetColor("_BaseColor", red);
    }

    private void SetupCameraAndLight()
    {
        float span = Rows * (BoxSize + Gap);
        Camera cam = Camera.main;
        if (!cam)
        {
            cam = new GameObject("Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.transform.position = new Vector3(0f, span * 0.5f, -span * 1.5f);
        cam.transform.LookAt(new Vector3(0f, span * 0.35f, 0f));
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, span * 4f);

        if (!FindAnyObjectByType<Light>())
        {
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.None; // no shadow pass over 16k instances
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }

    private void OnGUI()
    {
        if (_recorder == null) return;

        float fps = 1f / Mathf.Max(Time.smoothDeltaTime, 1e-5f);
        int objects = _bodyCount + _spheres.Count;
        string csv = _recorder.IsFinished ? "<color=#8fdd8f>CSV written</color>" : $"recording {MeasureSteps} steps…";
        string text =
            $"<b>box3d pyramid</b> — {Rows} rows, {_workers} worker thread(s)\n" +
            $"<size=26><b>{objects} objects</b>   <b>step #{_stepCount} · {_stepStats.Average:F2} ms</b>   <b>{fps:F0} FPS</b></size>\n" +
            $"step peak: {_stepStats.Peak:F2} ms   ({_bodyCount} boxes + {_spheres.Count} spheres)\n" +
            $"{csv}\n" +
            $"<color=#cccccc>left-click to throw a sphere</color>";

        // Dark backdrop so the numbers read over the bright scene.
        var rect = new Rect(100f, 10f, 560f, 150f);
        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = prev;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            richText = true,
            padding = new RectOffset(12, 12, 10, 10),
            normal = { textColor = Color.white },
        };
        GUI.Label(rect, text, style);
    }

    private void OnDestroy()
    {
        if (_world.IsValid) _world.Destroy();
        if (_material) Destroy(_material);
        if (_sphereMaterial) Destroy(_sphereMaterial);
    }
}
