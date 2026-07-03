# Joints

All nine box3d joint types are available, each created from a def (always from `.Default`) with
the two bodies and their local joint frames:

| Type | Create | What it does |
|---|---|---|
| `DistanceJoint` | `world.CreateDistanceJoint` | keeps two anchors at a length; rigid rod by default, optional spring/limits/motor |
| `MotorJoint` | `world.CreateMotorJoint` | drives relative position/velocity — the "mouse joint" for dragging |
| `Joint` (filter) | `world.CreateFilterJoint` | no constraint; just disables collision between two bodies |
| `ParallelJoint` | `world.CreateParallelJoint` | spring-aligns two frames' z-axes (keep upright / anti-roll) |
| `PrismaticJoint` | `world.CreatePrismaticJoint` | slide along one axis, rotation locked |
| `RevoluteJoint` | `world.CreateRevoluteJoint` | hinge with limits, spring, motor |
| `SphericalJoint` | `world.CreateSphericalJoint` | ball-socket with cone + twist limits — the ragdoll joint |
| `WeldJoint` | `world.CreateWeldJoint` | rigid (or soft-spring) connection |
| `WheelJoint` | `world.CreateWheelJoint` | suspension spring + spin motor + steering — vehicles |

## Frames, not anchors

Where Box2D used anchor points, box3d joints connect two **frames** (position + rotation, relative
to each body's origin). The frame rotations define the joint's axes — e.g. a revolute rotates
around the frame z-axis, a prismatic slides along frame A's x-axis, a wheel's suspension runs
along frame A's x-axis. Getting the frames right is 90% of joint setup:

```csharp
RevoluteJointDef def = RevoluteJointDef.Default;
def.Base.BodyIdA = doorFrame.Id;
def.Base.BodyIdB = door.Id;
def.Base.LocalFrameA = new B3Transform { Position = hingeOnFrame, Rotation = hingeAxisRotation };
def.Base.LocalFrameB = new B3Transform { Position = hingeOnDoor,  Rotation = hingeAxisRotation };
def.EnableLimit = true;
def.LowerAngle = 0f;
def.UpperAngle = math.radians(110f);
RevoluteJoint hinge = world.CreateRevoluteJoint(def);
```

## Typed joints and the common surface

Typed joints expose their full native accessor surface (`hinge.SetMotorSpeed`, `wheel.
SetSuspensionHertz`, `spherical.SetConeLimit`, …). Every typed joint converts implicitly to
`Joint` for the shared surface (validity, destroy, wake, forces, frames, user data):

```csharp
Joint asJoint = hinge;
asJoint.WakeBodies();
asJoint.Destroy(wakeAttached: true);
```

## Dragging bodies (mouse joint pattern)

box3d has no dedicated mouse joint; the motor joint is the tool. Create it between a static
anchor body and the target, then move the target frame each frame:

```csharp
MotorJointDef def = MotorJointDef.Default;
def.Base.BodyIdA = staticAnchor.Id;
def.Base.BodyIdB = grabbed.Id;
def.Base.LocalFrameA = new B3Transform { Position = grabPoint, Rotation = quaternion.identity };
def.LinearHertz = 5f;            // spring strength
def.LinearDampingRatio = 1f;
def.MaxSpringForce = 1000f * grabbed.GetMassData().Mass;
MotorJoint drag = world.CreateMotorJoint(def);

// per frame:
((Joint)drag).SetLocalFrameA(new B3Transform { Position = mouseWorldPoint, Rotation = quaternion.identity });
```

The *Mouse Drag* sample is exactly this, wired to a picking ray.

## Practical notes

- Limits are impulse-based: hit one at speed and it overshoots slightly for a step or two before
  settling. That's normal solver behavior, not a broken limit.
- Joint events (`world.GetJointEvents()`) fire when a joint's force/torque exceeds
  `Base.ForceThreshold`/`TorqueThreshold` — useful for breakable joints.
- `Base.CollideConnected` defaults to false (connected bodies don't collide).
- The *Joints* and *Vehicle* samples show a chain, a rope, and a complete drivable car
  (including a parallel-joint anti-roll trick borrowed from box3d's own samples).
