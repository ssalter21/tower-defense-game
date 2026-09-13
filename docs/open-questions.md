# Open questions

In scope, headed toward [the destination](vision.md#1-the-destination), not yet sharp enough to seam. A
question leaves this file when it is decided — into [the vision](vision.md), with the reversal, if any, in
[the decision log](decision-log.md).

## What the design research found

The nine surveys are retired and their verdicts are
[the design surveys digest](research/design-surveys-digest.md), with the fourteen uses a 2.75 ms match could
be put to. Nothing in it is decided.

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
[The survey](research/design-surveys-digest.md) recommended a scenery rampart with squads as one simulation
entity drawn as N bodies, and hitscan for fast squad weapons — with delivery kept as a *column in
`content/units.txt`* so projectile volume stays reversible per unit type. **Two independent lines — silhouette
legibility, and the attention budget of watching two boards — converge on squads being an archetype rather than
the model for the whole defense.** [Seam 1](build-order.md#1--the-match-format)'s to take or leave.

**The capacity schedule.** The capstone half of [the gate rounds](vision.md#the-gates) is decided and is in
[the roster](roster.md#what-things-cost) as rules: a grant is one capstone, a capstone costs the token and no
gold — charging gold on top would make it a permit rather than a price, a different mechanic with a different
failure mode — the currency is called a capstone, and it banks. What is open is the schedule itself: the
opening pair of two slots and ten count, and the 2/4/6/8 and 10/20/30/40 steps. It waits on the people
playtest — the playtestable build, alongside the balance sweep — and not on a run played on a branch, which
would be thrown out with them.

**Whether the wave is always on screen, or behind a control.** Reopened by the wheel. The
[chosen build-phase arrangement](build-order.md#7--the-interface) keeps what you are sending permanently
visible as a rail, and the wheel keeps the rail; but once a hex opens a wheel, the natural home for the wave
is a wheel of its own on the entrance hex, which is exactly behind a click. Sam's remark on choosing the rail
was that the sending is not the most important part, and that it may end up behind a UI element — which is a
real option and worth stating rather than drifting into. **What it costs is stated too, because it cuts against a finding this project keeps making:**
[the sending survey](research/design-surveys-digest.md) and the first played run both land on the
attacking half being the underweighted one, and a surface that is behind a click is a surface that gets used
less. The honest test is a sheet either way and a played round, not an argument.

**What a thumbnail is.** `RosterThumbnails` returns null and says so
deliberately — no per-unit image is committed anywhere, and both ways to close it are art decisions. The
chosen arrangement puts portraits at the centre of the build phase, so the seam is load-bearing. **The
wheel's MVP is wedges with words on them, so the seam stays load-bearing and stops blocking**: the wheel is
played before a portrait exists, and the portrait is signed after. The
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
mean, with best-of-ten as its natural payoff. Not decidable before a real field exists: a lobby is a field of
five, too few to draw from, so this waits on the round-robin's pool.

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
[above](research/design-surveys-digest.md). Not blocking until
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

**Rating at two scales at once.** The pool is all players and the rivalry is a friend group. The lobby is the
friend-group scale and is built first, with every player's bar shown and no rating at all; whether the two
scales are one ladder or two is unresolved until a pool exists.

**Does a shareable browser replay viewer matter enough to move the simulation to Rust?** **Current assumption:
no — C# throughout.** It bears on [seam 6](build-order.md#6--the-social-layer), since a replay you can send
someone who does not have the game is a different artefact from one you watch in the client.

**What the cost rule does not price.** The placed-unit rule prices average damage, cooldown and the bodies a
shot hits; the walking rule prices health and armour points. It prices **neither range, nor bubble radius, nor
shield, nor duration** — and elevation makes range worth substantially more. That silence is deliberate and stays until the map
has been measured, because a coefficient guessed against the one-hex corridor is a coefficient priced against
geometry that is going away. **The silence is not a judgement that these levers are free.**

**The Mage's price.** **The splash is authored** — a bubble on the target, radius 1000, damage payload — and
**the 92 is not touched**. The rule prices the row at 30 because `bodies` reads `targets`, which is 1; the row costs 92 because
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

**What a slowed creep looks like.** The contract is settled and the look is not. A creep carries the two
percentages in force and the pool in front of its health, a tower carries the percentage its cooldown is
displaced by, and a magnitude is a displacement whose sign says which way — so one field covers a slow and a
haste both. They are snapshot fields and not events because a seek re-simulates and hears nothing, so an
event-driven tint would be right until the first drag of the scrub bar; the reasoning is in
[ADR-0007](adr/0007-snapshot-is-the-only-view-input.md) and the line from the other side is in
[ADR-0008](adr/0008-match-events-are-decorative.md). **Nothing is drawn on a body carrying a modifier** — no
wash per payload, nothing for which way a speed moved, nothing on a body carrying two modifiers or on a tower
carrying one; which bodies an aura caught is read off the translucent circle it lays on the floor. **What is
still a placeholder is the bar**: two segments above the body, photographed in
[`docs/frames/effects-roster-tick-0700.png`](frames/README.md), which does not turn to face the camera and
whose segments are both shares of the authored health. That decision is Sam's, and so is how see-through the
circles should be.

**A damage payload has no name, so the schema is narrower than the signed column table.** An author **can**
spread the attack's own roll over a sphere, apply a percentage to speed, cooldown or armour, and grant a
shield. An author **cannot** write a bubble dealing a flat amount, and cannot write a damage buff or debuff of
any kind — the "+x% damage to nearby towers" shape the Cleric's Zeal wants. Two ADRs each close one half: a
bubble is one shot drawing one roll, so a flat amount beside a `damage` payload would be a second damage
source with a draw of its own ([ADR-0055](adr/0055-a-sweep-a-blast-and-an-aura-are-one-bubble.md)); and the
keyword `damage` means "the attack's own roll, spread", so a damage *modifier* has no name left to be authored
under ([ADR-0056](adr/0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md)).

The way out is cheap and costs no format version, because it is a keyword rather than a column: a sixth
payload value distinguishing "the roll this attack made" from "the damage stat", at which point both halves
come back. **It is not an agent's to name** — a payload keyword is roster vocabulary — and it is named when it
is built, not before: naming a payload word nobody is implementing would be signing a word blind. Consecration,
an armour aura authorable today, is the Cleric's capstone instead, and Zeal is written into
[the roster](roster.md#25--consecration--tier-3--status-live) as its *successor* so it is not re-invented.
Until the word exists, `roster.md`'s column table says the schema is narrower than the decision rather than
quietly restating the decision as the narrowing.

**Whether an aura may carry damage.** The loader refuses `bubblePeriod > 0` beside a `damage` payload, on the
argument that a pulse drawing dice outside a shot breaks the single-stream guarantee. The argument is sound and
the refusal may well be right. It is here because the signed column table permits a positive period beside any
payload — a whole-board pulse tower as one row — so the refusal closes a shape the decision opened. That shape
survives as a period of 0, which fires with the attack instead of pulsing. Striking the refusal is one line if
a pulsing damage aura is wanted; what it would then need is a stated rule for where its dice come from.

### Does the bot's value score divide by the gold it spends or by the gold it adds?

*Owned by [the sweep specification](specs/sweep-harness.md).* `CoverThenUpgradeBot` scores a purchase per gold
of the price *difference*, and an upgrade costs its target's full price — so a 30-gold soldier turned into a
40-gold archer in one round is 70 gold spent and scored as 10. `BuildPolicyTests` asserts the bot does this.
Divide by the gold paid, or keep the difference and report the waste? What settles it is whether the bot
models a player valuing a board or a player emptying a purse; the specification decides.

### Is a sweep row worth reading when the wall stops its creep outright?

*Owned by [the sweep specification](specs/sweep-harness.md).* The skeleton scout deals **0** over eight runs in
`content/sweep.csv`: a real reading of the board, and a row that ranks against nothing and cannot disagree
with itself across seeds. Leave the zero as the finding, play against a thinner wall, or add a column for how
far a row *survived* — the third is the only shape that does not choose between honesty and signal. The
specification decides.

### What is a spawner worth?

*Owned by [the sweep specification](specs/sweep-harness.md).* The Necromancer's 21 gold is its own 3380
effective health and cannot see the eleven Minions it raises before it leaks, so four hundred gold of them
returns **1200%** against a band of 60 to 95; a raised body's leak *is* charged, so the defending half is
honest and the sending half is unpriced. **What is settled:** the row keeps its derived 21, nothing is retuned
by hand, and the 1200 is the acceptance test for the sweep-derived rule
([ADR-0063](adr/0063-a-price-is-derived-from-the-row-and-its-silences-are-named.md)). **What is open:** the
term the derived rule gives a raise. It waits on a board worth deriving it against; the specification decides.

## What the drawn shapes left open

Three questions the roster's effects raised that are bigger than any one row; each waits on the billboarding
prototype, #290, which decides what the match may draw that is not a flat mesh, and each is Sam's to sign.

**The Mage's and the Sorcerer's splash draw the Mortar's burst, and cannot be told from it.** A signature is
chosen by the row the event names, and a blast centred on its target names the body the shot arrived at rather
than the shooter — so the burst is what *any* target-centred `damage` blast draws; the Unravel's carries
`armour`, which is what tells it apart. Three routes were tried and refused: putting the shooter on the event,
which [ADR-0008](adr/0008-match-events-are-decorative.md) exists to refuse; reading it off an earlier
`TowerFired`, which builds state out of an event stream that seeks discard; and inferring it from
`ProjectileSnapshot.TypeId` over the previous tick's shells, which is ambiguous when two shells arrive on one
body in one tick and reaches nothing for a hitscan blast. Either the two splashes are content with the burst,
or telling them apart needs something that is neither the row nor the payload.

**What ships for the Blessing is a circle on the floor and the word signed was "glow".** Nothing is drawn on a
blessed tower; what is drawn says where the blessing reaches rather than who got it. A lit floor and a body lit
from within are two different pictures and only the second is what "glow" plainly means — the same question
the real particle work will answer, rather than one to settle with another placeholder.

**The Overgrowth draws nothing, and nobody has checked that a board-wide hold is legible with no mark for it.**
It is the one aura on the roster with nothing on screen; the hold reads through the creeps not moving, or it
does not, and that is an eye check.

## What the playtest rebaseline leaves for a sitting

[The proposal](archive/playtest-rebaseline-proposal.md) put these beside their costs rather than deciding
them. Each is Sam's, and each is what a ticket on
step 7 has to carry before an agent starts. Plain words: a *metric* is one number a round or a run produces;
a *position* is where that number sits among the players in the lobby; the *release frame* is the frame of a
swing on which the shot leaves the hand.

- **Which metrics are scored.** Leak cost dealt, leak cost taken, health remaining or waves survived, gold
  unspent — all four, fewer, or others. Short term each is a bar on the end-of-round screen. Long term each
  is a thing a player optimises for, and a metric nobody can move is noise.
- **Whether the positions combine into one placing at the end of the run.** Opus Magnum never combines. A
  single placing makes a winner, which a room of friends will want; it also lets the offense decide the
  winner, which the old placing forbade.
- **Whether health taken stays the field average or becomes the sum of the waves.** Average keeps every number
  the sweep produced comparable. Sum makes a lobby of six twice as lethal as a lobby of three.
- **Whether a player sees the other boards before committing.** §3 calls this scouting in the lobby: the
  opponent's defense as of the end of the previous round, stale, never live. The folder makes it free; whether
  it is shown is a design choice.
- **What the wheel holds on an empty hex.** The nine roots, only the ones the purse affords, or the roots with
  the ladder reachable from each. Nine wedges with names fit; nine with ladders do not.
- **Where the wave is composed.** On the bar as today, in a wheel on the entrance hex, or both. One sheet
  either way. A wave behind a click is a wave composed less carefully, and the attacking half is already the
  underweighted one.
- **Whether the wheel opens on hover or on click.** Hover is what lights a cell today; a wheel that opens on
  hover covers the neighbours a player is about to look at.
- **Which two tower lines are animated first.** The recommendation is one melee line and one ranged line.
- **Whether the release frame sets the windup number, or the signed number picks the clip speed.** The first
  keeps the clip honest and moves sixteen content numbers again, with the golden and every dated picture. The
  second keeps the numbers and accepts that some swings play fast.
- **What a shot is allowed to be.** A model from the pack, a generated mesh, or a particle trail alone.
- **Which smoothing candidate ships, and how a level stays readable on it.** The skin or the pieces, from a
  sheet, with the contour or colour band asked of the same picture.
- **The words on the wrapper's screens, and which settings ship first.**
- **The sweep specification's five questions**, which are its own document:
  [`specs/sweep-harness.md`](specs/sweep-harness.md).
