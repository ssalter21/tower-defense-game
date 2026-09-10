# 0054 — Height is a relationship, a radius is a sphere, and a floor is under both

Elevation enters the simulation as **two rules and a floor**, in one type, in whole-hex integer arithmetic,
evaluated per route cell at load. `Reach` holds all three; `Footing.Reaches` is still the only range test and
now asks it.

| Rule | The arithmetic | Where the level term comes from |
|---|---|---|
| **a shot** | `hexDistance × 1000 + (targetLevel − towerLevel) × 500 ≤ range` | signed — shooting down refunds, shooting up charges |
| **a radius** | `hexDistance × 1000 + \|levelDifference\| × 500 ≤ radius` | a magnitude — height only ever costs |
| **the floor** | anything with any reach reaches the hexes touching it | neither; it is above the arithmetic |

Decided in [#213](https://github.com/ssalter21/tower-defense-game/issues/213), recorded in
[the decision log](../decision-log.md#16-august-2026-later--one-format-version-and-the-map-it-is-for), built in
#215. The per-level value and the tier count are recorded together in [seam 9](../build-order.md#9--the-board),
because neither means anything without the other.

## What was decided

**Height is a relationship between two things and never a property of one.** The common shipped form of
elevation is *+1 range per level*, and it makes a tower on a cliff better at everything — including shooting
the creep standing on the ridge above it, which is the one shot its position ought to make worse. A signed
difference makes the same tower better at shooting the valley and worse at shooting the ridge, so where you
build is a read of what walks past rather than a search for the highest cell.

**Half a hex per block and not a whole one.** At 1000 an archer swings between 1.2 and 5.2 hexes across the
board's relief, and the height map dominates every other thing about a placement. At 500 the *shape* of a fold
matters more than its heights, which is what the fold is for.

**A level is half a block, and that is a re-gridding rather than a rebalance.** This originally read *half a
hex per tier*, with a tier being the whole height a tile stands: three of them, 500 milli-hex each. A tier is
now two levels of 250, so a block of climb is worth exactly what it always was and a map is ported by doubling
its levels. What the finer grid buys is not range at all — it is that the terrain can rise through a half step,
which is the granularity the tile pack was cut for and the only thing separating a hillside from a flight of
stairs. See the decision log, 29 August 2026.

**A radius is a sphere, and that is a different rule rather than the same one.** Everything with a radius —
a sweep, a blast, an aura, the Soldier's self-centred bubble — takes the magnitude of the level difference,
so height is a cost in both directions and never a refund. Reusing the signed rule would let an aura tower on
a cliff blanket the board, which is the exact failure the sphere is chosen to prevent. The two are one
expression with two level terms, because the sign is the whole of the difference between them.

**A floor guarantees adjacency, on every kind of reach.** Any tower reaches the hexes touching it whatever
the terrain does. Without it a Soldier — 1000 milli-hex, the shortest range the roster authors — standing one
tier below the creep beside him has an effective range of half a hex and cannot hit the thing he is touching.
That is not a balance outcome anybody chose; it is the arithmetic reaching a case the design never meant to
cover.

**No reach is not a short reach.** Every walking row authors zero in the range column, and the signed term
alone gives a zero-range unit two tiers up an effective range of a whole hex. A radius of zero encloses
nothing and a range of zero shoots nothing, stated once in `Reach` rather than left to every future caller.

That withdraws one thing flat arithmetic used to grant: a zero-range unit no longer reaches **the hex it
stands on**, where `0 × 1000 ≤ 0` held before. Nothing has ever asked — a tower may not stand in the corridor,
so the route walk never asks at no distance at all — and *reaches nothing* is a whole answer where *reaches
only itself* is a special case waiting to be found by whoever writes the first self-centred bubble.

**Creeps do not slow going uphill.** Considered and rejected: it is too strong a natural incentive, and it
would put the ground under a creep into the movement step, which is the tick loop.

## Why it costs nothing in the tick loop

`TowerCoverage` is where the two dimensions stop: it intersects each tower's reach with the route once, at
load, and hands the tick loop disjoint ascending intervals of route distance. **The route is fixed and every
route cell's level is as fixed as its position**, so the level term is evaluated per route cell in that same
load-time walk, exactly as flat range was.

What changes downstream is only that a tower's coverage **fragments**: a ridge crossing the corridor carves a
hole in the middle of a run of route where flat ground would not. A list of disjoint intervals is already what
that type returns, and already for a reason — a folded corridor puts two legs in range with an out-of-range
stretch between them — so the hole a ridge makes needs no new machinery and no new branch in `Covers`.

This is the property the whole seam was priced on. The alternative — a level read at use time — puts a
position in a plane back into the hottest loop in the simulation, which is what
[ADR-0016](0016-target-references-carry-no-position.md) and `TowerCoverage` between them exist to keep out.

## What it costs

**A simulation version, and every stored record with it.** `SimulationVersion` 7 → 8. Nothing about the
committed map moves — it is entirely on the ground tier, where the signed difference is identically zero — but
a map with a fold in it has been loadable since the level layer landed, and every record made on one replays
to a different outcome under this. That is the condition the constant exists for.

**The rule fingerprint needed a fold in its scenario, not a new half.** `DerivationTests` folds five halves
and every one of them resolves a match against a tower and a route, so this rule runs through all of them —
and produced version 7's number exactly, because the scenario's map was written on the flat. A fold that runs
a rule and cannot see it is the same failure as one that never runs it. The scenario's map gained a fold and
the label went to `rule-fingerprint/6`, which is the first bump there taken for the scenario rather than for
the shape of the fold.

**A map is an argument to the range test now.** `Footing.Reaches` takes the map rather than two levels,
because both levels are facts the map already holds and a caller passing them in could pass the wrong ones.
`HexMap.LevelAt` gained a by-hex overload for it, so the corridor walk does not convert back to an offset
column and row itself.

**A radius has no caller yet.** `Reach.Encloses` is public and nothing in `sim/` calls it, which is normally a
smell. It is here because the sphere is half of one decision and the half that is easiest to get wrong later:
a bubble authored against `Shoots` would be a cliff-blanketing aura discovered after it shipped. The rule is
written down, tested, and waiting for the column that
[the roster](../roster.md#what-this-roster-needs-that-the-schema-does-not-have) says is coming.

## What was rejected

**A flat bonus for standing high, `+1 range per level`.** The common form, and the one the vision said until
#213. It makes elevation a property of the tower, which makes the highest cell the best cell and the board a
lookup.

**One rule for both, signed.** Cheaper by an expression and it is the aura-on-a-cliff failure by construction.

**One rule for both, absolute.** It costs a tower for shooting downhill, which deletes the reason to build
high at all and leaves elevation as pure downside.

**No floor, on the argument that the arithmetic should be uniform.** Uniform arithmetic that cannot hit an
adjacent target is not a rule anybody would author on purpose, and the case is not rare: it is the shortest
range in the roster against a one-tier step.

**Creep speed varying with the climb.** Rejected on design grounds above, and it would have cost the tick
loop what the coverage arrangement was built to save.
