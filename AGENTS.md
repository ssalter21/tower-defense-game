# Agent instructions

Working rules for anything — human or agent — doing execution work in this repo.

Four rules. Each exists because the obvious alternative fails quietly, which is the failure mode that costs the
most to find later. Keep this file short: it is loaded into every agent's context, so anything that is a
*finding* rather than an *instruction* belongs in [`docs/research/`](docs/research/).

## 1. Compile feedback comes from the project-local editor log

`client/Logs/Editor.log` is the log for this project. **Never read the global path** —
`%LOCALAPPDATA%\Unity\Editor\Editor.log` on Windows — even though it is the one Unity's own documentation names.

The trap is that the global file *exists* and *parses*. Reading it does not error; it answers, and the answer is
about something else. Measured here minutes apart: 3,350 bytes in the global log against 724,920 in the
project's own. An agent that trusts the global path gets a small, plausible, stale file and reports confidently
on a compile that never happened.

## 2. Say it in your reply when engine-side code went uncompiled

If you wrote C# that Unity has not compiled, and getting it compiled would need the developer to alt-tab,
**write that in your reply**. Do not sit and wait for an editor to notice. Do not quietly hand back untested
code either.

The point is where the friction lands. Waiting silently spends the developer's wall-clock time invisibly —
nobody ever sees the total, so nobody ever fixes it.

## 3. Every automation has a static command-line entry point

Anything an agent needs to run lives in `tools/` and runs from a shell: `run-headless-match.ps1`,
`run-parity-run.ps1`, `run-unity-tests.ps1`, `run-playmode-tests.ps1`, `run-editmode-tests.ps1`,
`run-player-tests.ps1`, `build-player.ps1`, `build-match-scene.ps1`, `build-test-assets.ps1`,
`adopt-unity-project.ps1`, `sync-streaming-content.ps1`, `capture-match-frames.ps1`,
`capture-art-previews.ps1`, `check-file-sizes.ps1`, `check-project-settings.ps1`.

**Nothing may depend on an editor bridge being installed** — no plug-in that has to be present in a running
editor, no socket to a live Unity, no "first open the project and press the button". A bridge is a dependency on
a *session*, and sessions are exactly what a fresh clone, a CI runner and an overnight agent do not have.
Batchmode (`-batchmode -executeMethod`, `-batchmode -runTests`) needs the editor closed and works from nothing.

## 4. Generated files are committed beside the change that caused them

If a change causes a file to be regenerated — a lockfile, a `.meta`, a built plug-in — that file goes in the
same commit. Not a follow-up, not "it'll regenerate".

The rule is what makes a fresh clone the same project as the one it was cloned from. The corollary bites too: if
a generated file must **not** be committed, that has to be arranged by construction — an ignore rule — and not
by remembering. Ignore rules only govern untracked files, so a *tracked* generated file like
`client/Packages/packages-lock.json` has to be watched for by hand. See `client/.gitignore`, which carries the
scars.

## Waiting on Unity

Three facts, measured. The evidence is in
[How long Unity takes to notice a rebuilt plug-in](docs/research/unity-hot-reload-timing.md).

- **Unattended work does not stall.** An untouched editor picks up a rebuilt plug-in on its own — but the delay
  ranged from 18 seconds to 11 minutes for the *same* editor doing the *same* thing. Poll for evidence; never
  sleep a fixed interval and assume.
- **Poll the project-local log for a marker your own code emitted.** Silence there means *not yet*, and never
  means *failed* — the log is written lazily and an idle editor can leave it hours behind.
- **If you need it now, close the editor and use batchmode.** `SetForegroundWindow` is refused to a background
  process, so an agent cannot force the refresh. Asking the developer to alt-tab works and costs about
  18 seconds.

## Where things are written down

- [`docs/vision.md`](docs/vision.md) — the standing document: what the game is, and the order it gets built in.
- [`docs/adr/`](docs/adr/) — why the code is shaped the way it is. Source comments say *what* the code does; the
  reasoning lives here.
- [`docs/research/`](docs/research/) — evidence notes. Each answers one question and cites primary sources.
- [`docs/agents/issue-tracker.md`](docs/agents/issue-tracker.md) — the tracker doc: labels, the effort review
  boundary, and how blocking, claiming and closing a ticket are done here.
