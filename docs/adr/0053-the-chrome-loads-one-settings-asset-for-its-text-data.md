# The chrome loads one settings asset, for its text data and nothing else

`Assets/Resources/RuntimePanelSettings.asset` is committed, and `RuntimePanel.LoadTextData` loads it once per
panel built and discards it. No panel is built from it and no value in it reaches the screen.

Loading a `PanelSettings` asset is what gives a player build the ICU data Unity's advanced text generator
shapes and measures every string through. Without it every label measures zero by zero and the whole HUD
collapses; the editor is immune because its own process has the data either way. The measurement is in
[a player build measures no text without a PanelSettings asset](../research/a-player-build-measures-no-text-without-a-panelsettings-asset.md).

The alternative was turning the advanced text generator off, which needs no asset. It was rejected: it trades
away shaping, bidirectional text and font fallback that nobody has written down as unwanted, and it is a
project setting, which in this repository only Unity writes.

## Consequences

**A serialized asset exists in a project whose chrome is deliberately all code.** `RuntimePanel`'s own reasoning
is that a panel authored into a scene would put the chrome into YAML whose diffs cannot be read. This asset is
the exception, and it is contained by being inert: it is loaded from one place, nothing is built from what that
load returns, and `Object.Instantiate` is not used, so no field in the YAML can reach a panel. Editing its
values changes nothing on screen.

**The call that loads it looks like dead code.** It returns a value nobody uses, and deleting it compiles,
passes every editor test, and silently breaks every player build. `Tests.PlayMode.ChromeLayoutTests` under
`tools/run-player-tests.ps1` is the run that fails when it goes — the editor cannot see it.

**The asset is generated, not authored.** It carries a reference into the engine's built-in resources
(`fileID: 20204`, `icudt73l`) that no hand-written YAML could name or check, so `tools/build-panel-settings.ps1`
writes it in batchmode and asserts the reference survived the write.

**It widens what `Resources` is for.** [0024](0024-art-is-serialized-references.md) says the project's one
`Resources` folder exists so the play-mode suite can load art without `AssetDatabase`, and is deliberately not
how the game gets its art. That still holds for art. This asset and the theme style sheet beside it are loaded
by the game as well, because they have to survive into a player and there is no scene to hand them over.
