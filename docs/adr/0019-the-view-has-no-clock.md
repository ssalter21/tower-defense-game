# The view has no clock, and interpolation is a pure function

`MatchView` offers three calls — step one tick, re-simulate to a tick, and draw — and something else decides when to call them. In the running client that is `PlaybackController` and nothing else; the frame capture and the tests drive the same three by hand, which is why they are three separate calls.

Interpolation takes the two snapshots and an alpha, and nothing else. It is not a playback head: it cannot drift, it holds no accumulated position, and drawing the same alpha twice draws the same picture.

## Consequences

Effects age in simulation ticks, so nothing that draws the match runs on a clock of its own. Playback speed multiplies the one clock and nothing else.

The camera's reset ease is the one thing here measured in seconds, and it is outside this rule rather than an exception to it: it draws nothing, and it is handed its elapsed seconds rather than reading them, so the frame capture and the tests place the camera by hand and exactly.

Nothing the simulation drives runs on a clock the simulation does not, which is what makes a frame capture reproducible and a test able to assert on an exact tick.
