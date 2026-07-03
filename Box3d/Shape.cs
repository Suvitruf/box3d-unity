using System;

namespace Box3d
{
    /// <summary>A collision shape attached to a body. Thin value wrapper over a shape id.</summary>
    public partial struct Shape : IEquatable<Shape>
    {
        public ShapeId Id;

        public bool IsValid => UnsafeBindings.b3Shape_IsValid(Id);

        public void Destroy(bool updateBodyMass = true)
        {
            UnsafeBindings.b3DestroyShape(Id, updateBodyMass);
            Id = default;
        }

        /// <summary>Application-specific data attached to the shape.</summary>
        public unsafe IntPtr UserData
        {
            get => (IntPtr)UnsafeBindings.b3Shape_GetUserData(Id);
            set => UnsafeBindings.b3Shape_SetUserData(Id, (void*)value);
        }

        /// <summary>Replaces the sphere geometry of a sphere shape.</summary>
        public unsafe void SetSphere(in Sphere sphere)
        {
            Sphere local = sphere;
            UnsafeBindings.b3Shape_SetSphere(Id, &local);
        }

        /// <summary>Replaces the capsule geometry of a capsule shape.</summary>
        public unsafe void SetCapsule(in Capsule capsule)
        {
            Capsule local = capsule;
            UnsafeBindings.b3Shape_SetCapsule(Id, &local);
        }

        public bool Equals(Shape other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is Shape other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
