using System;

namespace Box3d
{
    /// <summary>A Box3d simulation world. Thin value wrapper over a generation-validated world id —
    /// safe to copy; a stale handle fails <see cref="IsValid"/> rather than crashing.</summary>
    public partial struct World : IEquatable<World>
    {
        public WorldId Id;

        public static unsafe World Create(in WorldDef def)
        {
            WorldDef local = def;
            // Debug-shape callbacks must be set at world creation for DrawDebug to render shape
            // interiors. Wire the bridge unless the user supplied their own.
            if (local.CreateDebugShape == IntPtr.Zero)
            {
                local.CreateDebugShape = DebugDrawBridge.CreateShapePtr;
                local.DestroyDebugShape = DebugDrawBridge.DestroyShapePtr;
            }
            return new World { Id = UnsafeBindings.b3CreateWorld(&local) };
        }

        /// <summary>Destroys the world and everything in it. All body/shape/joint ids become stale.</summary>
        public void Destroy()
        {
            ClearCallbackSlots();
            UnsafeBindings.b3DestroyWorld(Id);
            Id = default;
        }

        public bool IsValid => UnsafeBindings.b3World_IsValid(Id);

        /// <summary>Advances the simulation. Use a fixed timeStep (e.g. Time.fixedDeltaTime);
        /// 4 sub-steps is the recommended default.</summary>
        public void Step(float timeStep, int subStepCount = 4)
        {
            UnsafeBindings.b3World_Step(Id, timeStep, subStepCount);
        }

        public unsafe Body CreateBody(in BodyDef def)
        {
            BodyDef local = def;
            return new Body { Id = UnsafeBindings.b3CreateBody(Id, &local) };
        }

        /// <summary>Move events for bodies that moved during the last step.
        /// The span points into transient engine memory — valid only until the next Step or
        /// world mutation. Consume immediately; do not store.</summary>
        public unsafe ReadOnlySpan<BodyMoveEvent> GetBodyMoveEvents()
        {
            BodyEventsRaw raw = UnsafeBindings.b3World_GetBodyEvents(Id);
            return new ReadOnlySpan<BodyMoveEvent>((void*)raw.MoveEvents, raw.MoveCount);
        }

        /// <summary>Contact begin/end/hit events from the last step. Transient — valid only until
        /// the next Step or world mutation. Shapes opt in via ShapeDef.EnableContactEvents /
        /// EnableHitEvents (both false by default).</summary>
        public unsafe ContactEvents GetContactEvents()
        {
            ContactEventsRaw raw = UnsafeBindings.b3World_GetContactEvents(Id);
            return new ContactEvents(
                new ReadOnlySpan<ContactBeginTouchEvent>((void*)raw.BeginEvents, raw.BeginCount),
                new ReadOnlySpan<ContactEndTouchEvent>((void*)raw.EndEvents, raw.EndCount),
                new ReadOnlySpan<ContactHitEvent>((void*)raw.HitEvents, raw.HitCount));
        }

        /// <summary>Sensor begin/end events from the last step. Transient — valid only until the
        /// next Step or world mutation. Both the sensor and visitor shapes must opt in via
        /// ShapeDef.EnableSensorEvents (false by default).</summary>
        public unsafe SensorEvents GetSensorEvents()
        {
            SensorEventsRaw raw = UnsafeBindings.b3World_GetSensorEvents(Id);
            return new SensorEvents(
                new ReadOnlySpan<SensorBeginTouchEvent>((void*)raw.BeginEvents, raw.BeginCount),
                new ReadOnlySpan<SensorEndTouchEvent>((void*)raw.EndEvents, raw.EndCount));
        }

        /// <summary>Joint events (force/torque threshold exceeded) from the last step. Transient —
        /// valid only until the next Step or world mutation.</summary>
        public unsafe ReadOnlySpan<JointEvent> GetJointEvents()
        {
            JointEventsRaw raw = UnsafeBindings.b3World_GetJointEvents(Id);
            return new ReadOnlySpan<JointEvent>((void*)raw.JointEvents, raw.Count);
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
    }
}
