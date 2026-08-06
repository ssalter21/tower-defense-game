# Exactly one match root in the scene, forever

There is one root object owning the match on screen, and the code refuses a second rather than supporting several.

## Considered options

The alternative to "exactly one" is a budget — how many matches may be on screen, what happens when the limit is reached, which one wins. That is a set of questions this project has no reason to answer.

## Consequences

Nothing in the root is a simulation input: it reads the map and the record, and the match on screen is the recorded one, seed included.
