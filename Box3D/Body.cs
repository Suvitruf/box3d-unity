using System;
using Unity.Mathematics;

namespace Box3D
{
    /// <summary>A rigid body. Thin value wrapper over a generation-validated body id.</summary>
    public partial struct Body : IEquatable<Body>
    {
        public BodyId Id;

        /// <summary>Wraps a body id — the way back into the wrapper API from ids delivered by
        /// events (e.g. <see cref="BodyMoveEvent.BodyId"/>) or stored by user code. Cheap and
        /// unchecked; test <see cref="IsValid"/>, or use <see cref="World.TryGetBody"/> for the
        /// validated form.</summary>
        public Body(BodyId id) => Id = id;

        public bool IsValid => UnsafeBindings.b3Body_IsValid(Id);

        public void Destroy()
        {
            if (Id.IsNull) return; // double-destroy would pass a null id into unvalidated native paths
            UnsafeBindings.b3DestroyBody(Id);
            Id = default;
        }

        public float3 Position => UnsafeBindings.b3Body_GetPosition(Id);

        public quaternion Rotation => UnsafeBindings.b3Body_GetRotation(Id);

        public B3Transform Transform => UnsafeBindings.b3Body_GetTransform(Id);

        public float3 LinearVelocity
        {
            get => UnsafeBindings.b3Body_GetLinearVelocity(Id);
            set => UnsafeBindings.b3Body_SetLinearVelocity(Id, value);
        }

        public float3 AngularVelocity
        {
            get => UnsafeBindings.b3Body_GetAngularVelocity(Id);
            set => UnsafeBindings.b3Body_SetAngularVelocity(Id, value);
        }

        public bool IsAwake => UnsafeBindings.b3Body_IsAwake(Id);

        /// <summary>The body's simulation type (static / kinematic / dynamic).</summary>
        public BodyType Type => UnsafeBindings.b3Body_GetType(Id);

        /// <summary>Whether the body participates in simulation (disabled bodies don't collide or move).</summary>
        public bool IsEnabled => UnsafeBindings.b3Body_IsEnabled(Id);

        /// <summary>Application-specific data attached to the body. Delivered back in
        /// <see cref="BodyMoveEvent.UserData"/> — the cheap transform-sync channel.</summary>
        public unsafe IntPtr UserData
        {
            get => (IntPtr)UnsafeBindings.b3Body_GetUserData(Id);
            set => UnsafeBindings.b3Body_SetUserData(Id, (void*)value);
        }

        /// <summary>Number of shapes attached to this body.</summary>
        public int GetShapeCount() => UnsafeBindings.b3Body_GetShapeCount(Id);

        /// <summary>Number of joints attached to this body.</summary>
        public int GetJointCount() => UnsafeBindings.b3Body_GetJointCount(Id);

        /// <summary>Copies the ids of shapes attached to this body into the buffer.
        /// Returns the number written. Size the buffer with GetShapeCount().</summary>
        public unsafe int GetShapes(Span<ShapeId> buffer)
        {
            fixed (ShapeId* p = buffer)
            {
                return UnsafeBindings.b3Body_GetShapes(Id, p, buffer.Length);
            }
        }

        /// <summary>Copies the ids of joints attached to this body into the buffer.
        /// Returns the number written. Size the buffer with GetJointCount().</summary>
        public unsafe int GetJoints(Span<JointId> buffer)
        {
            fixed (JointId* p = buffer)
            {
                return UnsafeBindings.b3Body_GetJoints(Id, p, buffer.Length);
            }
        }

        /// <summary>Snapshots every contact currently on this body — the touching shapes and their
        /// manifold(s) (points, normal, separation, impulses), as of the last <c>World.Step</c>.
        ///
        /// <para>Each native b3ContactData reaches its manifolds through internal engine memory that
        /// may become invalid — that pointer must never be stored. This copies the manifold data into
        /// managed memory here, so every returned <see cref="ContactData"/> is a safe snapshot.
        /// Contacts only carry manifold data once the shapes actually touch.</para></summary>
        public unsafe ContactData[] GetContacts()
        {
            int capacity = UnsafeBindings.b3Body_GetContactCapacity(Id);
            if (capacity == 0) return Array.Empty<ContactData>();

            Span<b3ContactData> buffer = capacity <= 32
                ? stackalloc b3ContactData[capacity]
                : new b3ContactData[capacity];
            int count;
            fixed (b3ContactData* p = buffer)
            {
                count = UnsafeBindings.b3Body_GetContactData(Id, p, capacity);
            }

            var result = new ContactData[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = ContactData.FromNative(buffer[i]);
            }
            return result;
        }

