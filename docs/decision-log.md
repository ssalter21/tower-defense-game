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

Made after [the skill note](open-questions.md#what-the-design-research-found) audited which of the genre's skill axes
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
cost nothing but a section.** That is the sequence in [the build order](build-order.md) working exactly as
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

---

## 11 August 2026 — the base is measured against the board it is actually spent on

Decided in [#165](https://github.com/ssalter21/tower-defense-game/issues/165), the first ticket of the
`played-from-a-shell` effort, because a build phase nobody has a reason to think about is not one worth sitting
at a prompt for. One number moved.

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **[`roster.md`](roster.md)** — what is missing | The opening purse, the income curve and the health pool were all tuned against a free six-tower defense, and **none of the three** had been measured against an empty opening board | **The income has been.** `content/ruleset.txt`'s `income` row is **168** rather than 100, swept at the committed shape; the purse and the pool are still where the free defense left them | A member of the canned field stands `content/defense.txt` for nothing and sends from behind it, where a run pays for its wall *and* its wave out of the base. At a hundred, all twenty-two rows of `content/sweep.csv` read `win_rate_bp` 0 and cost efficiency 0 to 5 — a report that separates nothing, and a build phase nobody has a reason to think about |

### The field was the other lever, and it was ruled out by measurement rather than by argument

`content/field.txt` is the opponent's *wave*, and what a run gets past an opponent is decided by that
opponent's *wall*. Thinning the field to five, two and one grunts leaves leak cost dealt at exactly the 86,
112, 243, 56 and 39 gold it was already at. The field moves what a run concedes and nothing about whether
sending is worth it, so the base was the only lever that could reach the column the sweep exists to produce.

**The window is narrow and it was walked.** At 160 the scout still wins one run in eight; at 200 four of the
five win every run they play. 168 is where the cost-efficiency column spreads 18 to 39 again, which is the
separating column `content/field.txt` was calibrated to produce.

### One honest imprecision, disclosed in the ruleset rather than smoothed

At eight runs a creep the five land between 37.5 and 87.5 percent, mean 55, against the 41-to-66 band
`content/field.txt`'s own header records. At thirty-two runs a creep three of the five sit inside that band,
and three other first seeds put the same five in the same order. It is a wider spread than the band claims;
`content/ruleset.txt`'s income block says so, in the file the number lives in.

**Worth keeping for its own sake:** the number nobody had looked at was the one holding the whole effort up.
The purse, the income and the health pool were priced against a board a run no longer opens on —
[#142](https://github.com/ssalter21/tower-defense-game/issues/142) made the defense a decision and emptied it —
and all three went on reading as settled, because each of them was individually defensible and nothing made
the three of them one question.

---

## 13 August 2026 — the first run played by a person

The `play` verb ([#176](https://github.com/ssalter21/tower-defense-game/pull/176)) put a build phase in front
of a person for the first time. Most of what it found is about **ordering** rather than about design, and one
of the reversals is of this file's own companion.

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **[The build order](build-order.md#the-sequence)**, on why step 5 is fifth | *Is the economy tense, is composing a wave interesting, does send order matter, is the roster varied* — "every one is answerable from a command line and a spreadsheet" | **Half of them are.** The economy and the roster answered from a shell. **Composing a wave and send order did not**, and the reason is structural rather than a gap in the verb | The CLI will never carry the visual elements — video replays, range indicators — and building them there is not worth it. What a person would learn from them there, the simulation can compute and summarise instead |
| **[§3 — three anchors, a shape and a filling](vision.md#the-gates)**, and the take gate with it | A public anchor schedule injects major variance at fixed known waves; one take per round, mandatory, bounding what may be fielded | **Deferred, not repealed.** Both come out of the played game until the roster has the depth to make a gate worth having | *There is no point in gating before we have the depth, it just holds back the early testing and experience.* The destination is unchanged; what moved is when it is built |
| **§3 — one purse** | The economy is the sharp decision the game is built around | **Not tense yet, and that is accepted rather than fixed** | Attack performance is rewarded, so spending on attack is the long-term investment and nothing argues against it. The expected correction is already in the design: health falls, and the run eventually has to pivot its spending. Not worth tuning before the roster can be judged |
| **[Open questions](open-questions.md)** — is placement worth having on this geometry | Thin until the maze lands, per [#142](https://github.com/ssalter21/tower-defense-game/issues/142), and possibly not worth the tickets | **Placement earns its place**, and is expected to get more interesting as elevation lands | Played rather than argued. This does **not** settle the separate question of whether the defending side has to be *towers* — squads on a rampart are still live |

### The build phase is not yet a decision worth making, and the roster is why

The question the specification was written around got a *not yet*, with a reason that is not about the build
phase: **composing a wave is not interesting enough, and there is not enough depth to make it so.** What it
wants is plenty of money to spend and options worth spending it on, and **neither can be judged against the
current roster** — six walkers and four towers, four of which are equivalent on a one-hex corridor.

That makes [seam 3](build-order.md#3--the-roster) the blocker on answering the question at all, ahead of the
interface work everybody expects to be next.

### What the CLI is for, stated as a test

The finding above is easy to over-read into *stop investing in `simcli`*, and that is not what it says. The
divergence is **visual and spatial**: replays and range overlays are read off a picture, a terminal cannot
carry one, and a person judging *feel* from a terminal is judging something the player will never see.

So the test is: **is it a picture, or a number?** Pictures belong in the client at step 5. Numbers the
simulation already computes are fair game at a prompt, which is why *what the wave is walking into*
([#181](https://github.com/ssalter21/tower-defense-game/issues/181)) and *how the last round went*
([#182](https://github.com/ssalter21/tower-defense-game/issues/182)) are not ruled out by this. Two CLI use
cases are unaffected either way: the simulation running itself, and debugging.

### The gate cannot be switched off in content, and that is worth knowing before anybody tries

Deferring the anchors is not an edit to `content/schedule.txt`. `sim/AnchorSchedule.cs` refuses a schedule
with no `anchor` rows, and refuses a changer pool no anchor draws from, so deleting the twelve placeholder
game changers makes the file unloadable rather than empty. The deferral is a code change with a content
change behind it, which is why it is
[#179](https://github.com/ssalter21/tower-defense-game/issues/179) and not a commit.

## 13 August 2026, later — the gates come out, and the client comes before the roster

A grilling the same evening, against [#183](https://github.com/ssalter21/tower-defense-game/issues/183) and
[#180](https://github.com/ssalter21/tower-defense-game/issues/180). **It reverses the entry above, which is a
day old**, and the reversal is about sequence rather than about design: nothing here says the roster is deep
enough, only that it is not the next thing built.

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **[The build order](build-order.md#the-sequence)** | Step 3 is being revisited before step 5 | **Step 5 is next.** The roster work is parked behind a playable client | *I'm really focused on wanting to get the game to be playable and viewable in Unity so I can start getting a real look and feel. That's the priority.* The roster finding stands; what it is worth waiting for does not |
| **The entry above**, on deferral | The take gate and the anchor schedule are deferred | **They are deleted, not switched off**, and the per-wave type limit goes with them | A mechanic carried through the client build switched off is a tax on every step of the thing actually wanted. It is on the record in git and in the ADRs, so bringing it back is reading a diff rather than redesigning |
| **[`content/upgrades.txt`](../content/upgrades.txt)**, and [ADR-0045](adr/0045-the-ladder-is-a-graph-not-a-list.md) / [ADR-0046](adr/0046-an-absent-ladder-folds-nothing.md) with it | The simulation never reads the ladder; an edge is an annotation nothing in a tick loop can observe | **The build phase reads it.** A unit that is some edge's target cannot be placed directly — it must be upgraded into | The ladder was always meant to be a prerequisite chain. *The idea is you have to buy the Archer as a precursor to the Ranger* |
| **[`ruleset.txt`](../content/ruleset.txt)**, the health pool | 1500, flagged as an open decision wanting something nearer 450–500 | **800** | Halfway. The pool has to bite for a concession to be a decision, but not so hard that a run being tested reads as *I died* when the question asked was *was that wave interesting* |

### Four things were being called one thing, and only one of them survives

The word *gate* was doing four jobs. Separating them is most of what this conversation was:

| | What it was | Verdict |
|---|---|---|
| **The forced pick** | One thing must be unlocked from the menu every round | **Deleted** |
| **The menu** | Three of the roster offered each round, drawn at random | **Deleted** |
| **The special rounds** | Waves 3, 6 and 9, a wider menu, twelve placeholder major options | **Deleted** |
| **Types per wave** | A wave may carry at most two creep types, widening only because the special rounds widen it | **Deleted** |
| **The upgrade prerequisite** | An Archer must stand before a Ranger can | **Kept, and newly enforced** |

The fourth is the one nobody had noticed was load-bearing. Slot width is derived — the starting width plus a
step for every anchor at or before the round — so deleting the anchors would have frozen every wave of every
run at two types, and no amount of roster depth reaches that. It is deleted outright instead: a wave may carry
whatever it can afford.

`ruleset.txt` loses its `slots` and `offering` rows, `content/schedule.txt` goes, and `sim/AnchorSchedule.cs`
goes with them. The take comes off every build row, which is a **command stream format 2** — reader branches
kept for 0 and 1, a new golden frozen, exactly as format 1 was done.

### The Ranger was not mispriced, it was unreachable-by-design and reachable-in-fact

[#180](https://github.com/ssalter21/tower-defense-game/issues/180) reported the one upgrade edge as dominated:
Archer and Ranger both cost 40, and upgrading pays the target's full price, so taking the edge cost 40 gold and
an Archer where placing cost 40 gold alone.

Measured while grilling it, the defect is worse than reported and the fix is smaller. The two rows are
identical in damage, cooldown, windup, backswing, delivery and cost, and the Ranger has 1,000 more range — so
the Ranger does not merely dominate the *edge*, it dominates the *Archer*, and the tier-1 row is dead content
from the moment the tier-2 is placeable.

**Neither price moves.** A tier that is strictly better than the rung below is what a tier is; what was missing
is that the rung below is supposed to be a prerequisite. A Ranger costs 40 for the Archer plus 40 for the
upgrade, and `roster.md`'s standing note that the equal price *is the rule rather than a mistake* survives
intact — as does the cost rule's deliberate silence about range.

### What the roster work becomes, and what it does not

[#183](https://github.com/ssalter21/tower-defense-game/issues/183) is parked rather than answered, and its
shape changed while it was being parked:

- **Depth comes from upgrading creeps** — stat and speed upgrades applied to the rows that exist — rather than
  from authoring new unit types.
- **Design vocabulary is the levers and never a category.** Speed, health, armour. *Fast and cheap* and
  *expensive and tough* are the two ends of one axis and do not need names; the words **swarm** and **wall**
  are rejected in the same way [§12's *ordinary* and *game changer*](vision.md) were.
- **An arcane shield is expected**, and it is both things at once: a pool a creep can carry in its own right,
  *and* something the Necromancer grants to creeps that enter its range which would not otherwise have one.
  The second half is the aura `roster.md` calls the largest engine ask on its page.
- **No new tower rows.** Five of the six proposed towers are blocked on levers the schema lacks, and none of
  them is what the playtest complained about.

### A column is cheaper than the files say, and this is the cheapest it will ever be

`units.txt` warns that a new column costs *a format version, a hash-layout bump and a retired ghost pool*.
Two thirds of that is currently free, which is worth knowing before the arcane shield is priced as expensive.
`UnitTypeTable` carries a reader branch per layout and both 1 and 2 still load, each folding under its own
hash label, so stored records keep replaying. And the retired ghost pool is real but **empty** — there is no
stored pool yet, only one canned field. What a column actually costs today is a reader branch and re-recording
the committed content.

### The Skeletons pack has six models and the roster document says four

[`docs/roster.md`](roster.md) states that KayKit's four skeleton models are exactly spent. The repo's own
character roster note listed **six**, and the two unnamed there are a
dedicated **Necromancer** and a **Skeleton Golem**. The count is corrected; no assignment moves. The Minion
and the Skeleton sharing a base model was never the shortage it looked like — the Skeleton is that model with
shield and sword, which is a kit variation the pack ships weapons for, and `roster.md` said so all along.

Neither pack is on this machine. `client/Assets/Art/Characters/` holds two FBX files, so every skin assignment
on that page is a plan rather than something anyone has looked at, and they are assigned for real once the
packs are downloaded.

---

## 13 August 2026, later still — the client is grilled, and the wave turns out to have been a bag

Grilling [#190](https://github.com/ssalter21/tower-defense-game/issues/190) against the standing documents
turned up one defect, reversed two recommendations that had been made against a smaller version of the ask,
and deferred most of a page.

### A wave was always a sequence, and the build phase quietly made it a set

The vision says it outright, under *You choose the order they come out in*: **a wave is a sequence, not a
bag**, and `content/wave.txt` has always been an ordered list of `(tick, type, count)`. `BuildPhase.Resolve`
did not honour it. Every slot was given the same release tick, so a wave's columns all began together and a
slot's position meant nothing — and the rule that filled slots must **ascend strictly by type id** existed
precisely because the arrangement was not a decision, canonicalising it so two identical waves could not have
two spellings.

**Nothing in the vision moves.** This is the implementation catching up to it: a slot's position becomes its
release offset, the ascending rule comes out, and the player arranges the wave by dragging. The vision's two
stated preconditions are already met — the corridor is single file, and *a count is a column, not a pile*.

What it costs is not small and is named here so nobody discovers it in the middle of the client work: the
ordering rule is deleted from the three places [ADR-0039](adr/0039-the-command-stream-is-the-only-route-into-a-run.md)
deliberately wrote it, the command stream goes to **format 3**, and every committed golden re-freezes —
`run.commands`, `run-outcome.txt`, `golden-trace.txt`, `match.replay`, `sweep.csv`, and `BudgetTests`'
calibration tick with them. Balance moves too, because waves arriving in sequence are a different defensive
problem from waves arriving together, so the roster is priced against a game that no longer exists and the
sweep harness is pointed at it afterwards rather than before.

### The interactive verb is not what the shell is for

`simcli play` was built so the build phase could be judged a fortnight before a client existed, and it did
that. But the shell's standing purpose is **mass headless simulation** — pricing the roster by computing it —
and `play` is the only one of eight verbs that takes human input. The balance work needs none of it.

So **`play` is deleted**, and deliberately last: the proving machinery moves into `sim` first, the client is
made to write command scripts, `play-run` is confirmed to replay one headlessly, and only then does the verb
go. Done in that order nothing is lost, because reproducing a run without opening Unity survives in `play-run`.
[The shell specification](archive/playing-a-run-from-a-shell.md) is archived in the same commit; its sections were
load-bearing only while the tests that pinned them existed.

Two things about the sweep harness that were assumed missing and are not: a **complete automated player
already exists** — `EvenShareBot` and `CoverThenUpgradeBot` behind the `BuildPolicy` seam, playing full
ten-wave runs with no human in them — and the throughput was settled on 6 August, at 2.75 ms a match. Half a
million runs is under four hours on one core, inside the existing `--runs` ceiling, with no code change. What
is actually missing is per-run output, parallelism, CLI access to the policy, and checkpointing; that is
[its own ticket](https://github.com/ssalter21/tower-defense-game/issues/190) and not this effort's.

### Two recommendations reversed by what the ask turned out to be

Both were made when the client's interface was going to be small, and both would have been thrown away.

- **Orthographic to perspective.** Zooming an orthographic camera crops; it does not take you into the scene.
  The ask is to orbit freely, go in close and look at a fight, which orthographic cannot do. The board stops
  being isometric-exact — under perspective the far end of the corridor converges — and `SceneFraming`,
  `CameraRigTests` and the frame-capture entry point all move with it. The six yaw snaps are deleted; one key
  eases back to a default angle.
- **uGUI to UI Toolkit.** A header, a palette, a thumbnail-carrying wave bar and drag-to-rearrange is a real
  interface, and a code-built uGUI version of it is a thing that gets rewritten. `PlaybackControls` ports
  across in the same effort rather than leaving two UI systems in one scene.

### What the first playable run does not have

Health is the only number the header carries beyond wave, gold and slots. **Leaks, kills and per-tower damage
are all out** — the first two exist inside a `Match` and are discarded by `Run`, and the third does not exist
at all: `Damage()` is not passed the tower id, and for projectile towers the shooter is not recoverable from
the snapshot or the events. Building that is the after-action effort, which is data-heavy, driven by the
vision, and deliberately not smuggled into a skeleton.

Also out, and additive later: the maze, roster depth, save-and-resume, and any framing of the field beyond a
number. **No forecast, in any mode** — prevention on screen covers what the rules refuse and stops there,
because a placement greyed out for being unwise is a computed outcome wearing the clothes of a rule.

### The art packs have been on the machine since 8 August

`roster.md` still said *"neither pack is on this machine"* and called every skin assignment a plan. The
complete collection has been in `Downloads` since the 8th, catalogued from the zip itself in
[the collection inventory](research/kaykit-collection-inventory.md) — 22 packs, CC0, 61 rigged characters,
159 clips. The assignments are adopted as written, with the Necromancer keeping **`Skeleton_Mage`**; the
dedicated Necromancer model the inventory found is not taken up.

Two rules are added that the page did not have. **Scale is the tier signal**: towers at 1.0, all creeps at
0.5, the Ranger at 1.5 so it does not read as its own tier-1. And **scale lives in `MatchArt` and never in
`units.txt`** — visual size is a view fact under [ADR-0007](adr/0007-snapshot-is-the-only-view-input.md), and
putting it in the content tables would make every art tweak a format version. The numbers are expected to move
once they have been looked at, which is the point of storing them somewhere free to change.

There is no plinth and no rule about which units are people and which are buildings. Size is the whole
differentiator.

---

## 13 August 2026, later still — the gates are actually out, and the ladder becomes the rule it was an annotation to

Implementing [#179](https://github.com/ssalter21/tower-defense-game/issues/179). The schedule above says the
gates come out; this is what came out with them, and the two standing claims that turned out to be false once
they had.

### One prerequisite replaced four, and it was already in the repository

`AnchorSchedule`, `AnchorFilling`, `Offering` and `Unlocks` are deleted, and `ShotBonus` and `Draws` with them
— nothing counters anything once there are no anchors, and nothing draws once there is no offering.
`content/schedule.txt` is gone, taking twelve placeholder names with it.

What is left is `content/upgrades.txt`, which was written as an annotation nobody read. It is now the rule:
**a unit some edge points at may not be placed**, and is reached by standing the rung below it and upgrading
into it. Refused rather than priced, because a tier that can be bought without the tier under it is not a
tier, it is a second row at a higher price.

| Where | What it said | What is true now |
|---|---|---|
| `content/upgrades.txt` header | *"THE SIMULATION NEVER READS THIS FILE."* Held as a property rather than a promise: the parsed ladder lived on the command line and was never handed to a run | **Overturned.** A run is handed a ladder and `BuildPhase.Resolve` asks it what may be placed. An edit to the file now retires stored records, which is why a command stream stamps a ladder hash |
| [ADR-0046](adr/0046-an-absent-ladder-folds-nothing.md) title | *"…the content hash covers content the simulation never reads"* | **Second clause overturned; the decision stands and matters more.** Folding the ladder into `types.ContentHash` is what retires a record made under a different ladder. An empty ladder still folds nothing, so `content/golden/defense-0.replay` is still legal forever |

[ADR-0036](adr/0036-the-anchor-schedule-is-a-shape-and-a-filling.md) and
[ADR-0037](adr/0037-the-offering-is-public-because-it-is-derived.md) describe machinery that no longer exists
and carry superseding notes. What survives from them is worth keeping: a content file's constraints belong in
its loader, and a derived thing needs no stamp because its inputs are already stamped. Both still hold, for
the ladder.

### Types-per-wave was load-bearing and nobody had said so

Wave slot widths were derived from how many anchors a run had passed — `2 2 3 3 3 4 4 4 5 5`. Deleting the
anchors alone would have frozen every wave at two types forever, so the width came out too. **What bounds a
wave now is the purse and nothing else**, which is the only thing a player is spending against.

### Three consequences that are not corrections

**The sweep's `ingredients` axis is gone.** It binned a creep's runs by how many distinct creeps the run ended
able to field, which varied *only* because the take rationed sending. Every run can send the whole roster from
wave one, so the axis is one value wide and separates nothing.

**Health 800 makes the death flag live.** The reference player in `TheRun.TheCommittedRun` — build a wall, then
shop — now runs out of health in its fourth round where 1500 let it survive ten. That is a better test than the
old one rather than a worse one: the four rounds it shares with the no-death vector are identical gold for
gold, so the flag demonstrably stops the loop without touching anything inside it.

**The sweep's defense never builds a ranger.** `CoverThenUpgradeBot` cannot place one, and its upgrade half
climbs to a strictly *dearer* row while the ranger costs exactly what the archer costs — so nothing it does
reaches the roster's one upgrade edge, and a balance question about the ranger cannot be answered from
`content/sweep.csv`. That is the bot's rule rather than the ladder's, and it is written down here because it is
the kind of hole that reads as a bug in the report six weeks later.

---

## 13 August 2026, last of the day — a wave becomes a sequence, and the version-bump trigger turns out to have a hole in it

Implementing [#191](https://github.com/ssalter21/tower-defense-game/issues/191), which the client grilling
above turned up. The vision always said a wave is a sequence and not a bag; `BuildPhase.Resolve` gave every
slot the same release tick, so it was a bag.

### A slot's position is its release order, and the wave is one column

Slot one's creeps walk out first, slot two's fall in behind them, and an order's offset is one spawn interval
per creep ahead of it — so the whole wave is a single column at a single cadence rather than several columns
starting together. The vision's two stated preconditions were already met: the corridor is single file, and *a
count is a column, not a pile*.

**The strict-ascending-by-type-id rule is deleted from all three places** [ADR-0039](adr/0039-the-command-stream-is-the-only-route-into-a-run.md)
wrote it. It existed to canonicalise an arrangement that was not a decision; once position is the release
order the arrangement *is* the decision, and asserting an order over it deletes the lever. What survives is
the half that was never about canonical bytes: **a creep fills at most one slot of a wave**. Canonical bytes
are not lost — every filled slot sends at least one creep, so the offsets ascend strictly and a wave's orders
are still unique and ascending on `(tick, type)`. The ordering became a consequence of the rule instead of a
rule of its own.

Command stream to **format 3**, simulation version to **3**. The format bump carries no field: a version-3
command is byte for byte a version-2 command, and only the stamp separates two identical byte runs describing
two different fights. That is what a format version is for, and the alternative is a stream that replays into a
confidently wrong result while passing every gate. `content/golden/command-2.commands` is frozen beside the
other two, read forever and replayed never.

### The behaviour fingerprint could not see this, and that was not obvious

`DerivationTests` folds a fixed scenario into one number and refuses to let the simulation version move without
it. Under the fold as it stood, this change produced `42346EF613910009` — **byte for byte version 2's**. The
scenario hands the match a wave written out in the test file, so folding a match says nothing about how a wave
is *composed*, and the rule that moved lives in `BuildPhase.Resolve`.

A row whose evidence equals its predecessor's is not evidence. The fold gained a second half — the tick, type
and count of every order a fixed build phase composes — and the label went `rule-fingerprint/1` →
`rule-fingerprint/2`, which retires versions 1 and 2's recorded values as uncomputable by this build rather
than leaving a table that quietly compares fewer things. Reverting the release rule now moves the number
`97AE0A007D5A9AB9` → `D5B62912DBA14BFA`, which was watched rather than reasoned about.

### Three consequences

**The fight moved, and the committed run is the measurement.** Same script, same seed, same defense: 334 gold
dealt over ten rounds became 261. Nothing was retuned. Creeps arriving in sequence are a different defensive
problem from creeps arriving together, so **the roster is priced against a game that no longer exists**.
Re-pricing is not this ticket.

**The sweep cannot see this change at all, and its report says so.** `content/sweep.csv` regenerated byte for
byte identical, because `EvenShareBot` fills one slot a round — a sweep row is about one creep, so its wave is
one column and the only arrangement there is. That is correct for attributing a row to a creep and it is a
hole in the report: no ordering question can be answered from it. The CSV now carries a `note` row saying so,
beside the two it already carried.

**The canned field's check got tighter rather than looser.** `RunContent.Field` refused a `--field` file whose
orders did not all leave on tick zero, on the ground that a field member stands in for a stored round. A stored
round is now one column at one cadence, so the check is that shape exactly: order one on tick zero, each order
after it one release behind the whole of the order above it. Tick zero admitted any number of simultaneous
columns; this admits exactly one arrangement per set of counts. `content/field.txt` did not move — it fills one
slot, which is the case where the two rules agree.

### Two claims [ADR-0039](adr/0039-the-command-stream-is-the-only-route-into-a-run.md) was still making from before #179

Found while amending it, and corrected there. `Run`'s public surface was described as `Advance(BuildPhase)`
**and `OfferingAt`**, which came off with the offering; a `BuildPhase`'s data surface was described as `Take`,
`TakeId` and `Slots`, which is now `Slots` and `Actions`. The structural claim the paragraph exists to make is
unchanged and still asserted by `CommandStreamTests` — the surface is exactly what a stored command carries.
---

## 14 August 2026 — the proving machinery moves under the client

Implementing [#193](https://github.com/ssalter21/tower-defense-game/issues/193). Nothing the
[ADR](adr/0050-a-decision-is-composed-in-a-local-and-proved-before-it-is-written.md) decided is repealed; one
sentence of it stopped being true when the code moved, and it is amended there.

### `ProvedSession.Written` was the one thing that touched a disk, and there is no longer such a thing

It was true while the prover lived in `simcli/`. What that also meant is that the claim the ADR is about — the
run somebody played and the record of it are one run — was reachable from a shell and from nowhere else, and
the Unity client owes exactly the same claim: [seam 2](build-order.md#2--the-submission-barrier) asks the
record format to transmit a turn as cleanly as it stores a ghost. So `PlayedScript`, `ProvedSession` and
`RunSummary` are in `sim/` now, and the file did not follow them: `System.IO` is a banned namespace there and
the IL scan reads the shipped image, so the prover cannot open a path. `simcli` adds `Written` back as an
extension over the proved session, and a client's half is Unity's storage rather than a path at all.

**The gate survived the split by moving onto the script.** A session that disagreed used to be stopped by
`Written` refusing to write it; with the write gone to the caller, a proved session that disagreed hands back
no script at all. A caller that never reads `Agreed` therefore has nothing to keep, which is what the refusal
was for. It is still by construction and it is no longer in the shell.

---

## 14 August 2026, later — the client stops opening on the recorded match

Implementing [#198](https://github.com/ssalter21/tower-defense-game/issues/198), which is
[ADR-0051](adr/0051-a-round-is-composed-on-screen-and-arrives-as-a-stored-command.md) built. Nothing that ADR
decided moved; two things written down elsewhere did.

### The player used to open on `content/match.replay`, and now it opens on a run

That was right for the whole of the walking skeleton: the recorded match was the only thing there was to look
at, and [the sit-down](sit-down.md) is written about it tick for tick. The client now holds a `Run` and opens on
the first round's build phase, reaching a match only once a round has been committed — and while the build
chrome had no modes to switch between, the recorded match went on playing underneath it, so a composed tower
could be stood on a hex a recorded tower was already drawn on. One board drawn by two things is what the mode
switch removes, and removing it means the recorded match is no longer what a build shows. It stays reachable
for `tools/capture-match-frames.ps1` and for the fixtures.

**What that costs is the sit-down's tick numbers.** Rows 1, 2, 3, 11 and 12 ask nothing about *which* match and
read against any committed round. Rows 4 to 10 name a tick of a match the build no longer plays. Whether they
are re-anchored to a round a run can reproduce, or retired onto the assertions that already carry the
load-bearing half, is [an open question](open-questions.md); `content/landmarks.txt` and the four bindings
`SitDownTests` holds the document to are untouched either way.

### The client was scoring its rounds against `content/wave.txt`, and the file it wanted was `content/field.txt`

Not a reversal — a defect, recorded because it survived two tickets unseen. `MatchRoot` built its `Run`'s field
pool from the committed defense and `wave.txt`, which is the skeleton's whole authored match: forty creeps,
three hundred and eighty gold of them, released over fourteen hundred ticks. The canned field is one build
phase's output, which is `field.txt`, and the shell's run verbs refuse a wave released over time by name —
`RunContent.Field` writes the sentence. The client had no such refusal, and nothing noticed while nobody
advanced the run. The first run that actually did died of health in round three. `field.txt` now ships in the
streaming copy; the refusal is still only the shell's, and what stands in for it on this side is that the right
file ships.

### A creep is bought once and attacks every round after

The wave was whatever the round in front of it happened to buy: round seven could field fewer creeps than round
six, and every round paid for its whole column again. Found on 14 August 2026 by playing it — *"the wave you
build is supposed to accumulate over the rounds, you aren't just paying for 1 creep for 1 round."*

Buying is now permanent. A build phase names the whole of its round's wave and is charged only for the increase
over what it carries; sending fewer of a type than is carried, or leaving one off altogether, is refused where
the decision is read. Two things were decided with it: the whole accumulated wave stays rearrangeable every
round, which is what keeps #197's drag live for the length of a run, and there is no selling a creep back — a
bad early purchase is a lasting commitment, which is the point.

**This replaced half of what `vision.md` said about the purse.** Scarcity used to come from a bounded, growing
set of wave slots refilled each round, with only the *unlock* permanent. No slot bound was ever implemented,
the unlocks came out with the offering in #179, and what is left is the rule above: the purse is the only
scarcity on the sending side, and permanence is what makes attacking compound. See
[ADR-0052](adr/0052-a-creep-is-bought-once-and-the-phase-is-charged-the-increase.md) for the shape and
[#207](https://github.com/ssalter21/tower-defense-game/issues/207) for the ticket.

**It is simulation version 4, and it exposed a hole in the mechanism that is supposed to catch exactly that.**
The behaviour fingerprint came out identical to version 3's, because every composition it folded resolved a
phase carrying nothing — and a phase carrying nothing prices as it always did. That is the second time the
table has had a blind half. The fold gained a third one.

**What it does not fix is the opponent.** The canned field is one stored round drawn ten times, so the run's
own wave compounds while the pressure coming back stays flat: the committed run now ends holding sixteen
hundred gold. That asymmetry is [#208](https://github.com/ssalter21/tower-defense-game/issues/208), and it is
not a free change — it collides with ADR-0042's rule that the field is measured once and fixed for the whole
run, which is what keeps income a fold over the outcome vector.
---

## 14 August 2026, later still — the gates come back with a different job, and a capstone is paid for out of a grant

Waves 3, 6 and 9 were deleted from the played game the day before yesterday, because a menu that rations a
four-creep roster rations nothing worth having. They come back here doing something that is not a menu: **a
gate widens the wave, deepens what a slot may hold, and pays for a capstone.**

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **[§3 — one purse](vision.md#one-purse)** | *The purse is the only scarcity on the sending side* | **The purse and a public capacity schedule.** A wave opens carrying two slots at ten apiece, and each gate adds two slots and ten count | A purse alone lets compounding gold end every run as one enormous box of whatever is most cost-efficient, which deletes send order as a decision by leaving one thing to order |
| **[§3 — the gate rounds](vision.md#the-gates)** | *Nothing bounds how many slots a wave carries* — written when the anchor-derived widths came out with the anchors | **Bounded again, on the same three rounds.** 2, 4, 6, 8 slots and 10, 20, 30, 40 count, fixed and public before a run starts | The widths were deleted as collateral of deleting the menu rather than on their own merits. What they lacked was a reason to exist that was not the menu, and capacity is one |
| **[§3 — one purse](vision.md#one-purse)**, and [6 August](#6-august-2026--six-reversals)'s settled answer | One currency; a second was priced and declined the same day | **Still one *income* currency.** A second thing exists, is granted three times a run, has no exchange rate and buys exactly one object | The 6 August rejection was of two *wallets fed by income* — the thing that makes every purchase a question about which pool to feed. A grant with a single sink is not that |
| **[The roster](roster.md#what-things-cost)** | Capstones are expected to break the pricing rule downward, and the first one is where the exemption gets written down | **There is no exemption, because gold does not buy a capstone.** The token is the whole price | An exemption to a gold rule, for a thing gold does not buy, is a clause about nothing |
| **[The build order](build-order.md#step-3-is-not-finished-and-a-played-run-is-how-that-was-found)** | The per-wave type limit is deleted rather than deferred, along with the forced pick, the round menu and the special rounds | **The first of the four to be redesigned.** It returns as a capacity schedule with a second dimension; the other three stay deleted | Deleting it was right and restoring it unchanged would have been wrong. It was a bound with no clock on it, and a gate is the clock |

### What a gate does, in one place

Waves 3, 6 and 9. At each one the wave gains **two slots**, every slot's **count cap rises by ten**, and the
player is handed **one capstone token**, spendable only on capstoning a tower already standing.

| Waves | Slots | Count cap | Capstones held |
|---|---|---|---|
| 1–2 | 2 | 10 | 0 |
| 3–5 | 4 | 20 | 1 |
| 6–8 | 6 | 30 | 2 |
| 9–10 | 8 | 40 | 3 |

The player is expected to spend a gate round on the wave, and that expectation is the mechanism rather than a
hope: a purchase is permanent and a wave only grows, so capacity — not gold — is what an attacking purchase is
ultimately spending. Two rounds of banking at 10% against a known round where the wave can finally take the
money is the timing decision the purse exists to make players practise.

### Why a capacity gate is not the gate that was deleted

[#179 deleted the gates](#13-august-2026-later-still--the-gates-are-actually-out-and-the-ladder-becomes-the-rule-it-was-an-annotation-to)
on one argument: *there is no point in gating before we have the depth, it just holds back the early testing
and experience.* That argument is about **options** — a menu offering one of three out of a roster of four
decides nothing. A capacity schedule rations **room**, and room is scarce the moment the purse can afford more
wave than the schedule allows, which is a question about the economy rather than about how many creeps exist.

**The honest half: at today's roster the two arguments do converge.** Two slots against four creep types is a
real bind, and it is the shallow-roster complaint one round further on. That is exactly why this lands as
design rather than as a ticket — the schedule is fitted after [seam 3](build-order.md#3--the-roster) has
produced something worth rationing, which is the order the 13 August entry already set.

### Two numbers were read rather than stated, and one thing has no name

The steps were specified: two slots and ten count per gate. **The opening pair was not** — 2 slots and 10
count is what makes the schedule 2/4/6/8 and 10/20/30/40, and it is an inference from the step rather than a
decision anybody made. So is **one token per gate**, which is what makes it three capstones a run. **And the
currency has no name.** All three are [open questions](open-questions.md) and all three are cheap to move
while nothing is built.

### What it will cost to build, since none of it is

Written down now because the shape of the bill is the argument for settling the numbers before the ticket:

- **Five ruleset rows**, in the file that already holds every number the rules are made of: the gate rounds,
  the starting width and its step, the starting count cap and its step, and the grant.
- **A refusal in `BuildPhase.Resolve`**, which is where a cap has to bite. A wave is carried and the phase is
  charged the increase ([ADR-0052](adr/0052-a-creep-is-bought-once-and-the-phase-is-charged-the-increase.md)),
  so the new rule is not *this wave is illegal* but *this raise is refused* — and a wave that is legal at wave
  5 is still legal at wave 6, because capacity only ever grows and a creep can never be taken back.
- **A simulation version bump**, because a rule that refuses compositions the previous version accepted
  changes what a stored record means.
- **A behaviour fingerprint that will not see it.** The fold has had a blind half twice already; a fold that
  composes only legal waves cannot tell a capacity rule from no capacity rule, so this one needs a composition
  that gets refused or it will produce the predecessor's number again.
- **A second currency in the record**, which is the only piece that is not a number. A token count is run
  state, capstoning is a placement action nobody has priced, and both cross the command stream.
- **A question the sweep cannot currently ask**: whether a capped slot ever strands gold. `EvenShareBot`
  fills one slot a round, so nothing in `content/sweep.csv` today touches a bound of any kind.

---

## 14 August 2026, last of the day — gold is paid for the damage a wave does

Implementing [#209](https://github.com/ssalter21/tower-defense-game/issues/209), raised by the developer
reading the committed run: *"what is the gold gain based on? It was supposed to be based on how much health
damage you do with your offense creep wave."* It was not. It was based on your **rank** against the field.

### What it said, and what is true now

| Where | What it said | What is true now |
|---|---|---|
| **§3** — one purse | Income is a flat base **plus a bonus in non-linear percentile bands over two distributions**: how your wave performed and how your defense performed, each against the field | **The bonus is proportional to the leak cost your wave dealt, uncapped.** One ruleset row, `bonus 25`, and every point of health damage pays gold at that rate |
| **§3** — one purse | The bonus is paid over **two** distributions, the wave's and the defense's | **The second half never existed and is not going in.** A defense already pays by not costing you health, and health is the run's clock. The claim came out of the vision rather than the code going in to meet it |
| **§3** — the coupling | You are paid against the field's distribution, never a named opponent | **You are paid for what your wave dealt.** No money still moves between players, and the reason is now simpler rather than statistical |

### The bands were paying a binary, and the committed run is the evidence

The bands paid 0, 5, 10 or 20 percent of a 168-gold base — one of 0, 8, 16 or 33 gold. Against the canned field
of one they [collapse to two](research/a-canned-field-of-one-collapses-the-bands.md), so in practice the wave
was answering *did anything get through*. Four rounds of the committed run, before and after:

| Round | Leak cost dealt | Bonus under the bands | Bonus at 25% |
|---|---|---|---|
| 4 | 36 | 33 | **9** |
| 6 | 198 | 33 | **49** |
| 9 | 416 | 33 | **104** |
| 10 | 673 | 33 | **168** |

Eighteen times the damage was paid identically; it is now paid about eighteen times as much. **25% is a
starting figure and a sweep target**, exactly as the base and the creep costs are.

### The ceiling got better rather than being given up

The load walk folds the purse forward at the most a round could have earned, so that a stored decision refused
at load was unaffordable however the run played
([ADR-0042](adr/0042-the-field-is-measured-off-the-pool.md)). An uncapped bonus reads like the end of that, and
is not: leak cost sums price times leaked over a wave's own orders, so a round deals at most **the full price
of the wave it sent** — computable from the stored slots without playing anything. The bound is now much
tighter, and the test that watches it says so: the walk used to carry 772 gold into the committed fixture's
fourth phase against a real purse of 663, and now carries 681 against 666.

### The behaviour fingerprint could not see this, for the third time

Simulation version to **5**, which retires every record made under 4. Under `rule-fingerprint/3` this build's
fingerprint came out `67E9F86CA94BE2D6` — **byte for byte version 4's**. All three halves of that fold resolve
matches and build phases and not one of them closes a wave, so the rule that moved lived somewhere the fold
could not reach. It gained a fourth half — what a wave pays a purse, itemised, and the ceiling a walk folds
instead — and the label went `rule-fingerprint/3` → `rule-fingerprint/4`. Reverting the payment to a flat share
of the base now moves the number `B234D73EC659D3A7` → `80A3DB0779957EA1`, which was watched rather than
reasoned about.

**The new half is folded through a ruleset written out in the test file**, not through
`content/ruleset.txt`. The payment is arithmetic over authored numbers, so folding the committed file would
make a retune of the bonus rate move the fingerprint and retire every record made under rules nobody changed —
which is the exact confusion the fingerprint exists to tell apart from a rule change.

### ADR-0042 is largely superseded, and what to do about that is a human's call

`PerformanceField`, `Run.Field`, `Run.FieldSamples`, `MeasureField`, the `run-measure/1` draw and the
percentile lookup exist only to price the bonus, and now have no consumer. **Nothing was deleted**: deleting a
working capability is not an agent's call, and the question is written down as an
[open one](open-questions.md#is-the-field-measurement-kept-now-that-nothing-prices-off-it) with what each
answer costs. One consequence is already banked either way — a played round no longer reads `Run.Field`, so the
**half a run per run** the ADR records as its price is not being spent.

### What regenerated, and what did not need to

`content/commands.txt` **did not need re-authoring**, which was the open risk: the early rounds send nothing
and are paid nothing, and the run still plays all ten waves. The ruleset hash moved (`7E1DA52C5F85D545` →
`D01EB9595248D3C9`), so `content/run.commands`, `content/run-outcome.txt`, `content/match.replay`, the goldens
and the streaming copy all regenerated.

**`content/sweep.csv` moved on every row, and the `bonus_gold` column stopped being a binary.** Under the bands
four of the five creeps earned *exactly* 2,376 gold over eight runs each — the failure this ticket is about,
sitting in the committed report where nobody read it. They now earn 9,288, 10,060, 10,901, 12,052 and 12,586
against a flat base of 13,440, which separates them in the order their leak cost dealt already did. The rest of
the report moved with it: leak cost dealt rose by a fifth to a third, the runs bank two and a half times as
much unspent gold, and cost efficiency fell from 362–442 to 316–360 because the extra income buys creeps faster
than they pay for themselves at this rate. **The win-rate column says nothing either way** — it was already
saturated at 10000 basis points on every row before this ticket, from #207. 25% is a starting figure and the
sweep is where it gets moved.

---

## 15 August 2026 — the opponent accumulates, and the run it kills is the evidence

Implementing [#208](https://github.com/ssalter21/tower-defense-game/issues/208), raised by the developer after
playing #207: *"the opponents field will need to start small and scale like the player now does."* It did not.
The field was one stored round drawn ten times, so a round-one opponent and a round-ten opponent sent the same
wave while the player's own wave compounded — and the committed run finished holding 1,647 gold it had no
reason to spend.

### What it said, and what is true now

| Where | What it said | What is true now |
|---|---|---|
| **ADR-0042** — the pool | The pool is a population, and the field is the K of it a round is resolved against | **The pool is a population per round.** Round seven draws from the members recorded at round seven, which is the shape a ghost pool has anyway: stored ghosts are accumulated rounds |
| **ADR-0042** — the measurement | *"It is fixed for the whole run, and that is what keeps the payment a fold"* | **Still true, and now at a price that is named.** The draw grew per round and the measurement did not — it reads the whole population at once, so the pool a run fights and the distribution it is measured against describe different populations |
| **`content/field.txt`** — the stand-in | One pair of orders, drawn with replacement | **One player, recorded once per round**, buying that column again every round: ten bodies in round one, seventy in round seven |

### Resolution 2, taken by the developer, and what it cost

The ticket put two resolutions up. **Resolution 1** measures the field per round — arguably more correct, and
it multiplies the sweep by the number of rounds. **Resolution 2** grows only what a run fights and leaves the
measurement flat, which is the alternative ADR-0042 explicitly considered and rejected on the grounds that the
pool and the distribution would then describe different populations.

Resolution 2 was taken, and most of the collision it was worried about had already evaporated: since #209
nothing prices off the distribution at all, so the population it describes is a population nothing consumes.
The ADR carries the amendment.

### The measurement that decided the shape of the committed run

**A wall kills a count, not a share.** The committed six-tower defense stops twelve bodies out of twenty, out
of forty, out of a hundred — the kill column is flat and every creep added to a column is a creep that gets
through. So an opponent who accumulates outruns any wall a run can afford: the ten-round committed run takes
**5,011 gold of damage against a health pool of 800** and dies in the fourth round. The numbers are in
[`a-wall-kills-a-count-not-a-share.md`](research/a-wall-kills-a-count-not-a-share.md).

The developer's ruling was to **grow the opponent and let the run die** rather than raise the health pool or
soften the curve: the tuning is what it is, and hiding it behind a gentle curve would have left the imbalance
in the file where nobody reads it.

### So `content/commands.txt` is four rounds long

Not by choice. `record-run` plays a script to the end before it writes anything, so a fifth build row is a row
nobody was alive to play and the file could not be recorded at all. The four rounds still demonstrate what they
are there for — the wall going up one tower a round, and #207's accumulation, with round three naming the ten
runners round two bought and paying for the ten it adds.

**The run also stops banking.** Waiting five rounds and spending the bank in one is the play against a field
that never grows; against one that does, the rounds a run banks through are the rounds that kill it.

### What the sweep lost, and what it kept

`content/sweep.csv` moved on exactly two columns. **The win rate went from 10000 basis points on every row to
zero** — every run of every creep now loses — and taken went from 1,634 to 33,512, identical on all five rows
because the incoming waves leak in full and the dice never touch them. Everything else is byte for byte what it
was: dealt, spent, defense, unspent, cost efficiency, base and bonus. The report is still an instrument on the
offense axis and is no longer one on the defensive axis, which is the honest reading of a tuning where offense
dominates.

**The sweep costs 78 seconds against 42.** Same 9,600 matches; a round-ten column is ten times as deep and its
match runs 6,098 ticks against 1,913. Measured on the same machine, both including the build.

### The behaviour fingerprint could not see this, for the fourth time

Simulation version to **6**, which retires every record made under 5. Under `rule-fingerprint/4` this build's
fingerprint came out `B234D73EC659D3A7` — **byte for byte version 5's**. Every half of that fold is *handed*
the pairing it folds, and who a round draws is decided above all of them. It gained a fifth half that plays a
three-round run against a population recorded per round, and the label went `rule-fingerprint/4` →
`rule-fingerprint/5`.

**The new half is folded over a roster written in the current column layout, and that is the second thing the
half caught.** A layout-1 row carries no cost column, so every unit in the fingerprint's own roster is free —
and a leak that costs nothing folds to zero whoever sent it. The half was written, watched passing under a
deliberately flat draw, and only the priced roster made it able to see the rule it is there for.

## 16 August 2026 — the sweep's owed columns become queries

[Step 4](build-order.md#4--the-balance-harness) owed three further columns: the both-columns check, outcome
spread, and win rate binned by ingredient count. **Two of the three stop being columns.** `simcli sweep
--per-run` keeps a row for every run under the folded ones — the creep row's own headings, plus the seed that
produced it — so a spreadsheet groups them and gets the distribution the fold is a summary of. Outcome spread
is a fold over those rows, and the ingredient bin comes back as a grouping if the take gate returns. Neither is
an edit to the harness any more, and only the both-columns check is still owed.

**The reason it is a mode rather than the default.** A row per run at the runner's ceiling of 100,000 runs a
creep is millions of rows held in memory and written as one string, so the sweep that wanted the fold alone
should not carry it. That ceiling is also where the argument for dropping parallelism was made
([#202](https://github.com/ssalter21/tower-defense-game/issues/202)), which is worth remembering together: the
workload that issue defends as four hours of single-core compute is, with `--per-run` on, bounded by memory
rather than by time. Streaming the rows out belongs with the scale work that was deferred, not with this.

**Which player swept is now written down.** The plan has always carried a `BuildPolicy` and the command line
never passed one, so every committed number was `EvenShareBot`'s with nothing on the file saying so. `--policy`
takes `even-share` or `all-in`, the name is a parameter row, and an unrecognised name is refused rather than
defaulted — a fallback produces a complete, correct-looking report about a player nobody asked for. `all-in`
was added because one legal value is not a choice; it is the bracket that answers what the defensive half of a
round is worth, which no single report can.

`content/sweep.csv` is regenerated for the new `seed` column and the `policy` row. **Every number in it is
unchanged**, which is the point: the report gained a way to be asked new questions without any old answer
moving.

## 16 August 2026, later — one format version, and the map it is for

[#213](https://github.com/ssalter21/tower-defense-game/issues/213) put five decisions in front of
[seam 9](build-order.md#9--the-board) while there was still no ghost pool to retire, on the argument that each
one acquires a migration cost the day [step 6](build-order.md#the-sequence) writes the first stored ghost. Six
things moved. The largest is that **pathfinding leaves the design permanently**, which is a partial reversal of
[6 August](#6-august-2026--six-reversals): the maze stays, the search does not.

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **§3** — the board is a maze | Pathfinding enters the simulation, and that is a determinism obligation | **Out, permanently, and line of sight with it.** A map may be as complex as it likes and always has exactly one path; `HexMap`'s load-time trace is the whole of it | A search is owed only when the map branches or the player alters the route by building, and neither is wanted. It was the seam's highest-risk item — one RNG stream, canonical order asserted, IL-scanned, in the hottest loop — and cutting it removes that from the critical path without costing one benefit the fold was wanted for |
| **§3** — elevation grants range | *+1 range per level*, the common shipped form | **A signed difference, at half that.** `baseRange + (towerLevel − targetLevel) × 500`, with radii reading as spheres where height only ever costs | A flat bonus makes a tower on a cliff better at everything, including shooting the creep standing above it. Height is a relationship, not a property. At 1000 an Archer swung between 1.2 and 5.2 hexes across three tiers; 500 keeps the fold's shape mattering more than its height map |
| **§3** — the record | `TowerLayout` and the hex map gain a level | **Only the map gains one.** A tower stands on a hex and the hex carries the level | A derivable coordinate is not a stored one. `PlacedTower` is unchanged, `RecordFormat.TowerBytes` stays at 6, and `GhostRecord`'s format survives untouched |
| **§3** — the map is generated | Maps are generated rather than authored | **The first map is hand-authored and generation waits behind it** | Selection pressure needs a fitness function, and a fitness function needs one map that is demonstrably good to calibrate against. The sweep already takes its map as a parameter, so scoring a candidate costs a flag — which is worth nothing without a reference |
| **[roster](roster.md#what-this-roster-needs-that-the-schema-does-not-have)** — five levers | Five levers, none to become a column without a research finding behind it | **Nine columns, fixed as a list**, with three of the five collapsing into one mechanic | A sweep, a blast and an aura are one shape — a bubble that emits something — so the column count is identical either way and only the divergence grows |
| **[roster](roster.md#11--soldier--tier-1--status-live)** — the Soldier | A corridor unit, and seam 9 takes his board away | **Kept, with a self-centred bubble** | A tower that strikes every creep touching it is the one tower whose whole value is positional, which is what a fold is for. Retiring him would have thrown that away |

### What "no pathfinder" buys, and what it forecloses

`sim/HexMap.cs` already traces the corridor at load and asserts it: every corridor cell has one or two corridor
neighbours, exactly two have one, those two are the entrance and the exit, and the walk from one to the other
visits every corridor cell. **Folding that corridor into a switchback changes none of it.**

The rule that a corridor cell may not have three corridor neighbours is **kept**, which means two legs of a
path may never be adjacent: every fold costs a row of ground between the legs, and a longer path comes from a
bigger board rather than a tighter one. That is what keeps the route derivable from the grid, and it is what
keeps a map a seed. Gap-row serpentines and spirals are still authorable, and at two-hex leg spacing an Archer
at 3200 already reaches across three legs.

**What is foreclosed is branching mazes and player-built routes.** Both are real designs, both are now out by
decision rather than by omission, and reversing either costs a search in the hot loop under `sim/`'s standards
against a live ghost pool.

### The bill, corrected

The ticket priced this as one format version. It is **four**, and the one it was most sure of turns out to be
free.

- `hex-map/1` → `hex-map/2`, for the level layer.
- `ReplayVersion`, because the bundle writes map cells and a level plane is new bytes.
- `content/units.txt` `layout 2` → `layout 3`, for the nine columns.
- `match-state/1` → `match-state/2` — **the large one, and it was not in the ticket at all.** There is no
  per-unit effect machinery in `sim/` today; every stat is read straight off the shared `UnitType` at use time.
  Timed effects are new per-creep state, and the Necromancer's aura measured in hex distance puts creep
  *positions* back into the tick loop, which is precisely the property
  [`TowerCoverage`](../sim/TowerCoverage.cs) was written to keep out of it. Every golden trace retires.

**What is free is the ghost record.** `TowerLayout` does not gain a level, so `RecordFormat.TowerBytes` stays
at 6 and `GhostRecord` is untouched.

**Corrected again on 16 August, by building the first half of it.** It is **five**, and the fifth is
`SimulationVersion` **6 → 7**, taken in
[#214](https://github.com/ssalter21/tower-defense-game/issues/214). Nothing about the tick loop moved — the
committed match still leaks the same twelve creeps on the same tick 5283 — but `Match` opens its rolling state
hash by folding the map hash, and the map hash now covers the height of every hex as well as its terrain. So
every stored record's rolling hash stops reproducing while its outcome does not, which is exactly the condition
the simulation version exists to retire records for. The rule-fingerprint table refused to let the number move
without it, which is what that table is for; the row it wanted is `(7u, 0xF7A080A6691EA488UL)`.

**Corrected a third time, by building the second half of it.** It is **six**, and the sixth is
`SimulationVersion` **7 → 8**, taken in [#215](https://github.com/ssalter21/tower-defense-game/issues/215) —
the signed difference, the sphere and the floor, in
[`sim/Reach.cs`](../sim/Reach.cs) and asked by the one range test there is. This one *is* a rule change, and
it is one whose retirement is invisible on the committed content: that map is entirely on the ground tier, so
the signed difference over it is identically zero, the golden trace does not move a byte and
`content/sweep.csv` does not move a number. What retires is every record made on a map with a fold in it —
loadable since the level layer landed an hour earlier, and replaying to a different outcome under this.

**The rule fingerprint could not see it, and the fix was the scenario rather than the fold.** Four times
before, a rule moved that `DerivationTests` was structurally blind to and the fold gained a half. Not this
time: every half of that fold already resolves a match against a tower and a route, which is exactly the code
path the rule moved in — and it produced version 7's number byte for byte, because the scenario's map was
written on the flat. So the map in it gained a fold, the label went to `rule-fingerprint/6`, and the row is
`(8u, 0xF3D0032E948518D4UL)`. The same scenario under the flat rule folds `0x12BD5CDF6025ECD9`, which is what
makes the row evidence rather than a number somebody wrote down.

**And the map hash is compared under the layout a record stamped it at.** `hex-map/1` folded the terrain alone
and `hex-map/2` folds the terrain and the levels, so the two are answers to different questions rather than two
answers to one. A replay bundle carries its stamp and its grid in the same bytes under a format version that
says which layout they were written at, so the older ones are still checked exactly — against the terrain,
which is all they ever pinned. Without that, `content/golden/defense-0.replay` — the one bundle nobody can make
again — would have failed the map gate on a layout bump, which is precisely the loss the restaging verb exists
to prevent. What the bump does retire is a stamp arriving without its record: a stored defense matched against
a map loaded today, folded under two layouts with nothing to say which.

### The map is text, and it is drawn by hand

`content/map.txt` gains a **second grid block** for levels, written in letters — `a`, `b`, `c` for the three
tiers — because the parser refuses digits on purpose and says so in its own error message. Odd rows are
indented in the file so what is typed matches the half-cell offset it produces. `MapHash` folds the levels,
which keeps the anti-cheat property and the a-map-is-a-seed property intact for free.

The authoring loop is a sketch, transcribed once, and edited directly thereafter against a render command —
chosen over a map editor because there is one map to draw and the file is the artefact that ships. That command
is [`tools/render-map.ps1`](../tools/render-map.ps1), and it draws the **parsed** map: a file that will not
load produces the loader's own refusal and no picture at all, so "is this a map yet" is answered by the same
corridor assertion the simulation runs rather than by a second reader that would eventually disagree with it.

### The stat is called shield, and arcane stays where it was

The absorbing pool was going to be called *arcane* until it turned out `ArmourType` already has an `Arcane`
member, with a real meaning in `content/ruleset.txt`: impact does 140 against it, pierce 100, magic 70. Two
different things spelled the same way in the same row was the whole of the objection. **The pool is `shield`**,
the armour type keeps its name, and the roster's [expectation of an "arcane
shield"](roster.md#open-questions) is satisfied by the pool plus the Necromancer's aura granting it — which is
what that entry always described.

## 16 August 2026, later still — nine columns land, and one guess in the cost rule stops being one

**`content/units.txt` goes `layout 2` → `layout 3`**, gaining the nine columns
[#213](https://github.com/ssalter21/tower-defense-game/issues/213) fixed as a list, built in
[#216](https://github.com/ssalter21/tower-defense-game/issues/216). The reasoning is
[ADR-0055](adr/0055-a-sweep-a-blast-and-an-aura-are-one-bubble.md). Two lines of the bill above are paid at
once — the layout and `match-state/1` → `match-state/2` — and `SimulationVersion` goes **8 → 9** with them.

| | Before | After | Why |
|---|---|---|---|
| **the `bodies` term** of the placed-unit cost rule | Hardcoded `Delivery == Projectile ? 3 : 1` | **The `targets` column** | It was a guess standing in for a column that did not exist. A Marksman would otherwise be priced at a single-target Archer's price |
| **what a bubble may carry** | — | Damage, or one of speed, cooldown, armour, shield. **Not range**, refused by name with the reason attached | Coverage is intersected with the route once, at load. A payload that moved a range drags the two dimensions back into the tick loop |
| **a row claiming both shot shapes** | — | **Refused at load** | *n* targets draws *n* rolls and a damage bubble draws one. A row claiming both draws one of them per body of the other, and the draw count is part of what every stored record replays through |
| **a bubble the tick loop cannot resolve** | — | **Refused when a match is built from it**, by name | The alternative is a Cryomancer standing on the board, firing, and slowing nothing, with a column that parsed perfectly and nothing anywhere saying so |

### What moved in the artefacts, and what did not

**Nothing about the committed match moved.** Every row authors one shot, no shield and no bubble, so the same
wave leaks the same twelve creeps on the same tick 5283, the four landmark ticks are unchanged, the committed
run still dies in round four having dealt 229, and `content/sweep.csv` is byte for byte what it was —
regenerated and unmoved, which is the honest result and not a tuned one. What moved is the hashes: the roster's
under `unit-types/3`, the match's under `match-state/2`, and every artefact that carries one of them.

**The rule fingerprint could not see it, and for the second time running the fix was the scenario.** Every half
of that fold is fought over a layout-1 or layout-2 roster, and no such row can carry a shield, a shot count
above one or a bubble at all — so #216's rules run in all five halves and are visible in none. The fold gained
a sixth half whose roster is layout 3 and whose two towers are the two shot shapes, the label went to
`rule-fingerprint/7`, and the row is `(9u, 0x1BAEAF1DA57D7D8EUL)`. With `Match.Absorbed`'s body struck out —
the shield spent by nothing, every other line untouched — the same scenario folds `0xF8B857E6175940A5`, which
is what makes the row evidence rather than a number somebody wrote down.

### The Mage is priced for a splash nobody has authored

| | The rule prices | The row costs |
|---|---|---|
| **Mage** | **30 gold** — 275 average damage, a 54-tick cooldown, one body | **92 gold** — three bodies' worth |

**This is a finding rather than a decision.** The old `bodies` guess read three off the delivery column, so the
Mage's price looked derived; what it was doing was propping up a splash the simulation has never had.
[The roster](roster.md#4--mage--tier-1--status-live) signs "splash of one additional hex", radius 1000, and
layout 3 is the first schema that could carry it — as a bubble on the target with a damage payload.

**#216 authored no bubble on the Mage and moved no price**, because either edit is somebody deciding what a
Mage is. What it did instead is pin the gap in `ContentTests` with both numbers in it, so the question stands
in the artefact rather than being silently answered. Three ways out, and none of them is a ticket's to take:
author the splash and accept that a radius is unpriced; reprice the row to 30 and accept a Mage at a third of
its old price; or make it genuinely fire three shots, which is a different tower.

## 16 August 2026, last — a stat can move while a match is running, and a floor stops that ending it

**`sim/` gains its first per-unit timed effects**, built in
[#217](https://github.com/ssalter21/tower-defense-game/issues/217) and decided in
[#213](https://github.com/ssalter21/tower-defense-game/issues/213). The reasoning is
[ADR-0056](adr/0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md). `SimulationVersion` goes **9 → 10**,
the state hash's label `match-state/2` → `match-state/3`, and the rule fingerprint's `rule-fingerprint/7` →
`rule-fingerprint/8`.

Before this, every stat was read straight off the shared `UnitType` at use time and a creep carried
`Id, Type, OrderIndex, Distance, Lateral, Hp, Shield, Phase, TicksInState` and nothing else. There was no
effect machinery in the simulation at all.

| | Before | After | Why |
|---|---|---|---|
| **an effect** | — | **A stat, a magnitude and a duration.** One model: speed, cooldown, armour and a granted shield pool | A slow, a rally, a curse and a pool are the same four fields with different numbers, exactly as a sweep, a blast and an aura were one bubble |
| **two of them on one stat** | — | **Strongest-wins, with the timer refreshed** | It is the only rule with a ceiling that does not depend on how many copies of a tower somebody could afford |
| **two of equal size and opposite sign** | — | **The lower one**, so the order is total | Otherwise a curse and a blessing resolve by whichever landed last, and two runs that differed only in build order fold different numbers |
| **when an effect stops** | — | **Exactly its duration of ticks after the one it landed on**, whichever phase emitted it | Expiry opens the tick and emission closes it, so a shot and a pulse mean the same thing by "duration" |
| **a creep's walking speed** | `_stepPerTick[orderIndex]` | **`Creep.Step`, per creep** | A modifier is per unit. An array indexed by the order would also have mis-reported every overtake involving a slowed creep, because `StepThisTick` reads the same number |
| **the slowest a creep can walk** | Nothing said | **A tenth of its authored speed, and never less than one milli-hex** | A safety rail. The constructor's "no speed" refusal is at construction only, and a runtime modifier walks straight past it |
| **a wave that could not cross at that floor** | — | **Refused when the match is built**, naming the tick it would have arrived on | The floor makes a hung match unreachable by arithmetic, and this is where that is proved for the map and wave in hand |
| **a bubble the tick loop could not resolve** | Refused when a match was built from it | **It plays.** #216's guard is deleted | That guard existed only because this machinery did not |
| **a damage bubble with a period** | Authorable | **Refused at load** | A pulse has no shot, and ADR-0003 is that the dice are rolled once per shot and nowhere else |
| **an aura centred on `target`** | Authorable | **Refused at load** | A pulse has nothing it landed on |
| **a speed reaching towers, a cooldown reaching creeps** | Authorable | **Refused at load** | Nothing that stands walks and nothing that walks attacks. A pool or an armour reaching a tower is *not* refused — that is a fact about the rows, not about the role, and refusing it would make `bubbleAffects` derivable and therefore empty |

### The two things somebody had to decide, decided here rather than in silence — and one of them wants a signature

**A shield payload's magnitude is a share of the health it stands in front of.** A shield is a pool rather
than a rate, so there is no authored number of its own for a percentage to be a percentage of — and the
consistent-looking alternative, a share of the recipient's own `shield` column, is inert on every row the
mechanic exists for: the roster's walking rows author no shield at all. The same choice settles how the pool
behaves: it **persists until spent or until its duration ends**, whichever comes first, a duration of zero
means until spent, and killing the emitter stops the pulses rather than stripping what is already granted.

**All of that is provisional.** #213's column table says "a percentage" and stops, so something had to be
chosen for the column to mean anything at all — but what a Necromancer's pool is worth, how long it lasts and
whether killing her takes it back are shapes of a creep rather than shapes of the simulation, and that is
Sam's by standing rule. It is written here and in ADR-0056 as the implementer's reading, it stays an open
question in [the roster](roster.md#7--necromancer--status-live), and moving any of it costs no format
version.

**A damage modifier is unauthorable, and the word is the reason.** #217's model names five modifiable stats
including damage, and `bubblePayload` has five values including `damage` — but that word already means *the
attack's own roll, spread*, which is what #216 built. The two readings cannot share a keyword, the list of
five is fixed, and a sixth would be widening the schema #213 closed. Four stats are modifiable and the fifth
name is taken.

### What moved in the artefacts, and what did not

**Nothing about the committed match moved, again.** No row of `content/units.txt` authors a bubble at all, so
the same wave leaks the same twelve creeps on the same tick 5283, the four landmark ticks are unchanged, the
run still dies in round four having dealt 229, and `content/sweep.csv` is byte for byte what it was —
regenerated and unmoved, which is the honest result and not a tuned one. What moved is the hashes.

**The rule fingerprint could not see it, for the seventh time and in the roster again.** The sixth half of
that fold is the one #216 added precisely because the five above it were fought over rosters that could not
say what it changed — and both of *its* bubbles carry damage, while a timed effect is emitted by a bubble
carrying a stat and by nothing else. So the rules ran in that half and were visible in none of the six. What
changed is the rows: a turret whose shot slows what it hits and a walker whose aura grants a pool to whatever
walks beside it. The label went to `rule-fingerprint/8` and the row is `(10u, 0x13EB7A4673B75F21UL)`. With
`Effects.ModifiedSpeed` returning the authored speed — the slow landing, expiring and changing no step, every
other line untouched — the same scenario folds `0x4B15804EC1BEDE48`, which is what makes the row evidence
rather than a number somebody wrote down.

### What a view still cannot see

**No snapshot field and no event says a creep is slowed.** Events are decorative by
[ADR-0008](adr/0008-match-events-are-decorative.md) and there are six of them; the snapshot is the view's only
input by [ADR-0007](adr/0007-snapshot-is-the-only-view-input.md). Adding either is a view contract taken in a
ticket about rules, so neither was taken — and it goes to [open questions](open-questions.md) rather than
being assumed, because the day a Cryomancer is signed is the day somebody has to draw one.

---

## 27 August 2026 — the board folds and climbs, and the corridor every number was priced on is gone

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **§8** — the board | Until the maze lands, the board is a **47-hex corridor one cell wide**, and on one-wide geometry many placements are equivalent | **A hand-drawn 51-hex corridor that folds and climbs through three tiers.** Still one hex wide; the tiers are what make one cell a different placement from another | [#218](https://github.com/ssalter21/tower-defense-game/issues/218). Selection pressure needs a fitness function and a fitness function needs one map known to be good to calibrate against, so generation and rotation stay deferred behind a board somebody drew |

### What the fold bought, measured rather than argued

**Six per cent.** The corridor is 51 hexes where it was 47 and the recorded match runs 5581 ticks where it ran
5283. The 15 August read wanted time under fire *multiplied* by folding the board; the board as drawn spends
most of its extra room on single-file descents, which cost one hex per row. A serpentine on the same 19-by-13
grid reaches ninety-odd hexes. **The room is there and this shape does not use it**, and that is written down
here rather than smoothed over, because the next map is drawn against this number.

Cost efficiency — dealt per 100 gold, which is a cost-weighted leak rate — rose for every creep: minion 329 to
354, scout 316 to 363, necromancer 360 to 383, skeleton 354 to 366, warrior 344 to 355. Win rate is zero for
every creep on both boards, so nothing separates them there and nothing did before.

Of the four things a folded board was supposed to fix, **two cannot be answered by the report at all.** Send
order is invisible because a sweep row fills one slot a round, which is a fact about `content/sweep.csv` and
not about the map. A defense that scales is invisible because the canned field's wave grows every round and
its wall does not — [#222](https://github.com/ssalter21/tower-defense-game/issues/222).

### Where a tier is allowed to change, and the leg that cannot hold one

**A ramp cannot turn, so a tier change has to sit between two corridor cells of the same row with corridor
either side of them.** The KayKit pack ships slope variants for `hex_road_A` alone. Two of the changes on the
board as first drawn sat on turns -- one on the right-hand descent, one on the left -- and neither had a tile
that could render it.

The right-hand one moved by carrying tier b up the descent, so **the exit stands at b** and the climb happens
mid-leg on row 5. The left-hand one could not be moved where it was: **row 8's leg is three cells long and both
ends are turns**, so no ramp fits on it at any tier. Tier a now holds all the way down the left descent and row
11 carries the climb instead. That is the rule biting on the drawing rather than on the file, and it is the
first thing to check when the next board is drawn: a leg needs four cells before it can hold a change.

### The committed defense stopped being chosen

**Six towers still, but computed: `CoverThenUpgradeBot`'s cover-most-per-gold rule on this map, under the
overlap the build gate asserts.** Hand-picked coordinates are what went stale everywhere at once the moment
the board moved — the mage at column 9 row 0 could no longer reach any part of the route, and the loader was
right to refuse it. What the file loses is the ability to stage a specific situation by hand; what it gains is
that redrawing the board is a regeneration rather than an outage.

It now covers the route end to end, where the old six left gaps near the entrance and either side of the first
corner. **A third of the wave still gets through** — twelve of forty, unchanged — so what leaks now leaks
because the wall cannot kill fast enough rather than because it cannot see.

### What a record from before the fold can and cannot say

**A version-0 or version-1 replay bundle predates the level plane, so it restages the folded board flat.** That
is not a defect: those formats carry no tiers and never could. It means a stripped record no longer agrees with
the live match, and while the map was flat those two hashes agreed for a reason that had nothing to do with
restaging. Two tests asserted that agreement and proved less than they looked like they did; they now assert
the flattened answers, which is the claim the format actually supports.

---

## 29 August 2026 — the ghost gets a purse, and the wall it buys with it is worse

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **the 27 August entry** — what a folded board could not answer | A defense that scales is invisible, because the canned field's wave grows every round and **its wall does not** | **The wall grows too.** A member of the canned pool opens holding `content/ruleset.txt`'s starting purse, spends the same half of it `CoverThenUpgradeBot` spends for a run, and is paid the same interest and the same flat base afterwards | [#222](https://github.com/ssalter21/tower-defense-game/issues/222). A growing wave measured against a frozen wall makes the late rounds of `content/sweep.csv` mean less the later they are, which is exactly where a defense that scales was supposed to show up |
| **[`content/defense.txt`](../content/defense.txt)** | The wall every member of the canned field stands behind, in every round | **The wall they open with.** Every round builds before it is recorded, so round one stands these six plus whatever round one's own share bought, and round ten stands ten rounds of that income on top of them | The ticket's open question was what purse a ghost has. The answer chosen was the run's own: the same half-purse rule, on an income the ghost is assumed to have had, so there is one economy and not two that can disagree |

### The bonus line is the one a stand-in cannot have

A wave is paid a share of the leak cost it dealt and nothing in the pool resolves a round of its own, so a
stand-in's round closes on the interest and the base and **no bonus**. Assuming one would be assuming a number
only a played round produces.

**Its wave is not priced, and the share it would cost leaves the purse anyway.** `content/field.txt` is
calibrated as roughly what a round's wave comes to *once a purse has bought a wall as well*, so pricing it here
would price one wave twice — and a pool handed a script no purse could compose would then be refused rather
than recorded, which is what `TheSweep.LethalField` exists to be. What carries into the next round is
therefore **what the wall declined to spend out of its own share**; the rest is the wave's and is gone. A purse
that banked the offensive share would compound at the ruleset's interest on gold a player spends — an
opponent growing richer for sending the same wave.

**The opening wall is handed over and not bought.** A member of the pool is a *recorded round* rather than a
run played from nothing, so `content/defense.txt` stands beside the opening purse instead of coming out of it.
Charging for it would refuse most layouts anybody could author: the committed six cost 344 gold against an
opening purse of 100.

### What it actually builds, and the finding that fell out

**Six mages by round six, and then nothing.** The committed route is covered end to end, so the rule finds
nothing to place and spends on upgrading: the four archers become mages one at a time in rounds two, three,
five and six — **344 gold of wall, then 396, 448, 448, 500 and 552 from round six on** — and after that every
placement is the dearest row the roster has and there is nothing left to buy. Two caps, both real: a rule that
only places where something is unshot at has nowhere to put a seventh tower however rich it gets, and a rule
that upgrades into the dearest row runs out of rows.

**And the dearer wall is a worse wall.** A mage costs 92 against an archer's 40 and fires once every 54 ticks
against 18 — a third less damage a tick for more than twice the price, bought for no reason but that it is
dearer. The committed run gets *more* past the ghost from round two on, not less: wave two's `dealt` went 17 to
36, wave three's 102 to 126, and the run's whole total 331 to 375. It still dies at wave four with 0 of 800
health left, because what kills it is the ghost's *wave* and that has not moved.

The sweep re-ranks with it, and not in one direction — the mage's magic attack meets the roster's armour
classes differently than the archer's pierce. Dealt over eight runs a creep: minion 43,621 to 36,847, scout
47,198 to 52,687, necromancer 48,944 to 48,944, skeleton 44,336 to 43,588, warrior 43,152 to 38,468. Win rate
is still zero on all five rows.

**This is left standing rather than tuned away.** "Upgrade the oldest placement into the dearest row" is
`CoverThenUpgradeBot`'s rule and a run plays by it too; a run's purse just rarely reaches the upgrade half,
while a ghost opening behind a covered route reaches it in round two. The bot is now the thing the report is
most obviously wrong about, which is a better place for it to be than invisible — and it is one open question
and not two, because fixing it fixes both walls at once.

**And the ticket is answered for six rounds of ten, not for all ten.** From round six the wall is frozen again
for want of anything to buy, so the late rounds a defense that scales was supposed to show up in are still a
growing wave against a still wall. What moved is that the freeze is now a property of the *player* rather than
of the pool, which is one place to fix instead of two — it is
[filed as an open question](open-questions.md) and not carried here as done.

---

## 29 August 2026 — the whole collection is imported

**What it said:** [the collection inventory](research/kaykit-collection-inventory.md) — "It is not in this
repository and should not go in whole", and the import pattern is "pull the individual files a scene needs,
commit those, leave the rest outside."

**What is true now:** every model in the 21 packs that ship an `fbx(unity)` export is committed, at
`client/Assets/Art/Kaykit/` — 4,247 models and 171 textures, 154 MB.

**Why it moved.** The 606 MB the original claim was priced against is four redundant export formats. One of
them is the one this project imports, and it is a quarter of the weight; the other three-quarters is `.blend`
sources, glTF, OBJ, user guides and contact sheets, none of which Unity ever reads. Nothing about the
5 MB-per-file tripwire moved — the largest file in the whole import is 443 KB.

**What it cost, and what it bought.** It costs every clone 154 MB it did not carry before, and that is a real
price with no undo. What it buys is that choosing scenery stops being an import request. The selective pattern
was correct while dressing the board meant picking six trees; it is the wrong shape the moment somebody is
dressing a board by hand and wants to look at what is available, because every candidate cost a round trip
through a copy step before it could be looked at *in situ*.

**The two things that had to change with it.** Both were latent and neither was a bug until now:

- **One material for all scenery.** `SceneryModels` held a single `surface`, the Medieval Hexagon atlas, which
  was correct while every model came from that pack. Every other pack ships its own atlas, so a City Builder
  crate drawn against the hexagon texture is not a slightly-wrong crate, it is confetti. A named model now
  carries its own material, resolved from the atlas its own importer bound.
- **A family and a variant number cannot address 4,247 models.** The generator asks for "a grove" and an
  index, and must keep doing so — it is scattering a board it has never seen. A person has already looked at
  the thing and means *that one*. So `content/dressing.txt` gained a second verb: `model <col> <row> <name>
  …`, addressing one model by its path under `Assets/Art/Kaykit`. `place` is untouched, the generator is
  untouched, and the committed board bakes byte-for-byte identical.

**What is deliberately not done.** The scene carries only the models the dressing file actually names, not the
catalogue — four thousand mesh references in `Match.unity` to draw the six somebody placed would be a scene
nobody could merge. The consequence is that `Bake` now names `tools/build-match-scene.ps1` in its own log
line, because the file and the scene are two artifacts and only the first one it writes itself.

---

## 3 September 2026 — the wall is bought by value, and the report's dealt column mostly empties

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **[`docs/open-questions.md`](open-questions.md)** — does the scripted player's upgrade half need to know what a tower is worth? | An open question with three answers: score an upgrade by value, refuse an upgrade that lowers damage a tick, or leave it | **Answered: score it by value.** `CoverThenUpgradeBot` scores every purchase left on a covered route the same way and buys the highest — the middle of a row's damage roll over the ticks between its shots, times the bodies one shot hits, times the route hexes it reaches from that cell, per gold that row costs above whatever stands on it | [#236](https://github.com/ssalter21/tower-defense-game/issues/236), decided by Sam. One rule for both halves of the phase is the one that stays right when the roster grows; the price-alone rule was buying a mage for no reason but that it was dearer |
| **the 29 August entry** — the two caps on the stand-in's wall | A rule that only places where something is unshot at has nowhere to put a seventh tower however rich it gets, and a rule that upgrades into the dearest row runs out of rows | **Neither cap is there.** Once nothing on the route is unshot the bot may stand a second tower on route something already watches, scored by that same number — so a redundant place and an upgrade are two candidates in one comparison rather than two phases | The same ticket. Redundant coverage is a real defensive move and the bot had no way to make it, which is what froze the stand-in's wall from round six |

### What the stand-in now builds

**Fourteen towers by round five, and dearer every round after.** The wall the canned pool opens behind is the
committed six, and half of every round's purse goes on it: **384 gold of wall in round one, then 464, 544,
624, 664, 716, 768, 768, 820 and 872** — against 344, 396, 448, 448, 500 and then 552 frozen from round six.
Nine of the ten rounds now buy something. Where the old rule bought the dearest row it could reach, this one
buys archers until the cells worth standing on are gone and then climbs into mages: round ten stands eight
archers and six mages.

### And the wave stops getting through

**The committed run now deals nothing at all.** Round by round its `dealt` went **0, 36, 126, 213 → 0, 0, 0,
0**, and its whole total **375 → 0**. What it *took* has not moved by a gold — 100, 200, 290, 387 in both —
so it still dies at wave four with 0 of 800 health left, and for the same reason: what kills it is the ghost's
wave, and that has not moved.

**The sweep loses one of its five rows outright and thins the other four.** Leak cost dealt over eight runs a
creep: minion **36,847 → 2,073**, scout **52,687 → 0**, necromancer **48,944 → 31,337**, skeleton **43,588 →
25,810**, warrior **38,468 → 22,769**. Taken falls with it — 37,305 → 27,289, 36,122 → 27,289, 35,844 →
19,794, 36,913 → 22,898, 36,913 → 21,267 — because a run's own wall is built by this rule too and now kills
what arrives. Cost efficiency follows dealt: 318 → 28, 386 → 0, 383 → 321, 351 → 259, 336 → 257. Win rate is
still zero on all five rows.

**A scout row of zeroes is a row that cannot answer anything**, and it is the finding rather than a reason to
retune: no price and no ruleset number moved here. The two lightest creeps are the ones a wall of archers
chews, so the report now says the fast cheap end of the roster does not survive contact with a defense that
spends its whole share — which is a statement about the roster and the board, made by a bot both sides play
by. `SweepTests.A_sweep_on_another_seed_is_another_population_of_runs` had to move to the necromancer's row to
find a number that still disagrees across two seeds, which is the same finding seen from the suite.

### Two readings of the decided sentence, and which one was built

**"The reach it already has" is the reach of the row being bought.** An upgrade is scored on what the *new*
row would watch from that cell and never on what the old one watches — a mage reaching 4,600 thousandths of a
hex where the archer under it reaches 3,200, so the extra reach is a large part of why any upgrade is bought
at all. The other reading — score the new row's damage against the old row's reach — describes no tower that
ever stands, and it has nothing to say at all about a second tower on an empty cell, which Sam's decision
scores by the same rule. **"Over" is read as "across" and not as "divided by"** for the same reason: dividing
by reach would make a wide tower worth less than a narrow one, which is the opposite of what the cover half
does.

**The phase opens when nothing more *can* be covered**, which is not the same sentence as "nothing on the
route is unshot". A route hex no legal cell reaches would otherwise hold the bot in the covering phase forever
and leave it unable to buy anything at all. On the committed board the two are the same moment.

### The stepping stone, which is the rule's own consequence

**The bot will buy a tower and climb out of it in the same round.** The score divides by what a row costs
*above* the one under it, while a build phase charges the row's whole price — an upgrade costs its target's
full price and always has, see `content/upgrades.txt`. So a cheap tower is a cheap way to make an upgrade look
good: a 30-gold soldier stood and then turned into an archer is 70 gold spent on a 40-gold archer. It is
asserted rather than hidden, in
`BuildPolicyTests.Once_the_route_is_covered_the_bot_buys_the_most_damage_over_the_route_per_gold`, because it
is the kind of thing somebody finds in the report and reads as a bug.

**Whether that denominator should be the gold actually paid is left open** and is
[filed as a question](open-questions.md) rather than decided here: the rule as decided says *price
difference*, and a rule rewritten on the way past is not the rule anybody chose.

---

## 5 September 2026 — the vision is cut to its decisions, and three sections learn to say they are not built

No design moved here. **The document did.** [The vision](vision.md) went from 780 lines and 8,758 words to
279 and 2,318 — a 74% cut by words — and everything removed was argument rather than decision.

The header of this file has said the reason since it was written: *"This file exists so the vision can stay
readable. It grows; the vision should not."* It had been growing anyway. Every claim in it arrived in the same
shape — a bold assertion, then *Three consequences*, *Two limits*, *The objection it must answer*, a warning
block — so 57 bold-lead paragraphs and 9 of those blocks were carrying roughly 900 words of actual decision.
The justification already had three homes: this log, 57 ADRs and 20 research notes. The vision was a fourth
copy of all three, and the copy with no test on it.

### What was wrong rather than merely long

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **[§3 — the gates](vision.md#the-gates)** | The capacity schedule and the capstone, stated as *"fixed, public and known before it starts"* with no mention of status | **The same design, marked *(designed, not built)*** | `grep -ri capstone sim/` is empty and `BuildPhase` still says *nothing bounds how many slots a wave carries*. The design was settled [on 14 August](#14-august-2026-later-still--the-gates-come-back-with-a-different-job-and-a-capstone-is-paid-for-out-of-a-grant); the vision never said it was unbuilt, so three documents read as disagreeing about whether it existed |
| **[Bottom line, claim 4](vision.md#bottom-line)** | *"All four, none deferred."* | **"The multiplayer is real, and all of it is deferred."** | [The build order](build-order.md) calls deferring the multiplayer *the largest scope deletion available* and defers all four. Co-op was listed as Unspecified in the vision's own table two sections later. A standing document cannot claim *none deferred* about four things three of which are deferred |
| **[§3 — the board](vision.md#the-board-is-a-maze)**, generation and rotation | Stated flat, with the deferral in a warning aside below it | **Marked *(designed, not built)*** | Same defect as the gates, and the aside was doing the work a status marker should do |
| **[§7 — the service](vision.md#7-what-runs-it)** | Stated flat | **Marked *(not built)*** | Nothing in the repository serves anything |
| **Numbers — base, bonus, health, interest, purse, snapshot price** | Restated in the vision with the measurement narrative that produced each one | **Named, not quoted.** The vision says a bonus is paid on leak cost dealt; what the rate *is* is [`content/ruleset.txt`](../content/ruleset.txt) | Every one of those numbers was argued three times — in the ruleset's own comments, here on the day it moved, and again in the vision. The ruleset is the only one of the three that cannot go stale |

### What was cut without anything being lost

The Sources section (32 lines and 17 comparison games), the objection-and-answer paragraphs, the three
eliminated damage candidates ([ADR-0001](adr/0001-fixed-point-arithmetic.md) and this log hold them), the
capstone's *three things it buys the design*, §4's costs table, and the derivations behind the Latin square
and the ×10 scale. **One comparison game survives in the whole document** — Element TD, as the depth target,
where naming it is shorter than describing it.

Two mechanics were dropped in a first pass and put back on review, because they are decisions rather than
argument: **a lobby smaller than ten is topped up from the pool**, and **the gate schedule's shape holds for a
rotation while its filling is drawn per run**.

### The anchors, and what was left to rot deliberately

Ninety links pointed into the vision. The nine numbered sections and four sub-sections — `#one-purse`,
`#depth-is-the-point`, `#the-board-is-a-maze`, `#how-a-shot-resolves` — were kept as anchors for that reason
as much as for their content. The twelve live links that named a heading which no longer exists were
retargeted, in [the build order](build-order.md), [open questions](open-questions.md),
[the roster](roster.md) and this file.

**The fifty-one links from [`research/`](research/) and [`archive/`](archive/README.md) were not.** Those are
frozen evidence notes that already carry banners saying the vision has moved past them; an unresolved anchor
lands a reader at the top of the vision, which is the right place for a note written against a version of it
that no longer exists. Rewriting them would be churn in the two directories that exist precisely so the
standing document does not have to carry their history.

### What this does not fix

**`check-docs.ps1` would not have caught any of it.** Its four invariants cover issue states, picture
freshness, the ADR count and ADR citations from source. Nothing checks whether a document's claim about the
*simulation* is true, which is why a section describing an unbuilt mechanism as fixed and public survived
three weeks in the file that outranks every other file in the repository. A fifth invariant — a document
claiming a mechanism exists when no code implements it — is not obviously mechanisable, and is not filed as a
ticket here because nobody has yet said what it would read.

---

## 5 September 2026, later — fifteen research notes are retired

The vision was cut to its decisions in the morning. `docs/research/` had the same problem in a larger form:
**twenty-four notes, 809 KB, of which nine were load-bearing.** Fifteen are deleted, leaving 90 KB — the
directory is 89% smaller and the count is down by well over half.

**The test was not age.** It was whether anything still needed the file. A note stays if code, a content file
or an ADR cites it — `sim/FieldPool.cs` and `content/field.txt` both point at the canned-field note, and the
number in `units.txt` is unreadable without the cost note — or if it holds a measurement that costs real time
to take again, which is why the hot-reload timings survive at nine kilobytes. Everything else was a survey
whose verdict had already been read.

**A survey that has been read is finished work.** Its findings were written into the vision, an ADR or open
questions on the day it landed, which is what a research note is *for*; the note is the working, and keeping
the working beside the answer gives a reader two places to look with no rule for which is current. Three of
the retired design notes had already grown supersession banners saying their premise had moved — the corridor,
the camera, the one-purse problem — which is the same fact arriving as maintenance cost. Commissioning a fresh
survey against the design as it now stands is cheaper than reading an old one and subtracting its banners.

| What went | Why it could |
|---|---|
| **Nine design surveys** — build depth, the attacking half, towers-versus-squads, upgrade graphs, creep variety, Element TD's ancestry, and the three HTML notes on fun, the planning phase and generated maps | Every verdict is in [open questions](open-questions.md#what-the-design-research-found), which had already carried five of them in a summary table. Three said of themselves that they decided nothing; three carried banners saying their premise had moved. The upgrade-graph survey landed whole into ADRs 0043, 0044 and 0045, each of which names the shape it rules out in its own words |
| **Three Unity build notes** — the agent workflow, project settings, sim-library integration | All three were adopted and then outlived by something that cannot go stale. `AGENTS.md` rule 3 now *forbids* the editor bridge the workflow note recommended; `tools/check-project-settings.ps1` asserts what the settings note chose; the integration note had corrected itself in four amendments |
| **The software factory** | It asked for the autonomy gradient to be written down. It was, on 3 September, as `AGENTS.md` rule 6. The rule outlives the argument for it |
| **Two KayKit listings** — the model index and the character roster | The index transcribed 2,252 model names; all 4,247 models are now imported at `client/Assets/Art/Kaykit/`, so `ls` answers the question against the files instead of against a copy of them. The character roster declared itself superseded on the numbers by the collection inventory, which stays |

**One thing was salvaged rather than summarised.** The planning-phase note catalogued fourteen uses of a
re-runnable 2.75 ms match as design material — six with no equivalent in the genre — and that list existed
nowhere else. It is now a ranked table in [open
questions](open-questions.md#what-a-275-ms-match-could-be-spent-on), which is where an undecided menu of
options belongs anyway.

**What this costs, stated rather than discovered later.** The primary sources are gone with the notes: a
citation chased from an ADR now stops at the ADR's own sentence, and re-establishing a claim means finding the
source again. That is the deliberate trade — the ADRs assert the findings they used in their own words, and a
footnote nobody follows is not worth 322 KB of prose to keep followable. Recovering any note is
`git show <sha>:<path>`; nothing is lost, only unindexed.

**`check-docs.ps1` would not have caught the drift this removes**, and it will not catch its absence either.
Its four invariants cover issue states, picture freshness, the ADR count and ADR citations from source. A
document citing a document that no longer exists is not among them — which is why every one of the twenty-eight
inbound links outside the index was repointed by hand here, in the vision, the build order, open questions, the roster, this
file, `AGENTS.md`, four ADRs, the archive and the two surviving notes that referenced retired ones.

---

## 5 September 2026, later still — the roster widens, and seam 3 comes off the shelf

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **[The build order](build-order.md#step-3-is-not-finished-and-a-played-run-is-how-that-was-found)** | Seam 3 is parked behind a playable client, and when it resumes its shape is *upgrading the creeps that exist*, in stats and speed, rather than authoring new unit types | **Seam 3 is next, and it is breadth.** As many towers and creeps as the collection supports; every tower line three stages — a base, a single-stat rung, and a capstone that changes how the tower works, drawn from what its model carries | Sam, 5 September 2026: *"I've changed my mind on how to approach the build. I want to make the game fun to play now. I want to now introduce as many towers and creeps as possible."* The client the roster was parked behind exists, so the wait is over |
| **[The roster](roster.md#what-is-deliberately-absent)** | Three shapes are absent and blocked on art — fast and cheap in numbers, slow and dear and very tough, fast and durable at once | **Candidates are named for all three**, in [the expansion proposal](roster-expansion-proposal.md). The proposal is under review and signs nothing | Every character in the KayKit collection is on disk, so "needs a model" is no longer a blocker; it is a choice |

**What was decided is the direction, and only the direction.** The proposal names 9 tower lines and 17 creeps,
assigns 31 of the 61 rigged KayKit characters and sets 30 aside. Every name, model and number in it is a
placeholder until Sam keeps it — per the standing rule that art and gameplay options are his to pick — and the
five standing proposals it would retire (Captain, Hero, the Pyromancer/Cryomancer branch, the unbuilt Mage
splash, the tier signal) stand in `roster.md` until he does. Each of those is its own entry here when it moves.

**The theme it fixes on the creep side is undead, plus the dark and hooded.** Skeletons were already the
creeps; the Mystery Monthly characters that read as gothic monsters or as masked and cloaked join them, and
everything modern, mechanical or comic is set aside with a one-line reason so it is not re-derived.

---

## 5 September 2026, last — Sam signs the roster, and six standing proposals move

The [expansion proposal](roster-expansion-proposal.md) was reviewed by Sam in full and taken. The entry above
recorded the *direction* and said each of the five standing proposals it would retire "is its own entry here
when it moves". They moved. A sixth reversal — the capstone token — was not in the proposal and arrived in the
review.

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **[The roster](roster.md#proposals-retired-on-5-september-2026)** — Captain and Hero | The Soldier line's tier 2 is an attack-speed aura and its tier 3 a 360° sweep | **Both retired as proposals.** The aura is the **Paladin's Blessing**; the sweep is the **Barbarian's Slam** | A tier 2 is one stat now, and both mechanics belong on models that ship the prop for them — a book and a statue, a two-handed slam clip. Neither row ever reached `units.txt`, so no id is burned |
| **[The roster](roster.md#proposals-retired-on-5-september-2026)** — the elemental branch | The Mage line forks at tier 2 into Pyromancer and Cryomancer and rejoins at the Frostfire Archmage | **The branch is retired.** Tier 2 is the **Sorcerer**, one stat; tier 3 is **Unravel**. The area slow is the **Druid's Overgrowth** | Three stages, no branch. It closes the roster's own open question that the tier-2 element pick was temporary, and it makes *one line, three stages* an invariant with no exception. Two roads ending at one tower made the pick a tempo decision rather than a build decision |
| **[The roster](roster.md#4--mage--tier-1--status-live)** — the Mage's splash | Author the splash, reprice the row, or make it fire three shots — "picking one is a signature rather than a fix" | **The splash is authored** — origin `target`, radius 1000, payload `damage` — **and the price is deliberately not touched.** The row stays at 92 against a rule that says 30 | Sam, 5 September 2026: *"The only thing on the mage thats deferred is the appropriate costing, all the functionality stays, it needs the splash."* Repricing a row whose value is a splash radius is what the cost rule is worst at; the price waits for the automated balance sweeps to be good enough to derive it. The gap is now held open on purpose rather than unanswered |
| **[The roster](roster.md#7--skeleton-mage--status-live)** — id 7 | id 7 is the **Necromancer**, wearing `Skeleton_Mage`, and the pack's dedicated Necromancer model "is left unused, which is a choice and not an oversight" | **id 7 is relabelled Skeleton Mage** and its long-unsigned aura is signed as **haste** (+20 speed, friend, radius 2000). A new **Necromancer** row wears the Necromancer model and takes the shield aura originally designed for id 7 | The pack's own necromancer should be the necromancer. The id does not move so no hash moves; only the new aura does that. The `units.txt` label moves too — `necromancer` becomes `skeleton-mage` — because two rows cannot share one, and renaming a label is free |
| **[The roster](roster.md#the-tier-signal-is-never-size)** — the tier signal | "Size is the tier signal, and it is the only one." The Ranger is the Archer at 1.5 scale | **Size is retired as a tier signal.** A rung is told apart by **colour, a prop, or a second model** — never by how big it is. Size now tells the two *sides* apart and nothing else | Sam, 5 September 2026: *"I dont like the size upgrade any more, I want it reverted. There needs to be changes to the assets, maybe new equipment or a new continuous aura or glow on the tower, colour change that makes sense."* Scaling a Paladin 1.5x when the pack ships a helmeted Paladin wastes the pack |
| **[The roster](roster.md#what-things-cost)** — what a capstone costs | The gates were **deleted, not switched off**, on 13 August, and the per-wave type limit went with them | **A capstone token comes back as a plain currency**: +1 at rounds 3, 6 and 9, spent on a capstone edge. **No gate comes back with it** — no capacity schedule, no per-wave type limit, no offering | The 14 August entry re-proposed gates carrying *both* a capacity schedule and a token; **only the token half is taken.** Sam, 5 September 2026: *"You get a capstone token on round 3 6 9. Its fine that they are not priced in, it's limited by the increments."* Nine capstones against three tokens is what makes the choice of line a decision |

**Scarcity is the grant schedule, not the price, and that is a decision rather than a gap.** Five of the nine
capstones — Shield Wall, Blessing, Consecration, Overgrowth, Unravel — change neither the damage roll nor the
bodies a shot hits, so the cost rule would price each identically to the rung below it. **No capstone premium
is to be authored.** An authored fudge factor inside a file whose whole claim is that both sides of the purse
are arithmetic would be a worse artefact than a flat number the sweep can report.

**The three engine asks were decided with the rest.** The **Cursed Villager becomes the Werewolf on the first
damage it takes**, and a lethal first hit does not kill it — the change resolves ahead of the death, so the
Werewolf enters on its own full 2600 and no Villager can ever be one-shot. The **Necromancer raises a Minion
every 150 ticks with no cap**, bounded only by how long it lives. The **Grave Robber pays 12 gold** to the
defender that kills it, half its own price, and a leaked one pays nothing.

> **The uncapped Necromancer was raised as a problem and kept as a trade-off.** A slowed Necromancer raises
> *more* — the speed floor is a tenth, so a fully-slowed one crosses in ~17,000 ticks instead of ~1,545 and
> raises on the order of a hundred Minions instead of ten. Shield Wall and Overgrowth are slows, so the two
> capstones built to handle a push are the two that make this body worst. Sam, 5 September 2026: *"I dont care
> that it gets stronger in theory if you slow it, thats an interesting trade off to spamming or stacking
> slows."* Recorded because it will look like an oversight to whoever meets it in a sweep.
>
> **What it does cost is a proof.** `Match.RequireItArrives` guarantees at construction that every *authored*
> order reaches the exit inside the tick ceiling, at the floor speed — and a creep spawned at runtime is in no
> order. Nothing runs away, because a Minion does not itself raise, but the arithmetic has to be re-derived
> over the spawner's own floored crossing time rather than assumed. That is the spawn ticket's ADR.

**Names: all 35 kept as proposed, and the grammar asymmetry is deliberate.** Tiers 1 and 2 are named for a
body — Sergeant, Templar, Bishop, Sorcerer, Elder, Cutthroat, Artificer — and every capstone is named for a
move: Shield Wall, Slam, Blessing, Consecration, Unravel, Overgrowth, Overwatch, Fan of Knives, Mortar. That
was put to Sam as a problem, since a row is a unit and every other row is named for the thing on the tile. He
kept it: *"the towers are tier 1 and 2 but the capstone is the name of an upgrade instead."* A capstone is the
one rung bought with a different currency and it is not a new body — it is a thing the tower learns to do.

**Models: all confirmed from a rendered sheet, with three changes.** The **Marksman holds `crossbow_2handed`,
not his rifle** — the only firearm the roster would touch, and it puts the top of the Archer line in a
different century. The **Grave Robber wears `Hoarder_Backpack`, not the sword** — the pack is what the mechanic
is about, and a sword on a creep that never attacks reads as a lie. And the **PlantWarrior is set aside**: it
was proposed as the Druid's tier 3 and was the only one of the six second models that read as a *different
creature* rather than the same person promoted. The Druid keeps his body and gains a **bare weirwood** on the
tile beside him — `Tree_Bare_*_Color8` from the Forest Nature pack, cream-white trunk under a red canopy.

**What the signing bought that is work rather than prose.** A per-row texture does not exist in the view —
`UnitArt` has no material field — so tier colour is a small view feature plus an import of the alternate
textures, none of which are in the repo. There is **no socket for a prop beside a tower**, only two hand
bones, so the Engineer's turret, the Paladin's statue, the Cleric's font and the Druid's tree have nowhere to
stand; the Engineer line cannot be drawn as designed at any tier without one. And a **glow is deliberately not
a tier signal** — it is reserved for reading *"this tower is projecting an aura"*, which is the meaning that
matters in play; the client has no `ParticleSystem` and two play-mode tests forbid one, so it would have to be
real mesh geometry either way.

---

## 6 September 2026 — the Villager transforms, and two written-down claims move with it

[#267](https://github.com/ssalter21/tower-defense-game/issues/267) built the first of the three engine asks
signed on 5 September: a creep becomes another row mid-lane. Two claims this repository had written down
turned out to be wrong once it existed, and both are here rather than edited away.

| Where | What it said | What is true now |
|---|---|---|
| **[`content/units.txt`](../content/units.txt)** — the layout row | Layout 3 "paid that price once, deliberately and **for the last time before the map is measured**" | **Layout 4, and the map is not measured yet.** A body that stops being one row and starts being another cannot be said in any arrangement of the twenty-eight columns, because every one of them describes a body that is one thing for as long as it exists. So `becomes` is the twenty-ninth column, layout 3 keeps its own reader branch and its own hash label, and every record stamped against the roster before this is retired |
| **[The roster](roster.md)** — the Villager/Werewolf pair | "The pair is therefore worth 1800 + 2600 = 4400 effective health *always*, not as a worst case" | **It is worth the Werewolf's 2860 effective health.** That arithmetic belonged to a trigger nobody signed — it is what a change *on death* would be worth. The signed trigger is the **first damage taken** and the change resolves ahead of it, so the Villager's own 1800 is never spent and the roll that triggers the change lands on the Werewolf. The band test moved with it, 36 to 88. **Whether the pair should be worth both pools is Sam's, and is unsigned** — it means moving the trigger onto the death, which is a different mechanic; it is an [open question](open-questions.md#should-the-cursed-villager-transform-on-damage-or-on-death) rather than a decision this ticket took |

**The reading taken, spelled out, because a later reader will need it.** "The change resolves ahead of the
death" was signed; three implementations of that sentence were available, and the one taken is **the change
resolves ahead of the *damage***. The shot arrives, the pools in front of health absorb, and if anything got
past then the body becomes the row it names — and *then* the roll is resolved, through the new row's armour
and against the new row's pool and death check. "Cannot be one-shot" is therefore not a rule at all: the row
that named a successor is already gone when the death check runs, so no hit of any size can kill it. The two
rejected readings both apply the damage first and then need an invented clamp — one point of health, so that
the transformation has somebody to happen to — and a clamp would make the Werewolf's entering pool a function
of how hard the Villager was hit, which is the opposite of what the roster signs. See
[ADR-0058](adr/0058-a-creep-becomes-another-row-mid-lane.md).

### The bill

Two retirements, both deliberate, and the label bumps that come with them.

- `content/units.txt` `layout 3` → `layout 4`, and `unit-types/3` → `unit-types/4`, for the one column.
- `SimulationVersion` **10 → 11**, because the tick loop's rules moved: a body can change row, and the matrix
  is consulted against the row it changed into.
- `match-state/3` → `match-state/4`. **Which row a creep is stopped being a constant of the creep**, so the
  type id is folded every tick beside its health and its phase instead of once at the spawn — and every stored
  record's rolling hash stops reproducing whether or not its roster authors a transformation.
- `rule-fingerprint/8` → `rule-fingerprint/9`, the third bump taken for the scenario rather than the shape of
  the fold: no roster in `DerivationTests` named a row to become, so the rules ran nowhere and the fingerprint
  would have recorded only that the state hash had a new layout.

**What is free is every record format.** `RecordFormat` is untouched: a roster is pinned by its content hash
and never by its column count, so the replay bundle, the ghost, the wave and the command stream all carry the
new hash in the field they already had.

---

## 6 September 2026 — a creep raises creeps, and the layout row moves a second time in a day

[#268](https://github.com/ssalter21/tower-defense-game/issues/268) built the second of the three engine asks
signed on 5 September: the Necromancer raises a Minion beside itself every 150 ticks, uncapped. One claim this
repository had written down moves with it, and one number it had estimated turns out to be wrong in the
direction that matters.

| Where | What it said | What is true now |
|---|---|---|
| **[`content/units.txt`](../content/units.txt)** — the layout row | Layout 3 was "the last widening before the map is measured", and [#267](https://github.com/ssalter21/tower-defense-game/issues/267) already recorded that layout 4 broke it | **Layout 5, hours later, and the map is still not measured.** A creep putting a creep on the board cannot be said in any arrangement of the twenty-nine columns: every one of them describes what a body *is*, and `becomes` is one body changing row rather than a second body arriving. So `raises` and `raisePeriod` are the thirtieth and thirty-first, layout 4 keeps its own reader branch and its own hash label, and every record stamped against the roster before this is retired again |
| **[The roster](roster.md)** — the Necromancer | "It lives about 1,545 ticks and raises **roughly ten**"; a fully-slowed one raises "on the order of a hundred" | **Eleven against the committed defense, and about a hundred and seventy at the floor speed.** The estimate was close and the reading it implies was not: four hundred gold of Necromancers returns **1200%** of its gold against a band of 60 to 95, where it returned 100 before. Nineteen bodies are sent and **228** leak |

**Uncapped is the decision and the reading is the finding.** [#250](https://github.com/ssalter21/tower-defense-game/issues/250)
signed the cadence and signed the absence of a cap in the same sentence, with the trade-off written beside
it: *a slowed one raises ten times as many, kept as a trade-off*. Shield Wall and Overgrowth are slows, so the
two capstones built to handle a push are the two that make this body worst. **No cap, decay or diminishing
return was added to make the number look better**, and no creep number was retuned: the return band was
already asserted as MISSED at both ends and the Necromancer's entry in the over list is updated from 100 to
1200 rather than converted to a pass.

**Two decisions the ticket had to take, both recorded in [ADR-0059](adr/0059-a-creep-raises-a-creep-and-the-board-is-what-caps-it.md).**

- **A raised body enters the array at the end, with the next entity id**, so the array stays in ascending id
  order — which `ReportPasses`, `Acquire` and the fold all read as part of the rules. The tiebreak follows
  from that and follows in the conservative direction: a body raised level with its raiser has the higher id
  and **loses every tie**, so a tower looking at both shoots the raiser.
- **A raised body's leak charges health, at the price of the row it is.** A body reaching the exit takes as
  much health off a defense whether or not anybody bought it, so charging nothing would make the raise free in
  the run economy. What nobody paid is the *sender's* half: creep cost is derived from effective health and a
  spawner's price cannot see what it spawns, so 21 gold of Necromancer arrives with 110 gold of Minions behind
  it. That gap is [open](open-questions.md#what-is-a-spawner-worth) and held open, beside the Mage's splash and
  the Vampire's shield.

### The bill

Two retirements, both deliberate, and the label bumps that come with them.

- `content/units.txt` `layout 4` → `layout 5`, and `unit-types/4` → `unit-types/5`, for the two columns.
- `SimulationVersion` **11 → 12**, because the tick loop grew a phase: a body puts a body on the corridor at
  the close of a tick, beside the wave's own release.
- `match-state/4` → `match-state/5`. The clock each creep raises on joins the per-tick fold, and the running
  count of leaks nobody sent joins the totals — so a run one tick from putting a body down stops hashing like
  one that is two ticks from it.
- `rule-fingerprint/9` → `rule-fingerprint/10`, the fourth bump taken for the scenario rather than the shape
  of the fold: no roster in `DerivationTests` raised anything, so the rule ran nowhere.

**What is free is every record format.** `RecordFormat` is untouched, for the reason #267 gives: a roster is
pinned by its content hash and never by its column count.

---

## 6 September 2026 — a kill pays, and both balance instruments turn out to be blind to it

[#269](https://github.com/ssalter21/tower-defense-game/issues/269) built the last of the three engine asks
signed on 5 September: the Grave Robber pays twelve gold to whoever kills it, mid-match, into the one purse.
The mechanic is small and what it measured is not.

| Where | What it said | What is true now |
|---|---|---|
| **[`content/units.txt`](../content/units.txt)** — the layout row | Layout 3 was "the last widening before the map is measured"; layouts 4 and 5 already broke it, hours apart | **Layout 6, the same day, and the map is still not measured.** What a body is worth *dead* cannot be said in any arrangement of the thirty-one columns: `cost` is what it is worth *alive*, charged against health when it reaches the exit, and a kill is the opposite outcome. So `bounty` is the thirty-second, layout 5 keeps its own reader branch and its own hash label, and every record stamped against the roster before this is retired again |
| **[The roster](roster.md)** — "the sweep is what is meant to derive it" | Every unpriced thing on this roster is held open for the sweep to measure | **The sweep cannot see this one, and neither can the return band.** Both instruments came back byte-identical, for two different structural reasons, and no number was touched to make that happen |

**Neither instrument is broken and neither is being relaxed.** The return band plays four hundred gold of one
row against the committed defense and reads `leaked × 100 / count`: it is a **leak rate**, which is exactly why
cost cancels out of it, and a body that was *killed* is outside the ratio entirely. The sweep plays whole runs
and varies the row a run **sends**, where a bounty is paid to whoever **kills** — so it would reach a run
through the opponents it defends against, and every opponent in the committed sweep is the stand-in out of
`content/field.txt`, which sends Minions. The Grave Robber reads 81 in the band and 367 in the sweep, before
and after, to the digit.

**What a reading of it needs is a stand-in that sends the row**, which is one line of `content/field.txt` and a
regenerated report — and that is deliberately not done here. The stand-in's column is calibrated rather than
chosen: its own header carries the measurement that a field member with the right total and the wrong number of
columns is a walkover, so swapping the row it sends would move every reading in the report for a reason that
has nothing to do with any of them.

**Three decisions the ticket had to take, all recorded in
[ADR-0060](adr/0060-a-kill-pays-the-defender.md).**

- **The payment is read off the row the body is standing as when it dies**, and never off the order that sent
  it. That is forced rather than chosen: a body a spawner raised is in no order at all, and one that changed
  row mid-lane is not the row its order names. It is deliberately *not* how a leak is priced, because a leak
  survives only as a count per order and a kill is resolved with the body still in the array.
- **The money is minted rather than taken**, and **the one purse spends it on either half of the board.** With
  two wallets, "a kill pays the defender" would mean the defense funds itself; with one, a round that kills
  well sends a bigger wave next round. That coupling is the one purse's and not the bounty's, and it is the
  first income that depends on what a player *built* rather than on what a player *sent*.
- **What the kills paid joins the per-tick fold.** The count of kills beside it cannot stand in for it: two
  matches that killed the same number of different rows agree on every other field in that fold, and a body
  that has been cleared away leaves nothing else behind. That is the shape of the bug #254 found in the
  snapshot comparison.

**One thing was deliberately not refused.** A bounty above the row's own cost loads perfectly well. Twelve is
half of twenty-four and `docs/roster.md` argues that half for *that row*; whether a body may ever be worth more
dead than it cost to send is a design question nobody has taken, and a refusal at load would take it.

**And one adjacent hole is named rather than fixed.** `WaveScript.FullPrice` is documented as the ceiling on
what a round can deal, and since #268 it is not one: a raised body's leak is charged at the raised row's price
and `FullPrice` counts only the bodies an order sends, so a pool holding a spawner bounds the load walk's bonus
below what a round could really earn — the one direction that produces false refusals. Nothing in the committed
content reaches it, and it is #268's line rather than this one's. The bounty's own ceiling,
`WaveScript.MostBountyPayable`, does count what a spawner puts down, bounded by `Match.TickCeiling` over the
raise period.

### The bill

Two retirements, both deliberate, and the label bumps that come with them.

- `content/units.txt` `layout 5` → `layout 6`, and `unit-types/5` → `unit-types/6`, for the one column.
- `SimulationVersion` **12 → 13**, because a match now produces a number a run spends.
- `match-state/5` → `match-state/6`. What the kills have paid joins the per-tick fold.
- `rule-fingerprint/10` → `rule-fingerprint/11`, and this one is **the first bump since
  `rule-fingerprint/6` that the scenario alone could not answer**. #269's rule fires where a body's health
  reaches zero, and **not one of the six halves that fold already had kills anything at all** — every body on
  every roster in `DerivationTests` walks to the exit. A bounty put on the sixth half's roster moved the
  fingerprint by nothing. The seventh half is a defense that kills what walks at it, over rows that pay three
  different numbers by three different routes.

**What is free is every record format**, again, and for the reason #267 gives.

**And one measured fact about the frames, found while re-capturing them.**
[`docs/frames/README.md`](frames/README.md) said "a frame is a function of its tick and nothing else". It is
not: capturing `-Ticks "342,344"` and `-Ticks "311,342,344"` produce different bytes for the two shared frames,
reproducibly, and only the three-tick list reproduces what is committed. #268 measured the same thing and
recorded it in a commit message; the README now says it, and its magic-lines recipe is the list that actually
made those frames.

---

## 6 September 2026, later — a capstone costs a token, and the 14 August proposal is half taken

[#273](https://github.com/ssalter21/tower-defense-game/issues/273) built the second currency the roster has
been describing since the nine tower lines were signed: **a capstone token, granted one at rounds 3, 6 and 9,
and the whole price of a capstone edge.** Three tokens against nine capstones, so which line reaches its top
is the decision.

**This is the [14 August proposal](#14-august-2026-later-still--the-gates-come-back-with-a-different-job-and-a-capstone-is-paid-for-out-of-a-grant)
half taken, and the half is deliberate.** That entry brought the gate rounds back carrying two things at once:
a **capacity schedule** — 2, 4, 6, 8 slots and 10, 20, 30, 40 count — and a **grant** that pays for a capstone.
Only the grant is built. The capacity half is not deferred pending a ticket and is not scheduled; it is where
[13 August](#13-august-2026-later--the-gates-come-out-and-the-client-comes-before-the-roster) left it. **Read
that entry as partly taken from here on.**

| Where | What it said | What is true now |
|---|---|---|
| **[§3 — one purse](vision.md#one-purse)** | *The purse is the only scarcity on the sending side* | **Still true, and it is now a sentence about one side of the board.** Nothing bounds a wave; what gained a second scarcity is the tower side, and only the top of a line |
| **The 14 August table's second row** | Waves 3, 6 and 9 widen the wave and deepen what a slot may hold | **Rounds 3, 6 and 9 hand over one token and do nothing else.** No slot count, no per-slot depth, no menu, no offering. The rounds are a schedule and not a gate |
| **[The roster](roster.md#what-things-cost)** | The token is a rule stated on that page and not yet code | **It is code.** A capstone row carries no gold price at all, and what is left open there is the client: a player holding no token sees a line stop at its second rung with nothing saying why |
| **[`content/upgrades.txt`](../content/upgrades.txt)** | *That is the whole content of an edge: no tier number, no direction marker, no cost* | **The keyword is the cost.** `upgrade` is the target's gold price and `capstone` is one token; the arity is still two ids, and the file is layout 2 |

### The keyword is a layout even though no column moved

`content/units.txt` states the rule that **adding or moving a column is a new layout and a new label**, because
two different column counts under one label is a silent misread nothing here could detect. This is the narrower
case: the count did not move and **what the keyword column means did**. Two ladders whose rows mean two
different things, both calling themselves layout 1, is the same class of thing — so `content/upgrades.txt` goes
to **layout 2** under `upgrade-ladder/2`, the price joins the fold there and not before, and layout 1 is kept
as a branch that reads every edge in gold and hashes to exactly what it always did.

**A keyword rather than a third column, and rather than reading the shape of the ladder.** A third column would
widen all eighteen rows to carry a price nine of them have. Deriving a capstone from the graph — the top of a
line is a unit with no outgoing edge — would make the price a property of what somebody authored *above* a row:
a fourth rung would silently re-price the third, and a line deliberately two rungs deep would silently make its
second rung free.

**That last clause about layout 1 needed an assertion of its own.** Regenerating the goldens copies
`content/upgrades.txt` over `content/golden/defense-1.upgrades`, so the moment the committed file moved to
layout 2 there was no committed layout-1 ladder left for the older branch to be measured against.
`UpgradeLadderTests` now pins that branch's hash as a literal, the way `ContentTests` pins the oldest unit
table's.

### What the record did not need

**No record format moved and none had to.** A command stream already carries the wave index every grant is a
function of and the actions every spend is a function of, and the ladder hash in its header pins which edges
cost a token — so a stored token count would be a second copy of a derivation, free to disagree with the
first. `CommandStream.Check` folds the balance forward beside the purse and the board, **exactly rather than at
a ceiling**, because nothing about a token depends on how a round played. `RecordFormat.CommandVersion` stays
3 and its reader branches are untouched. [ADR-0061](adr/0061-a-capstone-costs-a-token.md) states that decision.

**And one rule is left uncovered by every hash in this repository, named rather than fixed.** *Which* edges
cost a token is in the ladder hash; *how many tokens a round holds* is a list in `sim/Run.cs`, and moving it
would make refused streams legal with every stamp on every record still agreeing. The rule fingerprint cannot
reach it — the scenario it folds over has a ladder with no edges, so no capstone is ever priced there — and
widening that scenario would be a `SimulationVersion` bump for a rule that has not moved, which is the mistake
`DerivationTests`' own remarks name. So the schedule is pinned by an assertion there whose comment says what a
change to it owes.

### The capstone is unpriced on purpose, and the tool now says how unpriced

**Seven of the nine capstones price flat against the rung below, not five.** Five change neither term of the
damage rule — Shield Wall, Blessing, Consecration, Overgrowth and Unravel are auras or debuffs. **Slam and
Mortar are the other two**, and the reason is
[#256](https://github.com/ssalter21/tower-defense-game/issues/256)'s: the rule's bodies term is the `targets`
column, a damage-bubble row must leave `targets` at 1 because a bubble is one shot drawing one roll, so the
rule counts one body for a swing that hits everything touching the Barbarian. Seven is what `show-ladder`
prints and seven is what `docs/roster.md` says.

**No premium was authored to compensate**, which is what the ticket asked for and what the cost section already
argued. Scarcity is the grant schedule and not the price. What changed instead is what the tool *says* on those
seven: the flat-price note now ends *the rule prices the capstone at what it replaces — and nothing charges
that, because a token buys it*, so a reading and a bill are no longer the same sentence.

### The scripted player had to learn to spend one, and what it will not spend

`CoverThenUpgradeBot` builds both walls in the balance report, so a bot that could not spend a token would
leave the mechanic unmeasured — and, worse, would compose phases the rules refuse: its gold loop would still
have scored Fan of Knives and Overwatch on their cost columns and bought them with money nothing charges.

It spends a token the round it arrives, in a loop of its own after the gold, on the capstone that puts the most
damage on the route **per tick** — the gold loops' score with the gold taken out. **The one clause that is not
the gold loops' rule is the comparison against what is standing**: a capstone consumes the rung under it, so
one that scores worse would make a wall worse for a token. That clause was added after watching it happen: the
first build of the loop bought a capstone that lowered the stand-in's wall, and the committed run's fourth
round moved from 160 dealt to 187 — an instrument getting dumber rather than a finding about the roster.

**What the bot will not spend is most of them.** Five of the nine change no damage roll and no body count at
all, and the bot's score is damage — so those tokens go unspent, and the report says so by reporting a run that
did not spend them. That is the score's known blindness rather than a statement about the roster.

### And the sweep cannot see this mechanic at all

**`content/sweep.csv` came back byte-identical**, over seventeen rows and a hundred and thirty-six runs. That is
the reading, and it is a fact about the instrument rather than about the token.

**A ten-round bot run from an empty board ends on fourteen towers and every one of them is a line's *first*
rung** — five soldiers, four rogues, three clerics and a druid, measured on seed 20260807. The cover phase goes
on finding a cheap root that watches route nothing watches yet, and never climbs; with no rung 2 standing there
is no capstone edge to climb, so all three tokens are held to the end of the run. The canned stand-in *does*
reach a rung 2 — it opens behind `content/defense.txt` with a purse of its own — and the capstone above that
rung scores exactly what the rung scores, which is precisely the swap the "must beat what is standing" clause
declines.

**This is [6 September's bounty finding](#6-september-2026--a-kill-pays-and-both-balance-instruments-turn-out-to-be-blind-to-it)
a second time, in a different structure.** A mechanic can be built, tested and correct and still be invisible to
the balance report, because the report is played by one scripted player and that player's rule does not reach
it. Nothing was retuned and no number was softened to produce a movement: **what a reading of this needs is a
player that climbs**, which is a change to `CoverThenUpgradeBot`'s cover phase and not to anything this ticket
decided.

### The bill

One retirement, deliberate, and one label bump with it.

- `content/upgrades.txt` `layout 1` → `layout 2`, and `upgrade-ladder/1` → `upgrade-ladder/2`, for the second
  keyword. The ladder hash goes `6C432E189630BF3C` → `A52476D83A039248` and the content hash it folds into
  goes `CEAD5CE53790DD40` → `FAA0B1831B2CE190`.
- **`SimulationVersion` stays 13.** Nothing in a tick loop moved, and every stream recorded before this is
  already retired at the content stamp.
- **Every record format is free**, again.

**The committed match is untouched by the mechanic** — it is a defense and a wave read off files, with no build
phase in it: 3 of 40 leaked, tick 5302, state `441D37E128517F3D`, the same four landmarks. What moved is its
header, where the content stamp and the defense and wave hashes that fold the roster's content hash all
followed the ladder. **The committed run is untouched too**, and that is a fact about `content/commands.txt`
rather than about the rule: its four rounds place archers and never climb a line.
