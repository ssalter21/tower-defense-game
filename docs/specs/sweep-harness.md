# The sweep harness specification

**Status: the questions, unanswered.** A blank here is not an omission, it is the ask, as on
[the roster](../roster.md#how-to-edit-this). This document is filled in from a sitting with Sam, in the order
the sections run, and it is reviewed as a document before a column of the harness moves. The ruling that made
it, on 13 September 2026: the harness does not do what it is needed for, and it wants a whole spec.

## What the harness is today

A `simcli` mode and a comma-separated file, verified against a committed report. It plays every creep against
a wall of every attack type the roster has a tower for, over a population of seeds, under a scripted player,
and folds the result to a row per creep and wall, with a row per run on request. It takes every content file
and every shape parameter as an argument. The rules of its shape are
[ADR-0041](../adr/0041-the-sweep-computes-rows-and-the-shell-writes-them.md) and
[ADR-0058](../adr/0058-a-sweep-row-is-a-creep-against-one-attack-type.md); the columns are declared once in
`simcli/SweepColumns.cs`; the shell end is `tools/run-sweep.ps1`.

## What it cannot see, with the day each blindness was accepted

| Blindness | Why | Accepted | Expiry, as recorded |
|---|---|---|---|
| A capstone | The bot covers the whole route before it upgrades anything, and dies before it sets one | 12 September 2026 | Expired on 13 September: what the bot does with a capstone is this specification's to set |
| A kill's bounty | The field stand-in sends one row and never kills, so no run is paid | 12 September 2026 | The day runs are stored and the field is a population of real rounds |
| Any wall better than the bot's | One deliberately simple rule builds every wall, and a bot rule change moves every row | Standing, since the report's own `defense_gold` note | This specification |
| The unpriced levers | Range, radius, shield, duration, windup and backswing, the transforming pair, the spawner | Each on its own day; see [the roster](../roster.md#what-things-cost) | A sweep-derived cost rule, which this specification is the first step toward |
| A zero row | A creep the wall stops outright ranks against nothing | Left standing | [Open question](../open-questions.md#is-a-sweep-row-worth-reading-when-the-wall-stops-its-creep-outright), owned here |

## The five questions, in the order they decide each other

### 1 · What is it for at this stage

Three candidates, and they want different harnesses: **price the roster** (derive the cost column from play),
**find what dominates** (which builds and waves nobody should be able to lose with), and **score maps**. The
vision names all three. The playtest needs the second first, because a dominant build found by six friends in
an evening is a wasted evening.

_

### 2 · Who plays the runs

The bot is the whole blind spot. Options: a better scripted bot, where every rule change moves every row; a
population of bots with different rules, so a report is against a spread rather than one player; or the
playtest's own recorded rounds as the field, which is what the pool is for and what the 12 September expiry
names. The last makes the harness and the lobby one machine, and it is the recommendation.

_

### 3 · What a row is

Today a creep against a wall type. A playtest wants a matchup table, every tower line against every creep,
and a per-build row, this recorded board against this population. Whether both are one report or two, and
what the fold is over, is decided here.

_

### 4 · What a verdict looks like

A column that says *mispriced by this much*, which the current file deliberately does not carry because a
leak rate cancels price; or a dominance flag; or a map's score. A verdict is the thing a person reads without
opening the file, and the harness has never had one.

_

### 5 · How a verdict reaches content

By hand today. Whether the sweep proposes a cost column and a person signs it, whether a proposed number is a
row of the report or a file beside it, and what regenerating the golden costs each time it does. The cost
algorithm may become sweep-derived; this is the question that says how.

_

## What this specification does not decide

The bot's own rule for a capstone, the bounty's price, and the diminishing-returns question are balance
decisions this document names as inputs and does not take. Nothing here moves a number in `content/`.
