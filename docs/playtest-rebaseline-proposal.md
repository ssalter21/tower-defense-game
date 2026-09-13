# The playtest rebaseline

**A proposal, written 13 September 2026, for review.** It decides nothing. The vision moves only when Sam
edits [`vision.md`](vision.md) and records the reversal in [the decision log](decision-log.md); this page says
what those edits would be and why, so that the sitting that makes them has the whole picture in one place.
It is the same kind of document as [the roster expansion proposal](roster-expansion-proposal.md): the
argument as it was put, kept beside what was taken from it.

**The goal it is written against, in Sam's words:** *a lobby of around six friends, playtesting, as soon as
possible.* Six things stand between the build and that goal, and every one of them wants an MVP rather than
a finished version:

1. The tower animations and projectiles do not look good, even as placeholders, and want a new approach.
2. The UI is undirected; the vision is that a hex is selected and the build or upgrade options open in a wheel.
3. The board has odd angles and janky shapes between tiers and wants smoothing.
4. There is no main menu and no settings, so there is no wrapper for an actual game.
5. Scoring is not what the vision wants: a build plays against every other board in the lobby, and a score
   is where you sit on the curve, in the way Opus Magnum scores a solution.
6. The sweep harness does not do what it is needed for, and wants a specification of its own.

A seventh is not on the list and is the largest gap: **there is no networking of any kind in the project**,
and a lobby of six needs a way for six machines to exchange rounds. Section 3 puts it beside the scoring,
because they are one effort.

---

## 1. What the goal changes, and what it leaves alone

The standing documents are written around a different sequence: find the loop at zero latency against
opponents read from a folder, judge the economy and the roster from a shell, and defer every mode of
multiplayer until after the loop is found. That sequence has done its work. Steps 1 to 6 of
[the build order](build-order.md) are built, the roster has forty-four rows, the client can be clicked and
played, and the thing the sequence was protecting the project from, an engine effort spent on a game that
was not fun, is no longer the risk. The risk now is that nobody but Sam has played it.

**Three headline claims in the vision stop being true under the new goal.**

