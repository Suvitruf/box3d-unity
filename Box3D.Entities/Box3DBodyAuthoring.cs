using Unity.Entities;
using UnityEngine;

namespace Box3D.Entities
{
    /// <summary>Bakes a GameObject into a Box3D ECS body: adds the definition components that make
    /// <see cref="Box3DBodyLifecycleSystem"/> create a native body with one shape at runtime.</summary>
    public class Box3DBodyAuthoring : MonoBehaviour
    {
        public BodyType BodyType = BodyType.Dynamic;
        public Box3DShapeKind Shape = Box3DShapeKind.Box;

        [Tooltip("Box only.")]
        public Vector3 HalfExtents = new Vector3(0.5f, 0.5f, 0.5f);

        [Tooltip("Sphere and capsule.")]
        public float Radius = 0.5f;

        [Tooltip("Capsule only: half the distance between the hemisphere centers (along local Y).")]
        public float HalfHeight = 0.5f;

        [Tooltip("Zero falls back to the engine's default density.")]
        public float Density;

        public float Friction = 0.6f;
        public float Restitution;
        public float GravityScale = 1f;
        public float LinearDamping;
        public float AngularDamping;

        [Tooltip("Continuous collision detection for fast-moving bodies.")]
        public bool IsBullet;

        public bool EnableSleep = true;
    }

    public class Box3DBodyBaker : Baker<Box3DBodyAuthoring>
    {
        public override void Bake(Box3DBodyAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Box3DBodyDefinition
            {
                Type = authoring.BodyType,
                GravityScale = authoring.GravityScale,
                LinearDamping = authoring.LinearDamping,
                AngularDamping = authoring.AngularDamping,
                IsBullet = authoring.IsBullet,
                EnableSleep = authoring.EnableSleep,
            });
            AddComponent(entity, new Box3DShapeDefinition
            {
                Kind = authoring.Shape,
                HalfExtents = authoring.HalfExtents,
                Radius = authoring.Radius,
                HalfHeight = authoring.HalfHeight,
                Density = authoring.Density,
                Friction = authoring.Friction,
                Restitution = authoring.Restitution,
            });
        }
    }
}
