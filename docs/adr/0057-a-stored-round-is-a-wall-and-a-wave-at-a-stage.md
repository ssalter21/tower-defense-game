# 0057 — A stored round is a wall and a wave at a stage, and a folder of them is the pool

A run resolves every round against K opponents. Until now those opponents were one canned player composed
from `content/field.txt` and `content/defense.txt`, drawn ten times. They are now **stored rounds read out of
a directory**: a run at stage *s* on map *m* draws K of the rounds recorded at `(m, s)`, and every run
somebody plays can add its own rounds back to the folder. This is
[the loop of the vision's §2](../vision.md) at zero latency with no service in it, and it is step 6 of
[the build order](../build-order.md).

## What a stored round is

**A wall and a wave at a stage** — a new record kind, `RUND`, format version 0:

```
header  (18 bytes: magic, format version, simulation version, content hash)
u16     stage
        the whole of a defense record (GHST)
        the whole of a wave record (WAVE)
```

**Both halves are inlined whole rather than restated.** A defense and a wave each already have a reader, a
canonical order and a format version counted on their own (ADR-0010), so this kind carries their bytes and
neither the tower loop nor the order loop exists twice. It is the arrangement `ReplayBundle` already uses for
the same two halves, and the cross-check that all three headers name one simulation version and one roster
comes with it: a wall from one ruleset stapled to a wave from another is refused at the read, not at the draw
that would meet it.

**A wave is not optional, and that is what makes this a new kind rather than a bump to the defense.** The
option considered was extending `GhostRecord` with the round's orders. It was rejected because a defense
record is a defense: it is what a replay bundle inlines, what `content/golden/defense-0.replay` is evidence
for, and what the map-handle bump at version 1 was a rehearsal for. Half of every pairing in a run is *their*
wave against *this* defense, so a pool member with no wave is an opponent standing still — and a defense
version that carried an empty wave for every record ever written would be that field defaulted, which
`RecordFormat.GhostVersion` spends four paragraphs saying a reader may not do for anything a result depends
on.

**The stage is the field the kind exists for.** A pool is drawn per stage, because a member grows over a run
exactly as a run does (ADR-0042's #208 amendment), so a round that did not say which one it was played at
could be drawn against any of them. Stages count from one, as waves do; zero is refused rather than read as
the first.

**The map is named by the defense inside and not again here, and there is no seed.** The defense already
carries the hash that pins the geometry and the handle that looks a map up, so a second copy would be a second
thing to keep in agreement. A seed belongs to the run that played the round: putting one in would make rolling
different dice a different stored round, which is ADR-0030's argument for why a seed cannot live in a defense
either.

**Ids are the hash of the bytes and are not stored** (ADR-0030), and **the folder is named by them**: one file
per stored round, `<id>.round`. A file whose name is not the id of its own bytes is refused, because two files
holding one record is a field that meets somebody twice.

**There is no ruleset stamp, and that is the difference between an opponent and a replay.**
[ADR-0047](0047-a-bundle-stamps-its-ruleset.md) makes a bundle carry one because a bundle claims *this match
came to this result*, and every landing in it resolves through the matrix, the armour expression and the
floor — so a retuned number under a record's name is a different match wearing it. A stored round claims
nothing about a result. It is a wall and a wave, fought fresh under the ruleset of whatever run drew it,
exactly as the canned stand-in is: neither is priced, neither is replayed, and neither reports what it once
did. What a stored round does have to agree about is the roster its ids and stats are read out of and the
board its cells are on, which is what the content hash and the map hash are for.

## What the draw is

**K slots. The stage's stored rounds first, without replacement while they last; the canned field for the
rest.** The draw is derived, per ADR-0034 — one stream started at `fold("run-field/1") + seed + round` — so a
replayed run meets the same ten and a run on another seed does not.

**Without replacement is the change, and topping up is what keeps it total.** A field of ten out of a stage of
twenty is ten different opponents rather than ten draws that may repeat, which is what makes a wide pool a wide
field. A stage with three stored rounds is three opponents and seven of the canned field: a field is K wide
whatever the folder holds, and the outcome says how many of the ten were canned.

**A stage nobody has stored a round at draws exactly as it always did.** The top-up is one draw per slot with
replacement off the same stream, which is the whole of what the draw used to be — so a pool with nothing stored
consumes the same stream and meets the same members. That is not an argument, it is
`StoredPoolTests.A_stage_nobody_stored_a_round_at_is_the_field_it_always_was`, and it is why
`content/run-outcome.txt`, `content/sweep.csv` and every committed command stream are unmoved by this change.

**Stored rounds are indexed exactly and the stand-in clamps.** Past the deepest round it was recorded at, the
canned opponent's deepest round stands (`FieldPool.OfRounds`'s rule, unchanged). Stored rounds do not clamp: a
stage nobody has played is a stage of nobody, and standing round ten's opponents at round twenty would be
inventing a population.

## Where the halves are

**The simulation never touches the filesystem** (ADR-0018), so the folder is split at the byte:

- `Sim.StoredRounds` is handed one record's name and bytes at a time. It runs the gates — the name is the id,
  the bytes read, the three stamps are this board and this build and this roster (declared to `ReplayGate`,
  as every other record kind declares its own), both halves resolve — and files what survives under its stage
  **in id order**, whatever order the records arrived in. **It never throws for a bad record**: a refusal is a
  sentence on `Refusals` and the pool goes on filling.
- `Sim.RecordedRun` is the other direction: the rounds of a played run, composed into records and proved to
  read back before anybody writes one (ADR-0050).
- `simcli/RoundFolder.cs` and `client/Assets/View/StreamingContent.cs` enumerate a directory and hand the
  bytes over, and write the bytes the other one hands back. That is all either of them does — the ordering
  rule and the read-back are both above them — so the shell and the client cannot come to two opinions about
  which stored rounds a run may meet or what one is made of.

**A refused record is named and skipped, and that is a decision rather than laxity.** Reading one record stays
all-or-nothing (ADR-0013) — nothing is repaired, nothing is partially read. What is different is that a
*folder* is a runtime artefact that accumulates for as long as anybody plays, so a record from a format that
has since moved is the ordinary case in one. A run that refused to start over a stale file would be a run
nobody could play on the day a format moved.

## What it costs

**A round's field is no longer one object ten times.** With a wide stage, a round resolves twenty matches
against twenty different walls and waves rather than twenty against one, and nothing is shared between them.
The sweep is unaffected today because it plays against an empty pool; it will not be once one is seeded, and
`tools/run-sweep.ps1` takes no `--pool` for that reason.

**A run's numbers move the moment somebody seeds a folder, and that is reported rather than tuned.** The
committed run against an empty pool deals nothing over four rounds; against three of its own stored rounds it
deals 53. No number in `content/` moved to produce that, and none should: the pool's members are bot-played
rounds and inherit every caveat about the bot.

**The folder is a runtime artefact and is ignored by construction** — `client/.gitignore` covers
`Assets/StreamingAssets/content/pool/` and its `.meta`, per rule 4 of AGENTS.md. `tools/seed-pool.ps1` fills
one so a fresh clone has opponents; it commits nothing.

## What was rejected

**A second record beside the defense, paired by convention.** Two files per round, related by a name or by a
sidecar. Rejected: "this wave goes with this defense" would be a claim in a filename rather than a fact about
the bytes, which is exactly what ADR-0030 exists to prevent.

**Storing a whole run rather than its rounds.** A command stream already stores a run, and it is the wrong
artefact for a pool: a run is a decision *sequence* against a seed and a field, so drawing an opponent out of
one would mean replaying somebody's whole run to reach the round wanted. A round is what a field is made of,
and it is what `RoundOrders` already is.

**Drawing with replacement, as the pool has always been drawn.** Cheapest, and it makes a field of ten out of
a stage of twenty meet somebody twice about a third of the time — a run scored against nine opponents with one
of them counted double, for no reason anybody could see from the outcome.

**Refusing a run whose folder holds a stale record.** Loud, and wrong: see above.

## What closes the loop

**A run adds its own rounds back.** `simcli play-run --pool <dir> --store` stores the rounds of the run it
just played, and `RunLoop` does the same at the end of a session the prover agreed with — a run whose record
does not play back to what the player was shown is a run nobody can reproduce, and a pool of those could not
be checked against anything. `simcli store-run` plays a scripted player into a folder instead, which is what
`tools/seed-pool.ps1` calls so that a fresh clone has opponents at all.

**A root reads its pool once and keeps it.** A session is proved by building a second run on the same seed
and holding it against what the player was shown, so a population that grew between the two — by this very
run storing its rounds on the way out — would make the two runs differ for a reason that is not a determinism
bug. The pool a run plays against is the pool as it stood when the run was built.

## Where it lives

`sim/RoundRecord.cs`, `sim/StoredRounds.cs`, `sim/RecordedRun.cs`, `sim/FieldPool.cs` and `Run.FieldFor`;
`simcli/RoundFolder.cs`, the `--pool` and `--store` options on the run verbs and the `store-run` verb;
`client/Assets/View/StreamingContent.cs`, `MatchRoot.RunOn` and `WrittenRun.Stored`. Tested in
`sim.tests/RoundRecordTests.cs`, `sim.tests/StoredPoolTests.cs`, `sim.tests/CommandLineTests.cs` and
`client/Assets/Tests/PlayMode/StoredPoolTests.cs`.
