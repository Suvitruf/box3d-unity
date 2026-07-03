using System;
using System.Runtime.InteropServices;

namespace Box3d
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate void* b3AllocFcn([NativeTypeName("int32_t")] int size, [NativeTypeName("int32_t")] int alignment);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate void b3FreeFcn(void* mem);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int b3AssertFcn([NativeTypeName("const char *")] sbyte* condition, [NativeTypeName("const char *")] sbyte* fileName, int lineNumber);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate void b3LogFcn([NativeTypeName("const char *")] sbyte* message);

    internal partial struct b3CosSin
    {
        public float cosine;

        public float sine;
    }

    internal partial struct b3SegmentDistanceResult
    {
        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 point1;

        public float fraction1;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 point2;

        public float fraction2;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate void b3TaskCallback(void* taskContext);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate void* b3EnqueueTaskCallback([NativeTypeName("b3TaskCallback *")] IntPtr task, void* taskContext, void* userContext, [NativeTypeName("const char *")] sbyte* taskName);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate void b3FinishTaskCallback(void* userTask, void* userContext);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate void* b3CreateDebugShapeCallback([NativeTypeName("const b3DebugShape *")] b3DebugShape* debugShape, void* userContext);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate void b3DestroyDebugShapeCallback(void* userShape, void* userContext);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate float b3FrictionCallback(float frictionA, [NativeTypeName("uint64_t")] ulong userMaterialIdA, float frictionB, [NativeTypeName("uint64_t")] ulong userMaterialIdB);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate float b3RestitutionCallback(float restitutionA, [NativeTypeName("uint64_t")] ulong userMaterialIdA, float restitutionB, [NativeTypeName("uint64_t")] ulong userMaterialIdB);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    internal unsafe delegate NativeBool b3CustomFilterFcn([NativeTypeName("b3ShapeId")] ShapeId shapeIdA, [NativeTypeName("b3ShapeId")] ShapeId shapeIdB, void* context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    internal unsafe delegate NativeBool b3PreSolveFcn([NativeTypeName("b3ShapeId")] ShapeId shapeIdA, [NativeTypeName("b3ShapeId")] ShapeId shapeIdB, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 point, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 normal, void* context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    internal unsafe delegate NativeBool b3OverlapResultFcn([NativeTypeName("b3ShapeId")] ShapeId shapeId, void* context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate float b3CastResultFcn([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 point, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 normal, float fraction, [NativeTypeName("uint64_t")] ulong userMaterialId, int triangleIndex, int childIndex, void* context);

    internal partial struct b3Profile
    {
        public float step;

        public float pairs;

        public float collide;

        public float solve;

        public float solverSetup;

        public float constraints;

        public float prepareConstraints;

        public float integrateVelocities;

        public float warmStart;

        public float solveImpulses;

        public float integratePositions;

        public float relaxImpulses;

        public float applyRestitution;

        public float storeImpulses;

        public float splitIslands;

        public float transforms;

        public float sensorHits;

        public float jointEvents;

        public float hitEvents;

        public float refit;

        public float bullets;

        public float sleepIslands;

        public float sensors;
    }

    internal unsafe partial struct b3Counters
    {
        public int bodyCount;

        public int shapeCount;

        public int contactCount;

        public int jointCount;

        public int islandCount;

        public int stackUsed;

        public int arenaCapacity;

        public int staticTreeHeight;

        public int treeHeight;

        public int satCallCount;

        public int satCacheHitCount;

        public int byteCount;

        public int taskCount;

        [NativeTypeName("int[24]")]
        public fixed int colorCounts[24];

        [NativeTypeName("int[8]")]
        public fixed int manifoldCounts[8];

        public int awakeContactCount;

        public int recycledContactCount;

        public int distanceIterations;

        public int pushBackIterations;

        public int rootIterations;
    }

    [NativeTypeName("unsigned int")]
    internal enum b3JointType : uint
    {
        b3_parallelJoint,
        b3_distanceJoint,
        b3_filterJoint,
        b3_motorJoint,
        b3_prismaticJoint,
        b3_revoluteJoint,
        b3_sphericalJoint,
        b3_weldJoint,
        b3_wheelJoint,
    }

    internal unsafe partial struct b3ContactData
    {
        [NativeTypeName("b3ContactId")]
        public ContactId contactId;

        [NativeTypeName("b3ShapeId")]
        public ShapeId shapeIdA;

        [NativeTypeName("b3ShapeId")]
        public ShapeId shapeIdB;

        [NativeTypeName("const struct b3Manifold *")]
        public b3Manifold* manifolds;

        public int manifoldCount;
    }

    internal partial struct b3RayCastInput
    {
        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 origin;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 translation;

        public float maxFraction;
    }

    internal unsafe partial struct b3ShapeProxy
    {
        [NativeTypeName("const b3Vec3 *")]
        public Unity.Mathematics.float3* points;

        public int count;

        public float radius;
    }

    internal partial struct b3ShapeCastInput
    {
        public b3ShapeProxy proxy;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 translation;

        public float maxFraction;

        [NativeTypeName("_Bool")]
        public NativeBool canEncroach;
    }

    internal partial struct b3BoxCastInput
    {
        public B3Aabb box;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 translation;

        public float maxFraction;
    }

    internal partial struct b3CastOutput
    {
        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 normal;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 point;

        public float fraction;

        public int iterations;

        public int triangleIndex;

        public int childIndex;

        public int materialIndex;

        [NativeTypeName("_Bool")]
        public NativeBool hit;
    }

    internal partial struct b3BodyCastResult
    {
        [NativeTypeName("b3ShapeId")]
        public ShapeId shapeId;

        [NativeTypeName("b3Pos")]
        public Unity.Mathematics.float3 point;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 normal;

        public float fraction;

        public int triangleIndex;

        [NativeTypeName("uint64_t")]
        public ulong userMaterialId;

        public int iterations;

        [NativeTypeName("_Bool")]
        public NativeBool hit;
    }

    internal unsafe partial struct b3SimplexCache
    {
        public float metric;

        [NativeTypeName("uint16_t")]
        public ushort count;

        [NativeTypeName("uint8_t[4]")]
        public fixed byte indexA[4];

        [NativeTypeName("uint8_t[4]")]
        public fixed byte indexB[4];
    }

    internal partial struct b3ShapeCastPairInput
    {
        public b3ShapeProxy proxyA;

        public b3ShapeProxy proxyB;

        public B3Transform transform;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 translationB;

        public float maxFraction;

        [NativeTypeName("_Bool")]
        public NativeBool canEncroach;
    }

    internal partial struct b3DistanceInput
    {
        public b3ShapeProxy proxyA;

        public b3ShapeProxy proxyB;

        public B3Transform transform;

        [NativeTypeName("_Bool")]
        public NativeBool useRadii;
    }

    internal partial struct b3DistanceOutput
    {
        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 pointA;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 pointB;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 normal;

        public float distance;

        public int iterations;

        public int simplexCount;
    }

    internal partial struct b3SimplexVertex
    {
        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 wA;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 wB;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 w;

        public float a;

        public int indexA;

        public int indexB;
    }

    internal partial struct b3Simplex
    {
        [NativeTypeName("b3SimplexVertex[4]")]
        public _vertices_e__FixedBuffer vertices;

        public int count;

        public partial struct _vertices_e__FixedBuffer
        {
            public b3SimplexVertex e0;
            public b3SimplexVertex e1;
            public b3SimplexVertex e2;
            public b3SimplexVertex e3;

            public unsafe ref b3SimplexVertex this[int index]
            {
                get
                {
                    fixed (b3SimplexVertex* pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    internal partial struct b3Sweep
    {
        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 localCenter;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 c1;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 c2;

        [NativeTypeName("b3Quat")]
        public Unity.Mathematics.quaternion q1;

        [NativeTypeName("b3Quat")]
        public Unity.Mathematics.quaternion q2;
    }

    internal partial struct b3TOIInput
    {
        public b3ShapeProxy proxyA;

        public b3ShapeProxy proxyB;

        public b3Sweep sweepA;

        public b3Sweep sweepB;

        public float maxFraction;
    }

    [NativeTypeName("unsigned int")]
    internal enum b3TOIState : uint
    {
        b3_toiStateUnknown,
        b3_toiStateFailed,
        b3_toiStateOverlapped,
        b3_toiStateHit,
        b3_toiStateSeparated,
    }

    internal partial struct b3TOIOutput
    {
        public b3TOIState state;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 point;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 normal;

        public float fraction;

        public float distance;

        public int distanceIterations;

        public int pushBackIterations;

        public int rootIterations;

        [NativeTypeName("_Bool")]
        public NativeBool usedFallback;
    }

    [NativeTypeName("unsigned int")]
    internal enum b3TreeNodeFlags : uint
    {
        b3_allocatedNode = 0x0001,
        b3_enlargedNode = 0x0002,
        b3_leafNode = 0x0004,
    }

    internal partial struct b3TreeNodeChildren
    {
        public int child1;

        public int child2;
    }

    internal unsafe partial struct b3TreeNode
    {
        public B3Aabb aabb;

        [NativeTypeName("uint64_t")]
        public ulong categoryBits;

        [NativeTypeName("__AnonymousRecord_types_L1688_C2")]
        public _Anonymous1_e__Union Anonymous1;

        [NativeTypeName("__AnonymousRecord_types_L1697_C2")]
        public _Anonymous2_e__Union Anonymous2;

        [NativeTypeName("uint16_t")]
        public ushort height;

        [NativeTypeName("uint16_t")]
        public ushort flags;

        internal ref b3TreeNodeChildren children
        {
            get
            {
                fixed (_Anonymous1_e__Union* pField = &Anonymous1)
                {
                    return ref pField->children;
                }
            }
        }

        internal ref ulong userData
        {
            get
            {
                fixed (_Anonymous1_e__Union* pField = &Anonymous1)
                {
                    return ref pField->userData;
                }
            }
        }

        internal ref int parent
        {
            get
            {
                fixed (_Anonymous2_e__Union* pField = &Anonymous2)
                {
                    return ref pField->parent;
                }
            }
        }

        internal ref int next
        {
            get
            {
                fixed (_Anonymous2_e__Union* pField = &Anonymous2)
                {
                    return ref pField->next;
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal partial struct _Anonymous1_e__Union
        {
            [FieldOffset(0)]
            public b3TreeNodeChildren children;

            [FieldOffset(0)]
            [NativeTypeName("uint64_t")]
            public ulong userData;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal partial struct _Anonymous2_e__Union
        {
            [FieldOffset(0)]
            public int parent;

            [FieldOffset(0)]
            public int next;
        }
    }

    internal unsafe partial struct b3DynamicTree
    {
        [NativeTypeName("uint64_t")]
        public ulong version;

        public b3TreeNode* nodes;

        public int root;

        public int nodeCount;

        public int nodeCapacity;

        public int proxyCount;

        public int freeList;

        public int* leafIndices;

        public B3Aabb* leafBoxes;

        [NativeTypeName("b3Vec3 *")]
        public Unity.Mathematics.float3* leafCenters;

        public int* binIndices;

        public int rebuildCapacity;
    }

    internal partial struct b3TreeStats
    {
        public int nodeVisits;

        public int leafVisits;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    internal unsafe delegate NativeBool b3TreeQueryCallbackFcn(int proxyId, [NativeTypeName("uint64_t")] ulong userData, void* context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate float b3TreeQueryClosestCallbackFcn(float distanceSqrMin, int proxyId, [NativeTypeName("uint64_t")] ulong userData, void* context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate float b3TreeBoxCastCallbackFcn([NativeTypeName("const b3BoxCastInput *")] b3BoxCastInput* input, int proxyId, [NativeTypeName("uint64_t")] ulong userData, void* context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate float b3TreeRayCastCallbackFcn([NativeTypeName("const b3RayCastInput *")] b3RayCastInput* input, int proxyId, [NativeTypeName("uint64_t")] ulong userData, void* context);

    internal partial struct b3BodyPlaneResult
    {
        [NativeTypeName("b3ShapeId")]
        public ShapeId shapeId;

        [NativeTypeName("b3PlaneResult")]
        public PlaneResult result;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    internal unsafe delegate NativeBool b3PlaneResultFcn([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("const b3PlaneResult *")] PlaneResult* plane, int planeCount, void* context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    internal unsafe delegate NativeBool b3MoverFilterFcn([NativeTypeName("b3ShapeId")] ShapeId shapeId, void* context);

    internal partial struct b3HullVertex
    {
        [NativeTypeName("uint8_t")]
        public byte edge;
    }

    internal partial struct b3HullHalfEdge
    {
        [NativeTypeName("uint8_t")]
        public byte next;

        [NativeTypeName("uint8_t")]
        public byte twin;

        [NativeTypeName("uint8_t")]
        public byte origin;

        [NativeTypeName("uint8_t")]
        public byte face;
    }

    internal partial struct b3HullFace
    {
        [NativeTypeName("uint8_t")]
        public byte edge;
    }

    internal unsafe partial struct b3MeshDef
    {
        [NativeTypeName("b3Vec3 *")]
        public Unity.Mathematics.float3* vertices;

        [NativeTypeName("int32_t *")]
        public int* indices;

        [NativeTypeName("uint8_t *")]
        public byte* materialIndices;

        public float weldTolerance;

        public int vertexCount;

        public int triangleCount;

        [NativeTypeName("_Bool")]
        public NativeBool weldVertices;

        [NativeTypeName("_Bool")]
        public NativeBool useMedianSplit;

        [NativeTypeName("_Bool")]
        public NativeBool identifyEdges;
    }

    [NativeTypeName("unsigned int")]
    internal enum b3MeshEdgeFlags : uint
    {
        b3_concaveEdge1 = 0x01,
        b3_concaveEdge2 = 0x02,
        b3_concaveEdge3 = 0x04,
        b3_inverseConcaveEdge1 = 0x10,
        b3_inverseConcaveEdge2 = 0x20,
        b3_inverseConcaveEdge3 = 0x40,
        b3_allConcaveEdges = b3_concaveEdge1 | b3_concaveEdge2 | b3_concaveEdge3,
        b3_flatEdge1 = b3_concaveEdge1 | b3_inverseConcaveEdge1,
        b3_flatEdge2 = b3_concaveEdge2 | b3_inverseConcaveEdge2,
        b3_flatEdge3 = b3_concaveEdge3 | b3_inverseConcaveEdge3,
        b3_allFlatEdges = b3_flatEdge1 | b3_flatEdge2 | b3_flatEdge3,
    }

    internal partial struct b3MeshTriangle
    {
        [NativeTypeName("int32_t")]
        public int index1;

        [NativeTypeName("int32_t")]
        public int index2;

        [NativeTypeName("int32_t")]
        public int index3;
    }

    internal partial struct b3MeshNode
    {
        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 lowerBound;

        [NativeTypeName("__AnonymousRecord_types_L2115_C2")]
        public _data_e__Union data;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 upperBound;

        [NativeTypeName("uint32_t")]
        public uint triangleOffset;

        [StructLayout(LayoutKind.Explicit)]
        internal partial struct _data_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_types_L2118_C3")]
            public _asNode_e__Struct asNode;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_types_L2127_C3")]
            public _asLeaf_e__Struct asLeaf;

            internal partial struct _asNode_e__Struct
            {
                public uint _bitfield;

                [NativeTypeName("uint32_t : 2")]
                public uint axis
                {
                    get
                    {
                        return _bitfield & 0x3u;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~0x3u) | (value & 0x3u);
                    }
                }

                [NativeTypeName("uint32_t : 30")]
                public uint childOffset
                {
                    get
                    {
                        return (_bitfield >> 2) & 0x3FFFFFFFu;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x3FFFFFFFu << 2)) | ((value & 0x3FFFFFFFu) << 2);
                    }
                }
            }

            internal partial struct _asLeaf_e__Struct
            {
                public uint _bitfield;

                [NativeTypeName("uint32_t : 2")]
                public uint type
                {
                    get
                    {
                        return _bitfield & 0x3u;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~0x3u) | (value & 0x3u);
                    }
                }

                [NativeTypeName("uint32_t : 30")]
                public uint triangleCount
                {
                    get
                    {
                        return (_bitfield >> 2) & 0x3FFFFFFFu;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x3FFFFFFFu << 2)) | ((value & 0x3FFFFFFFu) << 2);
                    }
                }
            }
        }
    }

    internal partial struct b3MeshData
    {
        [NativeTypeName("uint64_t")]
        public ulong version;

        public int byteCount;

        [NativeTypeName("uint32_t")]
        public uint hash;

        public B3Aabb bounds;

        public float surfaceArea;

        public int treeHeight;

        public int degenerateCount;

        public int nodeOffset;

        public int nodeCount;

        public int vertexOffset;

        public int vertexCount;

        public int triangleOffset;

        public int triangleCount;

        public int materialOffset;

        public int materialCount;

        public int flagsOffset;
    }

    internal unsafe partial struct b3Mesh
    {
        [NativeTypeName("const b3MeshData *")]
        public b3MeshData* data;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 scale;
    }

    internal unsafe partial struct b3HeightFieldDef
    {
        public float* heights;

        [NativeTypeName("uint8_t *")]
        public byte* materialIndices;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 scale;

        public int countX;

        public int countZ;

        public float globalMinimumHeight;

        public float globalMaximumHeight;

        [NativeTypeName("_Bool")]
        public NativeBool clockwiseWinding;
    }

    internal unsafe partial struct b3HeightFieldData
    {
        [NativeTypeName("uint64_t")]
        public ulong version;

        public int byteCount;

        [NativeTypeName("uint32_t")]
        public uint hash;

        public B3Aabb aabb;

        public float minHeight;

        public float maxHeight;

        public float heightScale;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 scale;

        public int columnCount;

        public int rowCount;

        public int heightsOffset;

        public int materialOffset;

        public int flagsOffset;

        [NativeTypeName("_Bool")]
        public NativeBool clockwise;

        [NativeTypeName("uint8_t[3]")]
        public fixed byte padding[3];
    }

    internal partial struct b3CompoundCapsuleDef
    {
        [NativeTypeName("b3Capsule")]
        public Capsule capsule;

        [NativeTypeName("b3SurfaceMaterial")]
        public SurfaceMaterial material;
    }

    internal unsafe partial struct b3CompoundHullDef
    {
        [NativeTypeName("const b3HullData *")]
        public HullData* hull;

        public B3Transform transform;

        [NativeTypeName("b3SurfaceMaterial")]
        public SurfaceMaterial material;
    }

    internal unsafe partial struct b3CompoundMeshDef
    {
        [NativeTypeName("const b3MeshData *")]
        public b3MeshData* meshData;

        public B3Transform transform;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 scale;

        [NativeTypeName("const b3SurfaceMaterial *")]
        public SurfaceMaterial* materials;

        public int materialCount;
    }

    internal partial struct b3CompoundSphereDef
    {
        [NativeTypeName("b3Sphere")]
        public Sphere sphere;

        [NativeTypeName("b3SurfaceMaterial")]
        public SurfaceMaterial material;
    }

    internal unsafe partial struct b3CompoundDef
    {
        public b3CompoundCapsuleDef* capsules;

        public int capsuleCount;

        public b3CompoundHullDef* hulls;

        public int hullCount;

        public b3CompoundMeshDef* meshes;

        public int meshCount;

        public b3CompoundSphereDef* spheres;

        public int sphereCount;
    }

    internal partial struct b3CompoundData
    {
        [NativeTypeName("uint64_t")]
        public ulong version;

        public int byteCount;

        public int nodeOffset;

        public b3DynamicTree tree;

        public int materialOffset;

        public int materialCount;

        public int capsuleOffset;

        public int capsuleCount;

        public int hullOffset;

        public int hullCount;

        public int sharedHullCount;

        public int meshOffset;

        public int meshCount;

        public int sharedMeshCount;

        public int sphereOffset;

        public int sphereCount;
    }

    internal partial struct b3CompoundCapsule
    {
        [NativeTypeName("b3Capsule")]
        public Capsule capsule;

        public int materialIndex;
    }

    internal unsafe partial struct b3CompoundHull
    {
        [NativeTypeName("const b3HullData *")]
        public HullData* hull;

        public B3Transform transform;

        public int materialIndex;
    }

    internal unsafe partial struct b3CompoundMesh
    {
        [NativeTypeName("const b3MeshData *")]
        public b3MeshData* meshData;

        public B3Transform transform;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 scale;

        [NativeTypeName("int[4]")]
        public fixed int materialIndices[4];
    }

    internal partial struct b3CompoundSphere
    {
        [NativeTypeName("b3Sphere")]
        public Sphere sphere;

        public int materialIndex;
    }

    internal unsafe partial struct b3ChildShape
    {
        [NativeTypeName("__AnonymousRecord_types_L2520_C2")]
        public _Anonymous_e__Union Anonymous;

        public B3Transform transform;

        [NativeTypeName("int[4]")]
        public fixed int materialIndices[4];

        [NativeTypeName("b3ShapeType")]
        public ShapeType type;

        internal ref Capsule capsule
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->capsule;
                }
            }
        }

        internal ref HullData* hull
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->hull;
                }
            }
        }

        internal ref b3Mesh mesh
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->mesh;
                }
            }
        }

        internal ref Sphere sphere
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->sphere;
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("b3Capsule")]
            public Capsule capsule;

            [FieldOffset(0)]
            [NativeTypeName("const b3HullData *")]
            public HullData* hull;

            [FieldOffset(0)]
            public b3Mesh mesh;

            [FieldOffset(0)]
            [NativeTypeName("b3Sphere")]
            public Sphere sphere;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    internal unsafe delegate NativeBool b3CompoundQueryFcn([NativeTypeName("const b3CompoundData *")] b3CompoundData* compound, int childIndex, void* context);

    internal partial struct b3ManifoldPoint
    {
        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 anchorA;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 anchorB;

        public float separation;

        public float baseSeparation;

        public float normalImpulse;

        public float totalNormalImpulse;

        public float normalVelocity;

        [NativeTypeName("uint32_t")]
        public uint featureId;

        public int triangleIndex;

        [NativeTypeName("_Bool")]
        public NativeBool persisted;
    }

    internal partial struct b3Manifold
    {
        [NativeTypeName("b3ManifoldPoint[4]")]
        public _points_e__FixedBuffer points;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 normal;

        public float twistImpulse;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 frictionImpulse;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 rollingImpulse;

        public int pointCount;

        public partial struct _points_e__FixedBuffer
        {
            public b3ManifoldPoint e0;
            public b3ManifoldPoint e1;
            public b3ManifoldPoint e2;
            public b3ManifoldPoint e3;

            public unsafe ref b3ManifoldPoint this[int index]
            {
                get
                {
                    fixed (b3ManifoldPoint* pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    [NativeTypeName("unsigned int")]
    internal enum b3SeparatingFeature : uint
    {
        b3_invalidAxis = 0,
        b3_backsideAxis,
        b3_faceAxisA,
        b3_faceAxisB,
        b3_edgePairAxis,
        b3_closestPointsAxis,
        b3_manualFaceAxisA,
        b3_manualFaceAxisB,
        b3_manualEdgePairAxis,
    }

    [NativeTypeName("unsigned int")]
    internal enum b3TriangleFeature : uint
    {
        b3_featureNone = 0,
        b3_featureTriangleFace,
        b3_featureHullFace,
        b3_featureEdge1,
        b3_featureEdge2,
        b3_featureEdge3,
        b3_featureVertex1,
        b3_featureVertex2,
        b3_featureVertex3,
    }

    internal partial struct b3SATCache
    {
        public float separation;

        [NativeTypeName("uint8_t")]
        public byte type;

        [NativeTypeName("uint8_t")]
        public byte indexA;

        [NativeTypeName("uint8_t")]
        public byte indexB;

        [NativeTypeName("uint8_t")]
        public byte hit;
    }

    internal partial struct b3FeaturePair
    {
        [NativeTypeName("uint8_t")]
        public byte owner1;

        [NativeTypeName("uint8_t")]
        public byte index1;

        [NativeTypeName("uint8_t")]
        public byte owner2;

        [NativeTypeName("uint8_t")]
        public byte index2;
    }

    internal partial struct b3LocalManifoldPoint
    {
        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 point;

        public float separation;

        public b3FeaturePair pair;

        public int triangleIndex;
    }

    internal unsafe partial struct b3LocalManifold
    {
        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 normal;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 triangleNormal;

        public b3LocalManifoldPoint* points;

        public int pointCount;

        public int triangleIndex;

        public int i1;

        public int i2;

        public int i3;

        public float squaredDistance;

        public b3TriangleFeature feature;

        public int triangleFlags;
    }

    [NativeTypeName("unsigned int")]
    internal enum b3HexColor : uint
    {
        b3_colorAliceBlue = 0xF0F8FF,
        b3_colorAntiqueWhite = 0xFAEBD7,
        b3_colorAqua = 0x00FFFF,
        b3_colorAquamarine = 0x7FFFD4,
        b3_colorAzure = 0xF0FFFF,
        b3_colorBeige = 0xF5F5DC,
        b3_colorBisque = 0xFFE4C4,
        b3_colorBlack = 0x000000,
        b3_colorBlanchedAlmond = 0xFFEBCD,
        b3_colorBlue = 0x0000FF,
        b3_colorBlueViolet = 0x8A2BE2,
        b3_colorBrown = 0xA52A2A,
        b3_colorBurlywood = 0xDEB887,
        b3_colorCadetBlue = 0x5F9EA0,
        b3_colorChartreuse = 0x7FFF00,
        b3_colorChocolate = 0xD2691E,
        b3_colorCoral = 0xFF7F50,
        b3_colorCornflowerBlue = 0x6495ED,
        b3_colorCornsilk = 0xFFF8DC,
        b3_colorCrimson = 0xDC143C,
        b3_colorCyan = 0x00FFFF,
        b3_colorDarkBlue = 0x00008B,
        b3_colorDarkCyan = 0x008B8B,
        b3_colorDarkGoldenRod = 0xB8860B,
        b3_colorDarkGray = 0xA9A9A9,
        b3_colorDarkGreen = 0x006400,
        b3_colorDarkKhaki = 0xBDB76B,
        b3_colorDarkMagenta = 0x8B008B,
        b3_colorDarkOliveGreen = 0x556B2F,
        b3_colorDarkOrange = 0xFF8C00,
        b3_colorDarkOrchid = 0x9932CC,
        b3_colorDarkRed = 0x8B0000,
        b3_colorDarkSalmon = 0xE9967A,
        b3_colorDarkSeaGreen = 0x8FBC8F,
        b3_colorDarkSlateBlue = 0x483D8B,
        b3_colorDarkSlateGray = 0x2F4F4F,
        b3_colorDarkTurquoise = 0x00CED1,
        b3_colorDarkViolet = 0x9400D3,
        b3_colorDeepPink = 0xFF1493,
        b3_colorDeepSkyBlue = 0x00BFFF,
        b3_colorDimGray = 0x696969,
        b3_colorDodgerBlue = 0x1E90FF,
        b3_colorFireBrick = 0xB22222,
        b3_colorFloralWhite = 0xFFFAF0,
        b3_colorForestGreen = 0x228B22,
        b3_colorFuchsia = 0xFF00FF,
        b3_colorGainsboro = 0xDCDCDC,
        b3_colorGhostWhite = 0xF8F8FF,
        b3_colorGold = 0xFFD700,
        b3_colorGoldenRod = 0xDAA520,
        b3_colorGray = 0x808080,
        b3_colorGreen = 0x008000,
        b3_colorGreenYellow = 0xADFF2F,
        b3_colorHoneyDew = 0xF0FFF0,
        b3_colorHotPink = 0xFF69B4,
        b3_colorIndianRed = 0xCD5C5C,
        b3_colorIndigo = 0x4B0082,
        b3_colorIvory = 0xFFFFF0,
        b3_colorKhaki = 0xF0E68C,
        b3_colorLavender = 0xE6E6FA,
        b3_colorLavenderBlush = 0xFFF0F5,
        b3_colorLawnGreen = 0x7CFC00,
        b3_colorLemonChiffon = 0xFFFACD,
        b3_colorLightBlue = 0xADD8E6,
        b3_colorLightCoral = 0xF08080,
        b3_colorLightCyan = 0xE0FFFF,
        b3_colorLightGoldenRodYellow = 0xFAFAD2,
        b3_colorLightGray = 0xD3D3D3,
        b3_colorLightGreen = 0x90EE90,
        b3_colorLightPink = 0xFFB6C1,
        b3_colorLightSalmon = 0xFFA07A,
        b3_colorLightSeaGreen = 0x20B2AA,
        b3_colorLightSkyBlue = 0x87CEFA,
        b3_colorLightSlateGray = 0x778899,
        b3_colorLightSteelBlue = 0xB0C4DE,
        b3_colorLightYellow = 0xFFFFE0,
        b3_colorLime = 0x00FF00,
        b3_colorLimeGreen = 0x32CD32,
        b3_colorLinen = 0xFAF0E6,
        b3_colorMagenta = 0xFF00FF,
        b3_colorMaroon = 0x800000,
        b3_colorMediumAquaMarine = 0x66CDAA,
        b3_colorMediumBlue = 0x0000CD,
        b3_colorMediumOrchid = 0xBA55D3,
        b3_colorMediumPurple = 0x9370DB,
        b3_colorMediumSeaGreen = 0x3CB371,
        b3_colorMediumSlateBlue = 0x7B68EE,
        b3_colorMediumSpringGreen = 0x00FA9A,
        b3_colorMediumTurquoise = 0x48D1CC,
        b3_colorMediumVioletRed = 0xC71585,
        b3_colorMidnightBlue = 0x191970,
        b3_colorMintCream = 0xF5FFFA,
        b3_colorMistyRose = 0xFFE4E1,
        b3_colorMoccasin = 0xFFE4B5,
        b3_colorNavajoWhite = 0xFFDEAD,
        b3_colorNavy = 0x000080,
        b3_colorOldLace = 0xFDF5E6,
        b3_colorOlive = 0x808000,
        b3_colorOliveDrab = 0x6B8E23,
        b3_colorOrange = 0xFFA500,
        b3_colorOrangeRed = 0xFF4500,
        b3_colorOrchid = 0xDA70D6,
        b3_colorPaleGoldenRod = 0xEEE8AA,
        b3_colorPaleGreen = 0x98FB98,
        b3_colorPaleTurquoise = 0xAFEEEE,
        b3_colorPaleVioletRed = 0xDB7093,
        b3_colorPapayaWhip = 0xFFEFD5,
        b3_colorPeachPuff = 0xFFDAB9,
        b3_colorPeru = 0xCD853F,
        b3_colorPink = 0xFFC0CB,
        b3_colorPlum = 0xDDA0DD,
        b3_colorPowderBlue = 0xB0E0E6,
        b3_colorPurple = 0x800080,
        b3_colorRebeccaPurple = 0x663399,
        b3_colorRed = 0xFF0000,
        b3_colorRosyBrown = 0xBC8F8F,
        b3_colorRoyalBlue = 0x4169E1,
        b3_colorSaddleBrown = 0x8B4513,
        b3_colorSalmon = 0xFA8072,
        b3_colorSandyBrown = 0xF4A460,
        b3_colorSeaGreen = 0x2E8B57,
        b3_colorSeaShell = 0xFFF5EE,
        b3_colorSienna = 0xA0522D,
        b3_colorSilver = 0xC0C0C0,
        b3_colorSkyBlue = 0x87CEEB,
        b3_colorSlateBlue = 0x6A5ACD,
        b3_colorSlateGray = 0x708090,
        b3_colorSnow = 0xFFFAFA,
        b3_colorSpringGreen = 0x00FF7F,
        b3_colorSteelBlue = 0x4682B4,
        b3_colorTan = 0xD2B48C,
        b3_colorTeal = 0x008080,
        b3_colorThistle = 0xD8BFD8,
        b3_colorTomato = 0xFF6347,
        b3_colorTurquoise = 0x40E0D0,
        b3_colorViolet = 0xEE82EE,
        b3_colorWheat = 0xF5DEB3,
        b3_colorWhite = 0xFFFFFF,
        b3_colorWhiteSmoke = 0xF5F5F5,
        b3_colorYellow = 0xFFFF00,
        b3_colorYellowGreen = 0x9ACD32,
        b3_colorBox2DRed = 0xDC3132,
        b3_colorBox2DBlue = 0x30AEBF,
        b3_colorBox2DGreen = 0x8CC924,
        b3_colorBox2DYellow = 0xFFEE8C,
    }

    [NativeTypeName("unsigned int")]
    internal enum b3DebugMaterial : uint
    {
        b3_debugMaterialDefault = 0,
        b3_debugMaterialMatte,
        b3_debugMaterialSoft,
        b3_debugMaterialDead,
        b3_debugMaterialGlossy,
        b3_debugMaterialMetallic,
    }

    internal unsafe partial struct b3DebugShape
    {
        [NativeTypeName("b3ShapeId")]
        public ShapeId shapeId;

        [NativeTypeName("b3ShapeType")]
        public ShapeType type;

        [NativeTypeName("__AnonymousRecord_types_L2933_C2")]
        public _Anonymous_e__Union Anonymous;

        internal ref Capsule* capsule
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->capsule;
                }
            }
        }

        internal ref b3CompoundData* compound
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->compound;
                }
            }
        }

        internal ref b3HeightFieldData* heightField
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->heightField;
                }
            }
        }

        internal ref HullData* hull
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->hull;
                }
            }
        }

        internal ref b3Mesh* mesh
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->mesh;
                }
            }
        }

        internal ref Sphere* sphere
        {
            get
            {
                fixed (_Anonymous_e__Union* pField = &Anonymous)
                {
                    return ref pField->sphere;
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("const b3Capsule *")]
            public Capsule* capsule;

            [FieldOffset(0)]
            [NativeTypeName("const b3CompoundData *")]
            public b3CompoundData* compound;

            [FieldOffset(0)]
            [NativeTypeName("const b3HeightFieldData *")]
            public b3HeightFieldData* heightField;

            [FieldOffset(0)]
            [NativeTypeName("const b3HullData *")]
            public HullData* hull;

            [FieldOffset(0)]
            [NativeTypeName("const b3Mesh *")]
            public b3Mesh* mesh;

            [FieldOffset(0)]
            [NativeTypeName("const b3Sphere *")]
            public Sphere* sphere;
        }
    }

    internal unsafe partial struct b3DebugDraw
    {
        [NativeTypeName("_Bool (*)(void *, b3WorldTransform, b3HexColor, void *)")]
        public IntPtr DrawShapeFcn;

        [NativeTypeName("void (*)(b3Pos, b3Pos, b3HexColor, void *)")]
        public IntPtr DrawSegmentFcn;

        [NativeTypeName("void (*)(b3WorldTransform, void *)")]
        public IntPtr DrawTransformFcn;

        [NativeTypeName("void (*)(b3Pos, float, b3HexColor, void *)")]
        public IntPtr DrawPointFcn;

        [NativeTypeName("void (*)(b3Pos, float, b3HexColor, float, void *)")]
        public IntPtr DrawSphereFcn;

        [NativeTypeName("void (*)(b3Pos, b3Pos, float, b3HexColor, float, void *)")]
        public IntPtr DrawCapsuleFcn;

        [NativeTypeName("void (*)(b3AABB, b3HexColor, void *)")]
        public IntPtr DrawBoundsFcn;

        [NativeTypeName("void (*)(b3Vec3, b3WorldTransform, b3HexColor, void *)")]
        public IntPtr DrawBoxFcn;

        [NativeTypeName("void (*)(b3Pos, const char *, b3HexColor, void *)")]
        public IntPtr DrawStringFcn;

        public B3Aabb drawingBounds;

        public float forceScale;

        public float jointScale;

        [NativeTypeName("_Bool")]
        public NativeBool drawShapes;

        [NativeTypeName("_Bool")]
        public NativeBool drawJoints;

        [NativeTypeName("_Bool")]
        public NativeBool drawJointExtras;

        [NativeTypeName("_Bool")]
        public NativeBool drawBounds;

        [NativeTypeName("_Bool")]
        public NativeBool drawMass;

        [NativeTypeName("_Bool")]
        public NativeBool drawBodyNames;

        [NativeTypeName("_Bool")]
        public NativeBool drawContacts;

        public int drawAnchorA;

        [NativeTypeName("_Bool")]
        public NativeBool drawGraphColors;

        [NativeTypeName("_Bool")]
        public NativeBool drawContactFeatures;

        [NativeTypeName("_Bool")]
        public NativeBool drawContactNormals;

        [NativeTypeName("_Bool")]
        public NativeBool drawContactForces;

        [NativeTypeName("_Bool")]
        public NativeBool drawFrictionForces;

        [NativeTypeName("_Bool")]
        public NativeBool drawIslands;

        public void* context;
    }

    internal partial struct b3Recording
    {
    }

    internal partial struct b3RecPlayer
    {
    }

    internal partial struct b3RecPlayerInfo
    {
        public int frameCount;

        public int workerCount;

        public float timeStep;

        public int subStepCount;

        public float lengthScale;

        public B3Aabb bounds;
    }

    [NativeTypeName("unsigned int")]
    internal enum b3RecQueryType : uint
    {
        b3_recQueryOverlapAABB,
        b3_recQueryOverlapShape,
        b3_recQueryCastRay,
        b3_recQueryCastShape,
        b3_recQueryCastRayClosest,
        b3_recQueryCastMover,
        b3_recQueryCollideMover,
    }

    internal unsafe partial struct b3RecQueryInfo
    {
        public b3RecQueryType type;

        [NativeTypeName("b3QueryFilter")]
        public QueryFilter filter;

        public B3Aabb aabb;

        [NativeTypeName("b3Pos")]
        public Unity.Mathematics.float3 origin;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 translation;

        public int hitCount;

        [NativeTypeName("uint64_t")]
        public ulong key;

        [NativeTypeName("uint64_t")]
        public ulong id;

        [NativeTypeName("const char *")]
        public sbyte* name;
    }

    internal partial struct b3RecQueryHit
    {
        [NativeTypeName("b3ShapeId")]
        public ShapeId shape;

        [NativeTypeName("b3Pos")]
        public Unity.Mathematics.float3 point;

        [NativeTypeName("b3Vec3")]
        public Unity.Mathematics.float3 normal;

        public float fraction;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    internal unsafe delegate NativeBool b3MeshQueryFcn([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 a, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 b, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 c, int triangleIndex, void* context);

    internal static unsafe partial class UnsafeBindings
    {
        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SetAllocator([NativeTypeName("b3AllocFcn *")] IntPtr allocFcn, [NativeTypeName("b3FreeFcn *")] IntPtr freeFcn);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int b3GetByteCount();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SetAssertFcn([NativeTypeName("b3AssertFcn *")] IntPtr assertFcn);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3InternalAssert([NativeTypeName("const char *")] sbyte* condition, [NativeTypeName("const char *")] sbyte* fileName, int lineNumber);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SetLogFcn([NativeTypeName("b3LogFcn *")] IntPtr logFcn);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Version")]
        public static extern Box3dVersion b3GetVersion();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsDoublePrecision();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint64_t")]
        public static extern ulong b3GetTicks();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3GetMilliseconds([NativeTypeName("uint64_t")] ulong ticks);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3GetMillisecondsAndReset([NativeTypeName("uint64_t *")] ulong* ticks);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Yield();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Sleep(int milliseconds);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint b3Hash([NativeTypeName("uint32_t")] uint hash, [NativeTypeName("const uint8_t *")] byte* data, int count);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WriteBinaryFile(void* data, int size, [NativeTypeName("const char *")] sbyte* fileName);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* b3ReadBinaryFile([NativeTypeName("const char *")] sbyte* prefix, [NativeTypeName("const char *")] sbyte* fileName, int* memSize);

        [NativeTypeName("#define B3_ENABLE_VALIDATION 0")]
        public const int B3_ENABLE_VALIDATION = 0;

        [NativeTypeName("#define B3_NULL_INDEX -1")]
        public const int B3_NULL_INDEX = -1;

        [NativeTypeName("#define B3_HASH_INIT 5381")]
        public const int B3_HASH_INIT = 5381;

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SetLengthUnitsPerMeter(float lengthUnits);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3GetLengthUnitsPerMeter();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SetStallThreshold(float seconds);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3GetStallThreshold();

        [NativeTypeName("#define B3_HUGE ( 1.0e5f * b3GetLengthUnitsPerMeter() )")]
        public static readonly float B3_HUGE = (1.0e5f * b3GetLengthUnitsPerMeter());

        [NativeTypeName("#define B3_MAX_WORKERS 32")]
        public const int B3_MAX_WORKERS = 32;

        [NativeTypeName("#define B3_MAX_TASKS 256")]
        public const int B3_MAX_TASKS = 256;

        [NativeTypeName("#define B3_GRAPH_COLOR_COUNT 24")]
        public const int B3_GRAPH_COLOR_COUNT = 24;

        [NativeTypeName("#define B3_CONTACT_MANIFOLD_COUNT_BUCKETS 8")]
        public const int B3_CONTACT_MANIFOLD_COUNT_BUCKETS = 8;

        [NativeTypeName("#define B3_LINEAR_SLOP ( 0.005f * b3GetLengthUnitsPerMeter() )")]
        public static readonly float B3_LINEAR_SLOP = (0.005f * b3GetLengthUnitsPerMeter());

        [NativeTypeName("#define B3_MIN_CAPSULE_LENGTH ( B3_LINEAR_SLOP )")]
        public static readonly float B3_MIN_CAPSULE_LENGTH = ((0.005f * b3GetLengthUnitsPerMeter()));

        [NativeTypeName("#define B3_OVERLAP_SLOP ( 0.1f * B3_LINEAR_SLOP )")]
        public static readonly float B3_OVERLAP_SLOP = (0.1f * (0.005f * b3GetLengthUnitsPerMeter()));

        [NativeTypeName("#define B3_MAX_WORLDS 128")]
        public const int B3_MAX_WORLDS = 128;

        [NativeTypeName("#define B3_SPECULATIVE_DISTANCE ( 4.0f * B3_LINEAR_SLOP )")]
        public static readonly float B3_SPECULATIVE_DISTANCE = (4.0f * (0.005f * b3GetLengthUnitsPerMeter()));

        [NativeTypeName("#define B3_MESH_REST_OFFSET ( 1.0f * B3_LINEAR_SLOP )")]
        public static readonly float B3_MESH_REST_OFFSET = (1.0f * (0.005f * b3GetLengthUnitsPerMeter()));

        [NativeTypeName("#define B3_CONTACT_RECYCLE_DISTANCE ( 10.0f * B3_LINEAR_SLOP )")]
        public static readonly float B3_CONTACT_RECYCLE_DISTANCE = (10.0f * (0.005f * b3GetLengthUnitsPerMeter()));

        [NativeTypeName("#define B3_CONTACT_RECYCLE_ANGULAR_DISTANCE ( 0.99240388f )")]
        public const float B3_CONTACT_RECYCLE_ANGULAR_DISTANCE = (0.99240388f);

        [NativeTypeName("#define B3_MAX_AABB_MARGIN ( 0.05f * b3GetLengthUnitsPerMeter() )")]
        public static readonly float B3_MAX_AABB_MARGIN = (0.05f * b3GetLengthUnitsPerMeter());

        [NativeTypeName("#define B3_AABB_MARGIN_FRACTION 0.125f")]
        public const float B3_AABB_MARGIN_FRACTION = 0.125f;

        [NativeTypeName("#define B3_TIME_TO_SLEEP 0.5f")]
        public const float B3_TIME_TO_SLEEP = 0.5f;

        [NativeTypeName("#define B3_BODY_NAME_LENGTH 18")]
        public const int B3_BODY_NAME_LENGTH = 18;

        [NativeTypeName("#define B3_SHAPE_NAME_LENGTH 18")]
        public const int B3_SHAPE_NAME_LENGTH = 18;

        [NativeTypeName("#define B3_MAX_MANIFOLD_POINTS 4")]
        public const int B3_MAX_MANIFOLD_POINTS = 4;

        [NativeTypeName("#define B3_MAX_SHAPE_CAST_POINTS 64")]
        public const int B3_MAX_SHAPE_CAST_POINTS = 64;

        [NativeTypeName("#define B3_SHAPE_POWER 22")]
        public const int B3_SHAPE_POWER = 22;

        [NativeTypeName("#define B3_CHILD_POWER ( 64 - 2 * B3_SHAPE_POWER )")]
        public const int B3_CHILD_POWER = (64 - 2 * 22);

        [NativeTypeName("#define B3_MAX_SHAPES ( 1 << B3_SHAPE_POWER )")]
        public const int B3_MAX_SHAPES = (1 << 22);

        [NativeTypeName("#define B3_MAX_CHILD_SHAPES ( 1 << B3_CHILD_POWER )")]
        public const int B3_MAX_CHILD_SHAPES = (1 << (64 - 2 * 22));

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsValidFloat(float a);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Atan2(float y, float x);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CosSin b3ComputeCosSin(float radians);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Quat")]
        public static extern Unity.Mathematics.quaternion b3MakeQuatFromMatrix([NativeTypeName("const b3Matrix3 *")] B3Matrix3* m);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Quat")]
        public static extern Unity.Mathematics.quaternion b3ComputeQuatBetweenUnitVectors([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 v1, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 v2);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Matrix3 b3Steiner(float mass, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 origin);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3PointToSegmentDistance([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 a, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 b, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 q);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3SegmentDistanceResult b3LineDistance([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 p1, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 d1, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 p2, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 d2);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3SegmentDistanceResult b3SegmentDistance([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 p1, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 q1, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 p2, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 q2);


        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsValidVec3([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 a);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsValidQuat([NativeTypeName("b3Quat")] Unity.Mathematics.quaternion q);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsValidTransform(B3Transform a);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsValidMatrix3(B3Matrix3 a);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsValidAABB(B3Aabb a);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsBoundedAABB(B3Aabb a);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsSaneAABB(B3Aabb a);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsValidPlane(B3Plane a);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsValidPosition([NativeTypeName("b3Pos")] Unity.Mathematics.float3 p);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsValidWorldTransform([NativeTypeName("b3WorldTransform")] B3Transform t);

        [NativeTypeName("#define B3_PI 3.14159265359f")]
        public const float B3_PI = 3.14159265359f;

        [NativeTypeName("#define B3_DEG_TO_RAD 0.01745329251f")]
        public const float B3_DEG_TO_RAD = 0.01745329251f;

        [NativeTypeName("#define B3_RAD_TO_DEG 57.2957795131f")]
        public const float B3_RAD_TO_DEG = 57.2957795131f;

        [NativeTypeName("#define B3_MIN_SCALE 0.01f")]
        public const float B3_MIN_SCALE = 0.01f;

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3WorldDef")]
        public static extern WorldDef b3DefaultWorldDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BodyDef")]
        public static extern BodyDef b3DefaultBodyDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Filter")]
        public static extern CollisionFilter b3DefaultFilter();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3SurfaceMaterial")]
        public static extern SurfaceMaterial b3DefaultSurfaceMaterial();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ShapeDef")]
        public static extern ShapeDef b3DefaultShapeDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3DistanceJointDef")]
        public static extern DistanceJointDef b3DefaultDistanceJointDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3MotorJointDef")]
        public static extern MotorJointDef b3DefaultMotorJointDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3FilterJointDef")]
        public static extern FilterJointDef b3DefaultFilterJointDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ParallelJointDef")]
        public static extern ParallelJointDef b3DefaultParallelJointDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3PrismaticJointDef")]
        public static extern PrismaticJointDef b3DefaultPrismaticJointDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3RevoluteJointDef")]
        public static extern RevoluteJointDef b3DefaultRevoluteJointDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3SphericalJointDef")]
        public static extern SphericalJointDef b3DefaultSphericalJointDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3WeldJointDef")]
        public static extern WeldJointDef b3DefaultWeldJointDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3WheelJointDef")]
        public static extern WheelJointDef b3DefaultWheelJointDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ExplosionDef")]
        public static extern ExplosionDef b3DefaultExplosionDef();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3QueryFilter")]
        public static extern QueryFilter b3DefaultQueryFilter();

        [NativeTypeName("const b3SimplexCache")]
        public static readonly b3SimplexCache b3_emptyDistanceCache = new b3SimplexCache
        {
            metric = 0,
        };

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3HexColor b3GetGraphColor(int index);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3DebugDraw b3DefaultDebugDraw();

        [NativeTypeName("#define B3_DEFAULT_CATEGORY_BITS UINT64_MAX")]
        public const ulong B3_DEFAULT_CATEGORY_BITS = (18446744073709551615U);

        [NativeTypeName("#define B3_DEFAULT_MASK_BITS UINT64_MAX")]
        public const ulong B3_DEFAULT_MASK_BITS = (18446744073709551615U);

        [NativeTypeName("#define B3_DYNAMIC_TREE_VERSION 0x93EDAF889FD30B4Aull")]
        public const ulong B3_DYNAMIC_TREE_VERSION = 0x93EDAF889FD30B4AUL;

        [NativeTypeName("#define B3_HULL_VERSION 0x9D4716CE3793900Eull")]
        public const ulong B3_HULL_VERSION = 0x9D4716CE3793900EUL;

        [NativeTypeName("#define B3_MESH_VERSION 0xABD11AB62A6E886Dull")]
        public const ulong B3_MESH_VERSION = 0xABD11AB62A6E886DUL;

        [NativeTypeName("#define B3_HEIGHT_FIELD_HOLE 0xFF")]
        public const int B3_HEIGHT_FIELD_HOLE = 0xFF;

        [NativeTypeName("#define B3_HEIGHT_FIELD_VERSION 0x8B18CBD138A6BC84ull")]
        public const ulong B3_HEIGHT_FIELD_VERSION = 0x8B18CBD138A6BC84UL;

        [NativeTypeName("#define B3_COMPOUND_VERSION ( 0x830778DB07086EB4ull ^ B3_DYNAMIC_TREE_VERSION ^ B3_MESH_VERSION ^ B3_HULL_VERSION )")]
        public const ulong B3_COMPOUND_VERSION = (0x830778DB07086EB4UL ^ 0x93EDAF889FD30B4AUL ^ 0xABD11AB62A6E886DUL ^ 0x9D4716CE3793900EUL);

        [NativeTypeName("#define B3_MAX_COMPOUND_MESH_MATERIALS 4")]
        public const int B3_MAX_COMPOUND_MESH_MATERIALS = 4;

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3WorldId")]
        public static extern WorldId b3CreateWorld([NativeTypeName("const b3WorldDef *")] WorldDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DestroyWorld([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3GetWorldCount();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3GetMaxWorldCount();

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3World_IsValid([NativeTypeName("b3WorldId")] WorldId id);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_Step([NativeTypeName("b3WorldId")] WorldId worldId, float timeStep, int subStepCount);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_Draw([NativeTypeName("b3WorldId")] WorldId worldId, b3DebugDraw* draw, [NativeTypeName("uint64_t")] ulong maskBits);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Aabb b3World_GetBounds([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BodyEvents")]
        public static extern BodyEventsRaw b3World_GetBodyEvents([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3SensorEvents")]
        public static extern SensorEventsRaw b3World_GetSensorEvents([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ContactEvents")]
        public static extern ContactEventsRaw b3World_GetContactEvents([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3JointEvents")]
        public static extern JointEventsRaw b3World_GetJointEvents([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3TreeStats b3World_OverlapAABB([NativeTypeName("b3WorldId")] WorldId worldId, B3Aabb aabb, [NativeTypeName("b3QueryFilter")] QueryFilter filter, [NativeTypeName("b3OverlapResultFcn *")] IntPtr fcn, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3TreeStats b3World_OverlapShape([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 origin, [NativeTypeName("const b3ShapeProxy *")] b3ShapeProxy* proxy, [NativeTypeName("b3QueryFilter")] QueryFilter filter, [NativeTypeName("b3OverlapResultFcn *")] IntPtr fcn, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3TreeStats b3World_CastRay([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 origin, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 translation, [NativeTypeName("b3QueryFilter")] QueryFilter filter, [NativeTypeName("b3CastResultFcn *")] IntPtr fcn, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3RayResult")]
        public static extern RayResult b3World_CastRayClosest([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 origin, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 translation, [NativeTypeName("b3QueryFilter")] QueryFilter filter);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3TreeStats b3World_CastShape([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 origin, [NativeTypeName("const b3ShapeProxy *")] b3ShapeProxy* proxy, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 translation, [NativeTypeName("b3QueryFilter")] QueryFilter filter, [NativeTypeName("b3CastResultFcn *")] IntPtr fcn, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3World_CastMover([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 origin, [NativeTypeName("const b3Capsule *")] Capsule* mover, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 translation, [NativeTypeName("b3QueryFilter")] QueryFilter filter, [NativeTypeName("b3MoverFilterFcn *")] IntPtr fcn, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_CollideMover([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 origin, [NativeTypeName("const b3Capsule *")] Capsule* mover, [NativeTypeName("b3QueryFilter")] QueryFilter filter, [NativeTypeName("b3PlaneResultFcn *")] IntPtr fcn, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_EnableSleeping([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3World_IsSleepingEnabled([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_EnableContinuous([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3World_IsContinuousEnabled([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetRestitutionThreshold([NativeTypeName("b3WorldId")] WorldId worldId, float value);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3World_GetRestitutionThreshold([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetHitEventThreshold([NativeTypeName("b3WorldId")] WorldId worldId, float value);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3World_GetHitEventThreshold([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetCustomFilterCallback([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3CustomFilterFcn *")] IntPtr fcn, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetPreSolveCallback([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3PreSolveFcn *")] IntPtr fcn, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetGravity([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 gravity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3World_GetGravity([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_Explode([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("const b3ExplosionDef *")] ExplosionDef* explosionDef);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetContactTuning([NativeTypeName("b3WorldId")] WorldId worldId, float hertz, float dampingRatio, float contactSpeed);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetContactRecycleDistance([NativeTypeName("b3WorldId")] WorldId worldId, float recycleDistance);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3World_GetContactRecycleDistance([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetMaximumLinearSpeed([NativeTypeName("b3WorldId")] WorldId worldId, float maximumLinearSpeed);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3World_GetMaximumLinearSpeed([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_EnableWarmStarting([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3World_IsWarmStartingEnabled([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3World_GetAwakeBodyCount([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3Profile b3World_GetProfile([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3Counters b3World_GetCounters([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Capacity")]
        public static extern Capacity b3World_GetMaxCapacity([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetUserData([NativeTypeName("b3WorldId")] WorldId worldId, void* userData);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* b3World_GetUserData([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetFrictionCallback([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3FrictionCallback *")] IntPtr callback);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetRestitutionCallback([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3RestitutionCallback *")] IntPtr callback);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_SetWorkerCount([NativeTypeName("b3WorldId")] WorldId worldId, int count);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3World_GetWorkerCount([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_DumpMemoryStats([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_DumpShapeBounds([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("b3BodyType")] BodyType type);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_RebuildStaticTree([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_EnableSpeculative([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_DumpAwake([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_Dump([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3Recording* b3CreateRecording(int byteCapacity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DestroyRecording(b3Recording* recording);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const uint8_t *")]
        public static extern byte* b3Recording_GetData([NativeTypeName("const b3Recording *")] b3Recording* recording);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Recording_GetSize([NativeTypeName("const b3Recording *")] b3Recording* recording);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_StartRecording([NativeTypeName("b3WorldId")] WorldId worldId, b3Recording* recording);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3World_StopRecording([NativeTypeName("b3WorldId")] WorldId worldId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3SaveRecordingToFile([NativeTypeName("const b3Recording *")] b3Recording* recording, [NativeTypeName("const char *")] sbyte* path);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3Recording* b3LoadRecordingFromFile([NativeTypeName("const char *")] sbyte* path);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3ValidateReplay([NativeTypeName("const void *")] void* data, int size, int workerCount);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3RecPlayer* b3RecPlayer_Create([NativeTypeName("const void *")] void* data, int size, int workerCount);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RecPlayer_Destroy(b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3RecPlayer_StepFrame(b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RecPlayer_Restart(b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RecPlayer_SeekFrame(b3RecPlayer* player, int targetFrame);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3WorldId")]
        public static extern WorldId b3RecPlayer_GetWorldId([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3RecPlayer_GetFrame([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3RecPlayer_GetFrameCount([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3RecPlayer_IsAtEnd([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3RecPlayer_HasDiverged([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3RecPlayerInfo b3RecPlayer_GetInfo([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3RecPlayer_GetDivergeFrame([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RecPlayer_SetWorkerCount(b3RecPlayer* player, int count);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RecPlayer_SetKeyframePolicy(b3RecPlayer* player, [NativeTypeName("size_t")] ulong budgetBytes, int minIntervalFrames);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern ulong b3RecPlayer_GetKeyframeBudget([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3RecPlayer_GetKeyframeMinInterval([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3RecPlayer_GetKeyframeInterval([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern ulong b3RecPlayer_GetKeyframeBytes([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3RecPlayer_GetBodyCount([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BodyId")]
        public static extern BodyId b3RecPlayer_GetBodyId([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player, int index);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RecPlayer_SetDebugShapeCallbacks(b3RecPlayer* player, [NativeTypeName("b3CreateDebugShapeCallback *")] IntPtr createDebugShape, [NativeTypeName("b3DestroyDebugShapeCallback *")] IntPtr destroyDebugShape, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RecPlayer_DrawFrameQueries(b3RecPlayer* player, b3DebugDraw* draw, int queryIndex, int selectedIndex);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3RecPlayer_GetFrameQueryCount([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3RecQueryInfo b3RecPlayer_GetFrameQuery([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player, int index);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3RecQueryHit b3RecPlayer_GetFrameQueryHit([NativeTypeName("const b3RecPlayer *")] b3RecPlayer* player, int queryIndex, int hitIndex);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BodyId")]
        public static extern BodyId b3CreateBody([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("const b3BodyDef *")] BodyDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DestroyBody([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Body_IsValid([NativeTypeName("b3BodyId")] BodyId id);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BodyType")]
        public static extern BodyType b3Body_GetType([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetType([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3BodyType")] BodyType type);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetName([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("const char *")] sbyte* name);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* b3Body_GetName([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetUserData([NativeTypeName("b3BodyId")] BodyId bodyId, void* userData);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* b3Body_GetUserData([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Pos")]
        public static extern Unity.Mathematics.float3 b3Body_GetPosition([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Quat")]
        public static extern Unity.Mathematics.quaternion b3Body_GetRotation([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3WorldTransform")]
        public static extern B3Transform b3Body_GetTransform([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetTransform([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 position, [NativeTypeName("b3Quat")] Unity.Mathematics.quaternion rotation);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3Body_GetLocalPoint([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 worldPoint);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Pos")]
        public static extern Unity.Mathematics.float3 b3Body_GetWorldPoint([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 localPoint);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3Body_GetLocalVector([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 worldVector);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3Body_GetWorldVector([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 localVector);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3Body_GetLinearVelocity([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3Body_GetAngularVelocity([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetLinearVelocity([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 linearVelocity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetAngularVelocity([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 angularVelocity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetTargetTransform([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3WorldTransform")] B3Transform target, float timeStep, [NativeTypeName("_Bool")] NativeBool wake);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3Body_GetLocalPointVelocity([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 localPoint);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3Body_GetWorldPointVelocity([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 worldPoint);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_ApplyForce([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 force, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 point, [NativeTypeName("_Bool")] NativeBool wake);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_ApplyForceToCenter([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 force, [NativeTypeName("_Bool")] NativeBool wake);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_ApplyTorque([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 torque, [NativeTypeName("_Bool")] NativeBool wake);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_ApplyLinearImpulse([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 impulse, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 point, [NativeTypeName("_Bool")] NativeBool wake);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_ApplyLinearImpulseToCenter([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 impulse, [NativeTypeName("_Bool")] NativeBool wake);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_ApplyAngularImpulse([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 impulse, [NativeTypeName("_Bool")] NativeBool wake);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Body_GetMass([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Matrix3 b3Body_GetLocalRotationalInertia([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Body_GetInverseMass([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Matrix3 b3Body_GetWorldInverseRotationalInertia([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3Body_GetLocalCenterOfMass([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Pos")]
        public static extern Unity.Mathematics.float3 b3Body_GetWorldCenterOfMass([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetMassData([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3MassData")] MassData massData);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3MassData")]
        public static extern MassData b3Body_GetMassData([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_ApplyMassFromShapes([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetLinearDamping([NativeTypeName("b3BodyId")] BodyId bodyId, float linearDamping);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Body_GetLinearDamping([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetAngularDamping([NativeTypeName("b3BodyId")] BodyId bodyId, float angularDamping);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Body_GetAngularDamping([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetGravityScale([NativeTypeName("b3BodyId")] BodyId bodyId, float gravityScale);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Body_GetGravityScale([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Body_IsAwake([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetAwake([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("_Bool")] NativeBool awake);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_EnableSleep([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("_Bool")] NativeBool enableSleep);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Body_IsSleepEnabled([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetSleepThreshold([NativeTypeName("b3BodyId")] BodyId bodyId, float sleepThreshold);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Body_GetSleepThreshold([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Body_IsEnabled([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_Disable([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_Enable([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetMotionLocks([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3MotionLocks")] MotionLocks locks);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3MotionLocks")]
        public static extern MotionLocks b3Body_GetMotionLocks([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_SetBullet([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Body_IsBullet([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_EnableContactRecycling([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Body_IsContactRecyclingEnabled([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Body_EnableHitEvents([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("_Bool")] NativeBool enableHitEvents);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3WorldId")]
        public static extern WorldId b3Body_GetWorld([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Body_GetShapeCount([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Body_GetShapes([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3ShapeId *")] ShapeId* shapeArray, int capacity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Body_GetJointCount([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Body_GetJoints([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3JointId *")] JointId* jointArray, int capacity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Body_GetContactCapacity([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Body_GetContactData([NativeTypeName("b3BodyId")] BodyId bodyId, b3ContactData* contactData, int capacity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Aabb b3Body_ComputeAABB([NativeTypeName("b3BodyId")] BodyId bodyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Body_GetClosestPoint([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Vec3 *")] Unity.Mathematics.float3* result, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 target);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3BodyCastResult b3Body_CastRay([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 origin, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 translation, [NativeTypeName("b3QueryFilter")] QueryFilter filter, float maxFraction, [NativeTypeName("b3WorldTransform")] B3Transform bodyTransform);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3BodyCastResult b3Body_CastShape([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 origin, [NativeTypeName("const b3ShapeProxy *")] b3ShapeProxy* proxy, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 translation, [NativeTypeName("b3QueryFilter")] QueryFilter filter, float maxFraction, [NativeTypeName("_Bool")] NativeBool canEncroach, [NativeTypeName("b3WorldTransform")] B3Transform bodyTransform);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Body_OverlapShape([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 origin, [NativeTypeName("const b3ShapeProxy *")] b3ShapeProxy* proxy, [NativeTypeName("b3QueryFilter")] QueryFilter filter, [NativeTypeName("b3WorldTransform")] B3Transform bodyTransform);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Body_CollideMover([NativeTypeName("b3BodyId")] BodyId bodyId, b3BodyPlaneResult* bodyPlanes, int planeCapacity, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 origin, [NativeTypeName("const b3Capsule *")] Capsule* mover, [NativeTypeName("b3QueryFilter")] QueryFilter filter, [NativeTypeName("b3WorldTransform")] B3Transform bodyTransform);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ShapeId")]
        public static extern ShapeId b3CreateSphereShape([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("const b3ShapeDef *")] ShapeDef* def, [NativeTypeName("const b3Sphere *")] Sphere* sphere);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ShapeId")]
        public static extern ShapeId b3CreateCapsuleShape([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("const b3ShapeDef *")] ShapeDef* def, [NativeTypeName("const b3Capsule *")] Capsule* capsule);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ShapeId")]
        public static extern ShapeId b3CreateHullShape([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("const b3ShapeDef *")] ShapeDef* def, [NativeTypeName("const b3HullData *")] HullData* hull);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ShapeId")]
        public static extern ShapeId b3CreateTransformedHullShape([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("const b3ShapeDef *")] ShapeDef* def, [NativeTypeName("const b3HullData *")] HullData* hull, B3Transform transform, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 scale);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ShapeId")]
        public static extern ShapeId b3CreateMeshShape([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("const b3ShapeDef *")] ShapeDef* def, [NativeTypeName("const b3MeshData *")] b3MeshData* mesh, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 scale);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ShapeId")]
        public static extern ShapeId b3CreateHeightFieldShape([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("const b3ShapeDef *")] ShapeDef* def, [NativeTypeName("const b3HeightFieldData *")] b3HeightFieldData* heightField);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ShapeId")]
        public static extern ShapeId b3CreateCompoundShape([NativeTypeName("b3BodyId")] BodyId bodyId, [NativeTypeName("b3ShapeDef *")] ShapeDef* def, [NativeTypeName("const b3CompoundData *")] b3CompoundData* compound);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DestroyShape([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("_Bool")] NativeBool updateBodyMass);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Shape_IsValid([NativeTypeName("b3ShapeId")] ShapeId id);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3ShapeType")]
        public static extern ShapeType b3Shape_GetType([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BodyId")]
        public static extern BodyId b3Shape_GetBody([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3WorldId")]
        public static extern WorldId b3Shape_GetWorld([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Shape_IsSensor([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_SetUserData([NativeTypeName("b3ShapeId")] ShapeId shapeId, void* userData);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* b3Shape_GetUserData([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_SetDensity([NativeTypeName("b3ShapeId")] ShapeId shapeId, float density, [NativeTypeName("_Bool")] NativeBool updateBodyMass);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Shape_GetDensity([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_SetFriction([NativeTypeName("b3ShapeId")] ShapeId shapeId, float friction);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Shape_GetFriction([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_SetRestitution([NativeTypeName("b3ShapeId")] ShapeId shapeId, float restitution);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Shape_GetRestitution([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_SetSurfaceMaterial([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("b3SurfaceMaterial")] SurfaceMaterial surfaceMaterial);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3SurfaceMaterial")]
        public static extern SurfaceMaterial b3Shape_GetSurfaceMaterial([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Shape_GetMeshMaterialCount([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_SetMeshMaterial([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("b3SurfaceMaterial")] SurfaceMaterial surfaceMaterial, int index);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3SurfaceMaterial")]
        public static extern SurfaceMaterial b3Shape_GetMeshSurfaceMaterial([NativeTypeName("b3ShapeId")] ShapeId shapeId, int index);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Filter")]
        public static extern CollisionFilter b3Shape_GetFilter([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_SetFilter([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("b3Filter")] CollisionFilter filter, [NativeTypeName("_Bool")] NativeBool invokeContacts);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_EnableSensorEvents([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Shape_AreSensorEventsEnabled([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_EnableContactEvents([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Shape_AreContactEventsEnabled([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_EnablePreSolveEvents([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Shape_ArePreSolveEventsEnabled([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_EnableHitEvents([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Shape_AreHitEventsEnabled([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3WorldCastOutput")]
        public static extern b3CastOutput b3Shape_RayCast([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("b3Pos")] Unity.Mathematics.float3 origin, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 translation);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Sphere")]
        public static extern Sphere b3Shape_GetSphere([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Capsule")]
        public static extern Capsule b3Shape_GetCapsule([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const b3HullData *")]
        public static extern HullData* b3Shape_GetHull([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3Mesh b3Shape_GetMesh([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const b3HeightFieldData *")]
        public static extern b3HeightFieldData* b3Shape_GetHeightField([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_SetSphere([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("const b3Sphere *")] Sphere* sphere);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_SetCapsule([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("const b3Capsule *")] Capsule* capsule);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_SetHull([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("const b3HullData *")] HullData* hull);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_SetMesh([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("const b3MeshData *")] b3MeshData* meshData, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 scale);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Shape_GetContactCapacity([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Shape_GetContactData([NativeTypeName("b3ShapeId")] ShapeId shapeId, b3ContactData* contactData, int capacity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Shape_GetSensorCapacity([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3Shape_GetSensorData([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("b3ShapeId *")] ShapeId* visitorIds, int capacity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Aabb b3Shape_GetAABB([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3MassData")]
        public static extern MassData b3Shape_ComputeMassData([NativeTypeName("b3ShapeId")] ShapeId shapeId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3Shape_GetClosestPoint([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 target);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Shape_ApplyWind([NativeTypeName("b3ShapeId")] ShapeId shapeId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 wind, float drag, float lift, float maxSpeed, [NativeTypeName("_Bool")] NativeBool wake);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DestroyJoint([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool wakeAttached);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Joint_IsValid([NativeTypeName("b3JointId")] JointId id);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3JointType b3Joint_GetType([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BodyId")]
        public static extern BodyId b3Joint_GetBodyA([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BodyId")]
        public static extern BodyId b3Joint_GetBodyB([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3WorldId")]
        public static extern WorldId b3Joint_GetWorld([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Joint_SetLocalFrameA([NativeTypeName("b3JointId")] JointId jointId, B3Transform localFrame);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Transform b3Joint_GetLocalFrameA([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Joint_SetLocalFrameB([NativeTypeName("b3JointId")] JointId jointId, B3Transform localFrame);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Transform b3Joint_GetLocalFrameB([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Joint_SetCollideConnected([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool shouldCollide);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Joint_GetCollideConnected([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Joint_SetUserData([NativeTypeName("b3JointId")] JointId jointId, void* userData);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* b3Joint_GetUserData([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Joint_WakeBodies([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3Joint_GetConstraintForce([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3Joint_GetConstraintTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Joint_GetLinearSeparation([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Joint_GetAngularSeparation([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Joint_SetConstraintTuning([NativeTypeName("b3JointId")] JointId jointId, float hertz, float dampingRatio);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Joint_GetConstraintTuning([NativeTypeName("b3JointId")] JointId jointId, float* hertz, float* dampingRatio);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Joint_SetForceThreshold([NativeTypeName("b3JointId")] JointId jointId, float threshold);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Joint_GetForceThreshold([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3Joint_SetTorqueThreshold([NativeTypeName("b3JointId")] JointId jointId, float threshold);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3Joint_GetTorqueThreshold([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3JointId")]
        public static extern JointId b3CreateParallelJoint([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("const b3ParallelJointDef *")] ParallelJointDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3ParallelJoint_SetSpringHertz([NativeTypeName("b3JointId")] JointId jointId, float hertz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3ParallelJoint_SetSpringDampingRatio([NativeTypeName("b3JointId")] JointId jointId, float dampingRatio);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3ParallelJoint_GetSpringHertz([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3ParallelJoint_GetSpringDampingRatio([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3ParallelJoint_SetMaxTorque([NativeTypeName("b3JointId")] JointId jointId, float force);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3ParallelJoint_GetMaxTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3JointId")]
        public static extern JointId b3CreateDistanceJoint([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("const b3DistanceJointDef *")] DistanceJointDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DistanceJoint_SetLength([NativeTypeName("b3JointId")] JointId jointId, float length);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3DistanceJoint_GetLength([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DistanceJoint_EnableSpring([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableSpring);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3DistanceJoint_IsSpringEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DistanceJoint_SetSpringForceRange([NativeTypeName("b3JointId")] JointId jointId, float lowerForce, float upperForce);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DistanceJoint_GetSpringForceRange([NativeTypeName("b3JointId")] JointId jointId, float* lowerForce, float* upperForce);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DistanceJoint_SetSpringHertz([NativeTypeName("b3JointId")] JointId jointId, float hertz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DistanceJoint_SetSpringDampingRatio([NativeTypeName("b3JointId")] JointId jointId, float dampingRatio);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3DistanceJoint_GetSpringHertz([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3DistanceJoint_GetSpringDampingRatio([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DistanceJoint_EnableLimit([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableLimit);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3DistanceJoint_IsLimitEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DistanceJoint_SetLengthRange([NativeTypeName("b3JointId")] JointId jointId, float minLength, float maxLength);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3DistanceJoint_GetMinLength([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3DistanceJoint_GetMaxLength([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3DistanceJoint_GetCurrentLength([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DistanceJoint_EnableMotor([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableMotor);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3DistanceJoint_IsMotorEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DistanceJoint_SetMotorSpeed([NativeTypeName("b3JointId")] JointId jointId, float motorSpeed);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3DistanceJoint_GetMotorSpeed([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DistanceJoint_SetMaxMotorForce([NativeTypeName("b3JointId")] JointId jointId, float force);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3DistanceJoint_GetMaxMotorForce([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3DistanceJoint_GetMotorForce([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3JointId")]
        public static extern JointId b3CreateMotorJoint([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("const b3MotorJointDef *")] MotorJointDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3MotorJoint_SetLinearVelocity([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 velocity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3MotorJoint_GetLinearVelocity([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3MotorJoint_SetAngularVelocity([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 velocity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3MotorJoint_GetAngularVelocity([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3MotorJoint_SetMaxVelocityForce([NativeTypeName("b3JointId")] JointId jointId, float maxForce);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3MotorJoint_GetMaxVelocityForce([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3MotorJoint_SetMaxVelocityTorque([NativeTypeName("b3JointId")] JointId jointId, float maxTorque);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3MotorJoint_GetMaxVelocityTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3MotorJoint_SetLinearHertz([NativeTypeName("b3JointId")] JointId jointId, float hertz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3MotorJoint_GetLinearHertz([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3MotorJoint_SetLinearDampingRatio([NativeTypeName("b3JointId")] JointId jointId, float damping);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3MotorJoint_GetLinearDampingRatio([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3MotorJoint_SetAngularHertz([NativeTypeName("b3JointId")] JointId jointId, float hertz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3MotorJoint_GetAngularHertz([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3MotorJoint_SetAngularDampingRatio([NativeTypeName("b3JointId")] JointId jointId, float damping);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3MotorJoint_GetAngularDampingRatio([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3MotorJoint_SetMaxSpringForce([NativeTypeName("b3JointId")] JointId jointId, float maxForce);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3MotorJoint_GetMaxSpringForce([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3MotorJoint_SetMaxSpringTorque([NativeTypeName("b3JointId")] JointId jointId, float maxTorque);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3MotorJoint_GetMaxSpringTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3JointId")]
        public static extern JointId b3CreateFilterJoint([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("const b3FilterJointDef *")] FilterJointDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3JointId")]
        public static extern JointId b3CreatePrismaticJoint([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("const b3PrismaticJointDef *")] PrismaticJointDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3PrismaticJoint_EnableSpring([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableSpring);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3PrismaticJoint_IsSpringEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3PrismaticJoint_SetSpringHertz([NativeTypeName("b3JointId")] JointId jointId, float hertz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3PrismaticJoint_GetSpringHertz([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3PrismaticJoint_SetSpringDampingRatio([NativeTypeName("b3JointId")] JointId jointId, float dampingRatio);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3PrismaticJoint_GetSpringDampingRatio([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3PrismaticJoint_SetTargetTranslation([NativeTypeName("b3JointId")] JointId jointId, float targetTranslation);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3PrismaticJoint_GetTargetTranslation([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3PrismaticJoint_EnableLimit([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableLimit);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3PrismaticJoint_IsLimitEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3PrismaticJoint_GetLowerLimit([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3PrismaticJoint_GetUpperLimit([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3PrismaticJoint_SetLimits([NativeTypeName("b3JointId")] JointId jointId, float lower, float upper);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3PrismaticJoint_EnableMotor([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableMotor);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3PrismaticJoint_IsMotorEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3PrismaticJoint_SetMotorSpeed([NativeTypeName("b3JointId")] JointId jointId, float motorSpeed);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3PrismaticJoint_GetMotorSpeed([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3PrismaticJoint_SetMaxMotorForce([NativeTypeName("b3JointId")] JointId jointId, float force);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3PrismaticJoint_GetMaxMotorForce([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3PrismaticJoint_GetMotorForce([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3PrismaticJoint_GetTranslation([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3PrismaticJoint_GetSpeed([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3JointId")]
        public static extern JointId b3CreateRevoluteJoint([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("const b3RevoluteJointDef *")] RevoluteJointDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RevoluteJoint_EnableSpring([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableSpring);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3RevoluteJoint_IsSpringEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RevoluteJoint_SetSpringHertz([NativeTypeName("b3JointId")] JointId jointId, float hertz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3RevoluteJoint_GetSpringHertz([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RevoluteJoint_SetSpringDampingRatio([NativeTypeName("b3JointId")] JointId jointId, float dampingRatio);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3RevoluteJoint_GetSpringDampingRatio([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RevoluteJoint_SetTargetAngle([NativeTypeName("b3JointId")] JointId jointId, float targetRadians);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3RevoluteJoint_GetTargetAngle([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3RevoluteJoint_GetAngle([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RevoluteJoint_EnableLimit([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableLimit);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3RevoluteJoint_IsLimitEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3RevoluteJoint_GetLowerLimit([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3RevoluteJoint_GetUpperLimit([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RevoluteJoint_SetLimits([NativeTypeName("b3JointId")] JointId jointId, float lowerLimitRadians, float upperLimitRadians);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RevoluteJoint_EnableMotor([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableMotor);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3RevoluteJoint_IsMotorEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RevoluteJoint_SetMotorSpeed([NativeTypeName("b3JointId")] JointId jointId, float motorSpeed);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3RevoluteJoint_GetMotorSpeed([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3RevoluteJoint_GetMotorTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3RevoluteJoint_SetMaxMotorTorque([NativeTypeName("b3JointId")] JointId jointId, float torque);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3RevoluteJoint_GetMaxMotorTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3JointId")]
        public static extern JointId b3CreateSphericalJoint([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("const b3SphericalJointDef *")] SphericalJointDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SphericalJoint_EnableConeLimit([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableLimit);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3SphericalJoint_IsConeLimitEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3SphericalJoint_GetConeLimit([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SphericalJoint_SetConeLimit([NativeTypeName("b3JointId")] JointId jointId, float angleRadians);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3SphericalJoint_GetConeAngle([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SphericalJoint_EnableTwistLimit([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableLimit);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3SphericalJoint_IsTwistLimitEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3SphericalJoint_GetLowerTwistLimit([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3SphericalJoint_GetUpperTwistLimit([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SphericalJoint_SetTwistLimits([NativeTypeName("b3JointId")] JointId jointId, float lowerLimitRadians, float upperLimitRadians);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3SphericalJoint_GetTwistAngle([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SphericalJoint_EnableSpring([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableSpring);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3SphericalJoint_IsSpringEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SphericalJoint_SetSpringHertz([NativeTypeName("b3JointId")] JointId jointId, float hertz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3SphericalJoint_GetSpringHertz([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SphericalJoint_SetSpringDampingRatio([NativeTypeName("b3JointId")] JointId jointId, float dampingRatio);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3SphericalJoint_GetSpringDampingRatio([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SphericalJoint_SetTargetRotation([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("b3Quat")] Unity.Mathematics.quaternion targetRotation);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Quat")]
        public static extern Unity.Mathematics.quaternion b3SphericalJoint_GetTargetRotation([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SphericalJoint_EnableMotor([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool enableMotor);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3SphericalJoint_IsMotorEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SphericalJoint_SetMotorVelocity([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 motorVelocity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3SphericalJoint_GetMotorVelocity([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3SphericalJoint_GetMotorTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3SphericalJoint_SetMaxMotorTorque([NativeTypeName("b3JointId")] JointId jointId, float torque);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3SphericalJoint_GetMaxMotorTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3JointId")]
        public static extern JointId b3CreateWeldJoint([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("const b3WeldJointDef *")] WeldJointDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WeldJoint_SetLinearHertz([NativeTypeName("b3JointId")] JointId jointId, float hertz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WeldJoint_GetLinearHertz([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WeldJoint_SetLinearDampingRatio([NativeTypeName("b3JointId")] JointId jointId, float dampingRatio);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WeldJoint_GetLinearDampingRatio([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WeldJoint_SetAngularHertz([NativeTypeName("b3JointId")] JointId jointId, float hertz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WeldJoint_GetAngularHertz([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WeldJoint_SetAngularDampingRatio([NativeTypeName("b3JointId")] JointId jointId, float dampingRatio);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WeldJoint_GetAngularDampingRatio([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3JointId")]
        public static extern JointId b3CreateWheelJoint([NativeTypeName("b3WorldId")] WorldId worldId, [NativeTypeName("const b3WheelJointDef *")] WheelJointDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_EnableSuspension([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3WheelJoint_IsSuspensionEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_SetSuspensionHertz([NativeTypeName("b3JointId")] JointId jointId, float hertz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetSuspensionHertz([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_SetSuspensionDampingRatio([NativeTypeName("b3JointId")] JointId jointId, float dampingRatio);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetSuspensionDampingRatio([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_EnableSuspensionLimit([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3WheelJoint_IsSuspensionLimitEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetLowerSuspensionLimit([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetUpperSuspensionLimit([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_SetSuspensionLimits([NativeTypeName("b3JointId")] JointId jointId, float lower, float upper);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_EnableSpinMotor([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3WheelJoint_IsSpinMotorEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_SetSpinMotorSpeed([NativeTypeName("b3JointId")] JointId jointId, float speed);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetSpinMotorSpeed([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_SetMaxSpinTorque([NativeTypeName("b3JointId")] JointId jointId, float torque);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetMaxSpinTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetSpinSpeed([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetSpinTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_EnableSteering([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3WheelJoint_IsSteeringEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_SetSteeringHertz([NativeTypeName("b3JointId")] JointId jointId, float hertz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetSteeringHertz([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_SetSteeringDampingRatio([NativeTypeName("b3JointId")] JointId jointId, float dampingRatio);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetSteeringDampingRatio([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_SetMaxSteeringTorque([NativeTypeName("b3JointId")] JointId jointId, float torque);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetMaxSteeringTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_EnableSteeringLimit([NativeTypeName("b3JointId")] JointId jointId, [NativeTypeName("_Bool")] NativeBool flag);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3WheelJoint_IsSteeringLimitEnabled([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetLowerSteeringLimit([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetUpperSteeringLimit([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_SetSteeringLimits([NativeTypeName("b3JointId")] JointId jointId, float lowerRadians, float upperRadians);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3WheelJoint_SetTargetSteeringAngle([NativeTypeName("b3JointId")] JointId jointId, float radians);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetTargetSteeringAngle([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetSteeringAngle([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3WheelJoint_GetSteeringTorque([NativeTypeName("b3JointId")] JointId jointId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3Contact_IsValid([NativeTypeName("b3ContactId")] ContactId id);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3ContactData b3Contact_GetData([NativeTypeName("b3ContactId")] ContactId contactId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3DynamicTree b3DynamicTree_Create(int proxyCapacity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DynamicTree_Destroy(b3DynamicTree* tree);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3DynamicTree_CreateProxy(b3DynamicTree* tree, B3Aabb aabb, [NativeTypeName("uint64_t")] ulong categoryBits, [NativeTypeName("uint64_t")] ulong userData);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DynamicTree_DestroyProxy(b3DynamicTree* tree, int proxyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DynamicTree_MoveProxy(b3DynamicTree* tree, int proxyId, B3Aabb aabb);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DynamicTree_EnlargeProxy(b3DynamicTree* tree, int proxyId, B3Aabb aabb);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DynamicTree_SetCategoryBits(b3DynamicTree* tree, int proxyId, [NativeTypeName("uint64_t")] ulong categoryBits);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint64_t")]
        public static extern ulong b3DynamicTree_GetCategoryBits(b3DynamicTree* tree, int proxyId);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3TreeStats b3DynamicTree_Query([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree, B3Aabb aabb, [NativeTypeName("uint64_t")] ulong maskBits, [NativeTypeName("_Bool")] NativeBool requireAllBits, [NativeTypeName("b3TreeQueryCallbackFcn *")] IntPtr callback, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3TreeStats b3DynamicTree_QueryClosest([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 point, [NativeTypeName("uint64_t")] ulong maskBits, [NativeTypeName("_Bool")] NativeBool requireAllBits, [NativeTypeName("b3TreeQueryClosestCallbackFcn *")] IntPtr callback, void* context, float* minDistanceSqr);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3TreeStats b3DynamicTree_RayCast([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree, [NativeTypeName("const b3RayCastInput *")] b3RayCastInput* input, [NativeTypeName("uint64_t")] ulong maskBits, [NativeTypeName("_Bool")] NativeBool requireAllBits, [NativeTypeName("b3TreeRayCastCallbackFcn *")] IntPtr callback, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3TreeStats b3DynamicTree_BoxCast([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree, [NativeTypeName("const b3BoxCastInput *")] b3BoxCastInput* input, [NativeTypeName("uint64_t")] ulong maskBits, [NativeTypeName("_Bool")] NativeBool requireAllBits, [NativeTypeName("b3TreeBoxCastCallbackFcn *")] IntPtr callback, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DynamicTree_Validate([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3DynamicTree_GetHeight([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float b3DynamicTree_GetAreaRatio([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Aabb b3DynamicTree_GetRootBounds([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3DynamicTree_GetProxyCount([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3DynamicTree_Rebuild(b3DynamicTree* tree, [NativeTypeName("_Bool")] NativeBool fullBuild);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3DynamicTree_GetByteCount([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree);


        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DynamicTree_ValidateNoEnlarged([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DynamicTree_Save([NativeTypeName("const b3DynamicTree *")] b3DynamicTree* tree, [NativeTypeName("const char *")] sbyte* fileName);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3DynamicTree b3DynamicTree_Load([NativeTypeName("const char *")] sbyte* fileName, float scale);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3HullData *")]
        public static extern HullData* b3CreateCylinder(float height, float radius, float yOffset, int sides);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3HullData *")]
        public static extern HullData* b3CreateCone(float height, float radius1, float radius2, int slices);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3HullData *")]
        public static extern HullData* b3CreateRock(float radius);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3HullData *")]
        public static extern HullData* b3CreateHull([NativeTypeName("const b3Vec3 *")] Unity.Mathematics.float3* points, int pointCount, int maxVertexCount);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3HullData *")]
        public static extern HullData* b3CloneHull([NativeTypeName("const b3HullData *")] HullData* hull);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3HullData *")]
        public static extern HullData* b3CloneAndTransformHull([NativeTypeName("const b3HullData *")] HullData* original, B3Transform transform, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 scale);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DestroyHull([NativeTypeName("b3HullData *")] HullData* hull);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BoxHull")]
        public static extern BoxHull b3MakeCubeHull(float halfWidth);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BoxHull")]
        public static extern BoxHull b3MakeBoxHull(float hx, float hy, float hz);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BoxHull")]
        public static extern BoxHull b3MakeOffsetBoxHull(float hx, float hy, float hz, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 offset);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BoxHull")]
        public static extern BoxHull b3MakeTransformedBoxHull(float hx, float hy, float hz, B3Transform transform);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3BoxHull")]
        public static extern BoxHull b3MakeScaledBoxHull([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 halfWidths, B3Transform transform, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 postScale);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3ScaleBox([NativeTypeName("b3Vec3 *")] Unity.Mathematics.float3* halfWidths, B3Transform* transform, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 postScale, float minHalfWidth);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3MeshData* b3CreateGridMesh(int xCount, int zCount, float cellWidth, int materialCount, [NativeTypeName("_Bool")] NativeBool identifyEdges);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3MeshData* b3CreateWaveMesh(int xCount, int zCount, float cellWidth, float amplitude, float rowFrequency, float columnFrequency);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3MeshData* b3CreateTorusMesh(int radialResolution, int tubularResolution, float radius, float thickness);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3MeshData* b3CreateBoxMesh([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 center, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 extent, [NativeTypeName("_Bool")] NativeBool identifyEdges);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3MeshData* b3CreateHollowBoxMesh([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 center, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 extent);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3MeshData* b3CreatePlatformMesh([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 center, float height, float topWidth, float bottomWidth);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3MeshData* b3CreateMesh([NativeTypeName("const b3MeshDef *")] b3MeshDef* def, int* degenerateTriangleIndices, int degenerateCapacity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DestroyMesh(b3MeshData* mesh);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int b3GetHeight([NativeTypeName("const b3MeshData *")] b3MeshData* mesh);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3HeightFieldData* b3CreateHeightField([NativeTypeName("const b3HeightFieldDef *")] b3HeightFieldDef* data);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3HeightFieldData* b3CreateGrid(int rowCount, int columnCount, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 scale, [NativeTypeName("_Bool")] NativeBool makeHoles);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3HeightFieldData* b3CreateWave(int rowCount, int columnCount, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 scale, float rowFrequency, float columnFrequency, [NativeTypeName("_Bool")] NativeBool makeHoles);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DestroyHeightField(b3HeightFieldData* heightField);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DumpHeightData([NativeTypeName("const b3HeightFieldDef *")] b3HeightFieldDef* data, [NativeTypeName("const char *")] sbyte* fileName);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3HeightFieldData* b3LoadHeightField([NativeTypeName("const char *")] sbyte* fileName);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3ChildShape b3GetCompoundChild([NativeTypeName("const b3CompoundData *")] b3CompoundData* compound, int childIndex);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3QueryCompound([NativeTypeName("const b3CompoundData *")] b3CompoundData* compound, B3Aabb aabb, [NativeTypeName("b3CompoundQueryFcn *")] IntPtr fcn, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CompoundCapsule b3GetCompoundCapsule([NativeTypeName("const b3CompoundData *")] b3CompoundData* compound, int index);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CompoundHull b3GetCompoundHull([NativeTypeName("const b3CompoundData *")] b3CompoundData* compound, int index);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CompoundMesh b3GetCompoundMesh([NativeTypeName("const b3CompoundData *")] b3CompoundData* compound, int index);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CompoundSphere b3GetCompoundSphere([NativeTypeName("const b3CompoundData *")] b3CompoundData* compound, int index);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const b3SurfaceMaterial *")]
        public static extern SurfaceMaterial* b3GetCompoundMaterials([NativeTypeName("const b3CompoundData *")] b3CompoundData* compound);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CompoundData* b3CreateCompound([NativeTypeName("const b3CompoundDef *")] b3CompoundDef* def);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3DestroyCompound(b3CompoundData* compound);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint8_t *")]
        public static extern byte* b3ConvertCompoundToBytes(b3CompoundData* compound);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CompoundData* b3ConvertBytesToCompound([NativeTypeName("uint8_t *")] byte* bytes, int byteCount);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3MassData")]
        public static extern MassData b3ComputeSphereMass([NativeTypeName("const b3Sphere *")] Sphere* shape, float density);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3MassData")]
        public static extern MassData b3ComputeCapsuleMass([NativeTypeName("const b3Capsule *")] Capsule* shape, float density);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3MassData")]
        public static extern MassData b3ComputeHullMass([NativeTypeName("const b3HullData *")] HullData* shape, float density);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Aabb b3ComputeSphereAABB([NativeTypeName("const b3Sphere *")] Sphere* shape, B3Transform transform);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Aabb b3ComputeCapsuleAABB([NativeTypeName("const b3Capsule *")] Capsule* shape, B3Transform transform);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Aabb b3ComputeHullAABB([NativeTypeName("const b3HullData *")] HullData* shape, B3Transform transform);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Aabb b3ComputeMeshAABB([NativeTypeName("const b3MeshData *")] b3MeshData* shape, B3Transform transform, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 scale);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Aabb b3ComputeHeightFieldAABB([NativeTypeName("const b3HeightFieldData *")] b3HeightFieldData* shape, B3Transform transform);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Aabb b3ComputeCompoundAABB([NativeTypeName("const b3CompoundData *")] b3CompoundData* shape, B3Transform transform);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3IsValidRay([NativeTypeName("const b3RayCastInput *")] b3RayCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3OverlapCapsule([NativeTypeName("const b3Capsule *")] Capsule* shape, B3Transform shapeTransform, [NativeTypeName("const b3ShapeProxy *")] b3ShapeProxy* proxy);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3OverlapCompound([NativeTypeName("const b3CompoundData *")] b3CompoundData* shape, B3Transform shapeTransform, [NativeTypeName("const b3ShapeProxy *")] b3ShapeProxy* proxy);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3OverlapHeightField([NativeTypeName("const b3HeightFieldData *")] b3HeightFieldData* shape, B3Transform shapeTransform, [NativeTypeName("const b3ShapeProxy *")] b3ShapeProxy* proxy);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3OverlapHull([NativeTypeName("const b3HullData *")] HullData* shape, B3Transform shapeTransform, [NativeTypeName("const b3ShapeProxy *")] b3ShapeProxy* proxy);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3OverlapMesh([NativeTypeName("const b3Mesh *")] b3Mesh* shape, B3Transform shapeTransform, [NativeTypeName("const b3ShapeProxy *")] b3ShapeProxy* proxy);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern NativeBool b3OverlapSphere([NativeTypeName("const b3Sphere *")] Sphere* shape, B3Transform shapeTransform, [NativeTypeName("const b3ShapeProxy *")] b3ShapeProxy* proxy);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3RayCastSphere([NativeTypeName("const b3Sphere *")] Sphere* shape, [NativeTypeName("const b3RayCastInput *")] b3RayCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3RayCastHollowSphere([NativeTypeName("const b3Sphere *")] Sphere* shape, [NativeTypeName("const b3RayCastInput *")] b3RayCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3RayCastCapsule([NativeTypeName("const b3Capsule *")] Capsule* shape, [NativeTypeName("const b3RayCastInput *")] b3RayCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3RayCastCompound([NativeTypeName("const b3CompoundData *")] b3CompoundData* shape, [NativeTypeName("const b3RayCastInput *")] b3RayCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3RayCastHull([NativeTypeName("const b3HullData *")] HullData* shape, [NativeTypeName("const b3RayCastInput *")] b3RayCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3RayCastMesh([NativeTypeName("const b3Mesh *")] b3Mesh* shape, [NativeTypeName("const b3RayCastInput *")] b3RayCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3RayCastHeightField([NativeTypeName("const b3HeightFieldData *")] b3HeightFieldData* shape, [NativeTypeName("const b3RayCastInput *")] b3RayCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3ShapeCastSphere([NativeTypeName("const b3Sphere *")] Sphere* shape, [NativeTypeName("const b3ShapeCastInput *")] b3ShapeCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3ShapeCastCapsule([NativeTypeName("const b3Capsule *")] Capsule* shape, [NativeTypeName("const b3ShapeCastInput *")] b3ShapeCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3ShapeCastCompound([NativeTypeName("const b3CompoundData *")] b3CompoundData* shape, [NativeTypeName("const b3ShapeCastInput *")] b3ShapeCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3ShapeCastHull([NativeTypeName("const b3HullData *")] HullData* shape, [NativeTypeName("const b3ShapeCastInput *")] b3ShapeCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3ShapeCastMesh([NativeTypeName("const b3Mesh *")] b3Mesh* shape, [NativeTypeName("const b3ShapeCastInput *")] b3ShapeCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3ShapeCastHeightField([NativeTypeName("const b3HeightFieldData *")] b3HeightFieldData* shape, [NativeTypeName("const b3ShapeCastInput *")] b3ShapeCastInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3QueryMesh([NativeTypeName("const b3Mesh *")] b3Mesh* mesh, [NativeTypeName("const b3AABB")] B3Aabb bounds, [NativeTypeName("b3MeshQueryFcn *")] IntPtr fcn, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3QueryHeightField([NativeTypeName("const b3HeightFieldData *")] b3HeightFieldData* heightField, B3Aabb bounds, [NativeTypeName("b3MeshQueryFcn *")] IntPtr fcn, void* context);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3DistanceOutput b3ShapeDistance([NativeTypeName("const b3DistanceInput *")] b3DistanceInput* input, b3SimplexCache* cache, b3Simplex* simplexes, int simplexCapacity);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3CastOutput b3ShapeCast([NativeTypeName("const b3ShapeCastPairInput *")] b3ShapeCastPairInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern B3Transform b3GetSweepTransform([NativeTypeName("const b3Sweep *")] b3Sweep* sweep, float time);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern b3TOIOutput b3TimeOfImpact([NativeTypeName("const b3TOIInput *")] b3TOIInput* input);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3CollideSpheres(b3LocalManifold* manifold, int capacity, [NativeTypeName("const b3Sphere *")] Sphere* sphereA, [NativeTypeName("const b3Sphere *")] Sphere* sphereB, B3Transform transformBtoA);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3CollideCapsuleAndSphere(b3LocalManifold* manifold, int capacity, [NativeTypeName("const b3Capsule *")] Capsule* capsuleA, [NativeTypeName("const b3Sphere *")] Sphere* sphereB, B3Transform transformBtoA);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3CollideHullAndSphere(b3LocalManifold* manifold, int capacity, [NativeTypeName("const b3HullData *")] HullData* hullA, [NativeTypeName("const b3Sphere *")] Sphere* sphereB, B3Transform transformBtoA, b3SimplexCache* cache);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3CollideCapsules(b3LocalManifold* manifold, int capacity, [NativeTypeName("const b3Capsule *")] Capsule* capsuleA, [NativeTypeName("const b3Capsule *")] Capsule* capsuleB, B3Transform transformBtoA);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3CollideHullAndCapsule(b3LocalManifold* manifold, int capacity, [NativeTypeName("const b3HullData *")] HullData* hullA, [NativeTypeName("const b3Capsule *")] Capsule* capsuleB, B3Transform transformBtoA, b3SimplexCache* cache);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3CollideHulls(b3LocalManifold* manifold, int capacity, [NativeTypeName("const b3HullData *")] HullData* hullA, [NativeTypeName("const b3HullData *")] HullData* hullB, B3Transform transformBtoA, b3SATCache* cache);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3CollideCapsuleAndTriangle(b3LocalManifold* manifold, int capacity, [NativeTypeName("const b3Capsule *")] Capsule* capsuleA, [NativeTypeName("const b3Vec3 *")] Unity.Mathematics.float3* triangleB, b3SimplexCache* cache);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3CollideHullAndTriangle(b3LocalManifold* manifold, int capacity, [NativeTypeName("const b3HullData *")] HullData* hullA, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 v1, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 v2, [NativeTypeName("b3Vec3")] Unity.Mathematics.float3 v3, int triangleFlags, b3SATCache* cache);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void b3CollideSphereAndTriangle(b3LocalManifold* manifold, int capacity, [NativeTypeName("const b3Sphere *")] Sphere* sphereA, [NativeTypeName("const b3Vec3 *")] Unity.Mathematics.float3* triangleB);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3PlaneSolverResult")]
        public static extern PlaneSolverResult b3SolvePlanes([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 targetDelta, [NativeTypeName("b3CollisionPlane *")] CollisionPlane* planes, int count);

        [DllImport(Box3dLibrary.Name, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("b3Vec3")]
        public static extern Unity.Mathematics.float3 b3ClipVector([NativeTypeName("b3Vec3")] Unity.Mathematics.float3 vector, [NativeTypeName("const b3CollisionPlane *")] CollisionPlane* planes, int count);
    }
}
