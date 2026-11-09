using System;

namespace Box3D
{
    /// <summary>A persistent contact between two shapes, referenced from contact events.</summary>
    public partial struct Contact : IEquatable<Contact>
    {
        public ContactId Id;

        /// <summary>Wraps a contact id — the way back into the wrapper API from ids delivered by
        /// contact events (e.g. <see cref="ContactBeginTouchEvent.ContactId"/>). Cheap and
        /// unchecked; test <see cref="IsValid"/>, or use <see cref="World.TryGetContact"/> for the
        /// validated form.</summary>
        public Contact(ContactId id) => Id = id;

        /// <summary>Contacts are destroyed automatically by simulation and world changes —
        /// always validate ids taken from events before use.</summary>
        public bool IsValid => UnsafeBindings.b3Contact_IsValid(Id);

        /// <summary>Returns a snapshot of this contact — the two shapes and their manifold(s), with
        /// contact points, normal, separation and impulses. Reflects the state at the last
        /// <c>World.Step</c>.
        ///
        /// <para>Native b3ContactData points at the manifolds through internal engine memory that may
        /// become invalid — the raw pointer must never be stored. This copies the manifold data into
        /// managed memory here, so the returned <see cref="ContactData"/> is safe to keep. Validate
        /// <see cref="IsValid"/> before calling on an id taken from an event.</para></summary>
        public ContactData GetData()
        {
            return ContactData.FromNative(UnsafeBindings.b3Contact_GetData(Id));
        }

        public bool Equals(Contact other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is Contact other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Contact left, Contact right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Contact left, Contact right)
        {
            return !left.Equals(right);
        }
    }
}
