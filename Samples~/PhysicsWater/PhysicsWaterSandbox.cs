using Box3D;
using Box3D.Hybrid;
using Unity.Mathematics;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Drives the PhysicsWaterSandbox scene — a playground for the GPU particle water
/// (<see cref="Box3DWater"/>). The scene authors the water volume, a waterfall and a wind zone;
/// this script builds the pool and props from primitives on play, draws the control panel
/// (fill / drain / splash, waterfall and wind toggles) and lets you click-drag dynamic bodies
/// to throw them in: the wooden crates and the beach ball float, the iron block sinks.
/// Water SURFACE rendering needs URP with Depth Texture on (enable Opaque Texture too for
/// refraction) — the simulation itself runs on any pipeline with compute-shader support.</summary>
public class PhysicsWaterSandbox : MonoBehaviour
{
    [SerializeField, Tooltip("The pool's particle water.")]
    private Box3DWater Water;

    [SerializeField, Tooltip("Waterfall pouring into the pool, toggled by the panel.")]
    private GameObject Waterfall;

    [SerializeField, Tooltip("Wind zone over the pool surface, toggled by the panel.")]
    private GameObject Wind;

    [SerializeField, Min(0.1f), Tooltip("Drag spring stiffness in Hz (like the MouseDrag sample).")]
    private float DragHertz = 5f;

    // Pool authoring — the water volume in the scene is sized to sit inside these walls.
    private const float PoolInnerHalf = 4f;   // inner area 8x8
    private const float PoolWallHeight = 3.5f;
    private const float PoolWallThickness = 0.6f; // > smoothing radius + particle radius: splashes cannot tunnel through

    private Camera _camera;
    private Box3DBody[] _props;
    private Vector3[] _homes;
    private Quaternion[] _homeRotations;
    private readonly System.Collections.Generic.List<Material> _materials =
        new System.Collections.Generic.List<Material>();

    private void Start()
    {
        _camera = Camera.main;
        BuildPool();
        BuildProps();
    }

    private void OnDestroy()
    {
        foreach (Material material in _materials)
        {
            if (material) Destroy(material);
        }
        _materials.Clear();
    }

    // --- scene building (primitives, so the sample needs no asset dependencies) ---

