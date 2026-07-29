# Water

`Box3DWater` simulates particle-based liquid on the GPU and renders it as a continuous
screen-space water surface. Drop it in a scene with Box3D shapes and the water flows around
them; dynamic bodies float, bob and get pushed by waves.

**Requirements**: compute-shader support, and for the built-in surface rendering a URP camera
with **Depth Texture** enabled in the pipeline asset (enable **Opaque Texture** too or the water
falls back from true refraction to plain transparency). The simulation itself is
pipeline-agnostic — with another pipeline, bind `ParticleBuffer` to your own renderer
(VFX Graph, instanced meshes, …).

## Quick start

**GameObject → Box3D → Water** creates a water object (or add the `Box3DWater` component —
Add Component → Box3D → Water). Position it, size the cyan **Volume** box (the region that fills
with water on play) and the blue **Bounds** box (the simulation region) with the scene-view
handles, and press Play. Both boxes also live in the Inspector.

The Inspector has **Fill / Splash / Clear** buttons for poking the fluid in play mode.

## How it works

Every physics step the component:

1. mirrors nearby Box3D shapes into the GPU — spheres, capsules, box hulls and
   `Box3DTerrainShape` terrains are exact (terrain height grids are uploaded once and sampled
   per particle, so water pools in valleys and runs down slopes); other hulls use their local
   bounding box (oriented with the body); mesh, compound and component-less height-field shapes
   contribute only the **top face** of their world bounding box, and only while that box is
   plate-like (up to 1 m tall) — floors and road decks catch water on top, while a tall box (a
   road spline winding uphill, a building shell) is mostly air around its mesh and is skipped
   entirely (disable **Approximate Complex Shapes** to skip every approximation). Sides and
   bottom of the plate are open on purpose: solid faces would dam water at invisible walls or
   crush a pond against the terrain running through the box. Give an object exact water
   collision with sphere / capsule / box shapes (or a `Box3DTerrainShape`);
2. runs a position-based fluid solve in a compute shader — neighbor grid, density-constraint
   iterations (incompressibility), cohesion, XSPH viscosity, then collision against the mirrored
   shapes;
3. accumulates the collision push-back per dynamic body and reads it back asynchronously,
   applying it through the Box3D impulse API together with a submersion-scaled drag. That
   coupling is what makes props float — raise **Coupling Strength** if things sink, lower it if
   they get launched, and raise **Body Drag** for calmer floating.

Rendering is screen-space: particles are splatted as sphere impostors into per-camera depth,
thickness and foam buffers — depth is smoothed with a depth-aware bilateral blur, thickness and
foam with a Gaussian, so absorption and foam read continuous fields rather than individual
particle splats — and a fullscreen pass reconstructs normals to shade the surface: refraction of
the opaque scene, Beer–Lambert absorption tinted by **Water Color**, reflection-probe/sky
reflections with a Fresnel falloff, and a main-light specular highlight.

Two effects sell contact with the world:

- **Foam** — the solver tracks whitewater per particle: aerated (under-dense) water moving fast
  churns into foam, then decays back to clear. It shows where physics says it should — waterfall
  impacts, splashes, wakes — plus a foam rim wherever an object pierces the surface, broken up
  by animated noise. One slider (**Foam**) scales the whole effect; 0 turns it off and skips the
  foam pass entirely. **Foam Stretch** elongates fast, foamy droplets along their motion (round
  impostors thin out into streaks), so whitewater reads as flying spray instead of white balls;
  0 keeps every droplet round.
- **Shore Blend** — instead of a hard depth cut, the water melts softly into any geometry it
  touches over this distance (refraction also relaxes to zero at the waterline so edges don't
  wobble). Small values give a crisp waterline, larger ones a misty fade.

## Tuning cheat-sheet

| Feel | Change |
| --- | --- |
| Finer, more detailed water | lower **Particle Radius** (cost grows fast), raise **Max Particles** |
| Thick / syrupy | raise **Viscosity**, a little **Damping** |
| Splashy / lively | lower **Viscosity**, raise **Solver Iterations** or **Substeps** for stability |
| Droplets cling and sheet | raise **Cohesion** |
| Props sink / launch | raise / lower **Coupling Strength** |
| Bobbing never settles | raise **Body Drag** |
| Murkier water | raise **Absorption**, darker **Water Color** |
| Blobby surface | raise **Smoothing** or **Particle Render Scale** |
| More / less whitewater | **Foam** (waterfalls and splashes generate it on their own) |
| Foam looks like white blobs | raise **Foam Stretch**, raise **Smoothing** |
| Waterline too hard / too misty | **Shore Blend** down / up |
| GPU cost too high | lower **Render Scale**, **Max Particles**, or **Substeps** |

