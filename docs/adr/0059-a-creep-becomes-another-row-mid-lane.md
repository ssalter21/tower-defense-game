# 0059 — A creep becomes another row mid-lane, ahead of the damage that triggered it

The Cursed Villager turns into the Werewolf. Until now a body was one row for as long as it existed: the
`Creep` struct carried its `UnitType` from the spawn that set it to the tick it left the map, and the state
hash folded the type once, at the spawn, on the strength of that. A creep is now allowed to stop being one row
and start being another while it is walking, and this record is what that change carries and what order it
happens in.

## What the trigger is

**The first damage that reaches a body's health.** The `becomes` column of `content/units.txt` layout 4 names
the row a body of this one turns into, and the change fires in `Match.Damage` — the one place a roll becomes
an amount — the moment a roll gets past the pools in front of health.

A shield that swallows a roll whole is **not** damage taken and changes nothing. `Match.Absorbed` spends the
granted pool and then the authored one, raw and before the matrix; if nothing is left of the roll the method
returns without touching health, and the body is still what it was. That silence is the same one the damage
floor keeps for a hit that never reached the matrix.

**The named row may not name one in its turn.** The table refuses a chain where the column is read, which is
what makes "a hit lands on a row that names a successor" and "the first damage the body takes" the same
sentence. It also keeps the termination bound a comparison of two rows rather than a walk over a graph: a
match refuses to start unless its slowest possible walker still reaches the exit inside the tick ceiling, and
`Match.RequireItArrives` now takes that bound against the slower of the two speeds and the longer of the two
deaths.

## What order it happens in, and why that is the whole of "cannot be one-shot"

Inside one call to `Match.Damage`:

1. A shot at a body that is already dying, already gone, or never existed is discarded, exactly as before.
2. `Absorbed` spends the shield. If nothing got past, the call returns.
3. **The change resolves.** The body becomes the row its row names.
4. `Resolved` runs the roll through the matrix — **the new row's** armour type and armour value.
5. The amount comes off health, and the death check runs against **the new row's** pool.

**The change is ahead of the damage, not after it and not clamped against it.** Three readings were available
and this is the one taken:

- *Damage first, then transform, health carried as a share.* Rejected: a lethal first hit kills the Villager
  before there is anything to transform, so "cannot be one-shot" would have to be a clamp — an arbitrary rule
  leaving the body on one point of health so that the transformation has somebody to happen to. A guarantee
  bolted on beside the mechanic rather than falling out of it.
- *Damage first, clamped to leave a point, then transform.* Rejected for the same reason with an extra
  invented number in it: one health is a number nobody signed, and it makes the Werewolf's entering pool a
  function of how hard it was hit, which is the opposite of what `docs/roster.md` signs.
- *Transform first, then the damage lands on what is now standing there.* **Taken.** "Cannot be one-shot" is
  then not a rule at all — it is arithmetic. The row that named a successor is already gone when the death
  check runs, so no hit of any size can kill it. There is nothing to clamp and no special case to remember,
  and the sentence `docs/roster.md` signs — *the change resolves ahead of the death* — is implemented as
  written.

The consequence a reader should know: **the shot that triggers the change resolves against the new body.** A
shooter that acquired a Cursed Villager lands its roll on a Werewolf's hide and against a Werewolf's pool. The
alternative — resolve against the old row, then swap — would mean the tick loop damaging a body that no longer
exists, and the matrix is looked up exactly once per landing (ADR-0033).

## What carries over

| Carries | Does not |
|---|---|
| The entity id — it is the same body, so nothing aimed at it loses its target | — |
| Distance along the route and lateral offset | — |
| The wave order that released it, which is how a leak is priced | — |
| Every effect standing on it: magnitudes, expiries and the granted pool | — |
| What is left of the authored shield, raw | The new row's own authored `shield`, which is not granted |
| Health, as a **share of the pool** | Health as a number |
| — | The step per tick, which is re-derived from the new row's speed under the effects in force |
| — | The aura counter, which is put back to zero so the new row pulses on its own clock |

**Health carries as a share and never as a number**, because two rows have two pools and a raw carry-over
would be a fraction of one pool read as a fraction of the other. The share is integer arithmetic —
`hp * newMaxHp / oldMaxHp`, floored — and floored to at least one, because a body that changed row is a body
the change did not kill. For a trigger that is the first damage taken the share is always exactly one, so the
Werewolf enters on its full 2600; the share is what makes the rule true of a pair the roster has not authored
yet as well as of the pair it has.

