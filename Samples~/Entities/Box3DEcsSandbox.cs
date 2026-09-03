using System.Collections.Generic;
using Box3D.Entities;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>ECS sandbox: a box pyramid and sphere rain living as pure entities (no GameObjects),
/// simulated by the Box3D.Entities systems running on their own in FixedStepSimulationSystemGroup.
/// This script only spawns entities and draws them with instanced meshes straight from their
/// <c>LocalTransform</c>. Left-click drops a burst of spheres at the cursor. Inspect the live
/// entities via Window ▸ Entities ▸ Hierarchy.</summary>
public class Box3DEcsSandbox : MonoBehaviour
{
    [SerializeField, Tooltip("Spheres rained onto the pyramid at start.")]
    private int SphereCount = 150;

    [SerializeField, Tooltip("Base width (in boxes) of the starting pyramid.")]
    private int PyramidBase = 8;

    [SerializeField, Tooltip("Spawn area half-extent on X/Z for the sphere rain.")]
    private float SpawnRadius = 6f;

    [SerializeField, Tooltip("Height range spheres spawn in.")]
    private Vector2 SpawnHeight = new Vector2(8f, 20f);

    [SerializeField, Tooltip("Spheres per mouse-click burst.")]
    private int BurstCount = 20;

    private EntityManager _entityManager;
    private Camera _camera;
    private Mesh _sphereMesh;
    private Mesh _cubeMesh;
    private Material _sphereMaterial;
    private Material _boxMaterial;
    private Material _groundMaterial;
    private Matrix4x4 _groundMatrix;
    private Matrix4x4[] _matrices = new Matrix4x4[1024];
    private readonly List<Entity> _spheres = new List<Entity>();
    private readonly List<Entity> _boxes = new List<Entity>();

    private void Start()
    {
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            Debug.LogError("[Box3DEcsSandbox] No default ECS world — is the Entities package installed?");
            enabled = false;
            return;
        }
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _camera = Camera.main;

        _sphereMesh = TakePrimitiveMesh(PrimitiveType.Sphere);
        _cubeMesh = TakePrimitiveMesh(PrimitiveType.Cube);
        _sphereMaterial = MakeInstancedMaterial(new Color(0.9f, 0.5f, 0.2f));
        _boxMaterial = MakeInstancedMaterial(new Color(0.35f, 0.6f, 0.9f));
        _groundMaterial = MakeInstancedMaterial(new Color(0.45f, 0.45f, 0.45f));

        CreateBody(Box3DBodyDefinition.Static, Box3DShapeDefinition.Box(new float3(10f, 0.5f, 10f)), float3.zero);
        _groundMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(20f, 1f, 20f));

        CreatePyramid();
        for (int i = 0; i < SphereCount; i++)
        {
            float3 position = new float3(
                UnityEngine.Random.Range(-SpawnRadius, SpawnRadius),
                UnityEngine.Random.Range(SpawnHeight.x, SpawnHeight.y),
                UnityEngine.Random.Range(-SpawnRadius, SpawnRadius));
            _spheres.Add(CreateBody(Box3DBodyDefinition.Dynamic, Box3DShapeDefinition.Sphere(0.5f), position));
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) SpawnBurst(mouse.position.ReadValue());

        var groundParams = new RenderParams(_groundMaterial)
        {
            worldBounds = new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f)),
        };
        Graphics.RenderMesh(groundParams, _cubeMesh, 0, _groundMatrix);
        DrawInstanced(_cubeMesh, _boxMaterial, _boxes);
        DrawInstanced(_sphereMesh, _sphereMaterial, _spheres);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10f, 10f, 700f, 60f),
            $"Box3D ECS: {_boxes.Count + _spheres.Count} dynamic bodies as pure entities, " +
            "simulated by Box3D.Entities systems (FixedStepSimulationSystemGroup)\n" +
            $"Left-click: drop {BurstCount} spheres. Window ▸ Entities ▸ Hierarchy shows them live.");
    }

    private void CreatePyramid()
    {
        for (int row = 0; row < PyramidBase; row++)
        {
            int count = PyramidBase - row;
            float y = 1.001f + row * 1.002f; // ground top 0.5 + box half 0.5, with a hair of slack
            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) * 0.5f) * 1.002f;
                _boxes.Add(CreateBody(Box3DBodyDefinition.Dynamic,
                    Box3DShapeDefinition.Box(new float3(0.5f, 0.5f, 0.5f)), new float3(x, y, 0f)));
            }
        }
    }

    private void SpawnBurst(Vector2 screenPosition)
    {
        Ray ray = _camera.ScreenPointToRay(screenPosition);
        if (ray.direction.y >= -0.01f) return;
        Vector3 target = ray.origin - ray.direction * (ray.origin.y / ray.direction.y);
        target.x = Mathf.Clamp(target.x, -8f, 8f);
        target.z = Mathf.Clamp(target.z, -8f, 8f);

        for (int i = 0; i < BurstCount; i++)
        {
            Vector3 jitter = UnityEngine.Random.insideUnitSphere * 1.5f;
            float3 position = new float3(target.x + jitter.x, 10f + i * 1.1f, target.z + jitter.z);
            _spheres.Add(CreateBody(Box3DBodyDefinition.Dynamic, Box3DShapeDefinition.Sphere(0.5f), position));
        }
    }

    private Entity CreateBody(Box3DBodyDefinition body, Box3DShapeDefinition shape, float3 position)
    {
        Entity entity = _entityManager.CreateEntity();
        _entityManager.AddComponentData(entity, body);
        _entityManager.AddComponentData(entity, shape);
        _entityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private void DrawInstanced(Mesh mesh, Material material, List<Entity> entities)
    {
        if (entities.Count == 0) return;
        if (_matrices.Length < entities.Count) _matrices = new Matrix4x4[Mathf.NextPowerOfTwo(entities.Count)];
        for (int i = 0; i < entities.Count; i++)
        {
            LocalTransform transform = _entityManager.GetComponentData<LocalTransform>(entities[i]);
            _matrices[i] = Matrix4x4.TRS(transform.Position, transform.Rotation, Vector3.one);
        }

        var renderParams = new RenderParams(material)
        {
            worldBounds = new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f)),
            shadowCastingMode = ShadowCastingMode.On,
            receiveShadows = true,
        };
        for (int start = 0; start < entities.Count; start += 1023)
        {
            Graphics.RenderMeshInstanced(renderParams, mesh, 0, _matrices,
                Mathf.Min(1023, entities.Count - start), start);
        }
    }

    private static Mesh TakePrimitiveMesh(PrimitiveType type)
    {
        GameObject temp = GameObject.CreatePrimitive(type);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);
        return mesh;
    }

    private static Material MakeInstancedMaterial(Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { enableInstancing = true };
        material.SetColor("_BaseColor", color);
        return material;
    }
}
