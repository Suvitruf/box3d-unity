#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using Box3D;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Interactive physics playground: spawn textured objects from a menu (they rain from
/// the sky), drag/throw them with the left mouse button (motor joint), detonate explosions with
/// the right button, and play with a seesaw and a conveyor belt. Objects flash on hard impacts
/// (hit events).</summary>
public class Box3DPlayground : MonoBehaviour
{
    private class SpawnedObject
    {
        public Body Body;
        public Transform Visual;
        public Material Material;
        public Color BaseColor;
        public float Flash;
        public bool Persistent;
    }

    private static readonly string[] ShapeNames = { "Box", "Sphere", "Capsule", "Canister", "Dumbbell" };

    [SerializeField, Tooltip("Drag spring stiffness in Hertz.")]
    private float DragHertz = 5f;

    [SerializeField, Tooltip("Explosion radius in meters (right mouse button).")]
    private float ExplosionRadius = 4f;

    [SerializeField, Tooltip("Explosion strength (impulse per m² of exposed surface). Objects here are " +
                             "water-density — hundreds of kg — so this needs to be in the thousands.")]
    private float ExplosionImpulsePerArea = 3000f;

    [SerializeField, Tooltip("Box3D worker threads. 0 = auto (half the logical cores).")]
    private int WorkerCount = 0;

    [SerializeField, Tooltip("Base lit material for spawned objects. A scene-referenced asset keeps " +
                             "the shader from being stripped in player builds (everything else here " +
                             "is created at runtime).")]
    private Material BaseMaterial;

    private World _world;
    private Body _mouseAnchor;
    private Camera _camera;
    private Texture2D _checker;
    private readonly List<SpawnedObject> _objects = new List<SpawnedObject>();

    private int _shapeIndex;
    private float _spawnCount = 10f;
    private bool _debugDraw;
    private readonly Rect _panelRect = new Rect(10f, 10f, 250f, 380f);

    private MotorJoint _dragJoint;
    private bool _isDragging;
    private float _grabDistance;

    private readonly System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
    private double _stepAccumMs;
    private int _stepSamples;
    private float _stepAverageMs;
    private int _workers;

    private void Start()
    {
        _camera = Camera.main;
        _checker = CreateCheckerTexture();
        _workers = WorkerCount > 0 ? WorkerCount : Mathf.Max(1, SystemInfo.processorCount / 2);
        WorldDef worldDef = WorldDef.Default;
        worldDef.WorkerCount = (uint)_workers;
        _world = World.Create(worldDef);
        _mouseAnchor = _world.CreateBody(BodyDef.Default);

        CreateGround();
        CreateConveyor();
        CreateSeesaw();
    }

    // --- environment ---

    private void CreateGround()
    {
        Body ground = _world.CreateBody(BodyDef.Default);
        BoxHull hull = BoxHull.Create(30f, 0.5f, 30f);
        ShapeDef groundShape = ShapeDef.Default;
        groundShape.EnableHitEvents = true; // so objects flash when slamming into the floor
        ground.CreateHullShape(groundShape, in hull);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Ground";
        visual.transform.localScale = new Vector3(60f, 1f, 60f);
        Destroy(visual.GetComponent<Collider>());
        if (BaseMaterial) visual.GetComponent<MeshRenderer>().sharedMaterial = BaseMaterial;
    }

    private void CreateConveyor()
    {
        BodyDef beltBody = BodyDef.Default;
        beltBody.Position = new float3(-6f, 0.65f, 0f);
        Body belt = _world.CreateBody(beltBody);

        ShapeDef beltShape = ShapeDef.Default;
        beltShape.BaseMaterial.TangentVelocity = new float3(0f, 0f, 2.5f); // carries objects along +z
        beltShape.BaseMaterial.Friction = 1f;
        beltShape.EnableHitEvents = true;
        BoxHull beltHull = BoxHull.Create(1.5f, 0.15f, 5f);
        belt.CreateHullShape(beltShape, in beltHull);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Conveyor";
        visual.transform.position = new Vector3(-6f, 0.65f, 0f);
        visual.transform.localScale = new Vector3(3f, 0.3f, 10f);
        Destroy(visual.GetComponent<Collider>());
        MeshRenderer beltRenderer = visual.GetComponent<MeshRenderer>();
        Material beltMaterial = CreateInstanceMaterial(beltRenderer);
        beltMaterial.color = new Color(0.25f, 0.45f, 0.9f);
        beltRenderer.sharedMaterial = beltMaterial;
    }

