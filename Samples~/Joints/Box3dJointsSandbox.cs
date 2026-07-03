using System;
using Box3d;
using Unity.Mathematics;
using UnityEngine;

/// <summary>Joint sandbox: a chain of capsule links on spherical joints plus a ball hanging on a
/// distance joint, both pushed sideways at start so they visibly swing. Verifies joint creation,
/// defs, and the typed joint API in a running scene.</summary>
public class Box3dJointsSandbox : MonoBehaviour
{
    [SerializeField, Tooltip("Number of chain links.")]
    private int LinkCount = 10;

    [SerializeField, Tooltip("Length of one chain link in meters.")]
    private float LinkLength = 0.5f;

    [SerializeField, Tooltip("Capsule radius of a chain link.")]
    private float LinkRadius = 0.12f;

    [SerializeField, Tooltip("Sideways start velocity applied to the chain tip and the ball.")]
    private float InitialPush = 6f;

    [SerializeField, Tooltip("Draw the box3d debug visualization (shapes + joints) as lines.")]
    private bool DebugDraw = false;

    private World _world;
    private Transform[] _visuals;
    private LineRenderer _rope;

    private void Start()
    {
        _world = World.Create(WorldDef.Default);
        _visuals = new Transform[LinkCount + 1];

        Body chainTip = CreateChain();
        CreateHangingBall(chainTip);
    }

    private Body CreateChain()
    {
        float topY = LinkCount * LinkLength + 2f;
        float halfLink = LinkLength * 0.5f;

        BodyDef anchorDef = BodyDef.Default;
        anchorDef.Position = new float3(0f, topY, 0f);
        Body previous = _world.CreateBody(anchorDef);
        float previousHalf = 0f;

        for (int i = 0; i < LinkCount; i++)
        {
            float y = topY - halfLink - i * LinkLength;

            BodyDef linkDef = BodyDef.Default;
            linkDef.Type = BodyType.Dynamic;
            linkDef.Position = new float3(0f, y, 0f);
            linkDef.UserData = (IntPtr)i;
            Body link = _world.CreateBody(linkDef);

            var capsule = new Capsule
            {
                Center1 = new float3(0f, -(halfLink - LinkRadius), 0f),
                Center2 = new float3(0f, halfLink - LinkRadius, 0f),
                Radius = LinkRadius,
            };
            link.CreateCapsuleShape(ShapeDef.Default, capsule);

            SphericalJointDef jointDef = SphericalJointDef.Default;
            jointDef.Base.BodyIdA = previous.Id;
            jointDef.Base.BodyIdB = link.Id;
            jointDef.Base.LocalFrameA = new B3Transform { Position = new float3(0f, -previousHalf, 0f), Rotation = quaternion.identity };
            jointDef.Base.LocalFrameB = new B3Transform { Position = new float3(0f, halfLink, 0f), Rotation = quaternion.identity };
            _world.CreateSphericalJoint(jointDef);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = $"Link {i}";
            visual.transform.localScale = new Vector3(LinkRadius * 2f, halfLink, LinkRadius * 2f);
            Destroy(visual.GetComponent<Collider>());
            _visuals[i] = visual.transform;

            previous = link;
            previousHalf = halfLink;
        }

        previous.LinearVelocity = new float3(InitialPush, 0f, 0f);
        return previous;
    }

    private void CreateHangingBall(Body chainTip)
    {
        const float ropeLength = 1.5f;
        float halfLink = LinkLength * 0.5f;
        float3 tipBottom = chainTip.Position - new float3(0f, halfLink, 0f);

        BodyDef ballDef = BodyDef.Default;
        ballDef.Type = BodyType.Dynamic;
        ballDef.Position = tipBottom - new float3(0f, ropeLength, 0f);
        ballDef.UserData = (IntPtr)LinkCount;
        Body ball = _world.CreateBody(ballDef);
        ball.CreateSphereShape(ShapeDef.Default, new Sphere { Radius = 0.3f });

        DistanceJointDef jointDef = DistanceJointDef.Default;
        jointDef.Base.BodyIdA = chainTip.Id;
        jointDef.Base.BodyIdB = ball.Id;
        jointDef.Base.LocalFrameA = new B3Transform { Position = new float3(0f, -halfLink, 0f), Rotation = quaternion.identity };
        jointDef.Length = ropeLength;
        _world.CreateDistanceJoint(jointDef);

        ball.LinearVelocity = new float3(InitialPush, 0f, 0f);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Hanging Ball";
        visual.transform.localScale = Vector3.one * 0.6f;
        Destroy(visual.GetComponent<Collider>());
        _visuals[LinkCount] = visual.transform;

        _rope = RopeLine.Create();
    }

    private void FixedUpdate()
    {
        _world.Step(Time.fixedDeltaTime);

        foreach (BodyMoveEvent moveEvent in _world.GetBodyMoveEvents())
        {
            int index = (int)moveEvent.UserData;
            _visuals[index].SetPositionAndRotation(moveEvent.Transform.Position, moveEvent.Transform.Rotation);
        }

        if (_rope)
        {
            Transform tip = _visuals[LinkCount - 1];
            _rope.SetPosition(0, tip.TransformPoint(0f, -1f, 0f));
            _rope.SetPosition(1, _visuals[LinkCount].position);
        }
    }

    private void Update()
    {
        if (DebugDraw && _world.IsValid)
        {
            _world.DrawDebug(DebugDrawFlags.Shapes | DebugDrawFlags.Joints | DebugDrawFlags.JointExtras);
        }
    }

    private void OnDestroy()
    {
        if (_world.IsValid) _world.Destroy();
    }
}
