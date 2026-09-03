# What agents can build unattended, and what each seam needs first

**Research note** · 3 September 2026 · build research

**Question:** with steps 1–4 built, step 5 half-built and the tracker empty, which of the remaining work can an
agent run to completion with nobody at the keyboard, what proves each piece, and what is the smallest thing a
person has to hand over before an `/afk` queue can take it?

The aim behind the question is a change in what Sam's time is spent on: more design and playtesting, less
building. So the note ranks the seams by how little of a person they need, and it says for each one where the
human input lands — in the ticket, before the run — rather than in the run.

---

## Verdict

**The bottleneck on unattended building is ticket supply, not agent capability.** `gh issue list --state open`
returns nothing. `/afk` works a queue of `ready-for-agent` tickets on one effort branch, and there are none to
queue. Every AFK run so far — 14 August's seven tickets, 29 August's two — was preceded by a session that wrote
the tickets. That session is the job that does not go away, and it is the one where design decisions get
pre-loaded so the run never has to ask.

**Three facts shape what is safe to queue.**

1. **The gate verifies the simulation side completely and the client side not at all.** Every push runs the
   sim tests, the IL scan, the poison suite, the six-row determinism matrix and the headless replay; not one
   Unity test. The [software-factory note](the-software-factory.html) called this the autonomy gradient and
   asked for it to be written down. It still is not. What has changed since 7 August is that the client half
   gained its own instruments — three Unity runners that count their tests, `capture-ui-previews.ps1`,
   `capture-match-frames.ps1`, and synthetic clicks driving the built player — and every one of them needs the
   editor **closed**. AFK time is exactly when it is. So the client is now agent-verifiable *unattended* in a
   way it is not while Sam is working.
2. **Three standing rules block a class of work by policy rather than by capability.** Art is never chosen
   unattended, gameplay options are never named unattended, and an imbalance a change exposes is left standing
   rather than tuned away. None of that is a limitation to work around; it is where Sam's design time is meant
   to go. The consequence for AFK work is that a ticket in those areas has to carry the decision already made
   — a signed row, a chosen framing, a named currency — or it has to say in as many words that the agent may
   ship visibly-provisional filler.
3. **What an agent can always do in a decision's absence is measure the options.** The sweep runs fourteen
   thousand matchups in a dozen seconds and takes every content file as a parameter. A ticket that says
   *produce the numbers for these three readings of the Mage* is fully unattended; the signature stays Sam's
   and arrives with the evidence beside it. The 17 August layout choice — six candidates rendered, one picked
   by looking — is the same pattern on the visual side, and it is the pattern most of the B-tier below rests
   on.

---

## What each one buys, and whether it is worth it

The tiers say what an agent *can* do. This says what you *have* at the end, so the rows can be weighed against
the goal — more of your time on design and playtesting — rather than against how automatable they are.
**Worth it** is one of: **Now** (do it in the first queue), **With** (do it alongside the row it names), **Later**
(real value, but not before the rows above it), **Hygiene** (cheap, protects the AFK runs, changes nothing you
play).

