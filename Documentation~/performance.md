# Performance

All numbers from the *Benchmarks vs PhysX* sample scenes: identical seeded layouts simulated by
Box3d and by Unity's built-in PhysX, timed around the step call only, 600 measured steps after
warmup, editor on the same machine. Box3d ran its default **4 sub-steps per step** — arguably
doing more solver work per step than PhysX's defaults — and still posts these numbers. Treat all
figures as relative, not absolute; run the sample scenes on your own hardware.

## Results

**10,000 spheres raining onto a ground box** (many independent islands):

| | avg | p99 |
|---|---|---|
| PhysX (job workers) | 6.70 ms | 8.20 ms |
| Box3d, 1 worker | 8.38 ms | 10.83 ms |
| **Box3d, 16 workers** | **1.84 ms** | **2.35 ms** |

**64 piles × 16 bodies, sleep disabled** (many medium islands under sustained load):

| | avg | p99 |
|---|---|---|
| PhysX | 1.15 ms | 1.50 ms |
| Box3d, 1 worker | 1.66 ms | 2.37 ms |
| **Box3d, 16 workers** | **0.54 ms** | **0.73 ms** |

**Joint-grid "cloth", 256 bodies / 930 joints** (one big coupled island):

| | avg | p99 |
|---|---|---|
| **PhysX** | **0.54 ms** | 0.67 ms |
| Box3d, 16 workers | 0.71 ms | 0.82 ms |

## The pattern: islands decide the speedup

Box3d parallelizes across simulation **islands** (groups of bodies connected by contacts/joints).
Its scaling in these tests: 1 island → 1.4×, 64 islands → 3.1×, thousands → 4.5× (16 workers vs 1).

Practical guidance:

- Piles, debris, crowds, projectiles, destruction — many islands — are Box3d's home turf: expect
  multiples over PhysX, with much tighter p99s (better frame pacing).
- One huge jointed mechanism (a single rope bridge to nowhere, a giant cloth grid) is the least
  favorable shape — a single island can't spread across cores. PhysX's single-pass solver can win
  there at default settings.
- Sub-steps trade speed for constraint quality. `world.Step(dt, 1)` roughly matches PhysX's
  structure; the default 4 gives visibly stiffer joints and stacks.
- Single-threaded Box3d (1 worker) is within ~25% of fully-threaded PhysX on many-island scenes —
  relevant if you reserve your cores for other work.

## Measuring yourself

The benchmark sample writes per-step CSVs (avg/median/p95/p99/min/max/stddev plus every sample) to
a `PerfResults/` folder next to `Assets/`. Editor numbers are fine for comparisons; use a player
build for anything you intend to publish.
