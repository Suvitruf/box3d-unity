using Box3D;
using Box3D.Hybrid;
using Unity.Mathematics;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Drives the WaterPoolSandbox scene — a test bed for <see cref="Box3DWaterVolume"/>. The pool,
/// tap and props are all authored in the scene; this script runs the tap (the on-screen button
/// starts/stops filling), keeps the visuals in sync with the fill level, and lets you click-drag
/// the scene's dynamic bodies to throw them in: wooden crates and the beach ball float, the anvil
/// sinks. Optional dressing, each with an Inspector toggle: a wave-displaced surface mesh driven by
/// the SAME function the physics samples, splash particles on the water's
/// BodyEntered event, and an underwater tint when the camera goes below the surface.</summary>
public class WaterPoolSandbox : MonoBehaviour
{
    [SerializeField, Tooltip("The pool's water volume.")]
    private Box3DWaterVolume Water;

    [SerializeField, Tooltip("Translucent block scaled to the current fill level.")]
    private Transform WaterVisual;

    [SerializeField, Tooltip("Stream from the tap to the surface, shown while filling.")]
    private Transform Stream;

    [SerializeField, Tooltip("Grid mesh displaced by the water's surface function (waves). Hidden when Animate Surface is off.")]
    private MeshFilter WaterSurface;

    [SerializeField, Tooltip("Draggable bodies, teleported home by the Reset button.")]
    private Box3DBody[] Props;

    [SerializeField, Min(0f), Tooltip("Fill fraction per second while the tap runs (1 = fills an empty pool in a second).")]
    private float FillRate = 0.08f;

    [SerializeField, Min(0.1f), Tooltip("Drag spring stiffness in Hz (like the MouseDrag sample).")]
    private float DragHertz = 5f;

    [Header("Dressing (optional)")]
    [SerializeField, Tooltip("Show the wave-displaced surface mesh (uses Water.SampleSurfaceY, so visuals match physics). Off = flat block only.")]
    private bool AnimateSurface = true;

    [SerializeField, Tooltip("Splash droplets when a body hits the water (Box3DWaterVolume.BodyEntered).")]
    private bool EnableSplashes = true;

    [SerializeField, Range(0.2f, 3f), Tooltip("Splash count/strength multiplier.")]
    private float SplashIntensity = 1f;

    [SerializeField, Tooltip("Blue overlay when the camera is below the water surface.")]
    private bool UnderwaterTint = true;

    [SerializeField, Tooltip("Material for splash droplets (the water material).")]
    private Material SplashMaterial;

    [SerializeField, Tooltip("Droplet mesh (built-in sphere).")]
    private Mesh DropletMesh;

    // Must match the authored pool: water zone x/z in ±InnerHalf, y in [FloorTop, FloorTop+WaterDepth].
    private const float InnerHalf = 4f;
    private const float FloorTop = 0.4f;
    private const float WaterDepth = 3f;
    private const float StreamZ = -2.3f;  // under the tap's spout
    private const float StreamTop = 4.9f; // just below the tap
    private const int SurfaceGrid = 48;   // quads per side of the surface mesh

    private Camera _camera;
    private bool _filling;
    private Vector3[] _homes;
    private Quaternion[] _homeRotations;

    private Mesh _surfaceMesh;
    private Vector3[] _surfaceVertices;
    private ParticleSystem _droplets;

    private void Start()
    {
        _camera = Camera.main;
        _homes = new Vector3[Props.Length];
        _homeRotations = new Quaternion[Props.Length];
        for (int i = 0; i < Props.Length; i++)
        {
            _homes[i] = Props[i].transform.position;
            _homeRotations[i] = Props[i].transform.rotation;
        }

        BuildSurfaceMesh();
        if (EnableSplashes) BuildSplashSystems();
        Water.BodyEntered += OnBodyEntered;
        UpdateWaterVisuals();
    }

    private void OnDestroy()
    {
        if (Water) Water.BodyEntered -= OnBodyEntered;
        if (_surfaceMesh) Destroy(_surfaceMesh); // runtime mesh — not freed with the GameObject
    }

    private void Update()
    {
        if (_filling)
        {
            Water.FillLevel += FillRate * Time.deltaTime;
            if (Water.FillLevel >= 1f) _filling = false;
        }
        UpdateWaterVisuals();
#if ENABLE_INPUT_SYSTEM
        UpdateDrag();
#endif
    }

    // --- visuals ---

