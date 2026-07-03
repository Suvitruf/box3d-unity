using System;
using Box3d;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Drivable vehicle on box3d, ported from the engine's own "Driving" sample: box-hull
/// chassis, four sphere wheels on wheel joints (front steering, rear spin motors), a soft parallel
/// joint to the ground as an anti-roll stabilizer, wavy mesh terrain. W/S throttle, A/D steer.</summary>
public class Box3dVehicleSandbox : MonoBehaviour
{
    [SerializeField, Tooltip("Rear wheel motor speed in rad/s at full throttle.")]
    private float SpinSpeed = 30f;

    [SerializeField, Tooltip("Rear wheel motor torque limit in N·m.")]
    private float MaxSpinTorque = 5f;

    [SerializeField, Tooltip("Max steering angle in degrees.")]
    private float MaxSteeringDegrees = 45f;

    [SerializeField, Tooltip("Draw the box3d debug visualization.")]
    private bool DebugDraw = false;

    private World _world;
    private TriangleMesh _terrainMesh;
    private Body _chassis;
    private WheelJoint _frontLeft;
    private WheelJoint _frontRight;
    private WheelJoint _rearLeft;
    private WheelJoint _rearRight;

    private Transform _chassisVisual;
    private readonly Transform[] _wheelVisuals = new Transform[4];
    private readonly Body[] _wheels = new Body[4];
    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
        _world = World.Create(WorldDef.Default);

