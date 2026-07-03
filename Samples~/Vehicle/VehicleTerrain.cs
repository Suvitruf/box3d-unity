using UnityEngine;

/// <summary>Builds the wavy driving terrain shared by both vehicle sandboxes: one vertex/triangle
/// set used for the box3d TriangleMesh, the PhysX MeshCollider, and the rendered mesh — so both
/// engines drive on exactly the same surface.</summary>
public static class VehicleTerrain
{
    public const int CellsPerSide = 60;
    public const float CellSize = 1.5f;

    public static Vector3[] BuildVertices()
    {
        int lines = CellsPerSide + 1;
        var vertices = new Vector3[lines * lines];
        float origin = -CellsPerSide * CellSize * 0.5f;
        for (int z = 0; z < lines; z++)
        {
            for (int x = 0; x < lines; x++)
            {
                float wx = origin + x * CellSize;
                float wz = origin + z * CellSize;
                float height = 0.5f * Mathf.Sin(0.25f * wx) + 0.5f * Mathf.Cos(0.2f * wz);
                vertices[z * lines + x] = new Vector3(wx, height, wz);
            }
        }
        return vertices;
    }

    public static int[] BuildTriangles()
    {
        int lines = CellsPerSide + 1;
        var triangles = new int[CellsPerSide * CellsPerSide * 6];
        int t = 0;
        for (int z = 0; z < CellsPerSide; z++)
        {
            for (int x = 0; x < CellsPerSide; x++)
            {
                int i0 = z * lines + x;
                int i1 = i0 + 1;
                int i2 = i0 + lines;
                int i3 = i2 + 1;
                triangles[t++] = i0; triangles[t++] = i2; triangles[t++] = i1;
                triangles[t++] = i1; triangles[t++] = i2; triangles[t++] = i3;
            }
        }
        return triangles;
    }

    /// <summary>Creates the rendered terrain GameObject; returns its mesh (also usable for a
    /// MeshCollider).</summary>
    public static Mesh CreateVisual(Vector3[] vertices, int[] triangles)
    {
        var mesh = new Mesh { name = "Vehicle Terrain" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        var visual = new GameObject("Terrain");
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = new Color(0.45f, 0.55f, 0.35f),
        };
        return mesh;
    }
}
