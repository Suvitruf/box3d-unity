using UnityEditor;

namespace Box3D.Hybrid.Editor
{
    [CustomEditor(typeof(Box3DBallJoint))]
    [CanEditMultipleObjects]
    public class Box3DBallJointEditor : Box3DJointEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBase();
            Field("Axis");
            ConditionalField("UseConeLimit", "ConeAngle");
            ConditionalField("UseTwistLimit", "MinTwist", "MaxTwist");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
