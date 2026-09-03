using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A fixed (weld) joint, analogous to Unity's FixedJoint: rigidly locks this body to
    /// its connected body (or the world) at their current relative pose. Optional spring softness.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DJoint.png")]
    [AddComponentMenu("Box3D/Joints/Fixed Joint")]
    public class Box3DFixedJoint : Box3DJoint
    {
        [SerializeField, Min(0f), Tooltip("Linear spring stiffness (Hz). 0 = perfectly rigid.")]
        private float LinearHertz;

        [SerializeField, Min(0f), Tooltip("Angular spring stiffness (Hz). 0 = perfectly rigid.")]
        private float AngularHertz;

        private WeldJoint _weld;

        protected override Joint CreateJoint(BodyId bodyA, BodyId bodyB)
        {
            WeldJointDef def = WeldJointDef.Default;
            // Weld holds the two frames coincident; identity frame B + derived frame A freeze the
            // bodies at their current relative pose.
            SetupBase(ref def.Base, bodyA, bodyB, quaternion.identity);
            def.LinearHertz = LinearHertz;
            def.AngularHertz = AngularHertz;

            _weld = IBox3DWorld.Get(this).PhysicsWorld.CreateWeldJoint(def);
            return _weld;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Push Inspector edits to the live joint during play. The welded pose is captured at
            // creation and stays fixed.
            if (!Application.isPlaying || !_weld.IsValid) return;
            _weld.SetLinearHertz(LinearHertz);
            _weld.SetAngularHertz(AngularHertz);
            WakeBodies();
        }
#endif
    }
}
