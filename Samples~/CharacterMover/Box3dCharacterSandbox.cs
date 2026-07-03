using System;
using Box3d;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Kinematic character controller built on box3d's mover toolkit: WASD to walk,
/// Space to jump. Each step: CollideMover gathers planes → SolvePlanes corrects the movement
/// delta → ClipVector removes velocity into surfaces. The mover is not a rigid body — the
/// environment is static geometry it slides along.</summary>
public class Box3dCharacterSandbox : MonoBehaviour
{
    [SerializeField, Tooltip("Walk speed in m/s.")]
    private float WalkSpeed = 5f;

    [SerializeField, Tooltip("Jump start velocity in m/s.")]
    private float JumpSpeed = 6f;

    [SerializeField, Tooltip("Gravity applied to the mover.")]
    private float Gravity = 15f;

    private World _world;
    private float3 _position = new float3(0f, 1f, -6f);
    private float3 _velocity;
    private bool _grounded;
    private Transform _visual;

    private static readonly Capsule MoverCapsule = new Capsule
    {
        Center1 = new float3(0f, 0.4f, 0f),
        Center2 = new float3(0f, 1.4f, 0f),
        Radius = 0.4f,
    };

    private void Start()
    {
        _world = World.Create(WorldDef.Default);
        CreateEnvironment();

        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Character";
        capsule.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
        Destroy(capsule.GetComponent<Collider>());
        _visual = capsule.transform;
    }

    private void CreateEnvironment()
    {
        AddStaticBox(new float3(0f, -0.5f, 0f), quaternion.identity, new float3(15f, 0.5f, 15f));

        // Stairs of boxes.
        for (int i = 0; i < 5; i++)
        {
            AddStaticBox(new float3(3f + i * 1.2f, i * 0.35f, 2f), quaternion.identity, new float3(0.6f, 0.35f + i * 0.35f, 2f));
        }

        // Ramp: a rotated box.
        AddStaticBox(new float3(-4f, 0.8f, 3f), quaternion.RotateZ(math.radians(20f)), new float3(3f, 0.25f, 2f));

        // Scattered obstacles.
        AddStaticBox(new float3(0f, 0.75f, 4f), quaternion.RotateY(0.7f), new float3(0.75f, 0.75f, 0.75f));
        AddStaticBox(new float3(-3f, 0.5f, -3f), quaternion.RotateY(0.3f), new float3(0.5f, 0.5f, 0.5f));
    }

    private void AddStaticBox(float3 position, quaternion rotation, float3 halfExtents)
    {
        BodyDef def = BodyDef.Default;
        def.Position = position;
        def.Rotation = rotation;
        Body body = _world.CreateBody(def);
        BoxHull hull = BoxHull.Create(halfExtents.x, halfExtents.y, halfExtents.z);
        body.CreateHullShape(ShapeDef.Default, in hull);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetPositionAndRotation(position, rotation);
        visual.transform.localScale = (Vector3)(halfExtents * 2f);
        Destroy(visual.GetComponent<Collider>());
    }

    private void FixedUpdate()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float3 input = float3.zero;
        if (keyboard.wKey.isPressed) input.z += 1f;
        if (keyboard.sKey.isPressed) input.z -= 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (math.lengthsq(input) > 0f) input = math.normalize(input);

        _velocity.x = input.x * WalkSpeed;
        _velocity.z = input.z * WalkSpeed;
        _velocity.y -= Gravity * Time.fixedDeltaTime;
        if (_grounded && keyboard.spaceKey.isPressed) _velocity.y = JumpSpeed;

        StepMover(Time.fixedDeltaTime);

        _visual.position = (Vector3)(_position + new float3(0f, 0.9f, 0f));
    }

    private void StepMover(float deltaTime)
    {
        Span<CollisionPlane> planes = stackalloc CollisionPlane[16];
        int planeCount = _world.CollideMover(_position, MoverCapsule, QueryFilter.Default, planes);
        Span<CollisionPlane> active = planes.Slice(0, planeCount);

        _grounded = false;
        for (int i = 0; i < planeCount; i++)
        {
            if (active[i].Plane.Normal.y > 0.7f) _grounded = true;
        }

        float3 targetDelta = _velocity * deltaTime;
        PlaneSolverResult solved = Mover.SolvePlanes(targetDelta, active);
        _position += solved.Delta;
        _velocity = Mover.ClipVector(_velocity, active);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10f, 10f, 700f, 30f),
            $"WASD to walk, Space to jump — {(_grounded ? "grounded" : "airborne")} (box3d mover toolkit)");
    }

    private void OnDestroy()
    {
        if (_world.IsValid) _world.Destroy();
    }
}
