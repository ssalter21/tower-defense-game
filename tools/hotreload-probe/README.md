# Hot-reload probe (throwaway)

Measures how long Unity takes to notice a rebuilt managed plug-in.

## Why this exists

[#5](https://github.com/ssalter21/tower-defense-game/issues/5) calls the DLL
edit/rebuild/see-it loop **"the single highest-value thing for the walking
skeleton to establish in its first hour — the difference between a 3-second
loop and a 40-second one."**
[#15](https://github.com/ssalter21/tower-defense-game/issues/15) asks
[#10](https://github.com/ssalter21/tower-defense-game/issues/10) to record it,
noting it is *"only measurable once Unity exists."*

But there is no `sim/` project yet — building one is the execution effort, not
this map — so there is nothing to rebuild, and the question would otherwise sit
unanswered until the first execution session.

**Unity's reimport behaviour does not depend on what is inside an assembly.** A
three-line stub answers the timing question identically, provided it sits in the
same *kind* of place the real sim will. So this probe is:

- **`netstandard2.1`, no engine reference** — as [#5](https://github.com/ssalter21/tower-defense-game/issues/5) decided for the sim
- **an embedded UPM package** under `client/Packages/` — as [#15](https://github.com/ssalter21/tower-defense-game/issues/15) decided, and *not* `Assets/Plugins/`, because Unity can treat package contents differently from assets and that difference is part of what is being measured
- **not called `Sim`** — so it can never be mistaken for the real plug-in

`client/.gitignore` refuses to commit any of the generated **files**.

> **It leaked anyway, once.** That sentence used to end "the probe cannot leak
> into the repo by accident", and [#29](https://github.com/ssalter21/tower-defense-game/issues/29)
> found it was false. `client/Packages/packages-lock.json` is **tracked**, and
> Unity records the probe in it as an embedded dependency the moment it is
> installed. A `.gitignore` has no authority over what a tracked file says
> about an ignored one. The probe was installed when the Unity project was
> first committed, so the lockfile shipped naming a package no clone could
> ever contain — and every clone since has had Unity strip it straight back
> out, producing a diff on the very first run.
>
> **So: while the probe is installed, `packages-lock.json` will show a diff.
> That is expected. Never commit it.** After `-Remove`, let Unity refresh once
> and the lockfile returns to clean on its own.

## Use

Run **after** the Unity project exists at `client/`. All three commands are
safe to re-run.

```powershell
./tools/hotreload-probe/probe.ps1 -Install   # once
./tools/hotreload-probe/probe.ps1 -Bump      # the measurement, repeat freely
./tools/hotreload-probe/probe.ps1 -Trial     # the attended measurement (#51)
./tools/hotreload-probe/probe.ps1 -Remove    # deletes every trace
```

`-Install` creates the package, builds it, and drops an Editor script that logs
the plug-in's build stamp on **every domain reload**. `-Bump` rebuilds with a
fresh stamp — that is the thing to time.

## `-Trial`: the one measurement a human has to be present for

[#51](https://github.com/ssalter21/tower-defense-game/issues/51) wants the one
variant [#34](https://github.com/ssalter21/tower-defense-game/issues/34) could
not get: **a human alt-tabbing onto the editor in the seconds after a rebuild.**
An agent cannot stage it — Windows refuses `SetForegroundWindow` to a background
process, which was re-confirmed on this machine with the desktop idle for over
three minutes. So the keyboard part is yours. Everything around it is not.

```powershell
./tools/hotreload-probe/probe.ps1 -Trial     # alt-tab when it says to, twice
```

#34 burned two of its trials on four traps. Each is now structural rather than
remembered:

| The trap | What `-Trial` does about it |
| --- | --- |
| A continuously-focused editor never sees a transition | Refuses to rebuild until the editor is out of the foreground |
| You time a refresh that started without you | Derives the refresh **start** and calls the trial **void**, then retries |
| A quiet log is not proof it has not been picked up | Never asks the log to prove a negative — decides afterwards, from the start time |
| Timing by when a line became readable | Reads the editor's own clock out of the probe's message |

**Void is not negative.** A trial where the refresh was already under way is
discarded and re-run; only trials where the alt-tab came first are counted.

The alt-tab is timed by watching for the editor actually taking the foreground,
so there is no stopwatch to hold. System idle time is recorded at that instant
too: near zero is what says a human did it, and the run says so if it was not.

Results print as a table and are written to `trials.md` **inside the package**,
so `-Remove` takes them with everything else and `git status` stays clean.

`TrialLog.psm1` holds the log reading, and `TrialLog.Tests.ps1` holds it against
`fixtures/three-trials.log` — a verbatim slice of the real `Editor.log` from the
#34 session, whose numbers are already published in `CLAUDE.md`. Run it with:

```powershell
./tools/hotreload-probe/TrialLog.Tests.ps1
```

The Editor script reads the stamp by **reflection**, not by a direct type
reference. A direct reference would turn *"Unity never loaded the plug-in"* into
a red compile error, which is a confusing way to learn something. This way that
outcome prints in the Console as a legible result.

It also adds **Tools ▸ Hot-reload probe ▸ Print stamp now**, for checking which
build is currently loaded without forcing a reload.

## Answered

Questions 1 and 2 below were run and recorded, in
[#34](https://github.com/ssalter21/tower-defense-game/issues/34) and
[#51](https://github.com/ssalter21/tower-defense-game/issues/51). The short
version: **it is the focus *transition* that triggers it, not the focus
*state*.** An untouched, minimised editor picks the rebuild up by itself in
anything from 18 s to 11 min; an editor *holding* the foreground took nearly
fifteen minutes to do the same; an editor **alt-tabbed onto** starts refreshing
in about a quarter of a second. The attended loop is **17–19 s end to end**,
almost all of it Unity recompiling the assemblies that reference the plug-in.
The measured timings, and what they mean for an agent, are in `CLAUDE.md` at the
repo root. Questions 3 and 4 are still unrun.

## What to record on [#10](https://github.com/ssalter21/tower-defense-game/issues/10)

1. Does the new stamp appear **on focus alone**, with no other action?
2. Seconds from alt-tab to the Console line.
3. Same again with **Play mode running** — does it reload, refuse to, or does
   `dotnet build` fail with access-denied because Unity is holding the DLL?
   [#15](https://github.com/ssalter21/tower-defense-game/issues/15) predicts the
   third and lists "stop Play mode" as the fix; worth confirming.
4. If nothing happens: does **Ctrl+R** (Assets ▸ Refresh) do it, or is a full
   **Editor restart** needed? Which one was actually required is the answer.

For reference, `dotnet build` on this stub takes **~0.8 s incremental** on this
machine (measured 1 Aug 2026, .NET SDK 10.0.101), consistent with the ~0.8 s
[#5](https://github.com/ssalter21/tower-defense-game/issues/5) measured for a
`netstandard2.1` build. So anything beyond a second or so in the loop is
Unity's, not the compiler's — which is the whole point of measuring.

## Then delete it

`-Remove`, and drop this directory. It has one job and it is not part of the
skeleton.
