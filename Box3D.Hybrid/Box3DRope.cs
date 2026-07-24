using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Box3D.Hybrid
{
    /// <summary>A physics rope/cable, modeled on Source 2's Hammer cables: stretch it between this
    /// object and <see cref="EndPoint"/>, watch it hang live in the editor, then either **bake**
    /// the settled curve (a static cable — cheap, optional static collision) or leave it
    /// **dynamic** (a chain of capsule bodies linked by ball joints, built at runtime; ends attach
    /// to any <see cref="Box3DBody"/> found at the endpoints, so it swings, drapes and can be
    /// knocked around). Rendering goes through the required LineRenderer, which follows the
    /// simulation every frame.</summary>
    [Icon("Packages/com.suvitruf.box3d/Box3D.Hybrid.Editor/Icons/Box3DRope.png")]
    [AddComponentMenu("Box3D/Rope")]
    [RequireComponent(typeof(LineRenderer))]
    [DisallowMultipleComponent]
    public class Box3DRope : MonoBehaviour
    {
        public enum RopeMode
        {
            /// <summary>Simulated at runtime: capsule segment bodies + ball joints.</summary>
            Dynamic,
            /// <summary>Frozen curve: no simulation. Uses the baked points (or settles once at
            /// startup if never baked), with optional static collision.</summary>
            Baked,
        }

        [SerializeField, Tooltip("Dynamic: simulated at runtime (segments + joints). Baked: a frozen curve with optional static collision — bake it in the editor, or it settles once at startup.")]
        private RopeMode Mode = RopeMode.Dynamic;

        [SerializeField, Tooltip("The far end of the rope. Leave empty to use End Offset instead (drag its handle in the Scene view).")]
        private Transform EndPoint;

        [SerializeField, Tooltip("Far end in this object's local space, used when End Point is empty.")]
        private Vector3 EndOffset = new Vector3(3f, 0f, 0f);

        [SerializeField, Range(2, 100), Tooltip("Simulation segments. More = smoother drape, more bodies when Dynamic.")]
        private int Segments = 20;

        [SerializeField, Min(0f), Tooltip("Extra length as a fraction of the straight distance (0 = taut, 0.15 = 15% sag).")]
        private float Slack = 0.15f;

        [SerializeField, Min(0.005f), Tooltip("Rope radius in meters — collision thickness of the segments.")]
        private float Radius = 0.03f;

        [SerializeField, Min(1f), Tooltip("Rope density in kg/m³ (Dynamic mode).")]
        private float Density = 500f;

        [SerializeField, Range(0f, 1f), Tooltip("Segment friction — how much the rope grips what it touches or rests on.")]
        private float Friction = 0.6f;

        [SerializeField, Tooltip("Attach the first segment to a Box3DBody on this object or its parents (otherwise pinned to the world).")]
        private bool AttachStartToBody = true;

        [SerializeField, Tooltip("Attach the last segment to a Box3DBody on End Point or its parents (otherwise pinned to the world).")]
        private bool AttachEndToBody = true;

        [SerializeField, Tooltip("Let rope segments collide with the bodies the ends are attached to. Off (default): the rope ignores its attached bodies — the anchors sit at their surface, and contacts there fight the joints.")]
        private bool CollideWithAttached;

        [SerializeField, Tooltip("Baked mode: add static capsule collision along the curve.")]
        private bool BakedCollision = true;

        [SerializeField, HideInInspector]
        private Vector3[] BakedPoints; // local space, captured by the editor's Bake button

        private Box3DWorld _world;
        private Body[] _segments;
        private Joint[] _joints;
        private Body _startPin;    // static pin bodies, created only for unattached ends
        private Body _endPin;
        private Body _bakedBody;   // one static body carrying the baked collision capsules
        private float _halfSegment;
        private LineRenderer _line;
        private Vector3[] _renderPoints;

        internal RopeMode CurrentMode => Mode;
        internal bool HasBake => BakedPoints != null && BakedPoints.Length > 1;
        internal int SegmentCount => Segments;
        internal float RopeRadius => Radius;
        internal bool CollidesWithAttached => CollideWithAttached;

        internal Box3DBody FindStartAttachment()
        {
            return AttachStartToBody ? GetComponentInParent<Box3DBody>() : null;
        }

        internal Box3DBody FindEndAttachment()
        {
            return AttachEndToBody && EndPoint ? EndPoint.GetComponentInParent<Box3DBody>() : null;
        }

        internal Vector3 StartWorld => transform.position;

        internal Vector3 EndWorld => EndPoint ? EndPoint.position : transform.TransformPoint(EndOffset);

        private void Start()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;

            // Start (not Awake) so every Box3DBody the ends may attach to already exists.
            if (Mode == RopeMode.Baked) BuildBaked();
            else BuildDynamic();
        }

        private void BuildBaked()
        {
            Vector3[] points = HasBake ? BakedToWorld() : ComputeSettledPoints();
            ApplyToLine(points);
            if (!BakedCollision || points.Length < 2) return;

            _world = Box3DWorld.Instance;
            _bakedBody = _world.World.CreateBody(BodyDef.Default); // static, identity — capsules in world coords
            ShapeDef def = SegmentShapeDef();
            for (int i = 0; i < points.Length - 1; i++)
            {
                _bakedBody.CreateCapsuleShape(def, new Capsule
                {
                    Center1 = points[i],
                    Center2 = points[i + 1],
                    Radius = Radius,
                });
            }
        }

        private void BuildDynamic()
        {
            _world = Box3DWorld.Instance;

            // Spawn taut along the straight line between the ends. The slack lives in the joint
            // rest lengths, so the rope sags into place under gravity and drapes onto whatever it
            // meets — a pre-settled spawn would thread segments through scene geometry, because
            // the verlet preview can't see Box3D shapes.
            var nodes = new Vector3[Segments + 1];
            for (int i = 0; i <= Segments; i++)
            {
                nodes[i] = Vector3.Lerp(StartWorld, EndWorld, (float)i / Segments);
            }

            float restLength = Vector3.Distance(StartWorld, EndWorld) * (1f + Slack) / Segments;
            _halfSegment = restLength * 0.5f;

            _segments = new Body[Segments];
            for (int i = 0; i < Segments; i++)
            {
                float3 a = nodes[i];
                float3 b = nodes[i + 1];
                float3 dir = math.normalizesafe(b - a, new float3(0f, 0f, 1f));

                BodyDef bodyDef = BodyDef.Default;
                bodyDef.Type = Box3D.BodyType.Dynamic;
                bodyDef.Position = (a + b) * 0.5f;
                bodyDef.Rotation = quaternion.LookRotationSafe(dir, new float3(0f, 1f, 0f));
                bodyDef.LinearDamping = 0.1f;
                bodyDef.AngularDamping = 0.5f;
                // Thin, fast-moving segments tunnel through other objects without continuous
                // collision — bullet mode keeps the rope reacting to everything it sweeps past.
                bodyDef.IsBullet = true;
                _segments[i] = _world.World.CreateBody(bodyDef);

                ShapeDef shapeDef = SegmentShapeDef();
                float cap = Mathf.Max(0.001f, _halfSegment - Radius);
                _segments[i].CreateCapsuleShape(shapeDef, new Capsule
                {
                    Center1 = new float3(0f, 0f, -cap),
                    Center2 = new float3(0f, 0f, cap),
                    Radius = Radius,
                });
            }

            var joints = new List<Joint>(Segments + 1);

            // Chain: each shared node links the +Z tip of one segment to the -Z tip of the next.
            for (int i = 1; i < Segments; i++)
            {
                joints.Add(Link(_segments[i - 1], new float3(0f, 0f, _halfSegment),
                                _segments[i], new float3(0f, 0f, -_halfSegment)));
            }

            // Ends: a Box3DBody at the endpoint (rope follows/pulls it), else a static world pin.
            Box3DBody startBody = FindStartAttachment();
            Box3DBody endBody = FindEndAttachment();
            joints.Add(AttachEnd(_segments[0], new float3(0f, 0f, -_halfSegment), nodes[0], startBody, ref _startPin));
            joints.Add(AttachEnd(_segments[Segments - 1], new float3(0f, 0f, _halfSegment), nodes[Segments], endBody, ref _endPin));

            // The attach joints already skip their own pair (collide-connected is off), but the
            // rope's other segments would still hit the attached body — and the near-anchor ones
            // spawn overlapping it, which fights the joints. Filter the whole rope against each
            // attached body unless collision was explicitly requested.
            if (!CollideWithAttached)
            {
                if (startBody) FilterAgainst(joints, startBody.Body, _segments[0]);
                if (endBody) FilterAgainst(joints, endBody.Body, _segments[Segments - 1]);
            }

            _joints = joints.ToArray();
            _renderPoints = new Vector3[Segments + 1];
        }

        // Same material/filter convention as Box3DShape: category from the GameObject's layer,
        // mask from the layer collision matrix — so ropes honor Project Settings → Physics.
        internal ShapeDef SegmentShapeDef()
        {
            ShapeDef def = ShapeDef.Default;
            def.Density = Density;
            def.BaseMaterial.Friction = Friction;
            def.Filter.CategoryBits = 1UL << gameObject.layer;
            def.Filter.MaskBits = Box3DShape.CollisionMaskForLayer(gameObject.layer);
            return def;
        }

        private void FilterAgainst(List<Joint> joints, Body attached, Body alreadyJointed)
        {
            foreach (Body segment in _segments)
            {
                if (segment.Equals(alreadyJointed)) continue;
                FilterJointDef def = FilterJointDef.Default;
                def.Base.BodyIdA = attached.Id;
                def.Base.BodyIdB = segment.Id;
                joints.Add(_world.World.CreateFilterJoint(def));
            }
        }

        private Joint Link(Body a, float3 localA, Body b, float3 localB)
        {
            SphericalJointDef def = SphericalJointDef.Default;
            def.Base.BodyIdA = a.Id;
            def.Base.BodyIdB = b.Id;
            def.Base.LocalFrameA = new B3Transform { Position = localA, Rotation = quaternion.identity };
            def.Base.LocalFrameB = new B3Transform { Position = localB, Rotation = quaternion.identity };
            return _world.World.CreateSphericalJoint(def);
        }

        private Joint AttachEnd(Body segment, float3 segmentLocal, float3 worldNode, Box3DBody attach, ref Body pin)
        {
            Body other;
            float3 otherLocal;
            if (attach)
            {
                // Node in the attached body's frame (bodies have no scale — pose math, not
                // Transform.InverseTransformPoint, which would bake lossyScale in).
                other = attach.Body;
                otherLocal = math.mul(math.inverse(other.Rotation), worldNode - other.Position);
            }
            else
            {
                BodyDef pinDef = BodyDef.Default; // static
                pinDef.Position = worldNode;
                pin = _world.World.CreateBody(pinDef);
                other = pin;
                otherLocal = float3.zero;
            }
            return Link(other, otherLocal, segment, segmentLocal);
        }

        private void LateUpdate()
        {
            if (_segments == null) return;

            // _segments.Length, not Segments: the serialized field can be edited during play,
            // but the built chain keeps its creation-time size.
            int count = _segments.Length;
            for (int i = 0; i < count; i++)
            {
                _renderPoints[i] = SegmentTip(_segments[i], -_halfSegment);
            }
            _renderPoints[count] = SegmentTip(_segments[count - 1], _halfSegment);
            ApplyToLine(_renderPoints);
        }

        private static Vector3 SegmentTip(Body body, float alongZ)
        {
            return (Vector3)(body.Position + math.mul(body.Rotation, new float3(0f, 0f, alongZ)));
        }

        private void OnDestroy()
        {
            if (_joints != null)
            {
                foreach (Joint joint in _joints)
                {
                    if (joint.IsValid) joint.Destroy();
                }
            }
            if (_segments != null)
            {
                foreach (Body body in _segments)
                {
                    if (body.IsValid) body.Destroy();
                }
            }
            if (_startPin.IsValid) _startPin.Destroy();
            if (_endPin.IsValid) _endPin.Destroy();
            if (_bakedBody.IsValid) _bakedBody.Destroy();
        }

        // --- shared with the editor (preview, bake) ---

        /// <summary>The rope's free-space hang: a verlet relaxation between the current endpoints
        /// under the world's gravity, ignoring scene collision. Used for the editor preview and
        /// for baking — dynamic ropes spawn taut instead and drape via real physics.</summary>
        internal Vector3[] ComputeSettledPoints()
        {
            var points = new Vector3[Segments + 1];
            SettleCurve(points, StartWorld, EndWorld,
                Vector3.Distance(StartWorld, EndWorld) * (1f + Slack) / Segments, SceneGravity(), 240);
            return points;
        }

        internal Vector3 SceneGravity()
        {
            var world = Application.isPlaying && _world ? _world : FindAnyObjectByType<Box3DWorld>();
            return world ? world.GravityVector : new Vector3(0f, -9.81f, 0f);
        }

        internal float SettledSegmentLength()
        {
            return Vector3.Distance(StartWorld, EndWorld) * (1f + Slack) / Segments;
        }

        internal void ApplyToLine(Vector3[] worldPoints)
        {
            if (!_line) _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.widthMultiplier = Radius * 2f; // the visual is exactly as thick as the collision
            _line.positionCount = worldPoints.Length;
            _line.SetPositions(worldPoints);
        }

        internal void Bake(Vector3[] worldPoints)
        {
            BakedPoints = new Vector3[worldPoints.Length];
            for (int i = 0; i < worldPoints.Length; i++)
            {
                BakedPoints[i] = transform.InverseTransformPoint(worldPoints[i]);
            }
            Mode = RopeMode.Baked;
        }

        internal void ClearBake()
        {
            BakedPoints = null;
            Mode = RopeMode.Dynamic;
        }

        internal Vector3[] BakedToWorld()
        {
            var points = new Vector3[BakedPoints.Length];
            for (int i = 0; i < BakedPoints.Length; i++)
            {
                points[i] = transform.TransformPoint(BakedPoints[i]);
            }
            return points;
        }

        /// <summary>One verlet relaxation: pinned ends, distance constraints, gravity. Deterministic
        /// — the same inputs settle to the same curve, in the editor and at runtime.</summary>
        internal static void SettleCurve(Vector3[] nodes, Vector3 start, Vector3 end,
            float segmentLength, Vector3 gravity, int steps)
        {
            int last = nodes.Length - 1;
            var previous = new Vector3[nodes.Length];
            for (int i = 0; i <= last; i++)
            {
                nodes[i] = previous[i] = Vector3.Lerp(start, end, (float)i / last);
            }

            const float dt = 1f / 60f;
            for (int step = 0; step < steps; step++)
            {
                StepCurve(nodes, previous, start, end, segmentLength, gravity, dt);
            }
        }

        /// <summary>A single verlet step (integration + constraints) — the editor's animated
        /// preview calls this directly.</summary>
        internal static void StepCurve(Vector3[] nodes, Vector3[] previous, Vector3 start, Vector3 end,
            float segmentLength, Vector3 gravity, float dt)
        {
            int last = nodes.Length - 1;
            for (int i = 1; i < last; i++)
            {
                Vector3 velocity = (nodes[i] - previous[i]) * 0.96f;
                previous[i] = nodes[i];
                nodes[i] += velocity + gravity * (dt * dt);
            }
            nodes[0] = start;
            nodes[last] = end;

            for (int iteration = 0; iteration < 6; iteration++)
            {
                for (int i = 0; i < last; i++)
                {
                    Vector3 delta = nodes[i + 1] - nodes[i];
                    float distance = delta.magnitude;
                    if (distance < 1e-6f) continue;
                    Vector3 correction = delta * ((distance - segmentLength) / distance * 0.5f);
                    if (i != 0) nodes[i] += correction;
                    if (i + 1 != last) nodes[i + 1] -= correction;
                }
                nodes[0] = start;
                nodes[last] = end;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.47f, 0.86f, 0.47f, 0.9f);
            Gizmos.DrawWireSphere(StartWorld, Radius * 2f);
            Gizmos.DrawWireSphere(EndWorld, Radius * 2f);
        }
    }
}
