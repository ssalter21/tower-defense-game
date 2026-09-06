# 0059 — A creep raises a creep, and the board is what caps it

The Necromancer raises a Minion beside itself every 150 ticks. Until now every body on the corridor came out
of a wave order: `Match.Release` was the one thing that spawned a creep, `Creep.OrderIndex` said which order
paid for it, and a leak was priced by reading that order's type off the cost table. A creep can now put
another creep on the corridor, and this record is where that body enters the world, what it is worth, and what
still guarantees the match ends.

## What the columns are, and which layout they went on

**`content/units.txt` goes to layout 5, under the hash label `unit-types/5`.** `raises` names the row a body
of this one puts on the corridor beside itself; `raisePeriod` is the ticks between one and the next. Layouts 1
to 4 keep their own reader branches, their own labels and the hashes they always had.

**A new layout rather than an extension of layout 4**, which is the rule `content/units.txt` states about
itself: *adding or moving a column is a new layout and a new label*. Extending 4 in place would mean two
different column counts had both been called layout 4, and a table written yesterday would be read against
shifted fields by a reader that thinks it understands the number in the layout row. The cost of the new
layout is one branch and one label; the cost of reusing the old one is a class of silent misread that nothing
in this repository could detect.

**What the table refuses**, all where the columns are read:

| Refused | Because |
|---|---|
| A row that raises itself | A body putting a copy of itself down on a clock doubles a population, and no arithmetic bounds it |
| A row that raises a row nobody authored, or a row that stands | A raise puts a body on the corridor, and only a walking row is one |
| A row that raises a body with no health pool | Nothing could kill it, so a spawner making them is a leak nothing can answer |
| A raised row that raises in its turn | The second generation, directly |
| A row that becomes a row that raises | The second generation by a longer route — and see the pricing below |
| A raise with no cadence, or a cadence with no raise | The rule every other unread column in this file is refused by: a number read by nothing that still moves the content hash |
| A row that stands where it was put and names a row it raises | Nothing that stands is on the corridor at all |

The last two refusals in the table are what make **one wave order name at most one raised row**, which the
pricing below depends on.

## Where a raised body enters the array, and the tiebreak that follows

**At the end, with the next entity id.** `Match.Spawn` is now the one place a creep starts existing whichever
put it there — a wave release and a raise differ in where the body starts, what it walks at, and whether
anybody paid for it, and in nothing else. Ids are handed out in arrival order and `Match.ClearAwayTheGone`
keeps the array in ascending id, so a raised body sits behind everything already standing.

**That decides the tiebreak, and the direction it decides it in is the conservative one.** Targeting takes
the creep furthest along and settles a tie on the lower id, so a body raised level with its raiser — which is
exactly where it arrives — **loses every tie it is in**. A tower looking at a Necromancer and the Minion it
just raised shoots the Necromancer. The alternative, inserting a raised body beside its raiser, would have
meant either renumbering everything behind it or keeping the array in something other than id order, and
`Match.ReportPasses`, `Match.Acquire` and `Match.Fold` all read that order as part of the rules.

**And the raise runs where the wave's own release runs**: at the close of the tick, after everything on the
board has moved, shot, pulsed and been cleared away, and before the fold. A body raised on tick *t* is in the
picture of tick *t*, takes its first step on *t + 1*, and can be shot at from *t + 1* — exactly as one the
wave released on the same tick. Only a walking body raises, so killing a spawner stops the raises on the tick
it dies rather than at the end of its corpse.

**Immediately before that release rather than after it**, so that a body the wave puts down does not spend a
tick of its own cadence on the tick it arrived. Run the other way round and the first body of a column waits
a whole period and every body behind it waits one tick less, which would make "every 150 ticks" a fact about
the clock rather than about the body.

**The first raise is a whole period after the body arrives**, where an aura's counter starts at zero and
pulses on the tick its emitter spawns. The two are deliberately different: a pulse grants something to bodies
already there and costs nothing to have arrived, where a raise puts another body on the board — so firing it
on the arrival tick would mean a spawner that shows up already accompanied, which is a design statement
nobody signed. `docs/roster.md` signs *every 150 ticks*, and 150 ticks after it arrives is what that says.

**No die is rolled.** Where the body goes is its raiser's own distance and the next lateral offset in the
cycle, both determined, so the position of the dice stream is untouched by a raise and the stream stays a
running count of the shots fired. That is asserted rather than stated: `RaiseTests` reconstructs the landings
of a match full of raises out of a fresh `Pcg32` and compares them in order.

## What a raised leak charges, and what nobody paid for it

**A raised body's leak charges health, at the price of the row it was raised as.** A body reaching the exit takes exactly
as much health off a defense whether somebody bought it or not, so charging nothing for it would make the
raise free in the run economy and visible only as a longer match. `Match.LeakedRaisedByOrder` counts those
leaks beside `Match.LeakedByOrder`, and `Run.LeakCost` prices them off the raised row rather than off the
order's own type — which is why one order may name only one raised row, because the order index is the only
thing that says which row a raised body was once it has left the corridor.

