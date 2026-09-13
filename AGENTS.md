# Agent instructions

Working rules for anything — human or agent — doing execution work in this repo. Six rules, each because the
obvious alternative fails quietly. **This file is loaded into every agent's context, so it holds instructions
only**: a finding goes in [`docs/research/`](docs/research/), a reference in [`docs/`](docs/README.md).

## 1. Compile feedback comes from the project-local editor log

- Read `client/Logs/Editor.log`. Leave `%LOCALAPPDATA%\Unity\Editor\Editor.log` alone even though Unity's
  documentation names it: it parses, and it is a different compile — 3 KB there against 725 KB in the
  project's own, minutes apart.

## 2. Say it in your reply when engine-side code went uncompiled

- Wrote C# that Unity has not compiled? Say so in your reply, because no editor will notice for you and
  untested code handed back silently is the failure.
- Run the Unity tests yourself, editor closed — `run-editmode-tests.ps1`, `run-playmode-tests.ps1` or
  `run-unity-tests.ps1` — because `dotnet test sim.tests` is everything CI runs.
- Expect `check-unity-test-inventory.ps1` to pass a changed test or view file unbuilt: it counts the client's
  tests and compiles nothing.
- Treat `nightly-unity.ps1` (registered by `register-nightly-unity.ps1`; 03:00; one line per runner to
  `client/Logs/nightly.log`) as a catch, not a substitute: it tests one checkout the morning after.

## 3. Every automation has a static command-line entry point

- Put anything an agent needs to run in `tools/`, runnable from a shell — `ls tools/*.ps1`; every script's
  header says what it is for.
- Depend on nothing that needs a running editor — no plug-in that must be present, no socket to a live Unity,
  no "open the project and press the button" — because a session is what a fresh clone, a CI runner and an
  overnight agent do not have. Batchmode (`-batchmode -executeMethod`, `-batchmode -runTests`) needs the
  editor closed and works from nothing.
- Keep the board's editor menus — `Tools > Board > Edit Map`, `Scenery`, `Dress` — as `public static void`
  methods with no arguments, `content/map.txt` and `content/dressing.txt` the artefacts. What each does, and
  the `model`/`place` verbs, are [the board tools](docs/board-tools.md).

## 4. Generated files are committed beside the change that caused them

- Commit whatever a change regenerates — a lockfile, a `.meta`, a built plug-in — in the same commit, so a
  fresh clone is the same project. Not a follow-up, not "it'll regenerate".
- Exclude a generated file that must **not** be committed with an ignore rule, never by remembering. Ignore
  rules govern untracked files only, so watch a *tracked* one like `client/Packages/packages-lock.json` by
  hand; `client/.gitignore` carries the scars.

## 5. A worktree is finished when `git worktree list` stops naming it

- Close the editor before `git worktree remove`: the command unregisters and then deletes, an open Unity
  holds `client/Library`, the delete fails, and what remains is a directory under `.claude/worktrees/` with no
  `.git` file that neither `list` nor `prune` can see (five accumulated here, about 130,000 files).
- Check for orphans: anything in `ls .claude/worktrees` and not in `git worktree list`.
- Delete the local branch once its pull request merges — `git branch --merged origin/main` lists them.

## 6. What verifies an area decides how much of it an agent may do unattended

Match the autonomy to what would actually catch the change being wrong, because the gate proves the
simulation and only counts the client's tests (rule 2): a visual change held to the simulation's standard
passes every check on an artefact nobody has looked at. The evidence each row rests on is
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

- Poll `client/Logs/Editor.log` for a marker your own code emitted, never a fixed sleep: an untouched editor
  picks up a rebuilt plug-in on its own, in anything from 18 seconds to 11 minutes for the same editor doing
  the same thing.
- Read silence as *not yet*, never *failed* — the log is written lazily and an idle editor can leave it hours
  behind.
- Need it now? Close the editor and use batchmode. An agent cannot force the refresh
  (`SetForegroundWindow` is refused to a background process); asking the developer to alt-tab costs about
  18 seconds.

## Where things are written down

- [`docs/README.md`](docs/README.md) is the index. [`docs/vision.md`](docs/vision.md) is the standing
  document and holds decisions only; [`docs/decision-log.md`](docs/decision-log.md) holds every reversal and
  is written, not read — nothing an agent needs in order to act lives only there.
  [`docs/agents/issue-tracker.md`](docs/agents/issue-tracker.md) is the tracker doc.
- Write no comments in the source: the code says what it does through its names, and the why lives in
  [`docs/adr/`](docs/adr/), the commit message or the ticket.
- When a decision moves, edit the vision and record the reversal in the log. A standing document carries no
  struck-through claim, "this used to say" aside or dated amendment.