**The authored shield is not granted**, because a pool arrives when a body spawns and this body did not spawn.
Neither of the two rows on this roster carries one, so the rule is stated here rather than measured.

## What the drawn body is: snapshot state, and the event beside it

**Which row a creep is is a field of the snapshot**, and it always was —
`CreepSnapshot.TypeId`. Nothing was added there. That is the whole reason the fourth acceptance criterion is
satisfied by doing nothing: a seek re-simulates and subscribes nobody (ADR-0026), so a creep scrubbed back
across the tick it changed on would be right in the simulation and wrong on screen if the body it draws as
were an event. It is a picture, not a moment — the same argument ADR-0007 makes for what is *on* a unit.

**There is a `CreepTransformed` event as well, and it is decorative in the ADR-0008 sense**: it names an
entity id and a value read off the row, it carries no position, and a subscribed match produces the same
rolling hash as a silent one. It exists so a view can mark the moment. `MatchDecorations` draws nothing for
it today, for the reason `CreepLeaked` draws nothing: a puff or a flash at the change is an art decision, and
picking one unattended is not a thing this project does.

`EntityViewPool` is the one view seam that had to move. It kept a variant per live id and threw when an id
came back as a different one — "an entity does not change what it is mid-match". That is now false, so the
throw is a swap: the old view goes back on its own idle stack and one of the new variant takes its place,
which is the same subtraction the rest of that class runs on. Nothing there knows which swaps are legal; the
simulation refuses the illegal ones where the column is read.

## What it costs

**Two retirements, and both are deliberate.**

`content/units.txt` goes to **layout 4** under the hash label `unit-types/4`, so every record stamped against
the roster as it stood is retired. Layouts 1, 2 and 3 keep their own reader branches, their own labels and the
hashes they always had — `content/golden/defense-0.units` is a layout-1 table that can never be recorded
again, and it still folds to `39B848CEFDDCC9CF`.

`SimulationVersion` goes to **11**, because the tick loop's rules moved: a body can change row, the matrix is
consulted against the row it changed into, and `match-state/3` became `match-state/4` when the type id joined
the per-tick fold. Every stored record's rolling hash stops reproducing whether or not its roster authors a
transformation, which is exactly the condition that constant exists for.

**The state hash folds each creep's row every tick.** It used to fold the type once, at the spawn, into the
running value `Fold` absorbs — on the stated grounds that a creep's row cannot change. It can now, so the type
id sits beside the health and the phase in the per-creep loop. The alternative was folding the change into the
spawn value at the moment it happened, which would have left the committed match's trace untouched; it was
rejected because a fold whose per-tick loop is "the mutable state of a creep" with one mutable field routed
around it is a fold that is one edit away from being wrong.

**The price rule cannot see the pair.** Creep cost is effective health over 160, per row, and a Cursed
Villager is one row on the way in and another for the rest of the corridor. What a defense spends on one is
the Werewolf's 2860 effective health — the Villager's own 1800 is never touched, because the change resolves
ahead of the damage — so 11 gold buys what the rule prices at 18. The Werewolf's own 18 is a price nobody
pays, because nothing sends one. That gap is held open rather than closed, exactly as the Mage's splash and
the Vampire's shield are: cost is derived and is never a lever, and a coefficient for a transforming pair is
the sweep's to derive. **Whether the pair ought to be worth both pools is a design question and not this
one** — it means moving the trigger onto the death — and it is
[open](../open-questions.md#should-the-cursed-villager-transform-on-damage-or-on-death).

## Where it lives

- `sim/UnitType.cs` — `Becomes`, and the layout-4 fold.
- `sim/UnitTypeTable.cs` — the `becomes` column, the layout-4 branch, and `Link`, which resolves and refuses.
- `sim/Match.cs` — `Become`, its call site inside `Damage`, the successor checks in the constructor, and the
  per-tick fold.
- `sim/MatchEvents.cs` — `CreepTransformed`.
- `client/Assets/View/EntityViewPool.cs` — the variant swap.
- `content/units.txt` — layout 4, and the one row that names a successor.