    private void UpdateWaterVisuals()
    {
        float height = Water.FillLevel * WaterDepth;
        bool visible = height > 0.005f;

        // The translucent volume block. With the surface mesh on, its flat top ducks below the
        // deepest wave trough so crests and troughs both read as the mesh, not the box.
        float topMargin = AnimateSurface ? Water.WaveHeight + 0.05f : 0f;
        float blockHeight = Mathf.Max(height - topMargin, 0.01f);
        WaterVisual.gameObject.SetActive(visible);
        if (visible)
        {
            const float inset = 0.05f;
            WaterVisual.position = new Vector3(0f, FloorTop + blockHeight * 0.5f, 0f);
            WaterVisual.localScale = new Vector3(InnerHalf * 2f - inset, blockHeight, InnerHalf * 2f - inset);
        }

        // The wave surface mesh, displaced by the same function the physics samples.
        WaterSurface.gameObject.SetActive(AnimateSurface && visible);
        if (AnimateSurface && visible) UpdateSurfaceMesh();

        Stream.gameObject.SetActive(_filling);
        if (_filling)
        {
            float bottom = visible ? FloorTop + height : FloorTop;
            Stream.position = new Vector3(0f, (StreamTop + bottom) * 0.5f, StreamZ);
            Stream.localScale = new Vector3(0.15f, Mathf.Max(StreamTop - bottom, 0.01f), 0.15f);
        }
    }

    private void BuildSurfaceMesh()
    {
        int verts = SurfaceGrid + 1;
        _surfaceVertices = new Vector3[verts * verts];
        var uv = new Vector2[verts * verts];
        float extent = InnerHalf - 0.05f;
        for (int z = 0; z < verts; z++)
        {
            for (int x = 0; x < verts; x++)
            {
                float wx = -extent + 2f * extent * x / SurfaceGrid;
                float wz = -extent + 2f * extent * z / SurfaceGrid;
                _surfaceVertices[z * verts + x] = new Vector3(wx, FloorTop, wz);
                uv[z * verts + x] = new Vector2((float)x / SurfaceGrid, (float)z / SurfaceGrid);
            }
        }
        var triangles = new int[SurfaceGrid * SurfaceGrid * 6];
        int t = 0;
        for (int z = 0; z < SurfaceGrid; z++)
        {
            for (int x = 0; x < SurfaceGrid; x++)
            {
                int i = z * verts + x;
                triangles[t++] = i; triangles[t++] = i + verts; triangles[t++] = i + 1;
                triangles[t++] = i + 1; triangles[t++] = i + verts; triangles[t++] = i + verts + 1;
            }
        }
        _surfaceMesh = new Mesh { name = "Water Surface" };
        _surfaceMesh.MarkDynamic();
        _surfaceMesh.vertices = _surfaceVertices;
        _surfaceMesh.uv = uv;
        _surfaceMesh.triangles = triangles;
        _surfaceMesh.bounds = new Bounds(new Vector3(0f, FloorTop + WaterDepth * 0.5f, 0f),
            new Vector3(InnerHalf * 2f, WaterDepth + 2f, InnerHalf * 2f));
        WaterSurface.sharedMesh = _surfaceMesh;
    }

    private void UpdateSurfaceMesh()
    {
        for (int i = 0; i < _surfaceVertices.Length; i++)
        {
            Vector3 v = _surfaceVertices[i];
            v.y = Water.SampleSurfaceY(v.x, v.z);
            _surfaceVertices[i] = v;
        }
        _surfaceMesh.vertices = _surfaceVertices;
        _surfaceMesh.RecalculateNormals();
    }

    // --- splashes ---

    private void BuildSplashSystems()
    {
        _droplets = CreateParticleSystem("Splash Droplets", DropletMesh, gravity: 1f,
            startSize: new Vector2(0.06f, 0.16f), lifetime: 0.8f);
    }

