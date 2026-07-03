using System;
using Unity.Mathematics;

namespace Box3d
{
    // Owners of builder-allocated native geometry. Explicit lifetime: call Destroy() when done.
    //
    // THE LIFETIME RULE (the #1 footgun):
    // - Hull data is CLONED into the world's hull database at shape creation — a Hull may be
    //   destroyed right after creating shapes from it.
    // - TriangleMesh / HeightField / Compound data is REFERENCED by shapes — it must stay alive
    //   until every shape using it is destroyed.

    /// <summary>Owns native convex hull data (b3HullData*). Cloned at shape creation — safe to
    /// destroy after use. For plain boxes prefer the stack-based <see cref="BoxHull"/>.</summary>
    public struct Hull
    {
        internal IntPtr Data;

        public bool IsCreated => Data != IntPtr.Zero;

        /// <summary>Builds a convex hull enclosing the given points (at most maxVertices kept).</summary>
        public static unsafe Hull Create(ReadOnlySpan<float3> points, int maxVertices = 64)
        {
            fixed (float3* p = points)
            {
                return new Hull { Data = (IntPtr)UnsafeBindings.b3CreateHull(p, points.Length, maxVertices) };
            }
        }

        public static unsafe Hull CreateCylinder(float height, float radius, float yOffset = 0f, int sides = 16)
        {
            return new Hull { Data = (IntPtr)UnsafeBindings.b3CreateCylinder(height, radius, yOffset, sides) };
        }

        public static unsafe Hull CreateCone(float height, float bottomRadius, float topRadius = 0f, int slices = 16)
        {
            return new Hull { Data = (IntPtr)UnsafeBindings.b3CreateCone(height, bottomRadius, topRadius, slices) };
        }

        public static unsafe Hull CreateRock(float radius)
        {
            return new Hull { Data = (IntPtr)UnsafeBindings.b3CreateRock(radius) };
        }

        public unsafe void Destroy()
        {
            if (Data == IntPtr.Zero) return;
            UnsafeBindings.b3DestroyHull((HullData*)Data);
            Data = IntPtr.Zero;
        }
    }

    /// <summary>Owns native triangle mesh data (b3MeshData*). Static bodies only. REFERENCED by
    /// shapes — must outlive every shape created from it.</summary>
    public struct TriangleMesh
    {
        internal IntPtr Data;

        public bool IsCreated => Data != IntPtr.Zero;

        /// <summary>Builds a mesh from triangle soup. Indices are 3 per triangle.</summary>
        public static unsafe TriangleMesh Create(ReadOnlySpan<float3> vertices, ReadOnlySpan<int> triangles,
            bool identifyEdges = true, bool weldVertices = false, float weldTolerance = 0.005f)
        {
            fixed (float3* v = vertices)
            fixed (int* i = triangles)
            {
                var def = new b3MeshDef
                {
                    vertices = v,
                    indices = i,
                    vertexCount = vertices.Length,
                    triangleCount = triangles.Length / 3,
                    identifyEdges = identifyEdges,
                    weldVertices = weldVertices,
                    weldTolerance = weldTolerance,
                };
                return new TriangleMesh { Data = (IntPtr)UnsafeBindings.b3CreateMesh(&def, null, 0) };
            }
        }

        /// <summary>Engine-generated closed box mesh (extent = half sizes).</summary>
        public static unsafe TriangleMesh CreateBox(float3 center, float3 extent, bool identifyEdges = true)
        {
            return new TriangleMesh { Data = (IntPtr)UnsafeBindings.b3CreateBoxMesh(center, extent, identifyEdges) };
        }

        /// <summary>Engine-generated flat grid mesh in the XZ plane.</summary>
        public static unsafe TriangleMesh CreateGrid(int xCount, int zCount, float cellWidth, bool identifyEdges = true)
        {
            return new TriangleMesh { Data = (IntPtr)UnsafeBindings.b3CreateGridMesh(xCount, zCount, cellWidth, 1, identifyEdges) };
        }

        public unsafe void Destroy()
        {
            if (Data == IntPtr.Zero) return;
            UnsafeBindings.b3DestroyMesh((b3MeshData*)Data);
            Data = IntPtr.Zero;
        }
    }