| # | What you have at the end | Worth it | Why |
|---|---|---|---|
| A1 | AFK runs that know what they may touch unattended and what they must hand back | Hygiene | Half an hour; every later run reads it. Changes nothing in the game |
| A2 | A regenerated golden file cannot slip through an unattended run unread | Hygiene | The one hole an AFK agent can fall through; the more runs, the more it is worth |
| A3 | Docs that go red when they lie: a cited issue closed, a tick moved, a stale picture | Hygiene | Three drifts found today by hand; this finds the next ones for free |
| A4 | Sheets and frames of the board that actually ships | With A3 | Trivial once A3 exists; it is A3's first finding |
| A5 | Answers to the sweep's owed questions: does spread across plans exist, does breadth beat depth, where did this run sit | Later | Nothing you play. Its value is that A9 and B3 both read it |
| A6 | **The loop the vision describes, at zero latency.** Every run you play adds opponents; each run meets a different ten; the flat canned field goes | **Now** | The first thing that makes a run feel like the design rather than a fixture. Do B2 first or the pool is a folder of mage-walls |
| A7 | After a run, *the sniper instead of the tank at wave 7 was worth this much*, and how the round went over all ten, as text | Later | Real, and the numbers the client's post-round screen will read. Nothing to see until seam 7 draws it |
| A8 | Three more rows in the landmark table | Later | Pays off only when a directed camera exists |
| A9 | Fifty maps ranked by how much good plans disagree on them, each pictured, with the hand-drawn map's row among them | Later, as its own effort | The one large item. It answers whether the map you drew is good, and it is the renewable-depth mechanism the vision leans on. Fitness definition is an assumption you should read first |
| A10 | You learn a Unity test broke the morning after, not weeks after | Hygiene | The bow rendered wrong for weeks under a green suite; this is the cheapest catch |
| B1 | **The build phase you chose on 17 August is the one you play**: hex menu, portrait rail, one-line header | **Now** | This is the surface you will playtest on. Everything about feel is judged here |
| B2 | Ghost walls and the sweep's defense stop upgrading into a worse tower; rounds six to ten mean something | With A6 | Small; fixes the artefact every measurement reads. One decision from you |
| B3 | Number sheets for six towers and the aura, priced and swept; you sign; the roster goes from four towers to ten | **Now** | The 13 August finding was *the roster is too shallow to judge composing a wave*. This is that finding, answered |
| B4 | The Mage priced for what it does; the Soldier with the sweep his page describes; damage buffs authorable | With B3 | Three one-word decisions; the rows land with B3's |
| B5 | A slowed creep looks slowed | With B3, once a Cryomancer is signed | Pointless before then |
| B6 | The sit-down's seven tick rows point at something the build plays | Whenever | Tidy, not load-bearing |
| B7 | Sheets to pick from: how a tier reads, how a hit lands, what each faction looks like | **Now** | Look and feel is the stated priority, and picking from a sheet costs you minutes. Tier legibility is a veto, not a nicety |

**If only four rows happen, they are B3, B1, A6 with B2, and B7.** Those are the ones that change what a
playtest is: a roster deep enough to compose against, the chrome you chose, opponents that differ between runs,
and a look you picked. The hygiene rows go first in the queue because they are cheap and they make the rest
safe to run unattended, but nothing about the game moves when they land.

---

## The seams, ranked

**A** — fully unattended: the gate or a runner proves it and no design decision is inside it.
**B** — unattended once one named decision is written into the ticket.
**C** — not unattended, and the reason.

| # | Seam | Tier | Size | What proves it | What a person hands over first |
|---|---|---|---|---|---|
| A1 | Write the autonomy gradient into `AGENTS.md` | A | S | Human review of a seven-row table | Nothing |
| A2 | A tripwire on `-Regenerate`: a gate step that refuses a golden-path diff without a label | A | S | The gate goes red on the next undeliberate regeneration | Nothing |
| A3 | A documentary checker: cited issues, quoted ticks, stale sheets and frames | A | S | It is a checker; the gate runs it | Nothing |
| A4 | Re-capture the chrome sheets and the match frames on the folded, tiled board | A | S | The pictures are of the board that ships | Nothing |
| A5 | Sweep queries: outcome spread, ingredient bins, the both-columns check, where a run sat | A | S | `dotnet test`; a golden query output | Nothing |
| A6 | Step 6 — opponents read from a folder of ghost records, populated by the bots | A | M | Golden run, determinism, sim tests | Nothing |
| A7 | The retrospective's number half: what-if re-runs and the distribution over the field | A | M | Determinism; a golden what-if | Nothing |
| A8 | More landmarks: the closest call, the deciding shot, the biggest leak | A | S | Golden landmark table | Nothing |
| A9 | A map generator: seed → map, checked by the parser's own rules, scored by the sweep | A | L | Determinism, parser acceptance, the sweep's spread column | Nothing to build it; a look at the SVGs to judge it |
| A10 | A nightly Unity run on this machine, editor closed | A | S | The three runners' counts | Nothing |
| B1 | Step 5 — the chosen build-phase chrome: hex menu, portrait rail, minimal header | B | M | `ChromeLayoutTests`, a preview sheet held against `chosen-build-phase.png`, a synthetic-click drive of the built player | One line: the portrait framing |
| B2 | The upgrade half of `CoverThenUpgradeBot`, and its coverage cap | B | S | The sweep re-ranks; golden sweep | Which of the three answers on file |
| B3 | Roster candidates: numbers, prices and measured effects for the six proposed towers and the Necromancer aura | B | M | The sweep; `ContentTests` | Nothing to measure; a signature to land |
| B4 | The Mage's splash, the Soldier's sweep bubble, the damage-payload keyword | B | S | `ContentTests`, golden trace regenerated deliberately | One reading each, and a keyword |
| B5 | A slowed creep visible in `CreepSnapshot` | B | S | Play-mode tests; a captured frame | Which field |
| B6 | Sit-down rows 4–10: re-anchor or retire | B | S | `SitDownTests` | Which of the two |
| B7 | Presentation candidate sheets: tier legibility, hit reactions, faction palettes | B | M | A person looking at a sheet | Nothing to render; a pick to build |
| C1 | The capacity schedule and the capstone token | C | — | — | Not until the roster is deep enough to ration; recorded on 13 August |
| C2 | Naming: the defense currency, any new unit, any game changer | C | — | — | Sam's, always |
| C3 | Retuning health, prices, income, the bonus rate | C | — | — | Sam's, and the sweep is the argument |
| C4 | The service, the social layer, rating, the gamble, co-op | C | — | — | After step 6, and hosting is a decision |

