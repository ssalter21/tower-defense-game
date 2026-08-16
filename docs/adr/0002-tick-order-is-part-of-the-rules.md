# The order of work inside a tick is part of the rules

Within one tick the simulation does its work in a fixed order: whatever has run out expires, creeps move, dying creeps age, projectiles fly and land, towers act, auras pulse, the dead are cleared away, then the tick number advances and the wave releases whatever is due.

That order is a rule of the game rather than an implementation detail. Changing it changes stored replays even though no number in any content file moved, so it is covered by the simulation version (ADR-0009) rather than the content hash.

## Expiry opens the tick and emission closes it

The first and last of those phases arrived together with per-unit timed effects (ADR-0056) and their positions are load bearing rather than convenient. An effect landing on tick *t* is in force for ticks *t+1* through *t+duration*, and that has to be one sentence whichever phase emitted it — a bubble fired with an attack lands in the middle of a tick and one that pulses lands at the end of one. Expiring at the top and emitting at the bottom is what makes "expires exactly on its duration" mean the same thing for both.