    private ParticleSystem CreateParticleSystem(string name, Mesh mesh, float gravity,
        Vector2 startSize, float lifetime)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, worldPositionStays: false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = lifetime;
        main.startSpeed = 0f; // velocities are set explicitly per emitted particle
        main.startSize = new ParticleSystem.MinMaxCurve(startSize.x, startSize.y);
        main.gravityModifier = gravity;
        main.maxParticles = 512;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false; // burst-only via Emit()
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;    // manual Emit ignores it unreliably — don't depend on it

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.sharedMaterial = SplashMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }

    private void OnBodyEntered(Body body, Vector3 point, float speed)
    {
        if (!EnableSplashes || !_droplets) return;

        // Each droplet gets an explicit up-and-out velocity (visual-only randomness — the physics
        // stays deterministic). Manual Emit + the shape module is unreliable, so we don't use it.
        int count = Mathf.Clamp(Mathf.RoundToInt(speed * 8f * SplashIntensity), 4, 90);
        float kick = Mathf.Clamp(1.6f + speed * 0.25f, 1.6f, 4.5f);
        var droplet = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector2 spread = UnityEngine.Random.insideUnitCircle;
            droplet.position = point + new Vector3(spread.x * 0.15f, 0.05f, spread.y * 0.15f);
            droplet.velocity = new Vector3(spread.x * 0.9f, UnityEngine.Random.Range(0.8f, 1.4f), spread.y * 0.9f) * kick;
            _droplets.Emit(droplet, 1);
        }
    }


    // --- props / drag / GUI ---

    private void ResetProps()
    {
        for (int i = 0; i < Props.Length; i++)
        {
            Props[i].Position = _homes[i];
            Props[i].Rotation = _homeRotations[i];
            Body raw = Props[i].Body;
            if (raw.IsValid)
            {
                raw.SetLinearVelocity(float3.zero);
                raw.SetAngularVelocity(float3.zero);
            }
        }
    }

#if ENABLE_INPUT_SYSTEM
    private Box3D.Joint _dragJoint;
    private bool _isDragging;
    private float _grabDistance;

    private void UpdateDrag()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !_camera) return;

        if (mouse.leftButton.wasPressedThisFrame) TryStartDrag(mouse);
        else if (mouse.leftButton.isPressed && _isDragging) MoveDrag(mouse);
        else if (mouse.leftButton.wasReleasedThisFrame && _isDragging) StopDrag();
    }

    private void TryStartDrag(Mouse mouse)
    {
        World world = Box3DWorld.Instance.World;
        UnityEngine.Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
        RayResult result = world.CastRayClosest(ray.origin, (float3)(ray.direction * 100f), QueryFilter.Default);
        if (!result.Hit) return;

        Body hitBody = new Shape { Id = result.ShapeId }.GetBody();
        if (!hitBody.IsValid || hitBody.GetBodyType() != BodyType.Dynamic) return;

        _grabDistance = (float)math.distance((float3)ray.origin, (float3)result.Point);

        MotorJointDef def = MotorJointDef.Default;
        def.Base.BodyIdA = Box3DWorld.Instance.WorldAnchor.Id;
        def.Base.BodyIdB = hitBody.Id;
        def.Base.LocalFrameA = new B3Transform { Position = (float3)result.Point, Rotation = quaternion.identity };
        def.LinearHertz = DragHertz;
        def.LinearDampingRatio = 1f;
        def.MaxSpringForce = 1000f * hitBody.GetMassData().Mass;
        _dragJoint = world.CreateMotorJoint(def);
        _isDragging = true;
    }

    private void MoveDrag(Mouse mouse)
    {
        UnityEngine.Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
        float3 target = (float3)ray.origin + (float3)ray.direction * _grabDistance;
        Box3D.Joint joint = _dragJoint;
        joint.SetLocalFrameA(new B3Transform { Position = target, Rotation = quaternion.identity });
        joint.WakeBodies();
    }

    private void StopDrag()
    {
        Box3D.Joint joint = _dragJoint;
        joint.Destroy();
        _dragJoint = default;
        _isDragging = false;
    }
#endif

    private void OnGUI()
    {
        // Underwater overlay first, so the buttons draw on top of it.
        if (UnderwaterTint && _camera)
        {
            Vector3 cam = _camera.transform.position;
            bool inPoolColumn = Mathf.Abs(cam.x) < InnerHalf && Mathf.Abs(cam.z) < InnerHalf;
            if (inPoolColumn && Water.FillLevel > 0f && cam.y < Water.SampleSurfaceY(cam.x, cam.z))
            {
                Color previous = GUI.color;
                GUI.color = new Color(0.1f, 0.35f, 0.6f, 0.45f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = previous;
            }
        }

        GUILayout.BeginArea(new Rect(10f, 10f, 260f, 150f));
        if (GUILayout.Button(_filling ? "■ Stop water" : "▶ Start water", GUILayout.Height(32f)))
        {
            _filling = !_filling;
        }
        if (GUILayout.Button("Reset objects", GUILayout.Height(26f))) ResetProps();
        GUILayout.Label($"Pool fill: {Water.FillLevel:P0}");
#if ENABLE_INPUT_SYSTEM
        GUILayout.Label("Drag objects with the mouse and\nthrow them into the pool.");
#else
        GUILayout.Label("(Input System package not enabled — dragging is off.)");
#endif
        GUILayout.EndArea();
    }
}
