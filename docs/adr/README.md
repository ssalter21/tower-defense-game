# Architecture decision records

Decisions that are hard to reverse, surprising without context, and the result of a real trade-off. Each one is short: what was decided, and what it costs.

These were extracted from the source comments when the project moved to a comment convention that describes what the code does rather than why it is that way. The reasoning lives here now; the code says what it does.

## Simulation core

| # | Decision |
|---|---|
| [0001](0001-fixed-point-arithmetic.md) | Fixed-point arithmetic, truncating toward zero, overflowing loudly |
| [0002](0002-tick-order-is-part-of-the-rules.md) | The order of work inside a tick is part of the rules |
| [0003](0003-dice-rolled-once-per-shot.md) | The dice are rolled exactly once per shot |
| [0004](0004-rolling-state-hash-always-on.md) | The rolling state hash is always on, and folds per tick |
| [0005](0005-one-match-surface-no-modes.md) | One match surface, every scenario — no modes, flags or branches |
| [0025](0025-invariants-are-unconditional-throws.md) | Every invariant is an unconditional throw |
| [0033](0033-one-fused-damage-expression-and-a-named-pipeline.md) | One fused damage expression, evaluated once, behind a named pipeline |
| [0034](0034-run-level-draws-are-derived-positions.md) | Run-level draws come from derived positions; the match keeps its one stream |
| [0035](0035-a-runs-outcome-is-a-vector-and-health-is-a-clock.md) | A run's outcome is a vector, and health is a clock denominated in sauce |
| [0036](0036-the-anchor-schedule-is-a-shape-and-a-filling.md) | The anchor schedule is a shape and a filling, and the loader holds its constraints |
| [0037](0037-the-offering-is-public-because-it-is-derived.md) | The offering is public because it is derived, and a build phase is validated once |
| [0038](0038-a-shot-resolves-where-it-lands.md) | A shot resolves where it lands, and the ruleset is a match's argument |

## The simulation/view boundary

| # | Decision |
|---|---|
| [0006](0006-snapshot-pulled-not-pushed.md) | The snapshot is pulled, not pushed |
| [0007](0007-snapshot-is-the-only-view-input.md) | The snapshot is the only thing a view may draw game state from |
| [0008](0008-match-events-are-decorative.md) | Match events are purely decorative |
| [0016](0016-target-references-carry-no-position.md) | A target reference carries no position |
| [0020](0020-hex-orientation-is-a-view-concern.md) | Hex orientation is a view concern |
| [0023](0023-view-constants-are-never-simulation-inputs.md) | No constant in the view is a simulation input |

## Records and identity

| # | Decision |
|---|---|
| [0009](0009-three-identity-fields.md) | Three identity fields, owning three non-overlapping things |
| [0010](0010-format-versions-per-record-kind.md) | Format versions are counted per record kind |
| [0011](0011-content-hash-folds-parsed-integers.md) | The content hash folds parsed integers, not file bytes |
| [0012](0012-one-writer-many-readers.md) | One writer, many readers: history lives in the reader |
| [0013](0013-record-reading-is-an-all-or-nothing-gate.md) | Reading a record is all-or-nothing |
| [0014](0014-reading-and-replaying-are-separate-gates.md) | Reading and replaying are separate gates |
| [0015](0015-replay-bundles-are-self-contained.md) | A replay bundle is self-contained, and the seed lives in it |
| [0017](0017-canonical-order-is-asserted-not-restored.md) | Canonical order is asserted at load, never restored |
| [0018](0018-the-simulation-never-touches-the-filesystem.md) | The simulation is handed text and bytes, never paths |
| [0039](0039-the-command-stream-is-the-only-route-into-a-run.md) | The command stream is the only route into a run, and it stamps every table it means anything against |
| [0040](0040-a-run-is-authored-as-text-and-compiled-to-a-record.md) | A run is authored as text and compiled to a record |

## Drawing the match

| # | Decision |
|---|---|
| [0019](0019-the-view-has-no-clock.md) | The view has no clock, and interpolation is a pure function |
| [0021](0021-two-snapshots-matched-by-id.md) | The view holds two snapshots and matches entities by id |
| [0022](0022-draw-order-by-construction.md) | Draw order is by construction, not by sorting |
| [0024](0024-art-is-serialized-references.md) | Art arrives as serialized references, not a runtime lookup |
| [0026](0026-seeking-re-simulates-rather-than-caching.md) | Seeking re-simulates; there is no snapshot cache |
| [0028](0028-generated-placeholder-art-marks-the-seam.md) | Placeholder geometry is generated in code, marking the art seam |
| [0029](0029-exactly-one-match-root.md) | Exactly one match root in the scene |

## Reporting

| # | Decision |
|---|---|
| [0027](0027-a-landmark-table-with-a-hole-refuses-to-render.md) | A landmark table with a hole in it refuses to render |
| [0041](0041-the-sweep-computes-rows-and-the-shell-writes-them.md) | The sweep computes rows and the shell writes them, and every rate arrives with its operands |
