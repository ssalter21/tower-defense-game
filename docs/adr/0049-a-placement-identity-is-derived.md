# 0049 — A placement's identity is derived from the stream, and an action names a cell

A placement's id is **the ordinal of the *N*th `place` action in the run's command stream, counted from one**.
It is computed, never stored, and no row of any format carries it. An action names the cell it acts on.

## What was decided

**The id is derived and there is no field for it.** A `place` row carries a wave, a type and a cell; a
`upgrade` row carries the same four fields. Neither carries an id, because the id *is* the row's position
among the `place` rows above it. A stored id would be a second spelling of something the stream already says,
and the interesting records would be the ones where the two disagreed —
[ADR-0017](0017-canonical-order-is-asserted-not-restored.md)'s objection, arriving from the identity side
rather than the ordering side.

**The counter counts `place` rows only.** An upgrade keeps the placement's id and swaps its type, which is
what *an upgrade is a swapped placement* means. Minting a fresh id on every rung would kill a placement's name
each time it climbed one, and a placement is *an identity, a current type and a cell* — the identity is the
part that is supposed to survive.

**An action names its target by the hex, never by the id.** It is what a person types and what a future mouse
click produces; it needs no counter to be understood, and it refuses precisely — *nothing stands on 4, 6*. A
row naming an id would be unreadable in isolation: you would have to have counted every prior row in the file
to know what `upgrade 3` points at. The id's one structural advantage — surviving a placement rebuilt
underneath it — is dormant, because selling is out of scope and nothing is ever rebuilt.

**Ids retire on removal and are never reused.** No mechanism removes one today. The rule is stated now so
that the day one arrives, the answer is not decided by whichever loop happens to renumber first.

**The id is a run-local name, and it is printed.** `play-run`'s ending position carries it as a column, so the
board at the end reads back against the actions above it.

## What it costs

**An id means nothing outside its run, and per-tower statistics across runs need a wider one.** Two runs both
have a placement 1. Anything keying career statistics on `(placement, unit type)` across a library of runs
needs an identity this one is not, and building that is new work rather than a widening of this.

**A stream cut in half renumbers.** Every id depends on the whole prefix of `place` rows above it, so a tool
that takes the last four rounds of a run and calls them a stream produces different ids for the same
placements. That is honest — those *are* different runs — but it means an id may never be quoted between two
streams.

**Reading a row does not tell you what it does.** `upgrade 7 5 3` is legal against a board with something on
column 5, row 3 and refused against one without. The row is not self-contained, and the refusal has to come
from applying it. Every alternative that made the row self-describing made it carry state the stream already
holds.

## What was rejected

**A `u16 placement_id` field on the action.** Explicit, self-describing, and a second spelling of the row's
own position. It also has to be *assigned* by somebody, which puts the counter in the writer as well as the
reader.

**Minting a new id on upgrade.** It makes the id a name for *a tower of a type* rather than for a placement,
and there is already a word for that: the type id.

**Inferring the action's kind from the board** — empty cell means place, occupied means upgrade. Tempting, and
it turns a mistyped hex into the other action at a different price with nothing refusing. `CommandStream`'s
standing rule is that every failure is a refusal and never a skip, so the kind is always stated.

**Naming the placement by an author-chosen label.** It survives everything, and it puts a string in the record
format and a naming decision in front of every placement a person makes.
