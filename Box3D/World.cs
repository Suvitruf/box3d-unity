using System;
using UnityEngine;

namespace Box3D
{
    /// <summary>A Box3D simulation world. Thin value wrapper over a generation-validated world id —
    /// safe to copy; a stale handle fails <see cref="IsValid"/> rather than crashing.</summary>
    public partial struct World : IEquatable<World>
    {
        public WorldId Id;

        private static bool _glueProbed;

        // The Burst-callable paths (queries, events, body accessors) go through the b3u_* glue
        // exports; a native library built without them would otherwise fail later with a cryptic
        // EntryPointNotFoundException from some Burst call site. Probe once, up front, loudly.
        private static void ProbeGlueVersion()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (_glueProbed) return;
            _glueProbed = true;
            try
            {
                int version = BurstBindings.b3u_GlueVersion();
                if (version < 1)
                    Debug.LogError($"[Box3D] Native glue version {version} is older than this C# layer expects (1). " +
                                   "Rebuild the native plugins with the Box3D.Native~ build scripts.");
            }
            catch (EntryPointNotFoundException)
            {
                Debug.LogError("[Box3D] Native plugin predates the b3u_* glue exports — queries, events and " +
                               "body accessors will throw. Rebuild the native plugins with the Box3D.Native~ build scripts.");
            }
#endif
        }

        public static unsafe World Create(in WorldDef def)
        {
            ProbeGlueVersion();
            WorldDef local = def;
#if UNITY_WEBGL && !UNITY_EDITOR
            local.WorkerCount = 1; // WebGL players are single-threaded
#endif
            // Debug-shape callbacks must be set at world creation for DrawDebug to render shape
            // interiors. Wire the bridge unless the user supplied their own pair.
            bool bridgeOwnsDebugShapes = local.CreateDebugShape == IntPtr.Zero && local.DestroyDebugShape == IntPtr.Zero;
            if (bridgeOwnsDebugShapes)
            {
                local.CreateDebugShape = DebugDrawBridge.CreateShapePtr;
                local.DestroyDebugShape = DebugDrawBridge.DestroyShapePtr;
            }
            else if (local.CreateDebugShape == IntPtr.Zero || local.DestroyDebugShape == IntPtr.Zero)
            {
                Debug.LogWarning("[Box3D] WorldDef sets only one of CreateDebugShape/DestroyDebugShape — " +
                                 "the native engine requires both; expect crashes when shapes are drawn/destroyed.");
            }
            var world = new World { Id = UnsafeBindings.b3CreateWorld(&local) };
            DebugDrawBridge.SetBridgeOwned(world.Id, bridgeOwnsDebugShapes);
            return world;
        }

        /// <summary>Destroys the world and everything in it. All body/shape/joint ids become stale.</summary>
        public void Destroy()
        {
            if (Id.IsNull) return; // double-destroy would pass a null id into unvalidated native paths
            ClearCallbackSlots();
            UnsafeBindings.b3DestroyWorld(Id);
            Id = default;
        }

        public bool IsValid => UnsafeBindings.b3World_IsValid(Id);

        /// <summary>Resolves a body id (e.g. from <see cref="BodyMoveEvent"/>) to a live wrapper.
        /// False if the id is stale (body destroyed) or belongs to a different world. For ids taken
        /// straight from this world's current event stream, <c>new Body(id)</c> is the cheap
        /// unchecked path.</summary>
        public bool TryGetBody(BodyId id, out Body body)
        {
            body = new Body(id);
            return body.IsValid && body.GetWorld().Equals(Id);
        }

        /// <summary>Resolves a shape id (e.g. from a contact/sensor event or query result) to a
        /// live wrapper. False if the id is stale or belongs to a different world. For ids taken
        /// straight from this world's current event stream, <c>new Shape(id)</c> is the cheap
        /// unchecked path.</summary>
        public bool TryGetShape(ShapeId id, out Shape shape)
        {
            shape = new Shape(id);
            return shape.IsValid && shape.GetWorld().Equals(Id);
        }

        /// <summary>Resolves a joint id (e.g. from <see cref="JointEvent"/>) to a live wrapper.
        /// False if the id is stale or belongs to a different world. For ids taken straight from
        /// this world's current event stream, <c>new Joint(id)</c> is the cheap unchecked path.</summary>
        public bool TryGetJoint(JointId id, out Joint joint)
        {
            joint = new Joint(id);
            return joint.IsValid && joint.GetWorld().Equals(Id);
        }

