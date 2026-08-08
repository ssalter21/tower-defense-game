# The roster

**The design side of [`content/units.txt`](../content/units.txt).** That file holds the numbers the simulation
reads; this one holds what each unit is *for*, what it looks like, and what about it is still unsigned. Where a
number appears here it is a **proposal** until it appears there.

This is a working document. It is meant to be opened, argued with and edited every time new gameplay is
specified, so it is written line-by-line rather than as a wide table — a wide table is unreadable in a diff and
miserable to edit by hand.

## How to edit this

**One block per unit, the same six lines every time.** Leave a line blank when it is undecided. **A blank is not
an omission, it is the ask** — the blanks are the agenda for the next conversation.

| Line | What goes on it |
|---|---|
| `Does` | The mechanic, in the terms the simulation would have to implement |
| `Looks` | The art direction — model, silhouette, what reads at a glance |
| `Numbers` | Only what has actually been decided. `_` for what has not |
| `Needs` | What the schema or the engine would have to gain. `nothing` means it is authorable today |
| `Open` | The question that has to be settled before it can be signed |

**Status is one of four words.** `proposed` — written here and nowhere else. `signed` — the numbers are agreed.
`live` — there is a row in `content/units.txt`. `retired` — there was one, and there is not now.

**Ids come from `units.txt`'s one global space and ascend forever.** Never reused, never an index. **A tier is
its own id and its own row** — the Captain is not the Soldier with a flag set. Ids 1–10 are spent; proposals
below start at 11.

