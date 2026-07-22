namespace Box3d
{
    public partial struct Joint
    {
        /// <summary>The first body this joint connects.</summary>
        public Body BodyA => new Body { Id = UnsafeBindings.b3Joint_GetBodyA(Id) };

        /// <summary>The second body this joint connects.</summary>
        public Body BodyB => new Body { Id = UnsafeBindings.b3Joint_GetBodyB(Id) };

        /// <summary>Whether the two connected bodies are allowed to collide with each other.</summary>
        public bool CollideConnected => UnsafeBindings.b3Joint_GetCollideConnected(Id);
    }
}
