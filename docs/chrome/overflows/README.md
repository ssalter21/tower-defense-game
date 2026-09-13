# Candidates for the chrome the wide roster broke

Mocks for three of the four places the chrome stopped fitting when the roster went from twelve rows to
forty-four, drawn through the real chrome at 1600×900 — the size the built player runs at and the size
all of them break at. Issue [#282](https://github.com/ssalter21/tower-defense-game/issues/282), on the
roster sign-off map. The fourth place, a beside prop on an occupied tile, is a board question and lives
in [`docs/frames/beside-props/`](../../frames/beside-props/README.md).

**Nothing here decides anything.** AGENTS.md rule 6 puts anything a player sees on the human side of the
line; this renders the alternatives and stops. Signing one is
[#285](https://github.com/ssalter21/tower-defense-game/issues/285)'s.

```powershell
./tools/capture-chrome-candidates.ps1
```

The sheets are the ones [`spec.json`](spec.json) names, and one `.txt` per question says what each
sheet is a candidate for: [`wave-bar.txt`](wave-bar.txt), [`offer.txt`](offer.txt),
[`tokens.txt`](tokens.txt). The candidates themselves are classes under
`client/Assets/Editor/ChromeCandidates/`, named in the spec by type; they rearrange what the shipped
bars built and change nothing a bar says.

## What had to be built to draw them

**A build phase at wave 9, not wave 1.** Every one of these overflows needs a round the opening one
cannot be: a bar with every creep in it, a purse deep enough to climb a rung, a token in hand. The
sheets [`README.md`](../README.md) describes are all opening rounds, and writing late-round numbers into
one would draw a game this project does not ship — the header reads its wave and health off the run.
So a shot now carries a `wave`, and the capture **plays the rounds before it** through the loop's own
`Commit` and `GoOn`: the towers are `CoverThenUpgradeBot`'s (the simulation's own scripted player), the
wave is one of every creep the purse can still cover, cheapest first. The second rule is the capture's
and is framing, not balance — a scripted player that sends one column never fills a bar. A shot also
carries `upgrade`, a path of rungs the placed tower is climbed through, because the longest label on the
surface is on a Bishop and a Bishop is climbed to, not placed.

**Two facts came off the played-forward run rather than out of the ask:**

- **The bot never spent a token.** Eight rounds in, the header reads *3 held · 0 spent*: on half the
  purse, with the other half going to creeps, the bot stood no tier-2 rung by wave 9 and so had nothing
  to climb a capstone from. The "spent" state on the counter is therefore drawn from a shot that buys the
  Consecration in the round being photographed (`tokens-w9-spent-*`), not from the run's own history.
- **Health 198 of 800 at wave 9.** The same policy leaks three quarters of the pool. Nobody tuned it; it
  is what the sheets carry in their headers, and it is the run every candidate shares.

## The wave bar — [`wave-bar.txt`](wave-bar.txt)

Seventeen filled boxes and the trailing empty one want 3280 panel units of the 1872 between the margins.
As shipped, ten fit and the eleventh is cut, which is the frame #270 photographed.

| sheet | what it draws | what the 1× frame says |
|---|---|---|
| `wave-bar-as-built.png` | the bar as it ships | ten boxes; Grave Robber onward off the right edge |
| `wave-bar-scrolls.png` | a horizontal `ScrollView` | the same ten, a scroller under them; the box a creep is added through is off screen until scrolled to |
| `wave-bar-wraps.png` | flex-wrap onto a second line | all seventeen and the empty box, nine over eight; the row grows from 108 to about 200 units, upward into the board |
| `wave-bar-shrinks.png` | 128-unit boxes, type at 18 | thirteen fit; *Skeleton Warrior* already touches its box edges at 18 |
| `wave-bar-shrinks-to-fit.png` | 96-unit boxes, type at 13 | all seventeen and the empty box fit; a name is eleven pixels tall at 1600×900 |

## The offer's longest rung — [`offer.txt`](offer.txt)

*Consecration   1 capstone token* on a 208-unit panel, drawn past both edges onto the board.

| sheet | what it draws |
|---|---|
| `offer-as-built.png` | the ladder as it ships, the label overflowing both sides |
| `offer-fits-its-longest-rung.png` | width auto; the panel grows to its longest rung and hangs to the right of its hex, because it is placed by its left edge at half of 208 |
| `offer-is-wider.png` | a fixed 336, pulled back by half the difference so it stays centred |
| `offer-prices-beneath-the-name.png` | each rung two lines, name at 18 over price at 15 |

## A standing token count — [`tokens.txt`](tokens.txt)

Three places the count could live, each drawn in the state where tokens are held (wave 9), the state
where one has just been spent (wave 9, the Consecration bought this round), and the state where none has
been granted yet (wave 2, the moment the ticket names — a Bishop's ladder that opens on nothing).

| sheets | where the count lives |
|---|---|
| `tokens-*-in-the-header.png` | a fourth header field after the gold: *Capstone tokens N held · N spent · N to come* |
| `tokens-*-as-pips.png` | the word and three pips, one per grant on the schedule: filled held, dim spent, hollow to come |
| `tokens-*-at-the-ladder.png` | with the ladder open, a line at its top; with it shut on a tower whose only rung is a capstone, a line at the hex saying why |

**The words are placeholders** — see the `.txt`. And the at-the-ladder line overflows its own 208-unit
panel at wave 2 in exactly the way the offer does, which is the finding restated rather than a candidate
answering it.

## What is not here

A portrait rail for the wave (the standing answer from 17 August, waiting on `RosterThumbnails`); shorter
words for the token price (a word a player reads is Sam's to spell); a count on either bottom bar (both
already overflow at this roster, so a tag at the end of either is off screen); and
[#275](https://github.com/ssalter21/tower-defense-game/issues/275)'s palette candidates, which were not
rendered here — the three shapes are the wave bar's three, so that sitting can read them off these.
