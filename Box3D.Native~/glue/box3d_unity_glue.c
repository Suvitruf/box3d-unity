// Unity-specific glue compiled into the box3d library by glue/CMakeLists.txt (the box3d
// checkout itself is never modified).
//
// Burst cannot P/Invoke functions that pass or return structs by value (error BC1064), which
// rules out most of box3d's C API for Burst-compiled callers. These b3u_* exports re-expose the
// hot query/event/body entry points with pointer-and-primitive-only signatures:
//   - ids travel as raw bits (uint32_t b3WorldId / uint64_t b3BodyId — memcpy'd, NOT the
//     b3Store*/ToUInt32 packing), matching the C# side's *(uint*)&id reinterpretation;
//   - struct arguments and results go through pointers.
//
// Bump B3U_GLUE_VERSION whenever an export is added or changed; the C# layer probes it at world
// creation to fail loudly on stale binaries.

#include <box3d/box3d.h>

#include <stdint.h>
#include <string.h>

#if defined(_MSC_VER)
#define B3U_API __declspec(dllexport)
#else
#define B3U_API __attribute__((visibility("default")))
#endif

#define B3U_GLUE_VERSION 1

static b3WorldId b3u_LoadWorldId(uint32_t bits)
{
	b3WorldId id;
	memcpy(&id, &bits, sizeof(id));
	return id;
}

static b3BodyId b3u_LoadBodyId(uint64_t bits)
{
	b3BodyId id;
	memcpy(&id, &bits, sizeof(id));
	return id;
}

B3U_API int32_t b3u_GlueVersion(void)
{
	return B3U_GLUE_VERSION;
}

// ---------------------------------------------------------------- events

B3U_API void b3u_World_GetBodyEvents(uint32_t worldId, const b3BodyMoveEvent** moveEvents, int32_t* moveCount)
{
	b3BodyEvents events = b3World_GetBodyEvents(b3u_LoadWorldId(worldId));
	*moveEvents = events.moveEvents;
	*moveCount = events.moveCount;
}

B3U_API void b3u_World_GetSensorEvents(uint32_t worldId,
	const b3SensorBeginTouchEvent** beginEvents, int32_t* beginCount,
	const b3SensorEndTouchEvent** endEvents, int32_t* endCount)
{
	b3SensorEvents events = b3World_GetSensorEvents(b3u_LoadWorldId(worldId));
	*beginEvents = events.beginEvents;
	*beginCount = events.beginCount;
	*endEvents = events.endEvents;
	*endCount = events.endCount;
}

B3U_API void b3u_World_GetContactEvents(uint32_t worldId,
	const b3ContactBeginTouchEvent** beginEvents, int32_t* beginCount,
	const b3ContactEndTouchEvent** endEvents, int32_t* endCount,
	const b3ContactHitEvent** hitEvents, int32_t* hitCount)
{
	b3ContactEvents events = b3World_GetContactEvents(b3u_LoadWorldId(worldId));
	*beginEvents = events.beginEvents;
	*beginCount = events.beginCount;
	*endEvents = events.endEvents;
	*endCount = events.endCount;
	*hitEvents = events.hitEvents;
	*hitCount = events.hitCount;
}

B3U_API void b3u_World_GetJointEvents(uint32_t worldId, const b3JointEvent** jointEvents, int32_t* count)
{
	b3JointEvents events = b3World_GetJointEvents(b3u_LoadWorldId(worldId));
	*jointEvents = events.jointEvents;
	*count = events.count;
}

// ---------------------------------------------------------------- queries

// Mirrors the C# Box3D.RayHit layout exactly (56 bytes): points/normals stored as floats even in
// double-precision builds, matching the managed collector's narrowing.
typedef struct b3u_RayHit
{
	b3ShapeId shapeId;
	float px, py, pz;
	float nx, ny, nz;
	float fraction;
	uint64_t userMaterialId;
	int32_t triangleIndex;
	int32_t childIndex;
} b3u_RayHit;

typedef struct b3u_RayCollectorContext
{
	b3u_RayHit* buffer;
	int32_t capacity;
	int32_t count;
} b3u_RayCollectorContext;

typedef struct b3u_ShapeCollectorContext
{
	b3ShapeId* buffer;
	int32_t capacity;
	int32_t count;
} b3u_ShapeCollectorContext;

