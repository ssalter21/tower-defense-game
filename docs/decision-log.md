# The decision log

**Where [The Vision](vision.md) records the times it changed its own mind.**

A standing document that revises itself silently is one nobody can trust the age of. So every reversal lands
here, with what it said, what is true now, and why it moved — rather than being quietly edited out of the
vision and forgotten.

This file exists so the vision can stay readable. It grows; the vision should not.

**What is *not* here:** where the vision replaces a claim in one of the five archived deep dives. That is
[the archive index](archive/README.md#what-the-vision-overturns), because it is how you read
[`archive/`](archive/README.md) rather than a record of churn, and it is stable.

---

## Before 6 August 2026 — reading the finished skeleton

Three claims were written before the walking skeleton existed, and reading the finished skeleton changed them.

| Where | What it said | What is true now |
|---|---|---|
| **§5** — the harness | Sweep thousands of matches **overnight** | **Off by orders of magnitude.** `BudgetTests` measures the committed match at 2.75 ms — ~360 matches a second on one core. A sweep is a minute, so the harness is a `simcli` mode and a CSV, and it moves *before* the roster instead of after it. |
| **§8** — the seams | Eight seams ordered by what depends on what; the match format decided in full first | **Reordered, not repealed.** A dependency order does not say what is cheapest to *learn*, and the one untested claim in the whole design is that this is fun. The seams stand as destinations; the sequence in §8 is how they are approached. |
| **§8 seam 4** — the harness again | Depends on seams 1 and 3 | **Independent of both.** It needs a purse and a roster of any size, not a finished ruleset. |

---

## 6 August 2026 — six reversals

Made after [the skill note](research/fun-and-skill-expression.html) audited which of the genre's skill axes
this design could still charge the player for. Four of the six exist to buy back an axis the design had deleted
or inverted.

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **§3** — one purse | A single currency; the sharp decision the game is built around | **Reversed, then reversed back the same day — see the note below.** Two purses stood for a matter of hours; the settled answer is **one purse**, with the payback supplied by performance rather than by a second currency | A purchase that only subtracts has no timing question attached, and timing is what players practise |
| **§3 / §11** — the corridor | One hex wide, never branches; mazing and pathfinding permanently out of scope | **Reversed. A maze, deliberately hard to solve, at several elevation levels**, with elevation granting range | Geometry is the axis the genre was popularised on, and it was the largest deletion in the design |
| **§3** — send ordering | Rescued by the corridor, which *is* a single-file column | **Weakened, not repealed.** A branching map dilutes order; the map must now be designed to preserve it | Consequence of the reversal above, recorded rather than discovered later |
| **§3** — wave composition | The player composes the whole wave; a baseline wave was an open question | **Anchored.** A public schedule injects major variance at fixed, known waves | Without a public constant, preparation had nothing to be a skill about |
| **§3** — the offering | Not specified; the depth research ranked a private random offering third | **Public. Everyone sees the same options**, Mechabellum-style | A send is only a read if both players know the menu — and it makes the shop a second-order decision |
| **§2 / §3** — the async ghost | Opponents are drawn and their stored defense is what you compose against | **The round-robin no longer shows you a board.** It pays you in statistics over the field instead | A frozen defense cannot react or lie, so inspecting it produces a lookup rather than a read |
| **§2 / §3** — the map | One authored corridor, implicitly permanent | **Generated, and rotating daily or weekly**, selected by sweeping candidates for outcome spread | A hard map buys time; an unseen one buys it permanently — and the harness can already measure which is which |
| **Bottom line** | "the creeps you can send determined by the towers you chose" | **Dropped from the bottom line, still live in §3 as a direction.** A public shared offering is in tension with a private tower-gated pool, and seam 1 now owns the reconciliation | Recorded rather than silently cut |

### And the purse went back, later the same day

The first row above is a round trip, and it is left visible on purpose. One purse was reversed to two in the
morning and back to one by evening, decided in
[#72](https://github.com/ssalter21/tower-defense-game/issues/72).

**Nothing was wrong with the reasoning that produced two purses; it was answering a question that turned out to
have a cheaper answer.** The objection on file was never "one wallet is bad" — it was "attacking must pay you
back". A second currency pays that back through the economy's *structure*. Percentile bands pay it back through
the wave's *result*, and they cost one income rule instead of a whole parallel wallet with its own generator,
its own prices and its own balance surface.

Worth keeping for its own sake: **the two-purse decision survived less than a day of being written down, and
cost nothing but a section.** That is the sequence in [§8](vision.md#8-the-build-order) working exactly as
designed — a decision made on paper before any content exists is a decision that can be unmade for the price of
editing a paragraph. Had it been reversed after step 3, it would have cost a roster, a cost column and a record
format.

---

## 7 August 2026 — the documents were consolidated

Not a design reversal; a filing one, recorded here because it moved every document in the repository.

- **The five deep dives moved to [`archive/`](archive/)**, each with a banner saying what survived it. They are
  the reading the vision was built on, not the current plan, and several were still being read as the latter.
- **The reversal tables moved out of the vision and into this file.** They were 10% of it and growing weekly.
- **[`docs/README.md`](README.md) stopped carrying verdicts** and became an index. It was the last place still
  asserting "no mazing, ever" and "the hex corridor", which the maze reversal had contradicted the day before —
  the exact failure mode of keeping the same fact in two files.
- **`CLAUDE.md` became a pointer to [`AGENTS.md`](../AGENTS.md)**, and the 147-line hot-reload measurement that
  made up two thirds of it moved to
  [a research note](research/unity-hot-reload-timing.md). An agent file is loaded into every context; a
  measurement is not an instruction.
- **`tools/hotreload-probe/` was deleted.** It called itself throwaway, and the question it existed to answer is
  answered.

---

## 8 August 2026 — the roster is signed, and the clock slows

Decided in [#102](https://github.com/ssalter21/tower-defense-game/issues/102), which holds the reasoning for
each. [`roster.md`](roster.md) is where the units themselves live; this records only what changed its mind.

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **§3** — the currency | One currency, called **sauce** | **Renamed to gold.** Nothing else about the purse moved | No unit on the board could be named after sauce, and the roster is knights, archers, mages and skeletons. The word appears in no data file, so the rename retired nothing |
| **the unit table** | Ten rows, of which six walk | **Eight rows — five creeps and three towers.** `wisp`, `bulwark`, `lancer`, `sniper` and `sieger` are retired | The roster is scoped to the five creeps that were actually designed. The swarm and the wall were the two ends of the granularity axis and their loss is recorded rather than absorbed |
| **the attack types** | Assigned per tower — two impact, one pierce, one magic, for no recorded reason | **One attack type per tower line.** Soldier impact, Archer pierce, Mage magic | You can read what a tower does to a body by knowing which line it came from, and it fixes the lopsidedness by construction. It costs `sniper`, which was magic in a line that is now pierce |
| **the tower cost column** | Four prices — 40, 90, 200, 300 — following no written rule | **A basis: one gold per five damage a second, times the bodies a shot hits** | Three of the four live prices already obeyed it and nobody had written it down. Creeps are priced on the health a defense must spend; towers are now priced on the health they remove, so one purse prices both sides against one number |
| **the clock** | Creeps cross the 47-hex board in 18 seconds; the Archer fires five times a second | **Everything slows by three.** Durations ×3, creep speeds ÷3 | The pace was far faster than intended and nobody had looked at it in seconds. A uniform dilation changes the feel and nothing else — damage, health, range and every cost are untouched, and the wave resolves exactly as it did, over three minutes instead of one |

### What a uniform dilation costs, and what it deliberately does not

Worth stating separately because it is the one change here that touches every row and still changes no
balance. Cooldown, windup, backswing, flight and dying are durations in ticks; speed is distance per tick.
Multiplying the first group by three and dividing the second by three leaves **every ratio in the game where it
was** — the same shots land on the same bodies in the same order, and the committed run leaks the same thirteen
of forty.

The alternative on the table was to slow only the firing and leave creeps walking at 2.55 hexes a second,
compensating with much larger damage rolls. That was rejected: an Archer would need three shots to kill a
Minion where it now needs nineteen, so overkill would rise from a few percent to roughly a third and the
defense would get quietly weaker while the spreadsheet insisted nothing had changed.

**One honest imprecision.** 85 ÷ 3 is not an integer, so the new speeds are rounded — the Minion walks at 28
rather than 28.33. The leak may therefore land at twelve or fourteen rather than exactly thirteen. Exact
division would have meant slowing by five, which puts a ten-round run near fifty minutes.

### A correction to the roster document

[`roster.md`](roster.md) said [#91](https://github.com/ssalter21/tower-defense-game/issues/91) "already had to
cut `offering 3 3` to `offering 2 3` because the roster could not fill it". That is true of #91's own commit
and stops the story one commit early: `85fed39` put the offering **back to three** when the roster grew to ten,
and three is what `content/ruleset.txt` says today. Recorded here because the claim was load-bearing in the
argument for a larger roster, and it was wrong.

---

## 9 August 2026 — the upgrade edge is decided, and most of it was decided twice

Charted and worked as [#107](https://github.com/ssalter21/tower-defense-game/issues/107), which holds the nine
decisions taken before any ticket was written and the six that were taken in them. The vocabulary the map
turned out to need is in [ADR-0043](adr/0043-a-tier-is-its-own-id-and-its-own-row.md) through
[ADR-0046](adr/0046-an-absent-ladder-folds-nothing.md); this records only what changed its mind on the way.

**Five of the six resolved tickets reversed something, and three of the five reversed themselves rather than
each other.** That is worth saying plainly, because the map's whole premise was that a decision made on paper
before any content exists can be unmade for the price of editing a paragraph — and it was unmade five times
for exactly that price.

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **`roster.md`** — where the edge lives | Beside the cost table in **`ruleset.txt`**, and not a column in `units.txt` | **Its own file, `content/upgrades.txt`.** The instinct to keep it out of `units.txt` was right and the destination was wrong | `ruleset.txt`'s header states that *every row is required and none may appear twice*. It is a file of required, fixed-arity rules; an edge set is optional and variable-length — zero edges today, eight when the three lines are authored. Repetition was never the obstacle; `matrix` and `band` already repeat |
| **[#108](https://github.com/ssalter21/tower-defense-game/issues/108)** — what survives a swap | **Nothing but the cell.** A new row in the same place, no memory of what it was — the recommendation on the record | **Reversed by the human. The placement's identity survives**, as identity and not as a pointer: stats key on `(placement id, unit type id)`, so a Soldier-turned-Captain has two stat rows and one id | The game is data-driven and a Captain that was a Soldier for four waves has a career. Losing it on upgrade loses real data, and a `was` pointer would have bought the same answer with a reference that can go stale |
| **[#109](https://github.com/ssalter21/tower-defense-game/issues/109)** — precedent for that | *"Not one of the fourteen games stores a link back to what a tower used to be"*, so #108's answer *"has no direct precedent"* | **Both false.** A second research pass surveyed **Bloons TD 6**, which does exactly what #108 decided — mutates the existing tower, keeps its `ObjectId`, never resets `damageDealt` or `cashEarned`, accumulates `Tower.worth` | The first pass had not read BTD6. The correction strengthens #108 rather than changing it: the reversal was right and it now has the genre's deepest tower defense standing behind it instead of standing alone |
| **[#110](https://github.com/ssalter21/tower-defense-game/issues/110)** — which way a row reads | **Target first** — `follows <unit> <predecessor>`, sorted by target, and therefore append-only forever | **Overridden by the human. Source first**, `upgrade <from> <to>`, so a row reads as the act: *the soldier becomes the captain* | The cost charged against source-first during grilling was overstated — sorted by (`from`, `to`) the diamond's two rows come out **adjacent**, because a split's branches are authored at the same time and take consecutive ids. What remains is a real cost and it is recorded: a new tier is keyed on its parent's id, so it edits the middle of the file |
| **[#112](https://github.com/ssalter21/tower-defense-game/issues/112)** — how often the hash moves | **Twice** — once when the empty file lands, once when the first edge is authored — and goldens regenerate at each | **Once**, reversed by [#111](https://github.com/ssalter21/tower-defense-game/issues/111). An absent ladder folds nothing, so landing the empty file retires nothing at all | The first of the two moves was not merely wasteful, it was **illegal**, for a reason #112 could not see from where it stood. See below |
| **[#117](https://github.com/ssalter21/tower-defense-game/issues/117)** — fatal or advisory | A policy call, because *"a roster mid-edit may legitimately be in that state"* | **Not a policy call.** A fault is a red build gate, a note is a printed line, there is no third posture and no suppression mechanism | Every check a mid-edit roster could legitimately trip turned out to be **unstateable**. Three of the four candidate faults need a tier number, and no content file holds one — so the question dissolved rather than being answered |

### The collision that reversed #112, and why it was invisible from there

#110 folded `upgrades.txt` into the bundle that retires records, and #112 read that literally: an empty file
still hashes, so the hash moves the day the file lands. Read against the code, that move breaks something that
cannot be repaired.

`GoldenRecordTests.The_table_a_golden_was_recorded_against_is_committed_beside_it` **recomputes** a pinned
table's content hash and compares it against bytes frozen in the golden's header. The moment the *formula*
gains a ladder term, `content/golden/defense-0.replay`'s recomputes to a value that can never equal the one
frozen in it — and a version-0 bundle cannot be re-recorded, because the writer emits the current version and
only the current version. That is ADR-0009's rule broken by name: *a bump may retire a record, but it may never
retire the only evidence for a branch.*

Three ways out were weighed and two of them paid a price this repository has already refused. The third —
**an absent ladder folds nothing** — survives both, and is
[ADR-0046](adr/0046-an-absent-ladder-folds-nothing.md).

**Worth keeping for its own sake:** #112's reading was the conservative one, and conservative was wrong. "Move
the hash more often than strictly needed" looks like the safe direction right up until the thing it retires is
a file nothing can produce again.

### Three of four faults turned out to be sentences about a fact no file holds

#117 was written to decide whether a ladder checker should refuse an unreachable tier-2 tower, a skipped rung
or an orphan target, and whether refusing should be fatal. It could not, and the reason is upstream of it:
**`units.txt` layout 2 has eighteen columns and none of them is a tier**, and #110 kept a tier off the edge row
for its own reasons. So *"a tower above tier 1 with no incoming edge"* is not a check that was hard to write —
it is a check that cannot be spelled, because a live tier-1 tower and an unreachable tier-2 tower are the same
row with the same absence beside it.

What survives is two faults — **mixed roles** and **unequal roads** — and three notes, and the fatal-versus-
advisory question goes with the checks that could not be written. The honest cost is recorded rather than
glossed: a roster whose Marksman was never given an incoming edge passes everything, and that gap closes when a
tier number exists rather than when somebody writes a cleverer checker.

### A correction to #111's accounting

#111 concluded that on the day the first edge is authored, five files regenerate and *"nothing else moves"*.
That is right about generated content and it stops one reader short. The **view** parses `content/units.txt`
with the simulation's own parser and takes `content/match.replay` through `ReplayBundle.Replay`, which compares
the record's stamped hash against the parsed table's — so the player either ships `upgrades.txt` beside the
other five content files or its shipped record stops passing the gate, even though nothing on the view side
reads an edge. #112 ruled the file out of the streaming whitelist on the grounds that nothing on the view side
reads a ladder, which stays true and turns out not to be the question. Recorded here because it is the one
thing the map settled and did not notice, and because the test that presents the bill —
`MatchContentTests.TheShippedRecordPassesTheReplayGate` — is an engine-side test that only goes red in an
editor.

---

## 10 August 2026 — the defense becomes a decision, and the obvious bot was measured and thrown away

Charted and worked as [#142](https://github.com/ssalter21/tower-defense-game/issues/142), which holds five
scoping answers taken before any ticket and four decisions taken in them. The vocabulary the map needed is in
[ADR-0048](adr/0048-a-board-is-not-a-layout.md) and
[ADR-0049](adr/0049-a-placement-identity-is-derived.md); this records only what changed its mind on the way.

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **[#114](https://github.com/ssalter21/tower-defense-game/issues/114)** — how a placement is named | A placement has a derived id, which is *therefore* how a later row points at it | **Reversed by [#143](https://github.com/ssalter21/tower-defense-game/issues/143). An action names the hex.** The id survives, and nothing in any file ever spells it | A row naming an id is unreadable in isolation — you would have to have counted every prior row to know what `upgrade 3` points at. The id's one advantage, surviving a placement rebuilt underneath, is dormant while selling is out of scope |
| **`TowerCoverage`** — a tower that reaches no route | A refusal, in the same breath as off-the-map and inside-the-corridor | **Narrowed by [#144](https://github.com/ssalter21/tower-defense-game/issues/144). Still a refusal in an authored file; a *bad decision* for a placement made mid-run** | Off-map and in-corridor describe *impossible* positions; reaching nothing describes a bad one. Every other refusal in this repo is for something that could not have happened, and a player who builds where nothing walks has made a choice |
| **[#145](https://github.com/ssalter21/tower-defense-game/issues/145)** — what the sweep's bot ranks by | Best coverage per gold, which is what "a deliberately dumb bot" obviously means | **Reversed on an argument, restored by [#163](https://github.com/ssalter21/tower-defense-game/issues/163) on the measurement. Best coverage per gold** | The argument was pacing: three rangers cover all 47 route hexes of `content/map.txt` for 120 gold, so a per-gold rule has bought the whole route by wave 2, where walking up the price list spreads 420 gold of soldiers across most of a run. The sweep it produced separated no creep from any other — 22 creep rows, every one of them zero dealt gold and zero cost-efficiency — because a bot buying 14 range-1 soldiers pays triple for a worse wall than the one its opponents stand behind, and the wave attacks it on what is left. A board that changes is not what anybody reads `sweep.csv` for |
| **[#146](https://github.com/ssalter21/tower-defense-game/issues/146)** — which goldens retire on the format bump | An open question about `content/golden/`, assumed to have an answer | **Dissolved, then inverted.** #143 found `content/golden/` holds only replay bundles and **no golden command stream exists**, so nothing retires. The plan therefore *creates* one: today's version-0 `content/run.commands` is frozen as a golden before the bump | `RecordFormat` keeps a version-0 branch legal because *a golden record is committed against it forever*. Command stream version 0 had no such evidence, and after the bump would have had none at all |
| **`RoundOrders.ToString()`** | A spelling of "N towers standing", presumed printed somewhere | **Deleted.** Nothing in `sim/`, `simcli/` or `sim.tests/` calls it, and never did | A spelling nobody prints is one that can be wrong forever. Found by [#147](https://github.com/ssalter21/tower-defense-game/issues/147) while deciding what a round line says about a board |

### The sort that looks like ADR-0017 broken, and is not

[#144](https://github.com/ssalter21/tower-defense-game/issues/144) decided that the run holds placements in
placement order and *derives* a canonically-sorted `TowerLayout` per round. Read against
[ADR-0017](adr/0017-canonical-order-is-asserted-not-restored.md) — *canonical order is asserted at load, never
restored* — that is the prohibited move, and it is the first objection anybody will raise.

ADR-0017 is a rule about **stored records**: two identical records must not have two byte spellings, or
content-addressing one stops meaning anything. A run-built board is never stored as a layout. What is stored
is the command stream, and the stream keeps placement order — which
[#143](https://github.com/ssalter21/tower-defense-game/issues/143) had already made meaning-bearing, since the
ordinals depend on it. So the sort creates no second spelling of anything.

The rule that survives is shorter than either half of the argument: **assert what you read, compute what you
derive.** It is [ADR-0048](adr/0048-a-board-is-not-a-layout.md), written because the decision reads as a
violation right up until you know that a derived layout is a computation and not a load.

### The bot was chosen against geometry that is going away, and that is on the record

Every number this effort produces about placement is provisional by construction. Seam 9 replaces the one-hex
corridor with a maze at elevation, and on one-wide geometry a great many placements are equivalent — so the
build phase will feel thin until the maze lands, and the measurement above (three rangers, 120 gold, saturated
by wave 2) is a fact about *this* corridor and not about the mechanism.

Worth keeping for its own sake: **the corridor made the obvious bot rule useless, and the only way to find
that out was to compute the coverage table by hand before writing any of it.** The instinct — rank by value
for money — is right in a game where coverage is scarce. It is worthless in one where 120 gold buys all of it.

