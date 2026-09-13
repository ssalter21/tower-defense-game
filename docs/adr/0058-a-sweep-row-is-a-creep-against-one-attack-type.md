# 0058 — A sweep row is a creep against one attack type, and the wall is an axis of the report

**Decided.** `content/sweep.csv` scores **every creep against a wall of every attack type the roster has a
tower for**, one row per pair, the wall named in a column of its own — fifteen rows on five creeps by three
walls, where it was one row per creep against whatever the bot built.

- The damage matrix is a permutation in every row and column, so a wall of one attack type is a hard counter
  to one armour class and barely an inconvenience to another. Swept against one wall, a roster reports a
  landslide and a zero, and **which creep gets which is a fact about the defending bot** — #236 changed the
  bot from buying by price to buying by value and the Skeleton Scout's row went from the highest in the file
  to zero against a *cheaper* wall. Measured in
  [a sweep row measures the wall's attack type](../research/a-sweep-row-measures-the-walls-attack-type.md).
- The restriction is on what the opponent buys, applied in `CoverThenUpgradeBot.ByPrice` so both the cover
  loop and the upgrade loop obey it; a filter on placing alone would upgrade a pierce wall into a mixed one.
- A restricted wall opens **empty**: `content/defense.txt` is four archers and two mages, and a seeded wall
  carries a counter the column did not ask for. Same purse, same rounds, one difference.
- `any` is the way back to one wall and is a name, not a blank; it may not stand beside a restricted wall in
  one report, because "no restriction" is not a fourth attack type.

**Cost.** The three walls are not equally strong, because the three tower prices are not — a mage costs 92
against a soldier's 30 — so comparing *across* columns mixes matchup with density. Comparing *down* a column
does not, and that is the comparison the report exists for. The report still describes one deliberately
simple bot, never skilled play.

**Rejected.** Pinning a recorded wall and replaying it for every row (#242's proposal): reproducible, and the
ranking is still whichever mix the pinned record held — measured with `--policy all-in`, the Scout stays at
zero. One deliberately mixed wall: an arbitrary weighting that decides the ranking silently, and a mixed wall
can say a creep scored badly but never why; the zero is the information. Calling it a balance finding and
making the bot buy a mix: a wall of six soldiers scores 2,801–2,888 across the whole roster, and a wall too
weak to stop anything saturates as surely as one that stops everything.