**The sender paid nothing for any of them, and that is the gap.** Creep cost is effective health over 160,
per row, and it is derived rather than authored — so a Necromancer's 21 gold is its own 3380 effective health
and cannot see a Minion. Measured against the committed defense, one Necromancer raises **11** Minions before
it leaks, so 21 gold of body arrives with **110 gold** of bodies behind it. Four hundred gold of Necromancers
— nineteen of them — leaks **228 bodies**: the nineteen that were sent, and **209** that nobody bought. Its
reading in the roster's return band goes from **100 to 1200** against a band of 60 to 95.

**The gap is held open rather than closed**, exactly as the Mage's splash, the Vampire's shield and the
transforming pair are. A coefficient for a spawner guessed against one corridor is a coefficient guessed
against geometry that is going away, and the sweep is what is meant to derive it. Nothing is retuned here and
no cap is added: `docs/roster.md` signs an uncapped raise in as many words, and the number above is the
finding.

## What still guarantees the match ends

**The population is bounded by the board and the arrival is bounded by arithmetic, and only the second is
checked.** `Match.RequireItArrives` used to prove that every authored order reaches the exit inside the tick
ceiling at the slowest walk any combination of effects can leave it at. A raised body is in no order, so that
proof said nothing about it.

What replaces it is one more term rather than a walk over a graph:

- A body raises only while it is **walking**, so no raise happens later than the tick its raiser would have
  left the map at — which is the number the old proof already computed.
- What a raise puts down **raises nothing**, by either route, so there is no second generation.
- A body raised at that last moment has at worst a whole corridor in front of it.

So the bound is *the raiser's own latest, plus one floored crossing of the raised row, plus its longest
death*, and if that is inside the ceiling then no arrangement of effects produces a match that does not end.
`Match.LastOut` is that crossing, taken against a row and the row it becomes together.

**How many it raises is deliberately not bounded.** At the floor speed — a tenth of the authored one, which
is what `Effects.FloorSpeed` guarantees and what a stack of slows could in principle reach — a Necromancer
crosses the fifty-one hex corridor in about 25,000 ticks and raises **on the order of a hundred and seventy**.
With the committed Overgrowth on the board, which is a board-wide twenty percent slow, it raises **15** rather
than 11. That is a live trade-off against stacking slows and it is kept: the two capstones built to handle a
push are the two that make this body worst.

## What the view gets

**The raised body needs nothing new.** It is an entity in the snapshot from the tick it arrives, so
`EntityViewPool` claims a view for it by the same subtraction it claims one for a released creep, and a seek
back across the tick takes it off screen again with nobody having heard anything — which is what a seek is,
since it re-simulates and subscribes nobody (ADR-0026). `MatchViewTests` scrubs across the raise in both
directions.

**`CreepRaised` joins the decorative stream for the moment itself**, and it is decorative in the ADR-0008
sense: two entity ids, no position, and a subscribed match produces the same rolling hash as a silent one.
`MatchDecorations` draws nothing for it, for the reason it draws nothing for `CreepTransformed`: a grave
bursting, a flash or a column of light at the raise is an art decision, and picking one unattended is not a
thing this project does.

## What it costs

**Two retirements, and both are deliberate.**

`content/units.txt` goes to **layout 5** under `unit-types/5`, so every record stamped against the roster as
it stood is retired. `SimulationVersion` goes to **12**, because the tick loop grew a phase.

**The state hash folds the clock each creep raises on**, beside the health, the phase and the row it is, and
`match-state/4` becomes `match-state/5`. Whether a body was raised is a constant of it and folds once at the
spawn instead, beside the row it is and the lane it walks in; the running count of leaks nobody sent folds
with the other totals. A run that is one tick from putting a body on the board is a different match from one
that is two, and the per-tick fold is the only thing that would ever notice.

**The committed match is untouched by the mechanic.** The committed wave sends Minions and Skeleton Scouts
and neither raises: 3 of 40 leaked, tick 5302, the same four landmarks. Its rolling hash moves anyway, which
is what the version bump is for.

## Where it lives

- `sim/UnitType.cs` — `Raises`, `RaisePeriodTicks`, and the layout-5 fold.
- `sim/UnitTypeTable.cs` — the two columns, the layout-5 branch, and `LinkRaises`, which resolves and refuses.
- `sim/Match.cs` — `Raise`, `Spawn`'s new arguments, `FirstRaiseIn`, `WalkersOf`, `RequireWhatItRaisesArrives`,
  `LeakedRaisedByOrder` and the per-tick fold.
- `sim/Run.cs` — `LeakCost`, which prices a raised leak off the row that was raised.
- `sim/MatchEvents.cs` — `CreepRaised`.
- `client/Assets/View/MatchDecorations.cs` — the sink that hears it and draws nothing.
- `content/units.txt` — layout 5, and the one row that raises.