### A1 · The autonomy gradient, written down

Half an hour, and it is the rule every other row here depends on. The factory note's table is still right and
it is still nowhere: `sim/`, `simcli/`, `tools/` and `content/` are green-gate territory; `docs/` is drafted
freely and reviewed by a person; the client's non-visual half is written by an agent and proved by the runners;
its visual half is proposed by an agent and decided by a person; art is human-only. What has moved since the
note is the row for the runners — an agent runs them itself now, editor closed, and reports the counts. The
`writing-for-agents` skill is the tool; the table goes in AGENTS.md as an instruction, not a finding.

### A2 · The `-Regenerate` tripwire

A change to the rules plus a regeneration is a green gate on all six matrix rows, and nothing marks it. A gate
step that fails a pull request touching `content/golden-trace.txt`, `content/landmarks.txt`, `content/golden/`,
`content/sweep.csv` or `content/run-outcome.txt` unless the PR carries a `regenerated-deliberately` label makes
the escape hatch loud. It matters more the more runs happen unattended: an AFK sub-agent that regenerates to
get green is the dark factory's exact failure, and this is the one-line guard against it.

### A3 · The documentary checker

The slop surface here is prose, and it has drifted again since the last time it was measured:

- `docs/chrome/as-built-*.png` and `docs/frames/match-tick-*.png` are dated 14 and 17 August. The board folded on
  the 27th and was tiled on the 28th. All five show a flat grey-green corridor.
