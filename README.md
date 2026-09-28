# tower-defense-game

A tower defense game whose multiplayer is real and whose every mode is the same
machine at a different latency. What it is and where it is going lives in
**[The Vision](docs/vision.md)**.

## Status

A deterministic integer simulation, a ghost record format, a headless CLI, and
a Unity 6 view that scrubs a recorded match from snapshots.
A run is ten rounds of build phase and wave, authored as text and compiled to a
command stream, against a canned field.

[The build order](docs/build-order.md) sequences the rest by what
is cheapest to learn rather than by what depends on what. Its first three steps —
the economy, the run lifecycle and the roster — are built and run from a shell
with no engine in them; the fourth, the balance harness, was built and then
removed until it has a specification. **Step 5, build-phase interaction in the
client, is what comes next**; the loop against stored ghosts is step 6.

## Ideas / scope

[The Vision](docs/vision.md) fixes the destination and holds only what is
decided. [The build order](docs/build-order.md) is the sequence,
[open questions](docs/open-questions.md) is what is in scope but unsettled,
[the decision log](docs/decision-log.md) holds every time the vision changed its
mind, and [`docs/archive/`](docs/archive/README.md) holds the five deep dives it
was built on.

## Getting started

Two things run today, and they need nothing but the .NET SDK — no engine, no
editor, no licence:

```
./tools/run-headless-match.ps1
```

It plays two committed records to the end with nobody watching. The first is a
match: `content/match.replay`, one defense against one wave, reported as the
result triple, the final rolling state hash and the table of interesting ticks.
The second is a whole run: `content/run.commands`, ten build phases against a
canned field, reported round by round as what each wave got past the field and
what the field got past it.

`-Verify` checks the committed trace, the landmark table and the run's outcome
against a fresh play, which is what the build gate does; `-Regenerate` rewrites
them after a deliberate content change.

A run is authored as text. `content/commands.txt` is one `build` row per round —
the wave and how the wave's slots were filled — and `-Regenerate` compiles it
into the record, having read the bytes back and played them to the end first.

Every creep on the roster is sendable from wave one, so a build row is written
against `content/units.txt` and the purse alone. The one prerequisite the game
has is the upgrade ladder: a unit some edge of `content/upgrades.txt` points at
may not be placed, and is reached by upgrading the rung below it. There is a
tool that prints the edges:

```
./tools/show-ladder.ps1
```

A run is played in the client rather than at a prompt. What a session decided is
written out there as a `content/commands.txt` script, but only after that script
has been replayed into a fresh run and agreed round for round with what you were
shown — a session that disagrees writes nothing at all. `simcli record-run`
compiles that script and `simcli play-run` plays it back headlessly, which is how
a run somebody played is reproduced without opening the editor.

## Looking at it

With Unity installed and the editor closed:

```
./tools/build-player.ps1
```

That writes a double-clickable Windows player into `client/Builds/Windows/`.
It plays the same recorded match the command line does — the record ships beside
the executable, seed and all — which is what makes the tick numbers in
[the sit-down checklist](docs/sit-down.md) mean something. That checklist is
twelve things to look at, once, each naming the exact tick to look at and what
broken looks like.

## License

MIT — see [LICENSE](LICENSE).
