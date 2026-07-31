using System.Collections.Generic;
using Box3D.Hybrid;
using UnityEngine;

/// <summary>Test harness for the MonoBehaviour component layer: buttons spawn primitives carrying
/// <see cref="Box3DBody"/> + a shape component (added at runtime), which fall onto the scene's
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
    private readonly List<Material> _materials = new List<Material>();
    private GUIStyle _headerStyle; // created once inside OnGUI (GUI.skin is only valid there)

    // Tints a fresh instance of BaseMaterial and tracks it: runtime-created materials are not
    // scene objects, so destroying the GameObject alone would leak them.
    private void ApplyTintedMaterial(GameObject visual)
    {
        if (!BaseMaterial) return;
        var material = new Material(BaseMaterial) { color = Color.HSVToRGB(Random.value, 0.55f, 1f) };
        visual.GetComponent<MeshRenderer>().material = material;
        _materials.Add(material);
    }

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
            // Inactive during setup so Box3DBody.Awake (which gathers shapes) runs AFTER the
            // shape component is added.
            GameObject visual = GameObject.CreatePrimitive(primitive);
            visual.SetActive(false);
            Destroy(visual.GetComponent<Collider>());
            visual.transform.position = new Vector3(
                Random.Range(-3f, 3f), Random.Range(6f, 11f), Random.Range(-3f, 3f));
            visual.transform.rotation = Random.rotation;

            ApplyTintedMaterial(visual);

            Box3DShape shape = AddShape(visual, kind);
            shape.SetRestitution(Bounciness); // set before the body bakes the shape on activation
            visual.AddComponent<Box3DBody>();

            visual.SetActive(true);
            _spawned.Add(visual);
        }
    }

    private static Box3DShape AddShape(GameObject visual, ShapeKind kind)
    {
        switch (kind)
        {
            case ShapeKind.Box:
                return visual.AddComponent<Box3DBoxShape>();
            case ShapeKind.Capsule:
                return visual.AddComponent<Box3DCapsuleShape>();
            case ShapeKind.Hull:
                Box3DHullShape hull = visual.AddComponent<Box3DHullShape>();
                hull.SetMesh(visual.GetComponent<MeshFilter>().sharedMesh);
                return hull;
            default:
                return visual.AddComponent<Box3DSphereShape>();
        }
    }

    // A compound body: one Box3DBody with two child sphere shapes. If compound gathering works the
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
                ApplyTintedMaterial(ball);
                ball.AddComponent<Box3DSphereShape>().SetRestitution(Bounciness);
            }

            root.AddComponent<Box3DBody>(); // gathers both child sphere shapes
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
        Box3DBody previous = null; // null on the first link → hinged to the world

        for (int i = 0; i < links; i++)
        {
            GameObject link = GameObject.CreatePrimitive(PrimitiveType.Cube);
            link.SetActive(false);
            Destroy(link.GetComponent<Collider>());
            link.transform.position = anchor + new Vector3((i + 0.5f) * length, 0f, 0f);
            link.transform.localScale = new Vector3(length, thickness, thickness);

            ApplyTintedMaterial(link);

            link.AddComponent<Box3DBoxShape>();
            Box3DBody body = link.AddComponent<Box3DBody>();
            Box3DHingeJoint hinge = link.AddComponent<Box3DHingeJoint>();
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

    // A sphere hung from a fixed world point by a distance-joint spring, started stretched so it
    // bobs. Exercises the distance joint + spring + world connection.
    private void SpawnSpring()
    {
        for (int i = 0; i < SpawnCount; i++)
        {
            Vector3 top = new Vector3(Random.Range(-3f, 3f), 11f, Random.Range(-3f, 3f));

            GameObject bob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bob.SetActive(false);
            Destroy(bob.GetComponent<Collider>());
            bob.transform.position = top - new Vector3(0f, 3f, 0f); // hangs below, stretched
            ApplyTintedMaterial(bob);

            bob.AddComponent<Box3DSphereShape>();
            bob.AddComponent<Box3DBody>();

            Box3DDistanceJoint spring = bob.AddComponent<Box3DDistanceJoint>();
            spring.SetConnectedBody(null);        // hang from the world
            spring.SetConnectedAnchor(top);       // world anchor point
            spring.SetLength(1.5f);               // shorter than the 3 m drop → springs up
            spring.SetSpring(hertz: 3f, dampingRatio: 0.3f);

            bob.SetActive(true);
            _spawned.Add(bob);
        }
    }

    // A ball-joint chain, laid out as a horizontal arc curving into z. Because it starts non-planar,
    // gravity makes it swing AND twist in 3D through the free ball joints — visibly floppier than
    // the single-plane hinge chain (which couldn't follow this shape).
    private void SpawnBallChain()
    {
        const int links = 7;
        const float length = 0.6f;
        const float thickness = 0.15f;

        Vector3 anchor = new Vector3(Random.Range(-3f, 3f), 10f, Random.Range(-3f, 3f));
        var linkObjects = new List<GameObject>();
        Box3DBody previous = null;

        Vector3 cursor = anchor;
        Quaternion dir = Quaternion.identity;
        Quaternion turn = Quaternion.AngleAxis(22f, Vector3.up); // curve the chain into z

        for (int i = 0; i < links; i++)
        {
            Vector3 forward = dir * Vector3.right;

            GameObject link = GameObject.CreatePrimitive(PrimitiveType.Cube);
            link.SetActive(false);
            Destroy(link.GetComponent<Collider>());
            link.transform.SetPositionAndRotation(cursor + forward * (length * 0.5f), dir);
            link.transform.localScale = new Vector3(length, thickness, thickness);
            ApplyTintedMaterial(link);

            link.AddComponent<Box3DBoxShape>();
            Box3DBody body = link.AddComponent<Box3DBody>();
            Box3DBallJoint ball = link.AddComponent<Box3DBallJoint>();
            ball.SetConnectedBody(previous);
            ball.SetAnchor(new Vector3(-0.5f, 0f, 0f));

            linkObjects.Add(link);
            previous = body;
            cursor += forward * length;
            dir = turn * dir;
        }

        foreach (GameObject link in linkObjects)
        {
            link.SetActive(true);
            _spawned.Add(link);
        }
    }

    private void Clear()
    {
        foreach (GameObject go in _spawned)
        {
            if (go) Destroy(go); // Box3DBody.OnDisable destroys the native body
        }
        _spawned.Clear();
        foreach (Material material in _materials)
        {
            if (material) Destroy(material);
        }
        _materials.Clear();
    }

    private void OnDestroy()
    {
        Clear();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10f, 10f, 220f, 400f), GUI.skin.box);
        _headerStyle ??= new GUIStyle(GUI.skin.label) { richText = true };
        GUILayout.Label("<b>Component layer test</b>", _headerStyle);

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
        if (GUILayout.Button("Spawn ball chain")) SpawnBallChain();
        if (GUILayout.Button("Spawn spring")) SpawnSpring();
        if (GUILayout.Button("Clear")) Clear();

        GUILayout.FlexibleSpace();
        GUILayout.Label($"Live objects: {_spawned.Count}");
        GUILayout.EndArea();
    }
}
