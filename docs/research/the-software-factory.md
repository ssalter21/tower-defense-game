# Dex Horthy's software factory, assessed against this repository

**Research note** · 7 August 2026

**Question:** what is Dex Horthy's "software factory" approach, which of it applies here, which of it does
not, and what should change as a result?

---

## Verdict

**This repository is already a light factory, and it got there from the other direction.** Horthy arrives at
his conclusions by managing agents and watching a codebase rot; this project arrived at nearly the same
practices by chasing determinism. The two roads meet at the same rule, which Horthy calls **back pressure**:
*you may hand a loop only as much autonomy as you can cheaply and reliably verify.* A rolling state hash, a
committed golden trace, a poison suite and a six-row determinism matrix are not agent-management tools by
intent — but they are the best back-pressure instrument in this repository, and they exist.

So the useful output of this note is not "adopt the factory". It is **three things the factory framework
names that this repository has not named**, and one structural mismatch worth stating plainly.

The three gaps, in order of how much they cost:

1. **The autonomy gradient is felt, not written.** The simulation is verified six ways from a shell; the
   client is verified by a person dragging a slider once. Back pressure says those two halves must not
   receive the same amount of agent autonomy. Nothing in [`AGENTS.md`](../../AGENTS.md) says so.
2. **`-Regenerate` is a self-certifying escape hatch.** Any change to the rules can be made to pass the gate
   by rewriting the artefacts the gate checks against. This is correct by design and there is no guard on it.