**Adding a unit is a block here and a row there. Adding a *lever* is neither** — a shape that needs something
the schema lacks is a finding for [`research/`](research/) first, then a schema decision, then a row. See
[#99](https://github.com/ssalter21/tower-defense-game/issues/99): a new unit is a row, a new column is a format
version and every stored record retired.

**Costs are not authored for creeps.** Every walking row is priced at effective health ÷ 160 and the number
follows. Signing a creep means signing its **health, speed and armour** — the price is arithmetic. See
[the note](https://github.com/ssalter21/tower-defense-game/blob/effort/first-playable/docs/research/cost-is-not-a-balance-lever-under-a-one-for-one-leak.md),
which lands on `main` with [#98](https://github.com/ssalter21/tower-defense-game/pull/98).

## The index

| id | unit | role | tier | status |
|---|---|---|---|---|
| 11 | Soldier | tower | 1 | proposed |
| 12 | Captain | tower | 2 | proposed |
| 13 | Hero | tower | 3 | proposed |
| 14 | Archer | tower | 1 | proposed |
| 15 | Ranger | tower | 2 | proposed |
| 16 | Marksman | tower | 3 | proposed |
| 17 | Mage | tower | 1 | proposed |
| 18 | Pyromancer | tower | 2a | proposed |
| 19 | Cryomancer | tower | 2b | proposed |
| 20 | Frostfire Archmage | tower | 3 | proposed |
| 21 | Minion | creep | — | proposed |
| 22 | Skeleton | creep | — | proposed |
| 23 | Skeleton Warrior | creep | — | proposed |
| 24 | Skeleton Scout | creep | — | proposed |
| 25 | Necromancer | creep | — | proposed |

---

# Towers

Three lines, three tiers each, and **one attack type per line** — which is what makes the three-way cycle
readable off the board: you know what a tower does to a body by knowing which line it came from. That is a
proposal, not a decision; see open question 3.

## The Soldier line — impact?

### 11 · Soldier · tier 1 · status proposed

- **Does** — one hex of range, single target, medium damage. Height does not change it.
- **Looks** — knight, full helm down, short sword.
- **Numbers** — range 1000. Everything else `_`.
- **Needs** — nothing. Authorable today.
- **Open** — a one-hex tower is a corridor-geometry unit, and the one-hex corridor goes away at seam 9. What is
  a melee tower once the board is a maze with width?

### 12 · Captain · tier 2 · status proposed

- **Does** — on first engagement and every 10 s after, for 5 s, raises the attack speed of every tower within
  2 hexes.
- **Looks** — adds a shield, visor open, particle effect while the aura is up.
- **Numbers** — radius 2000, period 10 s in ticks `_`, duration 5 s in ticks `_`, magnitude `_`.
- **Needs** — **a periodic aura**: a radius, a period, a duration, and a modifier applied to another unit's
  cooldown. The schema has none of these, and none of them is a unit stat — an aura is a thing a unit *emits*.
- **Open** — does "first engagement" mean the tower's first shot or the wave's first contact? Deterministic
  either way, but they are different rules.

### 13 · Hero · tier 3 · status proposed

- **Does** — attacks sweep 360°, hitting everything in range rather than one target.
- **Looks** — helm off, two-handed greatsword.
- **Numbers** — `_`
- **Needs** — **a shot that resolves against every body in a radius.** The mortar's splash is the nearest
  existing thing; whether this is "splash with radius = range" or a distinct shot shape is a design call.
- **Open** — an all-targets melee tower is the natural answer to the swarm. Is that the intended counter?

## The Archer line — pierce?

### 14 · Archer · tier 1 · status proposed

- **Does** — three hexes of range, high single-target damage, slow rate of fire.
- **Looks** — the ranger model.
- **Numbers** — range 3000. Carry-over candidate: the live `bolt` row is range 3200 / cooldown 6 / 90–150 and
  is the same shape.
- **Needs** — nothing. Authorable today.
- **Open** — "high damage and slow" and `bolt`'s "modest damage, fast" are different tunings of one silhouette.
  Which one is the line's identity?

### 15 · Ranger · tier 2 · status proposed

- **Does** — +1 hex of range.
- **Looks** — stands a block higher.
- **Numbers** — range 4000.
- **Needs** — nothing. Authorable today, and it is the only tier on this page that is purely a number.
- **Open** — a tier that is one stat is cheap to build and thin to play. Is that acceptable as the middle rung,
  or does it want a second clause?

### 16 · Marksman · tier 3 · status proposed

- **Does** — multishot: picks three targets in range per volley.
- **Looks** — fades slightly while shooting, to read as speed.
- **Numbers** — targets 3, range `_`, damage `_`.
- **Needs** — **a target count.** Target selection currently picks one body; this makes it pick *n*, and the
  tiebreak rule has to extend to an ordered *n*.
- **Open** — three shots at one damage each, or one shot split three ways? The first is far stronger into the
  swarm.

## The Mage line — magic?

### 17 · Mage · tier 1 · status proposed

- **Does** — arcane damage with splash of one additional hex.
- **Looks** — the mage, book in hand.
- **Numbers** — splash radius 1000.
- **Needs** — nothing new; `mortar` already delivers a splash shot.
- **Open** — none.

### 18 · Pyromancer · tier 2a · status proposed

- **Does** — the fire branch. Extra damage.
- **Looks** — red palette, fire particles.
- **Numbers** — `_`
- **Needs** — nothing, if "extra damage" is a bigger damage roll.
- **Open** — is the extra damage flat, or is it `bonusVsTag` against one armour type?

### 19 · Cryomancer · tier 2b · status proposed

- **Does** — the frost branch. Adds a slow to the splash area.
- **Looks** — blue palette, frost particles.
- **Numbers** — slow magnitude `_`, duration `_`.
- **Needs** — **a speed modifier with a duration.** Speed is a constant on the row today. A slow is the first
  thing in the game that changes a creep's stats mid-walk, and it has to be deterministic and order-independent
  when two of them land on the same tick.
- **Open** — does a slow stack, refresh, or take the strongest?

### 20 · Frostfire Archmage · tier 3 · status proposed

- **Does** — both branches at once, both stronger.
- **Looks** — fire and frost on the same attack.
- **Numbers** — `_`
- **Needs** — whatever 18 and 19 need.
- **Open** — **this makes the tier-2 element choice temporary.** If both roads end at the same tower, the pick
  is a tempo decision rather than a build decision. Is that the intent, or should one branch stay chosen?

---

# Creeps

**Creeps never attack.** `dmgMin`, `dmgMax` and `attack` are zero and `none` on every walking row, and the
Necromancer's aura is not an exception to that — it buffs, it does not deal damage.

**Every creep with a health pool carries one armour type from the fixed three-way cycle**, so "no armour" is
not available: `armourValue 0` means the type still applies, at zero points.

### 21 · Minion · status proposed

- **Does** — health and nothing else. The baseline body.
- **Looks** — the minion skin, no tools.
- **Numbers** — armourValue 0. Health `_`, speed `_`, armour type `_`.
- **Needs** — nothing.
- **Open** — **which armour type?** It is the only creep on this page whose type is not implied by its
  description, and it is the row every other row is read against.

### 22 · Skeleton · status proposed

- **Does** — some armour.
- **Looks** — the minion skin with shield and sword.
- **Numbers** — armour `armoured`. Health `_`, speed `_`, armourValue `_` (< the Warrior's).
- **Needs** — nothing.
- **Open** — "some armour" and the Warrior's "has armour" differ by a word. What else separates them?

### 23 · Skeleton Warrior · status proposed

- **Does** — armour.
- **Looks** — the warrior skeleton, full kit.
- **Numbers** — armour `armoured`. Health `_`, speed `_`, armourValue `_`.
- **Needs** — nothing.
- **Open** — see 22. Two armoured rows want a second axis between them — health, speed or price granularity.

### 24 · Skeleton Scout · status proposed

- **Does** — faster, no armour value.
- **Looks** — the rogue skeleton.
- **Numbers** — armour `swift`, armourValue 0. Health `_`, speed `_`.
- **Needs** — nothing.
- **Open** — **make the speed a whole multiple of the Minion's.** The live table's runner is exactly 2× the
  grunt on purpose: it is what makes two bodies level for exactly one tick and forces the target-selection
  tiebreak to be consulted. A merely-different speed silently deletes that coverage.

### 25 · Necromancer · status proposed

- **Does** — an aura granting surrounding creeps arcane hit points, spent before their health.
- **Looks** — the mage skeleton, staff, casting continuously. A large arcane bubble showing the radius.
- **Numbers** — armour `arcane`. Health `_`, speed `_`, radius `_`, shield points `_`.
- **Needs** — **an aura** (see the Captain, 12) and **a second health pool that absorbs first.** Damage today
  resolves into one pool. This is the largest engine ask on the page.
- **Open** — does the shield regenerate, decay, or persist until spent? And does it move with the Necromancer,
  so killing it strips every body around it?

---

## What is live today, and what happens to it

`content/units.txt` on `effort/first-playable` holds ten rows. **None of them survives this proposal by name.**

| live row | id | disposition under this proposal |
|---|---|---|
| grunt | 1 | → **Minion**. Same job. Rename, or retire and re-id |
| runner | 2 | → **Skeleton Scout**. Same job |
| bolt | 3 | → **Archer**. Same shape, different tuning |
| mortar | 4 | → **Mage**. Splash already matches |
| wisp | 5 | **unmatched** — the swarm. This roster has no swarm |
| bulwark | 6 | **unmatched** — the wall that walks. This roster has nothing above the Warrior |
| drifter | 7 | → **Skeleton** or **Warrior**, roughly |
| lancer | 8 | **unmatched** |
| sniper | 9 | **retired**, or → **Marksman** as a tier rather than a row |
| sieger | 10 | **retired** |

**Ids are never reused, so a rename is either a genuine rename of an existing row or ten new rows and ten
retirements.** A rename does not move the content hash — the label is for humans and error messages, and
nothing in the simulation branches on it — so **renaming grunt → Minion is free and does not retire a single
stored record.** Re-iding is not free. Prefer renaming wherever the job is the same.

## What this roster needs that the schema does not have

Six levers, in rough order of cost. Per [#99](https://github.com/ssalter21/tower-defense-game/issues/99) each
is a research finding and a schema decision before it is a column, and **none of them should become a column
without that.**

1. **An upgrade edge.** Nothing in `units.txt` says the Captain follows the Soldier. A tier ladder is an
   *edge*, and the file has only rows. The edge probably belongs beside the cost table in `ruleset.txt` — which
   would keep #99's rows-not-columns constraint intact — and the Mage's two-way tier 2 means it is a graph, not
   a list.
2. **A target count**, for the Marksman.
3. **A radial shot**, for the Hero.
4. **A timed speed modifier**, for the Cryomancer — the first thing that changes a creep mid-walk.
5. **A periodic aura** — radius, period, duration, modifier — for the Captain and the Necromancer. An aura is
   emitted rather than possessed, so it is not a unit stat.
6. **A second health pool that absorbs first**, for the Necromancer's shield.

**And one thing that is not a lever but is engine work: the purse has to buy the defense.** Tiers are the
answer to [#98](https://github.com/ssalter21/tower-defense-game/pull/98)'s first open question — today
`Run.Advance` takes the defense as a free argument and only creeps are charged, so there is one wallet because
there is only one thing to buy. **This roster gives the defensive side something to spend on**, which is what
story 17 ("underbuilding my defense to fund my offense *is* spending health") has been missing.

## Which pack is which side — *decided 8 August 2026*

**KayKit's Skeletons are the creeps and the Adventurers are the towers.** This reverses the reading held
earlier the same day, under which the evil-mirror units — the Skeletons plus the Black Knight from Mystery
Monthly Series 5 — were going to be the defending side.

It closes what had been the open question blocking the roster: the creep art source. And the pack's
construction now works for the game rather than against it — each skeleton was built as a specific adventurer's
deliberate twin, so **the two sides of the board are the two halves of one pack**, and a body reads against the
tower it is the shadow of. Quaternius's Ultimate Monsters stay rejected.

Anything the four skeleton models do not cover still needs a source in KayKit's register, and still needs
choosing.

## Open questions

1. **Sauce.** The currency is sauce and nothing here is food. A knight-and-necromancer register makes that
   sharper, not softer. Either sauce is a joke the game is not in on, or it is the seed of a fiction nobody has
   authored yet.
2. **Attack types are unassigned.** Soldier/Archer/Mage map onto impact/pierce/magic almost by construction and
   that fixes the live table's lopsidedness (two impact, one pierce, one magic), but the sheet does not say it.
   One line per type is a strong, legible rule — confirm or reject it.
3. **Five creeps is fewer than six, and six was already tight.** The offering puts ordinary options on every
   menu and [#91](https://github.com/ssalter21/tower-defense-game/issues/91) already had to cut `offering 3 3`
   to `offering 2 3` because the roster could not fill it. Five makes the draw more degenerate, not less. Nine
   is the number the anchor schedule's content bill implies.
4. **No swarm and no wall.** [#94](https://github.com/ssalter21/tower-defense-game/issues/94) asked for a
   swarm, and the live `wisp` (57 bodies for 400 sauce) and `bulwark` (8 bodies for the same) are the two ends
   of the granularity axis that makes a purse a decision. Nothing here occupies either end.
5. **Towers get nine states and creeps get five flat rows.** The attacking side is the player's half and the
   half the depth direction cares about. A roster that tiers the defense and not the offense is upside down
   relative to that. See [creep upgrade
   systems](research/creep-wave-variety-and-creep-upgrade-systems.md) — the question of whether creeps tier
   too was researched and never answered.
6. **No numbers.** Nothing here can be signed into `units.txt` until health, speed and armour exist for every
   creep and range, cooldown, damage and cost for every tower. That is what
   [#99](https://github.com/ssalter21/tower-defense-game/issues/99) is actually asking for, and it is what this
   document's blanks are.
