# Driving the built player with synthetic input

**Research note** · measured 6 September 2026 · resolves
[#270](https://github.com/ssalter21/tower-defense-game/issues/270)

**Question:** a ticket asks for a whole run played in `client/Builds/Windows/TowerDefense.exe` and photographed.
Can an agent with no keyboard and no person at the machine drive that window itself, and what does it have to
get right?

---

## Verdict

**Yes, and two of the four things it has to get right will otherwise read as bugs in the game.** A run of ten
waves — nine towers placed, three capstones bought, a seventeen-creep wave composed, both directions of every
round watched — was driven end to end from PowerShell on 6 September 2026, and the frames are in
[`docs/frames/played-run/`](../frames/played-run/README.md).

**The window is found by enumerating the process's windows and taking the visible one with a title**; it is
called `client`, after `productName` in `ProjectSettings.asset`. `SetForegroundWindow` is refused to a
background process, as [the hot-reload note](unity-hot-reload-timing.md) already records, so the window is
raised with `SetWindowPos(HWND_TOPMOST)` and given the focus by a click on the empty sky above the board.

## The four things

**1. `SetCursorPos` does not inject an input event.** It moves the pointer and nothing else, so Unity's Input
System never sees the move: `Mouse.current.position` keeps the value it had. What that looks like is a game
that has stopped hovering — the hex under the pointer never lights, `BuildInput.Point` is answering about a
stale position — while clicks still land in the right place, because `mouse_event` samples the cursor as it
injects the button. **An hour went into that discrepancy on the assumption that the build light was broken.**
The fix is to inject the move as well:

```
mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK, nx, ny, 0, 0)
```

**2. Without `MOUSEEVENTF_VIRTUALDESK` the absolute coordinates are read against the primary display alone.**
On the two-monitor desk this was measured on — two 1920×1080 side by side, virtual desktop 3840 wide — every
injected x landed at half its intended value. The normalisation is over the virtual desktop's own bounds,
`GetSystemMetrics(SM_XVIRTUALSCREEN, SM_YVIRTUALSCREEN, SM_CXVIRTUALSCREEN, SM_CYVIRTUALSCREEN)`.

**3. The player is built with `runInBackground: 0`**, so it stops stepping the moment it loses focus. A burst
of `Graphics.CopyFromScreen` grabs is safe because nothing there takes the focus; starting a process that
raises a console is not. A hundred frames captured while the window was behind another one are a hundred
copies of one tick, and the tell is the playback bar reading `Play` rather than `Pause`.

**4. The window is not user-resizable and `SetWindowPos` resizes it anyway.** `resizableWindow: 0` stops a hand
dragging the frame; it does not stop an outside call, and the player reconfigures live — **the run survives**.
That is what made a seventeen-creep wave composable at all: `WaveBar` lays its boxes out at a fixed width with
`flexShrink: 0`, so the trailing box a creep is added through leaves a 1600-wide window once ten are down. The
wave was composed at 3400×911 and the window put back to 1600×900 before the round was committed.

## Reading the pictures back

The lit hex is `MatchTuning.BuildLightColor`, which renders at about `(135, 196, 229)` and is a colour nothing
else on the board wears. Sampling a box around the pointer for it and taking the centroid gives the hex's
centre on screen, which is a better place to click than the probe point — so placements are made by hovering
candidates until one lights and clicking the middle of what lit. That is the whole of how nine towers were
placed without anybody knowing where a cell is on screen.

## What this does not give the project

**No tool in `tools/`, and deliberately.** [AGENTS.md](../../AGENTS.md) rule 3 asks every automation for a
static command-line entry point and forbids depending on a session; driving a window with synthetic input **is**
a session dependency — it needs a desktop, a foreground, and nothing else stealing the focus — so a committed
`drive-player.ps1` would satisfy the letter of that rule while being the thing it was written against. A
capture that has to be reproduced belongs in batchmode, which is what `capture-match-frames.ps1` and
`capture-ui-previews.ps1` already are. What synthetic input is for is the question those cannot answer:
whether the shipped executable, with its own chrome and its own input path, actually plays.
