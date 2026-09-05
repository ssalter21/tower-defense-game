# 0058 — A sweep row is a creep against one attack type, and the wall is an axis of the report

`content/sweep.csv` scored each creep against one wall — whatever the defending bot happened to build — and
folded the result into one row per creep. It now scores **every creep against a wall of every attack type the
roster has a tower for**, one row per pair, with the wall named in a column of its own. On the committed
content that is five creeps by three walls and fifteen rows.

## Why one wall cannot price a roster

`content/ruleset.txt` authors the damage matrix so that **every row and every column is a permutation of the
same three cells** — the parser refuses a matrix that is not one, and that property is what makes no attack
type globally better and no armour type globally tougher.

The consequence for a report is not a nuance. A wall of one attack type is a hard counter to one armour class
and barely an inconvenience to another: pierce takes 140% off a swift body, magic takes 140% off an armoured
one, and `docs/roster.md` records that 140 against 70 is worth exactly double the shots to kill. So a roster
swept against a single wall reports a landslide and a zero, and **which creep gets which is a fact about the
defending bot rather than about any creep**.

That is not hypothetical. [#236](https://github.com/ssalter21/tower-defense-game/issues/236) changed the bot
from buying by price to buying by value, it began buying archers where it had bought mages, and the Skeleton
Scout's row went from the highest in the file to zero — while facing a *cheaper* wall than before. Two of five
rows reported nothing, and the file went on parsing and went on looking like a balance finding. The
measurements are in
[a sweep row measures the wall's attack type](../research/a-sweep-row-measures-the-walls-attack-type.md).

## What was rejected

**Pinning the wall — a recorded defense replayed identically for every row.** This was the shape
[#242](https://github.com/ssalter21/tower-defense-game/issues/242) proposed, on the reading that the rows had
lost a control when the bot stopped saturating. Measured, the opponents' wall was never per-row in the first
place — `RunContent.Sweep` builds one `FieldPool` and hands the same object to every row — and pinning the
*run's own* wall as hard as the harness allows (`--policy all-in`, every row standing an empty board and taking
an identical 44,000) leaves the Scout at zero. Pinning buys reproducibility, which is real. It does not buy
comparability, because the ranking is still decided by whichever mix the pinned record happened to hold.

**One pinned wall, deliberately mixed.** Cheaper, and every creep gets a non-zero number. Rejected because the
mix is an arbitrary weighting that silently decides the ranking, and because a mixed wall can say a creep
scored badly but never why. The zero is what carries the information; averaging it away loses the finding.

**Leaving it and calling it a balance finding.** The honest reading of the file was that the value-buying bot
converges on a single counter, which is true. Rejected because the alternative — make the bot buy a mix —
trades one uninformative report for another: measured, a wall of six soldiers scores 2,801 / 2,877 / 2,888 /
2,856 / 2,728 across the whole roster. A wall too weak to stop anything saturates as surely as one that stops
everything.

## What the wall is made of

**The restriction is on what the opponent buys, applied where the roster is ordered by price** —
`CoverThenUpgradeBot.ByPrice`. It therefore binds both halves of that rule: the cover loop places out of the
filtered list and the upgrade loop climbs the ladder out of it too. A filter on the placing half alone would
build a pierce wall and then upgrade it into a mixed one, and that failure would show up only in a report's
last rounds.

**A restricted wall opens on nothing.** `content/defense.txt` is four archers and two mages, so a wall asked
for pierce that opened behind it would carry an armoured creep's counter in its seed — and measured, the
Minion reported zero against both pierce *and* impact until the seed came out. Opening every restricted wall
empty makes the three columns equal by construction: same purse, same rounds, one difference. The authored
defense keeps its meaning for the unrestricted wall and for every other verb, where it is a recorded round's
opening layout and not a control.

**`any` is the way back to one wall, and it is a name rather than a blank.** A report played against whatever
the bot buys unrestricted is a legitimate question — it is the wall a run actually meets — but it is not what
this file is committed as, and a column that is empty on some reports and filled on others is one every reader
has to learn twice. It cannot be asked for beside a restricted wall, because "no restriction" is not a fourth
attack type and a file listing it next to pierce would carry two rows a reader would compare where one is the
other's superset.

## What this does not fix

**The three walls are not equally strong, because the three tower prices are not equal.** A mage costs 92
against a soldier's 30, so an equal purse buys a much sparser magic wall, and every creep scores better against
it than the matrix alone would predict — the armoured Minion does best of all against magic, which is
nominally its counter. Comparing *across* columns therefore mixes matchup with density. Comparing *down* a
column does not, and that is the comparison the report exists for. The wall column is what makes the
difference visible rather than silent.

**The report still describes a game and never skilled play.** Both walls are built by one deliberately simple
rule, which is what the file's own `defense_gold` note has said since #163.
