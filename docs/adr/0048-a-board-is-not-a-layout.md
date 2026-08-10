# 0048 — A board is not a layout, and deriving one is a computation

The run owns its defense. It holds **placements in placement order** and *derives* a `TowerLayout` for each
round, sorted into canonical order. `Run.Advance` takes no defense at all.

## The vocabulary

Five words, because the difference between them is the whole decision. Each is spelled the same way in prose,
in a ticket and in C#.

| Word | What it is | C# |
|---|---|---|
| **a board** | What the run holds and what a defensive action changes: placements, in the order they were placed | `Board` |
| **a placement** | An identity, a current type and a cell. Not a tower — it has an ordinal, it has no source line, and it survives changing type | `Placement` |
| **a layout** | What a match is handed: towers in canonical order, derived from a board | `TowerLayout`, unchanged |
| **an action** | One row of the command stream that changes the board: `place` or `upgrade`. Not a placement — an action is the instruction, a placement is what it creates or changes | `BuildAction`, `ActionKind` |
| **the payer** | The one walk that spends a round's gold: the take, then the actions in the order they were written, then the wave's slots | `BuildPhase.Resolve` |

**A placement is not a tower** and **an action is not a placement** are the two the code will try hardest to
collapse, because `PlacedTower` already exists and reads like both.

## What was decided

**The run owns the board.** [ADR-0039](0039-the-command-stream-is-the-only-route-into-a-run.md) says a
decision reaches the simulation through a stored command and by no other route. A defense the caller composes
and hands in each round is the `RoundOrders` overload that ADR deleted, wearing a different hat: a defense
assembled by anybody, applied against no map, paid for out of no purse. So `Run.Advance(BuildPhase)` takes one
argument, `CommandStream.Replay(Run)` and `CommandStream.Recorded(Run, commands)` lose theirs, and the run's
constructor takes no defense either — the opening board is empty.

Caller-ownership had a second cost that is easy to miss: it guarantees a *second implementation* of the
folding rules wherever a caller needs one. The sweep would fold a board, the CLI would fold a board, and one
day a server would fold a third.

**A board is not a layout, and the board is the new type.** Three things want to live on a board that have no
business on a `TowerLayout`: the placement ordinals
([ADR-0049](0049-a-placement-identity-is-derived.md)), placement order itself, and the *absence* of a source
line — `PlacedTower.Line` points into a text file that a placement made at wave 4 was never in. Deriving
rather than widening leaves `Match`, `TowerCoverage`, `GhostRecord` and the field pool reading exactly the
type they read today.

**The derived layout is sorted into canonical order, and that is the seam.** One place in the system turns
placement order into canonical order, and it is the derivation. Everything upstream of it is a sequence of
decisions; everything downstream of it is a position.

**An empty board is a position and not a fault.** A run starts with the purse and nothing on the map, so
standing nothing at wave 1 is legal. `TowerLayout.Parse`'s refusal of a file with no towers stays exactly
where it is: it is a rule about a *file*, and a defense file with nothing in it is one somebody forgot to
finish.

## Why this is not ADR-0017 broken

[ADR-0017](0017-canonical-order-is-asserted-not-restored.md) — *canonical order is asserted at load, never
restored* — reads like a flat prohibition on the sort above, and it is the first thing anyone will raise. It
is a rule about **stored records**: two identical records must not have two byte spellings, or
content-addressing a record ([ADR-0030](0030-record-ids-are-content-addressed.md)) stops meaning anything.

A run-built board is not stored as a `TowerLayout`. What is stored is the command stream, and the stream keeps
**placement order**, which is meaning-bearing there — the ordinals depend on it, and the same two placements
in the other sequence are a different run rather than a different spelling of one. So sorting the derived
layout creates no second spelling of anything; the canonical thing stays canonical. Deriving a layout is a
computation, like `Offering.Draw`, and not a load.

The rule that survives, stated so the next reader does not have to re-derive it: **assert what you read,
compute what you derive.**

## What it costs

**Placement sequence has no combat meaning, deliberately.** Iteration order *is* a simulation input — two
towers whose ranges overlap can pick the same creep on the same tick, and the loop order decides which lands
the killing shot. Sorting the derived layout means the order you built in cannot reach that. The alternative
was to let it: place-then-place in the other sequence would be a different match. On a one-wide corridor where
a great many placements are already equivalent, that is a difference nobody chose and nobody could read.

**A layout is derived once per round rather than held.** It is a sort of a handful of placements against a
match that takes milliseconds, and the cost is named here so nobody optimises it back into stored state.

**The board carries no source line, so the map refusals had to move.** The three map-aware refusals in
`TowerCoverage` throw `ContentException` carrying a file line, and a placement made at wave 4 has neither a
file nor a line. One shared map-aware predicate answers the question; two callers wrap it in the exception
type each deserves.

## What was rejected

**The caller keeps composing the defense and hands it to `Advance`.** The smallest diff by a wide margin, and
it is ADR-0039's deleted door reopened with the word *defense* on it.

**A board *is* a `TowerLayout`, widened with an ordinal and a nullable line.** It puts a run-mutable identity
and an optional file position onto the type the record format asserts an order over, so every reader of a
stored defense inherits fields that are only ever null for it.

**The derived layout keeps placement order.** Cheaper — no sort, no seam — and it makes the order of two
otherwise equivalent placements a silent simulation input.

**Sorting the board itself, so board and layout are one order.** It destroys the ordinals, which are the
placement's identity, on the first placement that sorts ahead of an older one.
