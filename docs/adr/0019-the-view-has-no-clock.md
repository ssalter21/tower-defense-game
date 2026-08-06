# The view has no clock, and interpolation is a pure function

`MatchView` offers three calls — step one tick, re-simulate to a tick, and draw — and something else decides when to call them. In the running client that is `PlaybackController` and nothing else; the frame capture and the tests drive the same three by hand, which is why they are three separate calls.

Interpolation takes the two snapshots and an alpha, and nothing else. It is not a playback head: it cannot drift, it holds no accumulated position, and drawing the same alpha twice draws the same picture.

## Consequences

Effects age in simulation ticks, so nothing in the view runs on a clock of its own. Playback speed multiplies the one clock and nothing else.

There is no clock in this client that the simulation does not drive, which is what makes a frame capture reproducible and a test able to assert on an exact tick.
