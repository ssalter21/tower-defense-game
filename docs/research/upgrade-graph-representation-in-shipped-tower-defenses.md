# How shipped games store an upgrade graph, and what a reconverging path costs

**Research note** · 8 August 2026 · commissioned by
[#109](https://github.com/ssalter21/tower-defense-game/issues/109), a ticket of
[#107 — the upgrade edge](https://github.com/ssalter21/tower-defense-game/issues/107)

**Question:** how do shipped tower defense games — and the data-driven games either side of them — represent an
upgrade graph **in data**, and what does a **branching-and-reconverging** path cost the format?

**Why it was asked.** #107's decision 6 commits to a new `content/upgrades.txt`, and nobody had looked at how
anyone else writes one. The thing that is not obvious is the **diamond**: [`docs/roster.md`](../roster.md) splits
the Mage at tier 2 into Pyromancer and Cryomancer and lands both roads at one tier-3 Frostfire Archmage, so that
row has **two predecessors**. A single "next tier" field cannot say that, and neither can a single "previous
tier" field.

**How this note was made.** Two research passes ran independently against the same ticket and were merged, which
is why the source list is unusually long and why several games carry two corroborating citations. Where the two
disagreed the disagreement is resolved in the text rather than averaged — the largest was §5, where one pass
concluded that no shipped game preserves provenance across an upgrade because it had **not surveyed BTD6**. It
does, and §5 is written from the pass that read it.

**Constraints this is read against**, from `content/units.txt` and #107: integers only and no decimal point; a new
unit is a **row** and never a column; the file is hand-edited and must read well in a diff; the simulation will
**not** enforce the graph. Where a source violates one of these it is called out rather than smoothed over.

**A second question was added mid-flight**, from the parallel grilling on
[#108](https://github.com/ssalter21/tower-defense-game/issues/108): when a shipped game upgrades a tower **in
place**, does anything of the previous tower survive in the data — a placement identity, accumulated per-tower
statistics, a link to what it came from? [§5](#5-what-survives-an-in-place-upgrade) answers it, and the answer is
friendlier than the question feared.

---

## Bottom line

### One. **Nobody ships a standalone edge table as the primary form.** Across every format read here, an upgrade graph is stored one of three ways: a **prerequisite list on the target row** (Factorio, 0 A.D., OpenRA, Warcraft III, StarCraft II, Dota 2 recipes), a **forward out-edge list on the source row** (Bloons TD 6, Element TD, Kingdom Rush, Warcraft III again), or **lexical nesting** (Mindustry). Exactly one — Infinitode 2 — keeps a separate array of `(parent, child)` pairs, and even it hangs the price on the node.

### Two. **A diamond is free in the prerequisite form and merely awkward in the out-edge form.** With prerequisites, the row for a node with eight predecessors looks exactly like the row for a node with one — the list is longer and nothing else changes (Factorio's `rocket-silo` names eight). With out-edges, a reconverging node is named by *two different source rows*, and the fact that they are the same destination is only visible by reading both. **Neither form breaks. The single-successor and single-predecessor fields are the only shapes that actually cannot express it** — and exactly one surveyed format has that shape: Legion TD 2's `UpgradesFrom`, a singular string, which is a *predecessor* rather than a successor. It is rare, not absent, and it is the one shape to rule out by name.

### Three. **Cost lives on the destination in eight of the twelve formats that price anything, three put it on the step, and one derives it — and both *branching* step formats paid for the choice.** BTD6 puts cost on the step and is the most rigorous implementation found — and **one of its 3,293 shipped edges is mispriced by $100 as a direct consequence**. StarCraft II puts cost on the step and pays with three-hop indirection and machine-minted identifiers. **This repo's decision 7 — an upgrade costs the full price of the target row — is the majority position and the only one that cannot be mispriced by route.**

### Four. **"A tier reachable two ways" is not a thing anyone stores. It is a property the format either makes free, or makes into a chore.** Cost-on-destination makes "both roads cost the same" true by construction and literally unstateable otherwise. Cost-on-step makes it an invariant somebody has to check: BTD6's holds for 3,292 of 3,293 edges, and the one failure is a real, live, in-game discount nobody noticed.

### Five. **Provenance survives an in-place upgrade in the one shipped tower defense that displays per-tower statistics.** BTD6 mutates the existing `Tower`, keeps its `ObjectId`, never resets `damageDealt` or `cashEarned`, and accumulates every upgrade's price into `Tower.worth`. **The decision that provenance must survive an upgrade is the shipped behaviour of the genre's deepest game**, not a novel requirement. Every engine that instead destroys and recreates (OpenRA, Factorio, Age of Empires II) either loses per-instance state or needs a hand-written transfer hook per statistic — and OpenRA needed a bugfix PR because two of them were silently dropped.

### Six. **Reconvergence is rare in the genre, and the games that do it are the ones whose storage shape made it cheap.** Kingdom Rush branches at tier 3 and *never* reconverges — using the identical out-edge mechanism Element TD uses to reconverge constantly. Dungeon Defenders, Defense Grid, Sanctum 2 and Orcs Must Die are linear ladders that keep the whole ladder on the tower's own row, which is what a non-branching design lets you get away with. **Storage shape predicts capability**, and this repo is choosing the shape before the roster needs it, which is the right order.

---

## 1. The three shapes, and what each one can say

Every format read reduces to one of these. The distinction that matters is **which row holds the relationship**,
because that is the row a person edits when they add a tier.

| Shape | Who holds the relationship | Diamond costs | Shipped in |
|---|---|---|---|
| **Prerequisites on the target** | the **new** row names what came before | *nothing* — the list gets longer | Factorio `prerequisites` [[1]](#s1); 0 A.D. `requirements` [[2]](#s2); OpenRA `Prerequisites:` [[3]](#s3); WC3 `ureq` [[4]](#s4); SC2 `CRequirement` [[5]](#s5); Dota 2 `ItemRequirements` [[6]](#s6) |
| **Out-edges on the source** | the **old** row names what it becomes | a second source row names the same target; nothing links them | BTD6 `TowerModel.upgrades` [[7]](#s7); Element TD `Upgrades` [[8]](#s8); Kingdom Rush `tw_upgrade` [[9]](#s9); WC3 `uupt` [[4]](#s4) |
| **A standalone pair list** | a third file names both ends | nothing — a second pair with the same `child` | Infinitode 2 `res/researches.json` links [[10]](#s10) |
| *(degenerate)* **Nesting** | indentation is the edge | **breaks** — needs a second, differently-shaped field | Mindustry `TechTree.node(…)` [[11]](#s11) |

**Warcraft III — the genre's ancestor — stores it both ways at once**, and that is the cautionary tale. Blizzard's
own `UnitMetaData.slk` defines `ureq` (`Requires`, a `techList`) on the target *and* `uupt` (`Upgrade`, a
`unitList` with `maxVal 12`) on the source [[4]](#s4). Two representations of one graph that must be kept
consistent by hand, with nothing checking them. Note that even WC3's "Upgrades To" is a **list of up to twelve**,
not a single successor. **The single-field shape #109 worried about is rare but real** — it appears once in this
survey, as Legion TD 2's `UpgradesFrom` [[15]](#s15), and on the *predecessor* side rather than the successor
side. That is the shape `content/upgrades.txt` must not adopt, and it is worth naming rather than assuming
nobody would.

**Nesting is the one shape that genuinely cannot do a diamond.** Mindustry's tech tree is Java, and a node's
parent is captured off a static mutable `context` during a nested lambda, so `parent` is a single reference
[[11]](#s11). Reconvergence had to be bolted on as a *second* field of a *different type* — `Seq<Objective>
objectives`, where extra predecessors are `new Research(x)` objects. The result is that expressing the second
predecessor looks nothing like expressing the first:

```java
node(ruinousShores, Seq.with(
    new SectorComplete(crateredBattleground),
    new Research(graphitePress),
    new Research(kiln),
    new Research(mechanicalPump)
), () -> {
```

> **The lesson for `content/upgrades.txt`.** If the row shape can say "this row follows that one" *once*, it can
> say it twice by being written twice — and the diamond costs one extra line. If instead the relationship is a
> field on the unit row, the diamond costs a format version. **Repeated optional rows are the cheap shape**, which
> is exactly why #107 rejected `ruleset.txt` (required, fixed arity) and a `units.txt` column.

---

## 2. Bloons TD 6: the lattice, and what it actually costs to store one

BTD6 is the obvious first stop and it repays the visit, because the crosspath system is a genuine three-dimensional
lattice rather than three lists, and the whole thing is legible from exported game data.

**Method and trust.** No public Il2Cpp `dump.cs` for BTD6 exists; modders open `Assembly-CSharp.dll` locally rather
than publishing dumps. The findings below come from **exported `GameModel` JSON** [[12]](#s12) plus **mod source
compiled against the interop assembly** [[7]](#s7)[[13]](#s13), which is second-best but is still the data and the
real signatures. Counts below were taken by census over the export, not sampled.

### 2.1 Every crosspath combination is a materialised row

**64 `TowerModel`s exist per standard tower**, one per reachable tier triple, exhaustively enumerated — **2,167
models in the export overall** [[12]](#s12). Nothing is generated at runtime. The tier triple is **dual-encoded**:
as a name suffix (`DartMonkey-203`) and as `TowerModel.tiers`, an `Il2CppStructArray<int>` of length three
[[7]](#s7). Across all 2,167 models the two encodings agree with **zero mismatches** — a redundancy that is
evidently maintained by tooling rather than by hand.

> ⚠️ **This is the "a new unit is a row and never a column" constraint taken to its logical end, and it is
> expensive.** BTD6's answer to "what is a 2-0-3 Dart Monkey" is *a whole tower row*, with every stat restated.
> That is fine when a generator writes the file and nobody diffs it. It is the wrong end of the trade for a
> hand-edited table — but it does confirm that **the destination of an upgrade is a unit row**, which is what
> #107 decision 3 already assumes.

### 2.2 The edges point forward, and nothing names a predecessor

`TowerModel.upgrades` is an `Il2CppReferenceArray<UpgradePathModel>`, and `UpgradePathModel` is literally a pair
of strings — constructor order `(upgrade, tower)` [[7]](#s7):

```csharp
new UpgradePathModel(upgrade /* UpgradeModel id */, tower /* target TowerModel name */)
```

So an edge object *does* exist in BTD6 — but it lives **on the source row**, not in a table of its own, and
**nothing in the data names a predecessor.** To learn what reaches `DartMonkey-220` you scan every tower's
`upgrades` array. The engine's own convenience collections make the asymmetry visible: `GameModel` carries
`upgrades`, `upgradesByName`, `bloons` and `bloonsByName` — and **no `towersByName`** [[7]](#s7). Mod Helper
maintains its own `Dictionary<string, TowerModel> TowerCache` to compensate.

There is a backward record, but it is on the **model**, not the instance and not the edge: `TowerModel.appliedUpgrades`
is an `Il2CppStringArray` naming **every** upgrade applied to reach that tier — the whole ordered ladder, not the
immediate predecessor [[7]](#s7). Mod Helper's upgrade patch works by *diffing* the old and new
`appliedUpgrades` to work out which upgrade just happened.

> **Provenance-as-a-list beats provenance-as-a-pointer.** No engine surveyed stores `upgradedFrom`. BTD6 stores
> the full ladder because "which upgrades does this tower have" is the query everyone actually runs. That is
> directly relevant to #108's requirement that per-tower stats sum across a ladder.

### 2.3 Cost is on the step — and that is where the one shipped bug is

`UpgradeModel.cost` is an `int` and carries the price of the step. `TowerModel.cost` is the **base purchase
price** and is invariant across the ladder — it is not the destination's total [[7]](#s7)[[12]](#s12). So BTD6 is
the format that answers "what does this step cost?" from the edge.

**The invariant this creates was checked over the whole export: for 3,292 of 3,293 edges,
`total(target) − total(source) == cost(edge)`** [[12]](#s12). Pricing is route-independent — but by *arithmetic
discipline*, not by construction.

**The one failure is real.** The edge from `Skywarden-014` to `Skywarden-024` names the upgrade `StormsPulse`
(a tier-0 upgrade, 175) where it should name `ThunderingArc` (tier 1, 275) — a **live $100 discount available
only on one of the two routes to that crosspath** [[12]](#s12). A second defect, a duplicated `BoomerangMonkey`
edge, was found in the same census.

> ⚠️ **Confidence.** That the *edge* is mislabeled is read directly from the exported data. That the player is
> actually charged the wrong amount is **well-supported inference**: the cost lookup reads the edge's named
> `UpgradeModel`, which is how `PathsPlusPlus` sets an arbitrary custom price — by temporarily swapping the
> tower's `upgrades` array before calling `GetTowerUpgradeCost` [[13]](#s13). Nobody has publicly decompiled
> `GetTowerUpgradeCost`, so this is not confirmed.

**This is the single most useful artefact in the note.** A shipped, enormously played, heavily modded game with
cost on the edge has a route-dependent mispricing in its data, and it survived to a version-54 export. Under
cost-on-destination the defect is not merely absent — it is **inexpressible**, because there is no place to write
a second price for the same destination.

### 2.4 The crosspath restriction is data in the simulation and code in the interface

The rule is *"at most one path past tier 2, at most two paths past tier 0"*, stated in mod source as
[[13]](#s13):

```csharp
public static bool DefaultValidTiers(int[] tiers) =>
    tiers.Count(i => i > 2) <= 1 && tiers.Count(i => i > 0) <= 2;
```

**Where does it live?** In the simulation it is *data* — the invalid combinations simply have no `TowerModel` and
no edge reaches them. In the interface it is *code*: `UpgradeObject`, `TowerSelectionMenu` and friends grey the
buttons out.

The decisive evidence is negative and strong: **`UltimateCrosspathing`, the mod whose entire purpose is removing
the restriction, patches no simulation method at all** — its patches touch only `UpgradeObject`,
`PowerProUpgradeObject`, `TowerSelectionMenu`, `Bank` and `Attack`, and it works by *generating the missing models
and edges* [[13]](#s13). If the simulation validated crosspaths, generating models would not be enough.

> **This is exactly #107's decision 5, shipped.** BTD6's simulation does not enforce the ladder; it merely has no
> data for what the ladder forbids. A graph that is an annotation rather than a load-time constraint is the
> normal arrangement, not a corner cut.

There *is* a simulation-side surface — `TowerManager.CanUpgradeTower(Tower, int pathIndex, int tier, int inputId,
ref float cost)`, `TowerManager.GetTowerUpgradeCost(Tower, int path, int tier)`,
`TowerManager.IsTowerPathTierLocked(Tower, int path, int tier)`, `Tower.GetUpgrade(int path)` [[13]](#s13) — but
`IsTowerPathTierLocked` appears to concern progression locks (XP, Monkey Knowledge) rather than crosspaths, and
that reading is *inference*, not confirmed.

⚠️ **One constraint violation worth recording:** `CanUpgradeTower(…, ref float cost)` passes cost as a **float**
at the simulation boundary even though `UpgradeModel.cost` is a hard `int`. Difficulty multipliers land in that
gap. The authored data is integral; the runtime price is not.

### 2.5 Integers, and the Paragon

**A complete census of all 790 upgrade JSON files found zero decimal points in `cost` or `xpCost`**
[[12]](#s12). BTD6's authored upgrade prices are integers, full stop. This repo's integers-only rule is not
unusual.

**The Paragon is a data edge**, held in its own scalar field `paragonUpgrade` rather than in the `upgrades`
array — a fourth path that is structurally not one of the three [[12]](#s12). The *sacrifice* mechanic that
determines the Paragon's power is bespoke code. That split is instructive: **the edge is data even when what the
edge does is entirely special-cased.**

---

## 3. Which shipped games actually reconverge

Four confirmed, nine ruled out. The ratio is itself a finding.

### 3.1 Element TD — the cleanest reconvergence in the genre

Element TD's dual- and triple-element towers are the diamond, several hundred times over, and the Dota 2 port is
open source so the data is readable — [MNoya/Element-TD](https://github.com/MNoya/Element-TD),
`game/dota_addons/element_td/scripts/npc/npc_units_custom.txt` [[8]](#s8). `blacksmith_tower` appears in the
`Upgrades` out-edge list of **both** `fire_tower` and `earth_tower`; `enchantment_tower` has three dual-element
predecessors.

**Three fields sit near the edge and only one of them is the edge — worth not conflating**, because the mistake
would import a concept this repo decided against in #108:

- `Upgrades` on the **source** is the ordered out-edge list. Reachability lives here, and reconvergence is just
  naming one target from two rows.
- `Requirements` on the **target** is **not** a predecessor check. It is a player-global *element level* gate —
  `mechanics/upgrades.lua`'s `MeetsItemElementRequirements` tests `playerData.elements[e]`. It decides whether
  the button is **live**, never whether the edge exists.
- `Element` on the target is the composition list — semantic identity, separate from both.

The step price is read off the destination at the moment of purchase, `mechanics/upgrades.lua` line 7:
`local cost = GetUnitKeyValue(newClass, "Cost")`.

The shape is **out-edges on the source**, exactly BTD6's and Kingdom Rush's — but Element TD is the one that
exploits it. And critically for #109's pricing question, **`Cost` and `TotalCost` live on the destination row**,
with pricing uniform by tier:

- all **6** single-element towers cost 175
- all **15** duals — and 15 is exactly C(6,2), so the lattice is complete — cost 425/600
- all **20** triples — C(6,3) — cost 900/1500

**Uniform tier pricing is what makes the denormalized `TotalCost` field truthful.** Because every road into a
dual tower passes through two singles at 175, "what have I spent" is the same number regardless of route, so a
stored total cannot disagree with a computed one. `mechanics/sell.lua` is why the field has to exist at all —
selling needs a total, and once the graph reconverges, a total is no longer derivable from the graph alone:

```lua
local goldCost = GetUnitKeyValue(tower.class, "TotalCost")
local refundAmount = round(goldCost * sellPercentage)
```

**And the interface gave up on drawing it as a tree.** `panorama/scripts/tower_table.js` renders an *element
matrix* rather than a graph — hovering an element glows every dual and triple whose composition contains it.
When a lattice gets wide enough, set-membership beats a tree diagram, which is worth remembering before anyone
tries to draw the Mage diamond.

> **This is the finding that most directly answers "how does anyone handle a tier reachable two ways for
> pricing".** The shipped answer is: *make the two ways cost the same*, then a single number on the destination
> is correct for both. Element TD did not solve route-dependent pricing — it **designed it out**, and this
> repo's decision 7 does the same thing by a different route (the target's full price, source's gold sunk).

**Element TD 2 reconverges too, and its format is not public.** The successor keeps the shape — Karawasa
describes the progression as `single > dual > triple > quad`, with a triple such as Nova reachable from
"three duals using elements from that triple" ([Steam](https://steamcommunity.com/app/1018830/discussions/0/3881598799638342855/)).
⚠️ **Dev statement for the shape; the storage was not found** — no mod support and no readable data files, so it
contributes a data point about design and none about format.

### 3.2 Infinitode 2 — the only standalone edge table found

The shipped `assets/res/researches.json` holds **593 nodes and 665 links**, with the links as a standalone array
of `parent`/`child`/`requiredLevels` records [[10]](#s10). A tree over 593 nodes would have 592 edges, so the 73
extra are the lattice. In-degree distribution: **519 nodes with one predecessor, 73 with two** — and **72 of
those 73 have their two parents in *different categories***, so these are real cross-branch merges rather than
local tidying. Roughly one node in eight reconverges.

⚠️ **Whether two incoming edges are AND or OR is not stated in the data or the docs.** AND is strongly implied —
under OR the second edge would be dead weight — but it is inference, and a format that leaves this to inference
is a format this repo should improve on rather than copy.

The record also carries its own drawing: nodes hold `x`/`y` and each link holds a bend point `pivotX`/`pivotY`,
so the graph and the diagram of it cannot drift apart.

The division of labour is worth copying: **the gate is on the edge** (`requiredLevels` — how far the parent must
be levelled before this link opens) while **the price is on the node.** That is the one clean example of a format
that puts something on the edge and something else on the row, and it split them along exactly the line you would
predict — *what unlocks* is a property of the pair, *what it costs* is a property of the destination.

### 3.3 Dota 2 item recipes — prerequisites on the target, with alternatives

Dota 2's `ItemRequirements` is the reconverging case done as prerequisites-on-target, and it is the only shape
found that supports **genuinely different totals per route**: requirements are given as numbered alternative
ingredient sets `"01"`, `"02"` [[6]](#s6). Two different component sets, two different totals, same destination
item. The shop **computes** the price difference rather than storing it.

That is the honest answer to "if two roads cost different totals, something has to say so": Dota's answer is
*nothing says so; it is derived at the point of sale.* Which is only affordable because the shop is code.

### 3.4 Gem TD and GemCraft — combining, and a graph that does not exist

GemCraft is the limiting case and the most quotable: **it stores no upgrade graph at all.** Combining two gems
computes `Cost = Component1.Cost + Component2.Cost` at runtime [[14]](#s14). There is nothing to author, nothing
to keep consistent, and no route-dependence question — the total is the sum of what went in, by definition.
Gem TD's fixed combine recipes are real but the storage was not found; ⚠️ **gameplay-guide level only.**

### 3.5 Ruled out — and one instructive near-miss

**Kingdom Rush is the near-miss and the best control case.** Every `tw_upgrade` edge in the shipped Lua was
extracted and checked: **zero reconvergent nodes**, in both KR1 and KR2 [[9]](#s9). Each base tower's menu offers
levels 2 and 3 plus **two** tier-4 specialisations, and the eight tier-4 towers are terminal — their menus carry
no `tw_upgrade` at all.

The mechanism that *terminates* the tree is a nice trick: a tier-4 target sets `tower.type` to a new type and
resets `tower.level = 1`, so the new type's menu table simply has no level-2 entry [[9]](#s9). The engine lookup
is literally `tower_menus[entity.tower.type][entity.tower.level]`.

**And the price tag is read off the destination**: `local nt = E:get_template(item.action_arg); price_tag =
tostring(nt.tower.price)` [[9]](#s9). Cost on the destination row, resolved through the edge — precisely decision 7.

Kingdom Rush uses the *same* storage shape as Element TD and never reconverges. That is what makes it the control:
**the format did not stop it; the design chose not to.**

Also ruled out: **Legion TD 2**, whose `UpgradesFrom` is a *singular string* and therefore structurally forbids
reconvergence [[15]](#s15) — the only format found that actually cannot do a diamond, and it is a
predecessor field, not a successor one. **Dungeon Defenders** keeps the whole ladder on the tower's own UE3
archetype as parallel arrays — `TArray<int> TowerUpgradeCosts` indexed by destination level, alongside
`MaxUpgradeLevel` and `UpgradeLevel` [[16]](#s16) — no branching, no next-tower pointer, and the ladder lives in
the same table as the stats. **Defense Grid** (three fixed ranks) and **Sanctum 2** (three linear ranks) do not
branch; ⚠️ **both guide-level only — neither exposes game data, and where their costs live is unresolved.**
**Orcs Must Die** turns out not to match its reputation: a linear 3-tier skull ladder *plus* an orthogonal
one-of-two equippable slot, which is a loadout and not a graph [[17]](#s17). **Mindustry** and **PvZ Heroes**
(a card game, no upgrade graph) were checked and do not reconverge.

**Plants vs Zombies 2 is worth one paragraph rather than one word, because it prices the step.** The decoded
RTON `PLANTLEVELS.json` holds 192 `PlantLevelStats` records and a complete key census contains **no
prerequisite, predecessor or successor field of any kind** — there is no graph. What it does have is a proven
off-by-one across parallel arrays: `len(LevelCoins) == len(LevelXP) == LevelCap - 1` while
`len(FloatStats[*].Values) == LevelCap`, which is the arithmetic signature of **cost on the transition, stats on
the destination** [[24]](#s24). It also keeps the ladder in a separate file from the stat table
(`PLANTPROPERTIES.json`), joined by an `RTID(...)` reference. So it belongs in the step-priced column below, not
merely in the ruled-out list.

---

## 4. Where the cost lives, and the "reachable two ways" problem

Collecting the answer, since #109 asks for one line and the evidence supports a firmer one.

| Format | Cost on… | Integers? | Route-dependent totals possible? |
|---|---|---|---|
| Factorio | destination (`unit = {count, ingredients, time}`) [[1]](#s1) | `count :: uint64`; ⚠️ `count_formula` for infinite techs | No |
| 0 A.D. | destination (`cost`, `researchTime`) [[2]](#s2) | yes for cost/time; floats only in effects | No |
| OpenRA | destination (`Valued: Cost:`) [[3]](#s3) | Integer | No |
| Warcraft III | destination (`goldbase` + `goldmod` × level) [[4]](#s4) | yes, bounded 0..99999 | No |
| Element TD | destination (`Cost`, `TotalCost`) [[8]](#s8) | yes | No — designed out by uniform tier pricing |
| Kingdom Rush | destination (`nt.tower.price`) [[9]](#s9) | yes | No (never reconverges) |
| Infinitode 2 | destination (gate on edge, price on node) [[10]](#s10) | yes | No |
| Dungeon Defenders | destination (`TowerUpgradeCosts[level]`) [[16]](#s16) | `TArray<int>` | N/A |
| **Bloons TD 6** | **step** (`UpgradeModel.cost`) [[7]](#s7) | int authored, ⚠️ float at sim boundary | **Yes — and one shipped edge does it** |
| **StarCraft II** | **step** (`<Resource>` on the ability's `InfoArray` slot) [[5]](#s5) | yes | **Yes** — same upgrade from two buildings could differ |
| **Plants vs Zombies 2** | **step** (`LevelCoins[]`, one shorter than the stat array) [[24]](#s24) | yes | N/A — no graph to route through |
| Dota 2 recipes | components (derived at the shop) [[6]](#s6) | yes | **Yes, deliberately** — alternative ingredient sets |

**Eight of twelve put it on the destination; one derives it; three put it on the step.** Of the three step
formats, the two with a *branching* graph are exactly the two where a destination can be priced differently by
route — which is not a coincidence but the definition. SC2 gets *flexibility* from it (the same upgrade could
legitimately cost more from a different building). BTD6 gets *a bug*. PvZ 2 gets away with it because its
"graph" is a straight line, which is the same licence Dungeon Defenders takes.

**And the sell-value problem is the tell.** Once a graph reconverges, "total invested" stops being derivable from
the graph, and there are exactly three shipped answers:

1. **Denormalize, and make it true by uniform pricing** — Element TD's `TotalCost`, safe only because every route
   costs the same [[8]](#s8).
2. **Track it at runtime on the instance** — Kingdom Rush's `tower.spent`, BTD6's `Tower.worth` [[7]](#s7)[[9]](#s9).
   Kingdom Rush's is three lines in `all/systems/tower_upgrade.lua` and shows how little the approach costs:

   ```lua
   ne.tower.spent = e.tower.spent + price
   local refund = km.round(e.tower.refund_factor * e.tower.spent)
   ```
3. **Recompute per route at the point of use** — Dota's shop [[6]](#s6).

> **For this repo, (2) is already the answer and it is free.** Decision 7 prices an upgrade at the target row's
> full price, so a running total on the placement is a single addition per upgrade and needs no arithmetic the
> cost table does not already do. Nothing needs to be stored in `upgrades.txt` for it.

---

## 5. What survives an in-place upgrade

Added from #108. The question: does a placement identity, an accumulated per-tower statistic, or a link to the
predecessor survive when a tower is upgraded in place?

**The answer splits cleanly into two families, and the tower defense in the sample is in the good one.**

| Game | Identity survives? | Per-unit counters survive? | Stored "upgraded from"? | Worth accumulates? |
|---|---|---|---|---|
| **Bloons TD 6** | **Yes** — same `Tower`, same `Tower.Id` (`ObjectId`) | **Yes** — `damageDealt`, `cashEarned` never reset | on the *model*: `appliedUpgrades` = whole ladder | **Yes** — `Tower.worth` = placement + every upgrade |
| Warcraft III | **Yes** — same unit handle; only `GetUnitTypeId` changes | no native counters; `SetUnitUserData` survives | **No** — the documented workaround is "build a hashtable" | N/A |
| StarCraft II | **Yes** for morphs — `Unit.tag` stable | no per-unit counters in the protocol | no (`unit_alias` relates *types*) | N/A |
| Age of Empires II | **No** — Genie *replaces* the unit object | none exist | no | N/A |
| OpenRA | **No** — `self.Dispose()` then `w.CreateActor(…)` | only what a trait opts into | no | N/A |
| Factorio | **No** — new `unit_number`; destroy + build events | N/A | no | N/A |

### 5.1 BTD6 keeps everything, and it is the precedent that matters

The upgrade **mutates the existing object**. The vanilla method is
`TowerManager.UpgradeTower(Tower tower, TowerModel def)` — an existing `Tower` instance plus a *new model*
[[7]](#s7). There is no `AddTower` anywhere in the upgrade path. The whole command is addressed **by id**:
`UnityToSimulation.UpgradeTower(int inputId, ObjectId towerId, int path, int tier, …)`, resolved with
`towerManager.GetTowerById(id)`.

**Two independent pieces of mod code prove the id and the counters survive.** An upgrade *queue* feature stores
`new QueuedUpgrade(tower.Id, path, tier, upgrade)` and re-resolves by `GetTowerById` several upgrades later — which
would break after the first step if `Id` changed. And an in-game damage chart computes per-round deltas as
`tower.damageDealt - previous[tower.Id]`, which only works if both the id and the accumulator survive
[[13]](#s13). The contrast case is the tell: a mod that genuinely creates a replacement tower has to copy the
counters by hand — `newtower.damageDealt = old.damageDealt; newtower.worth = old.worth;` [[13]](#s13).

**`Tower.worth` is provenance in monetary form.** Selling returns a percentage of *"the total amount of cash spent
on placing **and upgrading** it"* [[18]](#s18), and mod code that changes upgrade prices has to *correct* `worth`
afterwards (`tower.worth -= upgrade.cost; tower.worth += cost;`) because vanilla has already added the step's
price into it [[13]](#s13).

> ⚠️ **And the series is not consistent about this, which is the sharpest evidence of all.** The Bloons wiki
> records that *"in Bloons TD 5 Mobile, the Pop Count counter becomes 0 when a tower gets upgraded"* — and names
> BTD Battles Mobile and Bloons Monkey City Mobile alongside it [[18]](#s18). **BTD6 is deliberately not in that
> list.** The franchise shipped both behaviours and kept the one that preserves the counter for the game that
> displays per-tower damage. That is as close to a design ruling as this question is going to get.

### 5.2 What replacement costs, in the words of the engines that chose it

OpenRA is the reference implementation of the *other* family, and it is worth reading because it shows the price.
`Transform.cs` calls `self.Dispose()` and then `w.CreateActor(ToActor, init)` — a genuinely new actor with a new
id [[19]](#s19). To carry anything across, a trait must implement
**`ITransformActorInitModifier.ModifyTransformActorInit(Actor self, TypeDictionary init)`** — an interface whose
entire job is surviving an in-place upgrade. `GainsExperience` implements it to carry veterancy; `Cargo` to carry
passengers. Health is carried as a **percentage**, not an absolute, because MaxHP differs.

**The complete list of traits implementing it is four**, and two of them exist because of a bug report —
PR #15300, *"Fix Chronoshift-return and Iron Curtain being lost when a RA MCV (un)deploys"* [[19]](#s19). That is
literally "state was lost across an in-place upgrade", found in the field, after shipping.

Factorio is blunter. `LuaEntity.unit_number` is documented as *"A unique number identifying this entity for the
lifetime of the save"* — so a recreated entity necessarily gets a different one. And the upgrade is documented as
a destroy plus a build: `apply_upgrade()` raises `script_raised_destroy` for the old entity and
`script_raised_built` for the new, with a Factorio staff member confirming they are *"two different code
paths"* [[20]](#s20). Note that `apply_upgrade` **returns new `LuaEntity` handles** — a mutation would have
nothing to return. Every mod storing per-entity data keyed on `unit_number` must migrate it by hand on those
events, which is precisely the failure mode #108 is trying to avoid.

> **The design conclusion.** If per-tower statistics must sum across a ladder, **do not replace the placement**.
> BTD6 shows it works and is the shipped behaviour of the closest comparable game. OpenRA shows the alternative
> costs a hand-written transfer per statistic, forever, and that forgetting one is a silent data-loss bug rather
> than a crash.

---

## 6. Separate file, and what it bought

#109 asks whether anyone keeps the graph in a separate file from the unit table.

**Most do, and none of them says why.** Factorio's `technology.lua` sits beside `recipe.lua` and `item.lua`
[[1]](#s1); 0 A.D. goes furthest with **one hand-edited JSON file per technology**, ~170 of them, entirely
separate from the entity templates [[2]](#s2); SC2 splits `RequirementData.xml`, `RequirementNodeData.xml`,
`AbilData.xml` and `UnitData.xml` [[5]](#s5); WC3 splits the graph columns into `Profile.slk` while balance lives
in `UnitBalance.slk` [[4]](#s4). In tower defense specifically, **Kingdom Rush is the one that separates them** —
`tower_menus_data.lua` for the graph, `game_templates.lua` for the stats [[9]](#s9).

**The one inliner is the one with the worst signal-to-noise.** OpenRA puts `Prerequisites:` in the same YAML block
as `Health:`, `Armor:` and `RevealsShroud:`; the missile silo's block is roughly sixty lines of which two are
tech graph, and `Inherits: ^Building` means the effective row is not even fully visible in the diff [[3]](#s3).
**Dungeon Defenders inlines the whole ladder onto the tower archetype** and gets away with it precisely because
its ladder never branches [[16]](#s16).

**What separation buys, observably rather than by anyone's claim:** the graph stays reviewable. Adding one node in
0 A.D. is a new file with no context lines and no possible merge conflict; in Factorio it is one contiguous table
plus one token inside an existing `prerequisites` array; in OpenRA it is two meaningful lines buried in forty; in
SC2 it is three files and machine-minted identifiers like
`CountUnitAlias_SupplyDepotCompleteOnly3975993912TechTreeCheat`.

> **#107 decision 6 — a separate `content/upgrades.txt` with its own hash label — is the majority practice, and
> the minority practice is the one that reads worst in a diff.** No source argues for it; every source
> demonstrates it.

---

## 7. Reading the constraints against the evidence

| Constraint | Verdict |
|---|---|
| **Integers only, no decimal point** | **Comfortable.** BTD6's 790 upgrade files contain zero decimals in `cost`/`xpCost` [[12]](#s12); OpenRA `Cost` is Integer; 0 A.D. costs and research times are ints (floats appear only in *effects*); WC3 upgrade costs are ints bounded 0..99999. ⚠️ Two violations, both from one cause: **repeating or infinite upgrade tiers**. Factorio needs `count_formula = "2500*(L - 3)"` only because `max_level = "infinite"` [[1]](#s1); Mindustry derives cost through `pow(x, 1.11f)` [[11]](#s11). WC3 shows the disciplined middle: `goldbase` + `goldmod` × level, integers throughout [[4]](#s4). **A finite ladder never needs a formula.** ⚠️ Three further violations sit outside the graph and are worth separating, because none of them is about the ladder: Infinitode's `requiredLevelsLabelPos` (`0.5`) is a **label position** [[10]](#s10); Kingdom Rush's `range_factor` / `damage_factor` are **meta-progression multipliers** [[9]](#s9); and GemCraft's economy is float **end to end** [[14]](#s14) — the only genuinely non-integer economy surveyed, and it is also the only game with no authored graph to keep integral. |
| **A new unit is a row, never a column** | **Universal.** Every format adds a node as a row/record/file. BTD6 takes it furthest — a crosspath is a whole materialised `TowerModel`, 2,167 of them [[12]](#s12). Nothing surveyed adds a column to express a new tier. |
| **Hand-edited, reads well in a diff** | **This is where the formats separate**, and it argues for the prerequisite form in a separate file. Best: 0 A.D. (new file, zero context). Worst: SC2 (three files, hashed ids) and WC3 (columnar SLK behind a GUI — **the genre's ancestor is the worst model here for a text-first format**). The out-edge form has a specific diff hazard: adding a tier edits the *parent's* row, so a new unit's arrival is split across two places. |
| **The simulation will not enforce the graph** | **Normal, and BTD6 is the proof.** Its simulation has no crosspath validation — the rule is expressed by which models and edges exist, and the greying-out is interface code [[13]](#s13). ⚠️ The corollary the sources also supply: **nothing checks it either.** BTD6's mislabeled Skywarden edge and duplicated Boomerang edge both shipped [[12]](#s12), and WC3's double representation (`ureq` *and* `uupt`) has no consistency check at all [[4]](#s4). #107's open question "whether anything validates the ladder" is a real one, and the answer everywhere else is "no, and it shows." |

### What the evidence would have this repo do

Not decisions — #109 is a reading ticket and #107 holds the pen. But the sources point somewhere:

1. **Put the relationship on the new row, naming what precedes it.** It is the majority shape, it is the shape
   `Unlocks` already resembles, and it makes the Mage diamond one extra line rather than a format change.
2. **Keep cost off the edge.** Eight of twelve put it on the destination, and of the three that put it on the
   step, the two with a branching graph are the two where a route can mis-price a destination — one of which
   demonstrably did.
3. **A repeated, optional row is the right arity.** Nothing surveyed used a fixed-arity rule row for this, and
   Mindustry's single-parent nesting is the one shape that had to be extended to do a diamond at all.
4. **Do not replace the placement on upgrade.** BTD6 keeps the id and the counters; every engine that replaces
   pays a per-statistic tax and has the bug reports to show for it.
5. **Expect to want a lint eventually.** Every unvalidated graph in the survey has defects in shipped data.

---

## Sources

Trust is labelled per entry. **Primary** means a shipped data file, an official API reference, or first-party
source. **Extracted** means exported or decompiled game data. **Mod source** means code compiled against the real
game assembly — real signatures, community authorship.

<a id="s1"></a>1. **Factorio — technology prototypes.**
[`TechnologyPrototype`](https://lua-api.factorio.com/latest/prototypes/TechnologyPrototype.html),
[`TechnologyUnit`](https://lua-api.factorio.com/latest/types/TechnologyUnit.html),
[`wube/factorio-data` `base/prototypes/technology.lua`](https://github.com/wube/factorio-data/blob/master/base/prototypes/technology.lua)
(5,468 lines). `prerequisites :: array[TechnologyID]` on the target — *"List of technologies needed to be
researched before this one can be researched."* `rocket-silo` names eight. Cost is `unit = {count, ingredients,
time}` on the target; `count :: uint64`, must be > 0. `count_formula :: MathExpression` exists only for
`max_level = "infinite"`; real shipped values include `"2500*(L - 3)"` and `"2^(L-7)*1000"`. Techs point at
recipes via `effects = {{type = "unlock-recipe", …}}`; recipes only say `enabled = false` and never name a tech.
**Primary (official API docs + shipped data).**

<a id="s2"></a>2. **0 A.D. — technology JSON.**
[`simulation/data/technologies/`](https://github.com/0ad/0ad/tree/master/binaries/data/mods/public/simulation/data/technologies)
(~170 files, one per tech),
[`TechnologyManager.js`](https://github.com/0ad/0ad/blob/master/binaries/data/mods/public/simulation/components/TechnologyManager.js).
`requirements` is a boolean tree — `{"all": […]}`, `{"any": […]}`, leaves `{"tech":…}`, `{"civ":…}`,
`{"entity": {"class":…, "number": N}}`. `cost` and `researchTime` on the target row, integers; floats appear only
in `modifications`. `supersedes` (single string) and `replaces` (array). Either/or branches are a **third row** —
a four-line `pair_unlock_*.json` with `top`/`bottom`, mutual exclusion enforced in code. **Primary (open-source
shipped data).**

<a id="s3"></a>3. **OpenRA — rules YAML.**
[`mods/ra/rules/structures.yaml`](https://github.com/OpenRA/OpenRA/blob/bleed/mods/ra/rules/structures.yaml),
[trait docs](https://docs.openra.net/en/release/traits/),
[Tech levels and Prerequisites](https://github.com/OpenRA/OpenRA/wiki/Tech-levels-and-Prerequisites).
`Buildable: Prerequisites:` is a comma-separated list on the target actor; `!` negates, `~` hides, `~!` hides when
met. AND-only — OR is faked with a named alias node via `ProvidesPrerequisite: Prerequisite: anypower`. Cost is
`Valued: Cost:`, Integer, on the same actor. The only surveyed format that inlines the graph into the entity
table. **Primary (source + official docs);** ⚠️ the prose of the `Prerequisites` description came via a
summarising fetch — field name and type are read directly, wording may be paraphrased.

<a id="s4"></a>4. **Warcraft III — Blizzard's object metadata tables.** `UnitMetaData.slk` / `UpgradeMetaData.slk`,
mirrored at [`sumneko/w3x2lni`](https://github.com/sumneko/w3x2lni/tree/master/data/enUS-1.27.1/mpq/Units); SLK
parsed directly. `ureq` (`Requires`, `techList`) and `urqa` (`Requiresamount`, `intList`, positionally parallel)
on the target; `uupt` (`Upgrade`, `unitList`, **maxVal 12**, buildings only) on the source — the graph is stored
**both directions at once**. `udep` is literally named `DependencyOr`. Upgrade costs are integers:
`gglb`/`gglm` (`goldbase`/`goldmod`), `glmb`/`glmm`, `gtib`/`gtim`, `glvl` 1..100. Graph columns live in
`Profile.slk`, balance in `UnitBalance.slk`. **Primary (shipped metadata, parsed).** ⚠️ The raw id is `uupt`, not
`upgt` as commonly written; the reason for the SLK split and the diff-quality judgement are inference.

<a id="s5"></a>5. **StarCraft II — GameData XML.** [SC2Mapster/SC2GameData](https://github.com/SC2Mapster/SC2GameData):
`RequirementNodeData.xml` (full algebra — `CRequirementAnd/Or/Xor/Not/GT/LT/Eq/Sum/Mul/Const`, `CountUnit`,
`CountUpgrade`, `AllowUnit`…), `RequirementData.xml` (`CRequirement` with separate `index="Use"` and
`index="Show"` expressions), `AbilData.xml`. **Cost is on the step** — `Time=` and `<Resource>` sit on the
ability's `InfoArray index="Research3"` slot, not on the `CUpgrade` row. Machine-minted ids like
`CountUnitAlias_SupplyDepotCompleteOnly3975993912TechTreeCheat`. **Primary (shipped data).**

<a id="s6"></a>6. **Dota 2 — item recipes.** [ModDota item KeyValues reference](https://moddota.com/abilities/item-keyvalues);
shipped recipe rows in [`Pizzalol/SpellLibrary`](https://github.com/Pizzalol/SpellLibrary) (`item_recipe_manta_datadriven.txt`)
and [`OpenAngelArena/oaa`](https://github.com/OpenAngelArena/oaa) (`item_skadi.txt` with a repeated component,
`item_magic_lamp.txt` with two genuine alternative routes to one node). `ItemRequirements` on the
target item, with numbered alternative ingredient sets `"01"` / `"02"` — the only surveyed shape that supports
genuinely different totals to one destination; the shop computes the difference rather than storing it.
**Primary (shipped KV + first-party modding docs).**

<a id="s7"></a>7. **Bloons TD 6 — model types via BTD Mod Helper.**
[`gurrenm3/BTD-Mod-Helper`](https://github.com/gurrenm3/BTD-Mod-Helper) — API docs, `Tests/ModelSerializationTests.cs`,
`Patches/Towers/TowerManager_UpgradeTower.cs`, `Extensions/SimulationExtensions/TowerExt.cs`. `TowerModel.tiers`
is `Il2CppStructArray<int>`; `appliedUpgrades` is `Il2CppStringArray`; `upgrades` is
`Il2CppReferenceArray<UpgradePathModel>`; `UpgradePathModel` is `{string upgrade, string tower}`, ctor order
`(upgrade, tower)`. `GameModel` carries `upgrades`, `upgradesByName`, `bloons`, `bloonsByName` — and **no
`towersByName`**. Upgrade entry point is `TowerManager.UpgradeTower(Tower tower, TowerModel def)`; the sim command
is `UnityToSimulation.UpgradeTower(int inputId, ObjectId towerId, int path, int tier, …)`. **Mod source compiled
against the shipped assembly.**

<a id="s8"></a>8. **Element TD — the open-source Dota 2 port.**
[MNoya/Element-TD](https://github.com/MNoya/Element-TD), by the original Warcraft 3 designer Karawasa —
`game/dota_addons/element_td/scripts/npc/npc_units_custom.txt` for the tower KV data, plus
`mechanics/upgrades.lua` (`MeetsItemElementRequirements`, and the step price read at line 7),
`mechanics/sell.lua` (the `TotalCost` refund) and `panorama/scripts/tower_table.js` (the element-matrix
display). `blacksmith_tower` appears in the
`Upgrades` out-edge list of both `fire_tower` and `earth_tower`; `enchantment_tower` has three dual predecessors.
`Cost`/`TotalCost` on the destination; 6 singles at 175, 15 duals (C(6,2)) at 425/600, 20 triples (C(6,3)) at
900/1500. `sell.lua` is why `TotalCost` is denormalized. **Primary (open-source shipped data).**

<a id="s9"></a>9. **Kingdom Rush — shipped LuaJIT data.**
[KR1](https://github.com/DanQZ/KR1-Smarter-Soldiers-Mod/blob/main/kr1/data/tower_menus_data.lua) and
[KR2](https://github.com/DanQZ/KRF-Smarter-Soldiers-Mod/blob/main/kr2/data/tower_menus_data.lua) mirrors, plus
`kr1/game_templates.lua` and `all-desktop/game_gui.lua`. Menu lookup is
`tower_menus[entity.tower.type][entity.tower.level]`; tier-4 targets set a new `tower.type` and reset
`tower.level = 1`, terminating the tree. Price tag read off the destination:
`local nt = E:get_template(item.action_arg); price_tag = tostring(nt.tower.price)`. **Every `tw_upgrade` edge was
extracted and checked: zero reconvergent nodes.** KR5 moves numbers to `kr5/data/balance/balance.lua` as parallel
arrays indexed by level. **Extracted (shipped Lua via community mirrors).** ⚠️ Ironhide's desktop engine is
C++/bgfx with logic in LuaJIT — an earlier description of it as LÖVE was wrong. No decompiled ActionScript from
the 2011 Flash originals exists, so none of this speaks to those. Some KR5 dump entries are suspected mod
artifacts and were **not** counted as reconvergence evidence.

<a id="s10"></a>10. **Infinitode 2 — `res/researches.json`.** 593 nodes, 665 links, as a standalone array of
`parent`/`child`/`requiredLevels` records. In-degree `{1: 519, 2: 73}`. Gate on the edge, price on the node.
**Extracted (shipped game data).**

<a id="s11"></a>11. **Mindustry — `TechTree`.**
[`core/src/mindustry/content/TechTree.java`](https://github.com/Anuken/Mindustry/blob/master/core/src/mindustry/content/TechTree.java),
[`SerpuloTechTree.java`](https://github.com/Anuken/Mindustry/blob/master/core/src/mindustry/content/SerpuloTechTree.java),
`world/Block.java`. `TechNode` has a single `parent`, captured off a static mutable `context` during the nested
lambda; extra predecessors require `Seq<Objective> objectives`. Cost derived via
`Mathf.pow(requirements[i].amount, 1.11f)` with multipliers inherited from the parent node. Not a data file —
compiled Java. **Primary (open source).**

<a id="s12"></a>12. **Bloons TD 6 — exported game model.**
[`Amphiapple/BTD6_Towers`](https://github.com/Amphiapple/BTD6_Towers) (`GameModelExporter` output, versioned
`Towers_V40`…`Towers_V54`, so the graph is diffable across game versions). Census findings: 2,167 `TowerModel`s;
64 per standard tower; name-suffix and `tiers` array agree with zero mismatches; **3,292 of 3,293 edges satisfy
`total(target) − total(source) == cost(edge)`**; the exception is `Skywarden-014 → Skywarden-024` naming
`StormsPulse` (tier 0, 175) where `ThunderingArc` (tier 1, 275) belongs, plus a duplicated `BoomerangMonkey` edge;
**790 upgrade JSON files contain zero decimal points in `cost` or `xpCost`**; Paragon is a scalar `paragonUpgrade`
field. **Extracted (shipped game data).** ⚠️ That the mislabeled edge actually mis-charges the player is
well-supported inference, not confirmed — see [[13]](#s13). No public Il2Cpp `dump.cs` for BTD6 exists.

<a id="s13"></a>13. **Bloons TD 6 — behaviour via mod source.**
[`doombubbles/PathsPlusPlus`](https://github.com/doombubbles/PathsPlusPlus) (`Patches/GamePatches.cs`,
`PathPlusPlus.cs` — `DefaultValidTiers`, and setting a custom price by swapping the tower's `upgrades` array
before `GetTowerUpgradeCost`), `UltimateCrosspathing` (`Patches.cs` — touches only `UpgradeObject`,
`PowerProUpgradeObject`, `TowerSelectionMenu`, `Bank`, `Attack`; **no simulation method**),
[`doombubbles/UsefulUtilities`](https://github.com/doombubbles/UsefulUtilities) (`Utilities/UpgradeQueueing.cs`
storing `new QueuedUpgrade(tower.Id, …)`; `Utilities/InGameCharts/Meters.cs` computing
`tower.damageDealt - previous[tower.Id]`), `doombubbles/Paragonomics` (`tower.worth -= upgrade.cost; tower.worth
+= cost;`), [`GrahamKracker/KeystrokeActions`](https://github.com/GrahamKracker/KeystrokeActions)
(`TowerGoesDownATier.cs`, copying `damageDealt` and `worth` by hand when a tower really is recreated). Sim
surface: `TowerManager.CanUpgradeTower(Tower, int, int, int, ref float cost)`, `GetTowerUpgradeCost`,
`IsTowerPathTierLocked`, `Tower.GetUpgrade(int path)`. **Mod source compiled against the shipped assembly.**
⚠️ Reading `IsTowerPathTierLocked` as a progression lock rather than a crosspath check is inference.

<a id="s14"></a>14. **GemCraft — gem combining.**
[`gemforce-team/wGemCombiner`](https://github.com/gemforce-team/wGemCombiner/blob/master/WGemCombiner/Gem.cs).
Stores no upgrade graph; a gem literally *is* a two-parent node, holding `Component1` and `Component2` as
permanent fields, and a combined gem's cost is `Component1.Cost + Component2.Cost` computed at runtime. The one
genuinely float economy in the survey. **Reverse-engineered, not shipped data** — the devs did publish formula
notes, but this tool is a community reconstruction. ⚠️ Gem TD's fixed combine
recipes are real but their storage was not found — **gameplay-guide level only.**

<a id="s15"></a>15. **Legion TD 2 — unit data.** `UpgradesFrom` is a **singular string**, which structurally
forbids reconvergence — the only format surveyed that actually cannot express a diamond, and notably it is a
*predecessor* field rather than a successor one. The official API schema
[`SteffenCarlsen/LegionTD2_Api`](https://github.com/SteffenCarlsen/LegionTD2_Api) `docs/UnitStats.md` declares
`UpgradesTo | List<string>` against `UpgradesFrom | string`, alongside `GoldCost`, `GoldValue` and `TotalValue`;
the community parse in `attrib/legion2-builder` (`GameDefs.kt`) agrees field for field
(`@SerializedName("upgradesfrom") val upgradesFrom: String?`). Note `totalvalue` is the same denormalised
accumulated-spend field Element TD calls `TotalCost`. **Primary (official API schema), corroborated by an
independent parse of the shipped data.**

<a id="s16"></a>16. **Dungeon Defenders — UDK SDK class dump.**
[`butter124/DD_NewSDK`](https://github.com/butter124/DD_NewSDK) (`DD_UDKGame_classes.hpp`, `DD_UDKGame_structs.hpp`).
`UDKGame.DunDefTower` carries `TArray<FTowerUpgradeStat> TowerUpgradeInfos`, **`TArray<int> TowerUpgradeCosts`**
(indexed by destination level), `TArray<float> TowerUpgradeTimes`, `int MaxUpgradeLevel`, `int UpgradeLevel`,
`GetCostToUpgradeTower()`. No branching, no next-tower pointer; the whole ladder is `Edit` properties on the
tower's own archetype — i.e. the same table as its stats. Steps cost 100/200/400/700/1220 mana. **Extracted
(class dump of shipped package).**

<a id="s17"></a>17. **Orcs Must Die! — traps.** [Official wiki](https://orcsmustdie.fandom.com/wiki/Traps) and the
[OMD2 XML modding reference](https://orcsmustdie.fandom.com/wiki/Mod:Orcs_Must_Die!_2_XML_Reference). A linear
3-tier skull ladder *plus* a single 1-of-2 equippable unique-upgrade slot ("only one can be equipped at a time",
swappable) — a loadout, not a graph. OMD2 runs on Trinigy Vision with user-editable XML (`<Entity>`, `<Stats>`,
`<Trap>`). ⚠️ **Where the tier costs live is unresolved** — the XML reference contains no occurrence of
upgrade/tier/cost/skull/prerequisite. **First-party wiki + community modding reference.**

<a id="s18"></a>18. **Bloons wiki — Pop Count and Selling.**
[Pop Count](https://bloons.fandom.com/wiki/Pop_Count) (retrieved as wikitext): *"In Bloons TD 5 Mobile, the Pop
Count counter becomes 0 when a tower gets upgraded. This also applies to Bloons Tower Defense Battles Mobile and
Bloons Monkey City Mobile."* — **BTD6 is not in that list**; BTD6 renamed it Damage Count and split off a Cash
Count. [Selling](https://bloons.fandom.com/wiki/Selling): sale returns a percentage of *"the total amount of cash
spent on placing and upgrading it"* (70% default); sacrificed towers transfer their value.
**Community wiki, first-party behaviour.**

<a id="s19"></a>19. **OpenRA — transform machinery.**
[`OpenRA.Mods.Common/Activities/Transform.cs`](https://github.com/OpenRA/OpenRA/blob/bleed/OpenRA.Mods.Common/Activities/Transform.cs),
`TraitsInterfaces.cs`,
[`GainsExperience.cs`](https://github.com/OpenRA/OpenRA/blob/bleed/OpenRA.Mods.Common/Traits/GainsExperience.cs).
`self.Dispose()` then `w.CreateActor(ToActor, init)`. `INotifyTransform` has `BeforeTransform`, `OnTransform`,
`AfterTransform`; `ITransformActorInitModifier.ModifyTransformActorInit(Actor self, TypeDictionary init)` is the
only route for carrying state. Exactly four implementers: `GainsExperience`, `Cargo`, and (C&C mod)
`Chronoshiftable`, `ConyardChronoReturn` — the latter two added by
[PR #15300](https://github.com/OpenRA/OpenRA/pull/15300), *"Fix Chronoshift-return and Iron Curtain being lost
when a RA MCV (un)deploys"*. Health carried as a percentage. **Primary (open source).**

<a id="s20"></a>20. **Factorio — entity identity across fast replace.**
[`LuaEntity.unit_number`](https://lua-api.factorio.com/latest/classes/LuaEntity.html#unit_number) — *"A unique
number identifying this entity for the lifetime of the save."* `apply_upgrade()` raises `script_raised_destroy`
for the old entity and `script_raised_built` for the new, and returns **new** `LuaEntity` handles;
[forum thread](https://forums.factorio.com/viewtopic.php?p=699134) with Rseding91 (Factorio Staff) confirming
these are *"two different code paths."* Recipes and modules are preserved by the
[upgrade planner](https://wiki.factorio.com/Upgrade_planner); wire connections historically were not — see the mod
[Combinator Fast Replace](https://mods.factorio.com/mod/combinator-fast-replace). **Primary (official API docs +
developer statements).**

<a id="s21"></a>21. **Warcraft III — unit identity across upgrade.**
[Hive Workshop, "How to get building being upgraded?"](https://www.hiveworkshop.com/threads/how-to-get-building-being-upgraded.355791/):
*"There is only one unit… The unit itself never goes anywhere (it isn't literally replaced) so there is no [way to
get the old one] … you will have to either detect the order the instant it is given or build a database
(hashtable) to return parent buildings."* The type flips at order time, so the event's triggering unit already
reports the new type. `EVENT_PLAYER_UNIT_UPGRADE_START/CANCEL/FINISH` exist in
[`common.j`](https://github.com/lep/jassdoc/blob/master/common.j) with no descriptive documentation. No per-unit
kill counter native exists. **Community expert + primary API listing.**

<a id="s22"></a>22. **StarCraft II — unit tag across morph.**
[`s2client-api` `sc2_unit.h`](https://github.com/Blizzard/s2client-api/blob/master/include/sc2api/sc2_unit.h)
(`Tag` — *"A unique identifier for the instance of a unit"*);
[`python-sc2` `bot_ai_internal.py` / `bot_ai.py`](https://github.com/BurnySc2/python-sc2) — `on_unit_type_changed`
is reached by looking up the **same tag** in the previous frame's map and comparing `type_id`, and the docs name
"a hatchery morphed to lair" among its cases; morphs correspondingly do **not** fire `on_unit_created`. The
protocol carries no per-unit kill counter; the Galaxy editor's *Modify Unit* effect can explicitly *"copy
information including life, veterancy, and kills between units"*
([SC2Mapster](https://sc2mapster.fandom.com/wiki/Data/Effects/Modify_Unit)). **Primary API + widely-used
community library.** ⚠️ Zergling→Baneling cocoon continuity was not confirmed.

<a id="s23"></a>23. **Age of Empires II — unit replacement.**
[openage, "The openage Converter, Part II"](https://blog.openage.dev/the-openage-converter-part-ii-preparations-for-conversion.html)
— in AoE2 *"units are replaced with upgraded unit"*, in contrast to openage which *"upgrades only the attributes
that actually change."* Genie's tech effect is "Upgrade Unit (from → to)", applied to every instance of the source
type. AoE2 exposes no per-unit counters. **Clean-room reimplementation team's technical writing.** ⚠️ Whether the
engine reuses the object slot is not documented anywhere found; "AoE2 tracks per-unit kills" could not be
confirmed and should not be repeated.

<a id="s24"></a>24. **Plants vs Zombies 2 — decoded RTON plant tables.** `.VAX/PLANTLEVELS.json` (from the
shipped OBB; corroborated by `Kenny3-2/PVZ2-Modifications` `PlantLevels.json`). All 192 records are
`objclass: "PlantLevelStats"`, and a complete key census contains **no prerequisite, predecessor or successor
field** — there is no graph. Cost-on-step is established arithmetically rather than by a field name:
`len(LevelCoins) == len(LevelXP) == LevelCap - 1` while `len(FloatStats[*].Values) == LevelCap`. Separate file
from the stat table `PLANTPROPERTIES.json`, joined by an `RTID(...)` reference. The nearest thing to an edge is
`"CanPlantAgainToUpgrade": true` on Peapod and Ultomato, with numbered `BeamDPSUpgrade1/2/3` on the *same* row.
**Extracted (decoded shipped data).** ⚠️ PvZ Heroes is a card game and has no upgrade graph; it is not evidence
either way.
