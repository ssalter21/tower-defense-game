# The snapshot is the only thing a view may draw game state from

Nothing that draws the match reads simulation internals. The snapshot is the whole interface, and the rule is enforced by the shape of the types rather than by convention.

A creep's position in a snapshot is a distance along the path plus a lateral offset, never a point in a plane. Turning that into somewhere to stand is the view's job, and it needs the route, which the view already has because the map is static data loaded once.

## Consequences

Two dimensions stop at the simulation boundary. The simulation holds no 2D positions at all, which is also why target references carry none (ADR-0016) and why hex orientation is a view question (ADR-0020).

Fields the view never sees — a tower's cooldown counter, for instance — are exactly the fields the rolling state hash (ADR-0004) exists to cover, because nothing else would notice them drifting. That is why `TowerState` has no `Cooling` member: a tower between shots is idle to look at.
