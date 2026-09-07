# Candidate effect looks

**One question is open here, and it is how see-through an aura's circle should
be.** Everything else this folder used to ask was answered on 7 Sep 2026 by
being rejected — see [`docs/decision-log.md`](../../decision-log.md).

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
finally be is animation and particle work nobody has done, which this client
cannot host as written — `MatchViewTests.NothingInTheMatchTurnsToFaceTheCamera`
forbids `ParticleSystem`, line renderers, trail renderers, sprites and canvases
anywhere in the match, on the grounds that all of them billboard and this camera
orbits. Until that is settled the plainest honest shape is one circle.

The rejected frames are not kept. They are pictures of nine shapes that no longer
exist, and every one of them is in the history of this folder and on `#279`.

## What is still open: the alpha

`MatchTuning.AuraDiscAlpha` is **0.28** and that number is nobody's decision. The
three candidates here are a bracket around it:

| Candidate | Alpha | What it is trying to be |
|---|---|---|
| [`aura-alpha-light.txt`](aura-alpha-light.txt) | 0.15 | the floor barely tinted |
| [`aura-alpha-shipped.txt`](aura-alpha-shipped.txt) | 0.28 | what the file holds today |
| [`aura-alpha-heavy.txt`](aura-alpha-heavy.txt) | 0.45 | the circle reads first |

**The middle one names no value on purpose.** It is the baseline the other two
are read against, and it is a file rather than an absence so that all three
frames come out of one command and one code path.

**Two circles overlapping is the case that decides it, not one on empty floor.**
A single circle reads at almost any alpha. What the heavy end risks is doing what
the opaque ring did — lying over the corridor and the bodies walking down it —
and that only shows where two auras cross. Tick 272 of the `auras` context has
the Necromancer, the Witch and the Skeleton Mage all pulsing within a few ticks
of each other with bodies walking through, which is why both ticks below are
that context.

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
line. These are rendered alternatives and they stop there.
