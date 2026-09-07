# Candidate effect looks

**Five questions are open and one is closed.** The closed one is the alpha,
answered on 7 Sep 2026; the bracket that decided it is kept below as the record
of how. The open five are
[#280](https://github.com/ssalter21/tower-defense-game/issues/280)'s — the
things that do not read at the size the game is played at — and they are waiting
on [#284](https://github.com/ssalter21/tower-defense-game/issues/284).

**Read [the measurement](#what-the-frames-measure) before the pictures.** The
five open questions split cleanly in two, and the split is not what the ticket
expected: everything drawn *on the ground* moves one to two per cent of a
1600×900 frame, and everything *flying through the air* moves between four
hundredths and three hundredths of one per cent. Doubling the thrown knife's
length changes 209 pixels out of 1,440,000.

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

## The five open questions

Issue [#280](https://github.com/ssalter21/tower-defense-game/issues/280) is the
four things `#270` proved do not read at 1600×900, which come to five brackets
because the Consecration is two questions that compound. Twenty candidates,
every one drawn through the real match at the framing the built player uses.

| Bracket | Candidates | What is being asked |
|---|---|---|
| the thrown knife | [`knife-as-shipped`](knife-as-shipped.txt) 0.55 m · [`knife-half-again`](knife-half-again.txt) 0.85 m · [`knife-double`](knife-double.txt) 1.1 m · [`knife-dark`](knife-dark.txt) shipped size, dark blade | is it the size or the contrast? |
| the magic bolt | [`bolt-as-shipped`](bolt-as-shipped.txt) 0.45 m · [`bolt-half-again`](bolt-half-again.txt) 0.68 m · [`bolt-double`](bolt-double.txt) 0.9 m · [`bolt-dark`](bolt-dark.txt) shipped size, deep violet | the same, of the shape six rows fire |
| the mortar shell | [`shell-as-shipped`](shell-as-shipped.txt) · [`shell-pale`](shell-pale.txt) · [`shell-warm`](shell-warm.txt) · [`shell-bigger`](shell-bigger.txt) 0.28 m | it reads as a hole in the floor — is that the colour or the size? |
| where a ground effect stops | [`reach-as-shipped`](reach-as-shipped.txt) · [`reach-clipped`](reach-clipped.txt) · [`reach-shrunk`](reach-shrunk.txt) | a new rule, not a correction — see below |
| the Consecration | duty: [`light-as-shipped`](light-as-shipped.txt) 26/30 · [`light-half`](light-half.txt) 15/30 · [`light-brief`](light-brief.txt) 8/30 · radius: [`light-two-hex`](light-two-hex.txt) · [`light-one-hex`](light-one-hex.txt) | which of the two makes it a permanent hole? |

**Where a ground effect is allowed to reach is signed nowhere.** What the aura
shapes *do* is [`docs/roster.md`](../../roster.md)'s and is signed; that they run
off the edge of the board and hang over the background is a consequence nobody
chose. So the shipped answer is a candidate on the same footing as the other
two, and [`rim-emitters.txt`](../rim-emitters.txt) is the only defense on the
board that stands an aura on a rim — with one standing nowhere near one, as the
control.

**The Consecration's radius is not a look and cannot be asked with one.** How
far an aura reaches is `content/units.txt`'s number, and it decides which towers
the aura actually finds; drawing the light smaller than the reach would be a
picture that lies about what the tower is doing. So the two radius candidates
are unit tables — [`consecration-two-hex.txt`](../consecration-two-hex.txt) and
[`consecration-one-hex.txt`](../consecration-one-hex.txt) — paired with a look in
the plan table, and **a radius answer is a balance change that lands in
`content/units.txt` and `docs/roster.md`, not in `MatchTuning`.**

### What the frames measure

Per cent of a 1600×900 frame that changes against the candidate's own baseline,
counted at a per-channel difference of 8 or more, which is about where a
difference stops being arguable.

| Candidate | Frame moved |
|---|---|
| `reach-shrunk` @305 | 1.897% |
| `light-half` / `light-brief` @320 | 1.718% |
| `light-one-hex` @320 | 1.526% |
| `reach-clipped` @305 | 1.416% |
| `light-two-hex` @320 | 0.974% |
| `bolt-double` @313 | **0.030%** |
| `shell-bigger` @320 | **0.019%** |
| `knife-double` @617 | **0.015%** |
| `bolt-half-again` @313 | 0.014% |
| `shell-pale` / `shell-warm` @320 | 0.008% |
| `knife-half-again` @617 | 0.008% |
| `knife-dark` @617 | **0.004%** |

**Two orders of magnitude, and the line falls between the ground and the air.**
The two ground questions are decidable by looking at a played frame. The three
about a small object crossing the board are not: the largest change any of them
makes is three hundredths of one per cent, and the boldest — a knife at twice
the length — is 209 pixels. That is the same finding `#279` made about
`tower-marks-on` and `frost-halo`, arrived at from the other direction, and it
says the knife, the bolt and the shell may not be answerable by moving a size or
a colour at all.

**Three frames are byte-identical to their baseline and that is the answer, not
a fault.** The Consecration pulses at tick 301, so at 305 all three duty
candidates are still lit and match; at 313 only the eight-tick one has gone out;
at 320 only the shipped one is left. The ticks were picked to make the bracket
disagree on the frame rather than only in the file.

**The shell has no close frame, and that was measured rather than chosen.** Drawn
at a close distance of 14, `shell-pale` and `shell-warm` come out byte-identical
to `shell-as-shipped` — zero pixels, on a colour going from near-black to
near-white — because a shell at the shipped radius is behind a hex from that
camera. A magnified picture that cannot show the thing being decided is worse
than none.

## Two framings, and the wide one is the one that decides

Issue [#270](https://github.com/ssalter21/tower-defense-game/issues/270)
established that a sheet and the built player disagree: a slowed body reads
plainly magnified and not at all at 1600×900, which is the size a person actually
plays at. So a candidate is rendered twice wherever the second framing says
anything — and where it does not, it is rendered once and the plan table says
why. Three of the five open brackets have no close frame: two ask what happens
at the edge of the board, which the close camera crops out, and the shell's was
drawn and thrown away for showing nothing.

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
./tools/capture-effect-candidates.ps1              # every candidate, both framings
./tools/capture-effect-candidates.ps1 -CloseOnly   # just what is ignored
./tools/capture-effect-candidates.ps1 -Only reach-clipped,reach-shrunk
```

Which board a candidate is photographed on, which ticks it is asked for, how far
back the close camera stands, and — for the two radius candidates — which unit
table it is played against, are all in the plan table at the top of
`tools/capture-effect-candidates.ps1`. That is deliberate: a candidate file says
what the effect looks like and says nothing about which match shows it.

The editor must be closed — every capture is `-batchmode`.

**Read the render back before handing it over.** A run that exits 0 and writes
differing PNGs can still answer nothing: `#279` shipped seven frames that were
byte-identical to their baselines and only `cmp` found them. Diff each frame
against the shipped one and say out loud what changed.

## Nothing here decides anything

AGENTS.md rule 6 puts art and anything a player sees on the human side of the
line. These are rendered alternatives and they stop there; the signing above was
Sam's, off these pictures.