        public unsafe Shape CreateSphereShape(in ShapeDef def, in Sphere sphere)
        {
            ShapeDef localDef = def;
            Sphere localSphere = sphere;
            return new Shape { Id = UnsafeBindings.b3CreateSphereShape(Id, &localDef, &localSphere) };
        }

        public unsafe Shape CreateCapsuleShape(in ShapeDef def, in Capsule capsule)
        {
            ShapeDef localDef = def;
            Capsule localCapsule = capsule;
            return new Shape { Id = UnsafeBindings.b3CreateCapsuleShape(Id, &localDef, &localCapsule) };
        }

        /// <summary>Attaches a convex hull shape. The hull data is fully cloned by the engine, so a
        /// stack-allocated <see cref="BoxHull"/> is safe to discard afterwards.</summary>
        public unsafe Shape CreateHullShape(in ShapeDef def, in BoxHull hull)
        {
            ShapeDef localDef = def;
            BoxHull localHull = hull;
            return new Shape { Id = UnsafeBindings.b3CreateHullShape(Id, &localDef, &localHull.Base) };
        }

        /// <summary>Attaches a convex hull shape. The hull data is cloned into the world — the
        /// <see cref="Hull"/> may be destroyed after this call.</summary>
        public unsafe Shape CreateHullShape(in ShapeDef def, Hull hull)
        {
            if (!hull.IsCreated) throw new ArgumentException("Hull is not created (default or already destroyed)", nameof(hull));
            ShapeDef localDef = def;
            return new Shape { Id = UnsafeBindings.b3CreateHullShape(Id, &localDef, (HullData*)hull.Data) };
        }

        /// <summary>Attaches a triangle mesh shape (static bodies only). The mesh data is
        /// REFERENCED — the <see cref="TriangleMesh"/> must outlive this shape.</summary>
        public unsafe Shape CreateMeshShape(in ShapeDef def, TriangleMesh mesh, float3 scale)
        {
            if (!mesh.IsCreated) throw new ArgumentException("TriangleMesh is not created (default or already destroyed)", nameof(mesh));
            ShapeDef localDef = def;
            return new Shape { Id = UnsafeBindings.b3CreateMeshShape(Id, &localDef, (b3MeshData*)mesh.Data, scale) };
        }

        public Shape CreateMeshShape(in ShapeDef def, TriangleMesh mesh)
        {
            return CreateMeshShape(in def, mesh, new float3(1f, 1f, 1f));
        }

        /// <summary>Attaches a height field shape (static bodies only). The data is REFERENCED —
        /// the <see cref="HeightField"/> must outlive this shape.</summary>
        public unsafe Shape CreateHeightFieldShape(in ShapeDef def, HeightField heightField)
        {
            if (!heightField.IsCreated) throw new ArgumentException("HeightField is not created (default or already destroyed)", nameof(heightField));
            ShapeDef localDef = def;
            return new Shape { Id = UnsafeBindings.b3CreateHeightFieldShape(Id, &localDef, (b3HeightFieldData*)heightField.Data) };
        }

        /// <summary>Attaches a compound shape (static bodies only). The data is REFERENCED —
        /// the <see cref="Compound"/> must outlive this shape.</summary>
        public unsafe Shape CreateCompoundShape(in ShapeDef def, Compound compound)
        {
            if (!compound.IsCreated) throw new ArgumentException("Compound is not created (default or already destroyed)", nameof(compound));
            ShapeDef localDef = def;
            return new Shape { Id = UnsafeBindings.b3CreateCompoundShape(Id, &localDef, (b3CompoundData*)compound.Data) };
        }

        public bool Equals(Body other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is Body other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public static bool operator ==(Body left, Body right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Body left, Body right)
        {
            return !left.Equals(right);
        }

    }
}
