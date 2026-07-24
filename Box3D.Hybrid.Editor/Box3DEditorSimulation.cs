using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Edit-mode physics session behind the physics-simulation Scene tool. The chosen
    /// dynamic bodies are rebuilt in a throwaway native world — same shape gathering, damping and
    /// mass as play mode — while every other enabled scene shape is frozen as static collision
    /// (the rope preview's recipe), so what settles here is what play mode would produce. Also
    /// owns the mouse grab: a kinematic anchor tied to the grabbed body by a ball joint, with the
    /// anchor smoothed toward the cursor so dragging feels springy instead of violent.</summary>
    internal sealed class Box3DEditorSimulation : IDisposable
    {
        private const int SubSteps = 4;
        // Time constant of the anchor's chase toward the cursor — lower is snappier.
        private const float GrabSmoothTime = 0.06f;
        // Spin damping while held, so a grabbed body hangs from the cursor instead of pinwheeling.
        private const float GrabAngularDamping = 4f;

        private sealed class Entry
        {
            internal Box3DBody Component;
            internal Body Body;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<Box3DShape> _replicated = new List<Box3DShape>();
        private World _world;

        private Entry _grabbed;
        private Body _grabAnchor;
        private Joint _grabJoint;
        private float3 _grabLocalPoint;
        private float3 _grabTarget;
        private float _grabDistance;
        private float _restoreAngularDamping;

        /// <summary>Bodies actually simulating (selected dynamic bodies that had shapes).</summary>
        internal int BodyCount => _entries.Count;

        internal bool IsGrabbing => _grabbed != null;

        /// <summary>Current world-space point on the grabbed body (valid while grabbing).</summary>
        internal Vector3 GrabPoint => (Vector3)_grabbed.Body.GetWorldPoint(_grabLocalPoint);

        /// <summary>Where the cursor is pulling the grabbed body toward (valid while grabbing).</summary>
        internal Vector3 GrabTarget => (Vector3)_grabTarget;

        /// <summary>Builds the preview world. <paramref name="bodies"/> must be ordered parents
        /// before children so the write-back leaves nested bodies at their simulated world pose.</summary>
        internal Box3DEditorSimulation(IReadOnlyList<Box3DBody> bodies)
        {
            // Play mode installs this automatically; edit mode must ask — engine logs/asserts
            // reach the console, and a missing native library reports with build instructions.
            Box3DRuntime.Install();

            WorldDef worldDef = WorldDef.Default;
            worldDef.Gravity = SceneGravity();
            _world = World.Create(worldDef);

            var simulated = new HashSet<Box3DBody>(bodies);
            foreach (Box3DBody body in bodies)
            {
                CreateDynamic(body);
            }
            ReplicateStaticScene(simulated);
        }

        /// <summary>Advances the simulation one fixed step, pulling the grabbed body first.</summary>
        internal void Step(float deltaTime)
        {
            if (_grabbed != null)
            {
                // Chase the cursor with a critically-damped lag; the kinematic anchor then drags
                // the body through the ball joint at solver strength — soft feel, no explosions.
                float chase = 1f - Mathf.Exp(-deltaTime / GrabSmoothTime);
                float3 next = math.lerp(_grabAnchor.Position, _grabTarget, chase);
                _grabAnchor.SetTargetTransform(
                    new B3Transform { Position = next, Rotation = quaternion.identity },
                    deltaTime, wake: true);
                _grabbed.Body.SetAwake(true);
            }
            _world.Step(deltaTime, SubSteps);
        }

        /// <summary>Writes simulated poses back to the scene Transforms (entries are ordered
        /// parents first, so children land on their absolute simulated pose).</summary>
        internal void WriteBack()
        {
            foreach (Entry entry in _entries)
            {
                if (!entry.Component) continue; // deleted mid-simulation
                entry.Component.transform.SetPositionAndRotation(entry.Body.Position, entry.Body.Rotation);
            }
        }

        /// <summary>Tries to pick a simulating body with a cursor ray. Frozen scene geometry
        /// blocks the pick (no grabbing through walls). Returns true when a grab started.</summary>
        internal bool TryGrab(Ray ray, float maxDistance = 500f)
        {
            Release();

            Span<RayHit> hits = stackalloc RayHit[32];
            int count = _world.CastRay(ray.origin, (float3)ray.direction * maxDistance, QueryFilter.Default, hits);
            if (count == 0) return false;

            RayHit closest = hits[0];
            for (int i = 1; i < count; i++)
            {
                if (hits[i].Fraction < closest.Fraction) closest = hits[i];
            }

            BodyId hitBody = new Shape { Id = closest.ShapeId }.GetBody();
            Entry entry = null;
            foreach (Entry candidate in _entries)
            {
                if (candidate.Body.Id.Equals(hitBody)) { entry = candidate; break; }
            }
            if (entry == null) return false; // closest hit is frozen scenery

            _grabbed = entry;
            _grabDistance = closest.Fraction * maxDistance;
            _grabTarget = closest.Point;
            _grabLocalPoint = entry.Body.GetLocalPoint(closest.Point);
            _restoreAngularDamping = entry.Body.GetAngularDamping();
            entry.Body.SetAngularDamping(Mathf.Max(_restoreAngularDamping, GrabAngularDamping));

            BodyDef anchorDef = BodyDef.Default;
            anchorDef.Type = Box3D.BodyType.Kinematic;
            anchorDef.Position = closest.Point;
            _grabAnchor = _world.CreateBody(anchorDef);

            SphericalJointDef jointDef = SphericalJointDef.Default;
            jointDef.Base.BodyIdA = _grabAnchor.Id;
            jointDef.Base.BodyIdB = entry.Body.Id;
            jointDef.Base.LocalFrameA = new B3Transform { Position = float3.zero, Rotation = quaternion.identity };
            jointDef.Base.LocalFrameB = new B3Transform { Position = _grabLocalPoint, Rotation = quaternion.identity };
            _grabJoint = _world.CreateSphericalJoint(jointDef);
            return true;
        }

        /// <summary>Moves the grab target along the cursor ray, keeping the original pick depth —
        /// the body drags on a camera-facing plane through the grab point.</summary>
        internal void MoveGrab(Ray ray)
        {
            if (_grabbed == null) return;
            _grabTarget = (float3)(ray.origin + ray.direction * _grabDistance);
        }

        /// <summary>Lets go of the grabbed body, keeping its momentum (release mid-drag = throw).</summary>
        internal void Release()
        {
            if (_grabbed == null) return;
            if (_grabJoint.IsValid) _grabJoint.Destroy();
            if (_grabAnchor.IsValid) _grabAnchor.Destroy();
            _grabbed.Body.SetAngularDamping(_restoreAngularDamping);
            _grabbed = null;
        }

        public void Dispose()
        {
            Release();
            if (_world.IsValid) _world.Destroy();
            foreach (Box3DShape shape in _replicated)
            {
                if (shape) shape.ReleaseDetachedGeometry();
            }
            _replicated.Clear();
            _entries.Clear();
        }

        // Mirrors Box3DBody.Awake: a dynamic body at the component's pose, compound shapes gathered
        // from the hierarchy (stopping at nested bodies) and attached at their local frames.
        private void CreateDynamic(Box3DBody component)
        {
            Transform root = component.transform;

            BodyDef def = BodyDef.Default;
            def.Type = Box3D.BodyType.Dynamic;
            def.Position = root.position;
            def.Rotation = root.rotation;
            def.LinearDamping = component.LinearDampingValue;
            def.AngularDamping = component.AngularDampingValue;
            Body body = _world.CreateBody(def);

            var shapes = new List<Box3DShape>();
            Box3DBody.GatherShapes(root, shapes, isRoot: true);

            quaternion bodyInverse = math.inverse((quaternion)root.rotation);
            int attached = 0;
            foreach (Box3DShape shape in shapes)
            {
                Transform shapeTransform = shape.transform;
                float3 localPosition = root.InverseTransformPoint(shapeTransform.position);
                quaternion localRotation = math.mul(bodyInverse, (quaternion)shapeTransform.rotation);
                shape.CreateDetachedShape(body, localPosition, localRotation);
                _replicated.Add(shape);
                attached++;
            }

            if (attached == 0)
            {
                body.Destroy(); // no shapes — nothing to simulate, leave the object untouched
                return;
            }
            _entries.Add(new Entry { Component = component, Body = body });
        }

        // Every enabled scene shape that doesn't belong to a simulated body becomes static
        // collision at its current pose — the rest of the scene is frozen, exactly like play mode
        // would collide with it if nothing else moved.
        private void ReplicateStaticScene(HashSet<Box3DBody> simulated)
        {
            foreach (Box3DShape shape in UnityEngine.Object.FindObjectsByType<Box3DShape>(FindObjectsSortMode.None))
            {
                if (!shape.isActiveAndEnabled) continue;
                Box3DBody owner = shape.GetComponentInParent<Box3DBody>();
                if (owner && simulated.Contains(owner)) continue; // already part of a dynamic preview body

                BodyDef def = BodyDef.Default; // static
                def.Position = shape.transform.position;
                def.Rotation = shape.transform.rotation;
                Body body = _world.CreateBody(def);
                shape.CreateDetachedShape(body);
                _replicated.Add(shape);
            }
        }

        private static Vector3 SceneGravity()
        {
            var world = UnityEngine.Object.FindAnyObjectByType<Box3DWorld>();
            return world ? world.GravityVector : new Vector3(0f, -9.81f, 0f);
        }
    }
}
