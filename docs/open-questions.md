# Open questions

In scope, headed toward [the destination](vision.md#1-the-destination), not yet sharp enough to seam. A
question leaves this file when it is decided — into [the vision](vision.md), with the reversal, if any, in
[the decision log](decision-log.md).

## What the design research found

**Nothing is in flight, and the notes are gone.** Five surveys were commissioned against this file between 3
and 6 August 2026. Every one came back, every verdict below was taken out of it and into the vision, this file
or an ADR, and the notes themselves were
[retired on 5 September](decision-log.md#5-september-2026-later--fifteen-research-notes-are-retired). What
they found is here; the working is not, and re-commissioning a survey against the design as it now stands is
cheaper than keeping one written against a design that has moved. They were decision inputs for
[seams 1, 3 and 7](build-order.md#the-nine-seams).

| The question | What it found |
|---|---|
| **Build depth** — where does combinatorial depth come from? | Two structurally different routes, and **only the generative one is simultaneously a depth mechanism, an accessibility mechanism, and enumerable by the harness**. A one-wide corridor kills **one of eleven** mechanisms; what *nothing persists* removes is the onboarding ramp, and the fix is to move it inside the run |
| **The attacking half** — how is sending made deep? | Seven mechanisms, five survive, and the income loop the genre is built on is the one the single purse takes away. Defense-gates-offense has **one thin precedent, since removed** |
| **Why tower defense is fun** — where is the skill? | Six fun mechanisms, each of which **inverts into a known failure mode**. Skill comes from **eight axes**, of which this design was deleting two, inverting one and leaving a fourth unanchored |
| **Making the plan the game** — what carries a build phase? | Give away the mechanism completely and withhold the outcome. Perfect information about how the world works is what makes a plan a plan; perfect information about how it ends is data entry. What a 2.75 ms match can be spent on is the table below |
| **Towers, or placed squads?** | The aesthetic half is free and mostly decided; the mechanical half is one number — projectile volume — and it lands on `FlyProjectiles` rather than on target acquisition |
| **Creep wave variety** — has anyone upgraded creeps? | Variety is manufactured four structurally different ways and only **orthogonal properties that stack onto existing types** scales. A persistent creep upgrade tree has essentially one clean shipped example, *Tower Wars* (2012) |
| **Element TD's ancestry** | **No earlier Warcraft 3 map is on record as its inspiration** — every candidate the community names post-dates it, clones it, or belongs to a different subgenre. Element TD and Legion TD are opposite answers to *where does the decision live* |
| **Upgrade graphs in shipped games** | Landed whole into [ADR-0043](adr/0043-a-tier-is-its-own-id-and-its-own-row.md), [ADR-0044](adr/0044-a-new-unit-is-a-row-never-a-column.md) and [ADR-0045](adr/0045-the-ladder-is-a-graph-not-a-list.md), which carry the shapes to avoid and why |
| **Generated maps, and rotation** | Score generated maps by simulation — the standing objection, that simulating every candidate is too slow, does not apply at 2.75 ms. The cadence is not freshness against staleness but **the map against the ghost pool**, since a pool indexed by (map, stage) empties every time the map turns over |

### What a 2.75 ms match could be spent on

Fourteen uses of a re-runnable simulation as *design material* rather than as tooling, ranked by value per unit
of cost. Six have no equivalent anywhere in the genre — not because nobody thought of them, but because a game
built on frame-rate-dependent floating point cannot re-run anything and get the same answer twice. **None of
them is decided.** The list is here so the cheap ones are not reinvented and the expensive ones are not
stumbled into.

| | Mechanism | What it is, and what it costs | Lands at |
|---|---|---|---|
| 1 | **Both-columns sweep** | A roster where every unit has a strong *and* a weak column. A unit with no bad matchup fails the same way as one with no good matchup, so the target is not a flat win-rate table. Two CSV columns | Step 4 — the harness |
| 2 | **Solvability, measured** † | Sweep many good plans against a map and report the spread. Every competent plan scoring the same means the map is solved; plans that diverge mean it has decisions in it. The instrument the maze reversal needs, and it should exist before the map content does. The existing sweep, pointed at maps | Step 4 — the harness |
| 3 | **A distribution instead of a result** † | Your wave run against a sample of the field, reported as mean *and* spread, with the best and the average rewarded separately — peak play and robust play are different skills, and a player optimising one does worse at the other. SpaceChem's three competing metrics are the precedent. A sweep and two columns | Step 4, used at step 6 |
| 4 | **Give away the mechanism** | Full disclosure of how the world works, and none of how it ends. Nearly free once there is a screen, and the highest value per unit of effort on this list | Step 5 — the client |
| 5 | **True attribution** | Remove one tower and re-simulate; the difference is that tower's real contribution. It disagrees usefully with damage-dealt numbers, which reward whoever landed the last hit on something already dying. One match per tower — 33 ms on a twelve-tower board | Step 4 or 5 |
| 6 | **The computed highlight reel** † | The director knows the ending, so the moments worth watching are *chosen* rather than recorded — the tick closest to flipping, the first leak, the largest swing. A salience function over the event stream, and a camera that reads it. The presentation payoff of determinism | Step 6 onward |
| 7 | **Placement against the aggregate** | A histogram against everyone who reached the same stage, rather than a rank. SpaceChem's two stated reasons both apply: a leaderboard is a fantastic incentive to cheat, and for most players it only says that you are bad and not by how much. A count per bucket per stage | Seam 5 |
| 8 | **Retrospective review** † | Re-resolve every build phase against every alternative that was affordable at the time and report the swing. The most powerful teaching tool available here and the fastest route to a solved meta — report the swing and let the player judge it, never grade them with a number whose derivation they cannot see | After step 6, deliberately |
| 9 | **The paid oracle** † | Simulation sold to the player: three forecasts a build phase, a fourth costs gold, a coarse answer cheap and the distribution dear. No precedent anywhere, because no other game can afford to sell simulation by the unit. It makes *how much do I need to know before I commit* a purchase competing with a tower; the risk is analysis paralysis | Seam 1 decides; step 5 builds |
| 10 | **Par, computed** † | Sweep a stage and report the best line the harness found — difficulty derived from the game rather than asserted about it, refreshed when content changes, available before any player population exists. A floor on difficulty, not a truth | Opportunistic |
| 11 | **The ghost of your own best** | Trackmania's medal ghost, applied to a stage: your own best defense there, stored as an opponent. A graded solo ladder with no service behind it, and a floor of hand-shaped opponents that the cold-start problem needs anyway. A ghost record is a ghost record | Opportunistic |
| 12 | **The position as a puzzle you can send** | Records are content-addressed and self-contained, so any position is already a portable challenge — *here is the board and the budget I had at wave 12, beat my result*. A daily seeded stage is the same object with a schedule attached. A share button and a route | Opportunistic |
| 13 | **Commentary derived from events** | Match events are decorative and already emitted; a line of text on the three salient moments is the cheapest presence layer there is, and presence is what the social seam exists to manufacture. A template table | Seam 6 |
| 14 | **Counterfactual scrubbing**, and **the eval bar** | Hovering a purchase resolves the wave both ways side by side; a win-probability strip under the board re-simulates forward from each tick. Both are real compute during the watch and both want sampling or a price. The eval bar tells the viewer the ending early and can drain the tension it was meant to show — better as a post-match overlay than a live one | Interface work, unscheduled |

† No equivalent in the genre.

**Two decisions this puts in front of [seam 1](build-order.md#1--the-match-format).** *What can the player
compute before committing, and what does it cost them* — the answer decides whether the build phase is a set
of mechanics or a solver. And *is the round-robin's reward the best, the average, or both* — both is the
interesting answer, and it defines what the ladder measures and therefore what everyone optimises, so it is
not a UI decision.

## The questions

**Does the defending side have to be towers?** The alternative: **walls flanking the path as a placement
surface** — archers on a rampart beside the corridor — with squads that shoot, upgrade and get augmented. Two
parts are already settled. The walls **do not block**: they are a surface you place defenders onto, chosen for
how it looks, and do not alter the route. Squads are **static**: a stationary squad is a tower with a different
silhouette, and the moving-squad branch was priced and closed.

What survives is **projectile volume**. The ghost record costs nothing — a record stores inputs, and
projectiles are output — but every projectile resolves its target by a linear scan of the creep array every
tick it is in flight, so the term is **O(projectiles × creeps)** and the harness multiplies it by every match
it sweeps. **Modelling each archer as its own shooter buys nothing**: N archers on one cell share a coverage
interval, are handed the same target and never drift apart, so a squad is behaviourally identical to one
shooter firing N arrows *unless the bodies can die independently*. Attrition is the only thing that justifies
the expensive model, which turns a performance question into a design one.
[The survey](#what-the-design-research-found) recommended a scenery rampart with squads as one simulation
entity drawn as N bodies, and hitscan for fast squad weapons — with delivery kept as a *column in
`content/units.txt`* so projectile volume stays reversible per unit type. **Two independent lines — silhouette
legibility, and the attention budget of watching two boards — converge on squads being an archetype rather than
the model for the whole defense.** [Seam 1](build-order.md#1--the-match-format)'s to take or leave.

**What the seven tick-anchored sit-down rows point at now that a build opens on a run.**
[The sit-down](sit-down.md)'s rows 4 to 10 name a tick of `content/match.replay`, which the player opened on
until [#198](decision-log.md#14-august-2026-later--the-client-stops-opening-on-the-recorded-match) and no longer
does. Two answers, and both are cheap: **re-anchor them** to a round a run reproduces — which needs a committed
script, a seed and a landmark table for that round rather than for the recorded match, and buys a checklist that
walks the same path a player does — or **retire them** onto `LocomotionTests`, which already carries the
load-bearing half of rows 4 and 5 and is the reason those two stopped being judgements. The rows themselves are
not in question; every one of them names a real failure mode. Not blocking, and worth deciding the next time
somebody sits down with the build.

**What the gate rounds' remaining loose ends are, and what the defense currency is called.** Two of the four
readings here were settled on
[5 September 2026](decision-log.md#5-september-2026-last--sam-signs-the-roster-and-six-standing-proposals-move):
**a grant is one token**, which is what makes it three capstones a run, and **a capstone costs the token and no
gold** — charging gold on top would make the token a permit rather than a price, which is a different mechanic
with a different failure mode. Both are now rules in [the roster](roster.md#what-things-cost) rather than
readings.

Two are still open, and both are cheap to move while nothing is built. **The currency has no name**, and
everything player-facing here gets named deliberately — gold took two goes. **Whether a token banks** leans
toward yes, since a token that must be spent on the round it arrives forces the decision at the moment the run
knows least. A third is open for a different reason: **the capacity schedule** — the opening pair of two slots
and ten count, and the 2/4/6/8 and 10/20/30/40 steps — was deleted on 13 August and only the token half came
back, so it is a design waiting on a playtest rather than a reading waiting on a signature.

**Whether the wave is always on screen, or behind a control.** The
[chosen build-phase arrangement](build-order.md#7--the-interface) keeps what you are sending permanently
visible as a rail of portraits. Sam's remark on choosing it was that the sending is not the most important
part, and that it may end up behind a UI element — which is a real option and worth stating rather than
drifting into. **What it costs is stated too, because it cuts against a finding this project keeps making:**
[the sending survey](#what-the-design-research-found) and the
[13 August played run](decision-log.md#13-august-2026--the-first-run-played-by-a-person) both land on the
attacking half being the underweighted one, and a surface that is behind a click is a surface that gets used
less. The honest test is a sheet either way and a played round, not an argument.

**What a thumbnail is, now that a layout depends on one.** `RosterThumbnails` returns null and says so
deliberately — no per-unit image is committed anywhere, and both ways to close it are art decisions. The
chosen arrangement puts portraits at the centre of the build phase, so the seam is now load-bearing. The
mockups borrowed `tools/capture-armed-roster.ps1`'s framing — a three-quarter front at 215°, chosen to show
both hands — keyed and cropped square, which is a stand-in and not a decision. The two answers on file are a
committed image per live row, addressed by type id, or a bake with a camera, a pose and a framing chosen per
unit and a static entry point under `tools/`. Either way it is a look, and looks are signed off; each unit's
art direction is written on its **Looks** line in [the roster](roster.md).

**Co-operative play.** Wanted, and deliberately unstructured. Every other mode fits the submit-wait-resolve
loop; co-op may or may not, and it needs authored escalating content rather than player-composed waves, which
is a different content problem from anything else here. Revisit once seams 1 and 2 have resolved.

**The gamble.** Opting out of the field average to face a single opponent drawn from the distribution, possibly
choosing where in the distribution to draw from. The antidote to averaging making every round tend toward the
mean, with best-of-ten as its natural payoff. Not decidable before a real field exists at step 6.

**The paid predictor.** Named so it is not reinvented: an **average heatmap of where creeps died, layered onto
your own build**, aggregated over the simulated games. It needs per-cell kill attribution and a board to draw
on, so it is [seam 7](build-order.md#7--the-interface) and [seam 8](build-order.md#8--the-presentation) work,
and it is explicitly a thing to feel out in play. Until it exists, the round-robin's gold sink beyond ten
snapshots is the only paid information in the game. **The free-snapshot count and the price beyond it are sweep
parameters**, and the snapshot price is the first non-unit line in the cost column.

**Which towers carry which attack type, which creeps which armour type, and the `bonusVsTag` magnitude per
anchor.** Content, and [seam 3](build-order.md#3--the-roster)'s. 4.00× is a measured example, not a tuned
value.

**What the rotation cadence is, and how the pool survives it.** Faster rotation buys freshness against solving
and gives the whole player base one shared map to be compared on, but empties the `(map, stage)` ghost pool
every cycle — and the pool is what the async mode *is*. Slower rotation lets the pool fill and lets a map be
learned, which is most of where mastery would come from, at the cost of the map being solved before it turns
over. The three candidate answers are in
[§3](vision.md#the-board-is-a-maze), and what the survey found is
[above](#what-the-design-research-found). Not blocking until
step 6, since nothing before it reads a pool. **The rotation carries more than the map:** the
[gate schedule's *shape*](vision.md#the-gates) is on the same clock, so a cadence
choice sets how long a *preparation* problem stays learnable as well as how long a map does. Both want the same
answer — long enough to learn — which is a mild argument for slow.

**Whether a run carries a modifier, and what one would be.** A per-run mutator drawn at run start, changing one
rule for the whole run. Deliberately not opened: the field of ten is already the primary replay engine, since
your ten opponents differ every run, and a modifier pool is a whole system — balance interactions with
everything else, and a sweep that gains a dimension per modifier.

**How big the map archive has to be, and whether a map may ever repeat.** Whether the archive is large enough
that no player sees a map twice, or small enough that maps become known quantities with a metagame, is a design
choice and not a capacity one — and it is the cadence question viewed from the other end.

**Rating at two scales at once.** The pool is all players and the rivalry is a friend group. Whether those are
one ladder or two is unresolved.

### Is the field measurement kept, now that nothing prices off it?

**A decision for a human, raised by [#209](https://github.com/ssalter21/tower-defense-game/issues/209) and
deliberately not taken by it.** Gold is now paid for the health damage a wave does, so the payment reads no
distribution and no rank. That leaves `PerformanceField`, `Run.Field`, `Run.FieldSamples`, `MeasureField`, the
`run-measure/1` draw and the percentile lookup compiling, tested, and called by nothing —
[ADR-0042](adr/0042-the-field-is-measured-off-the-pool.md) is largely superseded, its own recorded cost
included.

**What keeping it costs is now nearly nothing, which is the surprise.** The measurement was lazy already and a
played round no longer asks for it, so the **half a run per run** the ADR records — the committed sweep going
from 9,600 matches to 14,400, and from about eight seconds to thirteen — is not being spent. What is left is
one measurement's worth of code that nothing exercises in anger, which is the ordinary cost of a capability
kept warm: it can rot without anything going red.

**What deleting it costs is the only answer on file to "where does this run sit against the field".** A placing,
a ladder, a percentile shown to a player, or a bonus that goes back to being relative all want exactly this,
and it is about a hundred lines with an ADR behind it.

**[#208](https://github.com/ssalter21/tower-defense-game/issues/208) has landed and did not wait on this.** The
pool is now a population per round and the measurement reads all of it at once, so the two are decoupled: the
answer here is still open, and taking it either way is still one measurement's worth of code. What #208 did
settle is the price of keeping it as it stands — the spread it reports is over a population no single round
fights, which is written into [ADR-0042](adr/0042-the-field-is-measured-off-the-pool.md)'s amendment. Anything
that gives the measurement a consumer has to pay that back by measuring per round.

**Three answers, and the middle one is not obviously wrong.** Delete it and take it back off git if it is
wanted. Keep it as it stands and accept untested-in-anger code. Or keep it and give it a consumer that is not
the purse — the sweep reporting where a run sat is the cheap one.

**Does a shareable browser replay viewer matter enough to move the simulation to Rust?** **Current assumption:
no — C# throughout.** It bears on [seam 6](build-order.md#6--the-social-layer), since a replay you can send
someone who does not have the game is a different artefact from one you watch in the client.

**What the cost rule does not price.** The placed-unit rule prices average damage, cooldown and the bodies a
shot hits; the walking rule prices health and armour points. It prices **neither range, nor bubble radius, nor
shield, nor duration** — and [#213](https://github.com/ssalter21/tower-defense-game/issues/213) has just made
range worth substantially more by tying it to elevation. That silence is deliberate and stays until the map
has been measured, because a coefficient guessed against the one-hex corridor is a coefficient priced against
geometry that is going away. **The silence is not a judgement that these levers are free.**

**The Mage's gap is now half-answered, and the open half is the price.** Settled on
[5 September 2026](decision-log.md#5-september-2026-last--sam-signs-the-roster-and-six-standing-proposals-move):
**the splash is authored** — a bubble on the target, radius 1000, damage payload — and **the 92 is not
touched**. The rule prices the row at 30 because `bodies` reads `targets`, which is 1; the row costs 92 because
92 was three bodies' worth of a splash. Authoring the splash makes the row do what it was priced for without
making the *rule* say so, since the rule counts `targets` and not bubble radius.

**What stays open is the number, and it waits on a tool rather than on a signature.** Repricing a row whose
value is a splash radius is exactly what the cost rule is worst at, so the price waits for the automated
balance sweeps to be trustworthy enough to derive it. Until then the gap stands, pinned in `ContentTests` with
both numbers in it, and it is the clearest single argument for the sweep-derived pricing this file already
contemplates.

**Whether a true stun is ever wanted.** A creep never drops below 10% of its authored speed, which is what
makes a match that cannot end unreachable by arithmetic rather than by careful authoring. It also means nothing
ever fully stops. Taking the floor out later is one comparison and no format version, but it puts back a hang
that any authored combination can reach, so it would want a stall cap of its own to replace what the floor was
doing.

**Whether effects need diminishing returns.** Effects are strongest-wins with the timer refreshed, so enough
uptime holds a creep at the floor indefinitely. With the floor in place that is a balance problem rather than a
correctness one. Diminishing returns is the standard answer and a real mechanic players learn; it costs a
per-creep counter and can be taken at any time, so it is not on the critical path of the migration.

**What a slowed creep looks like.** The contract is settled and the look is not. Timed effects landed in
[#217](https://github.com/ssalter21/tower-defense-game/issues/217) as internal state, visible in no `Snapshot`
field at all, and [#254](https://github.com/ssalter21/tower-defense-game/issues/254) answered the question of
*which field* the way that was open between "is it slowed" and "what is on it": a creep carries the two
percentages in force and the pool in front of its health, a tower carries the percentage its cooldown is
displaced by, and a magnitude is a displacement whose sign says which way — so one field covers a slow and a
haste both. They are snapshot fields and not events because a seek re-simulates and hears nothing, so an
event-driven tint would be right until the first drag of the scrub bar; the reasoning is in
[ADR-0007](adr/0007-snapshot-is-the-only-view-input.md) and the line from the other side is in
[ADR-0008](adr/0008-match-events-are-decorative.md). **What is left open is entirely the look.** The client
draws a wash of one colour per payload and a two-segment bar, photographed in
[`docs/frames/effects-roster-tick-0700.png`](frames/README.md); it does not say which way a speed moved, one
colour covers a slow and a haste, a tower carrying a modifier is not drawn at all, and the bar does not turn to
face the camera. Every one of those is a placeholder standing where a decision goes, and the decisions are
Sam's.

**Two halves of `bubbleMagnitude` went unimplemented, and together they are a column the signed table has and
the schema does not.** [#213](https://github.com/ssalter21/tower-defense-game/issues/213)'s column table reads
"`bubbleMagnitude` | A damage amount, or a percentage" and names five modifiable stats including damage.
Neither half of *damage* survived contact with the code, for a good reason each time and by a different ticket.
[#216](https://github.com/ssalter21/tower-defense-game/issues/216) declared a bubble one shot drawing one roll,
so a flat amount beside a `damage` payload would be a second damage source with a draw of its own
([ADR-0055](adr/0055-a-sweep-a-blast-and-an-aura-are-one-bubble.md)).
[#217](https://github.com/ssalter21/tower-defense-game/issues/217) found the keyword already taken — `damage`
means "the attack's own roll, spread" — so a damage *modifier* has no name left to be authored under
([ADR-0056](adr/0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md)).

Said plainly, so nobody has to reconstruct it from two ADRs: an author **can** spread the attack's own roll
over a sphere, apply a percentage to speed, cooldown or armour, and grant a shield. An author **cannot** write
a bubble dealing a flat amount, and cannot write a damage buff or debuff of any kind — including the "+x%
damage to nearby towers" shape the retired Captain was described by in the same table that authorised the
columns — and the shape the Cleric's Zeal wants now.

Each ticket recorded its own half in its own ADR; the sum was never put in front of anybody. The way out is
cheap and costs no format version, because it is a keyword rather than a column: a sixth payload value
distinguishing "the roll this attack made" from "the damage stat", at which point both halves come back. **What
it is not is an agent's to name** — a payload keyword is roster vocabulary. Until it is named, `roster.md`'s
column table says the schema is narrower than the decision rather than quietly restating the decision as the
narrowing.

**Its naming was deliberately deferred on 5 September 2026, with a reason.** The Cleric's capstone was the
first row that would have needed it: *Zeal*, every tower within two hexes dealing more damage. **Consecration
was signed instead** — an armour aura, authorable today — and Zeal is written into
[the roster](roster.md#consecration--tier-3--status-signed) as the *successor* rather than the alternative, so
it is not re-invented. Naming a payload word nobody is implementing this effort would be signing a word blind;
it gets named when it is built.

**Whether an aura may carry damage.** #217 refuses `bubblePeriod > 0` beside a `damage` payload at load, on the
argument that a pulse drawing dice outside a shot breaks the single-stream guarantee. The argument is sound and
the refusal may well be right. It is here because #213 permits a positive period beside any payload and says "A
whole-board pulse tower is one row", off Sam's own remark that a whole-board sweep "would, I guess, behave like
a pulse" — so the refusal closes a shape the decision opened. That shape survives as a period of 0, which fires
with the attack instead of pulsing. Striking the refusal is one line if a pulsing damage aura is wanted; what
it would then need is a stated rule for where its dice come from.

### Does the bot's value score divide by the gold it spends or by the gold it adds?

**Left standing by [#236](https://github.com/ssalter21/tower-defense-game/issues/236) rather than settled
inside it.** That ticket decided what a purchase on a covered route is worth — damage a tick, times the bodies
a shot hits, times the route hexes it reaches, per gold of the price difference — and `CoverThenUpgradeBot`
implements exactly that. The difference is the wrinkle: an upgrade costs its target's **full** price, which
`content/upgrades.txt` has said since the ladder was authored, so the number the score divides by is not the
number the purse hands over.

**What that buys is a stepping stone.** A 30-gold soldier stood on a good cell and turned into an archer in
the same round is 70 gold spent on a 40-gold archer, and the rule rates it highly because the second half of
it only cost 10 gold above what was standing. The run in `BuildPolicyTests` asserts the bot does this, so it
is visible rather than lurking.

**Two answers.** Divide by the gold actually paid, which is one number for both candidates and makes the
stepping stone score exactly what it is worth; or keep the difference, on the argument that what an upgrade is
worth is what it *adds* and the waste is a true thing about the rule that the report should carry. What
settles it is whether this bot is meant to model a player valuing a board or a player emptying a purse.

### Is a sweep row worth reading when the wall stops its creep outright?

**Raised by [#236](https://github.com/ssalter21/tower-defense-game/issues/236)'s regeneration.** A defense
that spends its whole share on a covered route now stops the light end of the roster: `content/sweep.csv` has
the skeleton scout dealing **0** over eight runs and the minion **2,073**, against 52,687 and 36,847 before.
A row of zeroes ranks against nothing, carries a cost efficiency of zero that means "never got through" rather
than "poor value", and cannot disagree with itself across seeds — `SweepTests` had to move its determinism
assertion up the roster to find a number that still moves — first to id 7, and then, once that row gained a
haste aura and began leaking in full, to the skeleton.

**It is a real reading of the board and not a broken harness**, which is what makes it a question. Three
shapes it could take: leave it and read a zero as the finding it is; play the sweep against a thinner wall so
that every row leaks something; or add a column that says what a row *survived* rather than what it dealt, so
a creep that never gets through is still ranked by how far it got. The last is the only one that does not
choose between honesty and signal.

### Should the Cursed Villager transform on damage, or on death?

**Raised by [#267](https://github.com/ssalter21/tower-defense-game/issues/267) building the trigger #250
signed.** The signed sentence is *the Villager transforms on first damage taken and cannot be one-shot*, and
the roster wrote a consequence beside it: *the pair is therefore worth 1800 + 2600 = 4400 effective health
always*. Those two do not both hold. A change that resolves ahead of the damage means the Villager's 1800 is
never spent — the roll that triggers it lands on the Werewolf — so a Villager is **2860 effective health for
11 gold**, which the cost rule prices at 18. The 4400 is what a change **on death** would be worth.

**The mechanic is built to the signed trigger and the arithmetic is corrected to match it.** What is not
decided is which of the two was wanted. On damage, the Villager is a wolf almost immediately and its own pool
is decoration; on death, it is a body that has to be killed twice and the first form is half of what was paid
for. They feel different and they price differently, and the second is a bigger creep than anything else on
the roster for eleven gold. **Nothing is retuned either way** — the price is derived, and a transforming pair
is the third thing the rule cannot see, beside the Mage's splash and the Vampire's shield.
