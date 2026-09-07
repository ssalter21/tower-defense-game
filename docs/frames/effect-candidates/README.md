# Candidate effect looks

**Every effect look in this game stands on nobody's signature, and this folder is
the set of alternatives that gets them signed.** `MatchTuning`'s own header says
it: ten shapes are signed and four are not, and *every number and colour on the
page is a placeholder* — the marks section included, which is what a slowed,
hastened, cursed or shielded body wears. The roster expansion put them all in one
tuning block and declared them rather than scattering them; this is what stands
beside that block.

**Nothing here decides anything.** AGENTS.md rule 6 puts art and anything a
player sees on the human side of the line. These are rendered alternatives and
they stop there. Signing them is [#284](https://github.com/ssalter21/tower-defense-game/issues/284)'s.

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

## How much of the frame each candidate actually moves

**Measured, at 1600×900, against the shipped look in the same frame.** This is
the number to read before any of the pictures, because it says which of these
decisions a player could see the outcome of at all. Every candidate was checked
against its baseline with `cmp` first: none of them is identical, so every row
below is a real difference and not a render that failed to take.

| Candidate | Of the frame | What moves |
|---|---|---|
| `ward-light-on-ground` | 2.56% | a disc of light two hexes across |
| `ring-heavy` / `ring-warm` / `ring-as-shipped` | ~2.0% | the ground plate the pulse leaves |
| `ward-ring-at-reach` | 2.02% | a ring at the edge of the ward |
| `hex-cage` | 1.66% | a cage standing over the Witch |
| `haste-ring-at-reach` | 0.88% | a ring at the edge of the haste |
| `hex-under-body` | 0.82% | a patch under each armoured body |
| `haste-under-body` | 0.57% | a patch under each hastened body |
| `frost-ring-at-reach` | 0.37% | a ring at the edge of the frostbite |
| `both-modifiers-third-colour` | 0.35% | the wash on bodies carrying both |
| `speed-both-moved` | 0.24% | both washes |
| `speed-warm-haste` / `speed-green-haste` | ~0.15% | the wash on hastened bodies |
| `blessing-at-the-feet` / `-collar` / `-held-halo` | 0.08–0.16% | the mark on three towers |
| `bar-crossed` / `bar-clamped` | 0.12% | the bars over a knot of creeps |
| `tower-marks-on` | 0.09% | one frostbitten tower's body |
| `frost-halo` | 0.04% | the mark over one caught tower |

**The bottom half of that table is the finding.** `tower-marks-on` moves a patch
of about 31×58 pixels — one tower's body — and `frost-halo` about 57×92. At the
size the game is played at, washing a frostbitten tower and moving where its mark
hangs are decisions whose whole visible consequence is smaller than a roster
thumbnail. That is #270's result arriving again on a different question, and it
is worth settling before any of the four Blessing candidates is argued about on
its merits.

**The top of the table is a finding too, and a less comfortable one.** The shared
ring is not an open ring: `MatchTuning` calls it one and draws a flat cylinder,
so at the two hexes every aura on the roster carries it is an opaque plate that
covers the corridor and the bodies standing on it. That is why the three `ring-`
candidates all move about 2% of the frame while the shapes drawn *on* bodies move
well under one.

## How a candidate works

A candidate is a `.txt` in this folder naming `MatchTuning` members and what to
draw them as. `tools/capture-match-frames.ps1 -Effects` plays the recorded match
with that look on and photographs it; **nothing in the file the game ships from
moves**, and the override lives for the length of the run and no longer. See
[`EffectLook.cs`](../../../client/Assets/View/EffectLook.cs) for why that
indirection exists and
[`EffectLookFile.cs`](../../../client/Assets/Editor/EffectLookFile.cs) for the
grammar.

**A key that is not a member is a fault that lists the near misses.** That is the
whole of the validation and it is the point of it: a look drawn from a misspelt
key renders the shipped value under a candidate's filename, and two candidate
frames that came out identical because both keys were wrong is a picture that
answers a question it was never asked.

**A `signature <unit id> <shape>` line moves which shape a row's bubble leaves**,
because a shape is bound per unit rather than tuned and cannot be asked with a
number at all. **It moves the colour with it, and that is a limit of these
frames rather than a proposal**: the pooled object is keyed by *piece*, and a
piece is a shape and a colour together — so the Witch's hex drawn as a cage comes
out in the ward's blue rather than the hex's violet. Read a shape candidate for
its shape.

## The questions, and what stands under each

### 1. The four creep aura shapes

Issue #266 named where those four effects leave from — the staff, the scythe, the
broom and the axe — and named no shape at all, and a walking row carries no
effect anchor, so neither half could be built as written. What ships is one shape
per row centred on the body. **A shape down here is as unsigned as the colour it
is drawn in**, which is not true of any of the ten capstone shapes.

| Candidate | The Skeleton Mage's haste |
|---|---|
| `auras-as-shipped` | a ring over the head of every creep it reached |
| `haste-under-body` | a patch on the ground under every body it reached |
| `haste-ring-at-reach` | a ring on the ground where the pulse stopped |

| Candidate | The Necromancer's ward |
|---|---|
| `auras-as-shipped` | a cage of arcs standing over the Necromancer |
| `ward-light-on-ground` | a disc of light out to the reach of the pulse |
| `ward-ring-at-reach` | a ring on the ground where the pulse stopped |

| Candidate | The Witch's hex ward |
|---|---|
| `auras-as-shipped` | a band of plates lying on the ground out to the reach |
| `hex-cage` | a cage of arcs standing over the Witch |
| `hex-under-body` | a patch on the ground under every body it armoured |

| Candidate | The Frost Wight's frostbite |
|---|---|
| `auras-as-shipped` | a crown of shards at the feet of every tower it caught |
| `frost-halo` | a ring over the head of every tower it caught |
| `frost-ring-at-reach` | a ring on the ground where the pulse stopped |

All four are photographed on one board at one tick, so a candidate for one is
read against the other three as they ship.

### 2. The shared blast and aura ring

One placeholder ring serves both a blast and an aura, and nothing says they
should look alike. **Every frame in this group draws both at once**: a Mage's
splash landing is the blast, and the Witch's own shape is taken off with
`signature 44 None` so her pulse leaves the plain ring — otherwise the aura ring
never appears at all, because every shipped aura carries a shape of its own.

| Candidate | |
|---|---|
| `ring-as-shipped` | one look for both, cold blue, thin, eight ticks |
| `ring-warm` | warm rather than cold, so it reads as a thing that happened |
| `ring-heavy` | thicker and held longer, so a wide bubble is read across |

**One constant serves both, so a candidate cannot give them different looks.**
What these frames can answer is whether one look reads as both; whether they
should differ is answered by looking at a blast and a pulse wearing the same
thing.

### 3. The Blessing

It ships as a halo and the signed word was *glow*. **A persistent glow was ruled
out of the roster expansion as needing real mesh geometry** — the client has no
`ParticleSystem` and a play-mode test forbids one, along with line renderers,
trail renderers, sprites and canvases, because all of them billboard and this
camera orbits. So every candidate here is mesh, and the question is where the
mesh goes.

| Candidate | |
|---|---|
| `blessing-halo-shipped` | 1.2 m across, 2.9 m up — the halo it ships with |
| `blessing-at-the-feet` | a wide ring on the ground the tower stands on |
| `blessing-collar` | a collar at the chest rather than a halo over the head |
| `blessing-held-halo` | the halo, wider and held nearly the whole pulse |

**They are captured at tick 274 and the first attempt at 270 answered nothing.**
The Blessing pulses on tick 271 and its glow stands twelve ticks, so 270 is one
tick before there is anything to photograph — all four candidates came out
byte-identical. Another case where `cmp` is the only instrument that would have
noticed; four plausible-looking frames of an empty question is exactly the
species of picture this project keeps deleting.

### 4. Telling a slow from a haste — the one that is a defect

A hastened body wears the same blue wash a slowed one does. The ring over its
head is the only thing that tells them apart, and #270 proved the ring reads at
1x while the wash does not. **Every frame in this group has a slowed creep and a
hastened one on it at once**, which is why it is fought against the four-lines
defense: the Shield Wall slows and the Skeleton Mage hastens, and the recorded
defense stands neither.

| Candidate | |
|---|---|
| `speed-one-colour` | one colour over both, as it ships |
| `speed-warm-haste` | haste warm amber against the slow's cold blue |
| `speed-green-haste` | haste the green of the ring already over its head |
| `speed-both-moved` | the slow deepened as well, so neither is the pale wash |

**These are fought against [`speed-pair.txt`](../speed-pair.txt) and not against
`creep-auras.txt`, and the reason is a finding.** That wave sends the Witch, who
armours every friend within two hexes — and a body carrying a speed modifier
*and* an armour one is drawn as neither of the two, because it takes the
both-modifiers colour. So against that wave nearly every hastened body is in the
third bucket and moving the haste colour changes nothing: **three of the four
candidates rendered byte-identical to the shipped look**, and `cmp` is the only
reason anybody knows. The wave beside them sends a Skeleton Mage and two rows
that author nothing, so every washed body in these frames is washed for its speed
alone.

### 5. The four other things #254 left standing

`speed-one-colour` above is the first of the five; these are the rest.

| Candidate | The question |
|---|---|
| `both-modifiers-third-colour` | a body carrying a speed modifier *and* an armour one shows only the speed — should carrying both look like a third thing? |
| `tower-marks-on` | a tower carrying a modifier is not drawn at all; the crown at a frostbitten tower's feet is the whole of what says it is firing a third slower |
| `bar-crossed` | the bar never turns, so it is read end-on from two of the four quadrants of the orbit; a second bar across it answers that without billboarding |
| `bar-clamped` | both segments are shares of the authored health, so a creep at full health with a pool worth two fifths of it draws one and two fifths of a bar |

**The two pooled rows draw nothing extra, and that is a result rather than an
omission.** The Vampire's blood and the Grave Robber's pack are `CreepSnapshot`
fields and not moments — which is why they survive a scrub — so they are the blue
segment of the bar over the body in every frame here and there is no event to
decorate from. Both rows walk in every frame of the `auras` group.

## Regenerating

```powershell
./tools/capture-effect-candidates.ps1                    # every candidate, both framings
./tools/capture-effect-candidates.ps1 -Only hex-cage     # one of them
./tools/capture-effect-candidates.ps1 -WideOnly          # only the frame that decides
```

The editor has to be closed: it is `-batchmode -executeMethod` and needs the
project lock. Which wave walks and which defense stands is in the script and not
in the candidate, because changing either changes the simulation and so changes
which tick a pulse lands on — a candidate says what an effect looks like and says
nothing about which match shows it.
