# tower-defense-game

A tower defense game whose multiplayer is real and whose every mode is the same
machine at a different latency. What it is and where it is going lives in
**[The Vision](docs/vision.md)**.

## Status

A deterministic integer simulation, a ghost record format, a headless CLI, a
balance sweep, and a Unity 6 view that scrubs a recorded match from snapshots.
A run is ten rounds of build phase and wave, authored as text and compiled to a
command stream, against a canned field.

[The build order](docs/build-order.md) sequences the rest by what
is cheapest to learn rather than by what depends on what. Its first four steps —
the economy, the run lifecycle, the roster and the sweep — are built and run
from a shell with no engine in them. **Step 5, build-phase interaction in the
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
the wave, what was taken off that round's public offering, and how the wave's
slots were filled — and `-Regenerate` compiles it into the record, having read
the bytes back and played them to the end first. The menus a take names come
from the run's seed, so there is a tool that prints them:

```
./tools/show-offerings.ps1
```

It prints every wave's menu for the seed the committed command stream carries;
`-Seed` reads another run's, and every content file is an argument.

The second thing is the balance harness:

```
./tools/run-sweep.ps1
```

It plays every creep in the roster over a population of runs and writes what
they came to as a comma-separated file — win rate, cost efficiency, what
attacking earned its sender beside what turning up paid, and all of those binned
by how many ingredients a run ended up holding. Fourteen thousand
matchups is a dozen seconds, which is the whole reason the tool is worth
having before the roster is large. Every one of the six content files is an
argument, so pointing it at another map to score it, or at another damage
matrix, costs a flag rather than an edit.

`content/sweep.csv` is the report a real sweep produced at the committed shape;
`-Verify` checks it against a fresh one and `-Regenerate` rewrites it. Any bound
the sweep placed on itself — a sampled seed count, a truncated roster — is a row
of the file, so a partial report never reads as a complete one.

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
