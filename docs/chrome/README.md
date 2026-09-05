# Chrome sheets

Rendered by `tools/capture-ui-previews.ps1`, headless, with the editor closed.

**These are how a layout gets chosen.** Art is never picked from a filename on this project, because "Idle_A"
and "Skeletons_Idle" are the same string to everyone and two different poses to nobody — that is what
[`capture-art-previews.ps1`](../../tools/capture-art-previews.ps1) exists for. A layout has the identical
failure: *the purse goes in the header* and *the purse goes over the palette* are two sentences that agree with
each other and two screens that do not. A candidate layout is a scratch class staged into the project for one
run, and what gets put in front of a person is the picture rather than the description.

**They are documentation, not an oracle** — the same call [the match frames](../frames/README.md) make.
Nothing compares a sheet to anything and nothing fails if one changes. What catches broken chrome is
`Tests.PlayMode/ChromeLayoutTests`.

## What is in a sheet

The real chrome over the real board. The playfield is a real `MatchRoot` on the committed map, the run is a
real `RunLoop` holding a real `ComposedRound`, and every price, name and purse on screen is what `content/`
says — a mockup with invented numbers on it is a picture of a game this project does not ship, and the numbers
are most of what a build phase looks like. Every state is reached through the methods the pointer goes
through, so a state the rules would refuse a player is a state a sheet cannot show.

## Running it

```powershell
./tools/capture-ui-previews.ps1 -Spec C:\scratch\ui-spec.json
```

A spec names an output directory, a sheet width, and the shots:

```json
{
  "outDir": "C:/scratch/sheets",
  "width": 1600,
  "shots": [
    { "id": "as-built-build",  "label": "Opening build phase", "state": "build" },
    { "id": "as-built-placed", "label": "A tower down, two creeps sent", "state": "build-placed" },
    { "id": "as-built-offer",  "label": "The ladder open", "state": "build-offer", "place": "archer" }
  ]
}
```

- **`state`** — `build` (the opening phase), `build-placed` (a tower down, two creeps sent), `build-hover` (the
  same, with the cell under the pointer lit through `BuildBoard.Lit`) or `build-offer` (the upgrade ladder
  open). `build-hover` spends less on creeps than it could, on purpose: a hex lights only where the rules would
  take a tower, so a state that had spent the last of the gold refuses to hover at all.
- **`place`** — which tower a composed state buys, by its label in `content/units.txt`. Every one of the nine
  roots has a rung above it, so any of them can be the subject of an upgrade shot; the committed sheet stays on
  the **archer** so that a candidate layout is compared against the same picture. A shot that asks for the
  offer and does not get it is refused rather than written, because an offer that did not open renders as an
  ordinary build phase.
- **`candidate`** — the `UiPreviewCapture.IUiPreviewLayout` to run, by type name. Empty means the chrome the
  game ships, which is what every candidate is compared against.

Paths in a spec take forward slashes. `JsonUtility` refuses a Windows path's backslash escapes, and it refuses
it as *invalid escape character* rather than as a bad path.

## What is committed

**The chosen arrangement**, which is what the build phase is being built toward:

- `chosen-build-phase.png` — chrome cut to a line of run state and a commit; towers chosen at the hex, with the
  options opening beside the cell the hover lit; the wave along a side rail in portraits. Decided 17 August
  2026, from six rendered layouts. Recorded in
  [the build order](../build-order.md#7--the-interface), with its two loose ends in
  [open questions](../open-questions.md).

**The baseline**, three sheets of the chrome as it stands, which is what a candidate is held against:

- `as-built-build.png` — the opening build phase. A hundred gold, nothing placed, nothing sent.
- `as-built-placed.png` — a soldier down and two creeps in the wave, at 52 gold left.
- `as-built-offer.png` — the ladder open on an archer, offering a ranger for 40.

The rest is regenerable and not committed, and that is arranged by [`.gitignore`](.gitignore) rather than by
whoever runs the capture next remembering to delete them: left to memory, a plain run leaves files in the tree
and the build gate's last step fails the next push for a dirty repository.

**A sheet is a claim about the content it was rendered from.** Move a price, a label, the ladder, the board or
what stands on it and these three are stale while going on looking entirely reasonable — a build phase with the
wrong number in it is not a picture that announces itself. Re-capture them whenever `content/units.txt`,
`content/upgrades.txt`, `content/map.txt` or `content/dressing.txt` moves. Those four are the ones
[`check-docs.ps1`](../../tools/check-docs.ps1) dates the committed sheets against.

**`rendered-from.txt` is what the capture leaves behind saying which content it drew.** A re-capture that comes
out pixel-identical — which is what happens whenever the content that moved is not on these sheets — leaves
nothing for a date to see, so the date alone would call a current sheet stale. The record answers that, and it
is written by the capture and never by hand. See [`_rendered-from.ps1`](../../tools/_rendered-from.ps1).

## Two things that had to be measured

**A runtime panel does not lay out in an edit-mode batchmode editor.** A bar built there resolves to `NaN` by
`NaN` and renders zero pixels; in play mode the same bar measures 399 by 199.5 and draws. So the capture enters
play mode and exits the editor itself, which is why its editor is launched without `-quit`.

**The screen has to be moved as well as the texture.** `ScreenCapture` returns null in batchmode and
`Screen.SetResolution` is ignored, so the camera and every bar are pointed at one render texture. That is not
sufficient on its own: the upgrade offer finds its hex through `RuntimePanelUtils.CameraTransformWorldToPanel`,
which reads the *screen* rather than the surface being rendered, and a batchmode screen is 640 by 480. Left
alone, the ladder lands 44 pixels above the top of a 900-pixel sheet and what gets written is a reasonable
build phase with no offer on it. `PlayModeWindow.SetCustomRenderingResolution` is what moves it, and `Screen`
goes on reporting 640 by 480 afterwards — so the evidence it worked is the offer's position, not the screen.
