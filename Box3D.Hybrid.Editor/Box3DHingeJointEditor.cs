using UnityEditor;

namespace Box3D.Hybrid.Editor
{
    [CustomEditor(typeof(Box3DHingeJoint))]
    [CanEditMultipleObjects]
    public class Box3DHingeJointEditor : Box3DJointEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawBase();
            Field("Axis");
            ConditionalField("UseLimits", "MinAngle", "MaxAngle");
            ConditionalField("UseMotor", "MotorSpeed", "MaxMotorTorque");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
