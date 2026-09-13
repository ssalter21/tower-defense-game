# The tuning target, and what each row returns against it

**Research note** · readings taken 11 September 2026 on the committed board, defense and roster in
[`content/`](../../content/); the band itself is [the roster's](../roster.md#the-tuning-target)

**Question:** the roster targets a quarter to a half of the wave leaking. Where does the committed match sit,
where does each creep row sit against its own return band, and which of the misses has a lever?

**Inputs:** `sim.tests/MatchTests.cs` for the committed match; `content/sweep.csv` and the landscape sweep for
the per-row returns — four hundred gold of one row against the committed defense, with the transformation and
the raise in.

---

## Bottom line

**Eight of forty leak, and the miss is a person's choice.** The band is ten to twenty, the committed match
leaks eight, and `MatchTests` asserts both — the reading as *missed*, both ends of the band as two exact lists
— rather than widening the band away, so the day somebody retunes, the tests go red and say which band to put
back. A defense that holds tells you nothing when it changes and one that collapses tells you nothing either;
a partial break makes the leak count a number a person can watch. The miss ends on one of two triggers: **a
Unity playtest saying the leak feels wrong, or the derived cost landing.**

**Six creep rows are outside their own band, and one of the six is out by an order of magnitude.** Eleven of
the seventeen return 60 to 95 percent of their gold; five are under and one is over.

| row | returns | | row | returns |
|---|---|---|---|---|
| Minion | **37** | | Vampire | 94 |
| Skeleton Scout | 84 | | Witch | 88 |
| Skeleton Mage | 95 | | Fiend | 92 |
| Skeleton | 78 | | Shade | **36** |
| Skeleton Warrior | **58** | | Cursed Villager | 94 |
| Necromancer | **1200** | | Werewolf | 90 |
| Bone Golem | **50** | | Grave Robber | 87 |
| Black Knight | 71 | | | |
| Frost Wight | 85 | | | |
| Abomination | **40** | | | |

**The Skeleton Mage sits on the band's upper edge, at exactly 95.** In, by the test's `> 95`; one more leak in
twenty-one and it joins the Necromancer's list. Written down so the day it does, nobody reads it as new.

## Why each miss is where it is

**Under the band, for two opposite reasons.** A splash is worth most against a dense column, and a column of
one cheap row is the densest thing that can be sent — so what the Mage's splash costs most is the fine end of
the granularity axis: the Minion at forty bodies and the Shade at fifty. The Bone Golem, the Abomination and
the Warrior are under it from the coarse end: the slowest bodies stand in front of the wall longest and are
shot at for longer.

**The Cursed Villager is in the band because it transforms.** Thirty-six of them is still the densest column
the splash can be pointed at, but each body is the Werewolf's 2860 effective health at the Villager's 11 gold.
The Werewolf's own reading matches, because nothing sends a Werewolf; the two read 94 and 90.

**Over the band, one row, and it is the Necromancer — three unpriced things at once.** Nineteen of them walk
together and each pulses a pool worth a quarter of a body's health over two hexes, so the column is handed raw
shield faster than four archers and two mages take it off, and every one of the nineteen leaks. Each also
raises a Minion every 150 ticks for as long as it walks, so **209 bodies nobody sent leak behind them**. The
cost rule reads health and armour and sees neither the pool, the reach that spreads it, nor the raise. The
Vampire at 94 is the same gap without the aura — a raw 1400 in front of 3360 — and the Grave Robber's 2000
sits behind an armoured body slow enough to be shot for it.

**A slowed Necromancer raises more.** One raises **11** before it leaks against the committed defense; with the
committed Overgrowth on the board it raises **15**; at the speed floor it crosses in about 25,000 ticks and
raises on the order of a hundred and seventy. Shield Wall and Overgrowth are slows, so the two capstones built
to handle a push are the two that make this body worst — stacking slows is supposed to cost something, and
this is where it costs.

**No row deals zero.** The floor is the Shade at 36. Two rows never win a round of the sweep, for opposite
reasons: the Minion, at 21 dealt per hundred gold, deals too little; the Necromancer, at **1399** — three times
the next figure — wins nothing because the sweep plays a row against *itself*, both sides get eleven free
Minions a body, and a run takes **7670** against a health pool of 800. An uncapped spawner is symmetric: the
row is not strong, it is a mirror nobody survives. The Cursed Villager reads **398 dealt per hundred gold and
eight of eight**, level with the Skeleton Mage, above the Werewolf's 389 and behind only the Vampire. Against
the smaller four-wave field the test fixture plays, the Abomination is the one row that deals nothing at all.

## What the instruments cannot see

**The sweep cannot see a bounty.** It varies the row a run **sends** and a bounty is paid to whoever **kills**,
so it would have to reach a run through the opponents it defends against — and every opponent in the committed
sweep is the stand-in out of `content/field.txt`, which sends Minions. The return band is a **leak rate** and
cannot see the half of a body that did not leak either, so the Grave Robber's reading is 81 before and after
the bounty, to the digit. The leak exchange rate itself did not move: no body is both charged and paid for.
[ADR-0061](../adr/0061-a-kill-pays-the-defender.md) records what a reading of it would need.

## What follows

**Nothing here is retuned, and that is a ruling rather than a deferral.** There is no price lever for the five
under: a creep's cost is derived, so making the Minion cheaper means making it weaker, which reopens a signed
row. Retiring the band for the sweep's weaker "no row deals zero" would give up the one test that says a row is
free money. The Necromancer keeps its derived 21, and its 1200 here and 1399 in the sweep are the acceptance
test for the derived cost rule: the day a creep price can see a pool, a reach and a raise, both readings come
inside their bands or the rule is wrong. The rule and its silences are
[ADR-0063](../adr/0063-a-price-is-derived-from-the-row-and-its-silences-are-named.md).

**Measure before you retune.** The leak count has moved without any creep row moving — an attack type changing
line, and the clock dilating while `wave.txt`'s order ticks did not — and the release cadence inside a column
is a simulation constant rather than a content number, so not every move can be answered from content at all.
