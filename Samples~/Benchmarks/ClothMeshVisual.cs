using UnityEngine;

/// <summary>Renders a cloth node grid as a connected mesh and updates vertices from node
/// positions each frame. Shared by the Box3D and PhysX cloth sandboxes. Single-winding
/// triangles; both sides visible via Cull Off on the material (duplicating triangles with
/// flipped winding z-fights and corrupts recalculated normals).</summary>
public class ClothMeshVisual
{
    private readonly Mesh _mesh;
    private readonly Vector3[] _vertices;
    private readonly GameObject _visual;
    private readonly Material _material;

    public ClothMeshVisual(int width, int height)
    {
        _vertices = new Vector3[width * height];

        int quadCount = (width - 1) * (height - 1);
        var triangles = new int[quadCount * 6];
        int t = 0;
        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int i0 = y * width + x;
                int i1 = i0 + 1;
                int i2 = i0 + width;
                int i3 = i2 + 1;

                triangles[t++] = i0; triangles[t++] = i2; triangles[t++] = i1;
                triangles[t++] = i1; triangles[t++] = i2; triangles[t++] = i3;
            }
        }

        _mesh = new Mesh { name = "Cloth" };
        _mesh.MarkDynamic();
        _mesh.vertices = _vertices;
        _mesh.triangles = triangles;

        var visual = new GameObject("Cloth Visual");
        visual.AddComponent<MeshFilter>().sharedMesh = _mesh;
        MeshRenderer renderer = visual.AddComponent<MeshRenderer>();

        var material = new Material(FindLitShader())
        {
            color = new Color(0.85f, 0.3f, 0.25f),
        };
        material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

        _visual = visual;
        _material = material;
    }

    /// <summary>Destroys the visual GameObject and the runtime mesh/material it owns (they are
    /// not scene objects, so destroying the GameObject alone would leak them).</summary>
    public void Destroy()
    {
        if (_visual) Object.Destroy(_visual);
        if (_mesh) Object.Destroy(_mesh);
        if (_material) Object.Destroy(_material);
    }

    public void SetVertex(int index, Vector3 position)
    {
        _vertices[index] = position;
    }

    public void Apply()
    {
        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    private static Shader FindLitShader()
    {
        // URP first (the dev project's pipeline); fall back for Built-in RP consumers.
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        if (!shader) shader = Shader.Find("Sprites/Default");
        return shader;
    }
}
