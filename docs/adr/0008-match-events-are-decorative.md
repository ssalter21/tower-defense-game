# Match events are purely decorative

Every event on `IMatchEvents` exists for something to react to visually. No event carries simulation state, and dropping every one of them changes nothing about how a match resolves.

Events are delivered only if someone subscribed, so a re-simulation that passes no sink discards them by construction (ADR-0006).

## Consequences

Two of the events exist only so a landmark table can be derived from a run. That is a reporting need rather than a simulation one, which is why they are events rather than fields on the snapshot.