        /// <summary>Advances the simulation. Use a fixed timeStep (e.g. Time.fixedDeltaTime);
        /// 4 sub-steps is the recommended default. Callable from Burst-compiled code (see
        /// <see cref="BurstBindings"/> for why this routes through a primitive redeclaration).</summary>
        public unsafe void Step(float timeStep, int subStepCount = 4)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            UnsafeBindings.b3World_Step(Id, timeStep, subStepCount);
#else
            WorldId id = Id; // local copy: field addresses inside a struct method aren't fixed
            BurstBindings.b3World_Step(*(uint*)&id, timeStep, subStepCount);
#endif
        }

        public unsafe Body CreateBody(in BodyDef def)
        {
            BodyDef local = def;
            return new Body { Id = UnsafeBindings.b3CreateBody(Id, &local) };
        }

        /// <summary>Move events for bodies that moved during the last step.
        /// The span points into transient engine memory — valid only until the next Step or
        /// world mutation. Consume immediately; do not store. Burst-callable.</summary>
        public unsafe ReadOnlySpan<BodyMoveEvent> GetBodyMoveEvents()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BodyEventsRaw raw = UnsafeBindings.b3World_GetBodyEvents(Id);
            return new ReadOnlySpan<BodyMoveEvent>((void*)raw.MoveEvents, raw.MoveCount);
#else
            WorldId id = Id;
            void* moves; int count;
            BurstBindings.b3u_World_GetBodyEvents(*(uint*)&id, &moves, &count);
            return new ReadOnlySpan<BodyMoveEvent>(moves, count);
#endif
        }

        /// <summary>Contact begin/end/hit events from the last step. Transient — valid only until
        /// the next Step or world mutation. Shapes opt in via ShapeDef.EnableContactEvents /
        /// EnableHitEvents (both false by default). Burst-callable.</summary>
        public unsafe ContactEvents GetContactEvents()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ContactEventsRaw raw = UnsafeBindings.b3World_GetContactEvents(Id);
            return new ContactEvents(
                new ReadOnlySpan<ContactBeginTouchEvent>((void*)raw.BeginEvents, raw.BeginCount),
                new ReadOnlySpan<ContactEndTouchEvent>((void*)raw.EndEvents, raw.EndCount),
                new ReadOnlySpan<ContactHitEvent>((void*)raw.HitEvents, raw.HitCount));
#else
            WorldId id = Id;
            void* begins; int beginCount;
            void* ends; int endCount;
            void* hits; int hitCount;
            BurstBindings.b3u_World_GetContactEvents(*(uint*)&id, &begins, &beginCount, &ends, &endCount, &hits, &hitCount);
            return new ContactEvents(
                new ReadOnlySpan<ContactBeginTouchEvent>(begins, beginCount),
                new ReadOnlySpan<ContactEndTouchEvent>(ends, endCount),
                new ReadOnlySpan<ContactHitEvent>(hits, hitCount));
#endif
        }

        /// <summary>Sensor begin/end events from the last step. Transient — valid only until the
        /// next Step or world mutation. Both the sensor and visitor shapes must opt in via
        /// ShapeDef.EnableSensorEvents (false by default). Burst-callable.</summary>
        public unsafe SensorEvents GetSensorEvents()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SensorEventsRaw raw = UnsafeBindings.b3World_GetSensorEvents(Id);
            return new SensorEvents(
                new ReadOnlySpan<SensorBeginTouchEvent>((void*)raw.BeginEvents, raw.BeginCount),
                new ReadOnlySpan<SensorEndTouchEvent>((void*)raw.EndEvents, raw.EndCount));
#else
            WorldId id = Id;
            void* begins; int beginCount;
            void* ends; int endCount;
            BurstBindings.b3u_World_GetSensorEvents(*(uint*)&id, &begins, &beginCount, &ends, &endCount);
            return new SensorEvents(
                new ReadOnlySpan<SensorBeginTouchEvent>(begins, beginCount),
                new ReadOnlySpan<SensorEndTouchEvent>(ends, endCount));
#endif
        }

        /// <summary>Joint events (force/torque threshold exceeded) from the last step. Transient —
        /// valid only until the next Step or world mutation. Burst-callable.</summary>
        public unsafe ReadOnlySpan<JointEvent> GetJointEvents()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JointEventsRaw raw = UnsafeBindings.b3World_GetJointEvents(Id);
            return new ReadOnlySpan<JointEvent>((void*)raw.JointEvents, raw.Count);
#else
            WorldId id = Id;
            void* events; int count;
            BurstBindings.b3u_World_GetJointEvents(*(uint*)&id, &events, &count);
            return new ReadOnlySpan<JointEvent>(events, count);
#endif
        }

        /// <summary>Applies a radial impulse to shapes within the explosion radius.
        /// Create the def via <see cref="ExplosionDef.Default"/>.</summary>
        public unsafe void Explode(in ExplosionDef def)
        {
            ExplosionDef local = def;
            UnsafeBindings.b3World_Explode(Id, &local);
        }

        /// <summary>Application-specific data attached to the world.</summary>
        public unsafe IntPtr UserData
        {
            get => (IntPtr)UnsafeBindings.b3World_GetUserData(Id);
            set => UnsafeBindings.b3World_SetUserData(Id, (void*)value);
        }

        public bool Equals(World other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is World other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public static bool operator ==(World left, World right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(World left, World right)
        {
            return !left.Equals(right);
        }

    }
}
