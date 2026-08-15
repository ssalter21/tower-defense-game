# 0045 — The ladder is a graph, not a list

> **Amended by [#179](https://github.com/ssalter21/tower-defense-game/issues/179), 13 August 2026.**
> Everything below stands. What changed is who reads the file: this ADR was written while the ladder was an
> annotation the command line held, and the simulation now reads it. `BuildPhase.Resolve` refuses `place` for
> any unit some edge points at, so an edge decides what a decision may do — and a stored record carries a
> ladder hash that retires it if the file moves underneath it. The shape, the arity and the ordering rules are
> untouched.


`content/upgrades.txt` is an edge set: one `upgrade <from> <to>` row per edge, and two rows where two roads
reach one tower. The Mage's tier 3 has two predecessors, so neither a "next tier" field nor a "previous tier"
field can hold the shape, and the file that holds it is a graph written down one edge at a time.

## What was decided

**The diamond is the reason, and it costs one extra line.** The Mage splits into the Pyromancer and the
Cryomancer and both roads end at the Frostfire Archmage, so the target is named by two rows. A single parent
pointer on the child forbids that by construction; a single successor field on the source forbids the split.
[The survey](../research/upgrade-graph-representation-in-shipped-tower-defenses.md) is blunt about which shape
to rule out by name — Legion TD 2's singular `upgradesFrom` string, which is rare rather than absent, and is
the one thing this format must not become.

**Storage shape predicts whether a game can reconverge at all**, which is why this is a decision rather than a
detail. Element TD and Kingdom Rush use the identical out-edge mechanism and only Element TD ever names one
target twice; Infinitode 2 ships 593 nodes and 665 links with 73 two-parent nodes; Bloons TD 6's crosspath
lattice stores a bare `(upgrade, tower)` pair with no path index at all. Shapes that make reconvergence native
appear only in games that reconverge. Nobody picks the expressive shape and declines to use it, so picking it
is the whole of the design decision.

**A row says succession and nothing else.** Source first, keyword and two ids, fixed arity, integers only, a
`layout` row, rows ascending strictly by (`from`, `to`) — asserted rather than sorted, exactly as `units.txt`
asserts its ids ascend, which is what makes a duplicate a comparison against the row above. There is no tier
column: the roster's tiers are `1`, `2`, `2a`, `2b` and `3`, two of which are not integers. There is no
direction marker: column position says it and a marker could disagree. There is no cost: the price of an
upgrade is the full price of the target's `units.txt` row, which the survey calls both the majority position
and the only one that cannot be mispriced by route — Bloons TD 6 prices the step and ships one mispriced edge
out of 3,293 as a direct result.

**Cycles are unstateable rather than detected.** The target id must strictly exceed the source id. Ids ascend
forever and a tier is always authored after the thing it follows, so a cycle cannot be written down — one
comparison, no traversal, no visited set, and a refusal that is about the row in front of you. It has a
second effect worth more than the check: **the file is a topological order of the ladder by construction**, so
any reader walking it top to bottom sees every source before any of its targets without sorting anything.

**The simulation is never handed one.** The ladder is parsed in `sim/` beside every other parser, held by the
command line, and never passed to `Run`. That turns "the simulation does not enforce the ladder" from a
promise somebody has to keep every time they touch that file into a property: `Run` cannot enforce it because
it cannot ask for it. It is the same move as banning `System.IO` from the simulation assembly and scanning the
compiled image for it — a constraint worth stating is a constraint worth making unstateable.

**What a checker may say about this graph splits into a fault and a note, and the split is not policy.** A
**fault** is a shape no legitimate roster ever has, at any point in its life: **mixed roles**, where the two
ends of an edge have different `UnitRole`s, which is where a one-digit id typo lands; and **unequal roads**,
where two paths join the same pair of ids at different lengths, which is what the diamond asserts about itself
and what a skipped rung breaks. Faults are fatal in the build gate. A **note** is a design statement, printed
and never judged: a **root** is a unit with no incoming edge, a **leaf** is a unit with no outgoing edge, and a
flat or falling price is a target that costs no more than its source. The walk returns both and refuses
neither, and its two callers take the two postures — a test over the committed files that goes red, and the
`ladder` verb, which prints and always exits zero.

**The reason the split needs no policy is that a tier number does not exist.** *"A tower above tier 1 with no
incoming edge"*, *"a skipped rung"* and *"an orphan target"* are each a sentence about a fact no content file
holds, so a live tier-1 tower and an unreachable tier-2 tower are the same row with the same absence beside
it. Every check a roster mid-edit could legitimately trip is one of those. What survives has no mid-edit state
in which it is correct, so a fault can be fatal without a suppression mechanism, and the ambiguous case is
*reported* — as a root, with no verdict attached — rather than checked.

## What it costs

**A rung can never be inserted below an existing unit.** `to > from` buys the cycle check and sells the
ability to author a tower that precedes the Archer, since its id would necessarily be higher. The routes out
are to retire the Archer's row and re-author it at a higher id, or to move to a second ladder layout with the
rule relaxed and real cycle detection built. This is a constraint on the roster's future, taken deliberately.

**A new tier edits the middle of the file rather than appending to it.** A row is keyed on its *parent's* id,
so it lands beside the parent. Target-first would have been append-only, because ids ascend forever, and
target-first was the recommendation; it was overridden so that a row reads as the act it describes — *the
soldier becomes the captain*. The cost is softened by the edge living in its own file rather than as a field
on the unit, so a new tier's arrival is split across two files and not across two places in one row.

**The two faults do not catch the failure people actually fear.** A roster whose Marksman was never given an
incoming edge passes everything. That is not a gap a cleverer checker closes; it is closed by a tier number
existing, and nothing in this design creates one. A skipped rung with no parallel road is undetectable for the
same reason — only the diamond makes a shortcut visible, and only because the long road is there to compare
against.

## What was rejected

**A parent pointer on the unit row.** The shape that forbids a diamond, and a column besides —
[0044](0044-a-new-unit-is-a-row-never-a-column.md).

**A variable-length predecessor or successor list, one row per unit.** Ragged arity, in a repository whose
unit table refuses a row whose field count does not match the layout it declared, bought to save lines in a
file of eight.

**A second pass in the loader applying these design rules to every ladder it is handed.** The strongest loser.
It would make a design statement an *unloadable file*, so a half-authored roster could not be run at all, and
it would apply this roster's rules to test fixtures and to a hand-typed file behind `--upgrades`. The
structural refusals live in the loader precisely because they are true of *any* ladder; these are true only of
this one, which is why their enforcer is a test over the committed files.

**A lint script in `tools/` and a second `check-ladder` verb.** The gate's three scripts each check something
`dotnet test` cannot — files on disk, YAML, no assembly — and this is a C# walk over data a `sim/` parser
produces. A second verb beside `ladder` would be a second reader of one file, free to disagree with the first.

**Binding the checker to `docs/roster.md`.** The roster states the ladder in section headings and one sentence
of prose, every proposed tower's id is `—`, and it is the design the file is derived *from* rather than a
transcription of it. The precedent for doc-binding is `SitDownTests`, which exists because a checklist
transcribes numbers a run produces; authority runs the other way here.
