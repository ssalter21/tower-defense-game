# The order of work inside a tick is part of the rules

Within one tick the simulation does its work in a fixed order: creeps move, dying creeps age, projectiles fly and land, towers act, the dead are cleared away, then the tick number advances and the wave releases whatever is due.

That order is a rule of the game rather than an implementation detail. Changing it changes stored replays even though no number in any content file moved, so it is covered by the simulation version (ADR-0009) rather than the content hash.
