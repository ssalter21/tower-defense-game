# A record carries three identity fields, owning three non-overlapping things

A stored record is identified by three separate numbers rather than one version:

| Field | Owns | Changes when |
|---|---|---|
| Record format version | **Layout** — where the bytes are | A field is added, moved or widened. Counted per record kind (ADR-0010) |
| Simulation version | **Behaviour** — tick order, targeting, the rounding rule, the dice algorithm | Any rule changes |
| Content hash | **The numbers** | Recomputed at load from the parsed tables; never set by hand |

The point of separating them is that somebody about to change something can tell which one is theirs without having to understand the other two. The simulation version is the only one of the three a person is ever expected to change by hand.

## Consequences

The awkward case is the one the split exists for: changing a rounding rule moves nothing in any content file and moves no byte layout, so neither the content hash nor the format version moves — and every stored replay now produces a different result. That is a simulation version bump.

The converse matters as much. Retuning a tower's damage is *not* a simulation version bump: the content hash already covers it automatically, and bumping the simulation version as well would retire every record made under an unchanged ruleset for no reason.
