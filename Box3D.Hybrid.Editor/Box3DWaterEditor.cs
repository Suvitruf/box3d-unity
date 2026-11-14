using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Rendering;

namespace Box3D.Hybrid.Editor
{
    /// <summary>Water inspector: the normal fields plus environment checks (compute support, an
    /// SRP with depth/opaque textures), a live particle readout and Fill / Splash / Clear buttons
    /// for testing in play mode. Scene view shows resizable handles for the fill volume (cyan)
    /// and the simulation bounds (blue). Multi-object editing: fields and buttons apply to every
    /// selected water, the particle count is the total, and each handle resizes its own water
    /// (OnSceneGUI runs once per selected object, on that object's SerializedObject).</summary>
    [CustomEditor(typeof(Box3DWater))]
    [CanEditMultipleObjects]
    public class Box3DWaterEditor : UnityEditor.Editor
    {
        private readonly BoxBoundsHandle _volumeHandle = new BoxBoundsHandle();
        private readonly BoxBoundsHandle _boundsHandle = new BoxBoundsHandle();
        private readonly TargetSerializedObjects _waters = new TargetSerializedObjects();

        // Pipeline-flag results cached with a 1 s refresh: reading them is reflection
        // (GetProperty + GetValue), and the inspector runs two passes per repaint.
        private double _nextPipelineCheck;
        private bool _depthTextureOff;
        private bool _opaqueTextureOff;

        private void OnDisable()
        {
            _waters.Dispose();
        }

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
                if (EditorApplication.timeSinceStartup >= _nextPipelineCheck)
                {
                    _nextPipelineCheck = EditorApplication.timeSinceStartup + 1.0;
                    _depthTextureOff = IsPipelineFlagOff("supportsCameraDepthTexture");
                    _opaqueTextureOff = IsPipelineFlagOff("supportsCameraOpaqueTexture");
                }
                if (_depthTextureOff)
                {
                    EditorGUILayout.HelpBox("Depth Texture is disabled in the render pipeline asset — the water surface needs it to sit correctly in the scene.", MessageType.Warning);
                }
                if (_opaqueTextureOff)
                {
                    EditorGUILayout.HelpBox("Opaque Texture is disabled in the render pipeline asset — the water will fall back to plain transparency instead of refracting the scene.", MessageType.Warning);
                }
            }

            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (Application.isPlaying)
            {
                int particles = 0;
                foreach (Object t in targets) particles += ((Box3DWater)t).ActiveParticleRange;
                EditorGUILayout.LabelField("Particles", particles.ToString("N0"));
            }
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Application.isPlaying ? "Fill" : "Fill (enter Play mode)"))
                    {
                        foreach (Object t in targets) ((Box3DWater)t).Fill();
                    }
                    if (GUILayout.Button("Splash"))
                    {
                        foreach (Object t in targets)
                        {
                            var water = (Box3DWater)t;
                            water.SpawnParticles(water.transform.position + Vector3.up * 2f, 0.5f,
                                Vector3.down * 4f, 512);
                        }
                    }
                    if (GUILayout.Button("Clear"))
                    {
                        foreach (Object t in targets) ((Box3DWater)t).Clear();
                    }
                }
            }
        }

        private static bool IsPipelineFlagOff(string property)
        {
            var prop = GraphicsSettings.currentRenderPipeline.GetType().GetProperty(property);
            return prop != null && prop.PropertyType == typeof(bool) &&
                   !(bool)prop.GetValue(GraphicsSettings.currentRenderPipeline);
        }

        private void OnSceneGUI()
        {
            var water = (Box3DWater)target;
            SerializedObject waterObject = _waters.Update(target);
            SerializedProperty volume = _waters.Property(target, "VolumeSize");
            SerializedProperty bounds = _waters.Property(target, "BoundsSize");

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
                    waterObject.ApplyModifiedProperties();
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
                waterObject.ApplyModifiedProperties();
            }
        }
    }
}
