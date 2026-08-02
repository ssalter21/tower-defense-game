# tower-defense-game

A tower defense game. Currently just the scaffolding — stack not chosen yet.

## Status

## Ideas / scope

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

## License

MIT — see [LICENSE](LICENSE).
