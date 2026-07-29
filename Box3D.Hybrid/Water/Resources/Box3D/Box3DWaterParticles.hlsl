// Shared vertex/fragment code for the Box3D water impostor passes.

StructuredBuffer<float4> _Box3DWaterPositions;  // xyz world pos, w alive flag
StructuredBuffer<float4> _Box3DWaterVelocities; // xyz velocity, w foam amount
float4x4 _Box3DWaterView;                       // camera worldToCameraMatrix
float4x4 _Box3DWaterProj;                       // GPU projection (render-into-texture convention)
float    _Box3DWaterRenderRadius;               // impostor radius (particle radius × render scale)
float    _Box3DWaterThicknessScale;
float    _Box3DWaterStretch;                    // spray elongation along motion (0 = always round)

struct Varyings
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;         // corner in [-1, 1], ellipse-local
    float3 viewCenter : TEXCOORD1;
    float foam : TEXCOORD2;
    float2 viewOffset : TEXCOORD3; // this corner's view-space offset from the center
};

static const float2 kCorners[6] =
{
    float2(-1, -1), float2(1, -1), float2(1, 1),
    float2(-1, -1), float2(1, 1), float2(-1, 1)
};

Varyings Vert(uint vid : SV_VertexID)
{
    uint index = vid / 6;
    float2 corner = kCorners[vid - index * 6];
    float4 particle = _Box3DWaterPositions[index];
    float4 velFoam = _Box3DWaterVelocities[index];

    Varyings o;
    o.uv = corner;
    o.viewCenter = mul(_Box3DWaterView, float4(particle.xyz, 1.0)).xyz;
    o.foam = velFoam.w;
    o.viewOffset = float2(0.0, 0.0);

    if (particle.w < 0.5)
    {
        o.pos = float4(0, 0, -2, 1); // dead slot: emit a degenerate off-screen quad
        return o;
    }

    // Whitewater should read as spray, not stacked balls: foamy/fast impostors stretch along
    // their screen-space motion and thin out crosswise (area-preserving), so churned water
    // breaks into streaks. Calm water keeps perfectly round impostors.
    float3 viewVel = mul((float3x3)_Box3DWaterView, velFoam.xyz);
    float flowLen = length(viewVel.xy);
    float spray = saturate(o.foam * 1.5 + 0.5 * smoothstep(2.5, 6.0, length(velFoam.xyz)));
    float elong = 1.0 + min(flowLen * 0.35, 2.0) * spray * _Box3DWaterStretch;
    float2 dir = flowLen > 1e-3 ? viewVel.xy / flowLen : float2(1, 0);
    float2 perp = float2(-dir.y, dir.x);

    o.viewOffset = (corner.x * dir * elong + corner.y * perp / sqrt(elong)) * _Box3DWaterRenderRadius;
    o.pos = mul(_Box3DWaterProj, float4(o.viewCenter + float3(o.viewOffset, 0.0), 1.0));
    return o;
}

// Sphere surface height above the billboard plane, in units of the impostor radius.
float SphereZ(float2 uv)
{
    float r2 = dot(uv, uv);
    clip(1.0 - r2);
    return sqrt(1.0 - r2);
}

float4 FragDepth(Varyings i, out float depth : SV_Depth) : SV_Target
{
    float nz = SphereZ(i.uv);

    // Camera looks down -Z in view space: the front of the sphere is the most positive z.
    float3 viewSurf = i.viewCenter + float3(i.viewOffset, nz * _Box3DWaterRenderRadius);
    float4 clipPos = mul(_Box3DWaterProj, float4(viewSurf, 1.0));
    depth = clipPos.z / clipPos.w;

    return float4(-viewSurf.z, 0, 0, 1); // positive eye depth
}

float4 FragThickness(Varyings i) : SV_Target
{
    // (1 - r²)² bump with the same disk-average as the sphere chord it replaces. The chord's
    // vertical slope at the rim stamps a hard-edged coin per particle into the additive
    // buffer — absorption then shows every ball. This profile fades to zero smoothly.
    float s = 1.0 - dot(i.uv, i.uv);
    clip(s);
    return float4(s * s * 4.0 * _Box3DWaterRenderRadius * _Box3DWaterThicknessScale, 0, 0, 1);
}

// Foam splat, z-tested against the impostor depth buffer (with a hair of bias toward the
// camera) so only whitewater on the visible surface accumulates — not foam deep inside.
float4 FragFoam(Varyings i, out float depth : SV_Depth) : SV_Target
{
    float s = 1.0 - dot(i.uv, i.uv);
    clip(s);

    float3 viewSurf = i.viewCenter + float3(i.viewOffset, sqrt(s) * _Box3DWaterRenderRadius);
    viewSurf.z += 0.02 * _Box3DWaterRenderRadius; // toward the camera: survive the LEqual test
    float4 clipPos = mul(_Box3DWaterProj, float4(viewSurf, 1.0));
    depth = clipPos.z / clipPos.w;

    // Soft center-weighted splat: dense piles accumulate gently instead of clipping to a disc.
    return float4(i.foam * s * s, 0, 0, 1);
}
