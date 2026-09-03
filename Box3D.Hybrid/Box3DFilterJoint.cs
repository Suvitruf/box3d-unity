using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A filter joint: applies no constraint, it just disables collision between this body
    /// and its connected body (they stay in the same simulation island). Handy for bodies that
    /// overlap by design.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DJoint.png")]
    [AddComponentMenu("Box3D/Joints/Filter Joint")]
    public class Box3DFilterJoint : Box3DJoint
    {
        protected override Joint CreateJoint(BodyId bodyA, BodyId bodyB)
        {
            FilterJointDef def = FilterJointDef.Default;
            def.Base.BodyIdA = bodyA;
            def.Base.BodyIdB = bodyB;
            return IBox3DWorld.Get(this).PhysicsWorld.CreateFilterJoint(def);
        }
    }
}
