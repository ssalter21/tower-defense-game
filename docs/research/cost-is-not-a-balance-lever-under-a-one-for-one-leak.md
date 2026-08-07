# Cost is not a balance lever while a leak is charged one for one

**Research note** · 7 August 2026 · measured while authoring the ten-row roster in
[`content/units.txt`](../../content/units.txt)

**Question:** the roster's brief says costs are authored against cost-per-effect rather than against stat lines
read in isolation. Against the committed corridor and the committed defense, what does moving a creep's cost
actually change?

**Inputs:** the committed map, defense and ruleset; `Match` over the committed simulation assembly; a column of
one creep type at a fixed sauce budget, swept across health, speed and armour type.

---

## Bottom line

**Under a one-for-one leak charge, the cost column cancels out of cost-efficiency entirely.** A wave order of
`count` creeps at `cost` sauce each costs `count * cost` to send and deals `leaked * cost` health, so the health
a sauce buys is `leaked / count` — the **leak rate**, and nothing else. Halving a creep's price doubles how many
of it a purse buys and halves what each leak charges, and the two cancel exactly.

Two consequences, and both are load-bearing for the sweep:

- **A creep is balanced by its survivability and by nothing else.** What the cost column controls is
  *granularity* — how many bodies a purse turns into, and therefore how a wave interacts with the slot width and
  the release cadence — plus how much health one concession costs. It does not control whether a creep is worth
  taking.
- **A row whose effective health is under the per-creep damage budget is a dead option at any price.** Against
  the committed defense that budget is a little over a thousand: `SpawnIntervalTicks` is 15 and the six towers
  deal roughly 62 damage a tick before typing, so a creep released into a single-order column meets about 930
  raw damage while it crosses, and about 1100 once the pierce-heavy line's cells are applied. An earlier draft of
  the roster carried a 500-health swarm creep at 3 sauce; it returned **zero** health per sauce at every budget
  from 150 to 600, and no price would have changed that.

## What was measured

A column of one creep type, 400 sauce of it, released into the committed defense on the committed map. Health
dealt is `leaked * cost`; the fraction below is that over the sauce spent.

| creep | health | armour | speed | cost | leaked | health per sauce |
|---|---|---|---|---|---|---|
| grunt | 1550 | Armoured +0 | 85 | 10 | 32 of 40 | 0.80 |
| runner | 1500 | Swift +0 | 170 | 9 | 32 of 44 | 0.73 |
| wisp | 1100 | Arcane +0 | 255 | 7 | 43 of 57 | 0.75 |
| bulwark | 5000 | Armoured +45 | 55 | 45 | 7 of 8 | 0.88 |
| drifter | 2400 | Arcane +25 | 100 | 19 | 17 of 21 | 0.81 |
| lancer | 3400 | Swift +0 | 85 | 21 | 15 of 19 | 0.79 |

And the surface the roster was fitted to, priced at effective health over 160 throughout, so that price is held
constant and only survivability moves:

- **Below about 900 effective health, nothing leaks at any speed.** 500 and 800 both return zero across every
  speed from 55 to 425 for Swift and Arcane bodies.
- **Speed helps, and it helps most near the threshold.** A 1100-health Swift body returns 6% of its sauce at
  speed 55 and 73% at 425. Well above the threshold the curve flattens: a 9000-health body returns 86% to 100%
  across the same range.
- **Armour type is worth about as much as a doubling of health against this defense.** Four pierce bolts and two
  impact mortars is a line that deals 79% to an Armoured body, 112% to an Arcane one and 119% to a Swift one, so
  an 800-health Armoured body leaks where an 800-health Swift body does not.

## What follows for the sweep

The cost-efficiency column [#95](https://github.com/ssalter21/tower-defense-game/issues/95) is asked for will
report leak rate under another name unless something changes the exchange. Three candidates, none of them this
ticket's to decide:

- Charge a leak something other than the creep's price — a flat amount, or a curve.
- Let the same sauce buy something other than creeps, so the purse has an alternative use to be measured against.
  The snapshot line item is the only one today and it costs 25.
- Leave it, and read the cost column as what it is: the knob that decides how many bodies a slot turns into.

The third is the current position, and it is why every walking row is priced at effective health over 160: with
efficiency flat by construction, an even price is what stops the cost column quietly becoming a granularity
lottery.
