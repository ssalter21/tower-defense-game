# 0052 — A creep is bought once, a phase names its whole wave, and the purse is charged the increase

A build phase composed a wave out of its own slots and paid for all of it, every round. So a run's seventh wave
could be smaller than its sixth, an early purchase bought one round of leak cost and nothing after it, and the
attacking half of the game had no compounding in it at all — the one thing every deep send system in the genre
is built on ([the survey](../research/attack-composition-and-sending.md#11-the-one-purse-problem-stated-precisely)).
Found on 14 August 2026 by playing it. This is how permanence was put in without giving the record a second
shape to carry.

## What was decided

**A creep is bought once and attacks every round after.** What a round fields is every purchase the run has
ever made. There is no selling one back and no leaving one at home, so a wave may only grow and a bad early
purchase is a lasting commitment.

**A phase's slots are the whole of its round's wave, and the purse is charged only the increase.** The
alternative — store the round's *additions* and let `Run` fold the wave up from every phase before it — is
fewer bytes and was rejected, because **the release order is a decision the round makes over creeps it did not
buy**. A record holding only the new ones could not say what the round actually sent, and reordering the
carried half would have had nowhere to live. So `BuildPhase.Resolve` takes what the round carries, prices each
slot at `count − carried(type)`, and refuses any slot below its floor.

**The refusal and the pricing are one rule read twice.** What cannot be given up is exactly what is not charged
for again. Both spellings of taking a creep back — a smaller count, and a slot dropped altogether — name the
type and both counts, because a player reading "invalid build phase" learns nothing they can act on.

**`Run.Carrying` is the last round's wave, not a tally beside it.** Every round already sends everything the
rounds before it bought, so the last round's wave *is* the running total. A second accumulator would have been
a derivation free to disagree with the record.

**The view opens each build phase holding the carried slots**, and asks before it offers: `CanSendFewer` is
false at the floor and a carried box cannot be emptied. Legality still comes from resolving a candidate and
discarding the `Build` — ADR-0051's prevention, with no second copy of the rule on the client.

## Why not the alternatives

**Charge for the wave every round and call permanence a bigger purse.** It keeps `Resolve` self-contained and
gets the compounding wrong: the point of a permanent buy is that early gold becomes a *stream*, and a rebought
wave makes it a rent.

**Let a round drop a creep for part of its price back.** A re-optimisable loadout rather than an accumulating
commitment. Rejected on 14 August 2026 with the rule above: mistakes are supposed to stick.

**Freeze the carried creeps in purchase order and let a round arrange only what it bought.** Simpler, and it
makes #197's drag decorative for nine rounds out of ten. Rejected the same day.

## Consequences

**Simulation version 4, and every record made under 3 is retired** (ADR-0009). The record *format* does not
move — a phase still stores slots — so no format version bumps and no reader branch is added.

**It caught a hole in the behaviour fingerprint, the second one that table has had.** Under
`rule-fingerprint/2` this build's fingerprint came out byte for byte version 3's, because both halves of that
fold resolve a phase carrying nothing and a phase carrying nothing prices exactly as it did before. A row whose
evidence equals its predecessor's is not evidence. The fold gained a third half — a composition against a
carried wave, folding what it *cost* — and the label went to `rule-fingerprint/3`.

**The committed run had to be re-authored monotone**, and the wave lines get longer and never shorter. The
numbers that re-authoring produced — leak cost dealt from 261 over ten rounds to 1757, waves six to eight
costing 19 to 34 gold apiece — belonged to a ten-round run against a stand-in that never grew, and
[#208](https://github.com/ssalter21/tower-defense-game/issues/208) has since taken both away: the file is four
rounds and deals 229. **What this decision claims is unaffected.** A round still names its whole wave, is still
charged the increase, and the file still only grows: round three names the ten runners round two bought and
pays for the ten it adds.

**The run ended rich, and that was the stand-in showing through** — the field was one stored round drawn ten
times, so the pressure coming back was flat while the run's own wave compounded. **#208 closed that**: the
stand-in is recorded once per round and buys its column again every round, so the run no longer outgrows what
it fights. It dies in the fourth round instead, which is a statement about the tuning rather than about this
rule — see [`a-wall-kills-a-count-not-a-share.md`](../research/a-wall-kills-a-count-not-a-share.md).

**A creep fills at most one slot, so depth is the only thing left to buy** once every type is in the wave.
That rule predates this decision and is not being broken for it; what it means now is that a late round raises
a box rather than opening one, and that one creep type cannot be interleaved around another in the column.
