using UnityEditor;
using UnityEngine;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Shared inspector for the mesh-sourced shapes: draws the default fields, then warns
    /// if the assigned mesh is missing or not Read/Write enabled (the common runtime-empty trap).
    /// In a multi-object selection each shape is checked on its own and named in its warning.</summary>
    public abstract class MeshSourceShapeEditor : UnityEditor.Editor
    {
        private readonly TargetSerializedObjects _shapes = new TargetSerializedObjects();

        private void OnDisable()
        {
            _shapes.Dispose();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            foreach (Object shape in targets)
            {
                _shapes.Update(shape);
                var mesh = _shapes.Property(shape, "Mesh").objectReferenceValue as Mesh;
                string who = targets.Length > 1 ? $"{shape.name}: " : "";
                if (!mesh)
                {
                    EditorGUILayout.HelpBox($"{who}Assign a mesh for this shape.", MessageType.Info);
                }
                else if (!mesh.isReadable)
                {
                    EditorGUILayout.HelpBox(
                        $"{who}'{mesh.name}' is not Read/Write enabled, so the shape will be empty at runtime. " +
                        "Enable Read/Write in the mesh's import settings.", MessageType.Warning);
                }
            }
        }
    }

    [CustomEditor(typeof(Box3DMeshShape))]
    [CanEditMultipleObjects]
    public class Box3DMeshShapeEditor : MeshSourceShapeEditor { }

    [CustomEditor(typeof(Box3DHullShape))]
    [CanEditMultipleObjects]
    public class Box3DHullShapeEditor : MeshSourceShapeEditor { }
}
