# The view holds two snapshots and matches entities by id

The view keeps the last two snapshots and nothing older. That is enough to interpolate between ticks, and it is what makes a vanished entity need no handling at all: it is an id that stopped appearing, and the pool releases it by subtraction.

## Consequences

There is no despawn message anywhere in this project, and a projectile whose target died mid-flight is not a special case in any file.

An entity's view object is bound to its id for as long as that id keeps appearing. There is exactly one way an object returns to the pool: its id stopped being in the snapshot.
