# 0047 — A replay bundle stamps its ruleset, and a bundle that names none is retired

The replay bundle carries the content hash of the ruleset the match was played under, at format version 1, and
`ReplayBundle.Replay` compares it. A version-0 bundle names no ruleset and is refused at that gate. It still
reads, still lists, still draws, and still restages.

## What was decided

**The ruleset joins the gate because a landing resolves through it.** The bundle's gate compared the simulation
version, the unit table's content hash and the map hash, and took a `Ruleset` it never looked at. Every landing
reads the matrix cell, the armour denominator, the armour percentage and the damage floor off that argument
([0033](0033-one-fused-damage-expression-and-a-named-pipeline.md),
[0038](0038-a-shot-resolves-where-it-lands.md)), so one retuned number made every stored bundle replay to a
different rolling state hash *while passing all three gates*. Measured on the committed bundle: moving the
armour denominator from 100 to 101 takes it from `B58DBED2315303D2` to `5A47152F5A40D790`, with the simulation
version, the content hash and the map hash all still saying it is the recorded match. The command stream has
gated on the ruleset since it was written ([0039](0039-the-command-stream-is-the-only-route-into-a-run.md));
this is the same argument arriving at the older kind.

**A version-0 bundle is retired at the ruleset gate rather than waved through it.** This is the case
`RecordFormat.GhostVersion` already names as the one a reader may not default: *a simulation-affecting field
may not be defaulted, ever, and the test for it is whether a replay's result can depend on the value.* Here it
provably can — that is the whole reason the field was added. There is no value the version-0 branch could
supply that is not an invented input: the ruleset that happens to be loaded is the numbers of today rather than
the numbers of that run, and a zero is a digest some ruleset legitimately folds to. So `RulesetHash` is
`Hash64?`, null means *this record does not say*, and null refuses. One lifted comparison covers both
refusals — a record that names no ruleset compares unequal to every ruleset there is — and the message reports
`no ruleset stamp` against the live hash, so what it says is what is true: nothing has disagreed, the record
simply never claimed.

**Retiring those bundles costs nothing, because nothing replayed them.** The version-0 golden,
`content/golden/defense-0.replay`, is the one file this decision could have destroyed — it can never be
re-recorded, and [0009](0009-three-identity-fields.md) forbids a bump retiring the only evidence for a reader
branch. It was already **restaged** rather than replayed, both by `GoldenRecordTests` and by
`tools/run-headless-match.ps1`, and has been since a simulation version bump first retired it for `Replay`.
Restaging is the operation that sets the rules question aside by name, so the bundle that has no answer to it
runs there exactly as before. The bill this decision could have presented had already been paid, for the same
reason, by an earlier one.

**`RestageUnderCurrentRules` sets the ruleset aside and says so.** It still enforces the map hash alone — that
gate asks whether the bytes are internally consistent, which nothing sets aside — and the `Restaging` it
returns now carries `RecordedRulesetHash` beside the recorded simulation version and content hash. A record
that names no ruleset never satisfies `RulesetsCoincide`, whatever is in front of it. Setting a question aside
is only different from not asking it if the answer travels with the result, which is why the stamp is in the
label and not merely in the gate.

**A version-0 bundle cannot be written back out.** `ToBytes` refuses it. There is one writer and it emits the
current format ([0012](0012-one-writer-many-readers.md)), the current format stamps a ruleset, and the only
values available are ones the record never made. Refusing is the one answer that does not manufacture a bundle
which could then pass a gate.

## What it costs

**Every stored bundle written before this change is now unreplayable, permanently.** In this repository that is
one file and it was already restaged. Anywhere bundles have been kept, the operation available to them is
restaging, and its output is labelled as not being the record's result. That is the intended cost: a replay
that silently is not the recorded match is the failure the record design refuses everywhere else.

**A ruleset retune now retires every stored bundle, including ones a retune could not have moved.** The hash
covers every number in `ruleset.txt`, so editing the offering ratio or the snapshot price retires bundles whose
outcome cannot depend on either. This is the conservative direction on purpose, and it is the same trade
[0046](0046-an-absent-ladder-folds-nothing.md) took: retiring too eagerly costs a regenerated golden, retiring
too late costs a record that quietly means the wrong thing.

**Two of the four record kinds are now at format version 1 and two are at 0.** That is per-kind counting
working ([0010](0010-format-versions-per-record-kind.md)), and `RecordFormatTests` states it as an assertion so
the counters cannot quietly re-converge.

**Five committed files moved with the change.** `content/match.replay` and `content/golden/defense-1.replay`
were re-recorded eight bytes longer and are read at format 1; `content/golden/defense-{0,1}.result` carry the
restaging label's new ruleset clause; `content/golden-trace.txt` and `content/landmarks.txt` name the new
format version in their provenance block. No simulation output moved — the same leak count, the same final
tick, the same rolling state hash and the same four landmarks. `client/Assets/StreamingAssets/content` carries
the re-recorded bundle, and the client already hands `MatchRoot` the parsed `ruleset.txt` it ships, so the
shipped record passes the new gate with no view-side change.

## What was rejected

**Letting version-0 bundles pass the ruleset gate unchecked.** The tempting option, because it costs nothing
today and keeps every old bundle replayable. It is the exact thing `RecordFormat.GhostVersion` writes down as
illegal: an unchecked gate for those records is a default of "whatever ruleset you happen to hold", and the
result is a replay that is confidently wrong and still validates — which is the failure this ticket exists to
close, reintroduced by the branch that was supposed to close it. It would also make a version-0 bundle *more*
permissive than a version-1 one, so the honest move for anybody wanting the old behaviour would be to strip
their stamp.

**A sentinel digest for "unstamped" instead of a nullable.** A sentinel that is also a legal value is a
sentinel nobody can test for, and unlike `GhostRecord.NoMapHandle` — where zero is reserved because handles
start at one — every 64-bit value here is a digest some ruleset folds to.

**Pinning a `ruleset` file beside each golden, the way `units` and `upgrades` are pinned.** It buys nothing:
the goldens are restaged, and restaging never asks which ruleset a record was made with. It would also add a
third file per version whose only job is to go out of sync.

**Putting the stamp in the shared `RecordHeader` so all four kinds carry it.** It is the tidier layout and it
is wrong for the same reason the ladder was folded rather than added as a field
([0046](0046-an-absent-ladder-folds-nothing.md), inverted): a defense and a wave mean nothing against a
ruleset, so they would carry a value nothing compares, and every stored record of all four kinds would bump at
once for a field two of them do not have a use for.
