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