    private void BuildPool()
    {
        Material deck = Mat(new Color(0.55f, 0.53f, 0.5f));
        Material wall = Mat(new Color(0.4f, 0.45f, 0.5f));

        // Ground slab (top at y = 0) — wide enough that overspill lands on it and stays.
        StaticBox("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 30f), deck);

        // Four walls forming the basin.
        float mid = PoolInnerHalf + PoolWallThickness * 0.5f;
        float y = PoolWallHeight * 0.5f;
        float span = PoolInnerHalf * 2f + PoolWallThickness * 2f;
        StaticBox("Wall N", new Vector3(0f, y, mid), new Vector3(span, PoolWallHeight, PoolWallThickness), wall);
        StaticBox("Wall S", new Vector3(0f, y, -mid), new Vector3(span, PoolWallHeight, PoolWallThickness), wall);
        StaticBox("Wall E", new Vector3(mid, y, 0f), new Vector3(PoolWallThickness, PoolWallHeight, PoolInnerHalf * 2f), wall);
        StaticBox("Wall W", new Vector3(-mid, y, 0f), new Vector3(PoolWallThickness, PoolWallHeight, PoolInnerHalf * 2f), wall);
    }

    private void BuildProps()
    {
        Material wood = Mat(new Color(0.62f, 0.44f, 0.26f));
        Material rubber = Mat(new Color(0.85f, 0.3f, 0.25f));
        Material iron = Mat(new Color(0.25f, 0.26f, 0.3f));

        // Light props splash into the pool on play; the iron block waits on the deck —
        // throw it in to watch it sink while the crates bob.
        _props = new[]
        {
            DynamicBox("Crate", new Vector3(-1.2f, 2.4f, 0.6f), 0.55f, density: 300f, wood),
            DynamicBox("Crate (1)", new Vector3(0.9f, 2.6f, -0.8f), 0.55f, density: 300f, wood),
            DynamicBox("Crate (2)", new Vector3(0.2f, 2.8f, 1.4f), 0.55f, density: 300f, wood),
            DynamicSphere("Beach Ball", new Vector3(1.7f, 3.2f, 0.3f), diameter: 0.8f, density: 90f, rubber),
            DynamicBox("Iron Block", new Vector3(6f, 0.25f, 2f), 0.5f, density: 7800f, iron),
        };

        _homes = new Vector3[_props.Length];
        _homeRotations = new Quaternion[_props.Length];
        for (int i = 0; i < _props.Length; i++)
        {
            _homes[i] = _props[i].transform.position;
            _homeRotations[i] = _props[i].transform.rotation;
        }
    }

    private static void StaticBox(string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject go = MakePrimitive(PrimitiveType.Cube, name, position, size, material);
        go.AddComponent<Box3DBoxShape>(); // no body above it -> its own static body
        go.SetActive(true);
    }

    private Box3DBody DynamicBox(string name, Vector3 position, float size, float density, Material material)
    {
        GameObject go = MakePrimitive(PrimitiveType.Cube, name, position, Vector3.one * size, material);
        var body = go.AddComponent<Box3DBody>(); // dynamic by default
        go.AddComponent<Box3DBoxShape>().SetDensity(density);
        go.SetActive(true);
        return body;
    }

    private Box3DBody DynamicSphere(string name, Vector3 position, float diameter, float density, Material material)
    {
        GameObject go = MakePrimitive(PrimitiveType.Sphere, name, position, Vector3.one * diameter, material);
        var body = go.AddComponent<Box3DBody>();
        go.AddComponent<Box3DSphereShape>().SetDensity(density);
        go.SetActive(true);
        return body;
    }

    // A render-only primitive, inactive so Box3D components can be configured before Awake.
    // The shape components bake the transform's scale, so the visual size IS the collision size.
    private static GameObject MakePrimitive(PrimitiveType type, string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.SetActive(false);
        go.name = name;
        Destroy(go.GetComponent<Collider>()); // PhysX is not used here
        go.transform.position = position;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    // Tracked so OnDestroy can free them — runtime materials aren't destroyed with their objects.
    private Material Mat(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        var material = new Material(shader) { color = color };
        _materials.Add(material);
        return material;
    }

    // --- props / drag / GUI ---

    private void ResetProps()
    {
        for (int i = 0; i < _props.Length; i++)
        {
            _props[i].Position = _homes[i];
            _props[i].Rotation = _homeRotations[i];
            Body raw = _props[i].Body;
            if (raw.IsValid)
            {
                raw.SetLinearVelocity(float3.zero);
                raw.SetAngularVelocity(float3.zero);
            }
        }
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse == null || !_camera) return;
        if (mouse.leftButton.wasPressedThisFrame) TryStartDrag(mouse.position.ReadValue());
        else if (mouse.leftButton.isPressed && _isDragging) MoveDrag(mouse.position.ReadValue());
        else if (mouse.leftButton.wasReleasedThisFrame && _isDragging) StopDrag();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (!_camera) return;
        if (Input.GetMouseButtonDown(0)) TryStartDrag(Input.mousePosition);
        else if (Input.GetMouseButton(0) && _isDragging) MoveDrag(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0) && _isDragging) StopDrag();
#endif
    }

    private Box3D.Joint _dragJoint;
    private bool _isDragging;
    private float _grabDistance;

    private void TryStartDrag(Vector2 screenPosition)
    {
        World world = Box3DWorld.Instance.World;
        UnityEngine.Ray ray = _camera.ScreenPointToRay(screenPosition);
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

    private void MoveDrag(Vector2 screenPosition)
    {
        UnityEngine.Ray ray = _camera.ScreenPointToRay(screenPosition);
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

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10f, 10f, 280f, 240f));
        if (GUILayout.Button("Fill pool", GUILayout.Height(28f))) Water.Fill();
        if (GUILayout.Button("Drain pool", GUILayout.Height(28f))) Water.Clear();
        if (GUILayout.Button("Splash!", GUILayout.Height(28f)))
        {
            Water.SpawnParticles(new Vector3(0f, 5f, 0f), 0.6f, new Vector3(0f, -6f, 0f), 1500);
        }
        if (GUILayout.Button(Waterfall.activeSelf ? "Waterfall: on" : "Waterfall: off", GUILayout.Height(28f)))
        {
            Waterfall.SetActive(!Waterfall.activeSelf);
        }
        if (GUILayout.Button(Wind.activeSelf ? "Wind: on" : "Wind: off", GUILayout.Height(28f)))
        {
            Wind.SetActive(!Wind.activeSelf);
        }
        if (GUILayout.Button("Reset objects", GUILayout.Height(24f))) ResetProps();

        GUILayout.Label($"Particles in use: {Water.ActiveParticleRange:N0}");
#if ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
        GUILayout.Label("Drag objects with the mouse and throw\nthem in — crates float, iron sinks.");
#endif
        GUILayout.EndArea();
    }
}
