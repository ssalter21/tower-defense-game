# The simulation assembly is handed text and bytes, never paths

Content loading takes a string of text and cannot open anything. Record reading is handed bytes. Nothing in the simulation assembly knows where its input came from, and error messages name the record rather than a file path.

## Consequences

Parsing is culture-invariant by construction: `int.Parse` is not used, nothing consults a culture, and a decimal point on a data line is a load error raised before tokenising rather than a silently truncated or locale-swapped number.

The simulation stays runnable anywhere — a test fixture, the headless command line, a server re-validating a submitted record — without a filesystem or a platform primitive in reach. This is the same constraint that keeps `System.Security.Cryptography` off the hashing path (ADR-0011).
