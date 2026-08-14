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
| **[§3 — three anchors, a shape and a filling](vision.md#three-gates-at-waves-3-6-and-9)**, and the take gate with it | A public anchor schedule injects major variance at fixed known waves; one take per round, mandatory, bounding what may be fielded | **Deferred, not repealed.** Both come out of the played game until the roster has the depth to make a gate worth having | *There is no point in gating before we have the depth, it just holds back the early testing and experience.* The destination is unchanged; what moved is when it is built |
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
[character roster note](research/kaykit-character-roster.md) lists **six**, and the two unnamed there are a
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
| **[§3 — the gate rounds](vision.md#three-gates-at-waves-3-6-and-9)** | *Nothing bounds how many slots a wave carries* — written when the anchor-derived widths came out with the anchors | **Bounded again, on the same three rounds.** 2, 4, 6, 8 slots and 10, 20, 30, 40 count, fixed and public before a run starts | The widths were deleted as collateral of deleting the menu rather than on their own merits. What they lacked was a reason to exist that was not the menu, and capacity is one |
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
