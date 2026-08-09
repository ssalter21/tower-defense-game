# 0044 — A new unit is a row, never a column

`content/units.txt` grows down, not across. A row is a line; a column is a new layout, a new hash label, a
reader branch that never goes away and every stored record made under the old layout readable only through it.

## What was decided

**The layout row is what makes the distinction a mechanism rather than a slogan.** The table declares
`layout 2` before its first unit row, there is a reader branch per layout, each layout folds under its own
hash label, and a row whose field count does not match the layout it declared is refused rather than read
against shifted fields. So adding a column is not an edit — it is a version, with a branch behind it forever.
Layout 1's fifteen columns still load, still fold under `unit-types/1`, and still keep the hash they always
had, which is why the bundle pinned to one still replays.

**A row costs a hash and a column costs a format, and the difference is not a matter of degree.** Both move
the content hash and retire the records pinned to the old one; a row's whole bill is a regeneration of the
current goldens. A column adds to that a layout the reader must branch on for the rest of the project, and
under ADR-0009 a branch may only be retired when it is not the only evidence for itself — so the older
layout's golden becomes a file that is kept forever because this repository can no longer produce it. That is
the cost the slogan is short for.

**It is why the upgrade edge got its own file rather than a nineteenth column.** Seven of the roster's ten
proposed towers wait on this one lever, and `units.txt`'s own header lists all six the file does not have: a
target count, a radial shot, a timed slow, an aura, a second health pool and the upgrade edge. Five of the six
are per-unit scalars and would each be a column somebody could argue for. The edge is not one of them at any
width, because it is a **relation between two rows**, and no row is wide enough to hold a relation without
either a ragged list or a scalar that forbids the diamond. A file of rows is what a relation is.

**And not `content/ruleset.txt` either, despite `docs/roster.md` proposing it.** That file's header states
that *every row is required and none may appear twice* — a rule it does not state is refused by name rather
than defaulted. It is a file of required, fixed-arity rules. An edge set is **optional and variable-length**:
zero edges on the day it lands, eight when the roster's three lines are authored. Repetition was never the
obstacle — `matrix` and `band` already repeat — optionality was. The roster's instinct to keep the edge out of
`units.txt` was right and its destination was wrong.

## What it costs

**Six proposed towers stay unauthorable on purpose, and the file says so.** `units.txt`'s header names the
levers it does not have and points at `docs/roster.md` rather than growing a column that would make one of
those units expressible and the other five still not. The alternative is a table that accretes a column per
design idea and a roster nobody can read a row of.

**Every tier is a wide line, restated.** Eighteen columns per row means a tier-2 tower that differs from its
predecessor in three numbers is authored as a full line, and two lines can drift in a column nobody meant to
touch. [0043](0043-a-tier-is-its-own-id-and-its-own-row.md) is where that price is paid and why.

**Column layout 1 is permanent.** `A_table_with_no_layout_row_is_layout_one_and_keeps_the_hash_it_always_had`
pins it, and `content/golden/defense-0.units` is the only evidence its branch reads. That file cannot be
regenerated, so it is kept rather than produced — which is the shape of every cost a column adds, made
concrete once.

## What was rejected

**A nullable column meaning "not applicable to this row".** A blank is a default, and a default is a number
nobody authored folded into a content hash as though somebody had — the posture `ruleset.txt` states in its
own header and the reason `--upgrades` is required rather than optional on every verb that takes `--units`.

**A second table keyed by unit id, one per lever.** It reads as cheaper than a column and is worse: two files
that must agree about which ids exist, with nothing checking them, which is exactly Warcraft III's `ureq`
against `uupt` in [the survey](../research/upgrade-graph-representation-in-shipped-tower-defenses.md). The
edge file is not this — it holds a relation nothing else can hold, and both of its ids are refused at load if
they name no row in `units.txt`.

**Widening the row to carry a variable-length predecessor list.** One line per unit, ragged. It saves lines in
a file of eight edges and buys a new parser concept that `units.txt` refuses by name, and it is the shape
Element TD ships with a hand-maintained `Count` beside it that must agree with its own entries — the exact
redundancy that rots under hand-editing.
