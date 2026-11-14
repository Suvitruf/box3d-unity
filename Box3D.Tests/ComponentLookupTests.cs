using System;
using System.Collections.Generic;
using System.Reflection;
using Box3D.Hybrid;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Box3D.Tests
{
    /// <summary>Component-layer lookups from native ids and runtime joint changes (public issue #8).
    /// Component lifecycle does not run in edit mode (SendMessage to such a component even trips
    /// Unity's ShouldRunBehaviour assert), so every spawned component is switched to
    /// <see cref="MonoBehaviour.runInEditMode"/> before its GameObject activates — Unity then runs
    /// Awake/OnEnable/OnDestroy itself, in play-mode order. Start waits for an editor tick, so the
    /// joint tests send it explicitly.</summary>
    public class ComponentLookupTests
    {
        // Box3DWorld.OnDestroy clears this; it never runs here because the world component itself
        // is not running in edit mode, so TearDown resets it by hand.
        private static readonly FieldInfo InstanceField =
            typeof(Box3DWorld).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly List<GameObject> _objects = new List<GameObject>();
        private Box3DWorld _world;
        private bool _ownsWorld;

        [SetUp]
        public void SetUp()
        {
            Assert.IsNotNull(InstanceField, "Box3DWorld._instance was renamed — update this test.");
            // In the editor the open scene may already hold a Box3DWorld; borrow it, never destroy it.
            _ownsWorld = !Object.FindAnyObjectByType<Box3DWorld>();
            _world = Box3DWorld.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) Despawn(go);
            _objects.Clear();
            if (!_world) return;
            World native = _world.World;
            if (native.IsValid) native.Destroy();
            if (_ownsWorld) Object.DestroyImmediate(_world.gameObject);
            // A borrowed scene world keeps its (now dead) native handle; dropping the static instance
            // makes the next Box3DWorld.Instance go through EnsureCreated again.
            InstanceField.SetValue(null, null);
        }

        private GameObject Spawn(string name, Vector3 position, params Type[] components)
        {
            var go = new GameObject(name);
            go.SetActive(false); // add everything first, then activate once: Awake, then OnEnable
            go.transform.position = position;
            foreach (Type component in components) go.AddComponent(component);
            _objects.Add(go);
            Activate(go);
            var body = go.GetComponent<Box3DBody>();
            if (body) Assert.IsTrue(body.Body.IsValid, $"{name}: Awake did not run when the object activated");
            return go;
        }

        /// <summary>Lets the components run in edit mode and activates the object, which makes Unity
        /// call Awake and OnEnable now — Box3DBody.Awake creates the body and attaches the shapes.</summary>
        private static void Activate(GameObject go)
        {
            foreach (MonoBehaviour behaviour in go.GetComponents<MonoBehaviour>()) behaviour.runInEditMode = true;
            go.SetActive(true);
        }

        private static void Despawn(GameObject go)
        {
            if (go) Object.DestroyImmediate(go); // running components get OnDisable/OnDestroy as in play mode
        }

        [Test]
        public void ShapeIdMapsBackToShapeAndBody()
        {
            GameObject go = Spawn("crate", Vector3.zero, typeof(Box3DBody), typeof(Box3DBoxShape));
            var body = go.GetComponent<Box3DBody>();
            var shape = go.GetComponent<Box3DBoxShape>();
            Assert.IsTrue(shape.Shape.IsValid);
            Assert.AreSame(body, shape.AttachedBody);

            Assert.IsTrue(Box3DWorld.TryGetShape(shape.Shape.Id, out Box3DShape foundShape));
            Assert.AreSame(shape, foundShape);
            Assert.IsTrue(Box3DWorld.TryGetBody(shape.Shape.Id, out Box3DBody foundBody));
            Assert.AreSame(body, foundBody);

            // The use case from the issue: a ray cast hit back to its components.
            _world.World.CastRayClosest(new B3Pos(-5f, 0f, 0f), new float3(10f, 0f, 0f), QueryFilter.Default, out RayResult hit);
            Assert.IsTrue(hit.Hit);
            Assert.IsTrue(Box3DWorld.TryGetShape(hit.ShapeId, out Box3DShape hitShape));
            Assert.AreSame(shape, hitShape);
            Assert.IsTrue(Box3DWorld.TryGetBody(hit.ShapeId, out Box3DBody hitBody));
            Assert.AreSame(body, hitBody);
        }

        [Test]
        public void OrphanShapeResolvesToItselfButHasNoBody()
        {
            var go = new GameObject("wall");
            go.SetActive(false);
            go.AddComponent<Box3DBoxShape>();
            _objects.Add(go);
            var autoAdded = go.GetComponent<Box3DBody>(); // the editor Reset adds one; this test wants none
            if (autoAdded) Object.DestroyImmediate(autoAdded);
            Activate(go); // Box3DShape.OnEnable creates the private static body
            var shape = go.GetComponent<Box3DBoxShape>();
            Assert.IsTrue(shape.Shape.IsValid);
            Assert.IsNull(shape.AttachedBody);

            Assert.IsTrue(Box3DWorld.TryGetShape(shape.Shape.Id, out Box3DShape found));
            Assert.AreSame(shape, found);
            Assert.IsFalse(Box3DWorld.TryGetBody(shape.Shape.Id, out _));
        }

        [Test]
        public void ReleasedShapeIsNoLongerFound()
        {
            GameObject go = Spawn("crate", Vector3.zero, typeof(Box3DBody), typeof(Box3DBoxShape));
            ShapeId id = go.GetComponent<Box3DBoxShape>().Shape.Id;
            _objects.Remove(go);
            Despawn(go);

            Assert.IsFalse(Box3DWorld.TryGetShape(id, out _));
            Assert.IsFalse(Box3DWorld.TryGetBody(id, out _));
        }

        [Test]
        public void SettingConnectedRebuildsTheLiveJoint()
        {
            GameObject anchorGo = Spawn("anchor", new Vector3(0f, 2f, 0f), typeof(Box3DBody));
            var anchor = anchorGo.GetComponent<Box3DBody>();
            GameObject doorGo = Spawn("door", Vector3.zero, typeof(Box3DBody), typeof(Box3DHingeJoint));
            doorGo.SendMessage("Start"); // Box3DJoint.Start creates the native joint
            var hinge = doorGo.GetComponent<Box3DHingeJoint>();

            Joint first = hinge.Joint;
            Assert.IsTrue(first.IsValid);
            Assert.IsNull(hinge.Connected);
            Assert.AreEqual(_world.WorldAnchor, first.BodyA);

            hinge.Connected = anchor;

            Assert.IsFalse(first.IsValid, "the previous native joint must be destroyed");
            Assert.IsTrue(hinge.Joint.IsValid);
            Assert.AreEqual(anchor.Body, hinge.Joint.BodyA);
            Assert.AreSame(anchor, hinge.Connected);
        }

        [Test]
        public void RebuildRestoresAJointWhoseConnectedBodyWasDestroyed()
        {
            GameObject anchorGo = Spawn("anchor", new Vector3(0f, 2f, 0f), typeof(Box3DBody));
            GameObject doorGo = Spawn("door", Vector3.zero, typeof(Box3DBody), typeof(Box3DHingeJoint));
            var hinge = doorGo.GetComponent<Box3DHingeJoint>();
            hinge.SetConnectedBody(anchorGo.GetComponent<Box3DBody>()); // before Start: stored, no joint yet
            Assert.IsFalse(hinge.Joint.IsValid);
            doorGo.SendMessage("Start");
            Assert.IsTrue(hinge.Joint.IsValid);

            _objects.Remove(anchorGo);
            Despawn(anchorGo);
            Assert.IsFalse(hinge.Joint.IsValid, "box3d destroys a joint together with its body");

            hinge.Connected = null; // back to the world anchor
            Assert.IsTrue(hinge.Joint.IsValid);
            Assert.AreEqual(_world.WorldAnchor, hinge.Joint.BodyA);
        }
    }
}
