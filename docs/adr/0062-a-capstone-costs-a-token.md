# 0062 — A capstone costs a token, and the count is derived rather than stored

**Decided.** The top of a tower line is bought with a **capstone token**: one token, no gold. A run is granted
one at rounds 3, 6 and 9, it banks, and three tokens meet nine capstones. The schedule and the one-token price
are the roster's; what is decided here is everything around them.

- **`content/upgrades.txt` goes to layout 2**, label `upgrade-ladder/2`. The keyword a row opens with is the
  currency: `upgrade` is the target row's full gold price, `capstone` is one token. A new layout though the
  field count did not move, because what the keyword column *means* moved — two files whose keyword means two
  things, both calling themselves layout 1, is a difference nothing here could detect. Layout 1 keeps its
  branch, label and hash, and a `capstone` row in a layout-1 file is refused.
- **`EdgePrice` names a currency, never an amount.** Gold's amount is the target's `cost`; a token's is one,
  and no column could make it two.
- **The count is derived, not stored.** A command stream already carries every input — the wave on each build
  phase, the actions against the stamped ladder, the ladder hash in the header — so `CommandStream.Check`
  folds the balance forward beside the purse and the board, **exactly** rather than at a ceiling (nothing
  about a token depends on how a round played), and refuses a stream that climbs a capstone it could not have
  held a token for. `RecordFormat.CommandVersion` stays 3; `SimulationVersion` stays 13, because a pricing
  rule no stored stream can reach without failing the content stamp is not a behaviour change.
- **The schedule itself is uncovered by every hash** — it is a list in `sim/Run.cs`, and the fingerprint's
  scenario has no capstone to buy — so `DerivationTests` pins it with an assertion whose comment says a change
  owes a `SimulationVersion` bump and a `BehaviourByVersion` row.
- **The sending side keeps one scarcity, the purse.** Only the tower side gained a second, and only at the
  top of a line. This is not the gate deleted on 13 August: no capacity schedule, no per-wave type limit, no
  offering. The 14 August proposal carried both halves and only the token half is taken.
- **No capstone is priced by the damage rule and no premium compensates**: seven of the nine read flat against
  the rung below and `show-ladder` says so
  ([ADR-0063](0063-a-price-is-derived-from-the-row-and-its-silences-are-named.md)).
- **The scripted player spends a token the round it arrives**, in its own loop after the gold, by damage over
  the route per tick, and only when the capstone beats the rung it replaces. Five of nine change no roll and
  no body count, so the bot leaves those tokens unspent — and on the committed content it spends none at all,
  because its ten-round wall is fourteen first rungs. `content/sweep.csv` is byte-identical: the harness
  cannot see this mechanic, structurally, as [ADR-0061](0061-a-kill-pays-the-defender.md) cannot see a bounty.

**Cost.** One retirement: the ladder hash and the content hash move, so every record stamped against the
roster as it stood is retired. The committed match and the committed run are untouched, byte for byte.

**Rejected.** A third price column (widens eighteen rows to carry nine prices). Deriving a capstone from the
ladder's shape — top of a line is a node with no outgoing edge — which would re-price the third rung the day a
fourth is added and make a two-rung line's second rung free. A stored token count (a second copy of a
derivation, free to disagree). Widening the fingerprint's scenario to cover the schedule (a bump for a rule
that has not moved).

Lives in `sim/UpgradeLadder.cs`, `Run.CapstoneTokens*`, `BuildPhase.Applied`, `CommandStream.Check`,
`CoverThenUpgradeBot.BestCapstone`, `simcli/Ladder.cs`, `client/Assets/View/ComposedRound.cs`,
`TowerPalette.PriceOfRung`, `RosterNames.cs`.
