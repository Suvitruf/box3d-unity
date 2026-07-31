#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>Editor-only rope preview: simulates the real capsule-chain rope in a throwaway
    /// native world where every enabled scene shape is frozen as static collision — so the Scene
    /// view (and Bake) shows the true drape over geometry, matching what play mode will do. The
    /// chain construction mirrors <see cref="Box3DRope"/>.BuildDynamic — keep them in sync.</summary>
    internal sealed class Box3DRopePreview : IDisposable
    {
        private readonly Box3DRope _rope;
        private readonly List<Box3DShape> _replicated = new List<Box3DShape>();
        private readonly Vector3[] _nodes;
        private World _world;
        private Body[] _segments;
        private Body _startPin;
        private Body _endPin;
        private float _halfSegment;

        internal Box3DRopePreview(Box3DRope rope)
        {
            _rope = rope;
            _nodes = new Vector3[rope.SegmentCount + 1];

            WorldDef worldDef = WorldDef.Default;
            worldDef.Gravity = rope.SceneGravity();
            _world = World.Create(worldDef);

            ReplicateScene();
            BuildChain();
        }

        /// <summary>Node positions of the previewed rope, start to end.</summary>
        internal Vector3[] Nodes
        {
            get
            {
                for (int i = 0; i < _segments.Length; i++)
                {
                    _nodes[i] = Tip(_segments[i], -_halfSegment);
                }
                _nodes[_segments.Length] = Tip(_segments[_segments.Length - 1], _halfSegment);
                return _nodes;
            }
        }

        /// <summary>Advances the preview; the end pins follow the (possibly dragged) endpoints.</summary>
        internal void Step(float deltaTime)
        {
            _startPin.SetTransform(_rope.StartWorld, Quaternion.identity);
            _endPin.SetTransform(_rope.EndWorld, Quaternion.identity);
            _world.Step(deltaTime, 4);
        }

        /// <summary>Runs the preview to rest and returns the draped nodes.</summary>
        internal Vector3[] Settle(int steps = 300)
        {
            for (int i = 0; i < steps; i++)
            {
                Step(1f / 60f);
            }
            return Nodes;
        }

        public void Dispose()
        {
            if (_world.IsValid) _world.Destroy();
            foreach (Box3DShape shape in _replicated)
            {
                if (shape) shape.ReleaseDetachedGeometry();
            }
            _replicated.Clear();
        }

        // Every enabled scene shape becomes static collision at its current pose — the preview
        // treats the scene as frozen. Shapes on the rope's attached bodies are left out (the
        // runtime filters those collisions with filter joints) unless collision was requested.
        private void ReplicateScene()
        {
            Box3DBody startAttach = _rope.FindStartAttachment();
            Box3DBody endAttach = _rope.FindEndAttachment();

            foreach (Box3DShape shape in UnityEngine.Object.FindObjectsByType<Box3DShape>(FindObjectsSortMode.None))
            {
                if (!shape.isActiveAndEnabled) continue;
                if (!_rope.CollidesWithAttached && (Under(shape, startAttach) || Under(shape, endAttach))) continue;

                BodyDef def = BodyDef.Default; // static
                def.Position = shape.transform.position;
                def.Rotation = shape.transform.rotation;
                Body body = _world.CreateBody(def);
                shape.CreateDetachedShape(body);
                _replicated.Add(shape);
            }
        }

        private static bool Under(Box3DShape shape, Box3DBody body)
        {
            return body && shape.transform.IsChildOf(body.transform);
        }

        private void BuildChain()
        {
            int segments = _rope.SegmentCount;
            Vector3 start = _rope.StartWorld;
            Vector3 end = _rope.EndWorld;
            _halfSegment = _rope.SettledSegmentLength() * 0.5f;

            _segments = new Body[segments];
            for (int i = 0; i < segments; i++)
            {
                float3 a = Vector3.Lerp(start, end, (float)i / segments);
                float3 b = Vector3.Lerp(start, end, (float)(i + 1) / segments);
                float3 dir = math.normalizesafe(b - a, new float3(0f, 0f, 1f));

                BodyDef bodyDef = BodyDef.Default;
                bodyDef.Type = Box3D.BodyType.Dynamic;
                bodyDef.Position = (a + (float3)b) * 0.5f;
                bodyDef.Rotation = quaternion.LookRotationSafe(dir, new float3(0f, 1f, 0f));
                bodyDef.LinearDamping = 0.1f;
                bodyDef.AngularDamping = 0.5f;
                bodyDef.IsBullet = true;
                _segments[i] = _world.CreateBody(bodyDef);

                float cap = Mathf.Max(0.001f, _halfSegment - _rope.RopeRadius);
                _segments[i].CreateCapsuleShape(_rope.SegmentShapeDef(), new Capsule
                {
                    Center1 = new float3(0f, 0f, -cap),
                    Center2 = new float3(0f, 0f, cap),
                    Radius = _rope.RopeRadius,
                });
            }

            for (int i = 1; i < segments; i++)
            {
                Link(_segments[i - 1], new float3(0f, 0f, _halfSegment),
                     _segments[i], new float3(0f, 0f, -_halfSegment));
            }

            // Both ends pin to static bodies — attached scene bodies are frozen here anyway.
            _startPin = Pin(start, _segments[0], -_halfSegment);
            _endPin = Pin(end, _segments[segments - 1], _halfSegment);
        }

        private void Link(Body a, float3 localA, Body b, float3 localB)
        {
            SphericalJointDef def = SphericalJointDef.Default;
            def.Base.BodyIdA = a.Id;
            def.Base.BodyIdB = b.Id;
            def.Base.LocalFrameA = new B3Transform { Position = localA, Rotation = quaternion.identity };
            def.Base.LocalFrameB = new B3Transform { Position = localB, Rotation = quaternion.identity };
            _world.CreateSphericalJoint(def);
        }

        private Body Pin(Vector3 at, Body segment, float alongZ)
        {
            BodyDef def = BodyDef.Default; // static
            def.Position = at;
            Body pin = _world.CreateBody(def);
            Link(pin, float3.zero, segment, new float3(0f, 0f, alongZ));
            return pin;
        }

        private static Vector3 Tip(Body body, float alongZ)
        {
            // One GetTransform instead of separate Position + Rotation natives (runs per segment per tick).
            B3Transform tf = body.GetTransform().ToB3Transform();
            return (Vector3)(tf.Position + math.mul(tf.Rotation, new float3(0f, 0f, alongZ)));
        }
    }
}
#endif
