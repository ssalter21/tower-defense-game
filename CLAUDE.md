# Agent instructions

Working rules for anything — human or agent — doing execution work in this repo.

## Unity picks up a rebuilt plug-in on its own, but not promptly

**You do not need the developer to alt-tab.** An editor that nobody touches
does eventually notice a rebuilt managed plug-in, reimport it and reload the
domain, with no keyboard or mouse input anywhere on the machine.

**Focus is not what triggers it**, so the question "does it recompile on focus
alone?" has a misleading shape. Both halves were measured directly:

- an editor **minimised and provably untouched** — not the foreground window,
  and using 0.02 s of CPU per 5 s wall, so genuinely asleep — picked the
  rebuild up anyway, three times out of three;
- an editor **holding the foreground continuously**, burning ~200% CPU, took
  **nearly fifteen minutes** to notice a rebuild sitting on disk.

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

The reimport itself, once it starts, is not the slow part: the asset refreshes
took 15–27 s wall, of which 10–15 s was Unity recompiling script assemblies
that depend on the plug-in. `dotnet build` on the probe stub is ~0.9 s. So
essentially all of the loop time is Unity deciding to look, not Unity working.

### What follows for an agent

- **Unattended background work does not stall.** The no-bridge working
  agreement — every automation reachable from a static command-line entry
  point, nothing depending on an editor bridge — holds for background work as
  well as attended work. It was going to be recorded as void for background
  work if refresh had needed a human. It does not, so it is not.
- **Never assume a fixed wait.** 18 seconds and 11 minutes were the same
  editor, minimised and untouched, doing the same thing. Poll for evidence; do
  not sleep and hope.
- **Do not count on forcing it by activating the window.** Twice the editor was
  deliberately restored and brought to the foreground over a rebuild that had
  provably been sitting unnoticed for minutes. Both times the refresh had
  already started ~18 s *earlier* on its own, so neither attempt is evidence
  that activating helps — or that it does not. If you need a rebuild picked up
  on demand, use batchmode rather than trying to poke a running editor.
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

Still open, and not worth blocking on: what actually schedules the check, and
why the same editor went 18 seconds once and minutes the next time. The
decision this answers — can an agent work while the developer is away — does
not depend on it.
