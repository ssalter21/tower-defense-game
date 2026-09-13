# 0057 — A stored round is a wall and a wave at a stage, and a folder of them is the pool

**Decided.** A run's K opponents are **stored rounds read out of a directory** rather than one canned player
drawn ten times: at stage *s* on map *m*, a run draws K of the rounds recorded at `(m, s)`, and every run
somebody plays can add its own rounds back. This is the vision's §2 loop at zero latency with no service, and
step 6 of the build order.

- **The record**, kind `RUND` format 0: the 18-byte header, `u16 stage`, then the whole of a defense record
  and the whole of a wave record, inlined byte for byte so neither loop exists twice and the three headers'
  stamps are cross-checked at the read. Stages count from one. The map is named by the defense inside; there
  is no seed (a seed belongs to the run that played it) and no ruleset stamp (a stored round claims no result,
  unlike a bundle — [ADR-0047](0047-a-bundle-stamps-its-ruleset.md)). Ids are the hash of the bytes and the
  file is `<id>.round`; a name that is not its bytes' id is refused.
- **The draw**: K slots, the stage's stored rounds first **without replacement**, the canned field topping up
  the rest, off one derived stream (`fold("run-field/1") + seed + round`). A stage with nothing stored draws
  exactly as it always did, so every committed artefact is unmoved. Stored rounds do not clamp past the
  deepest round recorded; the stand-in still does.
- **The simulation never touches the filesystem** ([ADR-0018](0018-the-simulation-never-touches-the-filesystem.md)):
  `Sim.StoredRounds` is handed names and bytes, runs the gates, files survivors by stage in id order and
  **never throws for a bad record** — a refusal is a sentence on `Refusals`. `RecordedRun` composes a played
  run into records proved to read back. The shell and the client only enumerate and write.
- **A run stores its own rounds** when the prover agreed with it; a root reads its pool once and keeps it, so
  the second proving run meets the same population.

**Cost.** A round resolves against K different walls and waves, sharing nothing. A run's numbers move the day
a folder is seeded (0 dealt over four rounds against an empty pool, 53 against three of its own), and that is
reported, not tuned. The folder is a runtime artefact, ignored by construction (`client/.gitignore`);
`tools/seed-pool.ps1` fills one and commits nothing; `run-sweep.ps1` takes no `--pool`.

**Rejected.** Extending the defense record with orders (a defense is a defense; a pool member with no wave is
an opponent standing still, and a defaulted field is what `GhostVersion` forbids). Two files paired by
convention ([ADR-0030](0030-record-ids-are-content-addressed.md)). Storing whole runs (drawing an opponent
would mean replaying somebody's run to reach a round). Drawing with replacement (a field of ten from twenty
meets somebody twice a third of the time). Refusing a run whose folder holds a stale record — a folder
accumulates across format moves, so that is the ordinary case.

Lives in `sim/RoundRecord.cs`, `StoredRounds.cs`, `RecordedRun.cs`, `FieldPool.cs`, `Run.FieldFor`;
`simcli/RoundFolder.cs`; `client/Assets/View/StreamingContent.cs`.
