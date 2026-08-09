# Reading and replaying are separate gates

Reading a record needs a known format version and nothing else. Replaying it needs, in addition, every table and version the record was made under to match what is in front of it. Which those are is the record kind's business: a replay bundle compares the simulation version, the unit table's content hash, the ruleset's ([0047](0047-a-bundle-stamps-its-ruleset.md)) and the map hash; a command stream compares the simulation version and the unit table, ruleset and anchor schedule hashes ([0039](0039-the-command-stream-is-the-only-route-into-a-run.md)).

When a replay gate fails it refuses by name and leaves the record perfectly readable, so a defense whose ruleset has moved on can still be listed, drawn and shown as historical.

**Each kind declares its stamps to one walk.** `ReplayGate.Require` takes the pairs a record kind names — the value the record stored beside the live value to compare it against — walks them in the declared order and refuses on the first mismatch. The kinds drifted while each re-derived the walk as its own run of `if` statements, and they drifted in the direction that shape hides: a check that is not there is a branch that is not there, and an absent branch looks like nothing at all. An absent row is a gap in a list, and the two kinds' lists can be read side by side.

A record that carries no value for a declared stamp refuses on it. What is missing is the record's claim, and a missing claim agrees with no live value there is — which is what retires a bundle written before the ruleset field existed ([0047](0047-a-bundle-stamps-its-ruleset.md)).

## Consequences

The two gates throw two exception types, and neither derives from the other — both derive straight from `Exception`. No `catch` can accidentally treat "these bytes are not a record" and "this record is from an older ruleset" as one thing. The whole reason there are two is that the second leaves the record usable.
