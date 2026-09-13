# 0061 — A kill pays the defender, and the band that measures leaks cannot see it

**Decided.** A kill mints gold inside the match. `content/units.txt` layout 6 adds `bounty`: gold paid to the
defender that kills a body of this row, into the one purse. The number is the roster's (the Grave Robber's
twelve is half its own twenty-four); everything around it is decided here.

- **Refused where the column is read:** a standing row that pays, a poolless row that pays, and **a bounty
  above the row's own cost** — money is minted, so a body worth more dead than sent makes killing the field's
  wave a better income than the round's, and neither instrument would see it. Equal is allowed. Sam took the
  ceiling on 12 September 2026
  ([the decision log](../decision-log/2026-09.md#12-september-2026-later-still--a-body-is-never-worth-more-dead-than-it-cost-to-send-and-the-table-refuses-one-that-is)).
- **Paid where a creep dies**, in `Match.Damage`, on the tick health reaches zero. **The row that pays is the
  row the body is standing as**, never the order that sent it: a transformed body's order names the row it
  stopped being, a raised body's order names its raiser. So a transforming pair pays its successor's number.
  A leak pays nothing; it is already charged against health, and no body is both.
- **One purse, and it does more work.** The match accumulates its bounty and hands it back on `MatchResult`;
  `Run.Play` pays it at `Purse.CloseWave` beside interest, base and bonus — a fourth line. It is minted, not
  transferred. Only the pairing with this round's towers standing pays this purse, averaged over K opponents.
  **Consequence recorded, not decided:** money a defense earns buys offense, because there is one wallet.
- **The band and the sweep are both blind**, for different reasons. The return band is a leak rate, and a
  killed body is outside the ratio; the sweep varies what a run *sends*, and its stand-in sends Minions, which
  pay nothing. `content/sweep.csv` came back byte-identical and the Grave Robber's band reading is 81 before
  and after. The band is left measuring what it measured; netting a bounty into a leak ratio would produce a
  number that is neither. What would read it is a stand-in that sends the row — one line of
  `content/field.txt`, deliberately not moved here because the stand-in's column is calibrated.
- **The money is in the rolling hash** (`_bounty`, `match-state/5` → `/6`): `_killed` is a count and a bounty
  is a sum over rows, so two matches killing the same number of different bodies would otherwise fold equal —
  #254's snapshot bug in the fold's clothes. It is also the first match state a build phase spends.
- `BountyPaid` is decorative; `MatchDecorations` draws nothing. The gain is on the tick and scrubs correctly
  by construction ([ADR-0026](0026-seeking-re-simulates-rather-than-caching.md)); `MatchViewTests` asserts it.
- `CommandStream.Check` folds a purse ceiling, so it gained `Run.MostBountyEarnable` — the most bountiful
  wave in the pool, each body read at its own or its successor's bounty whichever is larger, a raiser's output
  bounded by the ceiling over its period. Loose in the safe direction, and zero for every wave this roster
  can compose. *Finding beside it:* `WaveScript.FullPrice` has not been a ceiling on what a round deals since
  #268 (a raised leak charges the raised row), which is #268's line and is named, not changed.

**Cost.** `unit-types/6`, `SimulationVersion` 13, `rule-fingerprint/11` with a seventh half that kills (none
of the six existing halves killed anything). Every stored record retired; the committed match untouched.

**Rejected.** Deriving the bounty from `cost` (a design decision wearing a rule). A bounty spendable only on
towers (a second wallet in all but name). Leaving the money out of the fold.
