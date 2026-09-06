# 0062 — A capstone costs a token

The top of a tower line is bought with a **capstone token**: one token, no gold. A run is granted one at
rounds 3, 6 and 9, so three tokens meet nine capstones and which line reaches its top is the decision. This
record is what the currency is, where the price is written down, what a stored run had to gain to replay one
— which turns out to be nothing — and what the change retires.

**The schedule and the one-token price are `docs/roster.md`'s and not this record's.** What is decided here is
everything around them.

## Where the price is written down, and which layout it went on

**`content/upgrades.txt` goes to layout 2, under the hash label `upgrade-ladder/2`.** The keyword a row opens
with says which currency buys the edge: `upgrade` is the target row's full gold price and `capstone` is one
token and no gold at all. Layout 1 keeps its own reader branch, its own label and the hash it always had, and
a `capstone` row in a layout-1 file is refused rather than read.

**A new layout even though the field count did not move.** A row is still a keyword and two ids, so `content/upgrades.txt`'s standing rule — *one edge per row and the arity is fixed at two ids* — is untouched.
What moved is what the **keyword column means**, and that is the same silent-misread class the column rule
exists for: two files whose keyword means two different things, both calling themselves layout 1, is a
difference nothing here could detect. So the price joins the fold from layout 2, the label moves with it, and
a ladder read through one branch cannot hash equal to a ladder read through the other. `content/units.txt`
argued the general form of this at [#268](https://github.com/ssalter21/tower-defense-game/issues/268); this is
the narrower case, where the count is stable and the meaning is not.

**A keyword rather than a third column, and rather than a leaf walk.** A third column would have widened every
row of the file to carry a price on eighteen edges of which nine have one. And *deriving* a capstone from the
ladder's shape — the top of a line is a unit with no outgoing edge — would make the price a property of what
somebody has authored *above* a row rather than of the row itself: adding a fourth rung would silently
re-price the third, and a line deliberately two rungs deep would silently make its second rung free. The file
says which edges cost a token, in the one place the simulation reads.

**What a token price cannot say is an amount**, and that is deliberate. `EdgePrice` names a currency; gold's
amount is the target row's own `cost` column and a token's amount is one. There is no column anywhere that
could make it two, so there is no number to keep in agreement with anything.

## What is not priced, and why nothing is authored to compensate

**A capstone is not priced by the damage rule, and `show-ladder` says so on seven of the nine.** The rule is
one gold per five damage a second times the bodies a shot hits, and its bodies term is the `targets` column.
Five capstones change neither term — Shield Wall, Blessing, Consecration, Overgrowth and Unravel are auras or
debuffs — so the rule prices each of them identically to the rung below. **Two more read flat for a second
reason**: Slam and Mortar spread one roll over a bubble, and a damage-bubble row must leave `targets` at 1
because a bubble is one shot drawing one roll, so the rule counts one body for a swing that hits everything
around the tower. That is the Mage's 92-against-30 one rung higher: a bubble's worth is a radius, and radius is
unpriced.

**Seven, not five, is what the tool prints**, and `docs/roster.md`'s cost section says both numbers and which
is which. **No premium is authored to compensate.** Scarcity is the grant schedule and not the price; a
capstone's `cost` column stays authored, stays read by the flat-price note, and is charged by nothing.

## Whether this is a format version — it is not

**No record format moved, and none had to.** A stored command stream already carries every input the token
needs:

| What a token balance is made of | Where the stream already has it |
|---|---|
| What the schedule granted | The `u16 wave` on every build phase |
| What a decision spent | The actions it stores, against the ladder it is stamped with |
| Which edges cost a token | The ladder hash in the header, checked by the replay gate |

So a stored count would be a **second copy of a derivation, free to disagree with the first** — which is the
argument `RecordCommand` already makes about a round's purse, board and field. `CommandStream.Check` folds the
balance forward beside the purse and the board, one round at a time, and refuses a stream that climbs a
capstone it could not have held a token for.

**Exactly rather than at a ceiling**, which is the one place this differs from the purse. The walk closes every
wave at `Purse.CloseWaveAtBest` because a wave's income depends on how the round played; nothing about a token
depends on how anything played, so the walk's count is the count and every refusal it makes is final.

**Reader branches: unchanged.** `RecordFormat.CommandVersion` stays 3 and `IsKnown` still names 0, 1, 2 and 3.
The one branch this change adds is the ladder's, and it is a content layout rather than a record format:
`UpgradeLadder.IsKnownLayout` names 1 and 2, layout 1 folds no price and hashes to what it always did, and
`UpgradeLadderTests` pins that value as a literal — because once `content/upgrades.txt` moved to layout 2 there
is no committed file left for the older branch to be compared against.

**`SimulationVersion` stays 13.** The fingerprint in `DerivationTests` is a fold over a match and a build
phase, and a build-phase *pricing* rule that no stored stream can reach without also failing the content stamp
is not a behaviour change any record needs protecting from: the ladder hash moved, the content hash moved with
it, and every stream recorded before this is retired at the gate.

**One rule is left uncovered by every hash in the repository, and it is named rather than fixed.** *How many*
tokens a round holds is a list in `sim/Run.cs`. Moving it would make refused streams legal and legal streams
refused with every stamp on every record still agreeing — the fingerprint cannot reach it, because the
scenario it is folded over has a ladder with no edges and therefore no capstone to buy. So the schedule is
pinned by an assertion in `DerivationTests` whose comment says what a change to it owes: a `SimulationVersion`
bump and a `BehaviourByVersion` row, taken deliberately. Widening the fingerprint's scenario to cover it would
be a bump for a rule that has not moved, which is the mistake that file's own remarks name.

## What the sending side keeps

`sim/BuildPhase.cs` asserts that **the purse is the only scarcity on the sending side**, and it still is. A
wave carries whatever it can afford, nothing has to be unlocked and no schedule bounds a slot. What gained a
second scarcity is the **tower** side, and only the top of a line: every other edge still pays the target
row's full gold price out of the one purse.

**This is deliberately not the gate mechanic deleted on 13 August.** No capacity schedule, no per-wave type
limit, no offering. The [14 August proposal](../decision-log.md) carried both halves and only the token half is
taken.

## What the scripted player does with a token

`CoverThenUpgradeBot` builds both walls in the balance report — the run's own and the canned stand-in's — so a
bot that could not spend a token would leave the whole mechanic unmeasured, and one that spent it badly would
report the roster as weaker than it is.

**A token is spent the round it arrives**, because there is nothing else to do with one: gold banked earns
interest and buys a creep instead, and a token earns nothing and buys one kind of thing.

**In its own loop, after the gold, and only when the capstone beats what it replaces.** A score per gold has no
denominator for a thing gold does not buy, so a capstone is not comparable with anything the gold loops weigh;
it is chosen by damage over the route per tick, which is that same score with the gold taken out. The
comparison against the **standing** row is the part that is not the gold loops' rule: there the cell is free or
the upgrade costs its difference, and here the capstone consumes what is under it — so a capstone that scores
below the rung it replaces would make a wall worse for a token.

**What that score cannot see is most of what a capstone is.** Five of the nine change no damage roll and no
body count, so the bot leaves their tokens unspent. That is the score's known blindness rather than a statement
about the roster, and the report says so by reporting a run that did not spend them.

**And on the committed content it spends none at all: `content/sweep.csv` came back byte-identical.** A
ten-round bot run from an empty board ends on fourteen towers and every one of them is a line's *first* rung —
soldiers, rogues, clerics and a druid — because the cover phase keeps finding a cheap root that watches route
nothing watches yet. No rung 2 stands, so no capstone edge is climbable, so all three tokens are held to the
end. The canned stand-in does reach a rung 2, and the capstone above it scores exactly what that rung scores,
which is what the clause above declines. **So the balance harness cannot currently see this mechanic**, in the
same structural way [ADR-0061](0061-a-kill-pays-the-defender.md) records the sweep being unable to see a
bounty. Nothing was softened to make that come out: it is the reading.

## What it costs

**One retirement, and it is deliberate.** `content/upgrades.txt` goes to **layout 2** under
`upgrade-ladder/2`, so the ladder hash goes `6C432E189630BF3C` → `A52476D83A039248` and the content hash it
folds into goes `CEAD5CE53790DD40` → `FAA0B1831B2CE190`. Every record stamped against the roster as it stood is
retired.

**The committed match is untouched by the mechanic.** It is a defense and a wave read off files, not a run, so
no build phase and no token is involved: 3 of 40 leaked, tick 5302, state `441D37E128517F3D`, the same four
landmarks. What moved is its header — the content stamp, and the defense and wave hashes that fold the roster's
content hash with them — and not one number the match produced.

**The committed run is untouched too**, and that is a fact about the script rather than about the rule: the
four rounds of `content/commands.txt` place archers and never climb a line, and the canned stand-in they are
fought against buys no capstone. Its outcome vector is identical, and so is `content/sweep.csv` — byte for
byte, over 136 runs.

## Where it lives

- `content/upgrades.txt` — layout 2, the two keywords, and the nine capstone rows.
- `sim/UpgradeLadder.cs` — `EdgePrice`, the layout-2 branch, `IsCapstoneEdge`, and the flat-price note's
  second sentence.
- `sim/Run.cs` — `CapstoneTokenRounds`, `CapstoneTokensGrantedThrough`, `CapstoneTokensGrantedAt`, and the
  count a round holds.
- `sim/BuildPhase.cs` — the token branch in `Applied`, the refusal, and `Build.CapstoneTokensSpent`.
- `sim/CommandStream.cs` — the fold in `Check`, which is why no byte moved.
- `sim/FieldPool.cs` — the stand-in on the same schedule.
- `sim/CoverThenUpgradeBot.cs` — `BestCapstone`, and the capstone left out of the gold half.
- `simcli/Ladder.cs` — what `show-ladder` prints on a capstone edge.
- `client/Assets/View/ComposedRound.cs` — the round's token count, and `CostsACapstoneToken`.
- `client/Assets/View/TowerPalette.cs` — `PriceOfRung`, which is what a rung's button says.
- `client/Assets/View/RosterNames.cs` — the player-facing wording for the currency.
