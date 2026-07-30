using Box3D.Hybrid;
using UnityEngine;

/// <summary>Test helper: generates a bumpy mesh at runtime and gives it a static Box3DMeshShape,
/// so the mesh-collider path can be exercised without importing (and Read/Write-enabling) a mesh
/// asset. Drop this on an empty GameObject and press play.</summary>
public class Box3DMeshTerrain : MonoBehaviour
{
    [SerializeField, Min(2), Tooltip("Grid cells per side.")]
    private int Cells = 24;

    [SerializeField, Min(0.1f), Tooltip("Cell size in meters.")]
    private float CellSize = 0.8f;

    [SerializeField, Min(0f), Tooltip("Bump height.")]
    private float Amplitude = 0.5f;

    [SerializeField, Tooltip("Material for the terrain surface.")]
    private Material Material;

    private Mesh _mesh;

    private void Start()
    {
        Mesh mesh = _mesh = BuildMesh();

        // Inactive during setup so Box3DBody.Awake creates the shape after SetMesh/BodyType are set.
        var terrain = new GameObject("Mesh Terrain (generated)");
        terrain.transform.position = transform.position;
        terrain.SetActive(false);

        terrain.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = terrain.AddComponent<MeshRenderer>();
        if (Material) renderer.sharedMaterial = Material;

        terrain.AddComponent<Box3DMeshShape>().SetMesh(mesh);
        terrain.AddComponent<Box3DBody>().BodyType = Box3DBodyType.Static;

        terrain.SetActive(true);
    }

    private void OnDestroy()
    {
        if (_mesh) Destroy(_mesh); // runtime mesh — not freed with the GameObject
    }

    private Mesh BuildMesh()
    {
        int lines = Cells + 1;
        var vertices = new Vector3[lines * lines];
        float origin = -Cells * CellSize * 0.5f;
        for (int z = 0; z < lines; z++)
        {
            for (int x = 0; x < lines; x++)
            {
                float wx = origin + x * CellSize;
                float wz = origin + z * CellSize;
                float height = Amplitude * (Mathf.Sin(wx * 0.6f) + Mathf.Cos(wz * 0.5f)) * 0.5f;
                vertices[z * lines + x] = new Vector3(wx, height, wz);
            }
        }

        var triangles = new int[Cells * Cells * 6];
        int t = 0;
        for (int z = 0; z < Cells; z++)
        {
            for (int x = 0; x < Cells; x++)
            {
                int i0 = z * lines + x;
                int i1 = i0 + 1;
                int i2 = i0 + lines;
                int i3 = i2 + 1;
                triangles[t++] = i0; triangles[t++] = i2; triangles[t++] = i1;
                triangles[t++] = i1; triangles[t++] = i2; triangles[t++] = i3;
            }
        }

        var mesh = new Mesh { name = "Box3D Terrain" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
