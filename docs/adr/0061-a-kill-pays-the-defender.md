# 0061 — A kill pays the defender

The Grave Robber pays twelve gold to whoever kills it. Until now the only thing a body was worth to a defense
was the health it did **not** take off it: every line a purse was paid arrived when a wave closed, out of
numbers about the round as a whole, and a match had no economics of its own at all. A kill now mints gold
inside the match, and this record is what that column is, where the payment is made, which row pays it, and
what it does to the two measurements the cost note rests on.

**The number is `docs/roster.md`'s and not this record's.** Twelve is half the row's own twenty-four and the
roster argues the half. What is decided here is everything around it.

## What the column is, and which layout it went on

**`content/units.txt` goes to layout 6, under the hash label `unit-types/6`.** `bounty` is gold paid to the
defender that kills a body of this row. Layouts 1 to 5 keep their own reader branches, their own labels and
the hashes they always had, and a row whose field count does not match the layout it declared is refused from
either side.

**A new layout rather than an extension of layout 5**, which is the rule `content/units.txt` states about
itself: *adding or moving a column is a new layout and a new label*. Two different column counts both called
layout 5 is a class of silent misread nothing in this repository could detect, and #268 argued the same thing
one column earlier. The cost is one branch and one label.

**Why no existing column could say it.** `cost` is what a body is worth **alive** — a leak charges it against
health, one for one — and what a kill pays is the opposite outcome of the same body. A rule deriving one from
the other would be a design decision (see below, where one is deliberately not taken), so it is a column.

**What the table refuses**, both where the column is read:

| Refused | Because |
|---|---|
| A row that stands and pays | Nothing that stands is ever damaged here, so the kill the payment is made on never happens |
| A row with no health pool that pays | It cannot be damaged at all, so the same |

Both are the rule every other unread column in this file is refused by: a number read by nothing that still
moves the content hash.

**And one thing is deliberately not refused: a bounty above the row's own cost.** The Grave Robber's twelve is
half its twenty-four and the roster's argument is about that row's number. Whether a body may ever be worth
more dead than it cost to send is a design question nobody has taken, and a refusal here would take it.

## Where the payment is made, and which row pays

**Where a creep dies, which is one place.** `Match.Damage` is the only line in the simulation that sets a body
to dying; the payment is made there, on the tick health reaches zero, beside `_killed` and `CreepDied`.

**The row that pays is the row the body is standing as, and never the order that sent it.** That is the one
reading that answers all three routes a body can arrive by, and it is forced by two of them:

