# A wall kills a count, not a share, so an accumulating opponent outruns it

**Research note** · 15 August 2026 · measured while making the opponent's field scale per round for
[#208](https://github.com/ssalter21/tower-defense-game/issues/208)

**Question:** the stand-in in the field now buys [`content/field.txt`](../../content/field.txt)'s column once
more every round, so round seven sends seven times what round one sends. What does that cost a run, and can a
run build its way out of it?

**Inputs:** the committed map, defense, roster and ruleset; `Run`, `Match` and `Sweep` over the committed
simulation assembly; `content/commands.txt` played through `simcli play-run --no-death`.

---

## Bottom line

**A defense kills a roughly fixed NUMBER of creeps per match, not a fixed share of what walks in.** The
committed six-tower wall stops twelve bodies whether it is sent twenty or a hundred, so every creep added to a
column is a creep that gets through. Leak is therefore very nearly the whole of the increase, and an opponent
who accumulates outruns any wall a run can afford.

**A ten-round run against an accumulating stand-in takes 5,011 gold of damage against a health pool of 800.**
It dies in the fourth round. No build order fixes this: four archers cost 160 gold and buy about seven kills a
round, so out-killing round ten's hundred bodies would take roughly fifty towers and two thousand gold against
an income of 168 a round.

## What a wall stops

The committed defense — four archers and two mages — against one column of minions, released one body per
forty-five ticks, resolved to the end with no tick cap:

| sent | leaked | killed | ticks |
| ---: | -----: | -----: | ----: |
|   10 |      0 |     10 |  1913 |
|   20 |      7 |     13 |  2498 |
|   40 |     28 |     12 |  3398 |
|   70 |     58 |     12 |  4748 |
|  100 |     88 |     12 |  6098 |

The kill column is flat and the leak column is the whole of the growth. A longer match does not buy more kills,
which is the part that is not obvious: the towers keep firing for all 6,098 ticks, but a deeper column means
each arriving creep takes a smaller share of the same damage and fewer of them cross the threshold that kills
them. **Damage is spread, so bodies survive wounded.** A wave's depth is therefore strictly stronger than its
width under this tuning, which is the opposite of what
[`a-purse-in-one-column-beats-the-same-purse-in-four.md`](a-purse-in-one-column-beats-the-same-purse-in-four.md)
measured in the regime where a wall could still saturate.

## What that costs a run

`content/commands.txt` as it was authored for a flat stand-in — one archer a round for five rounds, then waves
— played against the accumulating one with death switched off:

| round | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| taken, flat stand-in | 100 | 90 | 65 | 30 | 33 | 32 | 32 | 32 | 31 | 33 |
| taken, accumulating | 100 | 190 | 265 | 330 | 438 | 537 | 638 | 737 | 838 | 938 |

The flat row falls as the wall goes up and totals 478 of 800 health — a run that ends holding 322 health and
1,647 gold, which is the artefact #208 was filed about. The accumulating row climbs by very nearly one authored
column of gold a round and totals **5,011**, which is six times the health pool. The first three rounds spend
555 of it, so the wall is still going up when the run is already most of the way dead.

## What it does to the sweep

The committed sweep runs with `--no-death`, so it still gets ten rounds out of every row. What moves:

| column | flat stand-in | accumulating |
| --- | ---: | ---: |
| win rate, every creep | 10000 bp | 0 bp |
| taken, every creep | 1,634 | 33,512 |
| dealt, minion → necromancer | 37,249 – 50,469 | 37,249 – 50,469 |

**The win-rate column is gone and the offense columns are untouched.** Every row loses every run, and taken is
identical to the gold on all five rows because the incoming waves leak in full and the dice never touch them.
What still separates the creeps is what they got past the stand-in's own wall — dealt, bonus and the
cost-efficiency column — because the attacking direction still meets six towers that kill some of what they are
sent and kill it by rolling.

## What it costs to sweep

The committed sweep, 40 runs of 10 rounds against a field of 10, on this machine, including its build:

| stand-in | wall clock |
| --- | ---: |
| flat, one column a round | 42 s |
| accumulating | 78 s |

The match count does not change — a played run has not touched `Run.Field` since #209, so the sweep is 9,600
matches either way. What doubles is how long a match takes: a round-ten column is ten times as deep and its
match runs 6,098 ticks against 1,913. **1.85× measured**, which is well inside what a sweep is run for and is
not a reason to choose a resolution.

## What this does not say

It does not say the accumulation is wrong. It says the health pool (800), the tower prices (40) and the leak
price (one for one on what a creep cost) were tuned against an opponent who never grew, and that the three of
them cannot survive one who does. Which of the three moves is a balance decision and is not taken here; the
run in `content/commands.txt` is four rounds long because that is the run this tuning supports, not because
four is a number anybody chose.
