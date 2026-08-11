# 0050 — A decision is composed in a local and proved before it is written

The interactive verb adds no simulation surface. `play` composes a round's `BuildPhase` **in a local**, prices
it after every typed word by calling the pure `BuildPhase.Resolve` and throwing the `Build` away, hands the run
nothing until `done`, and at the end **replays its own compiled script into a fresh run** and writes only if
the two agreed.

Everything a person types is therefore a candidate rather than a move, and every session is a determinism test
of the path the vision's step 5 will need.

## What was decided

**Composing reads the run and never writes it.** `BuildPrompt.Compose` is handed a `Run` and takes its
offering, its purse, its board and its unlocks off it; `Run.Advance` belongs to the caller, and is reached by
exactly one word. The loop therefore has no lifecycle in it — no round counter, no ending, no death — and the
lifecycle above it has no parsing in it. A round abandoned half-composed leaves nothing behind to unwind,
because nothing was wound.

**A word is priced by the call that would refuse it, and the pricing is dropped on the floor.**
[ADR-0037](0037-the-offering-is-public-because-it-is-derived.md) made validation one public call that
*applies nothing* — `BuildPhase.Resolve` checks a decision against the offering, the unlocks, the slot width,
the map, the board and the purse and returns a `Build`, and `Run` is what takes the new purse and unlocks back
— so the loop resolves a half-composed phase, keeps the answer only long enough to learn that
there was one, and discards the `Build`. What falls out is the invariant the whole loop rests on: **the
composed phase always resolves**, because a candidate that did not is never the one kept. `done` cannot arrive
at a decision the run will turn down.

The `Build` is thrown away rather than handed to the frame that draws the result. `RoundFrame.ToText` resolves
the phase again for itself, so a drawing cannot be made against a pricing of some other decision — the second
call is a handful of integer operations against a purse and a map, and paying for it buys the guarantee that
the gold on the screen belongs to the board on the screen.

**Undo steps back one accepted phase, not one typed word.** Every accepted word leaves a whole phase behind
and the loop keeps the list of them, so there is no separate ledger of words that could disagree with what is
actually composed. Two consequences are load-bearing rather than incidental: a *refused* word leaves nothing
to step back to, and a second `take` is undone to the first rather than to nothing.

**Every session is proved by replaying its own compiled script into a fresh run.** `ProvedSession` compiles
the decisions into the `content/commands.txt` grammar, plays that script into a run built on the same seed and
the same shape with nothing played into it, and holds every round report and the folded outcome against what
the player was shown. This is what makes the interactive path an *input* to the recorded path rather than a
second one beside it: [ADR-0039](0039-the-command-stream-is-the-only-route-into-a-run.md) says a decision
reaches the simulation through a stored command and by no other route, and a prompt that emitted rounds
nothing could store would be that door reopened with the word *interactive* on it.

**The decision to write is the prover's, not the verb's.** A script nobody played back is a record of nothing
in particular, and a caller free to write anyway is a caller free to skip the only claim this verb makes. So
`ProvedSession.Written` is the one thing that touches a disk, a disagreement writes nothing and exits non-zero,
and the sentence it prints says the fault is this program's — nothing a player can type can reach one.

## What it costs

**The rounds are resolved twice.** A round's `Advance` resolves the run's wave against every member of the
field, and the proving step plays all of them again on the way out. A ten-round session therefore pays for
twenty rounds of matches, about a second at the committed shape. That is the price of the claim and it is
named here so nobody optimises it into a cached result — a proof that reuses the thing it is proving is not
one.

**A session that quit before committing a round writes nothing at all.** The grammar has no row for a run
nobody played, so there is no script, nothing to prove, and no file. That is not a disagreement and is not
reported as one.

**The prompt owns rebuilding a phase, and `BuildPhase` gains nothing.** A take and a slot are arguments to
`BuildPhase.Of` and an action only to `BuildPhase.With`, so replacing a take or appending a send means
constructing the phase again. The `Taking` and `Filling` that would make that one call each live in
`BuildPrompt`, because composing is a shell's problem: nothing that reads or writes a command file ever
rebuilds a phase, since a stored one arrives whole.

## What was rejected

**A `--interactive` flag on `play-run`.** The smallest diff, and `play-run` is the verb a build gate calls — a
mode flag on it is a branch a gate can take by accident, and what that looks like is a run sitting at a prompt
waiting for a line nobody is there to type. Two verbs sharing every argument reader cost a `case` label.

**Composing on the run itself, advancing per word and rolling back.** It reads as the obvious thing right up
until the rollback has to restore a purse, a board, an unlock set and an ordinal counter — which is a second
implementation of every rule `Resolve` already holds, in a place where being wrong is silent.

**Keeping a ledger of typed words and rebuilding the phase from it to undo.** Two representations of one
decision, free to disagree about what is composed the first time a word is accepted into one and not the
other.

**Writing the script first and reporting the disagreement beside it**, on the argument that a session is worth
keeping either way. The file lands, the next thing to read it replays a run nobody played, and the one
sentence saying so has scrolled off the top of a terminal.

**Proving by comparing the fresh run against itself** — replaying the script and holding the replay against
the replay. It is green forever, and the interactive path drops out of the comparison entirely. The seam that
makes the real comparison possible is that `Played` hands the rounds back as data, so a test can supply a
session that says something the fresh run does not and watch the refusal work.
