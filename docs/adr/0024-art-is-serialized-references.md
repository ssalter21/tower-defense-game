# Art arrives as serialized references, not a runtime lookup

The match scene carries serialized references to the art it draws. `MatchArt` picks nothing and looks nothing up; it is a set of references resolved when the scene is built.

## Consequences

There is one `Resources` folder in the project, used only by the source that lets the play-mode suite load art without `AssetDatabase`. That path exists so the suite is not editor-only — a test class behind `#if UNITY_EDITOR` yields no tests in a player build — and it is deliberately not how the game gets its art.

The generated art asset is written by a tool rather than authored by hand, so it can be regenerated and diffed.
