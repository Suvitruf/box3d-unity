using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A distance joint, analogous to Unity's SpringJoint: keeps a point on this body a
    /// fixed distance from a point on the connected body (or the world). Rigid by default; enable
    /// the spring for rope/bungee behavior.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DJoint.png")]
    [AddComponentMenu("Box3D/Joints/Distance Joint")]
    public class Box3DDistanceJoint : Box3DJoint
    {
        [SerializeField, Tooltip("Anchor point on the connected body (world space if no connected body).")]
        private Vector3 ConnectedAnchor = Vector3.zero;

        [SerializeField, Tooltip("Rest length. 0 = the distance between the anchors at start.")]
        private float Length;

        [SerializeField, Tooltip("Behave like a spring instead of a rigid rod.")]
        private bool UseSpring;

        [SerializeField, Min(0f), Tooltip("Spring stiffness in Hertz.")]
        private float Hertz = 2f;

        [SerializeField, Min(0f), Tooltip("Spring damping ratio.")]
        private float DampingRatio = 0.5f;

        private DistanceJoint _distance;

        /// <summary>Sets the connected anchor (world space if no connected body). Before Start.</summary>
        public void SetConnectedAnchor(Vector3 anchor)
        {
            ConnectedAnchor = anchor;
        }

        /// <summary>Sets the rest length (0 = auto). Before Start.</summary>
        public void SetLength(float length)
        {
            Length = length;
        }

        /// <summary>Enables spring behavior with the given stiffness/damping. Before Start.</summary>
        public void SetSpring(float hertz, float dampingRatio)
        {
            UseSpring = true;
            Hertz = hertz;
            DampingRatio = dampingRatio;
        }

        protected override Joint CreateJoint(BodyId bodyA, BodyId bodyB)
        {
            Vector3 anchorB = WorldAnchor;
            Vector3 anchorA = Connected ? Connected.transform.TransformPoint(ConnectedAnchor) : ConnectedAnchor;

            DistanceJointDef def = DistanceJointDef.Default;
            def.Base.BodyIdA = bodyA;
            def.Base.BodyIdB = bodyB;
            def.Base.CollideConnected = Collides;
            def.Base.LocalFrameB = PointFrame(transform, anchorB);
            def.Base.LocalFrameA = Connected
                ? PointFrame(Connected.transform, anchorA)
                : new B3Transform { Position = anchorA, Rotation = quaternion.identity };

            def.Length = Length > 0f ? Length : Vector3.Distance(anchorA, anchorB);
            if (UseSpring)
            {
                def.EnableSpring = true;
                def.Hertz = Hertz;
                def.DampingRatio = DampingRatio;
            }

            _distance = IBox3DWorld.Get(this).PhysicsWorld.CreateDistanceJoint(def);
            return _distance;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Push Inspector edits to the live joint during play. Anchors and Connected Body are
            // baked into the joint frames at creation and stay fixed; Length 0 keeps the rest
            // length measured at creation.
            if (!Application.isPlaying || !_distance.IsValid) return;
            if (Length > 0f) _distance.SetLength(Length);
            _distance.EnableSpring(UseSpring);
            _distance.SetSpringHertz(Hertz);
            _distance.SetSpringDampingRatio(DampingRatio);
            WakeBodies();
        }
#endif

        private void OnDrawGizmosSelected()
        {
            Vector3 anchorB = WorldAnchor;
            Vector3 anchorA = Connected ? Connected.transform.TransformPoint(ConnectedAnchor) : ConnectedAnchor;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
            Gizmos.DrawLine(anchorA, anchorB);
            Gizmos.DrawWireSphere(anchorA, 0.06f);
            Gizmos.DrawWireSphere(anchorB, 0.06f);
        }
    }
}
