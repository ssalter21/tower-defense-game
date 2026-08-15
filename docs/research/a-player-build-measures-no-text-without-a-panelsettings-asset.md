# A player build measures no text without a PanelSettings asset

**Research note** · measured 14–15 August 2026 · resolves
[#210](https://github.com/ssalter21/tower-defense-game/issues/210)

**Question:** a standalone build of this client drew no chrome at all — no palette, no wave row, no run header,
a HUD about twenty pixels tall — while the same commit under the editor's Play mode drew all of it. What is
different about a player, and what is the smallest thing that fixes it?

---

## Verdict

**Unity's advanced text generator shapes and measures every string through ICU data, and a player gets that
data only when it *loads* a `PanelSettings` **asset**.** The editor process has it loaded regardless, so
Play mode is immune by construction — the difference has nothing to do with what the code does.

Note the verb. Unity's own error says to make sure the build *contains* such an asset, and containing one is
not sufficient: a build carrying the asset with nothing loading it fails exactly as a build with no asset does.
That was measured, and it is the single most surprising thing in this note.

This project built every panel's settings with `ScriptableObject.CreateInstance<PanelSettings>()` and committed
no such asset. In a player that means every string fails to measure, every label resolves to zero by zero, each
bar collapses, and what is drawn stops agreeing with what is hit-tested. Unity states the requirement itself,
in `Player.log`:

```
ICU Data not available. The data should be automatically assigned to the PanelSettings in the editor if the
advanced text option is enable in the project settings. It will not be present on PanelSettings created at
runtime, so make sure the build contains at least one PanelSettings asset
```

## The mechanism

`PanelSettings` serializes a field called `m_ICUDataAsset`. The editor fills it in when the asset is written;
nothing fills it in on an object made at runtime. The asset this project now commits carries:

```yaml
m_ICUDataAsset: {fileID: 20204, guid: 0000000000000000f000000000000000, type: 0}
```

`guid: 0000…f000…` is the engine's own built-in resources file and `fileID: 20204` is `icudt73l` — ICU 73's
little-endian data. So the reference is into the engine, which is why the asset cannot be hand-authored: no
YAML typed from memory could name that file id and be checked.

`RuntimePanel.LoadTextData` therefore loads the committed asset and throws it away. Nothing is built from it:
each panel's settings are still `ScriptableObject.CreateInstance<PanelSettings>()`, exactly as before, so no
value in that YAML reaches anything on screen. The load is the whole contribution, and it is the load rather
than the file's presence that counts — see the second table below.

## The measurement

`tools/run-player-tests.ps1` on `fix/player-icu-chrome`, the same suite either side of the change:

| | before | after |
|---|---|---|
| tests run outside the editor | 131 | 132 |
| passed | 94 | 130 |
| **failed** | **35** | **0** |
| skipped | 2 | 2 |

(The counts differ by one because the second of the two new fixtures — watch mode's chrome — was written after
the before-run.)

Every one of the 35 was a fixture that touches chrome — all of `BuildingTests`, most of `RunLoopTests` and
`WaveTests`, and the new `ChromeLayoutTests`. NUnit reported them against the unhandled log message quoted
above rather than against their own assertions.

The same fixtures were green in the editor throughout: `tools/run-playmode-tests.ps1` reported 131 of 131
passed before the change and after it. That is the whole shape of the defect — the run that could see it was
the one nobody had run.

#210 measured the gap at scale from a real build: 25,941 `ICU Data not available` lines and 14,014
`NullReferenceException`s in twenty seconds of play, and a `Player.log` of 222 MB in two minutes, against a
clean editor log for the same commit.

## Containing the asset is not enough — it has to be loaded

The first fix cloned the asset, which loads it as a side effect, so it could not tell the two explanations
apart. Three player runs on the same commit and the same committed asset, differing only in what the code does
with it:

| what `RuntimePanel.Settings` does | tests | failed |
|---|---|---|
| clones the asset (`Instantiate`) | 132 | 1 |
| loads it and discards the result | 132 | 1 |
| never loads it | 132 | **36** |

The one failure in the first two rows is `RunLoopTests.TheEndFrameSaysWhatTheRunCameTo`, which is unrelated to
text and fails in the editor too. The 36 are the ICU failures returning in full.

So the asset being in the build does nothing on its own. Something has to load it, which means the code that
does cannot be deleted as dead — and it looks exactly like dead code, since it returns a value nobody uses.
That is what `Tests.PlayMode.ChromeLayoutTests` under `tools/run-player-tests.ps1` is there to notice.

## Why the asset rather than turning the advanced text generator off

Switching the project to the legacy text path would also have worked, and would have kept every panel in code
with no asset at all. It was rejected on two counts. It gives up whatever the advanced generator provides —
shaping, bidirectional text, font fallback — which nobody here has written down as unwanted, so the trade is
being made blind. And it is a project setting, which in this repository only Unity writes: `check-project-settings.ps1`
reads those files and never edits them, deliberately.

The asset costs one serialized file in a project whose chrome is otherwise all code. That objection is real and
the answer is containment rather than denial: it is loaded from exactly one place, nothing is built from what
that load returns, and both `RuntimePanel.LoadTextData` and `View.Editor.PanelSettingsAsset` say in as many
words that it is an ICU carrier — so the next reader does not wire panels to it.

## What would have caught it, and now does

`tools/run-player-tests.ps1` already existed for exactly this class of defect — it runs the PlayMode suite in a
standalone player, the one place `UNITY_EDITOR` is undefined. It was not in the green table of
[#203](https://github.com/ssalter21/tower-defense-game/issues/203); the 120 green play-mode tests all ran in the
editor, where the ICU data is present. A suite that only runs where the bug cannot happen is a suite that
cannot see it.

`Tests.PlayMode.ChromeLayoutTests` is the assertion aimed at the mechanism rather than at a symptom: each bar
the game puts up, in both of its modes, and every string on it, must resolve to a width and a height. A bar has
a stated height and passes on its own whatever happens to the text, so the claim that carries the weight is the
one about labels, which have no size except what measuring returns.

A second guard sits beside it in the editor.
`Tests.EditMode.GeneratedProjectFilesTests.TheCommittedPanelSettingsCarryICUData` asserts the committed asset
has the reference at all, because an asset written without it exists, loads, and leaves a player measuring
nothing — identical from the outside to a working one.
