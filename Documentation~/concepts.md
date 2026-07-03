# Core concepts

Four rules underpin the whole API. Everything else is detail.

## 1. Everything is a small value handle

`World`, `Body`, `Shape`, `Joint` (and the typed joints) are tiny structs wrapping generation-
validated ids — not classes, not pointers. Copy them freely, store them in fields, put them in
collections; they are 4–12 bytes and never allocate.

When the underlying object is destroyed, existing handles become *stale*, not dangerous: using a
stale handle is detected by the engine. Check `IsValid` when in doubt (for example, on ids arriving
from end-touch events, which may reference already-destroyed shapes).

```csharp
Body body = world.CreateBody(def);
BodyId id = body.Id;              // persistable: ToUInt64()/FromUInt64() for dictionaries
body.Destroy();
Debug.Log(new Body { Id = id }.IsValid); // false — stale, safely detectable
```

Destruction is explicit (`Destroy()`); there are no finalizers. Destroying a world destroys
everything in it.

## 2. Defs come from `.Default`, always

Every creation parameter struct (`WorldDef`, `BodyDef`, `ShapeDef`, the joint defs,
`ExplosionDef`) carries a hidden validation cookie set by the engine's factory function.
**Never `new` or `default` a def** — start from the `Default` property, then mutate:

```csharp
BodyDef def = BodyDef.Default;   // correct: engine-initialized
def.Type = BodyType.Dynamic;

BodyDef broken = new BodyDef();  // WRONG: engine rejects it (missing internal cookie)
```

This is the single most common mistake; the engine asserts loudly when it happens.

## 3. Geometry lifetime: hulls are cloned, meshes are referenced

- `BoxHull` (stack value) and `Hull` (native allocation) are **cloned** into the world when a
  shape is created — you may destroy the source immediately afterwards.
- `TriangleMesh`, `HeightField`, and `Compound` data is **referenced** by shapes — it must stay
  alive until every shape using it is destroyed. Destroy the world (or the shapes) first, the
  geometry second.

See [Shapes & geometry](shapes-and-geometry.md) for the full story.

## 4. Event data is transient

Event getters (`GetBodyMoveEvents`, `GetContactEvents`, …) return spans over engine-internal
memory that is valid **only until the next `Step` or world mutation**. Consume immediately; copy
anything you need to keep. The contact/sensor bundles are `ref struct`s, so the compiler itself
prevents you from storing them.

## Conventions worth knowing

- **Math types** are `Unity.Mathematics` (`float3`, `quaternion`) — they convert implicitly to and
  from `Vector3`/`Quaternion`. `b3Quat` and Unity quaternions share the same memory layout.
- **Units** are meters/kilograms/seconds, angles in radians. Gravity is a free vector
  (`(0, -10, 0)` by default) — box3d has no built-in up axis.
- **Method names match the native API 1:1** minus the prefix (`body.GetLinearVelocity()` is
  `b3Body_GetLinearVelocity`), so box3d's own documentation applies directly. A curated set of
  properties (`body.Position`, `body.LinearVelocity`, `world.IsValid`, …) covers the hot paths.
- `Box3d.Joint` collides with `UnityEngine.Joint` in files importing both namespaces — add
  `using Joint = Box3d.Joint;` or qualify. (The typed joints — `RevoluteJoint` etc. — don't clash
  and are what you hold most of the time.)
- Determinism is a box3d design goal (cross-platform deterministic math). Same inputs, same steps →
  same results, which makes it a candidate for lockstep networking.
