using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    /// <summary>"GameObject → Box3D" creation menu (also in the Hierarchy + button and right-click
    /// menu): one-click physics objects for scene authoring. Dynamic items pair a Unity primitive
    /// for visuals (its PhysX collider removed) with a Box3DBody and the matching Box3D shape, so
    /// what the designer sees is exactly what simulates. Primitive dimensions match the shapes'
    /// defaults (cube 1m, sphere r=0.5, capsule r=0.5 h=2), so nothing needs retuning.</summary>
    internal static class Box3DObjectMenu
    {
        private const string Root = "GameObject/Box3D/";

        [MenuItem(Root + "World", false, 0)]
        private static void CreateWorld(MenuCommand command)
        {
            var go = new GameObject("Box3D World", typeof(Box3DWorld));
            Place(go, command, "Create Box3D World");
        }

        // One world per scene — the item greys out once one exists.
        [MenuItem(Root + "World", true)]
        private static bool ValidateCreateWorld()
        {
            return !Object.FindAnyObjectByType<Box3DWorld>(FindObjectsInactive.Include);
        }

        [MenuItem(Root + "Box", false, 20)]
        private static void CreateBox(MenuCommand command)
        {
            CreateShapePrimitive<Box3DBoxShape>(PrimitiveType.Cube, "Box", command, isStatic: false);
        }

        [MenuItem(Root + "Sphere", false, 21)]
        private static void CreateSphere(MenuCommand command)
        {
            CreateShapePrimitive<Box3DSphereShape>(PrimitiveType.Sphere, "Sphere", command, isStatic: false);
        }

        [MenuItem(Root + "Capsule", false, 22)]
        private static void CreateCapsule(MenuCommand command)
        {
            CreateShapePrimitive<Box3DCapsuleShape>(PrimitiveType.Capsule, "Capsule", command, isStatic: false);
        }

        // An empty body for compound objects: add child shapes (or child primitives from this
        // menu — their Reset sees this body and won't add another).
        [MenuItem(Root + "Empty Body", false, 23)]
        private static void CreateEmptyBody(MenuCommand command)
        {
            var go = new GameObject("Body", typeof(Box3DBody));
            Place(go, command, "Create Box3D Body");
        }

        [MenuItem(Root + "Wind", false, 30)]
        private static void CreateWind(MenuCommand command)
        {
            var go = new GameObject("Wind", typeof(Box3DWind));
            Place(go, command, "Create Box3D Wind");
        }

        [MenuItem(Root + "Explosion", false, 31)]
        private static void CreateExplosion(MenuCommand command)
        {
            var go = new GameObject("Explosion", typeof(Box3DExplosion));
            Place(go, command, "Create Box3D Explosion");
        }

        [MenuItem(Root + "Rope", false, 32)]
        private static void CreateRope(MenuCommand command)
        {
            var go = new GameObject("Rope", typeof(LineRenderer), typeof(Box3DRope));
            var line = go.GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            Place(go, command, "Create Box3D Rope");
        }

        [MenuItem(Root + "Static Box", false, 40)]
        private static void CreateStaticBox(MenuCommand command)
        {
            CreateShapePrimitive<Box3DBoxShape>(PrimitiveType.Cube, "Static Box", command, isStatic: true);
        }

        [MenuItem(Root + "Ground Plane", false, 41)]
        private static void CreateGround(MenuCommand command)
        {
            GameObject go = CreateShapePrimitive<Box3DBoxShape>(PrimitiveType.Cube, "Ground", command, isStatic: true);
            // A broad flat slab whose top face sits at y=0. Shape size follows the transform
            // scale (like all Box3D shapes), so the visual and the physics stay in sync.
            go.transform.localScale = new Vector3(20f, 1f, 20f);
            go.transform.position += new Vector3(0f, -0.5f, 0f);
        }

        private static GameObject CreateShapePrimitive<TShape>(PrimitiveType primitive, string name,
            MenuCommand command, bool isStatic) where TShape : Box3DShape
        {
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // visual only — physics is Box3D's
            var body = go.AddComponent<Box3DBody>();
            if (isStatic) body.BodyType = Box3DBodyType.Static;
            go.AddComponent<TShape>();
            Place(go, command, "Create Box3D " + name);
            return go;
        }

        private static void Place(GameObject go, MenuCommand command, string undoName)
        {
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            if (!go.transform.parent) StageUtility.PlaceGameObjectInCurrentStage(go);
            GameObjectUtility.EnsureUniqueNameForSibling(go); // excludes self — plain GetUniqueNameForSibling would rename the first "Box" to "Box (1)"
            Undo.RegisterCreatedObjectUndo(go, undoName);
            Selection.activeGameObject = go;
        }
    }
}
