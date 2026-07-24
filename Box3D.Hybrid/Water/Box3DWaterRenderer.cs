using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Box3D.Hybrid
{
    /// <summary>Screen-space liquid rendering for <see cref="Box3DWater"/>. Before each camera
    /// renders, the particle buffer is splatted into per-camera depth and thickness targets and
    /// the depth is smoothed with a bilateral blur (all outside the pipeline's own pass list, so
    /// no render-pipeline hooks are needed). A hidden fullscreen MeshRenderer child then shades
    /// the surface in the transparent queue: normals from smoothed depth, refraction of the
    /// opaque scene, thickness-based absorption, probe reflections and a specular highlight.</summary>
    internal sealed class Box3DWaterRenderer
    {
        private sealed class CameraTargets
        {
            public RenderTexture Depth;
            public RenderTexture Thickness;
            public RenderTexture Foam;
            public RenderTexture BlurTmp;
        }

        private readonly Box3DWater _water;
        private readonly CommandBuffer _cmd = new CommandBuffer { name = "Box3D Water" };
        private readonly Dictionary<Camera, CameraTargets> _targets = new Dictionary<Camera, CameraTargets>();
        private readonly List<Camera> _deadCameras = new List<Camera>();

        private Material _particleMat;
        private Material _blurMat;
        private Material _surfaceMat;
        private GameObject _surfaceGo;
        private MeshRenderer _surfaceRenderer;
        private Mesh _triangleMesh;
        private bool _refractionAvailable;

        public Box3DWaterRenderer(Box3DWater water) => _water = water;

        public void Enable()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Debug.LogWarning("Box3DWater: surface rendering needs a Scriptable Render Pipeline (URP); no pipeline asset is active. The simulation still runs — bind ParticleBuffer to your own renderer.", _water);
                return;
            }

            Shader particleShader = Resources.Load<Shader>("Box3D/Box3DWaterParticles");
            Shader blurShader = Resources.Load<Shader>("Box3D/Box3DWaterBlur");
            Shader surfaceShader = Resources.Load<Shader>("Box3D/Box3DWaterSurface");
            if (!particleShader || !blurShader || !surfaceShader)
            {
                Debug.LogWarning("Box3DWater: water shaders failed to load from package resources; surface rendering is disabled.", _water);
                return;
            }

            _particleMat = new Material(particleShader) { hideFlags = HideFlags.HideAndDontSave };
            _blurMat = new Material(blurShader) { hideFlags = HideFlags.HideAndDontSave };
            _surfaceMat = new Material(surfaceShader) { hideFlags = HideFlags.HideAndDontSave };
            _surfaceMat.renderQueue = 2990; // after opaques, before the bulk of the transparent queue

            // Refraction needs the pipeline's opaque texture; probe for the URP asset flag without
            // referencing the URP assembly.
            _refractionAvailable = true;
            var prop = GraphicsSettings.currentRenderPipeline.GetType().GetProperty("supportsCameraOpaqueTexture");
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                _refractionAvailable = (bool)prop.GetValue(GraphicsSettings.currentRenderPipeline);
            }

            _triangleMesh = new Mesh { name = "Box3D Water Fullscreen" };
            _triangleMesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            _triangleMesh.triangles = new[] { 0, 1, 2 };
            _triangleMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e5f); // never culled
            _triangleMesh.hideFlags = HideFlags.HideAndDontSave;

            _surfaceGo = new GameObject("Box3D Water Surface")
            {
                hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy,
                layer = _water.gameObject.layer,
            };
            _surfaceGo.transform.SetParent(_water.transform, worldPositionStays: false);
            _surfaceGo.AddComponent<MeshFilter>().sharedMesh = _triangleMesh;
            _surfaceRenderer = _surfaceGo.AddComponent<MeshRenderer>();
            _surfaceRenderer.sharedMaterial = _surfaceMat;
            _surfaceRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _surfaceRenderer.receiveShadows = false;
            _surfaceRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _surfaceRenderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes; // feeds unity_SpecCube0
            _surfaceRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        }

        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            foreach (CameraTargets t in _targets.Values) ReleaseTargets(t);
            _targets.Clear();
            _cmd.Release();
            if (_surfaceGo) Object.Destroy(_surfaceGo);
            if (_triangleMesh) Object.Destroy(_triangleMesh);
            if (_particleMat) Object.Destroy(_particleMat);
            if (_blurMat) Object.Destroy(_blurMat);
            if (_surfaceMat) Object.Destroy(_surfaceMat);
        }

        private static void ReleaseTargets(CameraTargets t)
        {
            if (t.Depth) t.Depth.Release();
            if (t.Thickness) t.Thickness.Release();
            if (t.Foam) t.Foam.Release();
            if (t.BlurTmp) t.BlurTmp.Release();
        }

        private void OnBeginCamera(ScriptableRenderContext context, Camera camera)
        {
            bool wanted = camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView;
            int range = _water ? _water.ActiveParticleRange : 0;
            if (_surfaceRenderer) _surfaceRenderer.forceRenderingOff = !wanted || range == 0;
            if (!wanted || range == 0 || _water.ParticleBuffer == null) return;

            PruneDeadCameras();
            CameraTargets targets = GetTargets(camera);

            Matrix4x4 view = camera.worldToCameraMatrix;
            Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);

            _cmd.Clear();
            _cmd.SetGlobalBuffer("_Box3DWaterPositions", _water.ParticleBuffer);
            _cmd.SetGlobalBuffer("_Box3DWaterVelocities", _water.VelocityBuffer);
            _cmd.SetGlobalMatrix("_Box3DWaterView", view);
            _cmd.SetGlobalMatrix("_Box3DWaterProj", gpuProj);
            _cmd.SetGlobalFloat("_Box3DWaterRenderRadius", _water.RenderRadius);
            _cmd.SetGlobalFloat("_Box3DWaterThicknessScale", 1f);

            // Sphere-impostor eye depth, hardware z-tested against itself.
            _cmd.SetRenderTarget(targets.Depth);
            _cmd.ClearRenderTarget(true, true, Color.clear);
            _cmd.DrawProcedural(Matrix4x4.identity, _particleMat, 0, MeshTopology.Triangles, range * 6);

            // Additive view-ray thickness.
            _cmd.SetRenderTarget(targets.Thickness);
            _cmd.ClearRenderTarget(false, true, Color.clear);
            _cmd.DrawProcedural(Matrix4x4.identity, _particleMat, 1, MeshTopology.Triangles, range * 6);

            // Whitewater splats, z-tested against the impostor depth so only surface foam shows.
            _cmd.SetRenderTarget(targets.Foam, targets.Depth);
            _cmd.ClearRenderTarget(false, true, Color.clear);
            if (_water.SurfaceFoam > 0f)
            {
                _cmd.DrawProcedural(Matrix4x4.identity, _particleMat, 2, MeshTopology.Triangles, range * 6);
            }

            // Bilateral smoothing: melt sphere depths into one surface without crossing silhouettes.
            if (_water.SurfaceSmoothing > 0f)
            {
                float projScaleY = camera.projectionMatrix[1, 1];
                float pixelsPerMeter = projScaleY * targets.Depth.height * 0.5f;
                _cmd.SetGlobalFloat("_Box3DWaterBlurDepthScale",
                    _water.SmoothingWorldRadius * _water.SurfaceSmoothing * pixelsPerMeter);
                _cmd.SetGlobalFloat("_Box3DWaterBlurFalloff", _water.SmoothingWorldRadius * 2f);
                var texel = new Vector4(1f / targets.Depth.width, 1f / targets.Depth.height,
                    targets.Depth.width, targets.Depth.height);

                for (int i = 0; i < 2; i++)
                {
                    BlurPass(targets.Depth, targets.BlurTmp, new Vector2(1f, 0f), texel);
                    BlurPass(targets.BlurTmp, targets.Depth, new Vector2(0f, 1f), texel);
                }
            }

            Graphics.ExecuteCommandBuffer(_cmd);

            // Per-camera surface inputs; cameras render one after another, so material state set
            // here is what this camera's transparent pass records.
            Matrix4x4 proj = camera.projectionMatrix;
            _surfaceMat.SetTexture("_Box3DWaterDepthTex", targets.Depth);
            _surfaceMat.SetTexture("_Box3DWaterThickTex", targets.Thickness);
            _surfaceMat.SetTexture("_Box3DWaterFoamTex", targets.Foam);
            _surfaceMat.SetMatrix("_Box3DWaterCamToWorld", camera.cameraToWorldMatrix);
            _surfaceMat.SetVector("_Box3DWaterProjExtents", new Vector4(1f / proj[0, 0], 1f / proj[1, 1], 0f, 0f));
            PushSurfaceLook();
        }

        private void BlurPass(RenderTexture src, RenderTexture dst, Vector2 dir, Vector4 texel)
        {
            _cmd.SetGlobalTexture("_Box3DWaterBlurSrc", src);
            _cmd.SetGlobalVector("_Box3DWaterBlurSrc_TexelSize", texel);
            _cmd.SetGlobalVector("_Box3DWaterBlurDir", dir);
            _cmd.SetRenderTarget(dst);
            _cmd.DrawProcedural(Matrix4x4.identity, _blurMat, 0, MeshTopology.Triangles, 3);
        }

        private void PushSurfaceLook()
        {
            _surfaceMat.SetColor("_TintColor", _water.SurfaceColor);
            _surfaceMat.SetFloat("_FoamStrength", _water.SurfaceFoam);
            _surfaceMat.SetFloat("_ShoreBlend", _water.SurfaceShoreBlend);
            _surfaceMat.SetFloat("_Box3DWaterTime", Time.time);
            _surfaceMat.SetFloat("_AbsorptionScale", _water.SurfaceAbsorption);
            _surfaceMat.SetFloat("_RefractionStrength", _water.SurfaceRefraction * 0.15f);
            _surfaceMat.SetFloat("_ReflectionStrength", _water.SurfaceReflection);

            bool refract = _refractionAvailable && _water.SurfaceRefraction > 0f;
            if (refract)
            {
                _surfaceMat.EnableKeyword("_BOX3D_WATER_REFRACTION");
                _surfaceMat.SetFloat("_SrcBlend", (float)BlendMode.One);
                _surfaceMat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            }
            else
            {
                _surfaceMat.DisableKeyword("_BOX3D_WATER_REFRACTION");
                _surfaceMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                _surfaceMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }
        }

        private CameraTargets GetTargets(Camera camera)
        {
            int width = Mathf.Max(1, (int)(camera.pixelWidth * _water.RenderResolutionScale));
            int height = Mathf.Max(1, (int)(camera.pixelHeight * _water.RenderResolutionScale));

            if (!_targets.TryGetValue(camera, out CameraTargets t))
            {
                t = new CameraTargets();
                _targets.Add(camera, t);
            }

            if (t.Depth == null || t.Depth.width != width || t.Depth.height != height)
            {
                ReleaseTargets(t);
                t.Depth = new RenderTexture(width, height, 24, RenderTextureFormat.RFloat)
                {
                    name = "Box3DWaterDepth", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
                };
                t.Thickness = new RenderTexture(width, height, 0, RenderTextureFormat.RHalf)
                {
                    name = "Box3DWaterThickness", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
                };
                t.Foam = new RenderTexture(width, height, 0, RenderTextureFormat.RHalf)
                {
                    name = "Box3DWaterFoam", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
                };
                t.BlurTmp = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat)
                {
                    name = "Box3DWaterBlur", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
                };
            }

            return t;
        }

        private void PruneDeadCameras()
        {
            foreach (KeyValuePair<Camera, CameraTargets> pair in _targets)
            {
                if (!pair.Key) _deadCameras.Add(pair.Key);
            }
            foreach (Camera dead in _deadCameras)
            {
                ReleaseTargets(_targets[dead]);
                _targets.Remove(dead);
            }
            _deadCameras.Clear();
        }
    }
}
