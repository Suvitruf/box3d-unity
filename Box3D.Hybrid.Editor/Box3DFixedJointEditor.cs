using UnityEditor;

namespace Box3D.Hybrid.Editor
{
    [CustomEditor(typeof(Box3DFixedJoint))]
    [CanEditMultipleObjects]
    public class Box3DFixedJointEditor : Box3DJointEditor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); // no conditional fields; inherits the anchor handle
        }
    }
}
