using System;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Box3D
{
    // Owners of builder-allocated native geometry. Explicit lifetime: call Destroy() when done.
    //
    // THE LIFETIME RULE (the #1 footgun):
    // - Hull data is CLONED into the world's hull database at shape creation — a Hull may be
    //   destroyed right after creating shapes from it.
    // - TriangleMesh / HeightField / Compound data is REFERENCED by shapes — it must stay alive
    //   until every shape using it is destroyed.
    //
    // THE COPY RULE (#2): these are value types, so every copy shares the same native pointer.
    // Destroy() nulls only the copy it was called on — other copies keep the now-dangling pointer
    // and still report IsCreated. Destroy exactly once, through one designated copy (typically the
    // field you stored it in), and never through a local or a returned copy as well.

    /// <summary>Owns native convex hull data (b3HullData*). Cloned at shape creation — safe to
    /// destroy after use. For plain boxes prefer the stack-based <see cref="BoxHull"/>.
    /// <para>Value type: copies share one native pointer — call <see cref="Destroy"/> exactly once,
    /// through one copy; other copies still report <see cref="IsCreated"/> but dangle.</para></summary>
    public struct Hull
    {
        internal IntPtr Data;

        public bool IsCreated => Data != IntPtr.Zero;

        /// <summary>Builds a convex hull enclosing the given points (at most maxVertices kept).</summary>
        public static unsafe Hull Create(ReadOnlySpan<float3> points, int maxVertices = 64)
        {
            if (points.Length < 4) throw new ArgumentException("a hull needs at least 4 points", nameof(points));
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
    /// shapes — must outlive every shape created from it.
    /// <para>Value type: copies share one native pointer — call <see cref="Destroy"/> exactly once,
    /// through one copy; other copies still report <see cref="IsCreated"/> but dangle.</para></summary>
    public struct TriangleMesh
    {
        internal IntPtr Data;

        public bool IsCreated => Data != IntPtr.Zero;

        /// <summary>Builds a mesh from triangle soup. Indices are 3 per triangle.</summary>
        public static unsafe TriangleMesh Create(ReadOnlySpan<float3> vertices, ReadOnlySpan<int> triangles,
            bool identifyEdges = true, bool weldVertices = false, float weldTolerance = 0.005f)
        {
            if (vertices.Length < 3) throw new ArgumentException("a mesh needs at least 3 vertices", nameof(vertices));
            if (triangles.Length < 3 || triangles.Length % 3 != 0)
                throw new ArgumentException("indices must be a non-empty multiple of 3", nameof(triangles));
            for (int index = 0; index < vertices.Length; index++)
            {
                if (!math.all(math.isfinite(vertices[index])))
                    throw new ArgumentException("vertices must be finite", nameof(vertices));
            }
            for (int index = 0; index < triangles.Length; index++)
            {
                if ((uint)triangles[index] >= (uint)vertices.Length)
                    throw new ArgumentOutOfRangeException(nameof(triangles), "every index must refer to a vertex");
            }
            // Older shipped plugins leak their temporary arrays on invalid mesh bounds,
            // including meshes collapsed by welding. Reject that failure path before P/Invoke.
            if (!MeshValidation.HasUsableTriangles(vertices, triangles, weldVertices, weldTolerance)) return default;
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
    /// REFERENCED by shapes — must outlive every shape created from it.
    /// <para>Value type: copies share one native pointer — call <see cref="Destroy"/> exactly once,
    /// through one copy; other copies still report <see cref="IsCreated"/> but dangle.</para></summary>
    public struct HeightField
    {
        internal IntPtr Data;

        public bool IsCreated => Data != IntPtr.Zero;

        /// <summary>Per-quad material index that removes a quad's collision (a hole).</summary>
        public const byte HoleMaterial = 0xFF;

        /// <summary>Builds a height field from row-major grid heights (countX × countZ values,
        /// index = z * countX + x; +X spans columns, +Z spans rows from the field's corner origin).
        /// Min/max heights define the quantization range; use the same values on adjacent fields
        /// that must line up. Optional per-quad material indices ((countX-1) × (countZ-1) values,
        /// same ordering) flow into hit events; <see cref="HoleMaterial"/> (0xFF) removes a quad's
        /// collision — terrain holes.</summary>
        public static unsafe HeightField Create(ReadOnlySpan<float> heights, int countX, int countZ,
            float3 scale, float globalMinimumHeight, float globalMaximumHeight, bool clockwiseWinding = false,
            ReadOnlySpan<byte> materialIndices = default)
        {
            if (countX < 2 || countZ < 2 || heights.Length != countX * countZ)
                throw new ArgumentException("heights must contain exactly countX*countZ values (each count >= 2)", nameof(heights));
            if (materialIndices.Length != 0 && materialIndices.Length != (countX - 1) * (countZ - 1))
                throw new ArgumentException("materialIndices must be empty or contain exactly (countX-1)*(countZ-1) values", nameof(materialIndices));
            fixed (float* h = heights)
            fixed (byte* m = materialIndices)
            {
                var def = new b3HeightFieldDef
                {
                    heights = h,
                    materialIndices = m,
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

    /// <summary>A triangle mesh instance inside a compound. The referenced <see cref="TriangleMesh"/>
    /// must be created before and stay alive until the compound is created (the compound bakes its
    /// own copy of the mesh data; instances sharing one mesh share that single copy).
    /// <see cref="Scale"/> is a per-axis local scale and must be set — zero is rejected, use 1 for
    /// unscaled; negative components mirror the mesh.</summary>
    public struct CompoundMeshInstance
    {
        public TriangleMesh Mesh;
        public B3Transform Transform;
        public float3 Scale;
        public SurfaceMaterial Material;
    }

    /// <summary>Who owns the memory behind a <see cref="Compound"/>, i.e. how
    /// <see cref="Compound.Destroy"/> releases it.</summary>
    internal enum CompoundMemory : byte
    {
        /// <summary>Baked by the engine; freed through it.</summary>
        Engine,
        /// <summary>Allocated by the wrapper for a deserialized blob; freed by the wrapper.</summary>
        Wrapper,
        /// <summary>Caller-owned native memory wrapped in place; never freed by the wrapper.</summary>
        External,
    }

    /// <summary>Owns native compound data (b3CompoundData*): up to 64K spheres/capsules/hulls/meshes
    /// baked into one static shape. Static bodies only. REFERENCED by shapes — must outlive them.
    /// <para>A baked compound is one flat, self-contained blob (children, materials, hull and mesh
    /// copies, culling tree), which makes it the open-world streaming format: bake tiles offline,
    /// <see cref="WriteTo(Stream)"/> them to storage, and bring them back at run time with
    /// <see cref="ReadFrom"/>, <see cref="FromBytes"/>, or — zero-copy — <see cref="FromNativeMemory"/>.
    /// Nothing is re-baked on load: the engine validates the blob and fixes up one internal pointer.</para>
    /// <para>Value type: copies share one native pointer — call <see cref="Destroy"/> exactly once,
    /// through one copy; other copies still report <see cref="IsCreated"/> but dangle.</para></summary>
    public struct Compound
    {
        /// <summary>Alignment of wrapper-allocated blobs; matches the engine's own allocator.</summary>
        private const int Alignment = 64;

        /// <summary>Stream I/O chunk. Unity's Mono FileStream has no Span overloads, so the base
        /// Stream stages each call through an ArrayPool buffer; chunks this size stay pooled (the
        /// shared pool caps at 1 MB) instead of allocating garbage for large blobs.</summary>
        private const int StreamChunk = 256 * 1024;

        /// <summary>The blob starts with its format version (8 bytes) and total length (4 bytes).</summary>
        private const int HeaderSize = sizeof(ulong) + sizeof(int);

        internal IntPtr Data;
        private CompoundMemory _memory;

        public bool IsCreated => Data != IntPtr.Zero;

        /// <summary>Size of the serialized blob in bytes (0 when not created).</summary>
        public unsafe int ByteCount => Data == IntPtr.Zero ? 0 : ((b3CompoundData*)Data)->byteCount;

        /// <summary>Number of child shapes — sphere, capsule, hull, and mesh instances (0 when not created).</summary>
        public unsafe int ChildCount
        {
            get
            {
                if (Data == IntPtr.Zero) return 0;
                var data = (b3CompoundData*)Data;
                return data->sphereCount + data->capsuleCount + data->hullCount + data->meshCount;
            }
        }

        /// <summary>Bakes a compound from child instances. Hull and mesh data is copied in, so the
        /// input <see cref="Hull"/>s and <see cref="TriangleMesh"/>es may be destroyed afterwards.</summary>
        public static unsafe Compound Create(ReadOnlySpan<CompoundSphereInstance> spheres = default,
            ReadOnlySpan<CompoundCapsuleInstance> capsules = default,
            ReadOnlySpan<CompoundHullInstance> hulls = default,
            ReadOnlySpan<CompoundMeshInstance> meshes = default)
        {
            if (spheres.Length + capsules.Length + hulls.Length + meshes.Length == 0)
                throw new ArgumentException("a compound needs at least one instance");
            for (int i = 0; i < hulls.Length; i++)
            {
                if (!hulls[i].Hull.IsCreated)
                    throw new ArgumentException($"hull instance {i} is not created", nameof(hulls));
            }
            for (int i = 0; i < meshes.Length; i++)
            {
                ValidateMeshInstance(meshes[i], i);
            }
            // Sphere/capsule/hull instances mirror the native defs and are pointer-cast. Mesh defs
            // carry a materials pointer, so they are assembled in temporary memory instead.
            b3CompoundMeshDef* meshDefs = meshes.Length == 0
                ? null
                : (b3CompoundMeshDef*)UnsafeUtility.Malloc(meshes.Length * sizeof(b3CompoundMeshDef), 8, Allocator.Temp);
            try
            {
                fixed (CompoundSphereInstance* s = spheres)
                fixed (CompoundCapsuleInstance* c = capsules)
                fixed (CompoundHullInstance* h = hulls)
                fixed (CompoundMeshInstance* m = meshes)
                {
                    for (int i = 0; i < meshes.Length; i++)
                    {
                        meshDefs[i] = new b3CompoundMeshDef
                        {
                            meshData = (b3MeshData*)m[i].Mesh.Data,
                            transform = m[i].Transform,
                            scale = m[i].Scale,
                            materials = &m[i].Material,
                            materialCount = 1,
                        };
                    }
                    var def = new b3CompoundDef
                    {
                        spheres = (b3CompoundSphereDef*)s,
                        sphereCount = spheres.Length,
                        capsules = (b3CompoundCapsuleDef*)c,
                        capsuleCount = capsules.Length,
                        hulls = (b3CompoundHullDef*)h,
                        hullCount = hulls.Length,
                        meshes = meshDefs,
                        meshCount = meshes.Length,
                    };
                    return new Compound { Data = (IntPtr)UnsafeBindings.b3CreateCompound(&def), _memory = CompoundMemory.Engine };
                }
            }
            finally
            {
                if (meshDefs != null) UnsafeUtility.Free(meshDefs, Allocator.Temp);
            }
        }

        private static unsafe void ValidateMeshInstance(in CompoundMeshInstance instance, int index)
        {
            if (!instance.Mesh.IsCreated)
                throw new ArgumentException($"mesh instance {index} is not created", "meshes");
            if (!math.all(math.isfinite(instance.Scale)) || math.any(instance.Scale == 0f))
                throw new ArgumentException($"mesh instance {index} needs a finite, non-zero scale on every axis (1 = unscaled)", "meshes");
            // The engine lines the instance's material array up with the mesh's own material slots.
            // Meshes built by this wrapper have exactly one slot, matching the single Material field.
            if (((b3MeshData*)instance.Mesh.Data)->materialCount != 1)
                throw new ArgumentException($"mesh instance {index} uses a mesh with several material slots; compound meshes take one material", "meshes");
        }

        /// <summary>Writes the serialized blob (<see cref="ByteCount"/> bytes) to a stream straight
        /// from native memory; the compound stays usable. Streams with Span overloads (MemoryStream,
        /// UnmanagedMemoryStream) take the bytes directly; Unity's Mono FileStream stages each chunk
        /// through a pooled buffer. Not safe while a world referencing the compound is stepping.</summary>
        public unsafe void WriteTo(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            ThrowIfNotCreated();
            int byteCount = ByteCount;
            byte* bytes = BeginSerialize();
            try
            {
                for (int offset = 0; offset < byteCount; offset += StreamChunk)
                {
                    stream.Write(new ReadOnlySpan<byte>(bytes + offset, math.min(StreamChunk, byteCount - offset)));
                }
            }
            finally
            {
                EndSerialize(bytes, byteCount);
            }
        }

        /// <summary>Copies the serialized blob into <paramref name="destination"/> (at least
        /// <see cref="ByteCount"/> bytes) and returns the number of bytes written; the compound stays usable.</summary>
        public unsafe int WriteTo(Span<byte> destination)
        {
            ThrowIfNotCreated();
            int byteCount = ByteCount;
            if (destination.Length < byteCount)
                throw new ArgumentException($"destination holds {destination.Length} bytes, the compound needs {byteCount}", nameof(destination));
            byte* bytes = BeginSerialize();
            new ReadOnlySpan<byte>(bytes, byteCount).CopyTo(destination);
            EndSerialize(bytes, byteCount);
            return byteCount;
        }

        /// <summary>The serialized blob as a new managed array (one copy) — e.g. to store a baked
        /// compound in an asset. Prefer <see cref="WriteTo(Stream)"/> for files.</summary>
        public byte[] ToArray()
        {
            ThrowIfNotCreated();
            var bytes = new byte[ByteCount];
            WriteTo(bytes);
            return bytes;
        }

        /// <summary>The engine's "convert to bytes": scrubs the one absolute pointer inside the blob
        /// (the culling tree's node array) in place — O(1), no copy — and returns the same memory.
        /// The compound is unusable until <see cref="EndSerialize"/> restores the pointer.</summary>
        private unsafe byte* BeginSerialize()
        {
            return UnsafeBindings.b3ConvertCompoundToBytes((b3CompoundData*)Data);
        }

        /// <summary>The engine's "convert from bytes" on the same memory: recomputes the node pointer
        /// from its stored offset, leaving the compound exactly as it was before serializing.</summary>
        private static unsafe void EndSerialize(byte* bytes, int byteCount)
        {
            UnsafeBindings.b3ConvertBytesToCompound(bytes, byteCount);
        }

        /// <summary>Reads a blob written by <see cref="WriteTo(Stream)"/> into a new native buffer owned
        /// by the result (<see cref="Destroy"/> frees it). The blob's own header sizes the buffer, so
        /// the stream may carry other data after it. Streams with Span overloads read straight into
        /// native memory; Unity's Mono FileStream stages each chunk through a pooled buffer — for
        /// zero-copy disk streaming read the file into a NativeArray yourself (e.g. with
        /// AsyncReadManager) and use <see cref="FromNativeMemory"/>. Returns a non-created compound
        /// if the blob was baked by a different engine version or is corrupt; throws
        /// <see cref="EndOfStreamException"/> if the stream ends early.</summary>
        public static unsafe Compound ReadFrom(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            Span<byte> header = stackalloc byte[HeaderSize];
            ReadExactly(stream, header);
            int byteCount = BitConverter.ToInt32(header.Slice(sizeof(ulong)));
            if (byteCount < sizeof(b3CompoundData)) return default;
            if (stream.CanSeek && stream.Length - stream.Position < byteCount - HeaderSize)
                throw new EndOfStreamException("the stream is shorter than the compound it declares");
            byte* bytes = (byte*)UnsafeUtility.Malloc(byteCount, Alignment, Allocator.Persistent);
            var blob = new Span<byte>(bytes, byteCount);
            header.CopyTo(blob);
            try
            {
                ReadExactly(stream, blob.Slice(HeaderSize));
            }
            catch
            {
                UnsafeUtility.Free(bytes, Allocator.Persistent);
                throw;
            }
            return Adopt(bytes, byteCount);
        }

        private static void ReadExactly(Stream stream, Span<byte> destination)
        {
            while (destination.Length > 0)
            {
                int read = stream.Read(destination.Slice(0, math.min(StreamChunk, destination.Length)));
                if (read <= 0) throw new EndOfStreamException("the stream ended before the compound was fully read");
                destination = destination.Slice(read);
            }
        }

        /// <summary>Creates a compound from a serialized blob held in managed memory (a TextAsset, an
        /// Addressables payload, …), copying it once into a new aligned native buffer that the result
        /// owns. Data after the blob is ignored. Returns a non-created compound if the blob was baked
        /// by a different engine version or is corrupt.</summary>
        public static unsafe Compound FromBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < sizeof(b3CompoundData))
                throw new ArgumentException("too few bytes to hold a compound", nameof(bytes));
            int byteCount = BitConverter.ToInt32(bytes.Slice(sizeof(ulong)));
            if (byteCount < sizeof(b3CompoundData) || byteCount > bytes.Length) return default;
            byte* buffer = (byte*)UnsafeUtility.Malloc(byteCount, Alignment, Allocator.Persistent);
            bytes.Slice(0, byteCount).CopyTo(new Span<byte>(buffer, byteCount));
            return Adopt(buffer, byteCount);
        }

        /// <summary>Wraps a serialized blob that lives in caller-owned native memory, in place and
        /// without copying — the zero-copy streaming path (pair it with Unity's AsyncReadManager or
        /// your own loader). The engine patches one pointer inside the buffer, which must stay
        /// allocated, unmoved, and 8-byte aligned until the compound and every shape using it are
        /// destroyed; several static bodies may share one buffer. Data after the blob is ignored.
        /// <see cref="Destroy"/> does not free the buffer. Returns a non-created compound if the blob
        /// was baked by a different engine version or is corrupt.</summary>
        public static unsafe Compound FromNativeMemory(NativeArray<byte> bytes)
        {
            if (!bytes.IsCreated) throw new ArgumentException("bytes are not allocated", nameof(bytes));
            if (bytes.Length < sizeof(b3CompoundData))
                throw new ArgumentException("too few bytes to hold a compound", nameof(bytes));
            var buffer = (byte*)bytes.GetUnsafePtr();
            if (((ulong)buffer & 7) != 0) throw new ArgumentException("bytes must be 8-byte aligned", nameof(bytes));
            int byteCount = *(int*)(buffer + sizeof(ulong));
            if (byteCount < sizeof(b3CompoundData) || byteCount > bytes.Length) return default;
            if (UnsafeBindings.b3ConvertBytesToCompound(buffer, byteCount) == null) return default;
            return new Compound { Data = (IntPtr)buffer, _memory = CompoundMemory.External };
        }

        /// <summary>Takes over a wrapper-allocated blob: the engine validates it and fixes up the tree
        /// pointer in place; on rejection the buffer is freed.</summary>
        private static unsafe Compound Adopt(byte* bytes, int byteCount)
        {
            if (UnsafeBindings.b3ConvertBytesToCompound(bytes, byteCount) == null)
            {
                UnsafeUtility.Free(bytes, Allocator.Persistent);
                return default;
            }
            return new Compound { Data = (IntPtr)bytes, _memory = CompoundMemory.Wrapper };
        }

        /// <summary>Releases the compound: engine-baked and deserialized blobs free their memory; a
        /// blob wrapped over caller-owned memory is only forgotten.</summary>
        public unsafe void Destroy()
        {
            if (Data == IntPtr.Zero) return;
            switch (_memory)
            {
                case CompoundMemory.Engine:
                    UnsafeBindings.b3DestroyCompound((b3CompoundData*)Data);
                    break;
                case CompoundMemory.Wrapper:
                    UnsafeUtility.Free((void*)Data, Allocator.Persistent);
                    break;
            }
            Data = IntPtr.Zero;
        }

        private void ThrowIfNotCreated()
        {
            if (Data == IntPtr.Zero) throw new InvalidOperationException("Compound is not created (default or already destroyed)");
        }
    }
}
