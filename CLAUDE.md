# Agent instructions

Working rules for anything — human or agent — doing execution work in this repo.

## Working agreements

Four rules. Each exists because the obvious alternative fails quietly, which is
the failure mode that costs the most to find later.

### 1. Compile feedback comes from the project-local editor log

`client/Logs/Editor.log` is the log for this project. **Never read the global
path** — `%LOCALAPPDATA%\Unity\Editor\Editor.log` on Windows — even though it is
the one Unity's own documentation names.

The trap is that the global file *exists* and *parses*. Reading it does not
error; it answers, and the answer is about something else. Measured here on
1 Aug 2026, minutes apart: the global log was **3,350 bytes, last written at
20:22**, while the project's own log was **724,920 bytes, last written at
20:56**. Both look like an editor log. Only one of them is about this project,
and it is not the documented one. An agent that trusts the global path gets a
small, plausible, stale file and reports confidently on a compile that never
happened.

### 2. Say it in your reply when engine-side code went uncompiled

If you wrote C# that Unity has not compiled, and getting it compiled would need
the developer to alt-tab, **write that in your reply**. Do not sit and wait for
an editor to notice. Do not quietly hand back untested code either.

The point is where the friction lands. Waiting silently spends the developer's
wall-clock time invisibly — nobody ever sees the total, so nobody ever fixes it.
Saying so puts the cost in the open, in the reply, every time, until it is
either accepted or engineered away.

### 3. Every automation has a static command-line entry point

Anything an agent needs to run lives in `tools/` and runs from a shell:
`tools/run-playmode-tests.ps1`, `tools/run-editmode-tests.ps1`,
`tools/run-unity-tests.ps1`, `tools/run-player-tests.ps1`,
`tools/adopt-unity-project.ps1`,
`tools/run-headless-match.ps1`, `tools/run-parity-run.ps1`,
`tools/build-match-scene.ps1`, `tools/build-test-assets.ps1`,
`tools/sync-streaming-content.ps1`,
`tools/capture-match-frames.ps1`, `tools/capture-art-previews.ps1`,
`tools/check-file-sizes.ps1`, `tools/check-project-settings.ps1`,
`tools/build-player.ps1`.
**Nothing may depend on an editor bridge being installed** — no plug-in that has to be present
in a running editor, no socket to a live Unity, no "first open the project and
press the button".

A bridge is a dependency on a *session*, and sessions are exactly what a fresh
clone, a CI runner and an overnight agent do not have. Batchmode
(`-batchmode -executeMethod`, `-batchmode -runTests`) needs the editor closed
and works from nothing, which is why it is the path everything here is built on.

### 4. Generated files are committed beside the change that caused them

If a change causes a file to be regenerated — a lockfile, a `.meta`, a built
plug-in — that file goes in the same commit. Not a follow-up, not "it'll
regenerate".

The rule is what makes a fresh clone the same project as the one it was cloned
from. A repo whose generated files are one commit behind is a repo where the
first thing every new checkout does is produce a diff nobody asked for, and the
second thing is teach its owner to stop reading `git status`.

The corollary bites too: if a generated file must **not** be committed, that has
to be arranged by construction — an ignore rule — and not by remembering. And
ignore rules only govern untracked files. They cannot stop a *tracked* generated
file, like `client/Packages/packages-lock.json`, from recording something a
clone could never contain; that has to be watched for by hand. See
`client/.gitignore`, which carries the scars.

## Unity picks up a rebuilt plug-in on its own, but not promptly

**You do not need the developer to alt-tab.** An editor that nobody touches
does eventually notice a rebuilt managed plug-in, reimport it and reload the
domain, with no keyboard or mouse input anywhere on the machine.

**It is the focus *transition* that triggers it, not the focus *state*.** That
one distinction reconciles three results which otherwise contradict each other,
and it is why "does it recompile on focus alone?" is the wrong question — it
asks about a state. All three halves were measured directly:

- an editor **minimised and provably untouched** — not the foreground window,
  and using 0.02 s of CPU per 5 s wall, so genuinely asleep — picked the
  rebuild up anyway, three times out of three;
- an editor **holding the foreground continuously**, burning ~200% CPU, took
  **nearly fifteen minutes** to notice a rebuild sitting on disk;
- an editor **alt-tabbed onto** began refreshing in **a third of a second**,
  twice out of two.

Holding focus buys nothing. *Arriving* at focus buys everything.

The delay is minutes, and it is not a fixed interval. Measured on this machine
(Windows 11, Unity `6000.5.6f1`, `client/`, .NET SDK 10.0.101), on 1 Aug 2026,
using `tools/hotreload-probe/`. Times are the **editor's own clock**, taken from
the stamp the probe prints during the domain reload, not from when the line
became readable:

| Editor state | plug-in rebuilt at | domain reload ran at | delay |
| --- | --- | --- | --- |
| minimised, untouched | 20:23:56.5 | 20:24:14.7 | **18 s** |
| minimised, untouched | 20:25:06.0 | 20:30:00.8 | **4 min 55 s** |
| minimised, untouched | 20:30:48.4 | 20:41:52.4 | **11 min 04 s** |
| focused, foreground | 20:08:26.7 | 20:12:03.4 | **3 min 37 s** |
| focused, foreground | 20:14:02.7 | 20:16:06.6 | **2 min 04 s** |
| focused, foreground | 20:17:35.7 | 20:32:22.6 | **14 min 47 s** |

And the attended case, measured 2 Aug 2026 with `probe.ps1 -Trial`: a human at
the keyboard alt-tabbing onto the editor in the seconds after each rebuild. The
refresh start is derived from the refresh record's own `Total`, less the phases
that run after the reload is logged.

