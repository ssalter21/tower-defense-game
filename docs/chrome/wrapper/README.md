# The wrapper

Three sheets of the wrapper at 1600×900, the size the built player runs at: the main menu the build opens on,
the settings screen, and a run played to its end with the way back to the menu. Issue
[#317](https://github.com/ssalter21/tower-defense-game/issues/317) built them so the words and the settings can
be signed from a picture rather than from a description; the sitting that signs them is
[#319](https://github.com/ssalter21/tower-defense-game/issues/319).

```powershell
./tools/capture-wrapper.ps1
```

**Every word on these screens is a placeholder.** Each screen says so in amber, naming the words it means.

| sheet | what it shows |
|---|---|
| `menu.png` | What `Match.unity` opens on: three buttons over the idle board. No run has started, and the one match root is the board behind (ADR-0029). |
| `settings.png` | The settings screen, opened from the menu. Each setting carries a line saying whether the client honours it and what shipping it still costs. |
| `over.png` | A run the scripted player played to its end: what it came to, and the header's one button offering the way back to the menu. |

## The settings shown, and the ones that are not

- **Player name** is on the screen because the playtest protocol needs it. Nothing reads it yet: it is kept
  until the game closes and reaches the game when the lobby folder writes a name on each stored round.
- **Lobby folder** is honoured. The next run takes its opponents from the folder's `pool/` and writes its rounds
  and its script there; the default is the player's own data folder, where a run wrote before. It is kept until
  the game closes, and remembering it between launches costs a saved preference.
- **Display** (full screen or windowed) and **resolution** are honoured the moment they change.
- **Volume is not shown.** The game makes no sound, so a slider would honour nothing.

The same settings screen opens over a run with Escape, and the board takes no clicks while it is up.
