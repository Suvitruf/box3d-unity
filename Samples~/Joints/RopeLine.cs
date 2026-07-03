using UnityEngine;

/// <summary>Creates the simple two-point line used to visualize rope joints in the sandboxes.</summary>
public static class RopeLine
{
    public static LineRenderer Create()
    {
        var line = new GameObject("Rope").AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.04f;
        line.endWidth = 0.04f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.black;
        line.endColor = Color.black;
        return line;
    }
}
