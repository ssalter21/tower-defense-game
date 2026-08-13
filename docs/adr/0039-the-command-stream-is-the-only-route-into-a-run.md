# 0039 — The command stream is the only route into a run, and it stamps every table it means anything against

A fourth record kind, `CMDS`, holds a run's build phases as `(wave index, decision)` pairs. `Run` consumes
them; nothing else does.

## What was decided

**The view emits a command, the command goes into the record, and the record is what the run consumes.** The
other shape — a decision reaching the tick loop by some other route, a callback the run asks mid-round, a
listener that answers — contaminates the one guarantee everything else in this repository rests on. Keeping it
buys the submission barrier nearly free later, because a submitted turn *is* a command batch, and it makes a
played run a replayable record, which makes every playtest a determinism test.

That claim is asserted structurally rather than described: `Run`'s public surface is exactly
`Advance(BuildPhase)` and `OfferingAt`, no parameter of `Run` or `Match` is a delegate, and the only interface
either accepts is the decorative event listener, whose every method returns void. A run cannot be asked a
question it could answer with something a record does not carry. Handing a decision straight in is
not a way around the record either, and that is asserted rather than argued: a `BuildPhase`'s whole public
data surface is `Take`, `TakeId` and `Slots`, which are the three fields a stored command carries, so every
decision reachable by handing one in is a decision a command could have made.

**A wave nobody was charged for cannot be handed to a run at all.** `Advance` once had a second overload
taking a `RoundOrders` — a defense and a wave, composed by anybody, resolved against no offering, checked
against no unlock, held to no slot width and bought out of no purse. It survived because the economy suite was
written through it: a fold of `Purse.CloseWave` over the outcome vector reproduced the run's own purse, which
is an identity that holds only while nothing is ever bought. Both are gone. The fold takes each round's
`Build.Spent` off the purse before closing the wave, and what a run may send is what its build phases paid
for.

**A build phase is stored as exactly what a decision is.** `u16 wave`, `u8 take kind`, `u16 take id`, and the
slots as `(u16 type_id, u16 count)` pairs with `(0, 0)` meaning empty. Nothing else: the offering a take was
made off is a pure function of the run's seed and the wave, so it is redrawn at load rather than carried. A
stored offering would be a second copy of a derivation, free to disagree with the first.

**Its format version is counted separately**, at 0, so the three kinds that existed before it did not move when
it arrived and no stored defense, wave or bundle looks newer than it is. That is
[0010](0010-format-versions-per-record-kind.md) paying for itself the first time a fourth kind was added.

**The whole stream is validated before the first round is played.** `Check` walks it against the run and folds
the three things a round moves — the unlocks, the purse and the board — forward through values of its own,
applying nothing, so a stream that would be refused at round four is refused before round one resolves. A run
that partially validates produces three rounds of outcome that somebody keeps. The validation is
`BuildPhase.Resolve` — the same surface a live build phase is checked by, so there is one implementation of
the rules and not two.

**Affordability is the one thing a decision can be refused for after a round has resolved.** What a wave can
afford depends on the band its offense reached, which is a number only a resolved round has, so the walk
carries a ceiling rather than the run's own purse and admits every decision the run could have afforded however
well it played. Everything else a stored decision can be wrong about — the take, the unlocks, the slot width,
the cell, the wave index — is settled by the walk, over values that do not depend on how a round played. (A run
whose health empties still stops mid-`Replay`, which is the run ending rather than a decision being refused.)

**The wave index is the one check `Resolve` cannot make.** It is handed an offering and has no way to know
which round is about to be played, so a decision made at wave seven and stored at wave three resolves perfectly
against wave three's menu — against a menu it was never shown, at a slot width it never had, out of a purse it
never held. `CommandStream.Check` compares the stored wave against the round, where both numbers are in hand.

**The stream stamps the ruleset's and the anchor schedule's content hashes beside the unit table's, and each
refuses on its own.** Until now no record header carried either, so retuning `content/ruleset.txt` retired
nothing. A command stream is the first record kind whose meaning depends on them: the ruleset prices the wave,
opens the purse and pays the interest; the schedule decides where the anchors are and how wide a round's slots
get. Measured while writing this: with the schedule stamp removed, moving the second anchor from wave six to
wave five leaves waves one to four's menus and widths untouched, so a four-round stream plays through a
rotation it was never recorded against and nothing downstream can tell. The hashes fold parsed integers rather
than file bytes ([0011](0011-content-hash-folds-parsed-integers.md)), so reformatting either file retires
nothing.

**Nothing is handed over that will not replay.** `CommandStream.Recorded` returns bytes only after parsing them
from its own output, taking them through the gate and playing them to the end — the rule the command line's
`record` verb already follows for a replay bundle.

## What it costs

**`Run` exposes its `Ruleset`, its `UnitTypeTable` and the field its waves are paid against.** The gate compares
the stream's stamps against the tables the run is actually playing rather than against tables a caller passed
alongside, which is the whole difference between a gate and a courtesy. The cost is three more public
properties on a type that was previously opaque about its inputs.

> **Amended 13 August 2026.** The strict-ascending half of the ordering rule is gone: a slot's position is now
> its release order, so an arrangement is a decision rather than a spelling to canonicalise. The three places
> below still refuse a *repeated* creep; none of them refuses one out of ascending order.
> [ADR-0051](0051-a-round-is-composed-on-screen-and-arrives-as-a-stored-command.md) says why, and
> [the decision log](../decision-log.md) records what it cost. Everything else on this page stands.

**The ordering rule is written three times.** `BuildPhase.Resolve` refuses a repeated creep when a decision is
resolved, `RecordCommand.Of` refuses one when a command is composed, and `ReadSlots` refuses one when bytes are
read. The middle one is what stops a writer emitting bytes its own reader would refuse, and the outer two fail
with different exception types on purpose — a fault in this program against a fault in stored bytes. The take's
own bounds are not duplicated: the spelled-out `RecordCommand.Of` overload builds a `BuildPhase` and inherits
that check rather than restating it.

**The load walk's purse fold is the run's arithmetic, restated.** `Check` closes each wave exactly as
`Run.Advance` does, and both read the distribution from one place, `Run.Field`, so the field a wave is paid
against and the field a walk predicts against cannot become two answers. The walk passes zero for what the wave
dealt, because it has not played the round; against a field nobody is at a percentile of, that amount does not
enter the sum. A test pins the two together — the purse the walk predicts for the round after the last is the
purse the run actually holds when it gets there.

## What was rejected

**Validating a decision at the round that plays it.** It is where `Run.Advance` already validates and it is not
enough: refusing at round four means rounds one to three resolved, and an outcome that exists is an outcome
somebody keeps.

**A `Replay` that takes the tables as arguments beside the run.** It permits checking against one ruleset and
playing against another, which is precisely the failure the stamps exist to catch.

**Storing the offering, the slot width or the purse in the stream.** All three are derived from the seed, the
wave and the tables the header already pins. A stored copy is a second answer that can disagree with the first,
and a record with two answers has none.

**Folding the ruleset and schedule hashes into the shared header's one content hash.** Three stamps that fail
as one cannot say which table moved, and "your record is retired" without a name is a message nobody can act
on.
