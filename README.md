# tower-defense-game

A tower defense game whose multiplayer is real and whose every mode is the same
machine at a different latency. What it is and where it is going lives in
**[The Vision](docs/vision.md)**.

## Status

The walking skeleton is built: a deterministic integer simulation, a ghost
record format, a headless CLI, and a Unity 6 view that scrubs a recorded match
from snapshots. What comes next is
[the eight seams](docs/vision.md#8-the-seams), each planned as its own map.

## Ideas / scope

[The Vision](docs/vision.md) fixes the destination; the
[five deep dives](docs/README.md) behind it are the reading it was built on.

## Getting started

One thing runs today, and it needs nothing but the .NET SDK — no engine, no
editor, no licence:

```
./tools/run-headless-match.ps1
```

It plays the committed replay bundle to the end with nobody watching and prints
the result, the final rolling state hash and the table of interesting ticks.
`-Verify` checks the committed trace and landmark table against a fresh run,
which is what the build gate does; `-Regenerate` rewrites them after a
deliberate content change.

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
