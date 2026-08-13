# 0037 — The offering is public because it is derived, and a build phase is validated once

> **Superseded by [#179](https://github.com/ssalter21/tower-defense-game/issues/179), 13 August 2026.**
> `Offering`, `Unlocks` and the take are deleted. Nothing rations what a run may send: every creep on the
> roster is sendable from wave one, a build phase carries slots and actions and no take, and the command
> stream's build row lost the two fields that stored one — which is what format version 2 is the bump for.
>
> What survives is the argument that a *derived* thing needs no stamp of its own because its inputs are
> already stamped, and the rule that a build phase is validated once where the decision is read. Both hold
> unchanged for the upgrade ladder, which is the one prerequisite left. Read the rest as a record of a gate
> the game no longer has.


A build phase is three things and one seam:

- **The offering** — `OrdinaryOptionsPerRound` creeps drawn out of the roster, plus the anchor's menu merged in
  on an anchor round — drawn fresh every wave at a position derived from the run's seed under the label
  `"run-offering/1"`.
- **The unlocks** — every option a run has taken, permanent, free, and the bound on what it may field.
- **The slots** — a creep type and a count each, as many as `AnchorSchedule.WaveSlotsAt` derives for the round,
  and any of them may be left empty.

`BuildPhase` is the decision as data. `BuildPhase.Resolve(offering, unlocks, purse, costs)` is where the four
checks live, and `Run.Advance(BuildPhase)` — the only way into a round — calls it and resolves the wave it
composed against the round's field.

## What was decided

**The offering is public because nothing private goes into its derivation.** It is a pure function of the run's
seed and the wave — never the purse, never the unlocks, never what was sent — so two players of one match are
handed the same list, and a menu read before the run is played is the menu the run plays against. Making a shop
"public" by copying it between players would be a claim about plumbing; deriving it is a claim a test can fail.
`BuildPhaseTests` fails on both halves: two runs that played different openings, and one that has played
nothing at all.

**What is already unlocked is not taken out of the pool.** An offering that thinned itself against what
somebody held would be a different offering for every player, which is the one property this type exists to
have. Being re-offered a creep you took is a wasted position on the menu, and that is the roster's business.

**Validation is one public call, because #92 revalidates a stored stream against it.** A command file is read,
checked and only then played, and the checking must be the same code the live path runs — two implementations
of "was this legal" is how a stored run partially validates. So `Resolve` takes the offering, the unlocks, the
purse and the cost table and returns a `Build`; it applies nothing. `Run` is what takes the new purse and
unlocks back.

**Every failure is a refusal and never a skip**, on the rule `WaveScript` already applies to an unknown type
id: a take naming an option the offering did not carry; a slot naming a creep the run never unlocked; more
slots than the round's width; a wave the purse cannot afford; two slots naming one creep; a slot sending none
of something, which is what `WaveSlot.Empty` is for. Each is asserted by name, because a suite that only
asserted "it threw" passes when the phase is refused for the wrong reason.

**The whole wave is priced before a coin moves.** `Purse.Spend` throws on an overspend and its own message says
reaching it means an unaffordable command was let through — so the affordability check belongs one layer up,
over the summed slots, or a purse is left part-spent on a wave that was never legal.

**And the same rule one layer up again.** `Run.Advance(BuildPhase)` resolves the decision, composes the orders
and checks the run is unfinished *before* it takes the new purse and unlocks back. A run that refused a round
and paid for it anyway is indistinguishable afterwards from a run that played one, which is the same defect at
the round's scale.

**Filled slots ascend strictly by type id.** A slot becomes one line of a wave, and a wave's lines ascend and
are unique on `(tick, type)` — asserted rather than sorted, because sorting would leave two identical waves
with two different sets of bytes (ADR-0017). One rule then also makes two slots on one creep a refusal rather
than a slot silently spent twice. Every slot releases on tick zero: a build phase composes *what* is sent, and
*when* would be a spacing constant no file authored.

**A wave with every slot empty is legal, and it is the only empty wave there is.** Not sending banks the round
at the ruleset's interest, which is a position rather than an omission. `WaveScript.Parse` and
`WaveScript.FromRecord` still refuse an empty wave, because a *file* or a *record* that sends nothing is one
somebody did not finish; `WaveScript.FromSlots` does not, because a build phase that sends nothing is a player
who decided to bank.

**A run opens on an authored balance.** `Ruleset` gained a `purse` row. Nothing has been earned when the first
build phase stands, so a run opening on nothing would have an opening round whose only affordable wave is the
empty one — ten waves with nine build phases in them. The committed value is one wave's base income.

**The ordinary count is bounded by the roster, and the bound is a refusal.** An option unlocks a creep and
appears on a menu once, so an offering cannot be drawn out of fewer creeps than it carries options. The
committed `offering` row is therefore `2 3` against the two walkers that exist, not the `3 3` the design names;
the number rises with the roster seam 3 authors. Answering with the same creep twice would be one option
wearing two positions.

## What it costs

**A fourth label in the derivation scheme.** `"run-offering/1"` joins the other three as a constant on `Run`,
for the reason ADR-0034 gives.

**The ruleset's layout version moved to `ruleset/3`.** Adding the `purse` row adds a field to the fold. It
retires nothing today, because no record header carries the ruleset's content hash.

**`Run` has a second public method.** `OfferingAt(int)` is a draw and moves nothing, and `RunTests`' reflection
assertion — that `Advance` is the only thing that moves anything — now names both and asserts that reading an
offering leaves the health, the purse and the unlocks where it found them.

**The defense is still an argument.** A build phase composes what is *sent*; what *stands* is handed in beside
it. Charging the purse for towers is a decision this ticket does not carry and the acceptance criteria do not
name, and it would make the opening round's committed six-tower defense unaffordable.

## What was rejected

**Storing an option by its index on the menu.** Smaller in a record and silently meaningless the moment a draw
moves: index 2 of a different offering is a different creep and nothing says so. A decision names a kind and an
id, so a take against a menu that moved is a refusal.

**Making the take optional.** Unlocking is free, so declining is a decision nothing rewards, and it would give
the stored decision a second shape for no gain.

**Sorting the slots into wave order.** It spares the author an ordering rule and it means two different stored
build phases compose one wave, which is where content-addressing stops meaning anything.

**Letting `Purse.Spend` be the affordability refusal.** Watched: it fires from a purse already part-spent on
the earlier slots of the same illegal wave, and says only that reaching there means an unaffordable command was
let through.

## Where it lives

`sim/Offering.cs`, `sim/Unlocks.cs`, `sim/BuildPhase.cs`, `sim/Draws.cs` — the partial Fisher-Yates an anchor's
menu and a round's offering now share — `sim/WaveScript.cs` — `FromSlots` — `sim/Run.cs` —
the `OfferingLabel` constant, `Unlocks`, `Offering`, `OfferingAt` and `Advance` —
`content/ruleset.txt`, and `sim.tests/BuildPhaseTests.cs`.
