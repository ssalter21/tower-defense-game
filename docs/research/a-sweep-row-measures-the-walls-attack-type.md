# A sweep row measures the wall's attack type

**The question.** Two of `content/sweep.csv`'s five rows report zero after #236, and #242 reads that as a lost
control: the wall the rows were compared against stopped being fixed, so pin one and the rows compare again.
Is that the mechanism?

**The answer is no, and the fix #242 proposes does not revive either row.** What separates the rows is not how
big the opponent's wall is but **which attack type it is made of**, matched against the creep's armour class.
The zeros are the damage matrix working exactly as authored, against a defending bot that converges on a
single tower type. Measured below.

## Two things #242 assumes that the code does not do

**The field's wall was never per-row.** `RunContent.Sweep` builds one `FieldPool` and hands the same object to
every row of the sweep — `FieldPool.Canned` is a pure function of the content, and the stand-in's purse is
paid a flat base and its own interest, never a bonus, so nothing about the run it is fought by reaches it. The
opponents' wall is already identical across all five rows and always was. #236 changed *what* that one wall
buys, not whether it varies.

**The column that moved is the run's own.** `defense_gold` is `build.Defense` summed over the run's own build
phases — what the row's player spent on its own board. It read 6,576 on every row because the old bot
saturated, and it ranges 5,872 – 8,720 now because the new one keeps buying out of a purse that tracks the
row's own bonus.

**Pinning that wall restores the control and changes neither zero.** `--policy all-in` is already the extreme
case: the player builds nothing, so every row stands the same empty board and takes an identical 44,000 in
leak cost. It is as fixed a wall as this harness can produce.

| creep | dealt, even-share | dealt, all-in (wall pinned empty) |
|---|---|---|
| minion | 2,073 | 7,404 |
| skeleton-scout | **0** | **0** |
| necromancer | 31,337 | 114,347 |
| skeleton | 25,810 | 94,032 |
| skeleton-warrior | 22,769 | 101,430 |

`taken_gold` is 44,000 on every all-in row — a real fixed control, and the scout still reports zero.

## It is not wall size either

The scout reports zero at every run length, including the shortest one the harness will play, where the
opponent stands six towers and one round of income:

| waves | opponent's wall | minion | skeleton-scout | necromancer |
|---|---|---|---|---|
| 2 | 384 gold | 459 | **0** | 1,676 |
| 4 | ~504 gold | 1,823 | **0** | 8,802 |
| 6 | ~624 gold | 2,275 | **0** | 25,244 |
| 10 | 872 gold | 7,404 | **0** | 114,347 |

A creep that leaks nothing against the smallest wall in the game is not being outgrown.

## What it is: pierce against swift

`content/ruleset.txt` authors the matrix as `attack × (swift, armoured, arcane)`:

| attack | swift | armoured | arcane |
|---|---|---|---|
| pierce (archer, ranger) | **140** | 70 | 100 |
| impact (soldier) | 70 | 100 | **140** |
| magic (mage) | 100 | **140** | 70 |

`docs/roster.md` records that 140 against 70 is worth exactly double the shots to kill. The Scout is `swift`
with 1,500 HP and no armour; #236's bot buys archers where the old one bought mages; pierce takes 140% of a
hit off a swift body. The old report's 52,687 for the Scout was the old bot's mages, at 100%.

Six towers of one type each, at `--waves 2` so the bot adds almost nothing on top:

| opening wall | minion (armoured) | skeleton-scout (swift) | necromancer (arcane) | skeleton (armoured) | skeleton-warrior (armoured) |
|---|---|---|---|---|---|
| six archers — pierce | 1,863 | **0** | 1,860 | 2,176 | 2,216 |
| six mages — magic | **42** | 908 | 1,922 | 583 | 314 |
| six soldiers — impact | 2,801 | 2,877 | 2,888 | 2,856 | 2,728 |

**The creep that reports zero changes with the wall.** Against archers it is the Scout; against mages it is
the Minion, whose armoured body takes 140% of a magic hit. Neither creep is weak — each is hard-countered by
one of the two towers the bot actually buys.

At the committed depth, under the committed player, the same three walls:

| opening wall | minion | skeleton-scout | necromancer | skeleton | skeleton-warrior |
|---|---|---|---|---|---|
| six archers | 4,514 | **0** | 32,703 | 32,627 | 30,631 |
| six mages | **71** | 110 | 34,406 | 15,995 | 18,012 |
| six soldiers | 22,893 | 4,276 | 41,157 | 45,162 | 40,190 |

Against a soldier wall the Minion reads 22,893 and every row separates. The two "dead" creeps are alive in
the one column where nothing counters them.

## What this means for the report

A sweep row is currently *this creep against whatever mix the defending bot happened to buy*, which is a fact
about the bot. Under a matrix built so that no attack type is globally better, one wall of one type cannot
price a roster: it will always read as a landslide and a zero, and which creep gets which depends on a tower
choice made elsewhere.

Pinning a recorded wall fixes the reproducibility — the rows stop moving when the bot changes — and leaves
the ranking decided by whichever mix that record happened to hold. The question the report exists to ask is
answered by the attack type, so that is what the report has to carry.

The soldier row is the other warning: 2,801 / 2,877 / 2,888 / 2,856 / 2,728 at `--waves 2` separates nothing
either. A wall too weak to stop anything saturates as surely as one that stops everything.

## How to reproduce

Editor closed, from a shell. `tools/_shared.ps1` joined `-ContentFile`'s option and value into one token until
this was written, so the examples in `run-sweep.ps1`'s own header refused; that is fixed.

```powershell
./tools/run-sweep.ps1 -Policy all-in -Out artefacts/all-in.csv
./tools/run-sweep.ps1 -ContentFile @{ defense = 'artefacts/wall-mage.txt' } -Out artefacts/mage.csv
```

A single-type wall is `content/defense.txt`'s six cells with one type id on every row — 3 archer, 4 mage,
11 soldier. An empty defense is refused, so the weakest wall available is six soldiers.
