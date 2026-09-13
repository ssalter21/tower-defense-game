# 0059 — A creep becomes another row mid-lane, ahead of the damage that triggered it

**Decided.** A body may stop being one row and start being another while it walks. `content/units.txt` layout
4 adds `becomes`, naming the successor row, and the change fires in `Match.Damage` — the one place a roll
becomes an amount — on **the first damage that reaches health**. A roll a shield swallows whole is not damage
taken. The named row must walk, must have a pool, and may not name a successor in its turn, refused where the
column is read; that keeps the termination bound a comparison of two rows (`RequireItArrives` takes the slower
speed and the longer death).

- **Order inside one `Damage` call:** discard a shot at a dying or gone body; spend the shield, and return if
  nothing got past; **the change resolves**; the roll goes through the matrix against the **new** row's armour;
  the amount comes off the **new** row's pool. "Cannot be one-shot" is therefore arithmetic, not a clamp: the
  row that named a successor is gone before the death check runs, and the triggering shot lands on the
  successor.
- **What carries:** the entity id, distance and lateral offset, the wave order (how a leak is priced), every
  effect standing on it, what is left of the authored shield, and health **as a share of the pool** floored to
  at least one — for a first-damage trigger the share is always one, so the Werewolf enters on its full 2600.
  **What does not:** health as a number, the successor's own authored shield (a pool arrives at a spawn and
  this body did not spawn), the step (re-derived under the effects in force), the aura counter (reset).
- **Which row a creep is was already a snapshot field**, `CreepSnapshot.TypeId`, so a scrub across the change
  is right by construction ([ADR-0026](0026-seeking-re-simulates-rather-than-caching.md)). `CreepTransformed`
  is a decorative event ([ADR-0008](0008-match-events-are-decorative.md)); `MatchDecorations` draws nothing
  for it, because a puff at the change is an art decision. `EntityViewPool` swaps the variant instead of
  throwing.

**Cost.** `unit-types/4`, `SimulationVersion` 11, `match-state/3` → `/4`: the state hash now folds each
creep's row **every tick** beside health and phase, where it folded the type once at the spawn. Every stored
record is retired. The price rule cannot see the pair — 11 gold buys the Werewolf's 2860, priced at 18 — and
the gap is held open with the Mage's splash and the Vampire's shield
([ADR-0063](0063-a-price-is-derived-from-the-row-and-its-silences-are-named.md)). Whether the pair should be
worth both pools meant moving the trigger onto the death; Sam kept it on the first damage
([the decision log](../decision-log/2026-09.md#12-september-2026-later--the-villagers-trigger-stays-on-the-first-damage-and-the-pair-is-worth-what-that-trigger-makes-it)).

**Rejected.** Damage first, then transform with health carried as a share (a lethal first hit kills the
Villager before there is anything to transform, so the guarantee becomes a bolted-on clamp). Damage first,
clamped to leave one point (an unsigned number, and the Werewolf's entering pool a function of how hard it was
hit). Folding the change into the spawn value to leave the committed trace untouched (a per-tick fold with
one mutable field routed around it is one edit from wrong).

Lives in `sim/UnitType.cs`, `UnitTypeTable.Link`, `Match.Become`, `MatchEvents.CreepTransformed`,
`client/Assets/View/EntityViewPool.cs`.
