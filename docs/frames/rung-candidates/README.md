# Candidate looks for the four rungs the tier rule under-serves

Alternatives for what four towers are **holding**, drawn through the real match at the framing the game
is played at. Issue [#281](https://github.com/ssalter21/tower-defense-game/issues/281), which is the
roster sign-off map's third prototype ticket. **Nothing here decides anything** — AGENTS.md rule 6 puts
anything a player sees on the human side of the line, and this renders the alternatives and stops.

```powershell
./tools/capture-rung-candidates.ps1
```

## Why this folder exists and `effect-candidates/` did not cover it

[`effect-candidates/`](../effect-candidates/README.md) moves an effect's colour, size and duration,
which are numbers in `MatchTuning`. **What a row is holding is not a number** — its props, the thing
standing beside it and where its shots leave from are bound in `MatchSceneBuilder`'s own table, and no
argument reached them. So `docs/roster.md` could sign four rungs with a look the bindings do not
deliver and there was no way to photograph any of the alternatives:

| rung | what the page signs | what the bindings give |
|---|---|---|
| the Mortar | "a heavier `turret_base`" | the same turret at the same size — the collection ships one |
| the Artificer | a turret **and** an `ammo_crate` | the turret; a tower has one beside socket |
| the Elder | tier 2 is colour **plus a prop** | colour |
| the Bishop | `Cleric_Mace`, and no word about the tome | a magic bolt leaving the head of a blunt weapon |

`roster/mage-hat-pitch.txt` named the gap on 7 September 2026 and said what it wanted: *"either a
batch-argument override on `MatchFrameCapture` or a ticket of its own"*. This is that argument —
`-matchFrameArt`, and `-Art` on `capture-match-frames.ps1`.

**The candidate look is a file and never an edit to the bindings.** It is applied to a *copy* of the art
the capture built, for the length of one run. `MatchSceneBuilder`, `MatchArt.asset`, the generated
scene, `content/units.txt` and `docs/roster.md` are all untouched, and a run with no candidate draws
precisely what it drew before. See [`UnitArtFile.cs`](../../../client/Assets/Editor/UnitArtFile.cs).

## The board

All five rows stand on [`../underserved-rungs.txt`](../underserved-rungs.txt), against the recorded
map, wave and seed. The tier-1 Engineer is on it and is not a question: *"heavier"* is a comparison, and
the thing the Mortar's turret has to be heavier **than** is the one standing beside him — so rows 35, 36
and 37 are within three cells of each other and one frame carries all three turrets at one camera.
`underserved-rungs-tick-*.png` is that board with nothing moved, and every candidate is read against it.

## Wide only, and that was measured

Every candidate was drawn at a close distance of 16 first, the way `capture-effect-candidates.ps1`
draws its bracket. **The close pass answers nothing here**: all four Mortar close frames came back
byte-identical *to each other*, on candidates whose whole content is the turret's size.

The reason is the rig rather than the distance. `OrbitCameraRig` frames the **whole board** and
`-Distance` moves the camera in along that heading, so a closer camera crops toward the middle of the
corridor — and the five rows this defense stands are in the top-left **corner**. The Mortar and the
Bishop fall outside the close frame entirely. A magnified picture that cannot show the thing being
decided is worse than no picture, which is the call issue #280 made about the shell.

**The magnified view of these four questions is a sheet**, and that is the better instrument anyway: a
prop is a solid object standing still, which is what `capture-armed-roster.ps1` frames one of per tile.
[`../roster/`](../roster/) carries one sheet per question at `-Width 700` and the same set again at
`-Width 28`. So the pair issue #270 asks for is the sheet and the played frame, not two cameras on one
board. What would make a close frame work is pointing the camera **at a cell**, which
`MatchFrameCapture` has no argument for; that is a tool change and not this ticket's.

## The ticks, and the four that were wrong first

`capture-match-frames.ps1` logs a per-tick line carrying the live creep count, the live shell count and
a running total per effect, so ticks are read off a cheap narrow run rather than found by opening
pictures. The obvious guesses — 300 and 700, which `../four-lines.txt` uses — are both useless here:

- **tick 300** has three creeps and **zero shells in the air**;
- **tick 700** has **zero creeps**: five towers that all reach four hexes clear the wave by about 420.

And **a hitscan bolt lives four ticks, not five.** The two Bishop candidates that differ only in where
the bolt leaves from were rendered at every tick from 311 to 326: pixel-identical from 315 onward,
differing at 311, 312, 313 and 314. Kept: **200 and 320** for a prop standing still, **311, 313, 320 and
340** for a shot in the air.

## What the frames measure

Against the baseline at the same tick and the same fixed camera, on a 1,440,000-pixel frame:

| candidate | pixels moved | of the frame |
|---|---|---|
| `mortar-turret-2.00` | 2,696–3,030 | 0.19–0.21% |
| `mortar-turret-1.75` | 2,061–2,395 | 0.14–0.17% |
| `mortar-turret-1.50` | 1,446–1,780 | 0.10–0.12% |
| `mortar-turret-1.25` | 887–1,221 | 0.06–0.08% |
| `bishop-mace-off-hand` | 274–871 | 0.02–0.06% |
| `artificer-crate-only` | 125–560 | 0.01–0.04% |
| `bishop-tome-beside` | 133–568 | 0.01–0.04% |
| `bishop-tome-off-hand` | 123–557 | 0.01–0.04% |
| `artificer-turret-and-crate` | 109–443 | 0.01–0.03% |
| the anchor alone, mace head → tome | 19–225 | 0.001–0.016% |

Issue #280 put anything drawn on the ground at 1–2% of the frame and anything crossing it through the
air at 0.004–0.030%. **The Mortar's turret is the only one of the four questions that clears that
second band**, which makes it the only one a played frame decides. The other three are decided at
magnification, on the sheets, or as a matter of what the record should say rather than what the screen
shows.

## What is committed and what is not

The `played/` frames are committed; nothing else here is. `.gitignore` beside this file says why. The
whole folder is temporary — it is the set of alternatives
[#284](https://github.com/ssalter21/tower-defense-game/issues/284) signs from, and it goes when the
signing does.
