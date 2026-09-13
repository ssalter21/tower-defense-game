# 0056 — An effect is a stat, a magnitude and a duration, and a creep never drops below a tenth of its speed

**Decided.** `sim/Effects.cs` is one model for every modifier: a stat (speed, cooldown, armour or shield — never
range), a magnitude and a duration, held in one slot per stat on the unit. A bubble with a non-damage payload
emits one; nothing else knows how a modifier is stored.

- **Strongest wins, timer refreshed.** Strength is distance from zero; equal magnitudes are ordered by sign
  with the lower winning, so two towers built in either order fold the same numbers. The surviving expiry is
  the later of the two. A weaker effect is discarded, not queued: when the strong one expires the unit is back
  at its row's number.
- **Expiry is an absolute tick**, cleared at the top of a tick, emitted at the close, so an effect landing on
  *t* with duration *n* is in force for exactly *t+1* … *t+n* whichever phase emitted it.
- **A shield payload grants a pool** as a share of the health it stands in front of, spent before the
  authored pool, restored to full by a pulse and never past it; it may author no duration, lasting until spent.
  *This reading is the implementer's and wants a signature* — it applies to
  [the Necromancer](../roster.md#38--necromancer--status-live).
- **The floor: a creep never drops below 10% of its authored speed, and never below one milli-hex**, applied
  after the modifier. It is a safety rail, not a balance number: `Match` refuses speed ≤ 0 at construction
  only, and a hundred-percent slow would otherwise run to `TickCeiling` and throw. `Match.RequireItArrives`
  proves every order reaches the exit inside the ceiling at floor speed, against the raw route length and the
  converted step, both of which round in the direction that flatters. There is deliberately no floor under
  cooldown — a cooldown of zero is a balance problem, not a hang.
- **Arithmetic.** The step per tick moved from the wave order to the creep (`Creep.Step`), so two Minions of
  one order slow separately and `ReportPasses` stays right. The modified step is one fused integer expression
  evaluated when the modifier changes — truncating twice computes a different function.
- **Creep positions come back into the tick loop, bounded**: a radius is hex distance so an aura reaches the
  next leg of a fold; the position is a table lookup, taken only on ticks a bubble fires; range is untouched.

**Cost.** `SimulationVersion` 9 → 10, `match-state/2` → `/3` (the fold carries four magnitudes always and the
expiries and pool when non-zero), `rule-fingerprint/7` → `/8` with a scenario that finally exercises a stat
bubble; every record under version 9 is retired. The committed match did not move.

**Rejected.** A stack per stat (additive runs away with gold; a queue runs the duration away). A countdown
per effect (whether the landing tick counts would differ between a shot and a pulse). Clamping the floor as
each effect lands (the floor is the creep's, not the modifier's). A damage aura — it would draw outside a shot
([ADR-0003](0003-dice-rolled-once-per-shot.md)); refused at load. A shield share of the recipient's own
shield column (inert on every row). Refusing every inert payload/side pair (only speed→tower and
cooldown→creep are refused, being permanent facts about the roles). A damage *modifier* — `damage` already
means the attack's own roll spread, and the fifth name is taken; the sixth keyword is
[open](../open-questions.md). An event for a modifier landing — a view wants a snapshot field
([ADR-0007](0007-snapshot-is-the-only-view-input.md)), which #254 added.

Decided in #213, built in #217; the narrative is
[the decision log](../decision-log/2026-08.md#16-august-2026-last--a-stat-can-move-while-a-match-is-running-and-a-floor-stops-that-ending-it).
