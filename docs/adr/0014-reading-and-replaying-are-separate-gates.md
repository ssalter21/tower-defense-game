# Reading and replaying are separate gates

Reading a record needs a known format version and nothing else. Replaying it needs, in addition, every table and version the record was made under to match what is in front of it. Which those are is the record kind's business: a replay bundle compares the simulation version, the unit table's content hash, the ruleset's ([0047](0047-a-bundle-stamps-its-ruleset.md)) and the map hash; a command stream compares the simulation version and the unit table, ruleset and anchor schedule hashes ([0039](0039-the-command-stream-is-the-only-route-into-a-run.md)).

When a replay gate fails it refuses by name and leaves the record perfectly readable, so a defense whose ruleset has moved on can still be listed, drawn and shown as historical.

## Consequences

The two gates throw two exception types, and neither derives from the other — both derive straight from `Exception`. No `catch` can accidentally treat "these bytes are not a record" and "this record is from an older ruleset" as one thing. The whole reason there are two is that the second leaves the record usable.
