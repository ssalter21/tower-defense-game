# Fixed-point arithmetic, truncating toward zero, overflowing loudly

The simulation computes in fixed-point (`Fix64`) rather than floating point, because a replay has to produce the same result on every machine that runs it and floating point does not promise that. Rounding is truncation toward zero for both multiplication and division, and overflow throws rather than saturating or wrapping.

## Consequences

Truncation is a rule of the game, not an implementation detail: changing where the sim truncates changes stored replays even though no number in any content file moved. That is what ADR-0009's simulation version exists to record.

Overflow throwing means an invariant breach stops the match rather than producing a plausible wrong number that diverges the state hash somewhere later and harder to find.
