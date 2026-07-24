#if ENABLE_INPUT_SYSTEM
using System;
using Box3D;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Interactive drag sample: click and drag spheres with the mouse. Picking uses
/// World.CastRayClosest; dragging uses a motor joint whose target frame follows the mouse —
/// box3d's replacement for a mouse joint.</summary>
public class Box3DDragSandbox : MonoBehaviour
{
    [SerializeField, Tooltip("Number of spheres to drop.")]
    private int SphereCount = 24;

    [SerializeField, Tooltip("Sphere radius in meters.")]
    private float SphereRadius = 0.5f;

    [SerializeField, Tooltip("Drag spring stiffness in Hertz.")]
    private float DragHertz = 5f;

    private World _world;
    private Body _mouseAnchor;
    private Transform[] _visuals;
    private Camera _camera;

    private MotorJoint _dragJoint;
    private bool _isDragging;
    private float _grabDistance;

    private void Start()
    {
        _camera = Camera.main;
        _world = World.Create(WorldDef.Default);
        _mouseAnchor = _world.CreateBody(BodyDef.Default);
        _visuals = new Transform[SphereCount];

        CreateGround();
        CreateSpheres();
    }

    private void CreateGround()
    {
        Body ground = _world.CreateBody(BodyDef.Default);
        BoxHull hull = BoxHull.Create(10f, 0.5f, 10f);
        ground.CreateHullShape(ShapeDef.Default, in hull);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Ground";
        visual.transform.localScale = new Vector3(20f, 1f, 20f);
        Destroy(visual.GetComponent<Collider>());
    }

    private void CreateSpheres()
    {
        UnityEngine.Random.InitState(917);
        for (int i = 0; i < SphereCount; i++)
        {
            float3 position = new float3(
                UnityEngine.Random.Range(-4f, 4f),
                UnityEngine.Random.Range(2f, 8f),
                UnityEngine.Random.Range(-4f, 4f));

            BodyDef bodyDef = BodyDef.Default;
            bodyDef.Type = BodyType.Dynamic;
            bodyDef.Position = position;
            bodyDef.UserData = (IntPtr)i;
            Body body = _world.CreateBody(bodyDef);
            body.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = SphereRadius });

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = $"Sphere {i}";
            visual.transform.localScale = Vector3.one * (SphereRadius * 2f);
            visual.transform.position = (Vector3)position;
            Destroy(visual.GetComponent<Collider>());
            _visuals[i] = visual.transform;
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame) TryStartDrag(mouse);
        else if (mouse.leftButton.isPressed && _isDragging) UpdateDrag(mouse);
        else if (mouse.leftButton.wasReleasedThisFrame && _isDragging) StopDrag();
    }

    private void TryStartDrag(Mouse mouse)
    {
        UnityEngine.Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
        RayResult result = _world.CastRayClosest(ray.origin, (float3)(ray.direction * 100f), QueryFilter.Default);
        if (!result.Hit) return;

        Body hitBody = new Shape { Id = result.ShapeId }.GetBody();
        if (!hitBody.IsValid) return;

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
        Box3D.Joint joint = _dragJoint;
        joint.Destroy();
        _dragJoint = default;
        _isDragging = false;
    }

    private void FixedUpdate()
    {
        _world.Step(Time.fixedDeltaTime);

        foreach (BodyMoveEvent moveEvent in _world.GetBodyMoveEvents())
        {
            int index = (int)moveEvent.UserData;
            _visuals[index].SetPositionAndRotation(moveEvent.Transform.Position, moveEvent.Transform.Rotation);
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10f, 10f, 600f, 30f), _isDragging
            ? "Dragging (motor joint spring)"
            : "Click and drag a sphere — picking: CastRayClosest, dragging: motor joint");
    }

    private void OnDestroy()
    {
        if (_world.IsValid) _world.Destroy();
    }
}
#else
using UnityEngine;

/// <summary>Inert stub — this sample requires the Input System package (com.unity.inputsystem).</summary>
public class Box3DDragSandbox : MonoBehaviour
{
    private void Start()
    {
        Debug.LogWarning("Box3DDragSandbox requires the Input System package (com.unity.inputsystem).");
    }
}
#endif
