# 0046 — An absent ladder folds nothing, and the content hash covers content the simulation never reads

`content/upgrades.txt` folds into the unit table's content hash — the one value every record header already
carries. A ladder with no edges folds nothing, so a golden with no ladder file beside it keeps the hash it was
recorded with, forever.

## What was decided

**The ladder joins the value all four writers already pass.** `GhostRecord`, `WaveRecord`, `ReplayBundle` and
`CommandStream` each stamp exactly one `types.ContentHash`, so folding the ladder in where both files are
parsed covers all four record kinds without a single writer signature moving, without a field being added and
without any of the four format versions bumping. The ghost stays at 1, the wave, the replay and the command
stream stay at 0, and the simulation version stays at 2 — nothing in the tick loop can observe an edge, so
none of them has anything to say about one.

**A ladder with no edges folds nothing, and that is what keeps an irreplaceable golden legal.**
`GoldenRecordTests.The_table_a_golden_was_recorded_against_is_committed_beside_it` *recomputes* a pinned
table's content hash and compares it against bytes frozen in the golden's header. A formula that
unconditionally gained a ladder term would recompute `content/golden/defense-0.replay`'s to a value that can
never equal the one sitting in that file — and the writer emits the current format version and only the
current one, so that bundle cannot be re-recorded. It would be ADR-0009's rule broken by name: a bump may
retire a record, but it may never retire the only evidence for a branch. Folding nothing is the only one of
the three ways out that does not pay a price this repository has already refused.

**Absence is a fact about the record, not a default supplied on its behalf.** The obvious objection is that
`ruleset.txt` refuses rather than defaults, and it is answered rather than waved at: the loader still opens
`upgrades.txt` every time it opens `units.txt`, still refuses it by name, and `--upgrades` is required on
every verb that takes `--units`, because an optional content file *is* a default. What folds nothing is a
ladder with no edges, not an unread file. `RecordFormat` already blesses the shape in its remarks on
`GhostVersion` — the version-0 branch defaults the map handle, legal only because the field cannot change a
replay's result — and the test is whether the value can reach an outcome. Here it provably cannot, because
the simulation is never handed a ladder at all ([0045](0045-the-ladder-is-a-graph-not-a-list.md)).

**The hash is knowingly conservative.** An edge cannot change a simulation's output today, so editing one
retires stored records that would replay identically. It is taken anyway, because a record that pins a content
bundle is a claim about *the content the run was authored under* — and once a defensive build phase reads
succession, the same command stream against a different ladder is a different match. A hash outside the bundle
would have to be moved inside on that day, and every record made in between would be ambiguous about which
ladder it meant. Retiring records too eagerly costs regenerated goldens; retiring them too late costs records
that quietly mean the wrong thing.

## What it costs

**The table `Match` receives carries a hash influenced by a file the simulation never sees.** It is inert —
nothing in the tick loop reads `ContentHash`, and the value reaches no rule — but it is real, and it softens
"the simulation is never handed a ladder" to "structural, except for one number". Zero format churn across
four record kinds is worth that, and it is written down here rather than discovered.

**Anything that reconstructs the hash needs the ladder, including things that read no edge.** The view parses
`content/units.txt` with the simulation's own parser and takes `content/match.replay` through
`ReplayBundle.Replay`, which compares the record's stamped hash against the parsed table's. So on the day the
live hash moves, the player either ships `upgrades.txt` beside the other five content files or its shipped
record stops passing the gate — and `tools/sync-streaming-content.ps1` names the two failures its whitelist
exists to catch as *content that ships and is never read* and *content that is read and does not ship*. This
is an honest third case: content that ships because a hash covers it. Which way that resolves belongs to the
effort that authors the first edge, and `MatchContentTests.TheShippedRecordPassesTheReplayGate` is what will
present the bill.

**The hash moves once, and on one day.** Nothing retires when the empty file lands. On the day the first edge
is authored the live hash moves and five files follow: `content/match.replay` and `content/run.commands` are
re-recorded, `content/golden/defense-1.{replay,units,result}` are re-pinned, and a `defense-1.upgrades` is
pinned beside them so the recomputation can reach the ladder it was recorded against.
`content/golden/defense-0.*` is untouched forever. Nothing else moves: the golden trace, the run outcome and
the sweep are outputs of runs, no simulation output changes, and `content/sweep.csv` records a ruleset hash
rather than a content one. Restaging was never at risk — `RestageUnderCurrentRules` enforces the map hash
alone and skips the content-hash gate by design, so a moving content hash never stops a golden *running*.

## What was rejected

**A sibling `u64 ladder_hash` field on each record.** The most explicit of the three, and the command stream
is precedent for it — it already carries the units, ruleset and schedule hashes as three separately gated
fields, each refusing by its own name. It is a format version bump on every kind that takes it, which is the
one cost this decision exists to avoid.

**A new carrier type holding the units and the ladder together.** It layers more cleanly and it touches all
four writers, and *bundle* already means a replay bundle in this vocabulary.

**Pinning an `upgrades` file beside every golden and relaxing the version-0 assertion.** It pays the forbidden
price directly: the assertion that catches a pin gone out of sync is the assertion being relaxed.

**Hashing the ladder outside the bundle that retires records.** It protects the goldens by giving up the thing
the file was deliberately put inside for, and it moves the problem to the day the build phase starts reading
succession rather than solving it.
