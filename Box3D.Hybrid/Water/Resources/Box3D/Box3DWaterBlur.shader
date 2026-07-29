// Box3D Water — separable smoothing of the impostor buffers, as fullscreen triangles generated
// from SV_VertexID (no camera matrices, no blit quad). Pass 0 is a bilateral blur for the depth
// buffer: the depth-difference weight melts sphere depths into a continuous surface while
// keeping silhouettes crisp; 0 means "no fluid" and never bleeds into the result. Pass 1 is a
// plain Gaussian for the additive fields (thickness, foam): they drive absorption and foam
// coverage directly, and unsmoothed they stamp every impostor as a visible coin.
Shader "Hidden/Box3D/WaterBlur"
{
    SubShader
    {
        HLSLINCLUDE
        Texture2D _Box3DWaterBlurSrc;
        SamplerState sampler_Box3DWaterBlurSrc;
        float4 _Box3DWaterBlurSrc_TexelSize; // set explicitly by the renderer
        float2 _Box3DWaterBlurDir;           // (1,0) then (0,1)
        float  _Box3DWaterBlurDepthScale;    // pixel radius at 1 m eye depth
        float  _Box3DWaterBlurFalloff;       // depth-difference tolerance in meters

        struct Varyings
        {
            float4 pos : SV_POSITION;
        };

        Varyings Vert(uint vid : SV_VertexID)
        {
            Varyings o;
            o.pos = float4(vid == 1 ? 3.0 : -1.0, vid == 2 ? 3.0 : -1.0, 0.5, 1.0);
            return o;
        }
        ENDHLSL

        Pass // 0: bilateral, for the depth buffer
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings i) : SV_Target
            {
                // Pixel-position addressing: destination pixel (x, y) reads source texel (x, y),
                // which is orientation-proof on every graphics API (src and dst are same-size RTs).
                float2 uv = i.pos.xy * _Box3DWaterBlurSrc_TexelSize.xy;
                float center = _Box3DWaterBlurSrc.SampleLevel(sampler_Box3DWaterBlurSrc, uv, 0).r;
                if (center <= 0.0) return 0.0;

                // Constant world-size footprint: nearby fluid gets a wide blur, distant fluid a
                // narrow one, so smoothing doesn't melt far-away detail.
                float radius = clamp(_Box3DWaterBlurDepthScale / center, 1.0, 16.0);
                float sigma = radius * 0.5;
                float invSpatial = 1.0 / (2.0 * sigma * sigma);
                float invRange = 1.0 / max(_Box3DWaterBlurFalloff, 1e-4);

                float sum = center;
                float weightSum = 1.0;

                [loop] for (float t = 1.0; t <= 16.0; t += 1.0)
                {
                    if (t > radius) break;
                    float spatial = exp(-t * t * invSpatial);

                    [unroll] for (int s = -1; s <= 1; s += 2)
                    {
                        float2 tapUv = uv + _Box3DWaterBlurDir * _Box3DWaterBlurSrc_TexelSize.xy * t * s;
                        float d = _Box3DWaterBlurSrc.SampleLevel(sampler_Box3DWaterBlurSrc, tapUv, 0).r;
                        if (d <= 0.0) continue;
                        float rangeDiff = (d - center) * invRange;
                        float w = spatial * exp(-rangeDiff * rangeDiff);
                        sum += d * w;
                        weightSum += w;
                    }
                }

                return float4(sum / weightSum, 0, 0, 1);
            }
            ENDHLSL
        }

        Pass // 1: plain Gaussian, for the additive fields (thickness, foam)
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            // Smoothed impostor eye depth: supplies the constant world-size footprint (the field
            // itself isn't a distance) and masks pixels with no fluid at all.
            Texture2D _Box3DWaterBlurDepth;

            float4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.pos.xy * _Box3DWaterBlurSrc_TexelSize.xy;
                float center = _Box3DWaterBlurSrc.SampleLevel(sampler_Box3DWaterBlurSrc, uv, 0).r;

                float eyeDepth = _Box3DWaterBlurDepth.SampleLevel(sampler_Box3DWaterBlurSrc, uv, 0).r;
                if (eyeDepth <= 0.0) return float4(center, 0, 0, 1); // no fluid: never composited

                float radius = clamp(_Box3DWaterBlurDepthScale / eyeDepth, 1.0, 16.0);
                float sigma = radius * 0.5;
                float invSpatial = 1.0 / (2.0 * sigma * sigma);

                float sum = center;
                float weightSum = 1.0;

                [loop] for (float t = 1.0; t <= 16.0; t += 1.0)
                {
                    if (t > radius) break;
                    float w = exp(-t * t * invSpatial);

                    [unroll] for (int s = -1; s <= 1; s += 2)
                    {
                        float2 tapUv = uv + _Box3DWaterBlurDir * _Box3DWaterBlurSrc_TexelSize.xy * t * s;
                        // Zero taps count: the fields legitimately fade to nothing off the fluid.
                        sum += _Box3DWaterBlurSrc.SampleLevel(sampler_Box3DWaterBlurSrc, tapUv, 0).r * w;
                        weightSum += w;
                    }
                }

                return float4(sum / weightSum, 0, 0, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
