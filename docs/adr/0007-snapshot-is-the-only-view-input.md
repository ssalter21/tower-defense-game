# The snapshot is the only thing a view may draw game state from

Nothing that draws the match reads simulation internals. The snapshot is the whole interface, and the rule is enforced by the shape of the types rather than by convention.

A creep's position in a snapshot is a distance along the path plus a lateral offset, never a point in a plane. Turning that into somewhere to stand is the view's job, and it needs the route, which the view already has because the map is static data loaded once.

## What is on a unit is a snapshot field

A creep carries the percentage its speed is displaced by, the percentage its armour is displaced by, and the
pool standing in front of its health. A tower carries the percentage its cooldown is displaced by. Four fields,
and they are the whole of what a timed effect looks like from outside the simulation.

**They are the snapshot's and not an event's, because state survives a scrub and a moment does not.** Seeking
re-simulates from the start and subscribes nobody, so the events of the re-run ticks are never built at all
([ADR-0026](0026-seeking-re-simulates-rather-than-caching.md)). An "a slow landed" event would therefore be
heard once, on the tick it landed, and never again — drag the bar back across that tick and the creep is still
slowed in the simulation and no longer slowed on screen, which is a view that disagrees with the match it is
drawing. A snapshot field is rebuilt every time a view asks for one, so it is right at every tick a view can
reach. That is the line [ADR-0008](0008-match-events-are-decorative.md) draws from the other side: the two
events a bubble emits say a bubble *went off*, which is a moment, and never what it left behind.

**A magnitude and not a flag.** "Is it slowed" and "what is on it" are different contracts, and only the
second tells a forty percent slow from a ninety percent one, or a slow from a haste — a magnitude is a
displacement and the sign is which way, so one field carries both directions and a view that draws one has
already been handed the other. What a modifier *does* stays derived and is not repeated here: the speed a
creep actually walks at and the armour a landing resolves through are the simulation's own arithmetic, and a
second copy of either is a number that can go out of date.

**One number for the two shield pools.** A creep's authored pool and a granted one are separate in the
rules — the granted one is spent first, because it is the one that can be taken away — and the state hash
folds them apart. Which of the two a point came off changes nothing a view could draw, so the snapshot adds
them together.

**The cooldown modifier is carried and the cooldown counter is not.** That is the paragraph below holding
rather than bending: a tower between shots is idle to look at, so how far through its wait it is stays exactly
the sort of field the rolling state hash exists to cover. What is on it is a different question, and a tower
firing at twice its authored rate for no visible reason is what this field exists to stop.

## Consequences

Two dimensions stop at the simulation boundary. The simulation holds no 2D positions at all, which is also why target references carry none (ADR-0016) and why hex orientation is a view question (ADR-0020).

Fields the view never sees — a tower's cooldown counter, for instance — are exactly the fields the rolling state hash (ADR-0004) exists to cover, because nothing else would notice them drifting. That is why `TowerState` has no `Cooling` member: a tower between shots is idle to look at.
