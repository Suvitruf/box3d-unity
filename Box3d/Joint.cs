using System;

namespace Box3d
{
    /// <summary>A joint of any type. Thin value wrapper over a generation-validated joint id.
    /// Typed joints (<see cref="RevoluteJoint"/>, …) convert implicitly to this.</summary>
    public partial struct Joint : IEquatable<Joint>
    {
        public JointId Id;

        public bool IsValid => UnsafeBindings.b3Joint_IsValid(Id);

        /// <summary>Application-specific data attached to the joint. Delivered back in
        /// <see cref="JointEvent.UserData"/>.</summary>
        public unsafe IntPtr UserData
        {
            get => (IntPtr)UnsafeBindings.b3Joint_GetUserData(Id);
            set => UnsafeBindings.b3Joint_SetUserData(Id, (void*)value);
        }

        public void Destroy(bool wakeAttached = true)
        {
            UnsafeBindings.b3DestroyJoint(Id, wakeAttached);
            Id = default;
        }

        public bool Equals(Joint other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is Joint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
