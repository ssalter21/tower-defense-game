# No constant in the view is a simulation input

`MatchTuning` holds every number that decides what the match looks like; `SceneFraming` holds the numbers that frame the static scene once. Change every constant in either file and the match's result, its per-tick hash and its landmark table are byte-for-byte identical — only the picture changes.

That is the test of whether a number belongs in the view rather than the simulation.

## Consequences

The split between the two view files is where the numbers come from, not what they are for: `SceneFraming` frames a static scene once, `MatchTuning` is consumed every frame by things the simulation drives.

The classes that draw the match cannot reach the simulation at all, and that is checked by a test rather than left as a convention.
