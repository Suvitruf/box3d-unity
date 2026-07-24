// Shared vertex/fragment code for the Box3D water impostor passes.

StructuredBuffer<float4> _Box3DWaterPositions;  // xyz world pos, w alive flag
StructuredBuffer<float4> _Box3DWaterVelocities; // xyz velocity, w foam amount
float4x4 _Box3DWaterView;                       // camera worldToCameraMatrix
float4x4 _Box3DWaterProj;                       // GPU projection (render-into-texture convention)
float    _Box3DWaterRenderRadius;               // impostor radius (particle radius × render scale)
float    _Box3DWaterThicknessScale;

struct Varyings
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;      // corner in [-1, 1]
    float3 viewCenter : TEXCOORD1;
    float foam : TEXCOORD2;
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

    Varyings o;
    o.uv = corner;
    o.viewCenter = mul(_Box3DWaterView, float4(particle.xyz, 1.0)).xyz;
    o.foam = _Box3DWaterVelocities[index].w;

    if (particle.w < 0.5)
    {
        o.pos = float4(0, 0, -2, 1); // dead slot: emit a degenerate off-screen quad
        return o;
    }

    float3 viewPos = o.viewCenter + float3(corner * _Box3DWaterRenderRadius, 0.0);
    o.pos = mul(_Box3DWaterProj, float4(viewPos, 1.0));
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
    float3 viewSurf = i.viewCenter + float3(i.uv * _Box3DWaterRenderRadius, nz * _Box3DWaterRenderRadius);
    float4 clipPos = mul(_Box3DWaterProj, float4(viewSurf, 1.0));
    depth = clipPos.z / clipPos.w;

    return float4(-viewSurf.z, 0, 0, 1); // positive eye depth
}

float4 FragThickness(Varyings i) : SV_Target
{
    float nz = SphereZ(i.uv);
    return float4(nz * 2.0 * _Box3DWaterRenderRadius * _Box3DWaterThicknessScale, 0, 0, 1);
}

// Foam splat, z-tested against the impostor depth buffer (with a hair of bias toward the
// camera) so only whitewater on the visible surface accumulates — not foam deep inside.
float4 FragFoam(Varyings i, out float depth : SV_Depth) : SV_Target
{
    float nz = SphereZ(i.uv);

    float3 viewSurf = i.viewCenter + float3(i.uv * _Box3DWaterRenderRadius, nz * _Box3DWaterRenderRadius);
    viewSurf.z += 0.02 * _Box3DWaterRenderRadius; // toward the camera: survive the LEqual test
    float4 clipPos = mul(_Box3DWaterProj, float4(viewSurf, 1.0));
    depth = clipPos.z / clipPos.w;

    return float4(i.foam * nz * nz, 0, 0, 1); // center-weighted so splats stay round
}