| # | rebuilt | alt-tabbed | refresh started | **alt-tab → refresh** | domain reload | rebuild → reload |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 21:51:29.4 | 21:51:33.3 | 21:51:33.6 | **0.34 s** | 21:51:48.4 | 19.0 s |
| 2 | 21:51:53.0 | 21:51:56.3 | 21:51:56.5 | **0.17 s** | 21:52:10.3 | 17.3 s |

The reimport itself, once it starts, is not the slow part: the asset refreshes
took 15–27 s wall, of which 10–15 s was Unity recompiling script assemblies
that depend on the plug-in. `dotnet build` on the probe stub is ~0.9 s. So
essentially all of the loop time is Unity deciding to look, not Unity working —
**and an alt-tab removes that deciding entirely.**

### What follows for an agent

- **Unattended background work does not stall.** The no-bridge working
  agreement — every automation reachable from a static command-line entry
  point, nothing depending on an editor bridge — holds for background work as
  well as attended work. It was going to be recorded as void for background
  work if refresh had needed a human. It does not, so it is not.
- **Never assume a fixed wait.** 18 seconds and 11 minutes were the same
  editor, minimised and untouched, doing the same thing. Poll for evidence; do
  not sleep and hope.
- **Activating the window does force it — and you still cannot do it.** A human
  alt-tab starts the refresh in ~0.25 s, so the trigger is real. But
  `SetForegroundWindow` from a background process is refused, confirmed twice on
  this machine, including with the desktop idle for 204 s. The one thing that
  would make the loop instant is the one thing an agent cannot reach. If you
  need a rebuild picked up on demand, use batchmode rather than poking a running
  editor — or ask the developer to alt-tab, which now has a known price.
- **Poll the project-local log**, `client/Logs/Editor.log`, for a marker your
  own code emitted. Silence there means *not yet*, and never means *failed*.
- **If you need it now, do not wait for the editor at all.** Close it and use
  `-batchmode -executeMethod`, which imports on startup unconditionally. An
  open editor holds an exclusive lock on the project, so batchmode needs it
  closed. That is the reliable path, and it is the one the working agreement
  already points at.
- **Say so in your reply** when you leave engine-side code uncompiled, rather
  than waiting silently — so the friction accumulates where it is visible.

### The log is buffered, and that will mislead you

`client/Logs/Editor.log` is written lazily. Bytes reach the file when the editor
next does some work, not when the event happened, and an idle editor can leave
its log hours behind. Measured here: at 20:08 the log of a session that had
started at **18:32** was 38 KB and stopped part-way through that startup; four
minutes later it was 217 KB, and the newly-arrived bytes described the 18:32
startup. Nearly two hours of already-finished work had never been flushed.

Two consequences, both of which nearly produced a wrong answer in this ticket:

- **A quiet log does not mean nothing happened.** It very often means the
  editor has not been busy enough to flush. Do not read silence as failure, and
  do not read it as "still running" either.
- **Never time anything by when a line became readable.** That measures your
  poll loop and Unity's flush, not the event. Anything you need a real
  timestamp for must carry its own — the probe prints the editor's own clock
  inside its message, which is the only reason the table above is trustworthy.
  In one trial the reload had already run **two seconds before** the action
  that appeared to cause it; only the embedded clock showed it.

Checking the file's size rather than its content buys you nothing here:
`(Get-Item …).Length` agrees with the open handle's length. The lag is Unity's
buffer, not Windows' metadata.

### How this was measured, and what is still open

Two editors were run at once: the developer's, and one launched by the agent on
a separate worktree that no human ever touched. The second exists because a
background process on Windows **cannot take the foreground while a human is at
the keyboard** — `SetForegroundWindow` is refused — so a trial run against a
live editor cannot attribute a refresh to the thing that was meant to cause it.
Every trial recorded the system-wide idle time from `GetLastInputInfo`, so a
human touching the machine mid-trial shows up in the record instead of silently
becoming the explanation.

**The alt-tab variant is now measured too**, by
[#51](https://github.com/ssalter21/tower-defense-game/issues/51), with
`tools/hotreload-probe/probe.ps1 -Trial`. #34 could not get it because Windows
refuses `SetForegroundWindow` to a background process — re-tested on this
machine with the desktop idle for 204 s and the target window minimised, the
call returned `false` and the foreground did not move. So the alt-tab is a
human's and `-Trial` takes everything else: it will not rebuild while the
editor holds the foreground, it timestamps the transition by watching for it,
and it decides from the refresh **start** whether the trial counts at all.

**Alt-tabbing starts the refresh in about a quarter of a second** — 0.34 s and
0.17 s. That is not "sooner", it is immediate: the editor is waiting for the
transition, and the 18 s – 11 min it otherwise takes is entirely the cost of
nobody supplying one.

**So [#5](https://github.com/ssalter21/tower-defense-game/issues/5)'s "3-second
loop or 40-second one" has its answer, and it is neither: the attended loop is
17–19 seconds.** Of that, ~1 s is `dotnet build`, ~0.25 s is the alt-tab
landing, and **~15 s is Unity recompiling the script assemblies that reference
the plug-in** — about 12 s of it `CompileScripts` alone. Being at the keyboard
buys back the waiting, not the compile. The compile is the floor, and it is the
only thing left worth attacking if the loop ever needs to be faster.

The first trial run produced five voids before two valid ones, and the reason
is worth keeping: the alt-tab window is **about six seconds**. On this machine
an untouched editor began refreshing 5.4, 6.3 and 7.9 s after a rebuild, so a
human who reads the prompt before moving has already lost the trial.

Also still open, and not worth blocking on: what actually schedules the check,
and why the same editor went 18 seconds once and 11 minutes the next time. The
decision this answers — can an agent work while the developer is away — does
not depend on either.