        Body ground = CreateTerrain();
        CreateVehicle(ground);
    }

    private Body CreateTerrain()
    {
        Vector3[] vertices = VehicleTerrain.BuildVertices();
        int[] triangles = VehicleTerrain.BuildTriangles();
        VehicleTerrain.CreateVisual(vertices, triangles);

        var points = new float3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++) points[i] = vertices[i];
        _terrainMesh = TriangleMesh.Create(points, triangles);

        Body ground = _world.CreateBody(BodyDef.Default);
        ground.CreateMeshShape(ShapeDef.Default, _terrainMesh);
        return ground;
    }

    private void CreateVehicle(Body ground)
    {
        // Values from box3d's sample_joint.cpp Driving sample.
        quaternion xToY = quaternion.RotateZ(math.PI / 2f); // suspension axis: chassis-local up
        quaternion zToY = quaternion.RotateX(-math.PI / 2f); // wheel spin axis orientation

        BodyDef chassisDef = BodyDef.Default;
        chassisDef.Type = BodyType.Dynamic;
        chassisDef.Position = new float3(0f, 2.5f, 0f);
        _chassis = _world.CreateBody(chassisDef);
        ShapeDef chassisShape = ShapeDef.Default;
        chassisShape.Density = 0.5f;
        BoxHull chassisHull = BoxHull.Create(2f, 0.5f, 1f);
        _chassis.CreateHullShape(chassisShape, in chassisHull);

        // Soft upright stabilizer (anti-roll): parallel joint to the ground, constraining the
        // chassis' up axis with a weak spring.
        ParallelJointDef stabilizer = ParallelJointDef.Default;
        stabilizer.Base.BodyIdA = ground.Id;
        stabilizer.Base.BodyIdB = _chassis.Id;
        stabilizer.Base.LocalFrameA = new B3Transform { Position = float3.zero, Rotation = zToY };
        stabilizer.Base.LocalFrameB = new B3Transform { Position = float3.zero, Rotation = zToY };
        stabilizer.Base.CollideConnected = true;
        stabilizer.Hertz = 0.5f;
        stabilizer.DampingRatio = 1f;
        _world.CreateParallelJoint(stabilizer);

        _frontLeft = CreateWheel(0, new float3(1.5f, 2f, 0.8f), xToY, zToY, steering: true);
        _frontRight = CreateWheel(1, new float3(1.5f, 2f, -0.8f), xToY, zToY, steering: true);
        _rearLeft = CreateWheel(2, new float3(-1.5f, 2f, 0.8f), xToY, zToY, steering: false);
        _rearRight = CreateWheel(3, new float3(-1.5f, 2f, -0.8f), xToY, zToY, steering: false);

        GameObject chassisVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chassisVisual.name = "Chassis";
        chassisVisual.transform.localScale = new Vector3(4f, 1f, 2f);
        Destroy(chassisVisual.GetComponent<Collider>());
        _chassisVisual = chassisVisual.transform;
    }

    private WheelJoint CreateWheel(int index, float3 position, quaternion xToY, quaternion zToY, bool steering)
    {
        const float radius = 0.4f;

        BodyDef wheelDef = BodyDef.Default;
        wheelDef.Type = BodyType.Dynamic;
        wheelDef.Position = position;
        wheelDef.Rotation = quaternion.RotateX(math.PI / 2f); // y→z like the sample
        wheelDef.AllowFastRotation = true;
        Body wheel = _world.CreateBody(wheelDef);

        ShapeDef wheelShape = ShapeDef.Default;
        wheelShape.Density = 2f;
        wheelShape.BaseMaterial.Friction = 3f;
        wheel.CreateSphereShape(wheelShape, new Sphere { Radius = radius });
        _wheels[index] = wheel;

        WheelJointDef jointDef = WheelJointDef.Default;
        jointDef.Base.BodyIdA = _chassis.Id;
        jointDef.Base.BodyIdB = wheel.Id;
        jointDef.Base.LocalFrameA = new B3Transform
        {
            Position = new float3(position.x, -0.5f, position.z),
            Rotation = xToY,
        };
        jointDef.Base.LocalFrameB = new B3Transform { Position = float3.zero, Rotation = zToY };
        jointDef.EnableSuspensionSpring = true;
        jointDef.SuspensionHertz = 4f;
        jointDef.SuspensionDampingRatio = 0.7f;
        jointDef.EnableSuspensionLimit = true;
        jointDef.LowerSuspensionLimit = -0.2f;
        jointDef.UpperSuspensionLimit = 0.2f;
        jointDef.EnableSpinMotor = !steering;
        jointDef.MaxSpinTorque = MaxSpinTorque;
        jointDef.EnableSteering = steering;
        jointDef.SteeringHertz = 10f;
        jointDef.SteeringDampingRatio = 0.7f;
        jointDef.MaxSteeringTorque = 5f;
        jointDef.EnableSteeringLimit = true;
        jointDef.LowerSteeringLimit = -math.radians(MaxSteeringDegrees);
        jointDef.UpperSteeringLimit = math.radians(MaxSteeringDegrees);
        WheelJoint joint = _world.CreateWheelJoint(jointDef);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = $"Wheel {index}";
        visual.transform.localScale = Vector3.one * (radius * 2f);
        Destroy(visual.GetComponent<Collider>());
        _wheelVisuals[index] = visual.transform;

        return joint;
    }

    private void FixedUpdate()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float throttle = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        float steer = (keyboard.aKey.isPressed ? 1f : 0f) - (keyboard.dKey.isPressed ? 1f : 0f);

        // The sample negates spin speed: positive throttle drives forward with these frames.
        _rearLeft.SetSpinMotorSpeed(-SpinSpeed * throttle);
        _rearRight.SetSpinMotorSpeed(-SpinSpeed * throttle);
        _frontLeft.SetTargetSteeringAngle(math.radians(MaxSteeringDegrees) * steer);
        _frontRight.SetTargetSteeringAngle(math.radians(MaxSteeringDegrees) * steer);
        if (throttle != 0f || steer != 0f) ((Box3d.Joint)_rearLeft).WakeBodies();

        _world.Step(Time.fixedDeltaTime);

        B3Transform chassisTransform = _chassis.Transform;
        _chassisVisual.SetPositionAndRotation(chassisTransform.Position, chassisTransform.Rotation);
        for (int i = 0; i < 4; i++)
        {
            B3Transform wheelTransform = _wheels[i].Transform;
            _wheelVisuals[i].SetPositionAndRotation(wheelTransform.Position, wheelTransform.Rotation);
        }
    }

    private void LateUpdate()
    {
        if (!_camera) return;
        float3 chassisPosition = _chassis.Position;
        float3 forward = math.mul(_chassis.Rotation, new float3(1f, 0f, 0f));
        Vector3 target = (Vector3)(chassisPosition - forward * 8f + new float3(0f, 4f, 0f));
        _camera.transform.position = Vector3.Lerp(_camera.transform.position, target, 5f * Time.deltaTime);
        _camera.transform.LookAt((Vector3)chassisPosition);
    }

    private void Update()
    {
        if (DebugDraw && _world.IsValid)
        {
            _world.DrawDebug(DebugDrawFlags.Shapes | DebugDrawFlags.Joints);
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10f, 10f, 700f, 30f), "W/S throttle (rear spin motors), A/D steer (front wheel joints) — box3d");
    }

    private void OnDestroy()
    {
        if (_world.IsValid) _world.Destroy();
        if (_terrainMesh.IsCreated) _terrainMesh.Destroy();
    }
}
