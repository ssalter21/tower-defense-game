# The chrome at the rounds that broke it

Five sheets of the shipped chrome at 1600×900 — the size the built player runs at — in the two late-round
states the roster's forty-four rows stopped it fitting: a wave with every creep in it, and a Bishop's ladder
offering a capstone for a token. Issue [#282](https://github.com/ssalter21/tower-defense-game/issues/282)
rendered candidates here for three of the four places the chrome overflowed; sitting
[#285](https://github.com/ssalter21/tower-defense-game/issues/285) signed one of each on 11 September 2026
and the candidates came out, classes and sheets both. What is here now is what ships, drawn at the rounds the
candidates were drawn at, so the next reader sees the overflow answered rather than a picture of it.

```powershell
./tools/capture-chrome-overflows.ps1
```

The sheets are the ones [`spec.json`](spec.json) names. The three answers, and the decision-log entry that
records them:

| sheet | what it shows | what was signed |
|---|---|---|
| `wave-bar.png` | wave 9, seventeen creeps and the empty box | **The boxes scroll sideways.** A horizontal `ScrollView`, the scroller under the boxes once they overflow, the empty box scrolled into view on every redraw. A holding answer: the chrome as a whole waits on a direction |
| `offer.png` | wave 9, a Bishop's ladder open | **The price beneath the name.** A rung is two lines, 64 tall, the name at 18 over the price at 15; the panel stays 208 wide and centred on its hex |
| `tokens-w2.png` | wave 2, a Bishop standing, no token granted yet | **A fourth header field after the gold**: *Capstone tokens 0 held · 0 spent · 3 to come*. This is the moment the ticket named — a ladder that opens on nothing — and the header now says why |
| `tokens-w9.png` | wave 9, three held, the ladder open | the same field with a token to spend |
| `tokens-w9-spent.png` | wave 9, the Consecration bought this round | the same field with one spent, read off the composed round the way the gold is |

The fourth place, a beside prop on an occupied tile, is a board question and lives in
[`docs/frames/beside-props/`](../../frames/beside-props/README.md).

## How the rounds are reached

Every one of these needs a round the opening one cannot be: a bar with every creep in it, a purse deep enough
to climb a rung, a token in hand. Writing late-round numbers into an opening round would draw a game this
project does not ship — the header reads its wave and health off the run — so a shot carries a `wave`, and the
capture **plays the rounds before it** through the loop's own `Commit` and `GoOn`: the towers are
`CoverThenUpgradeBot`'s (the simulation's own scripted player), the wave is one of every creep the purse can
still cover, cheapest first. A shot also carries `upgrade`, a path of rungs the placed tower is climbed
through, because a Bishop is climbed to, not placed. See `UiPreviewCapture.PlayTo`.

Two facts come off that run and are in every header here: **the bot never spends a token** (three held, none
spent at wave 9 — it stands no tier-2 rung by then), so the spent state is drawn from a shot that buys the
Consecration in the round photographed; and **health is 198 of 800 at wave 9**, because the same policy leaks
three quarters of the pool. Nobody tuned it.

## What holds the answers

`Tests.PlayMode/ChromeLayoutTests` carries one assertion per overflow, against the roster as shipped rather
than the count that fitted today: the bar scrolls to its empty box with every creep in it; every name on the
roster fits a rung over the longest price; the header's four fields sit before the button with the spacer
still holding width; and, since [#275](https://github.com/ssalter21/tower-defense-game/issues/275), every
root the round may build fits the palette bar on the row beneath — the bar in every sheet here. The next row
added turns one of them red rather than clipping quietly.
