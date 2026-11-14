using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Box3D
{
    internal static class MeshValidation
    {
        internal static bool HasUsableTriangles(ReadOnlySpan<float3> vertices, ReadOnlySpan<int> triangles,
            bool weldVertices, float tolerance)
        {
            if (weldVertices && !math.isfinite(tolerance))
                throw new ArgumentOutOfRangeException(nameof(tolerance));
            int[] representatives = weldVertices && tolerance > 0f ? FindRepresentatives(vertices, tolerance) : null;
            float lengthUnits = UnsafeBindings.b3GetLengthUnitsPerMeter();
            float slop = 0.005f * lengthUnits;
            float minArea = 0.01f * slop * slop;
#if BOX3D_DOUBLE
            float huge = 1e9f * lengthUnits;
#else
            float huge = 1e5f * lengthUnits;
#endif
            float3 lower = new float3(float.MaxValue);
            float3 upper = new float3(-float.MaxValue);
            bool hasTriangle = false;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                float3 p = vertices[representatives == null ? a : representatives[a]];
                float3 q = vertices[representatives == null ? b : representatives[b]];
                float3 r = vertices[representatives == null ? c : representatives[c]];
                float area = 0.5f * math.length(math.cross(q - p, r - p));
                if (area < minArea) continue;
                hasTriangle = true;
                lower = math.min(lower, math.min(p, math.min(q, r)));
                upper = math.max(upper, math.max(p, math.max(q, r)));
            }
            return hasTriangle && math.all(lower >= -huge) && math.all(upper <= huge);
        }

        // Match Box3D 0.1.0's spatial hash, neighbor order and newest-first chains exactly:
        // welding can collapse otherwise valid triangles. This only validates the input;
        // native Box3D still owns welding, mesh construction and collision.
        private static int[] FindRepresentatives(ReadOnlySpan<float3> vertices, float tolerance)
        {
            var heads = new Dictionary<ulong, int>();
            var next = new int[vertices.Length];
            var representatives = new int[vertices.Length];
            float cellSize = 2f * tolerance;
            if (!math.isfinite(cellSize)) throw new ArgumentOutOfRangeException(nameof(tolerance));
            for (int i = 0; i < vertices.Length; i++)
            {
                float3 cell = math.floor(vertices[i] / cellSize);
                // Leave room for neighboring coordinates before the native float-to-int cast.
                if (!math.all(math.abs(cell) < 2147483520f))
                    throw new ArgumentOutOfRangeException(nameof(tolerance), "weld grid coordinates exceed the native integer range");
                int3 origin = (int3)cell;
                int duplicate = -1;
                for (int x = -1; x <= 1 && duplicate < 0; x++)
                {
                    for (int y = -1; y <= 1 && duplicate < 0; y++)
                    {
                        for (int z = -1; z <= 1 && duplicate < 0; z++)
                        {
                            if (!heads.TryGetValue(Key(origin + new int3(x, y, z)), out int node)) continue;
                            while (node >= 0)
                            {
                                if (math.all(math.abs(vertices[i] - vertices[node]) <= tolerance))
                                {
                                    duplicate = node;
                                    break;
                                }
                                node = next[node];
                            }
                        }
                    }
                }
                if (duplicate >= 0)
                {
                    representatives[i] = representatives[duplicate];
                    continue;
                }
                representatives[i] = i;
                ulong key = Key(origin);
                next[i] = heads.TryGetValue(key, out int head) ? head : -1;
                heads[key] = i;
            }
            return representatives;
        }

        private static ulong Key(int3 cell)
        {
            unchecked
            {
                ulong key = 0;
                key ^= (ulong)cell.x + 0x9e3779b9UL + (key << 6) + (key >> 2);
                key ^= (ulong)cell.y + 0x9e3779b9UL + (key << 6) + (key >> 2);
                key ^= (ulong)cell.z + 0x9e3779b9UL + (key << 6) + (key >> 2);
                return key;
            }
        }
    }
}