## Waterfalls

`Box3DWaterfall` (GameObject → Box3D → Waterfall) is a continuous emitter: it pours particles
from a rectangular **lip** along its forward (+Z) axis — rotate the object to aim it. Out of a
cliff it's a waterfall, straight up a fountain, downward a spout; gravity shapes the stream
after launch, and selecting the emitter previews the exact flight arc in the scene view so you
can aim the landing spot before pressing Play.

It pours into the scene's `Box3DWater` (or an explicitly assigned one) and recycles that
water's **oldest** particles once the pool is full, so a stream runs forever on a fixed budget.
Size the budget with the steady-state rule:

> particles needed ≈ **Flow Rate** × seconds a particle survives (flight + time in the basin)

If the water's **Max Particles** is below that, the stream stays alive by eating the basin.
For open-world falls, turn the water's **Contain** off so particles drain below the bounds and
free their slots; make the bounds tall enough to cover the drop.

**Flow Rate** sets density, **Speed** sets reach, **Spread** breaks the sheet into natural
ropes and spray. Open and close the tap from code with `waterfall.IsFlowing = true/false`
(or the Inspector's Start/Stop Flow button in play mode).

## Scripting

```csharp
using Box3D.Hybrid;

var water = FindAnyObjectByType<Box3DWater>();

// A bucket of water thrown from a point — rain, hose jets, potion spills:
water.SpawnParticles(center: muzzle.position, radius: 0.4f,
                     velocity: muzzle.forward * 8f, count: 1024);

water.Fill();   // reset to a full volume
water.Clear();  // remove all water

// Custom rendering: float4 per particle — xyz world position, w = 1 alive / 0 dead.
ComputeBuffer particles = water.ParticleBuffer;
// Velocities: float4 — xyz velocity, w = foam amount in [0, 1].
ComputeBuffer velocities = water.VelocityBuffer;
int range = water.ActiveParticleRange;
```

`SpawnParticles` recycles the oldest particles once the pool (**Max Particles**) is full, so
emitters can run forever. For custom emitter shapes there is a batched overload —
`SpawnParticles(float4[] positions, float4[] velocities, int count)` with positions as
`float4(world xyz, 1)` — which is what `Box3DWaterfall` uses under the hood.

## Wind

A `Box3DWind` whose zone overlaps the water blows on it: fluid inside the zone gets
**Strength × Water Influence** (a slider on the wind) as an acceleration, gusting with the same
Perlin noise the rigid bodies feel. Wind grips the *surface*, not the bulk — the acceleration is
full on under-dense particles (the surface layer, spray) and on foam, and fades to nothing in the
incompressible interior, so a steady breeze drifts the top layer into waves against the far shore
instead of shoving the whole pool like a current. Set **Water Influence** to 0 to keep a wind
rigid-body-only. From code, any external system can drive the same input each `FixedUpdate` via
`Box3DWater.AddWind(center, rotation, halfExtents, acceleration)` (up to 8 volumes per step).

## Limits & notes

- **Particle Radius** defines the fluid's rest state (kernel reach, particle mass and spacing),
  so editing it in play mode re-seeds the water — a refill when **Fill On Start** is on,
  otherwise a clear — instead of letting the old arrangement violently re-relax. Set it before
  play for normal use; every other slider tunes live.

- One `Box3DWater` = one fluid body; several can coexist (each simulates and renders
  independently, and they don't interact with each other).
- With **Contain** off, water below the bounds floor is removed (open-world spills); its pool
  slots are reused by later `SpawnParticles` calls, not by `Fill`.
- Water in reflection-probe captures and camera previews is skipped by design.
- The surface composites after opaques near the front of the transparent queue: transparent
  objects *behind* the surface won't be visible through it in refraction mode.
- Two-way coupling reads back one small buffer per step (a few KB) with one-step latency; the
  per-body budget is 128 dynamic bodies and 256 shapes near the water bounds.
