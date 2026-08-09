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

## What a bump costs a record nobody can make again

Retiring a record is cheap when the record is regenerable, and every record in `content/` is — except one kind. `content/golden/` keeps one bundle per defense record format version that has ever shipped, and those are irreplaceable by construction: the writer emits the current version and only the current version, so a version-0 bundle can never be recorded again. They exist to prove that the reader branch for each retired format still reads.

Left alone, that makes a simulation version bump quietly destructive. It would retire one more golden every time, and the reader branch that golden stood for would go unproven from then on — the pool shrinking by one on each bump, permanently, as a side effect of a decision about rules.

So the goldens are **restaged, not replayed** (`ReplayBundle.RestageUnderCurrentRules`, and the `restage` verb on the command line). That is not a gate being softened. A golden is evidence about a *reader*: that these bytes still parse into that defense and that wave. Restaging parses them exactly as replaying does before running the result to a pinned outcome, and the one check it sets aside — "were these the same rules?" — is a question about a competitive record, not about a reader branch. It is set aside by name, in a differently-named operation whose output says on its own first line that it is not a replay. The version gate itself is asserted on whichever golden is current, which is the one bundle that can always be re-recorded.

The rule this leaves behind: **a bump may retire a record, but it may never retire the only evidence for a branch.** If a future identity field ever acquires irreplaceable artefacts of its own, it needs the same treatment.
