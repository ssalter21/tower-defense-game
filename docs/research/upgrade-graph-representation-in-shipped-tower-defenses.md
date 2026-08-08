# How shipped tower defenses store an upgrade graph, and what a reconverging path costs them

**Research note** · 8 August 2026 · commissioned by
[#109](https://github.com/ssalter21/tower-defense-game/issues/109), a ticket of the
[upgrade edge map (#107)](https://github.com/ssalter21/tower-defense-game/issues/107); input to
[`docs/roster.md`](../roster.md#what-this-roster-needs-that-the-schema-does-not-have) lever 1 and to the
design of `content/upgrades.txt`

**Question:** how do shipped tower defense games represent an upgrade graph *in data* — not how upgrades feel —
and what does a branching-and-reconverging path cost them?
**Why it was asked:** the Mage line splits at tier 2 into Pyromancer and Cryomancer and both roads end at the
same tier-3 Frostfire Archmage, so tier 3 has **two predecessors**. Neither a single "next" field nor a single
"previous" field can express that, and nobody had looked at how anyone else writes one.

**Provenance is flagged per claim.** `[PRIMARY]` = shipped data file, official API schema, modding
documentation or developer statement. `[GUIDE]` = gameplay-guide or wiki level only, cited for design shape and
never for data format.

---

## The headline

**Storage shape predicts whether a game can reconverge at all.** There are three shapes in the wild, and the
choice is not an implementation detail — it *is* the design decision:

1. **A single parent pointer on the child.** Legion TD 2's `upgradesfrom: string`, Mindustry's `parent`. A tree
   by construction; reconvergence is structurally impossible.
2. **An out-edge list on the source.** Element TD's `Upgrades`, Warcraft 3's `UUPT`, Kingdom Rush's per-level
   menu arrays. Reconvergence is **free** — you name the same target twice.
3. **A standalone edge table, or a component list on the target.** Infinitode 2's `links[]`, Dota 2's
   `ItemRequirements`. Reconvergence is native, and this is the **only** shape that cleanly supports multiple
   *alternative* routes to one node.

The sharpest observation in the survey: **Element TD and Kingdom Rush use the identical mechanism and only
Element TD exploits it.** Kingdom Rush could reconverge for free and never does. Meanwhile shapes 3 and the
prerequisites-on-target family appear *only* in games that actually reconverge. Nobody reaches for the
expressive shape and then declines to use it.

## The survey

| Game | Reconverges | Graph storage | Cost lives | Separate file | Provenance |
|---|---|---|---|---|---|
| Element TD (Dota 2) | **yes** | out-edge list on source, + element gate on target | dest row `Cost` + `TotalCost` | no | `[PRIMARY]` |
| Element TD 2 | **yes** | unknown | unknown | unknown | dev statement |
| Infinitode 2 | **yes** | standalone edge array `links[]` | node `levels[i].price`; gate on edge | **yes** | `[PRIMARY]` |
| Dota 2 recipes | **yes** | prereqs on target, numbered alternative sets | step `ItemCost`; total computed per route | yes | `[PRIMARY]` |
| Gem TD (WC3) | **yes** | fixed recipes; storage unknown | inputs only | unknown | `[GUIDE]` |
| GemCraft | **yes** | none — computed from two parent pointers | summed from parents, floats | n/a | reverse-engineered |
| Legion TD 2 | no | `upgradesfrom: string` on target (**singular**) | `goldcost` + `totalvalue` | n/a | `[PRIMARY]` (API) |
| Kingdom Rush 1/2/5 | no | out-edges on source, per-level menu arrays | dest `tower.price`; spend at runtime | **yes** | `[PRIMARY]` |
| Mindustry | no | single `parent` + nested DSL; `Research` objectives carry the real DAG | node `requirements` | n/a | `[PRIMARY]` |
| Dungeon Defenders | no | `UpgradeLevel` vs `MaxUpgradeLevel` int | `TowerUpgradeCosts[level]` | **no** | `[PRIMARY]` (SDK) |
| Plants vs Zombies 2 | no | none | step `LevelCoins[]` | yes | `[PRIMARY]` (decoded RTON) |
| Orcs Must Die 2/3 | no | linear ladder + orthogonal 1-of-2 slot | per tier, in skulls | unknown | `[GUIDE]` |
| Defense Grid | no | int level | unknown | n/a | `[GUIDE]` |
| Sanctum 2 | no | unknown | unknown | unknown | `[GUIDE]` |

## Element TD — the closest case, and its source is public

`[PRIMARY]` — [MNoya/Element-TD](https://github.com/MNoya/Element-TD), fully open source, by the original
Warcraft 3 designer Karawasa. This project's stated anchor, so it is the first place to look. All quotes from
`game/dota_addons/element_td/scripts/npc/npc_units_custom.txt`.

**It genuinely reconverges.** `blacksmith_tower` (fire + earth) appears in the `Upgrades` block of *both*
`fire_tower` and `earth_tower`. The triple `enchantment_tower` is listed by **three** duals.

```
"earth_tower"
{
    "Level" "1"   "Cost" "175"   "TotalCost" "175"
    "Element" "earth"   "DamageType" "earth"
    "Requirements" { "earth" "1" }
    "Upgrades" { "Count" "6"
        "1" "focused_earth_tower"  "2" "hydro_tower"  "3" "blacksmith_tower"
        "4" "moss_tower"  "5" "quark_tower"  "6" "gunpowder_tower" }
}
"blacksmith_tower"
{
    "Level" "1"   "Cost" "425"   "TotalCost" "600"
    "Element"      { "1" "fire"  "2" "earth" }
    "Requirements" { "fire" "1"  "earth" "1" }
}
```

**Three separate concepts, and it is worth not conflating them:**

- `Upgrades` on the **source** is the ordered out-edge list. This is what controls reachability, and
  reconvergence is just naming the same target from two rows.
- `Requirements` on the **target** is *not* a predecessor check. It is a player-global element-level gate —
  `mechanics/upgrades.lua: MeetsItemElementRequirements` tests `playerData.elements[e]`. It decides whether the
  button is **live**, not whether the edge exists.
- `Element` on the target is the composition list — semantic identity, separate from both.

**Cost sits on the destination row.** `mechanics/upgrades.lua` line 7:
`local cost = GetUnitKeyValue(newClass, "Cost")`.

**All routes into a reconvergent node cost the same, and that is load-bearing.** Every node in a tier is priced
identically — 6 singles at `175`/`175`, 15 duals at `425`/`600` (= C(6,2) exactly), 20 triples at `900`/`1500`
(= C(6,3) exactly). Uniform predecessor pricing is the *only* reason the denormalised scalar `TotalCost` can be
truthful. It exists because sell-back cannot walk the path backwards to sum it — `mechanics/sell.lua`:

```lua
local goldCost = GetUnitKeyValue(tower.class, "TotalCost")
local refundAmount = round(goldCost * sellPercentage)
```

**Display is not a tree.** `panorama/scripts/tower_table.js` renders an *element matrix*; hovering an element
glows every dual and triple whose composition contains it. When the graph reconverges, set-membership beats a
tree diagram.

## Infinitode 2 — a standalone edge table, and the numbers to size one by

`[PRIMARY]` — shipped `assets/res/researches.json`, path confirmed by
[Prineside's own dev-mode docs](https://infinitode.prineside.com/modding/?p=dev-mode-usage).

**593 nodes, 665 links.** A tree would have 592 edges. In-degree is `{1: 519, 2: 73}` — 73 nodes have two
distinct parents, 72 of them with parents in *different* categories. Real cross-branch merges, at scale.

The graph is a flat edge array naming both endpoints:

```json
{ "parent": "TOWER_BASIC_DAMAGE", "child": "TOWER_BASIC_GENERATION_ONE",
  "requiredLevels": 5, "pivotX": 4240, "pivotY": 2160 }
```

**Cost on the node, gate on the edge.** `requiredLevels` rides the link; `levels[i].price` rides the node. That
split is precisely what makes route-dependent pricing impossible to express, which is a feature if you do not
want it.

**Caveat:** whether two incoming edges are AND or OR is *not* stated in the data or the docs. AND is strongly
implied — under OR the second edge is dead weight — but it is inference.

## Kingdom Rush — the only one that keeps graph and stats in separate files

`[PRIMARY]` — decompiled LuaJIT from the desktop builds, carrying `-- chunkname: @./kr1/upgrades.lua`
provenance headers. (Engine correction worth recording: these are **not** Unity and not Flash. Ironhide's own
C++/bgfx engine, with all logic and data in LuaJIT.)

Every `tw_upgrade` edge was extracted and checked for two-parent nodes: **zero reconvergent nodes** in KR1 and
KR2. Four base towers, each `→ _2 → _3 →` one of two tier-4 towers, and the tier-4s have no `tw_upgrade` at all.

The graph lives in `data/tower_menus_data.lua` as per-level button lists; the stats and costs live in
`game_templates.lua`, with tuning later split again into `balance.lua`. **It is the only game in the survey
that separates them** — which is direct precedent for map decision 6, a standalone `content/upgrades.txt`.

**How the ladder terminates is a neat trick.** A tier-4 target resets `tower.level = 1` and *changes*
`tower.type`, becoming a new type whose menu table simply has no level-2 entry. No terminator flag, no
max-tier field.

**And it tracks accumulated spend as runtime state on the instance, not from the graph** —
`all/systems/tower_upgrade.lua`:

```lua
ne.tower.spent = e.tower.spent + price
local refund = km.round(e.tower.refund_factor * e.tower.spent)
```

## Does anything survive an in-place upgrade?

This was added to the ticket mid-flight, because
[#108](https://github.com/ssalter21/tower-defense-game/issues/108) decided that provenance **must** survive an
upgrade so per-tower stats can be summed across a ladder. The survey's answer is narrow but useful:

- **Nobody stores a link back to what a tower used to be.** Not one of the fourteen. The upgraded tower is the
  destination row, and its ancestry is not recorded anywhere.
- **But two games keep an accumulator on the instance**, which is the same need solved without a link.
  Kingdom Rush's `tower.spent` is a running total on the live tower, and Dungeon Defenders carries
  `UpgradeLevel` as an `Edit, Net` property on the tower actor.
- **GemCraft is the outlier and the only true provenance case**: a gem *is* its two parents. `Component1` and
  `Component2` are permanent fields, and `Cost` is summed from them at runtime, never stored
  ([`WGemCombiner/Gem.cs`](https://github.com/gemforce-team/wGemCombiner/blob/master/WGemCombiner/Gem.cs)).
  Reverse-engineered, not shipped data.

So #108's decision — that the placement keeps its **identity** through the swap and stats key on
`(placement id, unit type id)` — has no direct precedent, but it is the same family as Kingdom Rush's
per-instance accumulator, generalised from one scalar to a keyed set. That is the shape to steal.

## The pricing consequence — the real lesson

**Once the graph reconverges, "total invested" stops being derivable from the graph.** Three shipped answers,
and the choice matters:

1. **Make all predecessors cost the same and store a denormalised scalar** — Element TD's `TotalCost`, backed by
   uniform tier pricing. Cheapest to read, and *silently wrong* the moment you break the symmetry.
2. **Track it as runtime state on the instance** — Kingdom Rush's `tower.spent`. Correct for any graph, costs a
   field on the live object.
3. **Recompute per route at display time** — Dota's shop summing components plus recipe. The only one that can
   honestly show two different totals for two routes to one node.

**This bears directly on a decision already taken.** Map decision 7 prices an upgrade at the full price of the
target row with the source's gold sunk, which makes total-invested route-dependent as soon as two roads of
different length reach the Frostfire Archmage. Option 1 is therefore unavailable unless uniform tier pricing
becomes a hard rule — and `docs/roster.md`'s intended shallow-U reward curve is explicitly *not* uniform.

## One more stealable pattern

**Mindustry splits the tree from the DAG** (`core/src/mindustry/content/TechTree.java`). A strict single-parent
`parent` field drives layout and cost inheritance — the authoring DSL is nested lambdas, so you *literally
cannot type* a second parent — and an independent prerequisite list carries the true dependency graph:

```java
public @Nullable TechNode parent, rootNode;   // singular
public ItemStack[] requirements;              // cost on the node
public Seq<Objective> objectives;             // the real DAG
```

The UI stays a clean tree while the dependencies reconverge underneath. Relevant if the diamond ever needs to
*draw* well without the file having to.

## Where sources violate this project's constraints

Worth recording, since the finding is read against them:

- **Integers only, no decimal point.** Almost everything holds — all money is integer across Element TD,
  Infinitode, Legion TD 2, Kingdom Rush, Dungeon Defenders and PvZ 2. The exceptions are presentation or
  combat: Infinitode's `requiredLevelsLabelPos` (`0.5`, a label position), Kingdom Rush's `range_factor` /
  `damage_factor` meta-progression multipliers, and **GemCraft's entire economy**, which is float end to end.
- **A new unit is a row, never a column.** Element TD, Legion TD 2 and Kingdom Rush all hold this. Dungeon
  Defenders does not — its ladder is parallel arrays (`TowerUpgradeCosts[]`) in the same archetype as the
  stats, which is what a non-branching design lets you get away with and this project cannot.
- **Reads well in a diff.** Infinitode's `links[]` is the best of the survey on this count: one line per edge,
  both endpoints named. Element TD's `Upgrades { "Count" "6" ... }` carries a hand-maintained count that must
  agree with the entries — exactly the kind of redundancy that rots under hand-editing.

## Open, and cheap to settle

- **Element TD 2's data format is not public.** No mod support, no readable data files; the reconverging
  *shape* is confirmed by Karawasa
  ([Steam](https://steamcommunity.com/app/1018830/discussions/0/3881598799638342855/)) but not the storage.
- **Gem TD's recipe table** is almost certainly in JASS triggers rather than object data — Warcraft 3's `UUPT`
  cannot express a multi-unit consume — and no deprotected source was found. Cite for design shape only.
- **Orcs Must Die and Sanctum 2** trap/tier costs were not located. Both are `[GUIDE]` only; the OMD premise of
  "one of two per tier" is wrong, and it is really a linear skull ladder plus an orthogonal 1-of-2 equippable
  slot.

## Sources

- [MNoya/Element-TD](https://github.com/MNoya/Element-TD) — `npc_units_custom.txt`, `mechanics/upgrades.lua`,
  `mechanics/sell.lua`, `panorama/scripts/tower_table.js`
- Infinitode 2 shipped APK `assets/res/researches.json`
  ([archive](https://archive.org/details/com.prineside.tdi2_1173_apps.evozi.com));
  [Prineside modding docs](https://infinitode.prineside.com/modding/?p=dev-mode-usage)
- [ModDota item KeyValues](https://moddota.com/abilities/item-keyvalues); shipped recipe rows in
  `Pizzalol/SpellLibrary` and `OpenAngelArena/oaa`
- [SteffenCarlsen/LegionTD2_Api](https://github.com/SteffenCarlsen/LegionTD2_Api) — `docs/UnitStats.md`;
  `attrib/legion2-builder` `GameDefs.kt`
- Kingdom Rush decompiled LuaJIT — [DanQZ/KR1-Smarter-Soldiers-Mod](https://github.com/DanQZ/KR1-Smarter-Soldiers-Mod),
  [DanQZ/KRF-Smarter-Soldiers-Mod](https://github.com/DanQZ/KRF-Smarter-Soldiers-Mod)
- [Mindustry](https://github.com/Anuken/Mindustry) — `core/src/mindustry/content/TechTree.java`
- [DD_NewSDK](https://github.com/butter124/DD_NewSDK) — `DD_UDKGame_classes.hpp`
- PvZ 2 decoded RTON `PLANTLEVELS.json` — `Kenny3-2/PVZ2-Modifications`
- [gemforce-team/wGemCombiner](https://github.com/gemforce-team/wGemCombiner) — `WGemCombiner/Gem.cs`
- Warcraft 3 object field ids — `SinZ163/w3x-to-vmf/object_ids.json`;
  [world-editor-tutorials unit editor reference](https://world-editor-tutorials.thehelper.net/uniteditor.php)
