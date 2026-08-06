# The rolling state hash is always on, and folds per tick

The match folds its internal state into a rolling hash every tick. There is no constructor argument and no property that turns it off, because a flag would create a configuration in which the central assertion of the architecture is not running.

The fold is per tick rather than at the end of the match: an end-of-match hash says only whether two runs agreed, while a per-tick hash says which tick they stopped agreeing on.

## Consequences

It costs a fold over a few dozen integers per tick, which is measured against the re-simulation budget alongside everything else.

What is folded is internal simulation state, not the snapshot — the snapshot is a view artifact (ADR-0007) and hashing it would tie the assertion to a rendering concern.
