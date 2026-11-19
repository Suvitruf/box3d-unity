using Unity.Mathematics;

namespace Box3D
{
    /// <summary>A ray hit in the shape's local coordinate system.</summary>
    public struct ShapeRayHit
    {
        public float3 Point;
        public float3 Normal;
        public float Fraction;
    }

    /// <summary>Queries against standalone local-space geometry.</summary>
    public static class ShapeQueries
    {
        /// <summary>Cast from origin along translation, up to maxFraction of its length.
        /// A ray starting inside the sphere hits at the origin with fraction zero and a zero normal.</summary>
        public static unsafe bool RayCast(Sphere sphere, float3 origin, float3 translation, out ShapeRayHit hit,
            float maxFraction = 1f)
        {
            var input = new b3RayCastInput { origin = origin, translation = translation, maxFraction = maxFraction };
            b3CastOutput result = UnsafeBindings.b3RayCastSphere(&sphere, &input);
            hit = new ShapeRayHit { Point = result.point, Normal = result.normal, Fraction = result.fraction };
            return result.hit;
        }
    }
}
