# Candidate effect looks

**Nothing here is open.** Every question this folder ever asked was answered on
7 Sep 2026 — most of them by being rejected, the last of them by being picked.
See [`docs/decision-log.md`](../../decision-log.md). What is left is the bracket
that decided the alpha, kept as the record of how it was decided.

## What happened to the twenty-four candidates that were here

Issue [#279](https://github.com/ssalter21/tower-defense-game/issues/279) rendered
twenty-four alternatives for the effect looks standing on nobody's signature:
four creep aura shapes, the shared blast-and-aura ring, where the Blessing's mesh
goes, telling a slow from a haste, and the four other things `#254` left
standing. Sam looked at all of them and took none.

What he signed instead is simpler than any candidate on the sheet:

- **Every aura is one flat translucent circle**, as wide as it reached, in its
  own colour. Nine shapes — a ring, cracks, a halo, a light, roots, a cage,
  plates, a crown of shards — became one.
- **Nothing is drawn on the bodies an aura found.** No ring over a hastened
  creep, no glow on a blessed tower, no crown at a frostbitten one's feet, no
  wash of colour on a body carrying a modifier. Which bodies an aura caught is
  read off the circle they are standing in.
- **The Overgrowth draws nothing at all**, because its aura reaches sixty hexes
  and a circle at that radius is the screen washed flat rather than an area
  shown.

**This is an interim look and it is recorded as one.** What these effects should
finally be is animation and particle work nobody has done. That work is no longer
blocked: `MatchViewTests.NothingInTheMatchTurnsToFaceTheCamera` used to forbid
`ParticleSystem` outright, and on 7 September 2026 it was narrowed to what
[the vision](../../vision.md) actually says — nothing may **billboard**. A
particle system in `Mesh` render mode emits real geometry and faces nothing, so
it is allowed; line renderers, trail renderers, sprites and canvases still are
not. Until somebody does that work the plainest honest shape is one circle.

The rejected frames are not kept. They are pictures of nine shapes that no longer
exist, and every one of them is in the history of this folder and on `#279`.

## How the alpha was settled

`MatchTuning.AuraDiscAlpha` is **0.45**, and it is one of the few numbers in that
file somebody actually chose. Sam picked it off these three frames:

| Candidate | Alpha | What it was trying to be | |
|---|---|---|---|
| [`aura-alpha-light.txt`](aura-alpha-light.txt) | 0.15 | the floor barely tinted | rejected — the three auras merge into one haze and stop being separable |
| [`aura-alpha-shipped.txt`](aura-alpha-shipped.txt) | 0.28 | the value nobody chose | rejected — the cold circles read, the Blessing's gold does not |
| [`aura-alpha-heavy.txt`](aura-alpha-heavy.txt) | 0.45 | the circle reads first | **signed** |

**Two circles overlapping is what decided it, not one on empty floor.** A single
circle reads at almost any alpha. What the heavy end risked was doing what the
opaque ring did — lying over the corridor and the bodies walking down it — and
that only shows where two auras cross. Tick 272 of the `auras` context has the
Necromancer, the Witch and the Skeleton Mage all pulsing within a few ticks of
each other with bodies walking through, which is why both committed ticks are
that context.

**The three frames are kept and are not redrawn.** Each names its own alpha
outright — the middle one included, which used to name none and stood for
whatever the file held — so all three stay pictures of the values that were
compared, and re-running the capture reproduces them. They are evidence for a
decision rather than a description of the board, which is the same footing the
Mage hat-pitch bracket sits on.

## Two framings, and the wide one is the one that decides

Issue [#270](https://github.com/ssalter21/tower-defense-game/issues/270)
established that a sheet and the built player disagree: a slowed body reads
plainly magnified and not at all at 1600×900, which is the size a person actually
plays at. So every candidate is rendered twice.

- **[`played/`](played/)** — 1600×900, camera at the distance the whole floor
  fits, which is the framing the built player uses. **This is the frame that
  decides.** A candidate that cannot be told from the shipped one here has not
  answered the question, whatever it looks like close up.
- **`close/`** — the camera moved in to roughly a third of that distance, so what
  a shape is *made of* is readable. Narrower on purpose, and **not committed**:
  it is a supporting picture and the wide one is the artefact. Redraw it with
  `./tools/capture-effect-candidates.ps1 -CloseOnly`.

**The magnification is the camera and not the picture.** A 4,800-pixel render of
the same composition is the same frame in four times the bytes, and this
repository has a five-megabyte ceiling on one file.

## How a candidate works

A candidate is a `.txt` naming `MatchTuning`'s own constants and what to draw
them as. `tools/capture-match-frames.ps1 -Effects` plays the recorded match with
one on and photographs it.

**`MatchTuning` is untouched by any of it.** `EffectLook` answers out of that
file for every member nobody named, so the shipped look keeps one home and an
override lives for the length of a run and no longer. A key is a `MatchTuning`
member name, and an unknown one is a fault that lists the near misses rather than
a frame that quietly renders the shipped value.

A `signature <unit id> <aura>` line moves which aura a row's bubble is, which is
bound per unit and cannot be asked with a number at all. Since the shapes
collapsed, that line picks a colour and a lifetime rather than a shape.

**A comment is a line that opens with a hash, and nothing else is.** The parser
used to cut every line at the first `#`, which made `#rrggbb` "a key with no
value" and truncated any `question` naming an issue. Both are regression-tested
in `EffectLookTests`.

## Redrawing

```
./tools/capture-effect-candidates.ps1              # both framings, all three
./tools/capture-effect-candidates.ps1 -CloseOnly   # just what is ignored
./tools/capture-effect-candidates.ps1 -Only aura-alpha-heavy
```

The editor must be closed — every capture is `-batchmode`.

**Read the render back before handing it over.** A run that exits 0 and writes
differing PNGs can still answer nothing: `#279` shipped seven frames that were
byte-identical to their baselines and only `cmp` found them. Diff each frame
against the shipped one and say out loud what changed.

## Nothing here decides anything

AGENTS.md rule 6 puts art and anything a player sees on the human side of the
line. These are rendered alternatives and they stop there; the signing above was
Sam's, off these pictures.
