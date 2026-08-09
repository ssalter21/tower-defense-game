# 0043 — A tier is its own id and its own row

The Captain is not the Soldier with a flag set. A tier-2 tower is a new row in `content/units.txt` carrying a
new id out of the one global space, and an upgrade is a purchase that swaps one row for another rather than a
mutation of the row you already own.

## What was decided

**Every tier is a whole row, and the ladder joins rows rather than modifying them.** The alternative on the
table — a `tier` field, or an upgraded flag, or a variant marker — makes the Captain a state the Soldier's row
can be in. That is a tech tree wearing a tier ladder's name: nothing is consumed, so what an upgrade *costs*
becomes vacuous, the Soldier stands next to the Captain it supposedly became, and the price a tier carries is
its build price rather than an upgrade price. `docs/roster.md` rules it out by name and this is why.

**Ids come from one global id space, ascend forever, are never reused, and are never an index into anything.**
`content/units.txt` states it in its own header: towers and creeps are the same kind of thing there, so an id
is unique across every unit that has ever existed and a record that pins type 2 today still means that row in
five years. The gaps at 5, 6, 8, 9 and 10 are five retired rows and they stay empty permanently. **Ids are
taken when a unit is built and never reserved** — an earlier draft of the roster held 11–20 for the tower
tiers and 21–25 for the creeps, and abandoned it, because nine of those numbers were being held for units that
may never exist and a file whose gaps only make sense if you have read a document is drift with a schedule.

**An upgrade consumes a built tower and the placement keeps its identity through the swap.** The upgraded
tower is the *same placement* whose type changed, not a new placement pointing back at a dead one. Per-tower
statistics therefore key on **(placement id, unit type id)**: a Soldier-turned-Captain has two stat rows and
one id, the per-tier breakdown *is* those rows, and the career total is a group-by. There is no `was` pointer,
so there is no reference that can go stale and no chain to walk when summing. This was the reversal of a
recommendation that nothing but the cell survives, and it turns out to be the genre's shipped behaviour:
Bloons TD 6 mutates the existing tower, keeps its `ObjectId`, never resets `damageDealt` or `cashEarned`, and
accumulates `Tower.worth`. The engines that destroy and recreate instead each lose per-instance state or need
a hand-written transfer hook per statistic.

**A placement id is derived, not stored, and it is not a fourth ADR-0009 identity field.** It is the ordinal
of the placement that made it — the *N*th placement of the run's defensive command stream, or file order for
an authored defense — which is numerically what `Match` already produces. Ids ascend from 1, zero means no
placement, a sold placement's id is retired and never reused, and no record carries one. ADR-0009's three
fields identify *stored* records; this identifies a thing that is never stored.

**There are two ids on a tower, not one, and that is deliberate.** Today's tower id keeps its job as the
snapshot's join key even though it is exactly what `units.txt` bans for unit ids — `Match.cs:269` assigns
`_towers[index].Id = index + 1`, so inserting a tower at a lower cell shifts every id above it. It stays
because `Match.cs:285` runs tower, creep and projectile ids off **one counter**, and widening it would shift
every creep id in every golden trace for a change the simulation is not supposed to notice.

## What it costs

**Every tier restates a whole eighteen-column row even where it differs from its predecessor in two numbers.**
A Captain that is a Soldier with more health and a higher price is authored as a full line, and the two lines
can drift apart in a column nobody meant to change. That is the price of a type id meaning one fixed set of
numbers forever, which is what makes a stored record readable years later, and
[0044](0044-a-new-unit-is-a-row-never-a-column.md) is why the alternative is not cheaper than it looks.

**A new row moves the content hash and retires the records pinned to the old one.** Rows are cheap against
columns, not free against nothing. What a tier costs is a regeneration of the current goldens; what a column
would cost is a format.

**The ladder inherits the ascending-id rule as a hard constraint on the roster's future.** `content/upgrades.txt`
requires its target id to exceed its source id, which makes a cycle unstateable in one comparison — and makes
it impossible to ever insert a rung *below* an existing unit. A future tower meant to precede the Archer has a
higher id than the Archer, so its edge is illegal, and the routes out are to retire the Archer's row and
re-author it at a higher id, or to move the ladder to layout 2 with the rule relaxed and cycle detection
built.

## What was rejected

**A tier number stored anywhere.** The roster's tiers are `1`, `2`, `2a`, `2b` and `3`, and two of those are
not integers in files that refuse a non-integer before tokenising. A tier is also derivable from the graph,
and storing a derivable thing is Warcraft III's cautionary tale — two representations of one graph kept
consistent by hand with nothing checking them. Its absence is load-bearing rather than incidental: three of
the four faults anybody would want a ladder checker to catch are unstateable *because* no content file holds a
tier, and [0045](0045-the-ladder-is-a-graph-not-a-list.md) is where that lands.

**A permanent unlock, so that owning a Soldier makes Captains buildable on empty hexes.** `Unlocks` already
implements "free to unlock and paid to buy" as a fold over build phases, and the resemblance was a resemblance
rather than shared machinery: it is the *offensive* gate, one take per round, free and permanent. Inheriting
"free, permanent, one per round" is not wanted on a ladder that is supposed to cost gold, and there is one way
to a Captain and it eats a Soldier.

**A `was` pointer on the upgraded placement.** It is a reference that can go stale and a chain somebody has to
walk to sum a career. Identity gives the same answer with nothing to keep consistent, and of every game in
[the survey](../research/upgrade-graph-representation-in-shipped-tower-defenses.md) only GemCraft stores a
link back to what a tower used to be — and a gem *is* its two parents, which is a different mechanic wearing
the same word.
