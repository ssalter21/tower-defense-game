# Open questions

In scope, headed toward [the destination](vision.md#1-the-destination), not yet sharp enough to seam. A
question leaves this file when it is decided — into [the vision](vision.md), with the reversal, if any, in
[the decision log](decision-log.md).

## Research landed

**Nothing is in flight.** Every note commissioned against this file has come back. They are decision inputs for
[seams 1, 3 and 7](build-order.md#the-nine-seams).

| Note | What it found |
|---|---|
| [Build depth](research/build-depth-in-tower-defense.md) | Two structurally different routes to combinatorial depth, and **only the generative one is simultaneously a depth mechanism, an accessibility mechanism, and enumerable by the harness**. A one-wide corridor kills **one of eleven** mechanisms; what *nothing persists* removes is the onboarding ramp, and the fix is to move it inside the run |
| [The attacking half](research/attack-composition-and-sending.md) | Seven mechanisms for making sending deep, five survive, and the income loop the genre is built on is the one the single purse takes away. Defense-gates-offense has **one thin precedent, since removed** |
| [Why tower defense is fun](research/fun-and-skill-expression.html) | Six fun mechanisms, each of which **inverts into a known failure mode**. Skill comes from **eight axes**, of which this design was deleting two, inverting one and leaving a fourth unanchored |
| [Making the plan the game](research/planning-phase-and-simulated-stats.html) | How to elevate the build phase, and what a fast deterministic sim can be spent on as design material. Feeds [§9](vision.md#9-the-planning-phase-is-the-game) |
| [Towers, or placed squads?](research/towers-versus-placed-squads.md) | The aesthetic half is free and mostly decided; the mechanical half is one number — projectile volume — and it lands on `FlyProjectiles` rather than on target acquisition |

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
[The note](research/towers-versus-placed-squads.md) recommends a scenery rampart with squads as one simulation
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

**What the gate rounds' loose ends are, and what the defense currency is called.**
[Three gates](vision.md#three-gates-at-waves-3-6-and-9) fix the capacity schedule and hand out a capstone
token. Four things about them are readings rather than decisions, and all four are cheap to move while nothing
is built. **The currency has no name**, and everything player-facing here gets named deliberately — gold took
two goes. **The opening pair is two slots and ten count**, which is what makes the schedule 2/4/6/8 and
10/20/30/40; only the steps were specified, so the starting values are an inference from them. **A gate grants
one token**, which is what makes it three capstones a run. And **a capstone costs the token and no gold**,
which is the simplest reading and the one [the roster](roster.md#what-things-cost) is now written against —
charging gold on top would make the token a permit rather than a price, which is a different mechanic with a
different failure mode. Open beside them: **whether a token banks**, which leans toward yes, since a token
that must be spent on the round it arrives forces the decision at the moment the run knows least.

**Whether the wave is always on screen, or behind a control.** The
[chosen build-phase arrangement](build-order.md#7--the-interface) keeps what you are sending permanently
visible as a rail of portraits. Sam's remark on choosing it was that the sending is not the most important
part, and that it may end up behind a UI element — which is a real option and worth stating rather than
drifting into. **What it costs is stated too, because it cuts against a finding this project keeps making:**
[the sending research](research/attack-composition-and-sending.md) and the
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
[§3](vision.md#the-map-rotates-and-it-is-generated); the survey is
[Generated maps, and how often they turn over](research/generated-maps-and-rotation.html). Not blocking until
step 6, since nothing before it reads a pool. **The rotation carries more than the map:** the
[gate schedule's *shape*](vision.md#three-gates-at-waves-3-6-and-9) is on the same clock, so a cadence
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

**The correction that was owed is paid, and it left a row exposed.**
[#216](https://github.com/ssalter21/tower-defense-game/issues/216) made `bodies` read the `targets` column
instead of guessing 3 from `Delivery == Projectile`, so a Marksman is priced on arrival. What the guess had
been doing besides that is holding up the **Mage**: the rule prices it at **30 gold** and the row costs **92**,
because 92 is three bodies' worth of a splash the simulation has never had. `docs/roster.md` signs the splash —
one additional hex, radius 1000 — and `units.txt` layout 3 is the first schema that could carry it, as a bubble
on the target with a damage payload. **#216 authored no such bubble and moved no price**, because either is a
decision about what a Mage is; the gap is pinned in `ContentTests` with both numbers in it. Three ways out:
author the splash and accept an unpriced radius, reprice the row to what it does, or make it genuinely fire
three shots — which is a different tower.

**Whether a true stun is ever wanted.** A creep never drops below 10% of its authored speed, which is what
makes a match that cannot end unreachable by arithmetic rather than by careful authoring. It also means nothing
ever fully stops. Taking the floor out later is one comparison and no format version, but it puts back a hang
that any authored combination can reach, so it would want a stall cap of its own to replace what the floor was
doing.

**Whether effects need diminishing returns.** Effects are strongest-wins with the timer refreshed, so enough
uptime holds a creep at the floor indefinitely. With the floor in place that is a balance problem rather than a
correctness one. Diminishing returns is the standard answer and a real mechanic players learn; it costs a
per-creep counter and can be taken at any time, so it is not on the critical path of the migration.

**Nothing a view can see says a creep is slowed.** Timed effects landed in
[#217](https://github.com/ssalter21/tower-defense-game/issues/217) as internal state: they are folded into the
rolling state hash, where a run that drifts in one is caught, and they appear in no `Snapshot` field and in no
match event. That is deliberate — events are decorative by
[ADR-0008](adr/0008-match-events-are-decorative.md) and the snapshot is the view's only input by
[ADR-0007](adr/0007-snapshot-is-the-only-view-input.md), so adding either is a view contract and #217 was
about rules. It is a real gap all the same: the day a Cryomancer is signed is the day somebody has to draw a
slowed creep, and a creep that is walking at four tenths of its speed for no visible reason is the sort of
thing a playtest reports as a bug. The cheap answer is a field on `CreepSnapshot`; the question is which
field, because "is it slowed" and "what is on it" are different contracts.

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
damage to nearby towers" shape the Captain is described by in the same table that authorised the columns.

Each ticket recorded its own half in its own ADR; the sum was never put in front of anybody. The way out is
cheap and costs no format version, because it is a keyword rather than a column: a sixth payload value
distinguishing "the roll this attack made" from "the damage stat", at which point both halves come back. **What
it is not is an agent's to name** — a payload keyword is roster vocabulary. Until it is named, `roster.md`'s
column table says the schema is narrower than the decision rather than quietly restating the decision as the
narrowing.

**Whether an aura may carry damage.** #217 refuses `bubblePeriod > 0` beside a `damage` payload at load, on the
argument that a pulse drawing dice outside a shot breaks the single-stream guarantee. The argument is sound and
the refusal may well be right. It is here because #213 permits a positive period beside any payload and says "A
whole-board pulse tower is one row", off Sam's own remark that a whole-board sweep "would, I guess, behave like
a pulse" — so the refusal closes a shape the decision opened. That shape survives as a period of 0, which fires
with the attack instead of pulsing. Striking the refusal is one line if a pulsing damage aura is wanted; what
it would then need is a stated rule for where its dice come from.

### Does the scripted player's upgrade half need to know what a tower is worth?

**Raised by [#222](https://github.com/ssalter21/tower-defense-game/issues/222) and left standing rather than
fixed inside it.** `CoverThenUpgradeBot` covers the route by value — the type reaching the most unshot route
per gold — and then upgrades by **price alone**: the lowest-ordinal placement becomes the cheapest row dearer
than the one standing on it, whatever that row does. On the committed roster the archer costs 40 and fires
every 18 ticks and the mage costs 92 and fires every 54, so every upgrade the bot makes is a third less damage
a tick for more than twice the price.

**It was nearly invisible until the ghost got a purse.** A run's own purse rarely reaches the upgrade half at
all — there is always more route to cover — while a canned opponent opens behind a route covered end to end
and reaches it in round two. Now both walls of the report are built by this rule, and the report says so: the
committed run gets more past the ghost after it upgrades than before, and `content/sweep.csv` re-ranks the
roster by which armour class the mage's magic attack happens to meet. The measurements are in
[the 29 August decision-log entry](decision-log.md).

**It is also what caps the ghost's growth at round six.** Once the four archers are mages there is no dearer
row and no unshot hex, so rounds six to ten of every sweep are still a growing wave against a frozen wall —
the thing [#222](https://github.com/ssalter21/tower-defense-game/issues/222) set out to remove, removed for
half a run. That half is not a second ticket: it is this question, seen from the harness end.

**Three answers, and none of them is a tuning pass.** Upgrade by the same value rule the cover half uses, which
needs a per-gold score for a tower that covers nothing new. Refuse an upgrade that lowers damage a tick, which
is a rule about one column and would leave the mage unbuyable on this board. Or leave it, on the argument that
a deliberately simple bot is the point and the report already carries a note saying a row describes a game and
never skilled play. What settles it is what the report is for, which is a question about the harness rather
than about the bot.

**A second cap comes with it, and it is not the same question.** A rule that places only where nothing is shot
at stops placing the moment the route is covered, so a wall is capped at however many towers cover the board
— six here — however rich it gets. Redundant coverage is a real defensive move and this bot has no way to
make it.
