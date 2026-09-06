# A run somebody played

**These are screen captures of `client/Builds/Windows/TowerDefense.exe`, not renders.** Everything else under
[`docs/frames/`](../README.md) is drawn by a batchmode capture tool through a scene the tool assembles. These
ten are the built player's own back buffer, grabbed off the desktop while a run was being driven by synthetic
mouse and keyboard input — so what is in them is what a person double-clicking the executable sees, chrome
included.

**The run.** Ten waves on the committed board and the shipped content, played on 6 September 2026 in the
`afk-roster-expansion` worktree — **not** in the developer's own checkout. One tower of each of the nine lines
went down over waves 1 to 3; the three capstone tokens bought Consecration at wave 3, the Mortar at wave 6 and
the Fan of Knives at wave 9; waves 9 and 10 each sent all seventeen creeps. The run finished
**10 waves survived, 717 of 800 health left, 962 dealt over 10 rounds**, the session agreed with a fresh replay
of its own script, and the player log carried no exception.

Every frame is 1600×900, the size the player was run at, **except** the four the table calls a close: those are
crops of a frame that size, enlarged so a shape twenty pixels across can be seen at all.

## The frames

| | |
|---|---|
| [`build-phase.png`](build-phase.png) | Wave 9 being composed: nine lines standing, three of them capstones, and the seventeen-creep wave in the bar. The bar runs off the right edge: ten boxes fit, the eleventh is cut in half, and the trailing box a creep is added through is somewhere past 3000 pixels. |
| [`the-nine-lines-close.png`](the-nine-lines-close.png) | The same frame, the tower row at 3×. Several of them stand with their arms straight out and their weapon pointing sideways — the Paladin at the back most plainly, and the Druid carrying its staff flat across its chest. |
| [`capstone-consecration.png`](capstone-consecration.png) | Wave 9's defence at tick 76. The Consecration's disc of light is the cream circle; it reaches three hexes in every direction, it is up for 26 ticks in every 30, and it runs off the south edge of the board into the background. |
| [`capstone-mortar.png`](capstone-mortar.png) | Wave 9's defence at tick 2071. The Mortar's burst, centred on the body its shell arrived at. |
| [`capstone-fan-of-knives.png`](capstone-fan-of-knives.png) | Wave 10's defence, the Fan of Knives at 4×. Two knives are in the air — one over its shoulder, one arriving at a Minion. At 1× a knife is about twenty pixels of pale grey on a bright board, which is why the shape had to be enlarged to be photographed at all. |
| [`resolution.png`](resolution.png) | Wave 9's offence at tick 1264 — the whole seventeen walking an opponent's defence. Four effects at once: the Frost Wight's spikes, the Witch's hex plates, two of the Skeleton Mage's haste rings, and a defence tower wearing frost. |
| [`black-knight-beside-towers.png`](black-knight-beside-towers.png) | Wave 9's offence at 3×. The Black Knight stands beside four defence towers and is as tall as they are. This is what `EveryCreepStandsUnmistakablyLowerThanEveryTower` is red about. |
| [`a-slow-nobody-can-see.png`](a-slow-nobody-can-see.png) | A second, shorter run: a Shield Wall slowing an opponent's wave at wave 3, at the framing the game ships. The ring reads; the slowed bodies do not. |
| [`the-same-slow-close.png`](the-same-slow-close.png) | The same frame at 3×, where the slowed bodies are plainly blue and the unslowed ones plainly white. |
| [`run-over.png`](run-over.png) | The end frame, and where the script went. |

## What dates these, and what does not

`tools/check-docs.ps1` names them exempt from the invariant that dates a picture against the content it draws,
and the reason is in that file beside the six roster sheets it exempts for reasons of their own: **a
photograph of a session is dated by the session.** Re-capturing one is playing a different run — a different
seed's worth of arrival order, a different tick under the pointer — so asking for a re-capture asks for
something nobody can produce, and the picture that came back would not be this picture.

What that costs is real and worth stating: **nothing here will notice when these go stale.** A price moving in
`content/units.txt` makes the palette in `build-phase.png` a picture of a game this repository no longer
builds, and only a person reading it will see that. It is the same cost, and the same answer, as the roster
sheets carry.

## Reproducing them

There is no tool, and [the research note](../../research/driving-the-built-player-with-synthetic-input.md) is
where the reasoning and the four things a driver has to get right are written down — chiefly that
`SetCursorPos` moves the pointer without the game ever seeing it, which reads as a broken hover.

