// Box3D Water — screen-space surface composite. Drawn as a fullscreen triangle by a hidden
// MeshRenderer so it slots into the transparent queue of whatever camera is rendering. The
// smoothed impostor depth becomes a surface: view-space normals are reconstructed from depth
// gradients, the scene color is refracted through them, absorption tints by accumulated
// thickness, and a Fresnel mix adds probe reflections plus a main-light specular highlight.
//
// Deliberately include-free: every uniform is either set by this package per camera or is a
// global the render pipeline already provides (_CameraDepthTexture, _ZBufferParams, main light,
// ambient probe). With refraction available the pass overwrites the pixel (Blend One Zero);
// without an opaque texture the renderer switches the material to classic alpha blending.
Shader "Box3D/Water Surface"
{
    Properties
    {
        _TintColor("Tint", Color) = (0.09, 0.34, 0.42, 1)
        _AbsorptionScale("Absorption", Float) = 1.2
        _RefractionStrength("Refraction Strength", Float) = 0.12
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 0.9
        _ReflectionBlur("Reflection Blur", Range(0, 6)) = 1
        _SpecPower("Specular Power", Float) = 220
        _SpecIntensity("Specular Intensity", Float) = 0.8
        _SrcBlend("Src Blend", Float) = 1
        _DstBlend("Dst Blend", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ _BOX3D_WATER_REFRACTION

            Texture2D _Box3DWaterDepthTex;   SamplerState sampler_Box3DWaterDepthTex;
            float4 _Box3DWaterDepthTex_TexelSize;
            Texture2D _Box3DWaterThickTex;   SamplerState sampler_Box3DWaterThickTex;
            Texture2D _Box3DWaterFoamTex;    SamplerState sampler_Box3DWaterFoamTex;

            // Pipeline-provided globals.
            Texture2D _CameraDepthTexture;   SamplerState sampler_CameraDepthTexture;
            float4 _CameraDepthTexture_TexelSize;
            Texture2D _CameraOpaqueTexture;  SamplerState sampler_CameraOpaqueTexture;
            float4 _ZBufferParams;
            float3 _WorldSpaceCameraPos;
            float4 _MainLightPosition;
            float4 _MainLightColor;
            TextureCube unity_SpecCube0;     SamplerState samplerunity_SpecCube0;
            float4 unity_SpecCube0_HDR;

            // Set per camera by Box3DWaterRenderer.
            float4x4 _Box3DWaterCamToWorld;
            float4 _Box3DWaterProjExtents;   // (1/P00, 1/P11, unused, unused)

            float4 _TintColor;
            float _AbsorptionScale;
            float _RefractionStrength;
            float _ReflectionStrength;
            float _ReflectionBlur;
            float _SpecPower;
            float _SpecIntensity;
            float _FoamStrength;
            float _ShoreBlend;          // meters of soft fade where water meets geometry
            float _Box3DWaterTime;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), f.x),
                            lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), f.x), f.y);
            }

            struct Varyings
            {
                float4 pos : SV_POSITION;
            };

            Varyings Vert(uint vid : SV_VertexID)
            {
                // The mesh is a dummy; cover the target with one triangle regardless of geometry.
                Varyings o;
                o.pos = float4(vid == 1 ? 3.0 : -1.0, vid == 2 ? 3.0 : -1.0, 0.5, 1.0);
                return o;
            }

            float LinearEye(float raw)
            {
                return 1.0 / (_ZBufferParams.z * raw + _ZBufferParams.w);
            }

            float FluidDepth(float2 uv)
            {
                return _Box3DWaterDepthTex.SampleLevel(sampler_Box3DWaterDepthTex, uv, 0).r;
            }

            // View-space position of the fluid surface seen at uv.
            float3 ViewPos(float2 uv, float eyeDepth)
            {
                float2 ndc = uv * 2.0 - 1.0;
                return float3(ndc * _Box3DWaterProjExtents.xy * eyeDepth, -eyeDepth);
            }

            float4 Frag(Varyings i) : SV_Target
            {
                // Normalizing by the depth texture's size keeps this aligned with the pipeline's
                // actual render target even when a render-scale is active.
                float2 uv = i.pos.xy * _CameraDepthTexture_TexelSize.xy;

                float eyeDepth = FluidDepth(uv);
                clip(eyeDepth <= 0.0 ? -1 : 1);

                // Hidden behind scene geometry?
                float sceneRaw = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, uv, 0).r;
                float sceneEye = LinearEye(sceneRaw);
                clip(sceneEye - eyeDepth);

                // Reconstruct the view-space normal from depth gradients, picking the smaller
                // difference on each axis so silhouette edges don't produce skewed normals.
                float2 texel = _Box3DWaterDepthTex_TexelSize.xy;
                float3 p = ViewPos(uv, eyeDepth);

                float dR = FluidDepth(uv + float2(texel.x, 0));
                float dL = FluidDepth(uv - float2(texel.x, 0));
                float dU = FluidDepth(uv + float2(0, texel.y));
                float dD = FluidDepth(uv - float2(0, texel.y));

                float3 ddxPos = dR > 0.0 ? ViewPos(uv + float2(texel.x, 0), dR) - p : float3(0, 0, 0);
                float3 ddxNeg = dL > 0.0 ? p - ViewPos(uv - float2(texel.x, 0), dL) : ddxPos;
                if (dR <= 0.0) ddxPos = ddxNeg;
                float3 dpdx = abs(ddxPos.z) < abs(ddxNeg.z) ? ddxPos : ddxNeg;

                float3 ddyPos = dU > 0.0 ? ViewPos(uv + float2(0, texel.y), dU) - p : float3(0, 0, 0);
                float3 ddyNeg = dD > 0.0 ? p - ViewPos(uv - float2(0, texel.y), dD) : ddyPos;
                if (dU <= 0.0) ddyPos = ddyNeg;
                float3 dpdy = abs(ddyPos.z) < abs(ddyNeg.z) ? ddyPos : ddyNeg;

                float3 nView = cross(dpdx, dpdy);
                float nLen = length(nView);
                nView = nLen > 1e-6 ? nView / nLen : float3(0, 0, 1);
                if (dot(nView, -p) < 0.0) nView = -nView; // face the camera on either UV handedness

                float3 nWorld = normalize(mul((float3x3)_Box3DWaterCamToWorld, nView));
                float3 worldPos = mul(_Box3DWaterCamToWorld, float4(p, 1.0)).xyz;
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                float thickness = _Box3DWaterThickTex.SampleLevel(sampler_Box3DWaterThickTex, uv, 0).r;

                // Beer–Lambert: white light survives in the tint's proportions as it travels.
                float3 transmit = exp(-_AbsorptionScale * thickness * (1.0 - _TintColor.rgb));

                // Fresnel (water F0 ≈ 0.02).
                float fresnel = 0.02 + 0.98 * pow(1.0 - saturate(dot(nWorld, viewDir)), 5.0);

                float3 reflDir = reflect(-viewDir, nWorld);
                float4 probe = unity_SpecCube0.SampleLevel(samplerunity_SpecCube0, reflDir, _ReflectionBlur);
                float3 reflection = probe.rgb * unity_SpecCube0_HDR.x;

                float3 lightDir = normalize(_MainLightPosition.xyz);
                float3 halfDir = normalize(lightDir + viewDir);
                float3 specular = pow(saturate(dot(nWorld, halfDir)), _SpecPower)
                                  * _SpecIntensity * _MainLightColor.rgb;

                // How far behind the water surface the scene sits: 0 right at a waterline (an
                // object or shore piercing the surface), 1 in open water. Drives both the soft
                // merge into geometry and the foam rim that hugs the intersection.
                float shore = saturate((sceneEye - eyeDepth) / max(_ShoreBlend, 1e-3));

                // Whitewater: churned foam splatted by the particles + the waterline rim,
                // broken up by two octaves of drifting noise (coverage-threshold style).
                float churn = _Box3DWaterFoamTex.SampleLevel(sampler_Box3DWaterFoamTex, uv, 0).r;
                float cover = saturate(churn * 1.2 + (1.0 - shore) * 0.8) * _FoamStrength;
                float noise = 0.55 * ValueNoise(worldPos.xz * 6.0 + _Box3DWaterTime * 0.35)
                            + 0.45 * ValueNoise(worldPos.xz * 15.0 - _Box3DWaterTime * 0.6);
                float foam = smoothstep(1.0 - cover, 1.0 - cover + 0.3, noise) * saturate(cover * 2.0);
                float3 foamColor = _MainLightColor.rgb * 0.85 + 0.25;

                float3 color;
                float alpha;
            #if defined(_BOX3D_WATER_REFRACTION)
                // Refraction relaxes to zero at the waterline so edges don't wobble.
                float2 refrUv = uv + nView.xy * _RefractionStrength * saturate(thickness) * shore;
                float refrRaw = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, refrUv, 0).r;
                if (LinearEye(refrRaw) < eyeDepth) refrUv = uv; // don't refract foreground objects in
                float3 scene = _CameraOpaqueTexture.SampleLevel(sampler_CameraOpaqueTexture, refrUv, 0).rgb;
                color = lerp(scene * transmit, reflection, fresnel * _ReflectionStrength) + specular;
                color = lerp(scene, color, shore); // melt into whatever the water touches
                alpha = 1.0;
            #else
                // No opaque texture available: approximate transmission with alpha blending.
                color = lerp(_TintColor.rgb * _MainLightColor.rgb, reflection, fresnel * _ReflectionStrength)
                        + specular;
                alpha = saturate(1.0 - (transmit.r + transmit.g + transmit.b) / 3.0);
                alpha = max(alpha, fresnel * _ReflectionStrength);
                alpha *= shore; // fade out instead of a hard cut against geometry
            #endif

                color = lerp(color, foamColor, foam);
            #if !defined(_BOX3D_WATER_REFRACTION)
                alpha = max(alpha, foam * 0.9);
            #endif

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
