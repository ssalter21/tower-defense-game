# 0040 — A run is authored as text and compiled to a record

A whole run is played from a command file: a `CMDS` record holding a seed and the run's build phases. Somebody
has to be able to produce one, and the answer is a text file the command line compiles.

## What was decided

**The authoring form is text, in the same dialect as every other content file.** `content/commands.txt` holds
one `build` row per round — the wave, the half of that round's menu the take came off, the take's id, and then
a `type-id count` pair per wave slot with `0 0` for an empty one. Integers only, no decimal point, comments
free, `ContentException` naming the line. The alternatives were a verb that composes a decision from arguments,
which cannot express a ten-round run in one artefact anybody can review, and a binary somebody edits by hand,
which is a fixture that agrees with whatever its author believed the format was.

The text and the bytes carry the same fields in the same order, so the authoring format never needs a migration
to become a record — the position `WaveScript` has held since the skeleton, applied to a decision.

**The parser adds no rule.** `CommandScript` reads rows and hands them to `RecordCommand.Of`, which is where
the ordering, the empty-slot spelling and the wave numbering already live and where the byte reader enforces
them too. Where the record refuses, the refusal is rewrapped with the line it happened on and nothing else:
one implementation of each rule, and a person editing a file still gets told where. A second copy of "filled
slots ascend strictly by type id" living in a parser is a copy that stops agreeing the first time the rule
moves.

**A take cannot be authored blind, so the menus are printable.** An offering is a pure function of the run's
seed and the wave (ADR-0037), which is exactly why nobody can write a legal decision for a seed they have not
been shown. The `offerings` verb prints every wave's menu, spelled with the words a `build` row uses, off
`Run.OfferingAt` — the same call the run makes when it resolves a round and the same one the replay gate
validates a stored stream against. A listing derived any other way would be a second copy of a derivation, free
to show a menu nobody plays against.

**The record is written only after it has been read back and played.** `record-run` returns bytes from
`CommandStream.Recorded`, which serialises, re-reads, takes the result through the replay gate and runs it to
the end before handing anything over. Nothing that will not replay reaches the disk, which is the rule the
skeleton's `record` verb already followed for a replay bundle.

**The command line stays a thin shell.** Argument parsing, file reads, file writes. Every rule the three run
verbs reach is behind `CommandStream`, `BuildPhase` and `Run`. The one composition decision made out here is
the canned field: the population a round's opponents are drawn from is the single pair of orders
`content/defense.txt` and `content/field.txt` describe, standing in for a ghost pool that does not exist until
runs are stored. That is a thin pool rather than a missing one, and widening it is a longer list and no change
anywhere else.

**The canned field is `--field` and a match's wave is `--wave`, and no verb takes both.** A run's own waves are
composed by the build phases coming off the command stream and are read from no file; the only file of orders a
run verb wants is the opponent. One name for both was one file two callers could disagree about, and the
disagreement is silent — an authored match parses as a field perfectly and outspends every opponent it faces
several times over, so what comes back is a full report about nothing (ADR-0041 measures it). What tells the two
apart is structural: a field member stands in for a stored round, a stored round is a build phase's output, and
a build phase composes what is sent rather than when — so every order of one leaves on tick 0, and `RunContent`
refuses a file whose orders arrive over time.

**`--defense` is read twice, deliberately.** It is what stands while this run's waves are sent and it is the
defense the canned opponent stands behind, so both directions of a round are measured through the same wall.
That is what makes out-dealing the field a statement about the wave; a second defense file would be a second
wall and the win condition would stop meaning anything.

## Consequences

A whole run is reproducible from a shell with no engine, no licence, no editor and no session in it. A fresh
clone, a continuous-integration runner and an overnight agent can all play one.

`content/run.commands` and `content/run-outcome.txt` are committed together, and the outcome is the golden
trace's rule one level up: the trace pins a match tick by tick, this pins a run round by round, so a lifecycle
regression is a diff rather than an argument. Nothing that checks either regenerates it — they come out of
`tools/run-headless-match.ps1 -Regenerate` and out of nothing else, and its `-Verify` mode records into scratch
space to compare rather than over the file it is checking.

Three things can now go stale independently and each goes stale loudly: the script against the record (the
decisions are compared), the record against this build's tables (three content hashes at the replay gate), and
the outcome against the record (the vector is compared round by round).

Sweeping a run — the next thing this feeds — is a mode over the same six content arguments and the same `Run`
constructor, with rows computed in the library and written as a CSV out here.
