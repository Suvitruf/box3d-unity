// Box3D Water — particle impostor passes, drawn into offscreen targets by Box3DWaterRenderer.
// Pass 0 writes per-pixel eye depth of camera-facing sphere impostors (R32F + hardware z-test),
// pass 1 additively accumulates view-ray thickness (R16F). Both pull particle positions straight
// from the simulation buffer; no mesh, no matrices beyond the explicitly provided view/proj.
Shader "Hidden/Box3D/WaterParticles"
{
    SubShader
    {
        Pass // 0: depth
        {
            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask R

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FragDepth
            #include "Box3DWaterParticles.hlsl"
            ENDHLSL
        }

        Pass // 1: thickness
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend One One
            ColorMask R

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FragThickness
            #include "Box3DWaterParticles.hlsl"
            ENDHLSL
        }
    }
    Fallback Off
}
