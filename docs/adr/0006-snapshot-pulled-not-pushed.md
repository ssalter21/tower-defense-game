# The snapshot is pulled, not pushed

A match builds a snapshot only when someone calls `PullSnapshot`. A match nobody asks for a picture of never builds one.

Match events work the same way: they go to an object passed into `Advance`, so a re-simulation that passes nothing discards them by construction rather than by remembering to.

## Consequences

Instant-resolve needs no headless mode — it is the ordinary loop with nothing pulling snapshots and nothing collecting events. This is what makes ADR-0005's single surface possible.
