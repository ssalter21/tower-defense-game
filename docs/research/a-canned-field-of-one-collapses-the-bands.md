# A canned field of one collapses four percentile bands into two

**Research note** · 8 August 2026 · measured while wiring the performance bonus onto the canned field in
[`content/field.txt`](../../content/field.txt)

**Question:** the ruleset authors four progressive bands — 0th percentile pays nothing, 50th pays 5% of the
income base, 75th pays 10%, 90th pays 20%. Against the canned field that stands in for a ghost pool, which of
them does a run actually land in, and is the bonus a real number?

**Inputs:** the committed map, defense, roster, schedule and ruleset; `Run` and `Sweep` over the committed
simulation assembly; the canned pool of one from `content/field.txt`, the pool of four the run suite uses, and
`content/wave.txt` pointed at the same argument.

---

## Bottom line

**The bonus pays, and the bands are not mistuned — the stand-in distribution is degenerate.** A run measured
against the canned field earns roughly **9% of its flat base** in performance bonus, against a ceiling of 20%.
Nothing about that is a rounding artefact: half a run's rounds clear the field outright and the other half fall
short.

But the field the bonus is read off is a **near point mass**. Ten samples of the canned opponent, each averaged
over K=10 opponents exactly as a run's own round is, land between **14 and 19 gold**. A run's round is
therefore above the whole field or below the whole field, and almost never inside it: the percentile it comes
back with is 0 or 100, and the 50th and 75th bands are reachable only by a round that dealt exactly 18 or
exactly 19 gold — a two-gold window on a scale of hundreds.

**So the four authored bands behave as two.** That is a property of a population of one, not of the thresholds.
The same code against a pool of four members produces percentiles of 80, 80, 80, 90, 90, 100, 100, 100, 100 and
100 across a ten-wave run — all four bands live, and the middle two doing work.

The conclusion for the thresholds is therefore: **do not retune them against this field.** The number they need
to be right about is the spread of a real population, and this population has no spread. Step 6's ghosts are
what makes the middle bands testable; until then the bonus is correct, non-zero and progressive, and it
discriminates at one threshold rather than three.

## What was measured

### The spread of a field, by pool

Each field is ten samples, each the average of K resolutions, drawn from the run's own seed. The table is the
range the ten samples span.

| pool | members | sample range, gold | spread as a share of the mean |
| --- | --- | --- | --- |
| `content/field.txt` behind the committed defense | 1 | 14 – 19 | ~30% |
| `content/wave.txt` behind the committed defense | 1 | 120 – 130 | ~8% |
| four thinned defenses against three wave lengths | 4 | 134 – 271 | ~70% |

A one-member pool's spread is the residual dice noise of one fixed pairing, and averaging each sample over K
shrinks even that. A four-member pool's spread is the difference between the members, which is an order of
magnitude larger and is the thing a percentile is supposed to be measuring.

### What the bonus came to

The committed sweep: six creeps, eight seeds each, ten waves, K=10, no death. `income_base_gold` and
`bonus_gold` are the two columns this ticket added to `content/sweep.csv`.

| creep | leak cost dealt | base paid | bonus paid | bonus as a share of base |
| --- | --- | --- | --- | --- |
| grunt | 1916 | 8000 | 780 | 9.8% |
| runner | 1218 | 8000 | 585 | 7.3% |
| wisp | 1199 | 8000 | 605 | 7.6% |
| bulwark | 1762 | 8000 | 645 | 8.1% |
| drifter | 1792 | 8000 | 835 | 10.4% |
| lancer | 2007 | 8000 | 935 | 11.7% |

At 32 seeds a creep the same six come in between **7.8% and 10.4%**. The ordering tracks leak cost dealt, which
is what the bonus is a function of, so the column separates the roster the same way the offense column does.

### What the field's calibration decides

Pointing the same sweep at `content/wave.txt` — 380 gold of creeps, which no build phase in this economy can
compose — gives a field whose rounds are worth about 125 gold. Every creep then deals about 15 gold a round
and **the bonus is zero on every row**, alongside a win rate of zero on every row.

A field that outspends every opponent it faces therefore turns the offense half of the economy off as well as
the win-rate column, which is a second reason `field.txt` is a build phase's output rather than the skeleton's
authored match.

### What the extra income moved

The bonus is money, so it buys creeps, so the report moves. The committed sweep before and after, at eight
seeds a creep:

| creep | win rate before | win rate after | leak cost dealt before | after |
| --- | --- | --- | --- | --- |
| grunt | 2500 bp | 6250 bp | 1188 | 1916 |
| runner | 0 bp | 2500 bp | 706 | 1218 |
| wisp | 0 bp | 2500 bp | 690 | 1199 |
| bulwark | 2500 bp | 7500 bp | 1191 | 1762 |
| drifter | 1250 bp | 5000 bp | 1017 | 1792 |
| lancer | 3750 bp | 7500 bp | 1231 | 2007 |

At 32 seeds a creep the win-rate column moves from a **16% – 34%** band to a **41% – 66%** band. It is a wider
band around a more useful centre, so the report is a better instrument after the bonus than before it — which
is the sign that the offense half of the economy was doing nothing and now is.

## What this does not say

- **It says nothing about whether 5, 10 and 20 are the right percentages.** What it says is that the field this
  build measures against cannot tell them apart. Retuning them here would be fitting three numbers to a
  distribution with one value in it.
- **It says nothing about the thresholds either**, for the same reason. 0, 50, 75 and 90 are a shape; the shape
  is untested until a population has a shape of its own.
- **It is not evidence that the roster is balanced.** Every number above moved because every run got richer, and
  a uniform income rise is not a balance change. The ordering between creeps is what carried over, and it did.