- The vision still says the KayKit purchase is pending browser confirmation under #56; the whole collection is
  on disk and imported (#229).
- `docs/README.md` says 51 ADRs; there are 56.

A `tools/check-docs.ps1` that walks `docs/` for `#<n>` citations and asks `gh issue view --json state`, compares
every tick quoted in `sit-down.md` to `content/landmarks.txt`, and refuses when a committed sheet or frame is
older than the last commit to `content/map.txt` or `content/units.txt`, is a morning's work and runs in the
gate. The factory note asked for it as a nightly cron; a gate step is cheaper and needs no scheduler.

### A4 · Re-capture the sheets and frames

Both capture tools exist and are static entry points. This is one ticket, and it is the ticket A3 would have
filed on 28 August.

### A5 · Sweep queries

`--per-run` writes a row per run, so the columns the sweep was owed — outcome spread across plans, win rate
binned by ingredients taken, the both-columns check — are queries over a file that already exists. So is the
cheap third answer to the open question on `PerformanceField`: *the sweep reporting where a run sat* gives the
measurement a consumer that is not the purse. A `simcli query` verb or a `tools/query-sweep.ps1`, each query a
golden output. The keep-or-delete decision on the field stays Sam's; building the consumer does not pre-empt it,
it prices it.

### A6 · Step 6, opponents from a folder

The build order sizes this small — `GhostRecord` already round-trips, carries `MapHash` and `MapHandle`, and
#208 already made the pool a population per round. What is missing is the folder: a `simcli` verb that writes
each round of a played run out as a ghost at `(map, stage)`, a draw of K from the folder at the run's stage, a
top-up from the canned field when the folder is short, and the client reading the same folder. The pool is
populated by the scripted players — `even-share`, `all-in`, `CoverThenUpgradeBot` — which the vision's own
sources name as the answer to an empty pool. Nothing here is a design decision: a defense is a placement, not
an option a player is offered.

Two things the ticket should say plainly. A folder of bot-played ghosts inherits every complaint on file about
the bot (B2), so the sweep's caveat row travels with it. And ADR-0042 is largely superseded and should be
amended in the same commit rather than left describing a field nothing measures.

### A7 · The retrospective, number half

§9 of the vision says the simulator's home is the retrospective, and the CLI memory says numbers are fair game
at a prompt while pictures wait for the client. The numbers: re-run a finished run with one purchase changed
and report the difference; the distribution of a round over the whole field rather than the watched member;
the best and the average side by side. A `simcli what-if --commands <file> --round <n> --instead <build row>`
is one verb and one golden file. The client's post-round screen is seam 7 and comes later; this is what it will
read.

### A8 · More landmarks

Four landmarks exist. *Closest call*, *the shot that decided it* and *the biggest single leak* are computable
from the outcome vector and the event stream, each a golden row, and they are what a directed camera will be
pointed at when the presentation seam gets there.

### A9 · The map generator

The vision defers generation behind *one map that is demonstrably good to calibrate against*. The hand-drawn
map exists (#218) and has been measured, and the measurement was that it spends its room on descents — a
serpentine on the same grid reaches ninety hexes against its fifty-one. So the reference is known rather than
good, and the honest way to find out whether it is good is to score it beside candidates.

Everything the generator must satisfy is already a parser rule or a decision-log sentence: one path, never
branching; no corridor cell with three corridor neighbours; three tiers; a tier change sits between two cells of
the same row with corridor either side; a leg needs four cells to hold one; the exit's tier. `HexMap.ParseUtf8`
refuses what breaks the first three, the same sentence the map editor already uses. A candidate is a seed; the
sweep takes the map as a parameter and `--per-run` gives the spread; `draw-map` writes the SVG. The deliverable
is a ranked archive of, say, fifty candidates with the committed map's row among them, and a folder of pictures.
That is a large seam whose every step is checked by machine and whose output is exactly what a person should
judge by looking.

One assumption, stated: *good* is what the vision says it is — how widely outcomes spread across good plans —
and *good plans* are the three scripted players that exist. If Sam wants a different fitness, that is a line in
the ticket.

### A10 · A nightly Unity run

The Unity tests run nowhere but this machine, with the editor closed. A scheduled task at 03:00 that runs the
edit-mode, play-mode and player runners and writes the three counts to a log is the cheapest possible gate on
the half the gate does not cover, and it costs nothing while Sam is asleep. The `/schedule` skill or a Windows
task; either is fine, the log is the point.

### B1 · Step 5, the chosen chrome

Decided on 17 August from six rendered layouts, and not built: the as-built chrome still has the bottom bars.
Three pieces. The **hex menu** — options opening beside the lit cell, portrait, name and price each — replaces
the palette bar. The **rail** — the wave in portraits, in send order, draggable — replaces the wave bar. The
**header** shrinks to one line and a commit.

The one decision inside it is the portrait. `RosterThumbnails` returns null on purpose; the mockups borrowed
`capture-armed-roster.ps1`'s framing, a three-quarter front at 215°, keyed and cropped square, and the open
question says that is a stand-in. Sam chose the layout looking at those exact portraits. One line in the ticket
— *bake the portraits with the armed-roster framing, under `tools/`* — turns the whole seam into tier A. The
proof is a preview sheet at the chosen layout's states held beside `chosen-build-phase.png`, then a
synthetic-click drive of the built player through one round.

The rail's second loose end — whether the wave is always on screen or behind a control — is a sheet either
way. Render both; Sam picks.

### B2 · The bot's upgrade half

Three answers on the 29 August entry, none a tuning pass, and *what settles it is what the report is for*. Once
one is named, the change is a rule in one class, the sweep re-ranks, and the golden sweep is regenerated
deliberately. The coverage cap — a wall that stops at six towers however rich it gets — is the same ticket with
a second rule in it. This one is worth deciding before A6, because a folder of ghosts is a folder of this bot's
walls.

### B3 · Roster candidates

The single finding that gates the design question — *is composing a wave interesting* — is that the roster is
too shallow to tell. Every one of the six proposed towers and the Necromancer's aura is authorable and playable
today; each is blocked on a name and a set of signed numbers, and signing is Sam's.

What an agent does unattended is price and measure. For each proposed unit: a few candidate number sets, each
priced by the rule in `roster.md`, each swept against the roster under both policies, reported as dealt, taken,
leak and cost efficiency per creep, with the fold's caveat rows attached. The output is a sheet of numbers per
unit, ready to sign. Landing a signed row is a line, a `ContentTests` pin and a deliberate regeneration.

This is also where the CLI memory's line sits: a number is fair game at a prompt, and this is nothing but
numbers.

### B4 · Three small design readings

Each is a one-line decision and a small ticket after it. The Mage: author the splash and accept an unpriced
radius, reprice the row to 30, or fire three shots. The Soldier: author the self-centred sweep the roster
describes, or leave the single strike. The `bubbleMagnitude` gap: a sixth payload keyword distinguishing the
attack's roll from the damage stat, which the open question says is roster vocabulary and not an agent's to
name. Once named, both halves of the signed column come back for no format version.

### B5 · A slowed creep on screen

Timed effects are internal state. The day a Cryomancer is signed a creep will walk at four tenths of its speed
for no visible reason. The field to add to `CreepSnapshot` is a view contract under ADR-0007 — *is it slowed*
and *what is on it* are different contracts — and choosing is a sentence. The drawing is a play-mode test and a
captured frame.

### B6 · The sit-down's seven tick rows

Re-anchor to a round a run reproduces, or retire onto `LocomotionTests`. Both are cheap; the open question says
so. A word decides it.

### B7 · Presentation candidate sheets

Seam 8 is *agent proposes, human decides*, and the 17 August method makes proposing unattended. Three sheets
worth rendering without being asked: how a tier reads — colour, edge, height exaggeration — because a player
who cannot tell the tier cannot read the range; hit reactions and death weight on the creeps, from the clips
already imported; and a faction recolour, which is one PNG per side. Each sheet is a scratch class staged for
one run, the way `BuildPhaseCandidates` was. Sam looks; the pick becomes a ticket.

---

## What is deliberately not on the list

**The capacity schedule and the capstone token (C1).** Decided in the vision and explicitly not a ticket in the
build order: a schedule of slots and count caps rations room, and two slots against four creep types is the
shallow-roster complaint one round further on. It comes back when B3 has landed enough rows for a gate to be
gating something worth having, and its integers are sweep targets when it does.

**The three absent creep shapes.** Fast-and-cheap in numbers, slow-and-tough, fast-and-durable. Each is a row,
and each is blocked on a model, which is an art decision.

**Creep upgrades.** The 13 August direction is that creeps deepen by being upgraded rather than replaced, and
there is no mechanism written for what an upgrade on the sending side is — a dearer tier in the same slot, a
stat bought per creep, or something else. That is a design conversation before it is a ticket.

**The service.** Buildable, and after step 6. Where it runs and what it costs are decisions, and it is the one
permanent obligation in the plan.

---

## What this means for how the work is run

**Sam's job moves upstream, and that is the point.** The tickets are where decisions get pre-loaded: a framing,
a keyword, a number set, a pick from a sheet. An AFK run with a decision missing skips the ticket; a ticket with
the decision in it runs to green. The higher the share of tickets that carry their decisions, the higher the
share of building that happens while nobody is watching.

**A first queue, if the ranking above is accepted.** A1, A2, A3, A4, A10, then B2 and A6, on one effort
branch — the hygiene rows first so the gate is hardened before anything larger runs behind it. Only B2 needs a
word from you before the run starts, and it is one of three answers already on file. A5, A7, A8 and A9 wait;
A9 is its own effort when it comes.

**A parallel session that is not AFK.** B3's number sheets are the input to the one conversation that unblocks
the design question, and B1's one-line framing decision unblocks the chrome. Half an hour on those two turns
most of the B tier into A.

**Two things AFK runs should carry from the memory.** Dispatch sub-agents with `AGENTS.md` and a starting SHA
and have them run `/code-review` against it — `/implement` refuses a sub-agent, and the review gate caught a
shipped bug on every ticket of the 14 August run. And never report engine-side work done off a green suite
alone: capture a frame and look at it, or drive the build.

---

## Sources

Everything here is verified in this repository at commit `49b8964`, 3 September 2026: `docs/vision.md`,
`docs/build-order.md`, `docs/open-questions.md`, the 27 and 29 August entries of `docs/decision-log.md`,
`docs/roster.md`, `docs/research/the-software-factory.html`, `AGENTS.md`, `.github/workflows/build-gate.yml`,
`tools/`, the closed issues #173–#229, and `gh issue list --state open`, which is empty. Test counts as PR #229 reported them: 815 sim, 82 edit-mode,
136 play-mode.
