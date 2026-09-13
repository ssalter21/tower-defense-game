# 0063 — A price is derived from the row, and its silences are named

Every `cost` in `content/units.txt` is arithmetic over the row it sits on, never a number a person picked. A
creep costs its effective health — pool times armour multiplier — over 160; a tower costs one gold per five
damage a second, times the bodies a shot hits (the `targets` column). A dead tie rounds up: the Skeleton's
16.5 is 17, the Shade's 7.5 is 8, the Frost Wight's 52.5 is 53, one rule for all three so nobody "corrects"
one of them the other way later. Both sides are priced in one quantity so that one purse buys both.

**The rule is provisional.** It is the price until a cost derived from the sweep — many simulations rather
than a row's stats — can be trusted to replace it, and the readings below are that rule's acceptance test.

## What it does not price, and why nothing is authored to cover the gap

The rule reads health, armour, damage, cooldown and `targets`. It does not read **range, bubble radius,
shield, duration, windup or backswing, a transformation or a raise**, so:

- A rung that changes range alone prices flat against the rung below — `archer → ranger`, `cleric → bishop`,
  `druid → elder`, `engineer → artificer` — and `show-ladder` prints a *flat or falling price* note against
  each. It is a note, not a fault.
- A shield is not charged: the Vampire stands on 3360 plus a raw 1400 and the Grave Robber on 3900 plus 2000,
  and each is priced on the first number.
- The Cursed Villager is priced as two rows and sent as one. The change resolves ahead of the damage, so its
  own 1800 is never spent; 11 gold buys the Werewolf's 2860, which the rule prices at 18. The 7 gold is a
  pricing gap, not a reason to move the trigger.
- The Necromancer keeps its derived 21 while raising about 110 gold of Minions against the committed defense.
- Windup and backswing lengthen the cycle the cooldown alone describes, so a row with a pair is priced for a
  rate it does not reach. "Five damage a second" is a number about seconds: if the clock moves, re-derive the
  constant.
- Seven of the nine capstones price identically to the rung below — five change neither the roll nor the
  bodies, and Slam and Mortar spread one roll over a bubble whose `targets` stays 1. Nothing charges any of
  them, because a capstone is bought with a token ([ADR-0062](0062-a-capstone-costs-a-token.md)).

**One row is held by hand and it is the exception that proves the rule.** The Mage's 92 was three bodies'
worth of a splash; the rule reads 30. It stays at 92, the Sorcerer and Unravel inherit 124 from it, and the
number waits on the derived rule rather than on a signature.

## Alternatives rejected

- **An authored capstone premium.** Scarcity is the grant schedule — three tokens, nine places — not the price.
- **A guessed coefficient for range, radius or shield.** Priced against the one-hex corridor, it would be
  priced against geometry that is gone; elevation already makes range worth more than a flat coefficient says.
- **A hand price on the Necromancer** (21 plus what it raises, about 131). It would be the first authored cost
  on a table derived everywhere else, guessed against one corridor and overwritten the day the derived rule
  lands. A cap on the raise reopens a signed row.
- **Widening the return band** so the rows the rule under-prices read as in. The band is the one test that
  says a row is free money; it caught the Necromancer.

The readings each silence produces are [the tuning target](../research/the-tuning-target.md); the design side
is [the roster](../roster.md#what-things-cost).
