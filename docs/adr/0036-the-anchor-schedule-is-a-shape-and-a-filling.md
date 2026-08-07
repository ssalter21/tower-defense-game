# 0036 — The anchor schedule is a shape and a filling, and the loader is where its constraints live

The anchor schedule is one committed text file read into one type, in two layers:

- The **shape** — which waves are anchors, which tier each draws from, which one opens the steep counter, and
  what answers each — is authored in `content/schedule.txt` and holds for a whole rotation.
- The **filling** — which of a tier's game changers reach that anchor's menu — is drawn per run, at a position
  derived from the run's seed under the label `"run-filling/1"`, and revealed at run start.

Wave slot width is derived from the shape and authored nowhere. `AnchorSchedule.WaveSlotsAt` counts the
anchors at or before a round and hands the count to `Ruleset.WaveSlotsAt`, which is the ruleset's starting
width plus its per-anchor step. Against anchors at 3, 6 and 9 that produces 2, 2, 3, 3, 3, 4, 4, 4, 5, 5.

## What was decided

**Two layers, because one layer cannot have both properties.** Preparation is a skill about a constant, so
something has to hold still for a rotation; replay value comes from churn, so something has to move every run.
A schedule fixed on both is solved by Tuesday. A schedule drawn on both leaves nothing to prepare against.
Splitting them is what lets *everyone knows what lands at wave 9; nobody knows who took it* be true.

**A table rather than a set of constants.** The shape is a property of a rotation and the step 4 sweep will
want to sweep it the way it already takes its map as an argument. It is parsed exactly as `content/units.txt`
and `content/ruleset.txt` are — text handed in, never a path; a content hash folded over the parsed integers
in field order — so reindenting a column or renaming a game changer moves nothing and changing one number
retires every run pinned to the old shape.

**Slot width is derived, and a second series is refused by name.** Two schedules maintained separately drift
the first time somebody moves an anchor and forgets the copy, and nothing says so. So a `slots` row in
`content/schedule.txt` is not merely unrecognised — it has its own refusal, saying the number is computed.
The test that proves the derivation moves an anchor from wave 6 to wave 5 and watches the widths move with it,
because a series authored beside the shape would leave that green.

**Every constraint is enforced at load, each by name.** A constraint that is remembered is a constraint that
is not enforced, and a designer reading "could not load schedule" learns nothing they can act on. The loader
refuses: anchors out of wave order; a repeated game changer, which is one creep on two anchors' menus; tiers
that do not escalate with the waves; a count of steep counters other than one; a steep counter that is not the
last anchor's; a bonus on any tier but the steep anchor's, or a steep tier row carrying none; a game changer
that stands where it is put, or an answer that walks the corridor; a tier no anchor draws from, or an anchor
whose tier has no pool; and — the constraint seam 3 inherits rather than chooses — **a counter that is not
purchasable strictly before the anchor that needs it.** An answer that first appears at the wave it answers is
a forced simultaneous buy, and it deletes the axis the schedule exists to restore.

**`bonusVsTag` is a number on a game changer, not a column on every unit row.** It is what that anchor's
counter adds to its rolled damage *before* the type chart and armour, which is why a high-armour game changer
blunts its own counter rather than being exempt from mitigation. It is paid only to the unit type the anchor
named as its answer; anything else shooting the same creep is unprepared and gets nothing, which is the whole
of what preparing buys. The committed value is 825 against the mortar's average roll of 275, so a prepared
shot lands four times as hard: **steep rather than binary**, so mis-preparing punishes instead of eliminating.

**Nothing is keyed on what the filling drew.** The filling is one draw at one derived position; the field is
still drawn from the run and the round alone. A ghost pool sharded by which filling a run got would pay for
variance with a thinner pool, and rotation taxes that quite enough already. The test that pins this plays two
runs on one seed against two shapes with different tier pools and requires identical fields.

## What it costs

**A third label in the derivation scheme.** `"run-filling/1"` joins `"run-field/1"` and `"run-match/1"` as a
constant on `Run`, for the reason ADR-0034 gives: two draws that accidentally shared a label would be
correlated in a way nothing would report.

**`Run` gained a constructor parameter, and it has no default.** A run without a shape has no anchors, no slot
widths and no menus, and a default shape would be a schedule nobody authored folded into every run's
behaviour. Every call site names one.

**The steep-counter column is checked and not folded.** `Anchor.OpensTheSteepCounter` is authored, and the
loader requires it on the last anchor and nowhere else — so it carries no information the position does not,
and folding it would add a field to the hash that no retune can move. It is stated in the file for the same
reason the matrix rows name their attack type: so a shape that disagrees with itself is refused rather than
read.

**The tier pools are placeholders and say so.** Twelve game changer rows across three tiers, each fielding one
of the two creeps that exist. The nine authored game changers a shape wants are seam 3's content bill; what is
here is enough to make the draw a draw and the machinery testable.

## What was rejected

**Authoring the slot series beside the anchors.** It reads better in the file and it is a second source of
truth for one cadence. The whole point of one cadence governing the run is that an anchor is a single legible
landmark: the wave got wider, and something new arrived.

**Drawing the filling per rotation.** It would make ghosts perfectly coherent and it would make run 5 the same
as run 1, which is the objection the two layers exist to answer.

**Redrawing the filling as the run goes.** Cheaper to write as a property, and it means the menu a player
prepared against is not the menu they are offered. It is drawn once, in the constructor, and `RunTests`'
reflection assertion that `Advance` is the only member that moves anything keeps it that way.

**Refusing a run whose anchors fall outside its wave count.** Tempting, and it would have made a three-wave
sweep row against the ten-wave shape a load error. N is a parameter and a short run is a truncated run, not a
broken one, so the anchors past its end simply never arrive.

## Where it lives

`sim/AnchorSchedule.cs`, `sim/AnchorFilling.cs`, `content/schedule.txt`, `sim/Run.cs` — the `FillingLabel`
constant and the `Schedule` and `Filling` properties — and `sim.tests/AnchorScheduleTests.cs`.
