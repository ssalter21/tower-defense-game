# One RNG stream, seeded by the record, with no ambient nondeterminism

A match has exactly one random stream, taking exactly one input: the seed carried by the record. There is no stream selector.

The run above a match draws too — the field of a round, and later the offering and an anchor's filling. Those draws are outside this rule and inside [ADR-0034](0034-run-level-draws-are-derived-positions.md), which scopes this one to the match rather than widening it: a match's stream position is still a running count of the shots fired in it.

## Considered options

A second knob would be a second thing a replay has to reproduce. The first time two subsystems disagreed about which stream they were on, the symptom would be a desync with no bad line to point at.

## Consequences

Nothing in the simulation reaches ambient nondeterminism — no `System.Random`, no clock, no thread id, no hardware entropy. That is enforced rather than promised: an IL scan over the compiled assembly rejects every one of them, and the poison project proves the scan can see them.

The generator is O'Neill's PCG-XSH-RR 64/32 — a 64-bit LCG whose output is a xorshift-folded, randomly-rotated 32-bit word. It is chosen over a plain LCG because an LCG's low bits are short-period, and over a hash-based generator because it needs no intrinsics and no table, so the same integer arithmetic runs identically under Mono, IL2CPP and CoreCLR.

The stream's position is exposed because it is folded into the rolling state hash (ADR-0004). Accumulated fixed-point remainders, stream position and target-selection tie-breaks are the fields likeliest to desync and the ones a view never sees.