    private void CreateSeesaw()
    {
        // Static pivot block.
        BodyDef pivotDef = BodyDef.Default;
        pivotDef.Position = new float3(6f, 0.8f, 0f);
        Body pivot = _world.CreateBody(pivotDef);
        BoxHull pivotHull = BoxHull.Create(0.2f, 0.3f, 0.2f);
        pivot.CreateHullShape(ShapeDef.Default, in pivotHull);

        GameObject pivotVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pivotVisual.name = "Seesaw Pivot";
        pivotVisual.transform.position = new Vector3(6f, 0.8f, 0f);
        pivotVisual.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
        Destroy(pivotVisual.GetComponent<Collider>());
        if (BaseMaterial) pivotVisual.GetComponent<MeshRenderer>().sharedMaterial = BaseMaterial;

        // Dynamic plank on a revolute hinge (z-axis).
        BodyDef plankDef = BodyDef.Default;
        plankDef.Type = BodyType.Dynamic;
        plankDef.Position = new float3(6f, 1.2f, 0f);
        Body plank = _world.CreateBody(plankDef);
        ShapeDef plankShape = ShapeDef.Default;
        plankShape.EnableHitEvents = true;
        BoxHull plankHull = BoxHull.Create(2.5f, 0.08f, 0.6f);
        plank.CreateHullShape(plankShape, in plankHull);

        RevoluteJointDef hinge = RevoluteJointDef.Default;
        hinge.Base.BodyIdA = pivot.Id;
        hinge.Base.BodyIdB = plank.Id;
        hinge.Base.LocalFrameA = new B3Transform { Position = new float3(0f, 0.4f, 0f), Rotation = quaternion.identity };
        hinge.Base.LocalFrameB = B3Transform.Identity;
        _world.CreateRevoluteJoint(hinge);

        GameObject plankVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plankVisual.name = "Seesaw Plank";
        plankVisual.transform.localScale = new Vector3(5f, 0.16f, 1.2f);
        Destroy(plankVisual.GetComponent<Collider>());
        Register(plank, plankVisual.transform, plankVisual.GetComponent<MeshRenderer>(),
            new Color(0.8f, 0.65f, 0.4f), persistent: true);
    }

    // --- spawning ---