3. **The slop surface here is prose, not code — and nothing checks prose.** Horthy's degradation curve
   applies to specifications, and his framework assumes the spec is the stable thing. Here the spec is the
   thing that moves: seven vision reversals in five days. The 7 August consolidation
   ([`docs/decision-log.md`](../decision-log.md), PR [#78](https://github.com/ssalter21/tower-defense-game/pull/78))
   is a deliberate, well-aimed response to exactly this — but it was a human noticing, four days late, and
   nothing prevents the next one.

The mismatch: **the factory's core promise is throughput, and this project does not want throughput.** The
README says the game is built "for the pleasure of building it". Horthy's numbers — 35k lines in seven hours,
$12k a month in Opus tokens for three people — are answers to a question nobody here is asking. Take the
verification discipline; leave the volume.

---

## 1. What the approach actually is

Horthy's position is not one document. It is four artefacts written over about eighteen months, and they
only make sense read in order, because the later ones retract the optimism of the earlier ones.

### 1.1 12-Factor Agents (2025) — agents are mostly ordinary software

The [twelve factors](https://github.com/humanlayer/12-factor-agents) are a reaction against framework-shaped
agent loops. The thesis: reliable LLM software is **deterministic code with model calls placed at chosen
points**, not a loop handed a goal. The factors that matter for coding work:

| # | Factor | What it means here |
|---|---|---|
| 3 | Own your context window | Every token in is deliberate; context is engineered, not accumulated |
| 8 | Own your control flow | You write the graph; the model does not decide what happens next |
| 10 | Small, focused agents | Narrow scope per agent, not one agent that does everything |
| 12 | Stateless reducer | The agent is a function from state to state, so it can be paused and resumed |

### 1.2 Advanced Context Engineering (ACE-FCA) — research, plan, implement

[`ace-fca.md`](https://github.com/humanlayer/advanced-context-engineering-for-coding-agents/blob/main/ace-fca.md)
is the practical workflow, and its organising idea is **frequent intentional compaction**: because a model is
a stateless function, context quality is the only lever, so you deliberately distil progress into markdown
artefacts and start fresh rather than letting a window fill with tool output.

Three phases, each producing a reviewable artefact:

- **Research** — the agent establishes which files matter, how information flows, where the root cause or the
  seam is. Output is a document, not a change.
- **Plan** — exact files, exact changes, the verification for each phase, the success criteria.
- **Implement** — executed phase by phase, compacting between phases, each verified before the next starts.

The load-bearing claim is about **where human attention goes**. A wrong line of code costs seconds. A wrong
line of a plan costs hours. A wrong line of research costs days, because everything downstream inherits it.
So review moves upstream: humans read the 200–300 line research note and the plan, and read the resulting
diff much more lightly. Rules of thumb that come with it:

- Target **40–60% context utilisation**; roughly 170k tokens are usable before quality visibly degrades.
- Prioritise **correctness, then completeness, then brevity** — wrong context is worse than verbose context.
- **Delegate search to subagents**, so grep output and file listings burn a context window nobody keeps.
- **Throw out research that contradicts expectations** rather than force-fitting it.
- **Do not skip research.** Problem statement straight to plan produces worse architecture.
- **There are no magic prompts.** The result comes from the process, not the wording.

It also names a metric that is not correctness: **mental alignment**. When humans stop being familiar with
large parts of the codebase, the organisation degrades even while the tests stay green.

### 1.3 The dark factory, and its failure

From July to November 2025 Horthy ran a **fully automated factory**: agents wrote, reviewed and deployed, and
no human read a line. It failed — and the interesting part is *how*. **The tests stayed green the whole
time.** What accumulated was what Addy Osmani's write-up
([Software Factories, Light and Dark](https://addyosmani.com/blog/software-factories/)) calls **comprehension
debt**: the gap between the volume of code and anybody's understanding of it. It surfaced when a single bug
took weeks to find, in a codebase three to six months deep that nobody had read.

The diagnosis is a training-signal argument, and it is why he thinks better harnesses cannot fix it:
**reinforcement learning rewards passing tests, not preserving design.** Whether an architecture held up is
observable in months or years, which is too long a horizon to propagate as a reward. So models are, by
construction, optimised for the thing that is cheap to check and indifferent to the thing that is not.

### 1.4 Harness engineering is not enough — the factory as a loop

The [AI Engineer talk](https://www.youtube.com/watch?v=Ib5GBkD555M) puts the factory on one diagram: intent
and production signals feed a queue; the harness builds; automated checks and review gate it; deploy ships
it; monitoring turns production back into signals. Harness engineering — orchestration, sandboxing, tools —
makes the *build* box faster and cannot make the *gate* box smarter.

What comes out of that framing:

- **Back pressure.** Autonomy is capped by cheap, reliable verification. Verification, not generation, is the
  constraint on the whole system.
- **Graph workflows, not open loops.** "Mostly deterministic code, with LLM steps sprinkled in at just the
  right points." Agents hold coherence for roughly **three to ten steps** and lose the thread **past twenty**.
- **Small, verifiable automation beats large autonomous automation.** His concrete example is a nightly cron
  that fixes *exactly one* anti-pattern, commits, and opens *one small* pull request for a human to read.
- **The codebase is part of the harness.** Strong typing, test seams, short call stacks, clear component
  boundaries, dependency injection, architectural review before implementation. Code that is cheap to verify
  is code an agent may be trusted with.
- **Humans own the outer loop** — deciding whether the approach is right, verifying the diagnosis, accepting
  the consequences — and keep hard control of the high-stakes surfaces.

### 1.5 SlopCodeBench — the evidence

The newest artefact is a
[benchmark](https://github.com/humanlayer/advanced-context-engineering-for-coding-agents/blob/main/benchmarking-opus-5-on-slop-code-bench.md)
built to measure the thing the argument depends on. Requirements are revealed **incrementally across 17
checkpoints** on three problems, so the test is long-horizon maintenance rather than one-shot correctness. It
scores strict pass (new functionality *and* all inherited regression tests) alongside **41 slop metrics** —
volume, cyclomatic complexity, duplication, single-use functions, rule violations, dependency-graph
propagation cost, cyclic dependencies.

Frontier models score **6–24% strict pass**. Opus 5 leads at 24% (4 of 17) — while writing roughly **five
times more functions** than its competitors, with 51% of its output being test code against their 11–24%.
Complexity, duplication and violations climb across checkpoints for every model tested.

The conclusion Horthy draws, and the one worth carrying into this repository: **models degrade codebase
quality over time, and no amount of process removes the need for a human gate until that number is far
higher than it is.**

---

## 2. What this repository already does

Set the factory diagram beside this repository and most of the boxes are filled — several of them better than
the source material describes.

| Horthy's practice | Where it lives here |
|---|---|
| **Back pressure** — cheap reliable verification bounds autonomy | `tools/run-headless-match.ps1 -Verify`: plays the committed bundle, requires the trace, landmark table and every historical golden byte-for-byte, in one shell command, no engine, no licence |
| **Automated checks as the gate** | `.github/workflows/build-gate.yml` — file sizes, client project settings, streaming-content drift, build, tests, IL scan over both the committed *and* a fresh assembly, an eight-case poison suite, the preprocessor ban, the headless replay, and an assertion that the gate did not rewrite the tree |
| **Verify somewhere other than the developer's machine** | The determinism matrix: three operating systems, two architectures, two differently compiled images — six runs, all required to produce the same trace |
| **Graph workflows, static entry points** | Working agreement 3: everything an agent runs lives in `tools/` and runs from a shell, and **nothing may depend on an editor bridge**. Fifteen scripts, 1,617 lines |
| **Research → Plan → Implement** | `/wayfinder` maps → decision tickets (`type:grilling`, `type:research`, `type:prototype`, `type:task`) → `/to-spec` → `/to-tickets` → `ready-for-agent` → `/implement` → PR |
| **Specs in source control; specs become the code** | [`docs/vision.md`](../vision.md) as the standing document, five archived deep dives, twelve research notes, and issue [#70](https://github.com/ssalter21/tower-defense-game/issues/70) — a map that *is* a research artefact under review |
| **Architectural review before implementation** | 32 ADRs, each stating what was decided and what it costs |
| **Codebase as harness: seams, boundaries, invariants** | ADR-0007 (the snapshot is the only view input), ADR-0018 (the simulation never touches the filesystem), ADR-0025 (every invariant is an unconditional throw), ADR-0012 (one writer, many readers) |
| **Fighting comprehension debt** | PR [#67](https://github.com/ssalter21/tower-defense-game/pull/67): comments say what the code does, reasoning moves to ADRs. That is a direct comprehension-debt intervention, done deliberately |
| **Compaction of the standing documents** | PR [#78](https://github.com/ssalter21/tower-defense-game/pull/78), 7 Aug: deep dives to `archive/` with survival banners, reversal tables out of the vision into [`docs/decision-log.md`](../decision-log.md), `docs/README.md` demoted to an index, `CLAUDE.md` reduced to a pointer at `AGENTS.md` with the 147-line measurement moved to a research note. This is intentional compaction applied to the human-readable half of the project |
| **The human gate on what machines cannot check** | [`docs/sit-down.md`](../sit-down.md) — twelve things to look at, each pinned to an exact tick from the committed landmark table. "Vibes pass, so no row here is allowed to be a vibe" |

Two places this repository is **ahead of the framework**, and they should not be quietly re-recommended:

- **Golden screenshots were tried and rejected, with evidence.** `tools/capture-match-frames.ps1` says it
  outright: the frames are documentation, not an oracle, because two frames whose bones were definitively
  swapped rendered pixel-identical, reproducibly. The obvious "hash the frame at the landmark tick" gate is
  already known here to be a gate that cannot fail.
- **A gate that repairs what it finds is treated as no gate at all.** The workflow's tree-clean step exists
  because a step that fixes its own findings "would have gone green through every regression it exists to
  catch". That is the back-pressure principle stated more precisely than Horthy states it.

And one number worth putting beside SlopCodeBench: `sim/` is **7,106 lines** against **5,420 lines** of
tests — a ratio of 0.76, on a simulation whose every rule is checked against a committed trace. This is not a
codebase where an agent can quietly degrade correctness.

---

## 3. Where it does not fit

### 3.1 Throughput is not the goal

The factory exists to convert intent into shipped code faster. Its published evidence is volume: 35,000 lines
in seven hours, an intern at ten pull requests by day eight, $12,000 a month in tokens. This project's README
says it is built for the pleasure of building it, the art budget was deliberately $0, and the build order was
explicitly re-sequenced by *what is cheapest to learn* rather than by what unblocks what. **Adopting the
factory's throughput apparatus here would optimise a quantity the project does not value**, and the first
casualty would be the ADR-and-vision discipline that is currently its best asset.

Take: back pressure, the loop diagram, the small-verifiable-automation rule. Leave: the volume metrics, the
cost envelope, the argument for eliminating pull requests.

### 3.2 Mental alignment means something different with one human

Horthy's alignment metric is about a *team* drifting apart from a codebase. Here there is one person. The
disease is the same but the epidemiology is not: the risk is not that colleagues cannot read the code, it is
that **Sam in November cannot read what agents wrote in August**. The mitigations already in place — ADRs,
the vision's §9 list of what it overturns, research notes with verdict columns — are stronger than most teams
manage. This part of the framework is already discharged and does not need new machinery.

### 3.3 The client half cannot be given the factory's verification

The factory assumes the gate can be made cheap. Half of this repository is a Unity project where it cannot:
engine tests need a licence and a multi-gigabyte image, the editor holds an exclusive project lock, and the
one artefact that would make visual regressions checkable — the rendered frame — has been measured here to be
insensitive to a definitively broken skeleton. **This is not a gap to close. It is a permanent asymmetry**,
and the right response is to make the asymmetry explicit rather than to keep trying to close it (see 4.1).

### 3.4 The framework assumes the specification is the stable thing

This is the sharpest misfit, and it runs the other way from every other item here.

ACE-FCA's inversion — review the plan hard, review the diff lightly — rests on the plan being the cheap thing
to be wrong about. That is true, and this repository has leaned into it further than Horthy does: it has a
seven-step build order with **zero lines of code written against any of it**, and a planning apparatus
(`/wayfinder`, `/grilling`, `/to-spec`) that produces decisions rather than deliverables by default.

But the plan here is not stable. In the five days to 7 August: six vision reversals landed in
PR [#71](https://github.com/ssalter21/tower-defense-game/pull/71), a seventh in
[#74](https://github.com/ssalter21/tower-defense-game/issues/74); map #70 was rescoped mid-flight when two of
its four questions turned out to rest on dead premises; and the economy obligation for step 1 has now read
three different ways. The map itself says the churn is the point — being wrong on paper costs a text file —
and that is a fair defence of *each individual* reversal.

The aggregate is what needs the gate, and **there is no gate on prose.** Nothing fails when two documents
disagree. Two measured examples, both from the same file:

- The Part V §3.11 correction — Element TD 2 ships 59 towers and is not combinatorially complete — was
  **applied on 3 August at 14:05** by PR [#61](https://github.com/ssalter21/tower-defense-game/pull/61).
  `docs/README.md` was still telling readers the correction was "**not yet applied**" on **6 August at
  23:23**, three and a half days later.
- The same file was still asserting "**no mazing, ever**" and the one-hex corridor on 6 August, the day
  after the maze reversal contradicted both.

Neither is a large defect. Both are exactly the shape SlopCodeBench measures in code — a claim that was true
at checkpoint N and quietly false at checkpoint N+1 — and every frontier model in that benchmark exhibits it.
**There is no SlopCodeBench for design documents, and this repository's dominant output is design
documents.**

**The 7 August consolidation is the right response and it already happened.** PR #78 moved the deep dives to
`archive/`, moved reversal churn into `docs/decision-log.md`, and demoted `docs/README.md` to an index — and
its own log entry names the cause precisely: "the exact failure mode of keeping the same fact in two files".
That is a better diagnosis than anything in the factory literature, which does not discuss spec rot at all.

What it does not do is make the next occurrence cheaper to find. Both defects above were caught by a human
re-reading the documents days later, which is the manual-inspection regime this project has already rejected
for the simulation. **The structural point stands: implementation is the only back pressure on
specification, and this repository has not written a line of code against a seven-step build order.**

---

## 4. Recommendations

Ranked by cost of not doing them.

### 4.1 Write down the autonomy gradient — one table in `AGENTS.md`

Back pressure is currently intuition. Make it a rule, per area: what verifies this, and therefore how much
an agent may do unattended.

| Area | What verifies it | Autonomy |
|---|---|---|
| `sim/`, `sim.tests/`, `sim.poison/` | Gate + six-row matrix + IL scan + golden trace | Full — an agent may land changes here on the strength of a green gate |
| `content/*.txt` (rules text) | Trace changes and must be regenerated deliberately | Full to change, **never** to regenerate (see 4.2) |
| `simcli/`, `tools/` | The gate exercises the entry points | Full |
| `docs/` | Nothing | Full to draft, human to merge — the review *is* the gate |
| `client/` non-visual (`MatchViewTests`, records, plumbing) | Playmode/editmode runners, off CI, licence required | Agent may write; a human must run the runner and read the result |
| `client/` visual, camera, scale, framing | `docs/sit-down.md` and a person | Agent proposes; **a human decides** |
| Art assets | Nothing automatable | **Human only** — already the standing rule |

This costs half an hour and it converts the project's single most important operating fact from something
felt into something an agent reads before it starts. `AGENTS.md` is 71 lines and just went on a diet
specifically so instructions could live there and measurements could not; a seven-row table is an
instruction.

### 4.2 Put a tripwire on `-Regenerate`

`tools/run-headless-match.ps1 -Regenerate` rewrites `content/golden-trace.txt`, `content/landmarks.txt` and
the goldens. The script guards the ways it can be *misused* — it refuses to run with `-Verify`, and refuses
to run against anything but the committed simulation. It does not, and cannot, guard the thing that matters:
**a change to the rules plus a regeneration is a green gate.** Every check in the workflow, including all six
matrix rows, compares against files the same commit is entitled to have rewritten.

This is correct design — the alternative is a gate that can never go red on a deliberate content change — and
it is precisely the hole Horthy's dark factory fell through, because there too the tests stayed green.

The cheap fix is not a technical gate, it is an attention gate: **a diff touching `content/golden-trace.txt`,
`content/landmarks.txt` or `content/golden/` is a diff a human reads, always.** Encode it however is
cheapest — a `CODEOWNERS` entry, or a gate step that fails a pull request touching those paths unless it
carries a `regenerated-deliberately` label. The point is to make the escape hatch loud rather than to close
it.

### 4.3 Bind map #70's stop condition to a built step, not a decided one

Horthy's coherence range — three to ten steps, lost past twenty — is about agent loops, but the planning loop
here has the same shape and is currently on its second rescope. Map #70's stated stop condition is "nothing
left to decide before step 1 can be built". Change it to **"step 1 is built and the gate is green"**.

The concrete consequence: **do not open the seam-9 board/maze map until build-order step 1 is code.** The
maze reversal is deliberately excluded from #70 today, which is right, but it is queued — and a second map
opening before the first has produced a line of code is the planning loop losing the thread past twenty
steps. Steps 1 to 4 need no engine, no licence and no editor; there is nothing standing between the decisions
already made and a shell.

This is also the only real answer to 3.4. Implementation is the back pressure on specification. Nothing else
is.

### 4.4 Adopt the nightly one-anti-pattern cron — pointed at the documents

This is Horthy's most transplantable single practice, and here it should be aimed at the repository's actual
slop surface. A scheduled agent that checks **one** documentary invariant, and opens **one small** pull
request:

- **every issue or PR number cited in `docs/` still says what the document claims it says** — an issue the
  document calls open that is closed, a correction called "not yet applied" that merged;
- every `⚠️` flag in `docs/` has an open issue, and every closed issue's flag has been cleared;
- every ADR is referenced by the code it governs, and every ADR number cited in code exists;
- every tick number quoted in `docs/sit-down.md` matches `content/landmarks.txt`;
- every claim in an archived deep dive that the vision has overturned is marked as such in §9 and in the
  document's own banner.

**The first check on that list would have caught both defects in §3.4**, on the night of 3 August and the
night of 6 August respectively, instead of on 7 August by hand. It is also trivially cheap: the numbers are
already hyperlinked, and `gh issue view --json state` is the whole oracle.

Keep it to one anti-pattern per run and one small pull request, exactly as Horthy describes it. The value is
that a human can verify the output in under a minute — which is the whole rule.

### 4.5 Recognise step 4 as the largest back-pressure investment in the plan

Build-order step 4 — the sweep harness, every unit against every defense, win rate and cost-efficiency to a
CSV — is filed as "small, it is text rows". Under the back-pressure rule it is much more than that: it is
**the instrument that makes balance cheaply verifiable**, and balance is the most subjective quantity in the
game.

Until it exists, steps 1 and 3 add design levers with no gate on them at all: an agent that changes a cost or
adds a roster row produces a green build and an unknown game. After it exists, `content/units.txt` becomes an
area an agent may work autonomously, because there is a number to check.

That is an argument for building the harness alongside step 1 rather than after step 3 — or, if the ordering
holds, for saying explicitly in the vision that steps 1 and 3 are **human-judgement steps with no automated
gate**, which is currently true and unstated.

### 4.6 Keep code review mandatory on `sim/`, and say why

The temptation this repository creates is the exact one that sank the dark factory: the gate is so good that
reading the diff feels redundant. It is not, for a reason specific to this design — **the gate checks
consistency between the rules and the artefacts, not that the rules are the intended rules.** A change that
moves the trace and regenerates it is invisible to every automated check in the repository, on all six
platforms. `/code-review` exists; make it non-optional for `sim/` and for `content/`, on the record, next to
4.2.

---

## 5. What to reject

- **Dark mode in any form.** SlopCodeBench puts frontier models at 6–24% strict pass on long-horizon
  maintenance and shows complexity climbing across checkpoints for all of them. There is no version of
  "agents merge without a human reading" that survives that number.
- **Eliminating the pull request.** HumanLayer's product direction is to replace review with continuous
  steering. That is a bet on a live collaborative IDE. This repository's working agreements are built on the
  opposite property — that nothing depends on a session — and its pull requests carry the reasoning that ADRs
  and the vision are later distilled from. The PR is load-bearing here.
- **Throughput and cost metrics.** Lines per hour, PRs per day and monthly token spend measure something this
  project does not want more of.
- **Editor-bridge orchestration.** Unity's first-party MCP bridge needs a subscription
  ([`unity-agent-workflow.md`](unity-agent-workflow.md) §6), and working agreement 3 rules out session
  dependencies regardless. Batchmode is the correct path and already is the path.
- **Golden-frame comparison as a visual gate.** Already measured here to be insensitive to a broken
  skeleton. Semantic assertions in `MatchViewTests` plus the sit-down are the right instruments; the frames
  stay documentation.

---

## Sources

- [12-Factor Agents](https://github.com/humanlayer/12-factor-agents) — HumanLayer
- [Advanced Context Engineering for Coding Agents (`ace-fca.md`)](https://github.com/humanlayer/advanced-context-engineering-for-coding-agents/blob/main/ace-fca.md) — HumanLayer
- [Benchmarking Opus 5 on SlopCodeBench](https://github.com/humanlayer/advanced-context-engineering-for-coding-agents/blob/main/benchmarking-opus-5-on-slop-code-bench.md) — HumanLayer
- [Harness Engineering is not Enough: Why Software Factories Fail](https://www.youtube.com/watch?v=Ib5GBkD555M) — Dex Horthy, AI Engineer
- [Software Factories, Light and Dark](https://addyosmani.com/blog/software-factories/) — Addy Osmani
- [Context engineering with Dex Horthy](https://newsletter.pragmaticengineer.com/p/context-engineering-with-dex-horthy) — Gergely Orosz, The Pragmatic Engineer
- [The Limits of Lights-Out Coding with Dexter Horthy](https://www.heavybit.com/library/podcasts/high-leverage/ep-12-the-limits-of-lights-out-coding-with-dexter-horthy) — Heavybit, High Leverage ep. 12
