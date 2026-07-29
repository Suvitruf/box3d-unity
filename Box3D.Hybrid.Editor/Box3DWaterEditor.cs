using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Rendering;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Water inspector: the normal fields plus environment checks (compute support, an
    /// SRP with depth/opaque textures), a live particle readout and Fill / Splash / Clear buttons
    /// for testing in play mode. Scene view shows resizable handles for the fill volume (cyan)
    /// and the simulation bounds (blue).</summary>
    [CustomEditor(typeof(Box3DWater))]
    public class Box3DWaterEditor : UnityEditor.Editor
    {
        private readonly BoxBoundsHandle _volumeHandle = new BoxBoundsHandle();
        private readonly BoxBoundsHandle _boundsHandle = new BoxBoundsHandle();

        public override void OnInspectorGUI()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                EditorGUILayout.HelpBox("This device has no compute-shader support — the water simulation cannot run here.", MessageType.Error);
            }
            else if (GraphicsSettings.currentRenderPipeline == null)
            {
                EditorGUILayout.HelpBox("No Scriptable Render Pipeline is active. The simulation will run, but the built-in water surface needs URP — with the built-in pipeline, bind ParticleBuffer to a custom renderer instead.", MessageType.Warning);
            }
            else
            {
                WarnIfPipelineFlagOff("supportsCameraDepthTexture",
                    "Depth Texture is disabled in the render pipeline asset — the water surface needs it to sit correctly in the scene.");
                WarnIfPipelineFlagOff("supportsCameraOpaqueTexture",
                    "Opaque Texture is disabled in the render pipeline asset — the water will fall back to plain transparency instead of refracting the scene.");
            }

            DrawDefaultInspector();

            var water = (Box3DWater)target;
            EditorGUILayout.Space();
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Particles", water.ActiveParticleRange.ToString("N0"));
            }
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Application.isPlaying ? "Fill" : "Fill (enter Play mode)")) water.Fill();
                    if (GUILayout.Button("Splash"))
                    {
                        water.SpawnParticles(water.transform.position + Vector3.up * 2f, 0.5f,
                            Vector3.down * 4f, 512);
                    }
                    if (GUILayout.Button("Clear")) water.Clear();
                }
            }
        }

        private static void WarnIfPipelineFlagOff(string property, string message)
        {
            var prop = GraphicsSettings.currentRenderPipeline.GetType().GetProperty(property);
            if (prop != null && prop.PropertyType == typeof(bool) &&
                !(bool)prop.GetValue(GraphicsSettings.currentRenderPipeline))
            {
                EditorGUILayout.HelpBox(message, MessageType.Warning);
            }
        }

        private void OnSceneGUI()
        {
            var water = (Box3DWater)target;
            serializedObject.Update();
            SerializedProperty volume = serializedObject.FindProperty("VolumeSize");
            SerializedProperty bounds = serializedObject.FindProperty("BoundsSize");

            // Fill volume: local, rotates with the object.
            _volumeHandle.center = Vector3.zero;
            _volumeHandle.size = volume.vector3Value;
            _volumeHandle.SetColor(new Color(0.35f, 0.75f, 0.95f));
            using (new Handles.DrawingScope(water.transform.localToWorldMatrix))
            {
                EditorGUI.BeginChangeCheck();
                _volumeHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    volume.vector3Value = _volumeHandle.size;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            // Simulation bounds: world-axis-aligned around the object's position.
            _boundsHandle.center = water.transform.position;
            _boundsHandle.size = bounds.vector3Value;
            _boundsHandle.SetColor(new Color(0.35f, 0.55f, 0.95f));
            EditorGUI.BeginChangeCheck();
            _boundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                bounds.vector3Value = _boundsHandle.size;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