    /// <summary>Owns native height field data (b3HeightFieldData*). Static bodies only.
    /// REFERENCED by shapes — must outlive every shape created from it.</summary>
    public struct HeightField
    {
        internal IntPtr Data;

        public bool IsCreated => Data != IntPtr.Zero;

        /// <summary>Builds a height field from row-major grid heights (countX × countZ values).
        /// Min/max heights define the quantization range; use the same values on adjacent fields
        /// that must line up.</summary>
        public static unsafe HeightField Create(ReadOnlySpan<float> heights, int countX, int countZ,
            float3 scale, float globalMinimumHeight, float globalMaximumHeight, bool clockwiseWinding = false)
        {
            fixed (float* h = heights)
            {
                var def = new b3HeightFieldDef
                {
                    heights = h,
                    scale = scale,
                    countX = countX,
                    countZ = countZ,
                    globalMinimumHeight = globalMinimumHeight,
                    globalMaximumHeight = globalMaximumHeight,
                    clockwiseWinding = clockwiseWinding,
                };
                return new HeightField { Data = (IntPtr)UnsafeBindings.b3CreateHeightField(&def) };
            }
        }

        /// <summary>Engine-generated flat grid height field.</summary>
        public static unsafe HeightField CreateGrid(int rowCount, int columnCount, float3 scale, bool makeHoles = false)
        {
            return new HeightField { Data = (IntPtr)UnsafeBindings.b3CreateGrid(rowCount, columnCount, scale, makeHoles) };
        }

        public unsafe void Destroy()
        {
            if (Data == IntPtr.Zero) return;
            UnsafeBindings.b3DestroyHeightField((b3HeightFieldData*)Data);
            Data = IntPtr.Zero;
        }
    }

    /// <summary>A sphere instance inside a compound. Layout mirrors native b3CompoundSphereDef.</summary>
    public struct CompoundSphereInstance
    {
        public Sphere Sphere;
        public SurfaceMaterial Material;
    }

    /// <summary>A capsule instance inside a compound. Layout mirrors native b3CompoundCapsuleDef.</summary>
    public struct CompoundCapsuleInstance
    {
        public Capsule Capsule;
        public SurfaceMaterial Material;
    }

    /// <summary>A hull instance inside a compound. Layout mirrors native b3CompoundHullDef.
    /// The referenced Hull must be created before and stay alive until the compound is created
    /// (the compound bakes its own copy of hull data).</summary>
    public struct CompoundHullInstance
    {
        public Hull Hull;
        public B3Transform Transform;
        public SurfaceMaterial Material;
    }

    /// <summary>Owns native compound data (b3CompoundData*): up to 64K spheres/capsules/hulls baked
    /// into one static shape. Static bodies only. REFERENCED by shapes — must outlive them.</summary>
    public struct Compound
    {
        internal IntPtr Data;

        public bool IsCreated => Data != IntPtr.Zero;

        public static unsafe Compound Create(ReadOnlySpan<CompoundSphereInstance> spheres,
            ReadOnlySpan<CompoundCapsuleInstance> capsules = default,
            ReadOnlySpan<CompoundHullInstance> hulls = default)
        {
            fixed (CompoundSphereInstance* s = spheres)
            fixed (CompoundCapsuleInstance* c = capsules)
            fixed (CompoundHullInstance* h = hulls)
            {
                var def = new b3CompoundDef
                {
                    spheres = (b3CompoundSphereDef*)s,
                    sphereCount = spheres.Length,
                    capsules = (b3CompoundCapsuleDef*)c,
                    capsuleCount = capsules.Length,
                    hulls = (b3CompoundHullDef*)h,
                    hullCount = hulls.Length,
                };
                return new Compound { Data = (IntPtr)UnsafeBindings.b3CreateCompound(&def) };
            }
        }

        public unsafe void Destroy()
        {
            if (Data == IntPtr.Zero) return;
            UnsafeBindings.b3DestroyCompound((b3CompoundData*)Data);
            Data = IntPtr.Zero;
        }
    }
}
