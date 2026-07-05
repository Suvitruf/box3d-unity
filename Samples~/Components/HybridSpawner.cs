using System.Collections.Generic;
using Box3d.Hybrid;
using UnityEngine;

/// <summary>Test harness for the MonoBehaviour component layer: buttons spawn primitives carrying
/// <see cref="Box3dBody"/> + a shape component (added at runtime), which fall onto the scene's
/// static floor. Proves the hybrid layer works both scene-authored (the floor) and runtime-added.</summary>
public class HybridSpawner : MonoBehaviour
{
    private enum ShapeKind
    {
        Sphere,
        Box,
        Capsule,
        Hull,
    }

    [SerializeField, Tooltip("Material applied to spawned objects (random tint per object).")]
    private Material BaseMaterial;

    [SerializeField, Range(1, 30), Tooltip("Objects added per button press.")]
    private int SpawnCount = 5;

    [SerializeField, Range(0f, 1f), Tooltip("Bounciness of spawned objects. Restitution combines as max, so this alone makes them bounce off the floor.")]
    private float Bounciness = 0.5f;

    private readonly List<GameObject> _spawned = new List<GameObject>();

    private void Spawn(ShapeKind kind)
    {
        // Hull spawns a cube primitive: its convex hull is a clean box, so a mismatch would be
        // obvious. The other kinds use their matching analytic shape.
        PrimitiveType primitive = kind switch
        {
            ShapeKind.Sphere => PrimitiveType.Sphere,
            ShapeKind.Capsule => PrimitiveType.Capsule,
            _ => PrimitiveType.Cube,
        };

        for (int i = 0; i < SpawnCount; i++)
        {
            // Inactive during setup so Box3dBody.Awake (which gathers shapes) runs AFTER the
            // shape component is added.
            GameObject visual = GameObject.CreatePrimitive(primitive);
            visual.SetActive(false);
            Destroy(visual.GetComponent<Collider>());
            visual.transform.position = new Vector3(
                Random.Range(-3f, 3f), Random.Range(6f, 11f), Random.Range(-3f, 3f));
            visual.transform.rotation = Random.rotation;

            if (BaseMaterial)
            {
                visual.GetComponent<MeshRenderer>().material =
                    new Material(BaseMaterial) { color = Color.HSVToRGB(Random.value, 0.55f, 1f) };
            }

            Box3dShape shape = AddShape(visual, kind);
            shape.SetRestitution(Bounciness); // set before the body bakes the shape on activation
            visual.AddComponent<Box3dBody>();

            visual.SetActive(true);
            _spawned.Add(visual);
        }
    }

    private static Box3dShape AddShape(GameObject visual, ShapeKind kind)
    {
        switch (kind)
        {
            case ShapeKind.Box:
                return visual.AddComponent<Box3dBoxShape>();
            case ShapeKind.Capsule:
                return visual.AddComponent<Box3dCapsuleShape>();
            case ShapeKind.Hull:
                Box3dHullShape hull = visual.AddComponent<Box3dHullShape>();
                hull.SetMesh(visual.GetComponent<MeshFilter>().sharedMesh);
                return hull;
            default:
                return visual.AddComponent<Box3dSphereShape>();
        }
    }

