# The roster

**The design side of [`content/units.txt`](../content/units.txt).** That file holds the numbers the simulation
reads; this one holds what each unit is *for*, what it looks like, and what about it is still unsigned. Where a
number appears here it is a **proposal** until it appears there.

This is a working document. It is meant to be opened, argued with and edited every time new gameplay is
specified, so it is written line-by-line rather than as a wide table — a wide table is unreadable in a diff and
miserable to edit by hand.

## How to edit this

**One block per unit, the same five lines every time.** Leave a line blank when it is undecided. **A blank is
not an omission, it is the ask** — the blanks are the agenda for the next conversation.

| Line | What goes on it |
|---|---|
| `Does` | The mechanic, in the terms the simulation would have to implement |
| `Looks` | The art direction — model, silhouette, what reads at a glance |
| `Numbers` | Only what has actually been decided. `_` for what has not |
| `Needs` | What the schema or the engine would have to gain. `nothing` means it is authorable today |
| `Open` | The question that has to be settled before it can be signed |

**Status is one of four words.** `proposed` — written here and nowhere else. `signed` — the numbers are agreed.
`live` — there is a row in `content/units.txt`. `retired` — there was one, and there is not now.

**Ids come from `units.txt`'s one global space and ascend forever.** Never reused, never an index, never
reserved in advance — the next unit built takes id 15, whatever it is. **A tier is its own id and its own row**;
the Captain is not the Soldier with a flag set.

## What things cost

