# Every invariant in the simulation assembly is an unconditional throw

Invariants throw. Not an assertion, not a conditional-compilation macro, not a logged warning.

## Considered options

An assertion compiles out of the build that ships — which is precisely the build a desync will be found in months later, with nothing left to point at. The whole arrangement rests on the loud failure, so there must be no configuration in which it is switched off.

## Consequences

The banned-API scan enforces the other half: `Debug.Assert`, `Trace` and `[Conditional]` are all refused inside this assembly.

There are two invariant exception types, and the throw is always unconditional regardless of build configuration.
