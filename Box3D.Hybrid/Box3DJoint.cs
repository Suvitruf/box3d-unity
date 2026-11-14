using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>Base class for joint components, analogous to Unity's joints. Goes on a GameObject
    /// that has a <see cref="Box3DBody"/> (the constrained body); connects it to another
    /// <see cref="Connected"/> body, or to the world if that is null. The joint is created after all
    /// bodies exist (Start), so both endpoints are ready; <see cref="Connected"/>,
    /// <see cref="LocalAnchor"/> and the per-joint axis setters <see cref="Rebuild"/> it afterwards.</summary>
    [RequireComponent(typeof(Box3DBody))]
    public abstract class Box3DJoint : MonoBehaviour
    {
        [SerializeField, Tooltip("Body this joint attaches to. Null = the world (a fixed point).")]
        private Box3DBody ConnectedBody;

        [SerializeField, Tooltip("Local anchor point on this body (the joint pivot).")]
        private Vector3 Anchor = Vector3.zero;

        [SerializeField, Tooltip("Let the two connected bodies collide with each other.")]
        private bool CollideConnected;

        private Joint _joint;
        private Box3DBody _self;
        private bool _started;

        /// <summary>The world owning this joint (valid after Start).</summary>
        protected Box3DWorld World { get; private set; }

        /// <summary>The joint pivot in world space.</summary>
        protected Vector3 WorldAnchor => transform.TransformPoint(Anchor);

        /// <summary>The underlying Box3D joint (valid between Start and OnDestroy). <see cref="Rebuild"/>
        /// replaces it, so re-read this after changing a baked property rather than caching it.</summary>
        public Joint Joint => _joint;

        /// <summary>The body this joint connects to; null = the world (a fixed point), like Unity's
        /// null connectedBody. Setting it on a live joint rebuilds the joint at the bodies' current
        /// poses — every assignment does, so set it when something changes, not every frame.</summary>
        public Box3DBody Connected
        {
            get => ConnectedBody;
            set
            {
                ConnectedBody = value;
                Rebuild();
            }
        }

        /// <summary>The joint pivot on this body, in local space. Setting it on a live joint rebuilds
        /// the joint (every assignment does).</summary>
        public Vector3 LocalAnchor
        {
            get => Anchor;
            set
            {
                Anchor = value;
                Rebuild();
            }
        }

        /// <summary>Whether the two connected bodies may collide with each other. Applies to a live
        /// joint immediately (turning it off also removes any contact between them).</summary>
        public bool CollidesWithConnected
        {
            get => CollideConnected;
            set
            {
                CollideConnected = value;
                if (_joint.IsValid) _joint.SetCollideConnected(value);
            }
        }

        /// <summary>Sets the connected body — see <see cref="Connected"/>.</summary>
        public void SetConnectedBody(Box3DBody body) => Connected = body;

        /// <summary>Sets the local anchor point — see <see cref="LocalAnchor"/>.</summary>
        public void SetAnchor(Vector3 localAnchor) => LocalAnchor = localAnchor;

        private void Start()
        {
            World = Box3DWorld.Instance;
            _self = GetComponent<Box3DBody>();
            _started = true;
            CreateNativeJoint();
        }

        /// <summary>Destroys the native joint and creates it again from the current fields, with the
        /// joint frames taken from the bodies' current poses (so nothing snaps). The setters of the
        /// baked properties — <see cref="Connected"/>, <see cref="LocalAnchor"/>, a joint's axis —
        /// call this for you; call it yourself after changing several of them at once, or to bring
        /// back a joint box3d removed when its connected body was destroyed. A no-op before Start,
        /// which creates the joint from the fields anyway.</summary>
        public void Rebuild()
        {
            if (!_started) return;
            if (_joint.IsValid) _joint.Destroy();
            CreateNativeJoint();
        }

        private void CreateNativeJoint()
        {
            _joint = default;
            if (!World || !_self || !_self.Body.IsValid) return; // world or body already torn down
            if (ConnectedBody && !ConnectedBody.Body.IsValid)
            {
                Debug.LogWarning($"[Box3D] {name}: connected body '{ConnectedBody.name}' has no live Box3D body " +
                                 "(inactive, or not yet awake) — the joint is not created. Call Rebuild() once it has one.", this);
                return;
            }

            BodyId bodyB = _self.Body.Id;
            BodyId bodyA = ConnectedBody ? ConnectedBody.Body.Id : World.WorldAnchor.Id;
            _joint = CreateJoint(bodyA, bodyB);

            // box3d sets the collide-connected flag at joint creation but does NOT clear a contact
            // that already exists between the two bodies. If a world step runs between the bodies
            // spawning and this joint being created, an overlap contact (e.g. a wheel inside the
            // chassis) gets created and then persists — crushing the bodies apart with huge force.
            // Toggling collide-connected forces box3d to destroy that stale contact.
            if (!CollideConnected && _joint.IsValid)
            {
                _joint.SetCollideConnected(true);
                _joint.SetCollideConnected(false);
            }
        }

        private void OnDestroy()
        {
            if (_joint.IsValid) _joint.Destroy();
        }

        /// <summary>Wakes both connected bodies. A motor/steer change has no effect on a sleeping
        /// body, so call this when driving a joint after it may have settled.</summary>
        public void WakeBodies()
        {
            if (_joint.IsValid) _joint.WakeBodies();
        }

        /// <summary>Creates the native joint between the two bodies (bodyA = connected/world,
        /// bodyB = this).</summary>
        protected abstract Joint CreateJoint(BodyId bodyA, BodyId bodyB);

        /// <summary>Fills the shared joint-def base. Frame B uses <paramref name="frameRotationB"/>
        /// (this body's local frame orientation); frame A is derived so the two frames coincide in
        /// world space at the current pose — so creating the joint doesn't snap the bodies.</summary>
        protected void SetupBase(ref JointDefBase baseDef, BodyId bodyA, BodyId bodyB, quaternion frameRotationB)
        {
            baseDef.BodyIdA = bodyA;
            baseDef.BodyIdB = bodyB;
            baseDef.CollideConnected = CollideConnected;

            Vector3 anchor = WorldAnchor;
            baseDef.LocalFrameB = new B3Transform
            {
                Position = ToBodyLocal(transform, anchor),
                Rotation = frameRotationB,
            };

            // World orientation of frame B, expressed back in each body's local space for frame A.
            quaternion frameWorld = math.mul((quaternion)transform.rotation, frameRotationB);
            baseDef.LocalFrameA = ConnectedBody
                ? new B3Transform
                {
                    Position = ToBodyLocal(ConnectedBody.transform, anchor),
                    Rotation = math.mul(math.inverse((quaternion)ConnectedBody.transform.rotation), frameWorld),
                }
                : new B3Transform { Position = anchor, Rotation = frameWorld }; // world anchor: identity body
        }

        /// <summary>Frame rotation mapping box3d's frame z-axis onto <paramref name="worldAxis"/>
        /// (hinge, spherical cone, parallel).</summary>
        protected quaternion LocalAxisFrame(Vector3 worldAxis) => AxisFrame(Vector3.forward, worldAxis);

        /// <summary>Frame rotation mapping box3d's frame x-axis onto <paramref name="worldAxis"/>
        /// (prismatic slide axis).</summary>
        protected quaternion LocalSlideFrame(Vector3 worldAxis) => AxisFrame(Vector3.right, worldAxis);

        private quaternion AxisFrame(Vector3 fromLocal, Vector3 worldAxis)
        {
            Vector3 localAxis = transform.InverseTransformDirection(worldAxis);
            return Quaternion.FromToRotation(fromLocal, localAxis.sqrMagnitude > 1e-8f ? localAxis.normalized : fromLocal);
        }

        /// <summary>Whether connected bodies are allowed to collide.</summary>
        protected bool Collides => CollideConnected;

        /// <summary>A position-only local frame at a world point, on a body's Transform.</summary>
        protected static B3Transform PointFrame(Transform body, Vector3 worldPoint)
        {
            return new B3Transform { Position = ToBodyLocal(body, worldPoint), Rotation = quaternion.identity };
        }

        /// <summary>Converts a world point to a body's local frame WITHOUT scale — box3d bodies are
        /// unscaled (scale is baked into the shape), so Transform.InverseTransformPoint (which divides
        /// by lossyScale) would put anchors in the wrong place on a scaled GameObject.</summary>
        protected static float3 ToBodyLocal(Transform body, Vector3 worldPoint)
        {
            return Box3DBody.ToBodyLocal(body, worldPoint);
        }
    }
}
