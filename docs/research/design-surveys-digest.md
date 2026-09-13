# What the design surveys found

**Research digest** · nine surveys commissioned before 5 September 2026 and retired that day; the verdicts,
kept, and the one thing in them written down nowhere else

**Question:** what did each design survey conclude, once the working is thrown away?

**Inputs:** the nine retired surveys. They were decision inputs for
[seams 1, 3 and 7](../build-order.md#the-nine-seams); each verdict was taken into
[the vision](../vision.md), [open questions](../open-questions.md) or an ADR, and re-commissioning a survey
against the design as it now stands is cheaper than keeping one written against a design that has moved.

---

| The question | What it found |
|---|---|
| **Build depth** — where does combinatorial depth come from? | Two structurally different routes, and **only the generative one is simultaneously a depth mechanism, an accessibility mechanism, and enumerable by the harness**. A one-wide corridor kills **one of eleven** mechanisms; what *nothing persists* removes is the onboarding ramp, and the fix is to move it inside the run |
| **The attacking half** — how is sending made deep? | Seven mechanisms, five survive, and the income loop the genre is built on is the one the single purse takes away. Defense-gates-offense has **one thin precedent, since removed** |
| **Why tower defense is fun** — where is the skill? | Six fun mechanisms, each of which **inverts into a known failure mode**. Skill comes from **eight axes**, of which this design was deleting two, inverting one and leaving a fourth unanchored |
| **Making the plan the game** — what carries a build phase? | Give away the mechanism completely and withhold the outcome. Perfect information about how the world works is what makes a plan a plan; perfect information about how it ends is data entry. What a 2.75 ms match can be spent on is the table below |
| **Towers, or placed squads?** | The aesthetic half is free and mostly decided; the mechanical half is one number — projectile volume — and it lands on `FlyProjectiles` rather than on target acquisition |
| **Creep wave variety** — has anyone upgraded creeps? | Variety is manufactured four structurally different ways and only **orthogonal properties that stack onto existing types** scales. A persistent creep upgrade tree has essentially one clean shipped example, *Tower Wars* (2012) |
| **Element TD's ancestry** | **No earlier Warcraft 3 map is on record as its inspiration** — every candidate the community names post-dates it, clones it, or belongs to a different subgenre. Element TD and Legion TD are opposite answers to *where does the decision live* |
| **Upgrade graphs in shipped games** | Landed whole into [ADR-0043](../adr/0043-a-tier-is-its-own-id-and-its-own-row.md), [ADR-0044](../adr/0044-a-new-unit-is-a-row-never-a-column.md) and [ADR-0045](../adr/0045-the-ladder-is-a-graph-not-a-list.md), which carry the shapes to avoid and why |
| **Generated maps, and rotation** | Score generated maps by simulation — the standing objection, that simulating every candidate is too slow, does not apply at 2.75 ms. The cadence is not freshness against staleness but **the map against the ghost pool**, since a pool indexed by (map, stage) empties every time the map turns over |

## What a 2.75 ms match could be spent on

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

**Two decisions this puts in front of [seam 1](../build-order.md#1--the-match-format).** *What can the player
compute before committing, and what does it cost them* — the answer decides whether the build phase is a set
of mechanics or a solver. And *is the round-robin's reward the best, the average, or both* — both is the
interesting answer, and it defines what the ladder measures and therefore what everyone optimises, so it is
not a UI decision.
