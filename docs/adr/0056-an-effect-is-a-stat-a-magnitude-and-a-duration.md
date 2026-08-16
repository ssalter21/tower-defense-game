# 0056 — An effect is a stat, a magnitude and a duration, and a creep never drops below a tenth of its speed

[`sim/Effects.cs`](../../sim/Effects.cs) is the first per-unit state in this simulation that a rule can move
while a match is running. One model, not one per mechanic: a slow, a rally, a curse and a granted pool are the
same four fields with different numbers in them, exactly as a sweep, a blast and an aura turned out to be one
[`Bubble`](../../sim/Bubble.cs) in [ADR-0055](0055-a-sweep-a-blast-and-an-aura-are-one-bubble.md).

Decided in [#213](https://github.com/ssalter21/tower-defense-game/issues/213), recorded in
[the decision log](../decision-log.md#16-august-2026-later--one-format-version-and-the-map-it-is-for), built in
[#217](https://github.com/ssalter21/tower-defense-game/issues/217). `SimulationVersion` 9 → 10, the match's
state-hash label `match-state/2` → `match-state/3`, and the rule fingerprint's label `rule-fingerprint/7` →
`rule-fingerprint/8`.

## What was decided

**An effect is a stat, a magnitude and a duration.** What emits one is a bubble carrying a payload that is not
damage; what holds one is a slot on the unit; and nothing else in the simulation knows how a modifier is
stored. The modifiable stats are **speed, cooldown, armour and shield**. Range is not on the list and never
will be — a payload that moved a range would have to rebuild [`TowerCoverage`](../../sim/TowerCoverage.cs)'s
intervals inside the tick, which is the one thing that arrangement exists to prevent, and the word is refused
where the column is read.

**Stacking is strongest-wins, with the timer refreshed.** A player may build the same tower as many times as
gold allows, so a rule that added magnitudes would hand a big enough board an arbitrarily large modifier — and
a slow of more than a hundred percent walks a creep backwards. One slot per stat makes the ceiling the
strongest single row rather than the count of them. A second landing of the same magnitude refreshes the
timer and changes nothing else, which is the half a player can see.

**Ordering is asserted canonical, not restored and not incidental**, which here means the comparison is a
strict total order on the integers: strength is distance from zero, and two magnitudes equally far from it are
ordered by sign with the lower one winning. Without that last clause a curse and a blessing of the same size
would resolve by whichever landed last, and two runs that differed only in the order two towers were built
would fold different numbers. The surviving timer is the *later* of two expiries for the same reason — a
maximum is commutative and "the last one wins" is not.

**A weaker effect is discarded rather than queued.** When the strong one expires the unit is back at the
number on its row and not at the weak one. A queue is a stack wearing a different hat: it would make the total
time a stat spends displaced grow with the number of sources, which is what strongest-wins exists to stop.

**Expiry is an absolute tick, cleared at the top of a tick, and emission closes the tick.** So an effect
landing on tick *t* with a duration of *n* is in force for ticks *t+1* through *t+n* — exactly *n* ticks, and
the same *n* whichever phase emitted it. A countdown would have had to decide whether the tick an effect
landed on counts, and the answer would have differed between a bubble fired with an attack (the middle of a
tick) and one that pulses (the end of one).

**A shield payload grants a pool, as a share of the health it stands in front of.** It is the one payload
that grants rather than displaces, which is why it is the one that may author no duration at all: how long it
lasts is how long it takes to be spent. The granted pool is spent before the pool the row authored, because it
is the one that can be taken away. A pulse restores it to full and never past full.

## The floor, and why it is an invariant rather than a number

**A creep never drops below 10% of its authored speed**, binding every effect at once and applied after the
modifier. This is a safety rail and not a balance number.

[`Match`](../../sim/Match.cs)'s constructor refuses a unit with speed ≤ 0 **at construction only** — "a unit
that walks a corridor at nothing per tick never reaches the exit and never dies, so the match it is in cannot
end". A runtime modifier bypasses that guard entirely: a slow of a hundred percent is a legal number in a legal
column, and what the match does instead of hanging is run to `TickCeiling = 120000` and throw, thousands of
ticks after the mistake, with nothing to point at.

The floor makes a hung match unreachable **by arithmetic** rather than by careful authoring. Two clauses make
that true rather than nearly true:

- **At least one milli-hex, whatever the percentage truncates to.** A tenth of nine milli-hexes is zero in
  integer arithmetic, and zero is precisely the value the floor exists to make unreachable.
- **And the wave is checked against the map at construction.** `Match.RequireItArrives` computes, per order,
  the tick the last unit of it would reach the exit on walking at its floor speed, and refuses the match if
  that is at or past the ceiling. So the guarantee is proved for the map and the wave in hand rather than
  asserted in prose about a corridor whose length is an argument. It is taken against the **raw** route
  length and against the **converted** step rather than the milli-hexes it came from, because both
  conversions round in the direction that flatters the answer — a bound that is nearly right is a bound that
  passes the wave it exists to refuse.

## The arithmetic trap, and where it was avoided

`SpeedMilliHexPerTick` is converted to a Q32.32 step by `Fix64.FromRatio(speed, 1000)`, and `Match` calls that
"the one place the truncated remainder that the state hash exists to watch is created". Two things followed
from making it modifiable:

**The step moved from the wave order to the creep.** `_stepPerTick` was indexed by `OrderIndex`, and a
modifier is per unit — two Minions released by one order are not slowed together. `Match.StepThisTick` reads
the same number to work out where a creep was a tick ago, so an array indexed by the order would also have
mis-reported every overtake involving a modified creep, silently. It is now `Creep.Step`, and
`ReportPasses` is correct for a slowed creep because of it.

**Truncate once, not twice.** `FromRatio((speed × pct) / 100, 1000)` and multiplying the already-truncated
`Fix64` step by a fixed-point percentage compute *different functions* — the same hazard
[`DamageModel`](../../sim/DamageModel.cs)'s remarks name for a stat pipeline. It is one fused integer
expression, evaluated when the modifier changes rather than per tick, so the division happens a handful of
times in a match instead of once per creep per tick. Twenty-eight thousandths of a hex at sixty-five percent
is 18 thousandths and raw `77309411`; the other spelling is a different number, and `EffectTests` pins both.

## Creep positions come back into the tick loop, knowingly

A radius is measured in **hex distance**, by decision, so a Necromancer's aura reaches the neighbouring leg of
a fold rather than only the creeps behind it in the column. Route distance was offered as the free alternative
and was not taken.

That means a creep's position has to exist during a tick, which undoes the property `TowerCoverage` was
written around — so it is paid for deliberately and bounded three ways. A creep's `Distance` maps to a route
index and a route index has a hex, so it is a **table lookup rather than a search**. It is evaluated **only on
the ticks a bubble actually goes off**, which for an aura is once per period and never on the other ticks of
it. And **range is untouched**: coverage is still intersected with the route at load and the tick loop still
answers "in range" with two comparisons on a line.

## What was rejected

**A stack of effects per stat.** Additive magnitudes run away with the number of towers a player builds; a
queue of them makes the *duration* run away instead. Strongest-wins is the only rule with a ceiling that does
not depend on how much gold somebody had.

**A countdown per effect.** Cheaper by a subtraction and wrong at the edges: whether the tick an effect landed
on is one of the ticks it lasts would have depended on which phase emitted it, so "expires exactly on its
duration" would have been two different sentences for a shot and a pulse.

**Clamping the speed floor as each effect lands.** The floor is a property of the creep and not of any one
modifier, so it is applied after the modifier rather than inside it. With one slot per stat the two are the
same sentence today; they stop being the same sentence the moment a second slot exists, and the version that
survives that is the one written down here.

**A damage aura.** A bubble carrying damage with a period would have to draw outside a shot, and
[ADR-0003](0003-dice-rolled-once-per-shot.md) is that the dice are rolled exactly once per shot, for damage,
and nowhere else — which is what makes the stream's position a running count of the shots fired so far, and is
half of what every stored record replays through. Refused at load, with the reason attached. A tower that
wants to damage on a clock has a cooldown.

**A shield magnitude read as a share of the recipient's own shield column.** Consistent with the other three
payloads and inert on every row the mechanic was designed for: the roster's walking rows author no shield at
all, so an aura granting a share of it would grant nothing to everybody. A shield is a pool rather than a rate
and has no authored number of its own to scale, so the only quantity a percentage of it can mean is the pool
it stands in front of.

> **This one is provisional and wants a signature.** #213's column table says "a percentage" and stops, so
> something had to be chosen for the column to mean anything at all — and what it grants, how long it lasts
> and whether killing the emitter strips it are shapes of the Necromancer rather than shapes of the
> simulation. What is written above is the implementer's reading, taken because an implementation cannot
> leave it blank; it is Sam's to confirm or move, and [the roster](../roster.md#7--necromancer--status-live)
> carries it as an open question rather than a closed one.

**A floor under the cooldown modifier.** The speed floor exists because a speed of zero is a *termination*
hazard — nothing else in the simulation has one. A cooldown of zero is an ordinary authoring that the column
already permits, so a rally of a hundred percent produces a tower that fires every tick, which is a balance
problem and not a hang. `Effects.Modified` clamps at zero so the number can never go negative, and there is
deliberately nothing else: a floor invented for symmetry would be a balance number wearing a safety rail's
clothes, which is exactly what the speed floor is careful not to be.

**Refusing every payload the side it reaches has no use for.** Two are refused — a speed reaching towers and a
cooldown reaching creeps — because nothing that stands walks and nothing that walks attacks, and both are
permanent facts about the two roles. A pool or an armour reaching a tower is **not** refused, even though
nothing shoots a tower in this loop: that is a fact about the rows in `content/units.txt`, every one of which
authors a placed unit with no health pool, rather than about what a placed unit is. Refusing it as well would
have made `bubbleAffects` derivable from the payload and the role in every case, and a column carrying nothing
is a worse failure than an inert one — the fixed list of nine is what #213 bought.

**A damage modifier.** #217's model names five modifiable stats including damage, and the payload column has
five values including `damage` — but `damage` in that column already means *the attack's own roll, spread*,
which is what #216 built and what ADR-0055 records. The two readings cannot share a word, the list of five is
fixed, and inventing a sixth keyword would be widening the schema that #213 closed. So four stats are
modifiable and the fifth name is taken; it is written here rather than left as a silence.

**An event for a modifier landing.** [ADR-0008](0008-match-events-are-decorative.md) makes events decorative
and there are six of them; a seventh would be a view contract taken in a ticket about rules. What a slow does
is in the state hash, and a view that wants to draw one wants a snapshot field, which is its own decision.

## What it cost

**Every stored record made under simulation version 9 is retired.** The tick order gained two phases and a
creep's step became a fact about the creep, so the arithmetic of a tick moved; and `match-state/3` folds what
every unit is carrying — four magnitudes always, and the expiries and the granted pool when one of them is
non-zero — so every stored record's rolling hash stops reproducing whether or not its content authors an
effect. Both halves of that are what a simulation version exists to say.

**Nothing about the committed match moved.** Every row of `content/units.txt` authors no bubble at all, so the
same wave leaks the same twelve creeps on the same tick 5283, the four landmark ticks are identical, the run
still dies in round four having dealt 229, and `content/sweep.csv` is byte for byte what it was. What moved is
the hashes.

**And the rule fingerprint could not see any of it, for the seventh time and in the roster again.** The sixth
half of that fold is fought over a layout-3 roster, which is why it was added — but both of its bubbles
carried damage, and a timed effect is emitted by a bubble carrying a stat and by nothing else. The rules ran in
that half and were visible in none of the six. What changed is the rows: a turret whose shot slows what it
hits and a walker whose aura grants a pool to whatever walks beside it, the label went to
`rule-fingerprint/8`, and the row is `(10u, 0x13EB7A4673B75F21UL)`. With `Effects.ModifiedSpeed` returning the
authored speed — the slow landing, expiring and changing no step, every other line untouched — the same
scenario folds `0x4B15804EC1BEDE48`, which is what makes the row evidence rather than a number somebody wrote
down.
