# The decision log

**Where [The Vision](vision.md) records the times it changed its own mind.**

A standing document that revises itself silently is one nobody can trust the age of. So every reversal lands
here, with what it said, what is true now, and why it moved — rather than being quietly edited out of the
vision and forgotten.

This file exists so the vision can stay readable. It grows; the vision should not.

**What is *not* here:** where the vision replaces a claim in one of the five archived deep dives. That is
[§9 of the vision itself](vision.md#9-what-this-overturns), because it is how you read
[`archive/`](archive/) rather than a record of churn, and it is stable.

---

## What the vision overturns in itself

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
