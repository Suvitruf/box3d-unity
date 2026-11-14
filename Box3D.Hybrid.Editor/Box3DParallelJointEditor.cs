using UnityEditor;

namespace Box3D.Hybrid.Editor
{
    [CustomEditor(typeof(Box3DParallelJoint))]
    [CanEditMultipleObjects]
    public class Box3DParallelJointEditor : Box3DJointEditor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); // no conditional fields; inherits the anchor handle
        }
    }
}
