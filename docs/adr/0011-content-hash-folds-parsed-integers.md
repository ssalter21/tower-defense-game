# The content hash folds parsed integers, not file bytes

The content hash and map hash are folds over the *parsed* integers in field order, not hashes of the bytes read from disk. The algorithm is FNV-1a, 64-bit, chosen for being specified rather than for being strong.

## Considered options

Hashing file bytes would make reindenting a column, editing a comment, or changing a line ending retire every stored record pinned to the old hash. The hash would stop meaning "the numbers changed" and start meaning "somebody touched the file" — a signal nobody can act on and everybody learns to override.

## Consequences

A real tuning change moves the hash and nothing else does.

`System.Security.Cryptography` is on the banned list for the simulation assembly: nothing in the simulation may reach for a platform-provided primitive, because a platform primitive is exactly the kind of thing that can differ between machines and break a replay.

There is one place where hashing bytes is the right thing, and it is not this one.
