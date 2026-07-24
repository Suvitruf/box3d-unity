namespace Box3D
{
    public partial struct Shape
    {
        /// <summary>The collision filter (category, mask, group) that decides which shapes this collides
        /// with. See <see cref="SetFilter"/>.</summary>
        public CollisionFilter GetFilter() => UnsafeBindings.b3Shape_GetFilter(Id);

        /// <summary>Sets the collision filter. <paramref name="invokeContacts"/> re-evaluates existing
        /// contact pairs immediately (otherwise the change applies as pairs are next considered).</summary>
        public void SetFilter(CollisionFilter filter, bool invokeContacts = true)
        {
            UnsafeBindings.b3Shape_SetFilter(Id, filter, invokeContacts);
        }

        /// <summary>Whether this is a sensor (detects overlap via events, never produces a solid contact).</summary>
        public bool IsSensor() => UnsafeBindings.b3Shape_IsSensor(Id);

        /// <summary>The shape's fat (broadphase) AABB in world space.</summary>
        public B3Aabb GetAABB() => UnsafeBindings.b3Shape_GetAABB(Id);

        /// <summary>The body this shape is attached to.</summary>
        public Body GetBody() => new Body { Id = UnsafeBindings.b3Shape_GetBody(Id) };
    }
}
