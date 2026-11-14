using UnityEditor;

namespace Box3D.Hybrid.Editor
{
    [CustomEditor(typeof(Box3DSliderJoint))]
    [CanEditMultipleObjects]
    public class Box3DSliderJointEditor : Box3DJointEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBase();
            Field("Axis");
            ConditionalField("UseLimits", "MinTranslation", "MaxTranslation");
            ConditionalField("UseMotor", "MotorSpeed", "MaxMotorForce");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
