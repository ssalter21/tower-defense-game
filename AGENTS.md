# Agent instructions

Working rules for anything — human or agent — doing execution work in this repo. Six rules, each because the
obvious alternative fails quietly. **This file is loaded into every agent's context, so it holds instructions
only**: a finding belongs in [`docs/research/`](docs/research/), a reference in [`docs/`](docs/README.md).

## 1. Compile feedback comes from the project-local editor log

Read `client/Logs/Editor.log`. **Never read the global path** (`%LOCALAPPDATA%\Unity\Editor\Editor.log`), even
though Unity's own documentation names it. It exists and it parses, and it is about a different compile —
measured minutes apart: 3 KB in the global log against 725 KB in the project's own.

## 2. Say it in your reply when engine-side code went uncompiled

If you wrote C# that Unity has not compiled, and compiling it needs the developer to alt-tab, **write that in
your reply**. Do not wait silently for an editor to notice, and do not hand back untested code without saying
so.

**The build gate will not catch it.** `dotnet test sim.tests` is everything CI runs; no EditMode or PlayMode
test runs there. Run them yourself, editor closed: `run-editmode-tests.ps1`, `run-playmode-tests.ps1` or
`run-unity-tests.ps1`. `check-unity-test-inventory.ps1` counts the client's tests and goes red when the count
changes; it does not compile them, so a *changed* test or view file passes every gate step unbuilt.
`nightly-unity.ps1` (registered by `register-nightly-unity.ps1`; 03:00; one line per runner to
`client/Logs/nightly.log`) tests one checkout the morning after — a catch, not a substitute for running the
one you changed.

## 3. Every automation has a static command-line entry point

Anything an agent needs to run lives in `tools/` and runs from a shell — `ls tools/*.ps1`; every script's
header says what it is for. **Nothing may depend on an editor bridge** — no plug-in that must be present in a
running editor, no socket to a live Unity, no "open the project and press the button". A bridge is a
dependency on a session, which a fresh clone, a CI runner and an overnight agent do not have. Batchmode
(`-batchmode -executeMethod`, `-batchmode -runTests`) needs the editor closed and works from nothing.

The board's editor menus — `Tools > Board > Edit Map`, `Scenery`, `Dress` — obey this: each is a
`public static void` with no arguments, and `content/map.txt` and `content/dressing.txt` stay the artefacts.
What each does, and the `model`/`place` verbs in the dressing file, are in
[the board tools](docs/board-tools.md).

## 4. Generated files are committed beside the change that caused them

Whatever a change regenerates — a lockfile, a `.meta`, a built plug-in — goes in the same commit. Not a
follow-up, not "it'll regenerate". That is what makes a fresh clone the same project. The corollary: a
generated file that must **not** be committed is excluded by an ignore rule, never by remembering — and ignore
rules only govern untracked files, so a *tracked* one like `client/Packages/packages-lock.json` has to be
watched by hand. `client/.gitignore` carries the scars.

## 5. A worktree is finished when `git worktree list` stops naming it

**Close the editor before `git worktree remove`.** The command unregisters and then deletes; an open Unity
holds `client/Library`, the delete fails, and what remains is a directory under `.claude/worktrees/` with no
`.git` file that neither `list` nor `prune` can see. Five accumulated here, about 130,000 files. The check:
anything in `ls .claude/worktrees` and not in `git worktree list` is an orphan. Delete the local branch too
once its pull request merges — `git branch --merged origin/main` lists them.

## 6. What verifies an area decides how much of it an agent may do unattended

The gate proves the simulation and only counts the client's tests (rule 2), so **match the autonomy to what
would actually catch the change being wrong.** Holding a visual change to the simulation's standard passes
every check on an artefact nobody has looked at. The evidence each row rests on is
[What agents can build unattended](docs/research/what-agents-can-build-unattended.md).

| Area | What verifies it | Autonomy |
|---|---|---|
| `sim/`, `sim.tests/`, `sim.poison/` | The gate, the matrix, the IL scan, the golden trace | Full — a green gate is sufficient |
| `content/*.txt` | The trace moves and must be regenerated deliberately | Full to change; never to regenerate in order to get green |
| `simcli/`, `tools/` | The gate runs seven of these scripts; every other one runs from a shell | Full — run the one you changed |
| `docs/` | `check-docs.ps1`, on four claims a document makes about other files | Full to draft; a person's review is the gate |
| `client/`, non-visual | The three Unity runners, editor closed, counts reported | Full — the agent runs the runners itself |
| `client/`, visual | A captured frame or sheet, and a person | Agent proposes; a person decides |
| Art, names, numbers a player sees | Nothing automatable | Human only — the standing rules |

## Waiting on Unity

Measured in [How long Unity takes to notice a rebuilt plug-in](docs/research/unity-hot-reload-timing.md).

- **Unattended work does not stall** — an untouched editor picks up a rebuilt plug-in on its own — but the
  delay ranged from 18 seconds to 11 minutes for the same editor doing the same thing. Poll for evidence;
  never sleep a fixed interval and assume.
- **Poll `client/Logs/Editor.log` for a marker your own code emitted.** Silence means *not yet*, never
  *failed* — the log is written lazily and an idle editor can leave it hours behind.
- **If you need it now, close the editor and use batchmode.** An agent cannot force the refresh
  (`SetForegroundWindow` is refused to a background process); asking the developer to alt-tab costs about
  18 seconds.

## Where things are written down

[`docs/README.md`](docs/README.md) is the index. [`docs/vision.md`](docs/vision.md) is the standing document
and holds decisions only; [`docs/decision-log.md`](docs/decision-log.md) holds every reversal and is written,
not read — nothing an agent needs in order to act lives only there.
[`docs/agents/issue-tracker.md`](docs/agents/issue-tracker.md) is the tracker doc.

**The source carries no comments** — the code says what it does through its names, and the why lives in
[`docs/adr/`](docs/adr/), the commit message or the ticket.

**When a decision moves, edit the vision and record the reversal in the log.** Never leave a struck-through
claim, a "this used to say" aside or a dated amendment inside a standing document.
