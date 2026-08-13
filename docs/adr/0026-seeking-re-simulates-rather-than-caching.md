# Seeking re-simulates from the start; there is no snapshot cache

Dragging the scrub bar to a tick builds a fresh match and advances it to that tick, rather than reading a cached snapshot.

## Considered options

A cache can never disagree with itself, so scrubbing one would prove nothing. Re-simulating makes every drag of the scrub bar a fresh determinism check that either produces the same match or does not.

## Consequences

The events of the re-run ticks are discarded by nobody subscribing, rather than by anybody filtering — `Advance` takes the event sink as an argument and the seek call passes none, so the whole match's tracers, flashes and sparks are never built. Seeking to the end therefore does not detonate them all in one frame.

Only the decorations are cleared on a seek. The object pool is deliberately left alone, because the draw at the destination rebinds by id anyway (ADR-0021).

This puts re-simulation on the interactive path, which is what the re-simulation performance budget exists to protect.

The budget measures the cost of a **tick**, not the cost of a match: match times are scaled to the length the multiple was calibrated against, so slowing the release cadence cannot redden it. That is what stops the gate reporting a regression every time the clock moves, and it leaves one thing unwatched — seeking costs length times cost-per-tick, so a run stretched far enough is slow to seek with every tick inside budget. Nothing measures that yet, and it wants its own number rather than this one.