    private void SpawnObjects(int shapeIndex, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float3 position = new float3(
                UnityEngine.Random.Range(-4f, 4f),
                UnityEngine.Random.Range(8f, 14f),
                UnityEngine.Random.Range(-4f, 4f));
            SpawnObject(shapeIndex, position);
        }
    }

    private void SpawnObject(int shapeIndex, float3 position)
    {
        BodyDef bodyDef = BodyDef.Default;
        bodyDef.Type = BodyType.Dynamic;
        bodyDef.Position = position;
        bodyDef.Rotation = UnityEngine.Random.rotation;
        Body body = _world.CreateBody(bodyDef);

        ShapeDef shapeDef = ShapeDef.Default;
        shapeDef.EnableHitEvents = true;

        Color tint = Color.HSVToRGB(UnityEngine.Random.value, 0.55f, 1f);
        Transform visual;
        MeshRenderer renderer;

        switch (shapeIndex)
        {
            case 0: // Box
            {
                float half = UnityEngine.Random.Range(0.25f, 0.5f);
                BoxHull hull = BoxHull.CreateCube(half);
                body.CreateHullShape(shapeDef, in hull);
                (visual, renderer) = CreateVisual(PrimitiveType.Cube, Vector3.one * (half * 2f));
                break;
            }
            case 1: // Sphere
            {
                float radius = UnityEngine.Random.Range(0.25f, 0.45f);
                body.CreateSphereShape(shapeDef, new Sphere { Radius = radius });
                (visual, renderer) = CreateVisual(PrimitiveType.Sphere, Vector3.one * (radius * 2f));
                break;
            }
            case 2: // Capsule
            {
                float radius = 0.22f;
                float halfSpan = 0.35f;
                body.CreateCapsuleShape(shapeDef, new Capsule
                {
                    Center1 = new float3(0f, -halfSpan, 0f),
                    Center2 = new float3(0f, halfSpan, 0f),
                    Radius = radius,
                });
                (visual, renderer) = CreateVisual(PrimitiveType.Capsule,
                    new Vector3(radius * 2f, halfSpan + radius, radius * 2f));
                break;
            }
            case 3: // Canister (cylinder hull)
            {
                const float height = 1f;
                const float radius = 0.35f;
                Hull cylinder = Hull.CreateCylinder(height, radius);
                body.CreateHullShape(shapeDef, cylinder);
                cylinder.Destroy(); // hulls are cloned
                (visual, renderer) = CreateVisual(PrimitiveType.Cylinder,
                    new Vector3(radius * 2f, height * 0.5f, radius * 2f));
                break;
            }
            default: // Dumbbell: one body, three shapes
            {
                body.CreateCapsuleShape(shapeDef, new Capsule
                {
                    Center1 = new float3(-0.35f, 0f, 0f),
                    Center2 = new float3(0.35f, 0f, 0f),
                    Radius = 0.12f,
                });
                body.CreateSphereShape(shapeDef, new Sphere { Center = new float3(-0.45f, 0f, 0f), Radius = 0.28f });
                body.CreateSphereShape(shapeDef, new Sphere { Center = new float3(0.45f, 0f, 0f), Radius = 0.28f });
                (visual, renderer) = CreateDumbbellVisual();
                break;
            }
        }

        renderer.material.mainTexture = _checker;
        Register(body, visual, renderer, tint, persistent: false);
    }

    private (Transform, MeshRenderer) CreateVisual(PrimitiveType primitive, Vector3 scale)
    {
        GameObject visual = GameObject.CreatePrimitive(primitive);
        visual.transform.localScale = scale;
        Destroy(visual.GetComponent<Collider>());
        return (visual.transform, visual.GetComponent<MeshRenderer>());
    }

    private (Transform, MeshRenderer) CreateDumbbellVisual()
    {
        var root = new GameObject("Dumbbell");

        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(bar.GetComponent<Collider>());
        bar.transform.SetParent(root.transform, false);
        bar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        bar.transform.localScale = new Vector3(0.24f, 0.47f, 0.24f);

        MeshRenderer mainRenderer = bar.GetComponent<MeshRenderer>();
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(ball.GetComponent<Collider>());
            ball.transform.SetParent(root.transform, false);
            ball.transform.localPosition = new Vector3(0.45f * side, 0f, 0f);
            ball.transform.localScale = Vector3.one * 0.56f;
            ball.GetComponent<MeshRenderer>().sharedMaterial = mainRenderer.material;
        }
        return (root.transform, mainRenderer);
    }

    private void Register(Body body, Transform visual, MeshRenderer renderer, Color tint, bool persistent)
    {
        body.UserData = (IntPtr)_objects.Count;
        Material material = CreateInstanceMaterial(renderer);
        material.color = tint;
        foreach (MeshRenderer child in visual.GetComponentsInChildren<MeshRenderer>())
        {
            child.sharedMaterial = material;
        }
        renderer.sharedMaterial = material;
        _objects.Add(new SpawnedObject
        {
            Body = body,
            Visual = visual,
            Material = material,
            BaseColor = tint,
            Persistent = persistent,
        });
    }

    private void ClearSpawned()
    {
        StopDrag();
        var kept = new List<SpawnedObject>();
        foreach (SpawnedObject entry in _objects)
        {
            if (entry.Persistent)
            {
                kept.Add(entry);
                continue;
            }
            entry.Body.Destroy();
            Destroy(entry.Visual.gameObject);
        }
        _objects.Clear();
        _objects.AddRange(kept);
        for (int i = 0; i < _objects.Count; i++)
        {
            _objects[i].Body.UserData = (IntPtr)i;
        }
    }

    private Material CreateInstanceMaterial(MeshRenderer renderer)
    {
        // Prefer the scene-referenced asset: runtime-only scenes get their default material and
        // shader stripped from player builds (the "everything is magenta" trap).
        return BaseMaterial ? new Material(BaseMaterial) : renderer.material;
    }

    private static Texture2D CreateCheckerTexture()
    {
        const int size = 64;
        const int cell = 8;
        var texture = new Texture2D(size, size) { filterMode = FilterMode.Point };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool dark = ((x / cell) + (y / cell)) % 2 == 0;
                texture.SetPixel(x, y, dark ? new Color(0.55f, 0.55f, 0.55f) : Color.white);
            }
        }
        texture.Apply();
        return texture;
    }

    // --- interaction ---

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && !IsMouseOverPanel(mouse))
        {
            if (mouse.leftButton.wasPressedThisFrame) TryStartDrag(mouse);
            else if (mouse.leftButton.isPressed && _isDragging) UpdateDrag(mouse);
            else if (mouse.leftButton.wasReleasedThisFrame && _isDragging) StopDrag();

            if (mouse.rightButton.wasPressedThisFrame) ExplodeAtMouse(mouse);
        }

        foreach (SpawnedObject entry in _objects)
        {
            if (entry.Flash <= 0f) continue;
            entry.Flash = Mathf.Max(0f, entry.Flash - Time.deltaTime * 3f);
            entry.Material.color = Color.Lerp(entry.BaseColor, Color.red, entry.Flash);
        }

        if (_debugDraw && _world.IsValid) _world.DrawDebug();
    }

    private bool IsMouseOverPanel(Mouse mouse)
    {
        Vector2 position = mouse.position.ReadValue();
        return _panelRect.Contains(new Vector2(position.x, Screen.height - position.y));
    }

    private void TryStartDrag(Mouse mouse)
    {
        UnityEngine.Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
        RayResult result = _world.CastRayClosest(ray.origin, (float3)(ray.direction * 100f), QueryFilter.Default);
        if (!result.Hit) return;

        Body hitBody = new Shape { Id = result.ShapeId }.GetBody();
        if (!hitBody.IsValid || hitBody.GetMassData().Mass <= 0f) return; // statics aren't draggable

        _grabDistance = math.distance((float3)ray.origin, result.Point);

        MotorJointDef def = MotorJointDef.Default;
        def.Base.BodyIdA = _mouseAnchor.Id;
        def.Base.BodyIdB = hitBody.Id;
        def.Base.LocalFrameA = new B3Transform { Position = result.Point, Rotation = quaternion.identity };
        def.LinearHertz = DragHertz;
        def.LinearDampingRatio = 1f;
        def.MaxSpringForce = 1000f * hitBody.GetMassData().Mass;
        _dragJoint = _world.CreateMotorJoint(def);
        _isDragging = true;
    }

    private void UpdateDrag(Mouse mouse)
    {
        UnityEngine.Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
        float3 target = (float3)ray.origin + (float3)ray.direction * _grabDistance;

        Box3D.Joint joint = _dragJoint;
        joint.SetLocalFrameA(new B3Transform { Position = target, Rotation = quaternion.identity });
        joint.WakeBodies();
    }

    private void StopDrag()
    {
        if (!_isDragging) return;
        Box3D.Joint joint = _dragJoint;
        joint.Destroy();
        _dragJoint = default;
        _isDragging = false;
    }

    private void ExplodeAtMouse(Mouse mouse)
    {
        UnityEngine.Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
        RayResult result = _world.CastRayClosest(ray.origin, (float3)(ray.direction * 100f), QueryFilter.Default);

        float3 point;
        if (result.Hit)
        {
            point = result.Point;
        }
        else if (math.abs(ray.direction.y) > 1e-4f) // fall back to the ground plane
        {
            float t = -ray.origin.y / ray.direction.y;
            if (t < 0f) return;
            point = (float3)(ray.origin + ray.direction * t);
        }
        else
        {
            return;
        }

        ExplosionDef explosion = ExplosionDef.Default;
        explosion.Position = point;
        explosion.Radius = ExplosionRadius;
        explosion.Falloff = ExplosionRadius * 0.5f;
        explosion.ImpulsePerArea = ExplosionImpulsePerArea;
        _world.Explode(explosion);
    }

    // --- simulation ---

    private void FixedUpdate()
    {
        _stopwatch.Restart();
        _world.Step(Time.fixedDeltaTime);
        _stopwatch.Stop();
        _stepAccumMs += _stopwatch.Elapsed.TotalMilliseconds;
        _stepSamples++;
        if (_stepSamples >= 30)
        {
            _stepAverageMs = (float)(_stepAccumMs / _stepSamples);
            _stepAccumMs = 0.0;
            _stepSamples = 0;
        }

        foreach (BodyMoveEvent moveEvent in _world.GetBodyMoveEvents())
        {
            int index = (int)moveEvent.UserData;
            if (index < 0 || index >= _objects.Count) continue;
            _objects[index].Visual.SetPositionAndRotation(moveEvent.Transform.Position, moveEvent.Transform.Rotation);
        }

        ContactEvents contacts = _world.GetContactEvents();
        foreach (ContactHitEvent hit in contacts.Hit)
        {
            FlashShape(hit.ShapeIdA);
            FlashShape(hit.ShapeIdB);
        }
    }

    private void FlashShape(ShapeId shapeId)
    {
        var shape = new Shape { Id = shapeId };
        if (!shape.IsValid) return;
        Body body = shape.GetBody();
        int index = (int)body.UserData;
        if (index >= 0 && index < _objects.Count && _objects[index].Body.Equals(body))
        {
            _objects[index].Flash = 1f;
        }
    }

    // --- UI ---

    private void OnGUI()
    {
        GUILayout.BeginArea(_panelRect, GUI.skin.box);
        GUILayout.Label("<b>Spawn</b>", new GUIStyle(GUI.skin.label) { richText = true });
        _shapeIndex = GUILayout.SelectionGrid(_shapeIndex, ShapeNames, 2);

        GUILayout.Space(6f);
        GUILayout.Label($"Count: {(int)_spawnCount}");
        _spawnCount = GUILayout.HorizontalSlider(_spawnCount, 1f, 50f);

        GUILayout.Space(6f);
        if (GUILayout.Button($"Spawn {(int)_spawnCount} × {ShapeNames[_shapeIndex]}"))
        {
            SpawnObjects(_shapeIndex, (int)_spawnCount);
        }
        if (GUILayout.Button("Clear"))
        {
            ClearSpawned();
        }

        GUILayout.Space(6f);
        _debugDraw = GUILayout.Toggle(_debugDraw, "Debug draw");

        GUILayout.FlexibleSpace();
        GUILayout.Label($"Step: {_stepAverageMs:F2} ms ({_workers} workers)");
        GUILayout.Label($"Objects: {_objects.Count}");
        GUILayout.Space(4f);
        GUILayout.Label("LMB — drag / throw");
        GUILayout.Label("RMB — explosion");
        GUILayout.Label("Objects flash red on hard impacts");
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (_world.IsValid) _world.Destroy();
    }
}
#else
using UnityEngine;

/// <summary>Inert stub — this sample requires the Input System package (com.unity.inputsystem).</summary>
public class Box3DPlayground : MonoBehaviour
{
    private void Start()
    {
        Debug.LogWarning("Box3DPlayground requires the Input System package (com.unity.inputsystem).");
    }
}
#endif