| Where | What it says today | What the goal makes true |
|---|---|---|
| [Bottom line, pillar 4](vision.md#bottom-line) | *The multiplayer is real, and all of it is deferred.* | The lobby is the next thing built. The round-robin, co-op and the social layer stay deferred |
| [§3, what a match is](vision.md#3-what-a-match-is) | *Runs rank by waves survived, then health remaining; the offense never enters the placing.* | A run has several scores, each a position on the lobby's curve, and the offense is one of them |
| [§6, what it looks like](vision.md#6-what-it-looks-like) | *Art is not a risk item.* | The models are not the risk. How they animate, and what a shot looks like, is the work standing between the build and a playtest |

**Two more move by degree rather than by reversal.** [§2](vision.md#2-the-loop--one-machine-at-three-latencies)
says every round draws ten stored defenses and ten waves, and a lobby smaller than ten is topped up from the
pool. For a lobby of six friends the honest reading is *everyone present, and nobody else*: topping up from
the pool would have four of every ten opponents be the sim's own bot, which nobody in the room composed and
which dies at wave 4 under the signed roster. [§7](vision.md#7-what-runs-it) says a real server is the only
permanent obligation. It still is, and the lobby MVP does not need it yet, for the reason in section 3.

**What the goal leaves alone, and it is most of the vision.** One machine at every latency, the submission
barrier, no lockstep and no rollback, the planning phase as the whole game, nothing forecast and everything
mechanical shown, determinism as the deliverable, the maze that folds and climbs, KayKit as the final art
pipeline, nothing persisting but a rating. In particular [§9](vision.md#9-the-planning-phase-is-the-game)
already says *place players against a histogram, not a leaderboard*, and
[ADR-0035](adr/0035-a-runs-outcome-is-a-vector-and-health-is-a-clock.md) already refuses a single score. The
Opus Magnum scoring is that sentence built, not a new direction.

**The build order's sequence needs a new step 7.** Today it reads *then the generative depth, the two-board
interface, and the service*. The playtest wants none of those first. The proposed step 7 is the six MVPs
below, ordered in section 5, with the seams they belong to named so nothing is re-derived.

---

## 2. Where the build stands

Read from the client on 13 September, so the MVPs below are scoped against what exists rather than against
what the documents describe.

| Area | What exists | What is missing for the goal |
|---|---|---|
| **Animation** | A sim-driven Playables graph (`SimDrivenAnimator`) that poses a clip at a phase and never advances on its own, so scrubbing works and `LocomotionTests` holds. A tower has three slots, idle, windup and backswing, and the windup and backswing clips are **stretched across the authored tick counts**. Creeps walk by distance and die over the authored `dying` ticks. Clips come from the real KayKit rig banks | Hitscan towers are never posed at all; they stand in import pose. A clip stretched to fit a number is what *looks terrible*: a swing authored at 20 frames played over 11 ticks or 27 ticks is a swing at the wrong speed. No release frame: the shot leaves on the swing clip's last frame. No animation ADR |
| **Projectiles and effects** | One projectile kind, a sphere on a parabola from a derived origin. Seventeen decoration pieces in `MatchDecorations`, aged in ticks and cleared on seek, all built from code-generated meshes (`EffectMeshes`). Every aura and blast is one flat translucent disc. No particle system, trail or line renderer anywhere in game code | Real projectile models, trails, muzzle and impact effects. The billboard guard permits mesh-mode particle systems and bans everything that faces the camera, so real VFX is allowed by the rule and blocked only by never having been built |
| **Chrome** | UI Toolkit, written in C#, no UXML or USS. A header (wave, health, gold, capstones, one button), a bottom palette of nine roots with the upgrade ladder drawn beside the hex, a scrolling wave bar, playback controls and an offence/defence switch. Every piece signed on 11 September as a holding answer under the words *the UI will need a major rework* | A wheel. Portraits: `RosterThumbnails` returns nothing, so every surface is text |
| **Wrapper** | One scene, `Match.unity`, one root, and the player launches straight into a fresh run's first build phase. The end of a run is a text panel in the header | A main menu, settings, a lobby screen, an end-of-run screen |
| **Board** | 247 cells, a 51-cell corridor, nine half-block levels, no two touching cells more than one level apart. Tiles are KayKit pieces; a step is a stacked cliff post; the corridor climbs on the pack's ramp pieces; ground cells rise on its sloped tiles | Anything that joins two tiers as a slope rather than a cliff. The research note that made the levels half-blocks rejected decorative ledges and never tried a continuous surface |
| **Opponents and scoring** | A pool folder of stored rounds, one file per round, drawn by stage; the canned field stands in for what the stage cannot fill. A run's outcome is a per-round vector of leak cost dealt, leak cost taken and bounty; placing is waves survived then health. `PerformanceField` computes a percentile against a population and nothing reads it | A lobby: six pool folders that are one folder. A score that is a position on the lobby's curve, per metric |
| **The sweep** | A `simcli` mode and a CSV: every creep against a wall of every attack type, played by a scripted bot, folded to a row per pair, with per-run rows on request. Verified against a committed report | Whatever Sam needs it for, which is not written down. What is written down is what it cannot see: a capstone, a kill's bounty, and any wall better than the bot builds |

---

## 3. The six efforts

Each has a destination in one sentence, the MVP, what it builds on, what the standing rules say about it,
the decisions that are Sam's before an agent starts, what verifies it, and the documents it moves.

### 3.1 · The lobby, and how it scores

**Destination.** Six friends each build a board and compose a wave; every round, each wave runs at every
other board in the lobby; each player sees where their round sat on the lobby's curve, per metric, the way
Opus Magnum shows a solution against every other solution.

**The MVP is a shared folder, and the format already exists.** A stored round is a wall and a wave at a stage
([ADR-0057](adr/0057-a-stored-round-is-a-wall-and-a-wave-at-a-stage.md)), a run draws the rounds stored at its
stage, and the client already reads a pool folder. A lobby is that folder shared between six machines by
whatever syncs files (a Dropbox or Drive folder, Syncthing, a network share). The barrier from
[§2](vision.md#2-the-loop--one-machine-at-three-latencies), *everyone present has submitted*, is the client
counting the rounds at stage N in the folder against the lobby's size and waiting. Resolution is local and
deterministic on every machine, so every player watches the same match without exchanging results. A round
is hundreds of bytes. No server, no sockets, no accounts, and [§7](vision.md#7-what-runs-it)'s obligation is
deferred rather than repealed, exactly as the build order says it should be.

Two facts to state before it is chosen. A synced folder has no ordering guarantee and can show a half-written
file; the record reader already refuses a truncated record and the folder rule already says a stale record
must not stop a run, so the barrier polls and re-reads rather than trusting a count once. And the same design
runs a lobby over a relay later, because a relay is a folder with a network in front of it.

**The field is the lobby, not the pool.** K, the number of opponents a round is resolved against, becomes
*everyone else present*, five in a lobby of six, and the pool tops nothing up. Health taken stays the field
average, as §3 says, so a round against five is one number of health lost. A lobby of two plays against one
opponent and that is fine for a test.

**Scoring, the Opus Magnum shape.** Opus Magnum gives a solution three numbers, cost, cycles and area, and
for each one shows a histogram of every player's solutions with yours marked. It never adds them. The
proposal for the lobby is the same: per round and over the run, each player gets a position on the lobby's
curve for each of a small set of metrics, and the position is the score. The outcome vector already carries
the candidates:

| Metric | What it measures | Already computed |
|---|---|---|
| Leak cost dealt | How much the wave you sent hurt the other five boards | Yes, per round |
| Leak cost taken | How much the five waves hurt your board | Yes, per round |
| Health remaining, or waves survived | The old placing, kept as one metric among several | Yes |
| Gold unspent, or gold per point of damage | Efficiency, the analogue of Opus Magnum's cost | Spent and unspent gold are on the sweep row already; the run would need to carry them |

`PerformanceField` computes a percentile against a population, and the
[open question](open-questions.md#is-the-field-measurement-kept-now-that-nothing-prices-off-it) of whether to
keep it is answered by this: it gets its consumer, and the consumer is the score. With six players a
percentile is coarse, so what is shown is the six bars with yours lit rather than a number.

**What is Sam's before an agent starts, in plain words.**

- Which metrics are scored. The four above, fewer, or others. Short term: each one is a bar on the end-of-round
  screen. Long term: each one is a thing a player optimises for, and a metric nobody can move is noise.
- Whether the positions combine into one placing at the end of the run. Opus Magnum does not combine. A single
  placing makes a winner, which a room of friends will want; it also makes the offense enter the placing, which
  §3 forbids today and this proposal reverses.
- Whether health taken stays a field average or becomes the sum of five waves. Average keeps every number the
  sweep produced comparable. Sum makes a lobby of six twice as lethal as a lobby of three.
- Whether a player sees the other five boards before committing, which §3 calls scouting in the lobby: the
  opponent's defense as of the end of the previous round, stale, never live. The folder makes that free.

**What verifies it.** A lobby of two on one machine, two client instances against one folder, driven by
synthetic clicks (`run-player-tests.ps1` is the input-free check). Determinism makes the assertion cheap: both
instances must show the same match and the same six bars.

**Documents it moves.** Vision §2 (K for a lobby), §3 (the placing), pillar 4, §7 (deferred, not repealed);
build order step 7 and seam 2 (this is seam 2's first half, built early); open questions (the field
measurement, rating at two scales, the gamble); a decision-log entry for the placing reversal; a new ADR
*a lobby is a shared folder of stored rounds, and the barrier is a count at a stage*.

### 3.2 · The hex wheel

**Destination.** Select a hex, and the options for that hex open in a wheel around it: the nine roots on an
empty cell, the upgrade ladder on an occupied one. The chrome shrinks to what is not about a particular hex.

**The MVP.** One wheel, drawn by UI Toolkit in C# like everything else, positioned by the same
world-to-panel call the ladder uses today, with a wedge per option carrying a name and a price and refusing
what the purse or the rules refuse. The palette bar goes; the header stays; the wave bar stays where it is
until the wheel is played. Portraits are not in the MVP. A wedge with a name on it is playable, and
`RosterThumbnails` becoming real is a separate art decision the wheel should not wait on.

**What the standing record says.** The 17 August arrangement,
[`chosen-build-phase.png`](chrome/chosen-build-phase.png), put the towers *at the hex* with the options opening
beside it and the wave along a side rail in portraits. The wheel is the same decision with the menu wrapped
round the cell instead of beside it, so the arrangement is superseded in one respect and confirmed in the
other. The [open question](open-questions.md#the-questions) of whether the wave is always on screen or behind
a control is reopened by a wheel, because the natural place for the wave is a wheel on the entrance hex, and
that is exactly *behind a click*, which the 13 August finding warns against.

**What is Sam's before an agent starts.**

- What the wheel holds on an empty hex: the nine roots, or only the ones the purse affords, or the roots with
  the ladder reachable from each. Nine wedges with names fit; nine with ladders do not.
- Where the wave is composed. On the bar as today, in a wheel on the entrance hex, or both. Short term this is
  one sheet either way. Long term a wave behind a click is a wave composed less carefully, and the attacking
  half is already the underweighted one.
- Whether the wheel opens on hover or on click. Hover is what lights a cell today; a wheel that opens on hover
  covers the neighbours a player is about to look at.

**What verifies it.** A sheet from `capture-ui-previews.ps1` at each of its four states, then a played round.
`ChromeLayoutTests` holds the roster against the wedges the way it holds it against the bar. The rule from
[the chrome sheets](chrome/README.md) stands: a layout is chosen from a picture, never from a sentence.

**Documents it moves.** The chrome README (the chosen arrangement is superseded on the menu and kept on the
rail; a new chosen sheet); build order seam 7 (the wheel is the planning surface's MVP; the watching surface
is untouched); open questions (the wave's home, the thumbnail); a decision-log entry.

### 3.3 · Tower animation and projectiles, one tower at a time

**Destination.** Every tower fires with a swing that reads as a swing at the game's speed, releases a shot on
the frame the swing says it does, and the shot is a thing with a shape, a trail and an arrival.

**Why it looks wrong today, so the new approach answers the right problem.** The view already has the right
architecture: a pose is a pure function of the tick, so scrubbing and fast-forward are correct and
`LocomotionTests` proves it. What it does with that architecture is stretch one clip over the windup number
and another over the backswing number. A KayKit swing is authored at one speed; played over 11 ticks it is a
blur, over 27 it is a hesitation, and on the sixteen rows signed on 13 September Sam saw both. Hitscan towers
skip animation entirely. The shot leaves on the clip's last frame, not its contact frame. And a shot is a
sphere, a knife is a generated mesh, and an impact is a disc on the floor.

**The proposed approach: an animation score per tower, sampled by tick.** Each tower row gets an authored
timeline, kept in the art file beside the row: which clip plays in which state, at what speed, and on which
frame of the swing the shot is released. The simulation's windup and backswing numbers are then derived from
the score rather than the clip being stretched to the numbers: windup is the frames before the release frame
at the signed speed, backswing the frames after, both in ticks. The view still asks *what is the pose at
tick t*, so [ADR-0019](adr/0019-the-view-has-no-clock.md) holds, seeks stay exact and the two locomotion
tests stay green. The one design consequence is that a rate of fire becomes a property of the clip and the
speed it is played at, which is a balance input; the cost rule already reads none of it, so nothing is
priced differently until the sweep is.

For projectiles, the same idea: a shot is a KayKit model on a path from the release frame's hand position,
not from a derived point, with a trail as a mesh-mode particle system, which the billboard guard permits.
An impact is a short mesh-mode burst. A particle system can be simulated to a time and restarted, so an
effect on a seek is re-simulated to the tick the way the match is, and
[ADR-0008](adr/0008-match-events-are-decorative.md) is kept: the effect is drawn from what the snapshot says
happened, never from an event the seek did not hear.

**Per tower, because Sam said so and the rules agree.** Nine lines, twenty-seven rows, and each row's clips,
speed and release frame are a look, which is human-only under rule 6 of AGENTS.md. The pattern that worked on
the roster sign-off applies unchanged: a prototype ticket renders a line's candidates through the real match
at the shipped framing and decides nothing; a sitting signs one; the signed score lands with a re-captured
frame. Creeps get the same pass second, since their walk is already right and their deaths are signed.

**What is Sam's before an agent starts.**

- Which line goes first. The recommendation is one melee line and one ranged line, so the two shapes of the
  problem are settled before the other seven copy them.
- Whether the release frame sets the windup number, or the signed number picks the clip speed. The first keeps
  the clip honest and moves sixteen content numbers again; the second keeps the numbers and accepts that some
  swings are played fast.
- What a shot is allowed to be: a model from the pack, a generated mesh, or a particle trail alone.

**What verifies it.** `capture-match-frames.ps1` at the release tick, before and after, plus a played round.
An assertion should come out of it: the release frame of the signed score is the tick the simulation fires
on, which is a test over the art file and `Match`, not a picture.

**Documents it moves.** The roster's `Looks` line per tower gains the score; vision §6 (the sentence about
risk, and the placeholder ADR-0028's seam closing); a new ADR *an animation is a score sampled by tick, and the
release frame is the shot*; an amendment to ADR-0008 saying how a particle effect survives a seek; build order
seam 8; the decision-log entry that quotes the 13 September sentence and answers it.

### 3.4 · Board smoothing

**Destination.** Tiers read as terrain: a slope where the map climbs, a lip where it steps, never a stack of
posts under a floating tile.

**Where the jank comes from.** A step between two cells one level apart is drawn as the higher tile with a
cliff post under it, and only the corridor gets the pack's ramp pieces; ground cells beside it rise on sloped
tiles whose slope points one way. Where three cells of three heights meet, the pieces do not agree and the
corners show. [The research note](research/what-makes-the-board-read-flat.md) halved the level so a step is
half a metre, tried decorative ledges, measured no tonal change and rejected them; it did not try a
continuous surface.

**The MVP, two candidates, rendered not argued.** One: **a skin over the grid.** One generated mesh whose
vertices are the hex corners, each corner at the mean height of the cells meeting there, so every tier
boundary becomes a slope by construction and the corridor keeps its pack pieces on top. Two: **pieces for
every case.** Keep the tile per cell and add the pack's slope and corner pieces for each of the neighbour
configurations the map produces, which the slope limiter bounds to one level of difference. The first is a
mesh and a day; the second is an inventory of cases and a week, and it stays on the pack's look.

**The rule that bounds both.** Height is load-bearing information. Build order seam 8 makes tier legibility a
veto: a player who cannot tell which level a placement is on cannot read its range. A skin that smooths a
step out of sight also smooths the level away, so whichever candidate is chosen is judged on whether the
level can still be read, and a contour line or a colour band per level is on the table beside it. Picking
stays a rule about cells: `HexPicking` reads the map, not the mesh.

**What is Sam's.** Which candidate, from a sheet rendered by `render-map.ps1` and `capture-match-frames.ps1`
at the shipped framing, with the level-readability question asked of the same picture.

**Documents it moves.** Build order seam 9; the research note gains a successor; `BoardDressing.asset`'s
role, since a skin is a dressing decision; a decision-log entry.

### 3.5 · The wrapper: menu, settings, lobby screen, end of run

**Destination.** Double-click the build and be in a game: a title screen, a way to join or start a lobby, a
settings screen, and a screen at the end of a run that shows the six bars.

**The MVP.** Four screens in UI Toolkit, in the one scene. The match root idles until a run starts, so
[ADR-0029](adr/0029-exactly-one-match-root.md) and `SceneRootTests` hold and every batchmode capture keeps
working. Settings persist to the player's data folder: display mode and resolution, master volume, mouse
sensitivity, the player's name, the lobby folder's path. The end-of-run screen is section 3.1's six bars and
the old outcome line. Escape opens a pause menu with resume, settings and quit.

**What is Sam's.** The words on the screens, and which settings are in the first version. Nothing else here is
a design decision.

**What verifies it.** Synthetic clicks through every screen to a run and back, in the built player. The
sit-down's *Before you start* section and its row 12, the clean-machine double-click, are this effort's
acceptance test.

**Documents it moves.** The sit-down; AGENTS.md rule 3's list if a script is added; ADR-0029 gains the
sentence that the root may be idle; the vision's [§8, out of scope](vision.md#8-out-of-scope) stays as it is,
since a menu is not a store page.

### 3.6 · The sweep harness specification

**Destination.** A written specification of what the harness is for, what it plays, what it reports, and how
a verdict reaches `content/`, replacing a tool whose shape was decided one ticket at a time.

**Why a spec rather than a ticket.** Sam's sentence is *it doesn't do what I need*, and what he needs is not on
file. What is on file is a precise account of what it cannot see: a capstone (the bot never sets one), a
kill's bounty (the field never kills), any wall better than one bot's rule, and the roster's unpriced levers
(range, radius, shield, windup and backswing, the transforming pair, the spawner). Each was accepted as blind
with an expiry, and the 13 September entry sends the bot's blindness to *the balance-sweep effort*. A spec is
the document that effort starts from, and the memory that the cost algorithm may become sweep-derived says
the stakes: this tool is meant to replace the pricing rule.

**The questions the spec has to answer, in the order they decide each other.**

1. **What is it for at this stage.** Three candidates, and they want different harnesses: price the roster
   (derive the cost column from play), find what dominates (which builds and waves nobody should be able to
   lose with), and score maps. The vision names all three; the playtest needs the second first.
2. **Who plays the runs.** The bot is the whole blind spot. Options: a better scripted bot (every rule change
   moves every row); a population of bots with different rules, so a report is against a spread; or the
   playtest's own recorded rounds as the field, which is what the pool is for and what the 12 September entry
   says expires the stand-in. The last is the one this proposal recommends, because it makes the harness and
   the lobby one machine.
3. **What a row is.** Today a creep against a wall type. A playtest wants a matchup table, every tower line
   against every creep, and a per-build row, this recorded board against this population.
4. **What a verdict looks like.** A column that says *mispriced by this much*, which the current file
   deliberately does not carry because the leak rate cancels price; or a dominance flag; or a map's score.
5. **How a verdict reaches content.** By hand today. The spec says whether the sweep proposes a cost column
   and a person signs it, and what regenerating the golden costs each time it does.

**What is Sam's.** All five, in a sitting, before anything is built. The spec is a `type:grilling` outcome,
written by an agent from the answers and reviewed as a document, which is what rule 6 says docs are for.

**Documents it moves.** A new `docs/specs/sweep-harness.md`, which is a new directory and wants a line in the
docs index and in AGENTS.md's *where things are written down*; build order seam 4; ADR-0041 and ADR-0058
gain amendments once the spec lands; open questions (the bot's value score, zero rows, what a spawner is
worth, the Mage's price) either close into the spec or stay with it named as their owner.

---

## 4. The documents, file by file

What each standing document says that the goal makes stale, and the suggested edit. Nothing here is applied.

| Document | Says today | Suggested update |
|---|---|---|
| [`vision.md`](vision.md) pillar 4 | The multiplayer is real, and all of it is deferred | The lobby is next; the round-robin, co-op and the social layer stay deferred |
| `vision.md` §2 | K = 10 in every mode, topped up from the pool | K is everyone present in a lobby, with no top-up; ten from the pool in the round-robin |
| `vision.md` §3 | Runs rank by waves survived then health; the offense never enters the placing | A run is scored per metric as a position on the lobby's curve; whether the positions combine is Sam's ruling, recorded in the same edit |
| `vision.md` §6 | Art is not a risk item; the effort goes into lighting, VFX and camera | Keep the second half, strike the first, and add the animation score as the approach |
| `vision.md` §7 | A real server, self-run, not built | Add: the lobby MVP runs on a shared folder, and the server is the relay that replaces it |
| [`build-order.md`](build-order.md) sequence | Steps 1 to 4 built, step 5 next, then step 7 is depth, the two-board interface and the service | Steps 1 to 6 built; step 7 is the playtest MVPs in section 5's order; the old step 7 becomes step 8 |
| `build-order.md` step 5 prose | *Step 5 is fifth deliberately*, and the shell-versus-client reasoning | Keep the finding, retire the argument to the decision log; it argued for a sequence that is complete |
| `build-order.md` seam 2 | After step 6, half-paid by step 2 | Its first half is the lobby folder and barrier, built now |
| `build-order.md` seam 4 | Owed columns are queries | Owed a specification first |
| `build-order.md` seam 7 | The 17 August arrangement, with the menu beside the hex | The wheel supersedes the menu's placement and keeps the rail; the watching surface is untouched |
| `build-order.md` seam 8 | Independent, whenever there is appetite | On the critical path; the animation score per tower |
| `build-order.md` seam 9 | Generation and rotation deferred behind the first authored map | Add smoothing as the seam's next piece, under the legibility veto |
| [`open-questions.md`](open-questions.md) | Is the field measurement kept | Answered: it is the score's consumer |
| `open-questions.md` | Whether the wave is always on screen | Reopened by the wheel; decided from a sheet |
| `open-questions.md` | What a thumbnail is | Deferred behind the wheel's text MVP, still load-bearing |
| `open-questions.md` | The sit-down's seven tick rows | Retire onto `LocomotionTests`; the playtest is the new instrument |
| `open-questions.md` | Rating at two scales; the gamble | The lobby is the friend-group scale and is built first; the gamble waits on a pool |
| `open-questions.md` | The bot's value score; zero rows; what a spawner is worth | Owned by the sweep spec |
| [`decision-log.md`](decision-log.md) | Ends on 13 September, windup and backswing | Entries for each reversal above, on the day Sam makes it: the placing, K for a lobby, the multiplayer's deferral, art as a risk, the 17 August arrangement's menu |
| [`roster.md`](roster.md) | A `Looks` line per row; sixteen windup and backswing numbers signed as holding answers | The `Looks` line carries the animation score once signed; the sixteen numbers are re-derived from it |
| [`sit-down.md`](sit-down.md) | Twelve things to look at, once, at ticks of the recorded match | Retired, or rewritten as the playtest protocol: what six people are asked to look at and say, and what is recorded where. Row 12 becomes the wrapper's acceptance test |
| [`chrome/README.md`](chrome/README.md) | The chosen arrangement is what the build phase is being built toward | Superseded on the menu, kept on the rail; a new chosen sheet once the wheel is picked from one |
| [`docs/README.md`](README.md) | The index | This page; `specs/`; the playtest protocol |
| [`AGENTS.md`](../AGENTS.md) | Rule 3's script list; *where things are written down* | New scripts as they land; `docs/specs/` |
| [`research/what-agents-can-build-unattended.md`](research/what-agents-can-build-unattended.md) | B1 is the 17 August chrome; the four rows worth doing | Stale on B1 and B7; the note's method stands, its table does not. Either amend or retire under the 5 September rule |
| [ADR-0035](adr/0035-a-runs-outcome-is-a-vector-and-health-is-a-clock.md) | Placing is waves then health; a single score is refused | Amend: the placing is per-metric position; the refusal of a single score is the reason |
| [ADR-0042](adr/0042-the-field-is-measured-off-the-pool.md) | Largely superseded; nothing reads the measurement | Amend: the score reads it, against the lobby |
| [ADR-0057](adr/0057-a-stored-round-is-a-wall-and-a-wave-at-a-stage.md) | A run draws K at its own stage from a folder | Amend: a lobby is the folder, and the barrier is the count at the stage |
| [ADR-0028](adr/0028-generated-placeholder-art-marks-the-seam.md) | Generated meshes mark the seam | The seam closes as each tower's score lands; say so in an amendment |
| [ADR-0008](adr/0008-match-events-are-decorative.md), [ADR-0019](adr/0019-the-view-has-no-clock.md) | Events are decorative; the view has no clock | Both hold; add the sentence on how a particle effect is simulated to a tick |
| New ADRs | | A lobby is a shared folder; an animation is a score sampled by tick; a score is a position on a curve |
| New documents | | `docs/specs/sweep-harness.md`; `docs/playtest.md`, the protocol the sit-down becomes |

---

## 5. The order, and what Sam decides first

**Ordered by what makes a playtest exist, not by size.** Each row names what it unblocks and the decision it
waits on. Rows 3 to 6 run in parallel once 1 and 2 are on a branch, and the sweep spec is a sitting rather
than client work, so it runs beside everything.

| # | Effort | Why here | The one decision it waits on |
|---|---|---|---|
| 1 | The wrapper and the lobby folder (3.5, 3.1's transport) | Without these six people cannot be in the same game at all | The lobby is a shared folder, or something else |
| 2 | Scoring (3.1) | The end of a round has to say something to six people | Which metrics, and whether they combine |
| 3 | The wheel (3.2) | It is the surface the playtest is judged on, and the chrome was never directed | What the wheel holds, and where the wave lives |
| 4 | Animation and projectiles, first two lines (3.3) | The largest effort, and the one Sam has already ruled looks wrong; two lines settle the approach and the other seven copy it | Which two lines, and whether the release frame sets the number |
| 5 | Board smoothing (3.4) | Visible in every frame, but a cliff does not stop a playtest | Which candidate, from a sheet |
| 6 | The sweep spec (3.6) | Not on the playtest's path; on the path of everything the playtest's numbers are read against | The sitting |

**What an MVP means on each, stated so nothing is quietly widened.** A lobby is a folder, not a relay. A
score is six bars, not a rating. A wheel has words on it, not portraits. Two tower lines are animated, not
nine. One smoothing candidate ships, chosen from two. The sweep gets a document, not a rewrite.

**Every one of these stops at a human under the standing rules.** No agent picks a clip, a shot's model, a
wheel's contents, a metric's name or a smoothing candidate; each ticket renders the alternatives and stops,
which is the roster sign-off's method and it worked. The decisions in the right-hand column are the ones to
take first, in one sitting, so that six efforts can start from tickets that already carry their answers.

## 6. What this page does not propose

It does not edit the vision, the build order or an ADR; it says what the edits would be. It does not name
a creep, a tower, a metric or a screen's words. It does not choose the lobby's sync tool, the animation
approach's variant or the smoothing mesh; it puts the candidates beside their costs. And it does not write
the tickets: an effort label per row of section 5, a `map` issue each, and prototype and grilling tickets
under them are the next step once the sitting in section 5 has happened.
