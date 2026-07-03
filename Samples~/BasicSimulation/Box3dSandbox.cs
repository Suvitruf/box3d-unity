using System;
using Box3d;
using Unity.Mathematics;
using UnityEngine;

/// <summary>M1 sandbox: spheres raining onto a Box3d ground hull, rendered with Unity primitives.
/// Steps the world from FixedUpdate, syncs transforms via body move events, and shows the average
/// step cost for comparison against <see cref="UnityPhysicsSandbox"/>.</summary>
public class Box3dSandbox : MonoBehaviour
{
    [SerializeField, Tooltip("Number of spheres to drop.")]
    private int SphereCount = 100;

    [SerializeField, Tooltip("Sphere radius in meters.")]
    private float SphereRadius = 0.5f;

    [SerializeField, Tooltip("Spawn area half-extent on X/Z.")]
    private float SpawnRadius = 5f;

    [SerializeField, Tooltip("Height range spheres spawn in.")]
    private Vector2 SpawnHeight = new Vector2(5f, 25f);

    [SerializeField, Tooltip("Random seed so runs are comparable across physics engines.")]
    private int Seed = 12345;

    private World _world;
    private Transform[] _sphereTransforms;

    private void Start()
    {
        UnityEngine.Random.InitState(Seed);

        _world = World.Create(WorldDef.Default);

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
        _sphereTransforms = new Transform[SphereCount];

        for (int i = 0; i < SphereCount; i++)
        {
            float3 position = new float3(
                UnityEngine.Random.Range(-SpawnRadius, SpawnRadius),
                UnityEngine.Random.Range(SpawnHeight.x, SpawnHeight.y),
                UnityEngine.Random.Range(-SpawnRadius, SpawnRadius));

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
            _sphereTransforms[i] = visual.transform;
        }
    }

    private void FixedUpdate()
    {
        _world.Step(Time.fixedDeltaTime);

        foreach (BodyMoveEvent moveEvent in _world.GetBodyMoveEvents())
        {
            int index = (int)moveEvent.UserData;
            _sphereTransforms[index].SetPositionAndRotation(moveEvent.Transform.Position, moveEvent.Transform.Rotation);
        }
    }

    private void OnDestroy()
    {
        if (_world.IsValid) _world.Destroy();
    }
}