// Same semantics as the managed CastCollector: collect every hit, terminate when full.
static float b3u_CastCollector(b3ShapeId shapeId, b3Pos point, b3Vec3 normal, float fraction,
	uint64_t userMaterialId, int triangleIndex, int childIndex, void* context)
{
	b3u_RayCollectorContext* ctx = context;
	if (ctx->count == ctx->capacity) return 0.0f;
	b3u_RayHit* hit = ctx->buffer + ctx->count;
	hit->shapeId = shapeId;
	hit->px = (float)point.x;
	hit->py = (float)point.y;
	hit->pz = (float)point.z;
	hit->nx = normal.x;
	hit->ny = normal.y;
	hit->nz = normal.z;
	hit->fraction = fraction;
	hit->userMaterialId = userMaterialId;
	hit->triangleIndex = triangleIndex;
	hit->childIndex = childIndex;
	ctx->count++;
	return 1.0f;
}

// Same semantics as the managed OverlapCollector: stop the query once the buffer is full.
static bool b3u_OverlapCollector(b3ShapeId shapeId, void* context)
{
	b3u_ShapeCollectorContext* ctx = context;
	if (ctx->count == ctx->capacity) return false;
	ctx->buffer[ctx->count] = shapeId;
	ctx->count++;
	return true;
}

B3U_API void b3u_World_CastRayClosest(uint32_t worldId, const b3Pos* origin, const b3Vec3* translation,
	const b3QueryFilter* filter, b3RayResult* result)
{
	*result = b3World_CastRayClosest(b3u_LoadWorldId(worldId), *origin, *translation, *filter);
}

B3U_API int32_t b3u_World_CastRay(uint32_t worldId, const b3Pos* origin, const b3Vec3* translation,
	const b3QueryFilter* filter, b3u_RayHit* hits, int32_t capacity, b3TreeStats* stats)
{
	b3u_RayCollectorContext ctx = { hits, capacity, 0 };
	b3TreeStats treeStats = b3World_CastRay(b3u_LoadWorldId(worldId), *origin, *translation, *filter,
		b3u_CastCollector, &ctx);
	if (stats) *stats = treeStats;
	return ctx.count;
}

B3U_API int32_t b3u_World_OverlapAABB(uint32_t worldId, const b3AABB* aabb, const b3QueryFilter* filter,
	b3ShapeId* results, int32_t capacity, b3TreeStats* stats)
{
	b3u_ShapeCollectorContext ctx = { results, capacity, 0 };
	b3TreeStats treeStats = b3World_OverlapAABB(b3u_LoadWorldId(worldId), *aabb, *filter,
		b3u_OverlapCollector, &ctx);
	if (stats) *stats = treeStats;
	return ctx.count;
}

// ---------------------------------------------------------------- bodies

B3U_API void b3u_Body_GetTransform(uint64_t bodyId, b3WorldTransform* result)
{
	*result = b3Body_GetTransform(b3u_LoadBodyId(bodyId));
}

B3U_API void b3u_Body_SetTransform(uint64_t bodyId, const b3Pos* position, const b3Quat* rotation)
{
	b3Body_SetTransform(b3u_LoadBodyId(bodyId), *position, *rotation);
}

B3U_API void b3u_Body_GetLinearVelocity(uint64_t bodyId, b3Vec3* result)
{
	*result = b3Body_GetLinearVelocity(b3u_LoadBodyId(bodyId));
}

B3U_API void b3u_Body_SetLinearVelocity(uint64_t bodyId, const b3Vec3* velocity)
{
	b3Body_SetLinearVelocity(b3u_LoadBodyId(bodyId), *velocity);
}

B3U_API void b3u_Body_GetAngularVelocity(uint64_t bodyId, b3Vec3* result)
{
	*result = b3Body_GetAngularVelocity(b3u_LoadBodyId(bodyId));
}

B3U_API void b3u_Body_SetAngularVelocity(uint64_t bodyId, const b3Vec3* velocity)
{
	b3Body_SetAngularVelocity(b3u_LoadBodyId(bodyId), *velocity);
}

B3U_API void b3u_Body_ApplyLinearImpulse(uint64_t bodyId, const b3Vec3* impulse, const b3Pos* point, bool wake)
{
	b3Body_ApplyLinearImpulse(b3u_LoadBodyId(bodyId), *impulse, *point, wake);
}

B3U_API void b3u_Body_ApplyLinearImpulseToCenter(uint64_t bodyId, const b3Vec3* impulse, bool wake)
{
	b3Body_ApplyLinearImpulseToCenter(b3u_LoadBodyId(bodyId), *impulse, wake);
}
