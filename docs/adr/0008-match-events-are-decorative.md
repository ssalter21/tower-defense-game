# Match events are purely decorative

Every event on `IMatchEvents` exists for something to react to visually. No event carries simulation state, and dropping every one of them changes nothing about how a match resolves.

Events are delivered only if someone subscribed, so a re-simulation that passes no sink discards them by construction (ADR-0006).

**A parameter is an entity id, a count, or an enum member off the emitter's row.** Nothing else, and the shape
is asserted by reflection in `MatchTests` rather than left to the doc comment. The rule refuses anything an
effect could be built out of and hold on to — a position, a handle, a reference with a lifetime — so a `Hex`, a
`Fix64` and a `Bubble` are all refused, and it is exactly the `Bubble` case that shows why the enum is
admitted rather than tolerated: an enum member is a value with no identity and nothing behind it, where the
struct it came off would carry a radius, a period and a duration a listener could start aging on its own.

The enum was let in for `bubblePayload` when [#253](https://github.com/ssalter21/tower-defense-game/issues/253)
added the two events a bubble going off emits. An `int` would have kept the older, tighter assertion and cost
the caller a cast back into the type it came from, which is the smuggling this rule exists to make visible
rather than the discipline it exists to enforce.

## Consequences

Two of the events exist only so a landmark table can be derived from a run. That is a reporting need rather than a simulation one, which is why they are events rather than fields on the snapshot.

Two more say a bubble went off — a blast where a shot resolved, an aura where its period came round — and name
the entity it was centred on rather than the place. A listener resolves the position out of the snapshot it is
already drawing, which is [ADR-0007](0007-snapshot-is-the-only-view-input.md) holding: an event that carried
the centre as a coordinate would be a second opinion about where something is, and the two could disagree.