**Neither side of the purse is authored. Both are arithmetic**, and they are priced in the same quantity so
that one wallet can buy both — which is what [§3's one purse](vision.md#one-purse) actually requires.

| | The rule | Which means |
|---|---|---|
| **A creep** | effective health ÷ 160 | You pay for the health a defense must spend to stop it. Effective health is the pool times the armour multiplier |
| **A tower** | one gold per **5 damage a second**, times the bodies a shot hits | You pay for the health it removes |

Signing a creep therefore means signing **health, speed and armour**; the price follows. Signing a tower means
signing **damage, cooldown and how many bodies it hits**.

> ⚠️ **Two honest gaps in the tower rule.**
>
> **It does not price range.** A one-hex tower and an eight-hex tower with the same damage cost the same, so
> the Soldier being cheap is an accident of the formula agreeing with the design rather than the formula
> knowing that reach is worth paying for.
>
> **The constant is tied to the tick rate.** "Five damage a second" is a number about seconds. If
> [the clock](#the-clock) moves, re-derive the constant or every tower silently stops being based.

**The rule does not reach a capstone, because gold does not buy one.**
[A gate hands the player one token](vision.md#the-gates) at waves 3, 6 and 9, and the
token is the whole price: three capstones a run, and the decision is which tower line to spend one on rather
than what it costs. The cost column prices what gold buys, and the top of a line is not in it. That retires
the shallow-U exemption this section used to reserve — an exemption to a gold rule, for a thing gold does not
buy, is a clause about nothing.

**What that leaves this file owing is the tiers themselves**: a capstone per tower line, none of them a strict
upgrade on another, each strong enough that spending one of three tokens on it is a real decision — and a
statement of how a capstone's numbers get chosen, since the damage rule no longer chooses them. **No capstone
exists yet**, so this is intent rather than a clause: `units.txt` states the pricing rule plainly and says
nothing about exceptions, because a stated exemption with no instance is a rule nobody can check. The first
capstone is where it gets written down.

## The clock

**Thirty ticks a second.** Every duration in `units.txt` — cooldown, windup, backswing, flight, dying — is in
ticks, and speed is thousandths of a hex per tick. The board is a **51-hex corridor that folds and climbs
through three tiers**.

| | |
|---|---|
| Minion walking speed | 0.84 hexes/sec |
| Time to cross the board | 61 sec |
| One wave | ~3 min |
| Archer rate of fire | 1.67/sec |
| Mage rate of fire | 0.56/sec |

**Two speed relationships are load-bearing.** The Scout walks at exactly **twice** the Minion — 56 against 28 —
so two bodies are level for exactly one tick as one passes the other, which is the case the target-selection
tiebreak exists for. The Necromancer at 33 and the Warrior at 18 are deliberately *not* whole multiples, so a
pass that lands between ticks exists as well. A merely-different speed silently deletes one of those two cases.

## The index

| id | unit | role | tier | status | label in `units.txt` |
|---|---|---|---|---|---|
| 1 | Minion | creep | — | live | `minion` |
| 2 | Skeleton Scout | creep | — | live | `skeleton-scout` |
| 3 | Archer | tower | 1 | live | `archer` |
| 4 | Mage | tower | 1 | live | `mage` |
| 7 | Necromancer | creep | — | live | `necromancer` |
| 11 | Soldier | tower | 1 | live | `soldier` |
| 12 | Skeleton | creep | — | live | `skeleton` |
| 13 | Skeleton Warrior | creep | — | live | `skeleton-warrior` |
| 14 | Ranger | tower | 2 | live | `ranger` |
| — | Captain | tower | 2 | proposed | — |
| — | Pyromancer | tower | 2a | proposed | — |
| — | Cryomancer | tower | 2b | proposed | — |
| — | Hero | tower | 3 | proposed | — |
| — | Marksman | tower | 3 | proposed | — |
| — | Frostfire Archmage | tower | 3 | proposed | — |
| 5, 6, 8, 9, 10 | *retired* | — | — | — | see [below](#what-is-retired-and-why) |

**The file interleaves towers and creeps, and it has gaps.** Ids ascend strictly down the file and ascend past
the roles, so two creeps sit below a tower and 5, 6, 8, 9 and 10 are permanently absent. Both are deliberate:
the order records what was decided when, and the gaps make the retirements visible instead of papering over
them. Grouping is what this document is for.

**Labels in `units.txt` are lowercase single tokens, so the two-word names are hyphenated there.** The parser
allows letters, digits, `-` and `_` and nothing else — a space would be two fields. The label is for people
reading the file and for error messages; nothing in the simulation branches on it, and renaming one moves no
hash.

**The ladder that joins them is [`content/upgrades.txt`](../content/upgrades.txt)**, one `upgrade <from> <to>`
row per edge, printed by `./tools/show-ladder.ps1`. It holds one edge today: `archer → ranger`.

---

# Towers

> **A widening is proposed.** [The expansion proposal](roster-expansion-proposal.md) puts nine lines on this
> page, three stages each, and would retire the Captain, the Hero and the elemental branch below. Until Sam
> signs it, what follows is current.

Three lines, three tiers each, and **one attack type per line** — Soldier impact, Archer pierce, Mage magic.
It is what makes the three-way cycle readable off the board: you know what a tower does to a body by knowing
which line it came from, and it costs nothing, because attack type is a column that already exists.

**Every tier on this page is authorable as of layout 3 and playable as of #217**, which built the timed
effect three of them rest on; see [the column
list](#what-this-roster-needs-that-the-schema-does-not-have). What each one still needs is a name and a set of
signed numbers.

## The Soldier line — impact

### 11 · Soldier · tier 1 · status live

- **Does** — one hex of range, striking every creep touching him, fast. The adjacency floor means height never
  takes his neighbours away from him.
- **Looks** — knight, full helm down, short sword.
- **Numbers** — range 1000, cooldown 15 (two swings a second), damage 60–90, windup 7, backswing 5, hitscan,
  impact, **cost 30**.
- **Needs** — nothing. `bubbleRadius` and `bubbleOrigin` landed with layout 3, so the sweep is a bubble on
  himself with no period: radius 1000, origin `self`, affects `enemy`, payload `damage`. **It is not authored
  in `content/units.txt`** — the row there still fires one shot at one creep, because giving the Soldier his
  sweep is a design decision and a balance change rather than a schema one.
- **Answered** — he was the unit seam 9 was expected to retire, and he is kept instead. A tower that strikes
  everything touching it is the one tower whose whole value is positional, which is exactly what a fold is
  for.

> **Why it is the cheapest thing on the board.** 150 damage a second against the Archer's 200, at 30 gold
> against 40 — identical value per gold, with a third of the reach. You buy Soldiers because they are cheap,
> not because they are good, and the melee line earns its identity at tiers 2 and 3 rather than here.

### Captain · tier 2 · status proposed

- **Does** — on first engagement and every 10 s after, for 5 s, raises the attack speed of every tower within
  2 hexes.
- **Looks** — adds a shield, visor open, particle effect while the aura is up.
- **Numbers** — radius 2000. Period, duration and magnitude `_`.
- **Needs** — **nothing but numbers.** Layout 3 authors this row and #217 plays it: origin `self`, affects
  `friend`, payload `cooldown`, a negative magnitude, a period and a duration. Two Captains in range of one
  tower do not stack — the stronger magnitude wins and the timer refreshes — so the aura is a floor on attack
  speed rather than a multiplier on the count of them.
- **Open** — does "first engagement" mean the tower's first shot or the wave's first contact? Deterministic
  either way, but they are different rules. **And the period and duration are in seconds here** — at 30 ticks a
  second, write them as ticks when they are signed.

### Hero · tier 3 · status proposed

- **Does** — attacks sweep 360°, hitting everything in range rather than one target.
- **Looks** — helm off, two-handed greatsword.
- **Numbers** — `_`
- **Needs** — nothing. A shot that resolves against every body in a radius is a bubble with origin `self` and
  payload `damage`, which is the Soldier's sweep at a bigger radius — and the tick loop resolves that shape
  today. Whether the radius equals the range is still a design call.
- **Open** — it was the natural answer to the swarm, and [there is no swarm](#what-is-deliberately-absent). So
  what is it the answer to now?

## The Archer line — pierce

### 3 · Archer · tier 1 · status live

- **Does** — three hexes of range, modest damage, fast.
- **Looks** — the ranger model.
- **Numbers** — range 3200, cooldown 18, damage 90–150, windup 9, backswing 6, hitscan, pierce, **cost 40**.
- **Needs** — nothing.
- **Open** — none.

**The line's identity is fast-and-modest**, and the home for slow-and-heavy is the Marksman at tier 3. Four of
the six committed defense slots are Archers, so retuning this row moves most of what the golden trace measures.

### 14 · Ranger · tier 2 · status live

- **Does** — +1 hex of range.
- **Looks** — the Archer's model at **1.5 scale**. The rows are identical in everything but range, so size is
  what separates the rungs on sight.
- **Numbers** — range 4200, and every other number the Archer's: cooldown 18, damage 90–150, windup 9,
  backswing 6, hitscan, pierce, **cost 40**.
- **Needs** — nothing. It is the only tier on this page that is purely a number.
- **Open** — none. A tier that is one stat is the middle rung, and a second clause can be added to it later
  without moving its id.

> **It costs the same as the Archer, and that is the rule rather than a mistake.** A tower is priced at one gold
> per five damage a second times the bodies a shot hits, and **the rule does not price range** — so a tower that
> differs from the Archer in range alone prices identically to it. `./tools/show-ladder.ps1` prints a *flat or
> falling price* note against the `archer → ranger` edge for exactly this reason. It is a note and not a fault,
> nothing goes red, and there is nothing here for anybody to go and fix.
>
> The gap is [already written down beside the rule](#what-things-cost). Two futures were named and neither
> gates this row: range may become an input to the cost algorithm, or the algorithm may be replaced by
> something derived from **many simulations rather than from a row's stats**.

### Marksman · tier 3 · status proposed

- **Does** — multishot: picks three targets in range per volley. **This is where the line's slow-and-heavy
  tuning lives.**
- **Looks** — fades slightly while shooting, to read as speed.
- **Numbers** — targets 3. Range and damage `_`.
- **Needs** — nothing. `targets` landed with layout 3 and target selection answers an ordered *n* under the
  same total order it always answered one under.
- **Answered** — three shots at one damage each. `targets` of *n* fires *n* shots at *n* creeps and draws *n*
  damage rolls; one shot split *n* ways is the other shape, and it is a bubble. **The cost rule's bodies term
  reads this column**, so a Marksman is priced on arrival rather than at a single-target Archer's price.

## The Mage line — magic

### 4 · Mage · tier 1 · status live

- **Does** — magic damage with splash of one additional hex.
- **Looks** — the mage, book in hand.
- **Numbers** — range 4600, cooldown 54, damage 210–340, windup 21, backswing 15, projectile, flight 33,
  splash radius 1000, magic, **cost 92**.
- **Needs** — nothing. The splash is authorable as of layout 3: origin `target`, radius 1000, payload `damage`.
- **Open, and now visible in a test.** The cost rule's bodies term used to guess three from the delivery
  column; since #216 it reads `targets`, and the Mage's row fires one shot at one creep. **So the rule prices
  this row at 30 gold and it costs 92** — the difference is three bodies under a splash the simulation has
  never had. #216 authored no bubble here and moved no price, because either is a decision about what a Mage
  is; `ContentTests` pins both numbers so the question stands rather than being silently answered. Author the
  splash and accept an unpriced radius, reprice the row, or make it genuinely fire three shots.

### Pyromancer · tier 2a · status proposed

- **Does** — the fire branch. Extra damage.
- **Looks** — red palette, fire particles.
- **Numbers** — `_`
- **Needs** — nothing, if "extra damage" is a bigger damage roll.
- **Open** — is the extra damage flat, or is it `bonusVsTag` against one armour type?

### Cryomancer · tier 2b · status proposed

- **Does** — the frost branch. Adds a slow to the splash area.
- **Looks** — blue palette, frost particles.
- **Numbers** — slow magnitude `_`, duration `_`.
- **Needs** — **nothing but numbers.** She is a bubble of radius 0, origin `target`, affects `enemy`, payload
  `speed`, a negative magnitude and a positive duration — no dedicated slow columns exist and none are coming.
  #217 built the timed effect behind it, so the row plays.
- **Open** — none about the mechanic. A slow does not stack: strongest-wins with the timer refreshed, which is
  what [#217](decision-log.md#16-august-2026-last--a-stat-can-move-while-a-match-is-running-and-a-floor-stops-that-ending-it)
  made true. **A creep never drops below a tenth of its authored speed**, so a magnitude past -90 buys nothing
  and the deepest useful slow is bounded by a rule rather than by taste.

### Frostfire Archmage · tier 3 · status proposed

- **Does** — both branches at once, both stronger.
- **Looks** — fire and frost on the same attack.
- **Numbers** — `_`
- **Needs** — whatever the Pyromancer and Cryomancer need.
- **Open** — **this makes the tier-2 element choice temporary.** If both roads end at the same tower, the pick
  is a tempo decision rather than a build decision. Is that the intent, or should one branch stay chosen?

---

# Creeps

**Creeps never attack.** `dmgMin`, `dmgMax` and `attack` are zero and `none` on every walking row, and the
Necromancer's aura is not an exception to that — it buffs, it does not deal damage.

**Every creep with a health pool carries one armour type from the fixed three-way cycle**, so "no armour" is
not available: `armourValue 0` means the type still applies, at zero points.

**The five signed rows, in full:**

| id | name | maxHp | speed | armour | armourValue | dying | effective hp | cost |
|---|---|---|---|---|---|---|---|---|
| 1 | Minion | 1550 | 28 | armoured | 0 | 36 | 1550 | **10** |
| 2 | Skeleton Scout | 1500 | 56 | swift | 0 | 36 | 1500 | **9** |
| 7 | Necromancer | 2400 | 33 | arcane | 25 | 36 | 3000 | **19** |
| 12 | Skeleton | 2200 | 28 | armoured | 20 | 36 | 2640 | **17** |
| 13 | Skeleton Warrior | 3400 | 18 | armoured | 45 | 48 | 4930 | **31** |

> **The Skeleton's price is the one rounding decision on the page.** 2640 ÷ 160 is exactly **16.5** — the only
> dead tie among the five — and it is resolved **upward to 17**. Every other row lands clearly on one side.
> Recorded because a tie is the kind of thing someone recomputes later, reads as an error, and silently
> "corrects" in the other direction.

> ⚠️ **Three armoured, one swift, one arcane.** Two of the three matrix columns have a single occupant, so the
> type chart is barely exercised by the roster you can actually send — and the committed wave sends only the
> Minion and the Scout, so arcane never appears in the golden run at all. This is a consequence of scoping to
> five creeps and it resolves itself the moment the roster grows.

### 1 · Minion · status live

- **Does** — health and nothing else. The baseline body.
- **Looks** — the minion skin, no tools.
- **Numbers** — 1550 hp, speed 28, armoured, armourValue 0, dying 36, cost 10.
- **Needs** — nothing.
- **Open** — none.

**This is the row every other row is read against**, which is why nothing about it moves. Re-baselining it
would re-baseline every measurement in the sweep.

### 12 · Skeleton · status live

- **Does** — the Minion with a little armour. The low rung of the armoured ladder.
- **Looks** — the minion skin with shield and sword.
- **Numbers** — 2200 hp, speed 28, armoured, armourValue 20, dying 36, cost 17.
- **Needs** — nothing.
- **Open** — none, but see below.

> **This is knowingly the dullest row on the page** — the Minion at the same speed with a bigger pool and some
> armour. Speed is the only lever that would fix it, and moving it off 28 breaks the whole-multiple
> relationship with the Scout that makes the target-selection tiebreak get consulted at all. A boring middle
> rung was judged cheaper than deleting a test.

### 13 · Skeleton Warrior · status live

- **Does** — slow and genuinely armoured. The heavy.
- **Looks** — the warrior skeleton, full kit.
- **Numbers** — 3400 hp, speed 18, armoured, armourValue 45, dying 48, cost 31.
- **Needs** — nothing.
- **Open** — none.

**What separates it from the Skeleton is two axes, not one** — armour 45 against 20, and speed 18 against 28.

### 2 · Skeleton Scout · status live

- **Does** — fast, no armour value.
- **Looks** — the rogue skeleton.
- **Numbers** — 1500 hp, speed 56, swift, armourValue 0, dying 36, cost 9.
- **Needs** — nothing.
- **Open** — none.

**Exactly twice the Minion's speed, and that is load-bearing rather than tidy.** See [the clock](#the-clock).

### 7 · Necromancer · status live

- **Does** — walks. **The aura is not signed** — see below.
- **Looks** — the mage skeleton, staff, casting continuously. A large arcane bubble showing the radius.
- **Numbers** — 2400 hp, speed 33, arcane, armourValue 25, dying 36, cost 19.
- **Needs** — nothing but numbers. Origin `self`, affects `friend`, payload `shield`, a period and a
  duration, measured in hex distance so it reaches the neighbouring leg of a fold. #217 built the effect state
  behind it.
- **Open** — the magnitude and the period, and a signature on three rules #217 had to pick to build anything
  at all. What it built: the granted pool **persists until spent or until its duration ends**, whichever
  comes first, with a duration of zero meaning until spent; it does **not** move with the Necromancer, so
  killing one stops the pulses and what is already granted is spent or times out rather than vanishing; and
  the magnitude is **a share of the health it stands in front of**, because a pool has no rate of its own for
  a percentage to be a percentage of. All three are the implementer's reading rather than a decision — an
  implementation cannot leave a column blank — and any of them can be moved without another format version.
  See [ADR-0056](adr/0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md).

> **The row lands and the mechanic is built; what is missing is the numbers.** What is in `units.txt` is a
> walking arcane body with no bubble on it. The second health pool arrived with layout 3 — `shield`, absorbing
> first and raw — the columns that describe the aura came with it, and #217 built the machinery that grants
> one unit's pool from another unit's bubble over a duration. Signing the radius, the period, the magnitude
> and the duration is a gameplay decision and nobody has taken it.

---

## What is retired, and why

**Ids are never reused, so 5, 6, 8, 9 and 10 stay empty forever.**

| id | row | why |
|---|---|---|
| 5 | `wisp` | The swarm. 57 bodies for 400 gold — one end of the granularity axis. Out of scope with the roster at five creeps |
| 6 | `bulwark` | The wall. 8 bodies for the same 400 — the other end. Same reason |
| 8 | `lancer` | A swift heavy with no designed counterpart |
| 9 | `sniper` | **Magic, in a line that is now pierce.** One attack type per line retires it as written; it may return as the Marksman |
| 10 | `sieger` | An impact projectile whose line's tier 3 is the Hero — a 360° melee sweep, which a slow siege shell is not |

**Nothing structural breaks.** Stored bundles carry their own copy of the unit table —
`content/golden/defense-0.units` is still in the fifteen-column layout 1 and still replays — so retiring a row
invalidates no record; it leaves those bundles pinned to an older roster, which is exactly what they are for.

## What is deliberately absent

> **Candidates for all three are named in [the expansion proposal](roster-expansion-proposal.md)**, since
> 5 September 2026, along with six further tower lines and eight further creeps. It is a proposal: this table
> stands until a row below it is signed.

**Recorded so it is not silently re-proposed.** These are not design rejections — they are shapes that were
wanted and are not being built yet, and each is blocked on art rather than on argument.

| shape | what it was for | what it needs |
|---|---|---|
| **Fast and cheap, in numbers** | The fine end of the granularity axis — many light bodies, so a purse is a decision about *shape* rather than a lookup | A model. The Skeleton Golem and the pack's own Necromancer are the two unassigned |
| **Slow, dear and very tough** | The coarse end — a few heavy bodies, priced the same | A model |
| **Fast and durable at once** | The pairing `lancer` occupied without a design behind it | A model |

> **These are named by their levers on purpose.** *Swarm* and *wall* were the words until 13 August 2026 and
> they are rejected: speed, health and armour are the levers, and the two ends of the granularity axis are
> just the ends of it. A category name invites a category the schema does not have. Same reasoning as
> [§12's *ordinary* and *game changer*](vision.md).

**Two consequences worth carrying.** With five creeps and `offering 3 3`, three fifths of the roster is on
every menu, so the draw is barely a draw — accepted deliberately rather than overlooked. And the Hero's 360°
sweep was proposed as the swarm's answer, so until a swarm exists it is answering a question nobody asked.

## The tuning target

**Against the committed defense and wave, twelve of forty creeps leak.** That is deliberate: a defense that
holds tells you nothing when it changes, and one that collapses tells you nothing either. A partial break makes
the leak count a number a person can watch, and the band to keep it inside is a quarter to a half.

**Measure before you retune.** Two changes have moved this number without any creep row moving — an attack type
changing line, and the clock dilating while `wave.txt`'s order ticks did not. Both were found by running the
match rather than by reading the spreadsheet, and one of them — the release cadence inside a column, a
simulation constant rather than a content number — could not be fixed from content at all.

## Which pack is which side

**KayKit's Skeletons are the creeps and the Adventurers are the towers.** Each skeleton was built as a specific
adventurer's deliberate twin, so **the two sides of the board are the two halves of one pack**, and a body reads
against the tower it is the shadow of. Quaternius's Ultimate Monsters are rejected.

**The pack holds six models and four are assigned**: the Minion and the Skeleton share the minion skin, the
Warrior takes the warrior, the Scout the rogue and the Necromancer the mage. The Minion and the Skeleton
sharing is a **kit variation and not a shortage** — the Skeleton is that model with shield and sword, and the
pack ships the weapons for it.

The two not named above are a dedicated **Necromancer** and a **Skeleton Golem**, the second of which the
publisher sells as a boss; the [collection inventory](research/kaykit-collection-inventory.md) counts all six.
The Necromancer keeps **`Skeleton_Mage`**, and the dedicated Necromancer model is left unused, which is a
choice and not an oversight.

### The assignments are signed

The complete collection — 22 packs, CC0, 61 rigged characters, 159 clips — is on disk and catalogued from the
archive itself in [the collection inventory](research/kaykit-collection-inventory.md). The assignments above
are **adopted as written** rather than left as a plan, and they were adopted by a person.

**The collection is extracted, and the assignments are on screen.** It sits at
`~/repos/kaykit-collection/`, beside the checkout rather than inside it, and the seven character models the
nine units need are imported into `client/Assets/Art/Characters/` — see
[§1](research/kaykit-collection-inventory.md#1-where-it-is-and-what-it-costs-to-keep) and
[§10](research/kaykit-collection-inventory.md#10-already-imported). Nothing in the project reads that folder;
imports are copied out of it by hand.

> ⚠️ **The one thing on the board that is not signed is the bow.** The models are per unit type; the *weapon*
> is still per delivery, so `bow_withString` goes into the hand of whichever row is a `projectile` — and the
> Mage is the only one. **So the Mage draws a bow rather than holding a book**, and the Archer and the Ranger,
> being hitscan, hold nothing and stand in their bind pose.
>
> **This was not chosen; it is what the delivery rule already did**, showing through now that the models are
> right. Fixing it is three decisions nobody has made and none of them is on this page: which prop the Mage
> holds, which prop the Soldier and the two archers hold, and which clips a hitscan tower is posed by — the
> ranged bank has bow and magic sets, the melee bank a `Melee_1H_*` set, and picking between them is an art
> decision. Recorded here rather than guessed at.

**Size is the tier signal, and it is the only one.** Three multipliers, applied to the model as it is drawn:

| What | Scale | Why |
|---|---|---|
| Towers | **1.0** | the baseline everything else is read against |
| Every creep | **0.5** | a creep is unmistakably smaller than the thing shooting it, at any camera angle |
| Ranger (14) | **1.5** | the Archer's upgrade shares its model, so nothing but size separates the rungs |

**Scale lives in `MatchArt` and never in `content/units.txt`.** Visual size is a view fact under
[ADR-0007](adr/0007-snapshot-is-the-only-view-input.md), and a column in the content tables would make every
art tweak cost a format version and a re-record. These numbers are expected to move once somebody has looked
at them, which is the whole reason they are stored somewhere free to change.

**Measured, rather than assumed.** With those multipliers the tallest body on the board is 1.40 m and the
shortest tower 2.45 m, so a creep is a little over half the height of the thing shooting it. An edit-mode
test measures both off the geometry and fails if the gap closes to within a fifth, because comparing the two
multipliers would prove nothing — a half applied to a taller model is not smaller than a one applied to a
shorter one, and the creeps and the towers come from two different packs.

**There is no plinth, and no rule about which units are people and which are buildings.** That distinction was
considered and dropped: it is not a thing this page needs to have an opinion about.

## What this roster needs that the schema does not have

**Decided, fixed as a list, and built.** [#213](https://github.com/ssalter21/tower-defense-game/issues/213)
fixed the list; [#216](https://github.com/ssalter21/tower-defense-game/issues/216) landed it, and
`content/units.txt` is layout 3 as of 16 August 2026. **The schema does not lack these any more** — the
section title is kept because the table below is what every block above points at. The five levers became
**nine columns**, and three of the five collapsed into one mechanic, because a sweep, a blast and an aura are
all the same shape: a bubble that emits something. The reasoning is
[ADR-0055](adr/0055-a-sweep-a-blast-and-an-aura-are-one-bubble.md).

| Column | Meaning |
|---|---|
| `shield` | A pool that absorbs first and raw. Armour does not apply to it, overkill carries through to health, and it does not regenerate. 0 = none |
| `targets` | Shots per attack, each its own damage roll, targets taken nearest-to-exit first. 1 = an ordinary single shot |
| `bubbleRadius` | Milli-hex, read as a sphere. 0 = the target alone; absent = no bubble |
| `bubbleOrigin` | `self` or `target`. The Soldier's sweep centres on the tower; a mortar's blast centres on what it hit |
| `bubbleAffects` | `friend` or `enemy` — and which units that is depends on the emitter's role, because a tower's enemy is what walks and a walker's enemy is what stands |
| `bubblePeriod` | Ticks. 0 = fires with the attack; positive = pulses on its own, which is what makes it an aura. An aura is centred on `self` and may not carry `damage` |
| `bubblePayload` | `damage`, or one of the modifiable stats — speed, cooldown, armour, shield. **Range is not modifiable**, because it would force coverage back into the tick loop. Damage is not modifiable *today* either, because the keyword is taken by the roll a damage bubble spreads — a narrowing of the signed column list, and [an open question](open-questions.md) rather than a decision |
| `bubbleMagnitude` | A percentage. A shield is a share of the health it stands in front of, and may not be negative. The signed table also allowed **a flat damage amount**, which nothing implements — same open question |
| `bubbleDuration` | Ticks. 0 = instant, and for a shield it means "until spent" |

**What that authors.** The Cryomancer needs no dedicated slow columns at all: she is a bubble of radius 0,
origin `target`, payload `speed`, negative magnitude, positive duration. The Captain is the same mechanic with
a period and origin `self`. A mortar is origin `target` with a real radius and payload `damage`. The
Necromancer grants shield to the creeps around it — origin `self`, affects `friend`, payload `shield` —
measured in hex distance rather than along the marching column, so it reaches the neighbouring leg of a fold. A
tower that pulses over the whole board is one row.

**Effects are one model**: a stat, a magnitude and a duration. Strongest-wins with the timer refreshed on
reapplication, resolved by a strict total order so that two effects landing in either order reach the same
state. An effect is in force for exactly its duration of ticks after the one it landed on. A creep never drops
below **10% of its authored speed** — a floor binding every effect at once, which is what makes a match that
cannot end unreachable by arithmetic rather than by careful authoring. Built in #217; the reasoning is
[ADR-0056](adr/0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md).

**The columns landed one ticket ahead of the machinery, deliberately.** #216 built the three columns the tick
loop could read on its own — `shield`, `targets`, and a damage bubble that fires with the attack and lands
instantly — and a row authoring anything else parsed, folded, stored and refused by name the moment a match
was built out of it. #217 built the rest and deleted that refusal, so **every shape these nine columns can
author now plays**. What is left for all six proposed towers is a name and a set of signed numbers.

A new unit is still a row, and a new column is still a format version with every stored record made under the
old one retired. **This is the last widening the roster asks for before the map has been measured.**

**The upgrade edge is not on this list.** It is [`content/upgrades.txt`](../content/upgrades.txt), a file of its
own rather than a column, and the reasoning is in
[ADR-0043](adr/0043-a-tier-is-its-own-id-and-its-own-row.md) through
[ADR-0046](adr/0046-an-absent-ladder-folds-nothing.md).

**And the purse buys the defense.** A run opens on an empty board and a build phase pays for its placements and
its upgrades out of the same wallet its wave comes out of — see
[ADR-0048](adr/0048-a-board-is-not-a-layout.md). So the tower costs above are *based* rather than arbitrary and
something spends them.

What is missing now is a measurement rather than a mechanism. The opening purse, the income curve and the
health pool were every one of them tuned against a six-tower defense the run was handed for free, and **the
income is the one of the three that has since been measured against an empty opening board** — 168 gold a wave
rather than a hundred, because a run pays for its wall and its wave out of that one row. The purse and the
pool are still where the free defense left them; `content/ruleset.txt`'s health block says what that leaves
open.

## Open questions

1. **Layout 3 and its machinery have both landed, and the queue is now a design queue rather than a schema
   one.** Every one of the six proposed towers is *authorable* and *playable* — the columns exist, the
   fixtures prove each shape [parses](../sim.tests/ContentTests.cs) and [plays](../sim.tests/EffectTests.cs).
   What is left is what was always left: naming them and signing their numbers. The ladder itself is built.
2. **Towers get nine states and creeps get five flat rows** — and the answer, from
   [13 August](decision-log.md#13-august-2026-later--the-gates-come-out-and-the-client-comes-before-the-roster),
   is that **creeps deepen by being upgraded rather than by being replaced**: stat and speed upgrades on the
   rows that exist, not new unit types. An **arcane shield** is expected and is two things at once — a pool a
   creep carries in its own right, and a pool the Necromancer grants to creeps entering its range that would
   not otherwise have one. The second half is the aura in
   [what this roster needs](#what-this-roster-needs-that-the-schema-does-not-have). See [what the creep-variety
   survey found](open-questions.md#what-the-design-research-found). **Creeps get no prerequisite chain** —
   the gating came out precisely because it held back testing, and a chain on the sending side puts a version
   of it straight back.
3. **The Soldier keeps his hex.** Answered by [#213](https://github.com/ssalter21/tower-defense-game/issues/213): one hex of range, plus a self-centred bubble,
   so his swing strikes every creep touching him rather than one of them. A corner placement inside a fold
   reaches two legs at once — a positional value the flat corridor could not offer, and the reason he was kept
   rather than retired.
4. **The Mage's price rests on a splash nobody has authored, and the guess propping it up is gone.** The cost
   rule's bodies term read three off the delivery column until
   [#216](https://github.com/ssalter21/tower-defense-game/issues/216); it reads `targets` now, and the Mage's
   row fires one shot at one creep. **The rule prices it at 30 gold and it costs 92.** Author the splash as a
   bubble and accept that a radius is unpriced, reprice the row, or make it genuinely fire three shots — three
   different towers, and picking one is a signature rather than a fix.
5. **The three absent shapes need models**, and art is not chosen unattended.
