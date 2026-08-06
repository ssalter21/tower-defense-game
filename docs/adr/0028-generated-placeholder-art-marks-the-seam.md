# Placeholder geometry is generated in code, marking the seam where real art goes

The hex tile mesh and the match's decorations are generated from code rather than authored as assets. The tile mesh is the seam where a real tile model goes, and nothing outside that one class depends on its being generated.

## Consequences

Choosing a tile model is an art decision, and art decisions are not made unattended — generating a placeholder keeps the project runnable without pre-empting one.

Decoration geometry is real geometry only: a tracer is a thin stretched box and a spark is a small one. Nothing in the decorations file is load-bearing for what the match looks like at rest, and the rule that decorations cannot affect the simulation is enforced by the interface's shape rather than by convention (ADR-0008).

The playback bar is built in code from constants, like the camera and the light, for the same reason.
