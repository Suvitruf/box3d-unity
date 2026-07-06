using Unity.Mathematics;
using UnityEngine;

namespace Box3d.Hybrid
{
    /// <summary>Base class for joint components, analogous to Unity's joints. Goes on a GameObject
    /// that has a <see cref="Box3dBody"/> (the constrained body); connects it to another
    /// <see cref="ConnectedBody"/>, or to the world if that is null. The joint is created after all
    /// bodies exist (Start), so both endpoints are ready.</summary>
    [RequireComponent(typeof(Box3dBody))]
    public abstract class Box3dJoint : MonoBehaviour
    {
        [SerializeField, Tooltip("Body this joint attaches to. Null = the world (a fixed point).")]
        private Box3dBody ConnectedBody;

        [SerializeField, Tooltip("Local anchor point on this body (the joint pivot).")]
        private Vector3 Anchor = Vector3.zero;

        [SerializeField, Tooltip("Let the two connected bodies collide with each other.")]
        private bool CollideConnected;

        private Joint _joint;

        /// <summary>The world owning this joint (valid after Start).</summary>
        protected Box3dWorld World { get; private set; }

        /// <summary>The joint pivot in world space.</summary>
        protected Vector3 WorldAnchor => transform.TransformPoint(Anchor);

        /// <summary>Sets the connected body (null = world). Must be set before the joint is created
        /// (Start).</summary>
        public void SetConnectedBody(Box3dBody body)
        {
            ConnectedBody = body;
        }

        /// <summary>Sets the local anchor point. Must be set before the joint is created (Start).</summary>
        public void SetAnchor(Vector3 localAnchor)
        {
            Anchor = localAnchor;
        }

        private void Start()
        {
            World = Box3dWorld.Instance;

            Box3dBody self = GetComponent<Box3dBody>();
            BodyId bodyB = self.Body.Id;
            BodyId bodyA = ConnectedBody ? ConnectedBody.Body.Id : World.WorldAnchor.Id;

            _joint = CreateJoint(bodyA, bodyB);
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

        /// <summary>The connected body (null = world), for joints that place two distinct anchors.</summary>
        protected Box3dBody Connected => ConnectedBody;

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
            return math.mul(math.inverse((quaternion)body.rotation), (float3)(worldPoint - body.position));
        }
    }
}
