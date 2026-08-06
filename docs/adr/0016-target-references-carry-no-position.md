# A target reference is a kind and an id, and carries no position

`TargetRef` names what a projectile is aimed at. It holds no coordinate of any sort.

## Considered options

A projectile that stored where it was going would need that point kept in step with a target that moves. That is either homing logic in the simulation, or a projectile that flies at where its target used to be.

Storing a reference instead makes homing free: the simulation counts down, and the view interpolates toward wherever the target is in the snapshot it is drawing.

## Consequences

This keeps free 2D out of the simulation permanently (ADR-0007). There is no field here that could hold a point, so nobody can add one without changing this type — and changing this type is a change to the record format's shape.

The union has a tower arm because the id space is shared: towers and creeps are one kind of thing in this project, so a placed unit shooting at another placed unit needs no new machinery.

A shot lands whatever became of its target, rather than being cancelled when the target dies.
