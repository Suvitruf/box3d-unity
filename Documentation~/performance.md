# Performance

All numbers from the *Benchmarks vs PhysX* sample scenes: identical seeded layouts simulated by
Box3D and by Unity's built-in PhysX, timed around the step call only, 600 measured steps after
warmup, editor on the same machine. Box3D ran its default **4 sub-steps per step** — arguably
doing more solver work per step than PhysX's defaults — and still posts these numbers. One
asymmetry to know: `Physics.Simulate` includes PhysX's transform write-back inside the timed call,
while Box3D's move-event sync happens outside the stopwatch — this slightly inflates the PhysX
numbers and isn't separable without under-measuring PhysX. Treat all figures as relative, not
absolute; run the sample scenes on your own hardware.

## Results

**10,000 spheres raining onto a ground box** (many independent islands):

| | avg | p99 |
|---|---|---|
| PhysX (job workers) | 6.70 ms | 8.20 ms |
| Box3D, 1 worker | 8.38 ms | 10.83 ms |
| **Box3D, 16 workers** | **1.84 ms** | **2.35 ms** |

**64 piles × 16 bodies, sleep disabled** (many medium islands under sustained load):

| | avg | p99 |
|---|---|---|
| PhysX | 1.15 ms | 1.50 ms |
| Box3D, 1 worker | 1.66 ms | 2.37 ms |
| **Box3D, 16 workers** | **0.54 ms** | **0.73 ms** |

**Joint-grid "cloth", 256 bodies / 930 joints** (one big coupled island):

| | avg | p99 |
|---|---|---|
| **PhysX** | **0.54 ms** | 0.67 ms |
| Box3D, 16 workers | 0.71 ms | 0.82 ms |

**Real-scene destruction — a town of ~10,000 bodies** (running-bond brick buildings, scattered
props, and terrain, smashed by a scripted sequence of wrecking balls — a mixed, realistic workload):

![Box3D destruction demo](https://projects.cdn.aapanasik.com/box3d-unity/destructions-box3d.gif)

| | avg | median | p95 | p99 | max |
|---|---|---|---|---|---|
| PhysX | 10.94 ms | 10.48 | 13.89 | 14.60 | 16.10 |
| **Box3D, 16 workers** | **7.67 ms** | **7.53** | **9.81** | **10.34** | **10.52** |

Identical body count (9,973) and an identical seeded town + scripted shots on both engines, so it is
directly reproducible. Box3D is ~**1.4× faster across the entire distribution**, with lower variance
— and its *slowest* step (10.5 ms) is about equal to PhysX's *median* step, so frame times stay
tighter through the collapses. At a 50 Hz fixed step (20 ms budget) Box3D uses ~38% of the budget to
PhysX's ~55%, leaving far more headroom for more bodies.

## The pattern: islands decide the speedup

Box3D parallelizes across simulation **islands** (groups of bodies connected by contacts/joints).
Its scaling in these tests: 1 island → 1.4×, 64 islands → 3.1×, thousands → 4.5× (16 workers vs 1).

Practical guidance:

- Piles, debris, crowds, projectiles, destruction — many islands — are Box3D's home turf: expect
  multiples over PhysX, with much tighter p99s (better frame pacing).
- One huge jointed mechanism (a single rope bridge to nowhere, a giant cloth grid) is the least
  favorable shape — a single island can't spread across cores. PhysX's single-pass solver can win
  there at default settings.
- Sub-steps trade speed for constraint quality. `world.Step(dt, 1)` roughly matches PhysX's
  structure; the default 4 gives visibly stiffer joints and stacks.
- Single-threaded Box3D (1 worker) is within ~25% of fully-threaded PhysX on many-island scenes —
  relevant if you reserve your cores for other work.

## Measuring yourself

The benchmark sample writes per-step CSVs (avg/median/p95/p99/min/max/stddev plus every sample) to
a `PerfResults/` folder next to `Assets/`. Editor numbers are fine for comparisons; use a player
build for anything you intend to publish.
