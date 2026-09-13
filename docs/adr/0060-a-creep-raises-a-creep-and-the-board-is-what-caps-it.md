# 0060 — A creep raises a creep, and the board is what caps it

**Decided.** A walking body may put another body on the corridor beside itself on a clock. `content/units.txt`
layout 5 adds `raises` (the row) and `raisePeriod` (ticks between raises). A new layout rather than an
extension of 4, by the file's own rule: two column counts under one layout number is a misread nothing here
could detect.

- **Refused where the columns are read:** a row that raises itself; a row that raises an unauthored, standing
  or poolless row; a raised row that raises in its turn; a row that *becomes* a row that raises; a raise with
  no cadence or a cadence with no raise; a standing row that raises. So there is no second generation, and one
  wave order names at most one raised row — which is what prices the leak.
- **Where it enters:** `Match.Spawn` is the one place a creep starts existing, whoever put it there. A raised
  body takes the next id and sits at the end of the array, so it **loses every targeting tie** — a tower
  looking at a Necromancer and its Minion shoots the Necromancer. It arrives at the raiser's own distance in
  the next lane offset, on a full pool, on the tick's close immediately before the wave's own release, and
  walks and is shot at from the tick after. Only a walking body raises, so killing the spawner stops the
  raises the tick it dies.
- **The first raise is a whole period after arrival**, where an aura pulses on the tick its emitter spawns: a
  pulse costs nothing to have arrived, and a spawner that shows up already accompanied is a design statement
  nobody signed. *Every 150 ticks* means 150 ticks after it gets there.
- **No die is rolled**; the stream stays a running count of shots, asserted by `RaiseTests`.
- **A raised leak charges health at the raised row's price** (`LeakedRaisedByOrder`, `Run.LeakCost`); the
  sender paid nothing for it, and that gap is held open, uncapped
  ([ADR-0063](0063-a-price-is-derived-from-the-row-and-its-silences-are-named.md)).
- **The match still ends by arithmetic:** the bound is the raiser's own latest tick, plus one floored crossing
  of the raised row, plus its longest death — one term, not a graph walk, because nothing raised raises. How
  many it raises is deliberately unbounded: about 11 against the committed defense, 15 under the Overgrowth,
  on the order of 170 at the floor speed. Stacking slows costs something, and this is where.
- `CreepRaised` is decorative; `MatchDecorations` draws nothing for it. The view needs nothing else — a
  raised body is an entity in the snapshot from the tick it arrives.

**Cost.** `unit-types/5`, `SimulationVersion` 12 (the tick loop grew a phase), `match-state/4` → `/5` folding
each creep's raise clock per tick and whether it was raised once at the spawn; every stored record retired.
The committed match is untouched by the mechanic and its hash moved anyway.

**Rejected.** Inserting a raised body beside its raiser (renumbering, or an array not in id order, which
`ReportPasses`, `Acquire` and `Fold` all read as rules). Raising on the arrival tick. Running the raise after
the wave's release (the first body of a column would wait a whole period and every body behind it one tick
less). A cap or a decay on the raise. A guessed coefficient for a spawner.

Lives in `sim/UnitTypeTable.LinkRaises`, `Match.Raise`, `Match.RequireWhatItRaisesArrives`, `Run.LeakCost`,
`MatchEvents.CreepRaised`.
