# The roster

**The design side of [`content/units.txt`](../content/units.txt).** That file holds the numbers the simulation
reads; this one holds what each unit is *for*, what it looks like, and what about it is still unsigned. Where a
number appears here it is a **proposal** until it appears there.

This is a working document. It is meant to be opened, argued with and edited every time new gameplay is
specified, so it is written line-by-line rather than as a wide table — a wide table is unreadable in a diff and
miserable to edit by hand.

> **Signed 8 August 2026 by [#102](https://github.com/ssalter21/tower-defense-game/issues/102), and live in
> `content/units.txt` since [#104](https://github.com/ssalter21/tower-defense-game/issues/104).** The eight
> signed rows, the five retirements, the currency rename and the regenerated goldens all landed on
> `effort/first-playable`. Every unit below marked `live` has a row you can read; everything still marked
> `proposed` is waiting on a lever the schema does not have, and those are listed under
> [what is deliberately absent](#what-is-deliberately-absent). What changed its mind on the way here is in
> [the decision log](decision-log.md#8-august-2026--the-roster-is-signed-and-the-clock-slows).

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
its own id and its own row** — the Captain is not the Soldier with a flag set.

**Ids are taken when a unit is built, never reserved.** An earlier draft of this document held 11–20 for the
tower tiers and 21–25 for the creeps. That reservation is abandoned: nine of those ids were being held for
units that may never exist, and a file whose gaps only make sense if you have read a document is exactly the
drift this roster exists to close. The next unit built takes id 15, whatever it is.

## What things cost

**Neither side of the purse is authored. Both are arithmetic**, and they are priced in the same quantity so
that one wallet can buy both — which is what [§3's one purse](vision.md#one-purse--restored-6-august-2026)
actually requires.

| | The rule | Which means |
|---|---|---|
| **A creep** | effective health ÷ 160 | You pay for the health a defense must spend to stop it. Effective health is the pool times the armour multiplier |
| **A tower** | one gold per **5 damage a second**, times the bodies a shot hits | You pay for the health it removes |

Signing a creep therefore means signing **health, speed and armour**; the price follows. Signing a tower means
signing **damage, cooldown and how many bodies it hits**.

**The tower rule was not invented for this document — it was already in three of the four live prices and
nobody had written it down.** Reading each live tower's damage per second, multiplied by the bodies it hits,
against the price it already carried:

| live tower | damage/sec × bodies | ÷ 5 | price it already had |
|---|---|---|---|
| `bolt` | 200 × 1 | 40 | **40** — exact |
| `sieger` | 500 × 3 | 300 | **300** — exact |
| `mortar` | 152.8 × 3 | 91.7 | **90** — within 2% |
| `sniper` | 333 × 1 | 67 | **200** — nowhere near |

*(Figures at [the current clock](#the-clock). The ratios are what matter and they do not move with it.)* Three
rows out of four were already priced on a rule nobody had stated. The one that was not is `sniper`, which is
retired below for an unrelated reason — and which misses precisely because it is the longest-ranged tower in
the game and **the rule does not price range.**

> ⚠️ **Two honest gaps in the tower rule.**
>
> **It does not price range.** A one-hex tower and an eight-hex tower with the same damage cost the same, which
> is precisely why `sniper` — range 8000, priced at three times what its damage implied — never obeyed it. The
> Soldier being cheap is therefore an accident of the formula agreeing with the design rather than the formula
> knowing that reach is worth paying for.
>
> **The constant is tied to the tick rate.** "Five damage a second" is a number about seconds, and
> [the clock](#the-clock) has moved once already. If it moves again, re-derive the constant or every tower
> silently stops being based.

**Capstones are expected to break the rule, downward.** The intended reward curve is a shallow U: efficient at
the bottom, dear in the middle, efficient again at the top. So a tier-3 tower should come in *under* what its
damage implies. **This is intent, not a clause** — no capstone exists yet, and a stated exemption with no
instance is a rule nobody can check, so `units.txt` states the rule plainly and says nothing about exceptions.
The first capstone is where it gets written down.

## The clock

**Thirty ticks a second.** Every duration in `units.txt` — cooldown, windup, backswing, flight, dying — is in
ticks, and speed is thousandths of a hex per tick. The board is a **47-hex corridor**.

**Everything slowed by three on 8 August 2026.** Durations ×3, creep speeds ÷3. Nothing else moved: damage,
health, range and every cost are exactly as they were, so the committed run resolves the same way over three
minutes instead of one.

| | Before | After |
|---|---|---|
| Minion walking speed | 2.55 hexes/sec | **0.84 hexes/sec** |
| Time to cross the board | 18 sec | **56 sec** |
| One wave | 61 sec | **~3 min** |
| Archer rate of fire | 5.0/sec | **1.67/sec** |
| Mage rate of fire | 1.7/sec | **0.56/sec** |

The reasoning, and the alternative that was rejected, are in
[the decision log](decision-log.md#what-a-uniform-dilation-costs-and-what-it-deliberately-does-not).

**Two speed relationships are load-bearing and survive the dilation.** The Scout walks at exactly **twice** the
Minion — 56 against 28 — so two bodies are level for exactly one tick as one passes the other, which is the
case the target-selection tiebreak exists for. The Necromancer at 33 and the Warrior at 18 are deliberately
*not* whole multiples, so a pass that lands between ticks exists as well. A merely-different speed silently
deletes one of those two cases.

## The index

| id | unit | role | tier | status | label in `units.txt` | was |
|---|---|---|---|---|---|---|
| 1 | Minion | creep | — | live | `minion` | `grunt` |
| 2 | Skeleton Scout | creep | — | live | `skeleton-scout` | `runner` |
| 3 | Archer | tower | 1 | live | `archer` | `bolt` |
| 4 | Mage | tower | 1 | live | `mage` | `mortar` |
| 7 | Necromancer | creep | — | live | `necromancer` | `drifter` |
| 11 | Soldier | tower | 1 | live | `soldier` | — new |
| 12 | Skeleton | creep | — | live | `skeleton` | — new |
| 13 | Skeleton Warrior | creep | — | live | `skeleton-warrior` | — new |
| 14 | Ranger | tower | 2 | live | `ranger` | — new |
| — | Captain | tower | 2 | proposed | — | |
| — | Pyromancer | tower | 2a | proposed | — | |
| — | Cryomancer | tower | 2b | proposed | — | |
| — | Hero | tower | 3 | proposed | — | |
| — | Marksman | tower | 3 | proposed | — | |
| — | Frostfire Archmage | tower | 3 | proposed | — | |
| 5 | ~~wisp~~ | creep | — | retired | — | see [below](#what-is-retired-and-why) |
| 6 | ~~bulwark~~ | creep | — | retired | — | |
| 8 | ~~lancer~~ | creep | — | retired | — | |
| 9 | ~~sniper~~ | tower | — | retired | — | |
| 10 | ~~sieger~~ | tower | — | retired | — | |

**The file interleaves towers and creeps, and it has gaps.** Ids ascend strictly down the file and ascend past
the roles, so two creeps sit below a tower and 5, 6, 8, 9 and 10 are permanently absent. Both are deliberate:
the order records what was decided when, and the gaps make the retirements visible instead of papering over
them. Grouping is what this document is for.

**Labels in `units.txt` are lowercase single tokens, so the two-word names are hyphenated there.** The parser
allows letters, digits, `-` and `_` and nothing else — a space would be two fields — so *Skeleton Warrior* is
`skeleton-warrior` on its row. The label is for people reading the file and for error messages; nothing in the
simulation branches on it, and renaming one moves no hash.

---

# Towers

Three lines, three tiers each, and **one attack type per line** — Soldier impact, Archer pierce, Mage magic.
*Decided, not proposed.* It is what makes the three-way cycle readable off the board: you know what a tower
does to a body by knowing which line it came from, and it costs nothing, because attack type is a column that
already exists. It also fixes the live table's lopsidedness — two impact, one pierce, one magic — by
construction rather than by tuning.

**Only the three tier-1 towers are authorable.** Every tier above them needs a lever the schema lacks, starting
with an upgrade edge; see [what the schema does not have](#what-this-roster-needs-that-the-schema-does-not-have).

## The Soldier line — impact

### 11 · Soldier · tier 1 · status live

- **Does** — one hex of range, single target, fast. Height does not change it.
- **Looks** — knight, full helm down, short sword.
- **Numbers** — range 1000, cooldown 15 (two swings a second), damage 60–90, windup 7, backswing 5, hitscan,
  impact, **cost 30**.
- **Needs** — nothing. Authorable today.
- **Open** — a one-hex tower is a corridor-geometry unit, and the one-hex corridor goes away at seam 9. What is
  a melee tower once the board is a maze with width?

> **Why it is the cheapest thing on the board.** 150 damage a second against the Archer's 200, at 30 gold
> against 40 — identical value per gold, with a third of the reach. You buy Soldiers because they are cheap,
> not because they are good, and the melee line earns its identity at tiers 2 and 3 rather than here.

### Captain · tier 2 · status proposed

- **Does** — on first engagement and every 10 s after, for 5 s, raises the attack speed of every tower within
  2 hexes.
- **Looks** — adds a shield, visor open, particle effect while the aura is up.
- **Numbers** — radius 2000. Period, duration and magnitude `_`.
- **Needs** — **a periodic aura**: a radius, a period, a duration, and a modifier applied to another unit's
  cooldown. The schema has none of these, and none of them is a unit stat — an aura is a thing a unit *emits*.
- **Open** — does "first engagement" mean the tower's first shot or the wave's first contact? Deterministic
  either way, but they are different rules. **And the period and duration are in seconds here** — at 30 ticks a
  second and after [the dilation](#the-clock), write them as ticks when they are signed.

### Hero · tier 3 · status proposed

- **Does** — attacks sweep 360°, hitting everything in range rather than one target.
- **Looks** — helm off, two-handed greatsword.
- **Numbers** — `_`
- **Needs** — **a shot that resolves against every body in a radius.** The Mage's splash is the nearest
  existing thing; whether this is "splash with radius = range" or a distinct shot shape is a design call.
- **Open** — it was the natural answer to the swarm, and [there is no swarm](#what-is-deliberately-absent). So
  what is it the answer to now?

## The Archer line — pierce

### 3 · Archer · tier 1 · status live

- **Does** — three hexes of range, modest damage, fast.
- **Looks** — the ranger model.
- **Numbers** — range 3200, cooldown 18, damage 90–150, windup 9, backswing 6, hitscan, pierce, **cost 40**.
- **Needs** — nothing. It is `bolt`, dilated and renamed.
- **Open** — none.

> **The line's identity is `bolt`'s, not the earlier draft's.** This document used to describe the Archer as
> "high single-target damage, slow rate of fire" — the opposite tuning of the same silhouette. `bolt`'s tuning
> won for three reasons: it makes the rename free, it keeps four of the six committed defense slots untouched,
> and **the line already has a home for slow-and-heavy — that is what the Marksman is.** Choosing the other
> reading would have retuned the tower the committed defense is mostly made of, to buy early what tier 3 gives
> anyway.

### 14 · Ranger · tier 2 · status live

- **Does** — +1 hex of range.
- **Looks** —
- **Numbers** — range 4200, and every other number the Archer's: cooldown 18, damage 90–150, windup 9,
  backswing 6, hitscan, pierce, **cost 40**.
- **Needs** — nothing. Authorable today, and it is the only tier on this page that is purely a number.
- **Open** — none. A tier that is one stat was the question, and the answer is yes, for now: it is the middle
  rung, it is what the upgrade edge was built to be able to state, and a second clause can be added to it
  later without moving its id.

> **It costs the same as the Archer, and that is the rule rather than a mistake.** A tower is priced at one gold
> per five damage a second times the bodies a shot hits, and **the rule does not price range** — so a tower that
> differs from the Archer in range alone prices identically to it. `./tools/show-ladder.ps1` prints a *flat or
> falling price* note against the `archer → ranger` edge for exactly this reason. It is a note and not a fault,
> nothing goes red, and there is nothing here for anybody to go and fix.
>
> The gap is [already written down beside the rule](#what-things-cost) and this leans on it rather than
> tripping over it. Two futures were named and neither gates this row: range may become an input to the cost
> algorithm, or the algorithm may be replaced by something derived from **many simulations rather than from a
> row's stats**. Both sit with the capstone exemption, and neither is this effort's.

### Marksman · tier 3 · status proposed

- **Does** — multishot: picks three targets in range per volley. **This is where the line's slow-and-heavy
  tuning lives.**
- **Looks** — fades slightly while shooting, to read as speed.
- **Numbers** — targets 3. Range and damage `_`.
- **Needs** — **a target count.** Target selection currently picks one body; this makes it pick *n*, and the
  tiebreak rule has to extend to an ordered *n*.
- **Open** — three shots at one damage each, or one shot split three ways? **Note the cost rule reads "times
  the bodies a shot hits", so the answer changes the price directly.**

## The Mage line — magic

### 4 · Mage · tier 1 · status live

- **Does** — magic damage with splash of one additional hex.
- **Looks** — the mage, book in hand.
- **Numbers** — range 4600, cooldown 54, damage 210–340, windup 21, backswing 15, projectile, flight 33,
  splash radius 1000, magic, **cost 92**.
- **Needs** — nothing new; it is `mortar`, dilated and renamed.
- **Open** — the cost assumes **three bodies under a splash**. That is a placeholder, not a measurement, and it
  is the single assumption the Mage's whole price rests on.

> **Two things about this row are changes and not carry-overs.** Its attack type moves **impact → magic** to
> obey the one-type-per-line rule, which is a value change and moves the content hash. And its price moves
> 90 → 92 so the cost rule holds exactly rather than nearly.
>
> ⚠️ **The attack-type change makes the committed defense stronger.** Magic hits Armoured for 140 where impact
> hit for 100, and most of the committed wave is Armoured. Fewer than thirteen of forty will leak. **That is
> measured before anything is retuned** — see [the tuning target](#the-tuning-target).

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
- **Needs** — **a speed modifier with a duration.** Speed is a constant on the row today. A slow is the first
  thing in the game that changes a creep's stats mid-walk, and it has to be deterministic and order-independent
  when two of them land on the same tick.
- **Open** — does a slow stack, refresh, or take the strongest?

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
> five creeps and it resolves itself the moment the roster grows. It was not worth distorting the fiction to
> fix now.

### 1 · Minion · status live

- **Does** — health and nothing else. The baseline body.
- **Looks** — the minion skin, no tools.
- **Numbers** — 1550 hp, speed 28, armoured, armourValue 0, dying 36, cost 10.
- **Needs** — nothing. It is `grunt`, at a third of the speed.
- **Open** — none.

**This is the row every other row is read against**, which is why nothing about it moved except the clock.
Re-baselining it would re-baseline every measurement in the sweep.

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
That was the open question the earlier draft left: "some armour" and "armour" differ by a word, and here they
differ by a number and a cadence.

### 2 · Skeleton Scout · status live

- **Does** — fast, no armour value.
- **Looks** — the rogue skeleton.
- **Numbers** — 1500 hp, speed 56, swift, armourValue 0, dying 36, cost 9.
- **Needs** — nothing. It is `runner`, at a third of the speed.
- **Open** — none.

**Exactly twice the Minion's speed, and that is load-bearing rather than tidy.** See [the clock](#the-clock).

### 7 · Necromancer · status live

- **Does** — walks. **The aura is not signed** — see below.
- **Looks** — the mage skeleton, staff, casting continuously. A large arcane bubble showing the radius.
- **Numbers** — 2400 hp, speed 33, arcane, armourValue 25, dying 36, cost 19.
- **Needs** — nothing *for the body*. The aura needs two levers the schema lacks.
- **Open** — does the shield regenerate, decay, or persist until spent? And does it move with the Necromancer,
  so killing it strips every body around it?

> **The row lands; the mechanic does not.** What goes into `units.txt` is `drifter` dilated and renamed — a
> walking arcane body. The aura granting surrounding creeps arcane hit points spent before their health needs
> **an aura** and **a second health pool that absorbs first**, which is the largest engine ask on this page.
> Until then the Necromancer is a creep that looks like it should do something and does not, and that is worth
> knowing when it appears on a menu.

---

## What is retired, and why

**Five live rows have no home under the signed roster and are deleted.** Ids are never reused, so 5, 6, 8, 9
and 10 stay empty forever.

| id | row | why |
|---|---|---|
| 5 | `wisp` | The swarm. 57 bodies for 400 gold — one end of the granularity axis. Out of scope with the roster at five creeps |
| 6 | `bulwark` | The wall. 8 bodies for the same 400 — the other end. Same reason |
| 8 | `lancer` | A swift heavy with no designed counterpart |
| 9 | `sniper` | **Magic, in a line that is now pierce.** One attack type per line retires it as written; it may return as the Marksman, which is a tier and needs an upgrade edge first |
| 10 | `sieger` | An impact projectile whose line's tier 3 is the Hero — a 360° melee sweep, which a slow siege shell is not |

**Nothing structural breaks.** `content/wave.txt` sends only types 1 and 2 and `content/defense.txt` places
only 3 and 4, so the committed run never referenced any of them. Stored bundles carry their own copy of the
unit table — `content/golden/defense-0.units` is still in the fifteen-column layout 1 and still replays — so
retiring a row invalidates no record; it leaves those bundles pinned to an older roster, which is exactly what
they are for.

## What is deliberately absent

**Recorded so it is not silently re-proposed.** These are not design rejections — they are shapes that were
wanted and are not being built yet, and each is blocked on art rather than on argument.

| shape | what it was for | what it needs |
|---|---|---|
| **A swarm** | The fine end of the granularity axis — many cheap bodies, so a purse is a decision about *shape* rather than a lookup. Asked for in [#94](https://github.com/ssalter21/tower-defense-game/issues/94) | A model. KayKit's four skeletons are spent on the five signed creeps |
| **A wall that walks** | The coarse end — a few very tough bodies, priced the same | A model |
| **A swift heavy** | Fast *and* durable, the shape `lancer` occupied without a design behind it | A model |

**Two consequences worth carrying.** With five creeps and `offering 3 3`, three fifths of the roster is on
every menu, so the draw is barely a draw — accepted deliberately rather than overlooked. And the Hero's 360°
sweep was proposed as the swarm's answer, so until a swarm exists it is answering a question nobody asked.

> **The offering is three, not two.** An earlier version of this document said
> [#91](https://github.com/ssalter21/tower-defense-game/issues/91) "already had to cut `offering 3 3` to
> `offering 2 3`". That was true of #91's own commit and stopped one commit early: `85fed39` put it **back to
> three** when the roster grew to ten, and three is what `content/ruleset.txt` says today.

## The tuning target

**Against the committed defense and wave, seventeen of forty creeps leak.** That is deliberate: a defense that
holds tells you nothing when it changes, and one that collapses tells you nothing either. A partial break makes
the leak count a number a person can watch. It was thirteen of forty before the signature.

**Measured, and the measurement moved something.** Three changes in this commit push on the leak, and the third
was not on the list:

| | Direction | Size |
|---|---|---|
| The Mage moves impact → magic | Defense stronger — magic hits Armoured for 140 where impact hit 100 | Large |
| Rounding in the dilated speeds | Either way | ~1% |
| **The clock dilated and `wave.txt`'s ticks did not** | Defense far weaker — the wave arrives three times faster than the towers now fire | **13 → 25 of 40** |

The third is what the "measure before you retune" rule was for. Left alone, `content/wave.txt` would have
compressed the whole wave threefold against the new clock, and the leak landed at **25 of 40** — outside the
quarter-to-half band. Multiplying every order tick in that file by three, which is the one lever the signature
authorised, brought it to **17 of 40**, and finishing the dilation in code brought it to **12 of 40**. No creep
row moved for any of it.

> **One part of the dilation could not reach content at all, and was finished in code later the same day.**
> The fifteen-tick release cadence inside a column is a simulation constant, not a number in `wave.txt`, so a
> column of ten still emptied over a hundred and fifty ticks while its units walked a third as far in them —
> leaving columns three times denser in space than they were. That was a real change in wave shape, and it
> could not be undone from content. It was undone in `Match.SpawnIntervalTicks`, fifteen to forty-five, at the
> price the constant carries: a simulation version bump to 2, which retires every record made under version 1.
> The bump was spent then rather than later because the only records that existed were this repository's own
> and all but the historical goldens are regenerable by one command. **With the cadence dilated the leak lands
> at 12 of 40**, back inside the quarter-to-half band and close to the 13 it sat at before the clock moved,
> which is what a change that promised to be pure time was always supposed to do.

## What this roster needs that the schema does not have

Six levers, in rough order of cost. Per [#99](https://github.com/ssalter21/tower-defense-game/issues/99) each
is a research finding and a schema decision before it is a column, and **none of them should become a column
without that.** A new unit is a row; a new column is a format version and every stored record made under the
old one retired.

1. **An upgrade edge.** Nothing in `units.txt` says the Captain follows the Soldier. A tier ladder is an
   *edge*, and the file has only rows. The edge probably belongs beside the cost table in `ruleset.txt` — which
   would keep #99's rows-not-columns constraint intact — and the Mage's two-way tier 2 means it is a graph, not
   a list. **This is what blocks every tower above tier 1**, so it is first by dependency as well as by cost.
2. **A target count**, for the Marksman.
3. **A radial shot**, for the Hero.
4. **A timed speed modifier**, for the Cryomancer — the first thing that changes a creep mid-walk.
5. **A periodic aura** — radius, period, duration, modifier — for the Captain and the Necromancer. An aura is
   emitted rather than possessed, so it is not a unit stat.
6. **A second health pool that absorbs first**, for the Necromancer's shield.

**And one thing that was not a lever but was engine work: the purse buys the defense now.** A run opens on an
empty board and a build phase pays for its placements and its upgrades out of the same wallet its wave comes
out of — see [ADR-0048](adr/0048-a-board-is-not-a-layout.md). So the tower costs above are *based* rather than
arbitrary and something spends them, which is what story 17 (*"underbuilding my defense to fund my offense
**is** spending health"*) was waiting on.

What is missing now is a measurement rather than a mechanism. The opening purse, the income curve and the
health pool were every one of them tuned against a six-tower defense the run was handed for free, and none of
the three has been measured against an empty opening board. `content/ruleset.txt`'s health block says what
that leaves open.

## Which pack is which side — *decided 8 August 2026*

**KayKit's Skeletons are the creeps and the Adventurers are the towers.** This reverses the reading held
earlier the same day, under which the evil-mirror units — the Skeletons plus the Black Knight from Mystery
Monthly Series 5 — were going to be the defending side.

It closes what had been the open question blocking the roster: the creep art source. And the pack's
construction now works for the game rather than against it — each skeleton was built as a specific adventurer's
deliberate twin, so **the two sides of the board are the two halves of one pack**, and a body reads against the
tower it is the shadow of. Quaternius's Ultimate Monsters stay rejected.

**The four skeleton models are exactly spent**: the Minion and the Skeleton share the minion skin, the Warrior
takes the warrior, the Scout the rogue and the Necromancer the mage. Anything beyond them —
[the three absent shapes](#what-is-deliberately-absent) first — needs a source in KayKit's register, and still
needs choosing.

## Open questions

The five that closed on 8 August 2026 are gone from this list rather than struck through; they are in
[the decision log](decision-log.md#8-august-2026--the-roster-is-signed-and-the-clock-slows) with their
reasoning. What is left:

1. **Every tower above tier 1 is blocked on the upgrade edge**, and the edge is a schema decision nobody has
   taken. Seven of the ten proposed towers are waiting on it.
2. **Towers get nine states and creeps get five flat rows.** The attacking side is the player's half and the
   half the depth direction cares about. A roster that tiers the defense and not the offense is upside down
   relative to that. See [creep upgrade
   systems](research/creep-wave-variety-and-creep-upgrade-systems.md) — the question of whether creeps tier
   too was researched and never answered, and scoping to five creeps has made it sharper rather than softer.
3. **The Soldier is a corridor unit.** One hex of range is a shape the one-hex board makes sensible, and seam 9
   takes that board away.
4. **The Mage's price rests on an unmeasured three.** Bodies-under-a-splash is the only number in the cost
   rule that was guessed, and it is a multiplier rather than a term.
5. **The three absent shapes need models**, and art is not chosen unattended.