| Route | What the order says | What is standing |
|---|---|---|
| A wave release | The row that pays | The row that pays |
| A body that changed row mid-lane (#267) | The row it *was* | The row it *became* |
| A body a spawner raised (#268) | The row that raised it | The row it was raised as |

A raised body's `OrderIndex` points at its raiser's order, so the order cannot say what it is; a transformed
body's order names the row it has stopped being. The body in hand is the only thing that knows, and the body
is what has just been killed. **So a transforming pair pays its successor's number.** Today the Villager
becomes the Werewolf and neither pays, so nothing observable turns on it — which is exactly why it is written
down rather than left to be discovered by the first pair that does.

**That is deliberately not how a leak is priced**, and the asymmetry is a property of what each has in hand. A
leak is counted per wave order and priced afterwards off the order's type (`Run.LeakCost`), because a count is
all that survives a body leaving the corridor; #268 had to add a second count, `LeakedRaisedByOrder`, and a
rule that one order may name at most one raised row, to price the raised half at all. A kill is resolved with
the body still in the array, so it needs neither.

**A leak pays nothing.** Reaching the exit is the opposite outcome and is already priced, against health, at
the row's cost. No body is both charged and paid for: a body either reaches the exit or dies, and the two
branches are a hundred and thirty lines apart in `Match`.

## What it does to the one-purse rule

**The rule holds, and it is now doing more work.** There is one wallet, it buys towers and creeps alike, and a
bounty lands in it. Three things follow, and the third is the one nobody signed.

**First: the money is earned during a wave and still arrives between rounds.** A match has no purse and cannot
reach one — the simulation touches nothing outside itself — so `Match` accumulates what its kills paid and
hands it back on `MatchResult`, and `Run.Play` pays it into the purse at the same seam interest, the base and
the bonus land at: `Purse.CloseWave`. What changed is not *when* the purse moves but what the number is a
function of. A wave payment has four lines now instead of three.

**Second: it is minted rather than taken.** Nothing is deducted from the sender. Gold can be created in this
economy — the base and the interest already are — where health cannot: the pool is a clock that only runs down
(ADR-0035). So the two sides of a body are not a transfer between two players; they are a charge against one
player's clock and a credit to another player's wallet, in a quantity that is nominally the same unit.

**Third, and this is the consequence of the one purse rather than of the bounty: money earned by a defense
buys offense.** With two wallets, "a kill pays the defender" would mean the defense funds itself. With one, a
round that kills well sends a bigger wave next round. That is a coupling the one-purse rule creates and this
column is the first thing to exercise, because it is the first income that depends on what a player *built*
rather than on what a player *sent*. It is recorded here rather than decided: the roster signed twelve gold,
not where the twelve is spendable, and the alternative — a bounty that may only be spent on towers — is a
second wallet in all but name and would be the end of the one-purse rule.

**Which side of a pairing collects.** A round meets K opponents twice over: its wave against their defense,
and their wave against its defense. Only the second has this round's towers standing in it, so only the second
pays this purse. What the *opponent's* defense was paid for killing this round's wave is income for a stored
ghost, which has no purse to be paid into and no next round to spend it in. And the payment is the **average**
over the K opponents, exactly as leak cost dealt and taken are: summed, a defense would be paid ten times for
killing one wave.

## What it does to the leak exchange rate

**The rate itself does not move.** A leak charges health equal to what the creep cost to send, one for one;
nothing about that reads the bounty column, and the two outcomes are exclusive. The sentence
`docs/research/cost-is-not-a-balance-lever-under-a-one-for-one-leak.md` rests on — *what gold buys is health a
defense has to spend to stop it, so cost cancels out and a row is balanced by what it survives* — is still
true of every leak.

**What moves is the other end of the same body.** Before this, the two outcomes of one creep were:

| | Before | After |
|---|---|---|
| It reaches the exit | costs the defense its price, in health | unchanged |
| It is killed | costs the defense nothing, and pays nothing | pays the defense its bounty, in gold |

So the swing between the two outcomes of one Grave Robber is its 24 of health plus 12 of gold, where every
other row's swing is its cost alone. **The rate is unchanged and the spread around it is not.**

**And that is what the balance measurements in the cost note cannot see.** The return band —
`MatchTests.Every_walking_row_returns_a_comparable_share_of_its_gold_against_the_committed_defense`, four
hundred gold of one row against the committed defense — computes `leaked * 100 / count`. It is a **leak rate**
and nothing else, which is precisely why cost cancels out of it. With a bounty on the board, a row's leak rate
is no longer the whole of what a row is worth: two rows with identical leak rates now differ if one of them
pays for the bodies that did not leak.

**The band is left measuring what it always measured, and this record is where that is said out loud.** Three
reasons:

- **It is not this ticket's to retune.** The band is asserted as *missed* at both ends, as two exact lists,
  and the standing instruction is that a miss is updated as a miss and never converted into a pass. Changing
  what the number *means* would move five rows' readings for a reason that has nothing to do with any of them.
- **Netting the bounty off would make the reading wrong in a different way.** A defense that kills a body is
  paid once; a defense that lets it past is charged once. Subtracting one from the other inside a single ratio
  produces a number that is neither a leak rate nor a return, and the band's claim — *no row is dead and none
  is free money* — is a claim about leaks.
- **The sweep is the instrument that reads purses**, and it plays whole runs where the band plays one match.
  That is where a reading of this mechanic would come from — except that today it does not, and why is the
  section below.

**So the Grave Robber's band reading is 81 before and after**, unchanged to the digit, and that is the finding
rather than an oversight: the band cannot see this mechanic at all.

## What the sweep reads, which is also nothing, and that is the sharper finding

**`content/sweep.csv` came back byte-identical.** Not one figure of it moved: the Grave Robber still returns
367 gold dealt per hundred spent, still wins eight of eight, still deals 38,120 over eighty rounds. **Nothing
was tuned to make that so** — it is what a real sweep produced after the column landed.

**The reason is structural, and it is worth writing down because it is not obvious.** The sweep varies the
creep a run **sends** and holds everything else fixed. A bounty is paid to whoever **kills**, so it reaches a
run through the opponents it defends against — and every opponent in the committed sweep is the canned
stand-in out of `content/field.txt`, which sends a column of Minions and nothing else. A Minion pays nothing.
So across the whole report, in every run of every row, the bounty line is nought.

**Both balance instruments are therefore blind to this mechanic, for two different reasons**, and neither is a
bug in the instrument:

| Instrument | What it varies | Why it reads nothing |
|---|---|---|
| The return band, in `MatchTests` | The row a column is made of, against the committed defense | It counts leaks and prices them. A body that was killed is outside the ratio entirely |
| The sweep, `content/sweep.csv` | The row a **run** sends, against the canned stand-in | The bounty is defensive income, and the stand-in sends Minions |

**What would read it is a sweep whose stand-in sends the row.** That is one line of `content/field.txt` and a
regenerated report, and it is not this ticket's to change: the stand-in's column is *calibrated* — its own
header carries the measurements showing that a field member with the right total and the wrong number of
columns is a walkover — and swapping the row it sends would move every reading in the report for a reason that
has nothing to do with any of them. It is recorded here as what the next measurement of this mechanic needs.

## Whether the money is in the rolling hash

**It is.** `_bounty` folds once a tick beside `_leaked`, `_killed` and `_leakedRaised`, and `match-state/5`
becomes `match-state/6`.

**The argument against, taken seriously and rejected.** The bounty is derived: it is a sum over the kills, and
the kills are already folded. But `_killed` is a *count*, and the bounty is a *sum over rows* — so two matches
that killed the same number of different bodies agree on every other field in this fold and differ only in the
money. A body that has died and been cleared away leaves nothing else behind: it is out of the creep array, so
the per-creep half of the fold says nothing about it. That is the shape of the bug #254 found in the snapshot
comparison — four fields missing, and two different pictures comparing equal — and the fold is the one place
this repository has for noticing it.

**The argument for, positively.** Gold is state a build phase reads. The fold's job is internal state a
snapshot never carries, and this is the first such state that leaves the match and is *spent*. A run one kill
away from affording a tower is a different run from one that is not, and until now nothing inside a match
could have made that difference.

**`SimulationVersion` goes to 13** and the rule fingerprint's label to `rule-fingerprint/11`, which added a
**seventh half** to the fold rather than moving a roster. That is worth recording on its own: #269's rule
fires where a body's health reaches zero, and **not one of the six halves that fingerprint already had kills
anything at all** — every body on every roster in that file walks to the exit. A bounty put on the sixth
half's roster moved the fingerprint by nothing at all, twice over. The seventh half is a defense that kills
what walks at it, over a roster where all four walking rows pay and the row a body is standing as at the kill
is not the row its order names for two of them: a goon pays the husk's three where its own row says one, and a
shade pays its own one where the raiser that put it down pays two. So reading the payment off the order is a
different fingerprint rather than the same one.

## What the view gets

**`BountyPaid` joins the decorative stream**, carrying the body that paid and what its row pays: two integers,
no position, and a subscribed match produces the same rolling hash as a silent one (ADR-0008). It is the
eleventh event.

**`MatchDecorations` draws nothing for it**, for the reason it draws nothing for `CreepTransformed` and
`CreepRaised`: a coin, a number floating off the body, a flash on the purse — each is an art decision, every
colour and duration in it would be unsigned, and picking one unattended is not a thing this project does. What
the ticket asks for is that the view *shows the gain on the tick and scrubs correctly*, and both halves are
true without a shape being chosen:

- **The gain is on the tick**, because the payment and the event are made on the tick health reaches zero, and
  the view drives the same `Match.Advance` a headless run does.
- **It scrubs correctly by construction.** What a match has paid is a number on the match, and a seek
  re-simulates from tick zero with nobody subscribed (ADR-0026) — so a scrub to either side of the tick reads
  the running total the re-simulation arrived at, and no decoration is left over from a tick that is now in the
  future. `MatchViewTests` scrubs across a paying kill in both directions and asserts the number moves with it.

**The day a shape is signed, it goes in `MatchTuning` beside the other placeholders**, whose header still
reads "TEN SHAPES ARE SIGNED, FOUR ARE NOT". Nothing was added to it here, because nothing was chosen.

## What bounds the money for whoever cannot play the match

`CommandStream.Check` walks a stored stream and folds a purse forward at a **ceiling**, so that everything it
refuses was unaffordable however the run played. A ceiling that left out an income line would stop being one,
so the walk gained a second bound beside `WaveScript.FullPrice`: `Run.MostBountyEarnable`, which is the most
bountiful wave in the run's pool, because a round's bounty comes out of the opponents it met and is the
average over K of them.

**What one wave can pay is arithmetic**, in `WaveScript.MostBountyPayable`, and its second term is the
interesting one. A body pays its own row's bounty or its successor's, whichever is larger, because which of the
two it is standing as when it dies is unknown there. And a body that raises puts more bodies down, **each read
the same way** — bounded because a raise happens once a period for as long as the raiser walks, and a match
that has not ended by `Match.TickCeiling` throws rather than returning a result. So the ceiling divided by the
period is a count no spawner can exceed in a match anybody ever gets an answer out of. **What a raise puts down
may become another row but may not raise** — `UnitTypeTable.LinkRaises` refuses the second and not the first —
so the term takes one step and stops.

**It is loose in the direction that is safe and zero for every wave this roster can compose**, because nothing
here raises a row that pays.

> **A finding about the line beside it.** `WaveScript.FullPrice` is documented as the ceiling on what a round
> can *deal*, and since #268 it is not one: a raised body's leak is charged at the raised row's price
> (`Run.LeakCost`) and `FullPrice` counts only the bodies an order sends. A pool holding a spawner therefore
> bounds the walk's bonus below what a round could really earn, which is the one direction that produces false
> refusals. Nothing in the committed content reaches it — the stand-in sends Minions — and it is #268's line
> rather than this one's, so it is named here and not changed.

## What it costs

**Two retirements, and both are deliberate.** `content/units.txt` goes to **layout 6** under `unit-types/6`,
so every record stamped against the roster as it stood is retired; the content hash goes
`B52AAD46D08E9871` → `CEAD5CE53790DD40`. `SimulationVersion` goes to **13**, because a match now produces a
number a run spends.

**The committed match is untouched by the mechanic.** The committed wave sends Minions and Skeleton Scouts and
neither pays: 3 of 40 leaked, tick 5302, the same four landmarks, nought gold paid. Its rolling hash moves
anyway — `14AFD6260FFD30E4` → `441D37E128517F3D` — which is what the version bump is for.

## Where it lives

- `sim/UnitType.cs` — `Bounty`, and the layout-6 fold.
- `sim/UnitTypeTable.cs` — the column, the layout-6 branch, and `ReadBounty`.
- `sim/Match.cs` — the payment in `Damage`, `Pay`, `Bounty`, and the per-tick fold.
- `sim/MatchResult.cs` — `Bounty`, which is what leaves the match.
- `sim/Purse.cs` — the fourth line of a `WavePayment`.
- `sim/RunOutcome.cs` — `BountyEarned`, so the purse stays a fold over the vector.
- `sim/Run.cs` — `Scored`, which reads both halves of one resolution, and `MostBountyEarnable`.
- `sim/WaveScript.cs` — `MostBountyPayable`, the ceiling.
- `sim/MatchEvents.cs` — `BountyPaid`.
- `client/Assets/View/MatchDecorations.cs` — the sink that hears it and draws nothing.
- `content/units.txt` — layout 6, and the one row that pays.
