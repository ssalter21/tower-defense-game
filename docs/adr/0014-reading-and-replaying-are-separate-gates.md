# Reading and replaying are separate gates

Reading a record needs a known format version and nothing else. Replaying it needs, in addition, the simulation version, the content hash and the map hash all to match what is in front of it.

When a replay gate fails it refuses by name and leaves the record perfectly readable, so a defense whose ruleset has moved on can still be listed, drawn and shown as historical.

## Consequences

The two gates throw two exception types, and neither derives from the other — both derive straight from `Exception`. No `catch` can accidentally treat "these bytes are not a record" and "this record is from an older ruleset" as one thing. The whole reason there are two is that the second leaves the record usable.