    // A compound body: one Box3dBody with two child sphere shapes. If compound gathering works the
    // two spheres tumble together as one rigid "peanut"; if not, they'd fall independently.
    private void SpawnCompound()
    {
        for (int i = 0; i < SpawnCount; i++)
        {
            var root = new GameObject("Compound Peanut");
            root.SetActive(false);
            root.transform.position = new Vector3(
                Random.Range(-3f, 3f), Random.Range(6f, 11f), Random.Range(-3f, 3f));
            root.transform.rotation = Random.rotation;

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(ball.GetComponent<Collider>());
                ball.transform.SetParent(root.transform, worldPositionStays: false);
                ball.transform.localPosition = new Vector3(side * 0.6f, 0f, 0f);
                if (BaseMaterial)
                {
                    ball.GetComponent<MeshRenderer>().material =
                        new Material(BaseMaterial) { color = Color.HSVToRGB(Random.value, 0.55f, 1f) };
                }
                ball.AddComponent<Box3dSphereShape>().SetRestitution(Bounciness);
            }

            root.AddComponent<Box3dBody>(); // gathers both child sphere shapes
            root.SetActive(true);
            _spawned.Add(root);
        }
    }

    // A hinge chain anchored to the world at one end, laid out horizontally so it whips down under
    // gravity. Exercises hinge joints, connected bodies, and the world anchor (first link's null
    // connected body). Joints are created in Start, after every link's body exists.
    private void SpawnChain()
    {
        const int links = 6;
        const float length = 0.6f;
        const float thickness = 0.15f;

        Vector3 anchor = new Vector3(Random.Range(-3f, 3f), 10f, Random.Range(-3f, 3f));
        var linkObjects = new List<GameObject>();
        Box3dBody previous = null; // null on the first link → hinged to the world

        for (int i = 0; i < links; i++)
        {
            GameObject link = GameObject.CreatePrimitive(PrimitiveType.Cube);
            link.SetActive(false);
            Destroy(link.GetComponent<Collider>());
            link.transform.position = anchor + new Vector3((i + 0.5f) * length, 0f, 0f);
            link.transform.localScale = new Vector3(length, thickness, thickness);

            if (BaseMaterial)
            {
                link.GetComponent<MeshRenderer>().material =
                    new Material(BaseMaterial) { color = Color.HSVToRGB(Random.value, 0.55f, 1f) };
            }

            link.AddComponent<Box3dBoxShape>();
            Box3dBody body = link.AddComponent<Box3dBody>();
            Box3dHingeJoint hinge = link.AddComponent<Box3dHingeJoint>();
            hinge.SetConnectedBody(previous);
            hinge.SetAnchor(new Vector3(-0.5f, 0f, 0f)); // the link's left end (local)
            hinge.SetAxis(new Vector3(0f, 0f, 1f));      // swing in the x-y plane

            linkObjects.Add(link);
            previous = body;
        }

        foreach (GameObject link in linkObjects)
        {
            link.SetActive(true); // Awake creates bodies now; joints create in Start next frame
            _spawned.Add(link);
        }
    }

    private void Clear()
    {
        foreach (GameObject go in _spawned)
        {
            if (go) Destroy(go); // Box3dBody.OnDisable destroys the native body
        }
        _spawned.Clear();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10f, 10f, 220f, 350f), GUI.skin.box);
        GUILayout.Label("<b>Component layer test</b>", new GUIStyle(GUI.skin.label) { richText = true });

        GUILayout.Label($"Count per press: {SpawnCount}");
        SpawnCount = (int)GUILayout.HorizontalSlider(SpawnCount, 1f, 30f);

        GUILayout.Label($"Bounciness: {Bounciness:F2}");
        Bounciness = GUILayout.HorizontalSlider(Bounciness, 0f, 1f);

        GUILayout.Space(6f);
        if (GUILayout.Button($"Spawn {SpawnCount} spheres")) Spawn(ShapeKind.Sphere);
        if (GUILayout.Button($"Spawn {SpawnCount} boxes")) Spawn(ShapeKind.Box);
        if (GUILayout.Button($"Spawn {SpawnCount} capsules")) Spawn(ShapeKind.Capsule);
        if (GUILayout.Button($"Spawn {SpawnCount} hulls")) Spawn(ShapeKind.Hull);
        if (GUILayout.Button($"Spawn {SpawnCount} compounds")) SpawnCompound();
        if (GUILayout.Button("Spawn hinge chain")) SpawnChain();
        if (GUILayout.Button("Clear")) Clear();

        GUILayout.FlexibleSpace();
        GUILayout.Label($"Live objects: {_spawned.Count}");
        GUILayout.EndArea();
    }
}
