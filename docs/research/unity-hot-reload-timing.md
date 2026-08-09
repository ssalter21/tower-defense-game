# How long Unity takes to notice a rebuilt plug-in

**Research note** · measured 1–2 August 2026 · resolves
[#5](https://github.com/ssalter21/tower-defense-game/issues/5),
[#34](https://github.com/ssalter21/tower-defense-game/issues/34) and
[#51](https://github.com/ssalter21/tower-defense-game/issues/51)

**Question:** the sim is a managed plug-in built outside Unity. When it is rebuilt, how long before the editor
notices — and does an agent working while nobody is at the keyboard get stuck waiting?

> **This note was the back half of `CLAUDE.md` until 7 August 2026.** It is a measurement, not an instruction,
> and it was moved here so the agent file could stay short. The four rules that *are* instructions live in
> [`AGENTS.md`](../../AGENTS.md); the ones this note produced are quoted there in a sentence each.

---

## Verdict

**Unity picks up a rebuilt plug-in on its own, but not promptly — and an agent does not need the developer to
alt-tab.** An editor that nobody touches does eventually notice a rebuilt managed plug-in, reimport it and
reload the domain, with no keyboard or mouse input anywhere on the machine. The delay is minutes and it is not
a fixed interval.

**It is the focus *transition* that triggers it, not the focus *state*.** That one distinction reconciles
three results which otherwise contradict each other, and it is why "does it recompile on focus alone?" is the
wrong question — it asks about a state. All three halves were measured directly:

- an editor **minimised and provably untouched** — not the foreground window, and using 0.02 s of CPU per 5 s
  wall, so genuinely asleep — picked the rebuild up anyway, three times out of three;
- an editor **holding the foreground continuously**, burning ~200% CPU, took **nearly fifteen minutes** to
  notice a rebuild sitting on disk;
- an editor **alt-tabbed onto** began refreshing in **a third of a second**, twice out of two.

Holding focus buys nothing. *Arriving* at focus buys everything.

## The measurements

Measured on this machine (Windows 11, Unity `6000.5.6f1`, `client/`, .NET SDK 10.0.101) on 1 August 2026, using
`tools/hotreload-probe/` — a throwaway three-line stub project, deleted once it had answered this. Times are the
**editor's own clock**, taken from the stamp the probe printed during the domain reload, not from when the line
became readable:

| Editor state | plug-in rebuilt at | domain reload ran at | delay |
| --- | --- | --- | --- |
| minimised, untouched | 20:23:56.5 | 20:24:14.7 | **18 s** |
| minimised, untouched | 20:25:06.0 | 20:30:00.8 | **4 min 55 s** |
| minimised, untouched | 20:30:48.4 | 20:41:52.4 | **11 min 04 s** |
| focused, foreground | 20:08:26.7 | 20:12:03.4 | **3 min 37 s** |
| focused, foreground | 20:14:02.7 | 20:16:06.6 | **2 min 04 s** |
| focused, foreground | 20:17:35.7 | 20:32:22.6 | **14 min 47 s** |

And the attended case, measured 2 August 2026 with `probe.ps1 -Trial`: a human at the keyboard alt-tabbing onto
the editor in the seconds after each rebuild. The refresh start is derived from the refresh record's own
`Total`, less the phases that run after the reload is logged.

| # | rebuilt | alt-tabbed | refresh started | **alt-tab → refresh** | domain reload | rebuild → reload |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 21:51:29.4 | 21:51:33.3 | 21:51:33.6 | **0.34 s** | 21:51:48.4 | 19.0 s |
| 2 | 21:51:53.0 | 21:51:56.3 | 21:51:56.5 | **0.17 s** | 21:52:10.3 | 17.3 s |

The reimport itself, once it starts, is not the slow part: the asset refreshes took 15–27 s wall, of which
10–15 s was Unity recompiling script assemblies that depend on the plug-in. `dotnet build` on the probe stub is
~0.9 s. So essentially all of the loop time is Unity deciding to look, not Unity working — **and an alt-tab
removes that deciding entirely.**

**So [#5](https://github.com/ssalter21/tower-defense-game/issues/5)'s "3-second loop or 40-second one" has its
answer, and it is neither: the attended loop is 17–19 seconds.** Of that, ~1 s is `dotnet build`, ~0.25 s is the
alt-tab landing, and **~15 s is Unity recompiling the script assemblies that reference the plug-in** — about
12 s of it `CompileScripts` alone. Being at the keyboard buys back the waiting, not the compile. The compile is
the floor, and it is the only thing left worth attacking if the loop ever needs to be faster.

## What follows for an agent

- **Unattended background work does not stall.** [`AGENTS.md`](../../AGENTS.md) rule 3 — every automation
  reachable from a static command-line entry point, nothing depending on an editor bridge — holds for
  background work as well as attended work. It was going to be recorded as void for background work if refresh
  had needed a human. It does not, so it is not.
- **Never assume a fixed wait.** 18 seconds and 11 minutes were the same editor, minimised and untouched, doing
  the same thing. Poll for evidence; do not sleep and hope.
- **Activating the window does force it — and you still cannot do it.** A human alt-tab starts the refresh in
  ~0.25 s, so the trigger is real. But `SetForegroundWindow` from a background process is refused, confirmed
  twice on this machine, including with the desktop idle for 204 s. The one thing that would make the loop
  instant is the one thing an agent cannot reach.
- **If you need it now, do not wait for the editor at all.** Close it and use `-batchmode -executeMethod`, which
  imports on startup unconditionally. An open editor holds an exclusive lock on the project, so batchmode needs
  it closed. That is the reliable path, and it is the one rule 3 already points at.
- **Or ask the developer to alt-tab**, which now has a known price: about 18 seconds, against minutes of
  waiting.

## The log is buffered, and that will mislead you

`client/Logs/Editor.log` is written lazily. Bytes reach the file when the editor next does some work, not when
the event happened, and an idle editor can leave its log hours behind. Measured here: at 20:08 the log of a
session that had started at **18:32** was 38 KB and stopped part-way through that startup; four minutes later it
was 217 KB, and the newly-arrived bytes described the 18:32 startup. Nearly two hours of already-finished work
had never been flushed.

Two consequences, both of which nearly produced a wrong answer in this ticket:

- **A quiet log does not mean nothing happened.** It very often means the editor has not been busy enough to
  flush. Do not read silence as failure, and do not read it as "still running" either.
- **Never time anything by when a line became readable.** That measures your poll loop and Unity's flush, not
  the event. Anything you need a real timestamp for must carry its own — the probe printed the editor's own
  clock inside its message, which is the only reason the tables above are trustworthy. In one trial the reload
  had already run **two seconds before** the action that appeared to cause it; only the embedded clock showed
  it.

Checking the file's size rather than its content buys you nothing here: `(Get-Item …).Length` agrees with the
open handle's length. The lag is Unity's buffer, not Windows' metadata.

## How this was measured, and what is still open

Two editors were run at once: the developer's, and one launched by the agent on a separate worktree that no
human ever touched. The second exists because a background process on Windows **cannot take the foreground
while a human is at the keyboard** — `SetForegroundWindow` is refused — so a trial run against a live editor
cannot attribute a refresh to the thing that was meant to cause it. Every trial recorded the system-wide idle
time from `GetLastInputInfo`, so a human touching the machine mid-trial shows up in the record instead of
silently becoming the explanation.

**The alt-tab variant was measured by [#51](https://github.com/ssalter21/tower-defense-game/issues/51)**, with
`probe.ps1 -Trial`. #34 could not get it because Windows refuses `SetForegroundWindow` to a background process —
re-tested on this machine with the desktop idle for 204 s and the target window minimised, the call returned
`false` and the foreground did not move. So the alt-tab is a human's and `-Trial` took everything else: it would
not rebuild while the editor held the foreground, it timestamped the transition by watching for it, and it
decided from the refresh **start** whether the trial counted at all.

The first trial run produced five voids before two valid ones, and the reason is worth keeping: the alt-tab
window is **about six seconds**. On this machine an untouched editor began refreshing 5.4, 6.3 and 7.9 s after a
rebuild, so a human who reads the prompt before moving has already lost the trial.

Also still open, and not worth blocking on: what actually schedules the check, and why the same editor went
18 seconds once and 11 minutes the next time. The decision this answers — can an agent work while the developer
is away — does not depend on either.
