# Technology Stack Assessment

**Part III of III** · 30 July 2026

**Subject:** Async ghost round-robin tower defense
**Aesthetic north star:** Legion TD 2
**Input:** [Async Ghost Round-Robin: A Design & Feasibility Deep Dive](async-ghost-round-robin.md) (Part II)

> ### 📦 Archived — superseded as a plan, still current as a stack
>
> **Its verdict is what the repository is built out of** — Unity 6 client, plain C# integer sim library, no
> realtime networking — and none of that has moved. What is archived is the framing: it sequenced a commercial
> game, its "no realtime networking" now holds for a different reason (live PvP *is* in scope and still needs
> none), and its fixed-camera rule was overturned by the isometric orbit. Its balance-as-computation claim is
> the method [The Vision §5](../vision.md#5-how-it-is-balanced) adopts.

---

## Recommendation

**Unity 6 for the client, a plain C# integer library for the simulation, and no realtime networking at all.**

The decisive move is not picking an engine. It is refusing to put the simulation inside one. The sim becomes
an ordinary .NET class library with no engine reference, no floating-point types, and no rendering — consumed
unchanged by the Unity client, the ASP.NET server that re-validates results, and a headless CLI that runs
thousands of matches a minute for balance sweeps.

Once you make that split, the language debate collapses. **Every hazard in Fiedler's catalogue is a
floating-point hazard** — under-specified SSE, `sin`/`cos` disagreeing between AMD and Intel, debug diverging
from release, a compiler upgrade breaking stored replays. Integer arithmetic is exactly specified by both the
C# and Rust standards: fixed width, two's complement, defined wrapping. Go pure-integer and cross-platform
determinism stops being a property of your language and becomes a property of your discipline — enforced with
an analyzer and a CI matrix, not a rewrite.

So the real question is which language makes that discipline enforceable while getting you fastest to the
step-3 gate: *is composing a wave against a fixed layout actually fun?* For a team of one to four chasing the
Legion TD 2 look, that answer is C#. Rust is the better language and the wrong recommendation — see
[the fork](#the-one-input-i-do-not-have).

---

## 1. What the design has already decided

| From the design | Technical requirement | Rules in / out |
|---|---|---|
| Deterministic sim, day one | Fixed-point integer gameplay math, isolated from rendering | Out: engine physics, engine transforms, `deltaTime`, any float in gameplay |
| Server re-runs the records | Identical sim code must run headless | Out: sim-inside-engine. Favours one language across client and server |
| Ghosts replay for years across patches | Versioned sim + versioned wire format | Out: reflection-based serializers |
| Nobody is online at once | Request/response over HTTPS | Out: sockets, rollback, lockstep |
| Records under a kilobyte (100k ghosts < 100 MB) | One Postgres table | Out: object storage, sharding, Redis |
| Steps 1–4 need no server | Client must be complete offline | Out: server-authoritative-by-default architectures |
| Replays watchable and shareable | Renderer must scrub, fast-forward, instant-resolve | Out: any view layer that accumulates its own state |
| Legion TD 2 aesthetic | Stylized 3D, ~40–60 skinned units, ability VFX, dense UI | Out: 2D-first engines, thin-3D frameworks |

**The pleasant surprise:** read that list for what is *missing*. No netcode, no matchmaking service, no realtime
infrastructure, no horizontal scale. Asynchronous play does not merely raise the population ceiling — it
removes the hardest and most expensive third of a competitive multiplayer stack. What you spend instead is
determinism discipline, which is cheap at the start and never needs an ops team.

---

## 2. The one decision: three artefacts, one simulation

The simulation is a **library**. Not a subsystem of the game project, not a folder inside Unity — a separately
compiled, separately versioned artefact with no dependency on anything that draws pixels.

```
   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
   │   Client    │  │   Server    │  │   Harness   │
   │  Unity 6    │  │ ASP.NET Core│  │   Console   │
   │             │  │             │  │             │
   │ renders and │  │ re-runs and │  │ determinism │
   │ interpolates│  │ compares    │  │ + balance   │
   └──────┬──────┘  └──────┬──────┘  └──────┬──────┘
          └────────────────┼────────────────┘
                           ▼
        ┌──────────────────────────────────────┐
        │        SIMULATION CORE               │
        │  netstandard2.1 · no engine refs     │
        │  Q32.32 fixed point · seeded PCG     │
        │  struct-of-arrays · one tick fn      │
        └──────────────────────────────────────┘
```

Two consequences justify the discipline:

**The engine choice becomes reversible.** Everything expensive and irreplaceable — the balance, the unit
interactions, the thing that takes years to get right — lives in a library that has never heard of Unity. If
Unity's terms turn hostile again, you port a renderer, not a game. That is a stronger answer to engine risk
than picking a different engine.

**Balance becomes a computation.** A headless match at ~10 ms means 100,000 AI matchups in minutes. After every
balance change you sweep the matrix and read off dominant and dead units before a player sees the patch. A sim
welded into an engine cannot do this, and no amount of playtesting substitutes for it. This is the
second-biggest payoff after anti-cheat, and almost nobody plans for it in advance.

---

## 3. Simulation language: C# vs Rust, judged on enforceability

Since integer semantics are specified identically in both, neither is "more deterministic." They differ in how
hard it is to *accidentally stop being* deterministic. Each has exactly one structural advantage.

### Rust's structural edge — under `no_std`, the footguns are not in scope

`sin`, `cos` and the transcendentals Fiedler warns about live in `std`/`libm`. A `#![no_std]` sim crate cannot
call them because they do not exist in its namespace. The hazard is removed by the compiler rather than policed
by a reviewer. Pair with the `fixed` crate and `rand_pcg` (no floating point, two `u128` words, identical
everywhere).

**But it introduces the exact bug Part II cites.** Rust panics on integer overflow in debug and wraps in
release — that is MotoGP's "replays recorded in debug won't play back in release," reappearing in a language
that was supposed to have solved it. Fix in one line: `overflow-checks = true` on the sim crate's release
profile, plus explicit `wrapping_*` / `saturating_*` wherever wrapping is intended.

### C#'s structural edge — the assembly boundary is the enforcement

A .NET class library that does not reference `UnityEngine` *cannot* call into the engine — not by convention,
but because the types are unresolvable at compile time. The single most likely way to destroy determinism in an
engine-based game is gameplay code reaching for `Time.deltaTime`, `Vector3` or `Physics.OverlapSphere`, and the
project layout makes that a build error. C# also has no debug/release overflow asymmetry: integer overflow is
`unchecked` in both configurations by default.

**What it does not give free:** `float` and `Math.*` are always in scope, and this document's original answer to
that — `BannedApiAnalyzers` with the list below, escalated to build errors — **does not work.** Measured
(1 Aug 2026, .NET SDK 10.0.101, analyzer 4.14.0, `RS0030` at `error`): the analyzer registers on the source
`IOperation` tree and never visits types in declarations, parameters, returns, or built-in arithmetic, so
`float x = a * b;` compiles clean; and `lock (gate)` is an `ILockOperation`, not an invocation, so banning
`System.Threading.Monitor` does not see it. Four of the eight rows below are enforced; three are not, and the
analyzer reports nothing about the difference.

The enforcement is therefore **structural, at the artefact level**: a Mono.Cecil scan of the compiled `Sim.dll`
in `sim.tests`, checking every method's signature, local slots, instruction stream and referenced-member
signatures, run against both the committed DLL and a fresh build. A companion `sim.poison` project carries one
deliberate violation per row, so the scan is proven able to fail. `BannedApiAnalyzers` is **not used in this
project.** See [#14](https://github.com/ssalter21/tower-defense-game/issues/14) for the full reasoning and the
measurements.

### Banned inside the simulation assembly

*Enforced by IL scan of the compiled assembly, not by review comments and not by source analyzers.*

| Symbol | Why | Caught by |
|---|---|---|
| `float`, `double`, `decimal` | The entire hazard catalogue. No "just for a UI number" exception — one leak loses determinism silently and you find out months later. | `ldc.r4`/`ldc.r8`/`conv.r4`/`conv.r8`/`conv.r.un`/`ckfinite`, plus any `Single`/`Double`/`Decimal` in a signature or local slot |
| `Math.*`, `MathF.*` | Returns doubles; transcendentals differ across silicon vendors. Write integer `Sqrt` and a lookup-table `SinCos` against your fixed-point type. | referenced-member signature |
| `Dictionary`, `HashSet` | **Iteration order is explicitly unspecified.** Two runs can visit entities in different orders and diverge. Use arrays indexed by entity id, or `SortedDictionary`. | type reference |
| `Array.Sort`, `List.Sort` | Introsort — **not stable.** Equal elements land in arbitrary order. Use a total-order comparator that can never return zero. | member name — all overloads, no per-overload list |
| `DateTime.Now`, `Guid.NewGuid`, `System.Random` | Ambient nondeterminism. All randomness comes from the seeded PCG in the record; all timing from the tick counter. | type reference |
| `Task`, `Parallel`, `lock` | Thread scheduling is not reproducible. The tick loop is single-threaded. Parallelise across *matches* on the server, never within one. | type reference; `lock` via `Monitor.Enter`/`Exit` in the instruction stream |
| `System.Diagnostics.Debug`, `Trace` | `Debug.Assert` is `[Conditional("DEBUG")]` and compiles out of Release, so a load-time invariant written that way is absent from the shipping build. Per [#15](https://github.com/ssalter21/tower-defense-game/issues/15) the sim's invariants are unconditional throws. | referenced-member signature (visible because Debug is the committed configuration) |
| `UnityEngine.*` | Free — the assembly has no such reference, so this enforces itself. | free — the assembly has no such reference |

`#if DEBUG` / `#if !DEBUG` / `#if RELEASE` are banned in `sim/` by a **source-level** test, because a
preprocessor directive leaves no residue in either artefact and cannot be seen at IL level.

### The one input I do not have

This assumes you are **not** already a fluent Rust developer. If you are, flip it — take the Rust core, keep
Unity or Godot as the client over FFI, and accept the boundary.

Rust also wins decisively on one specific feature: a browser replay viewer is a 100–300 KB WASM payload versus
several megabytes for the .NET runtime. If "share a replay as a link anyone can click" is a top-three feature
rather than a nice-to-have, that alone justifies the switch.

---

## 4. Client engine

Legion TD 2 was itself built in Unity, by a four-person team, from a Warcraft III mod lineage. Its "fighters
that auto-attack incoming waves" mechanic is structurally the same object this design needs to draw. Naming it
as the north star is therefore also a technical data point — the look is known to be reachable in Unity at that
team size.

| Engine | Stylized 3D | Art pipeline, tiny team | Dense UI | Sim integration | Verdict |
|---|---|---|---|---|---|
| **Unity 6** (C#) | Proven — LTD2 itself | Strongest: Synty, KayKit, Mixamo | UI Toolkit | Same language, direct assembly ref | **Recommended** |
| **Godot 4.6** (C#/GDScript) | Capable; Jolt default in 4.6 | Thinner; shipped hits skew 2D | Workable | Cleanest Rust FFI via GDExtension | Strong runner-up |
| **Bevy** (Rust) | Possible | Immature animation, no editor | Hand-built | Native | Not for the shipping client |
| **Unreal 5** (C++) | Overkill | Wrong economics; raises min spec and art bar | UMG | FFI or port | Skip |

### Why Unity, stated honestly

Not because it is the best engine — because of the asset ecosystem, which is the actual constraint on a small
team reaching this look. Fifty distinct readable fantasy units is an art problem, not a code problem, and
coherent stylized packs (Synty POLYGON, KayKit, Quaternius, rigged through Mixamo) are how one or two people
ship that. **Coherence beats fidelity:** one pack's worth of units reads better in a crowded lane than fifty
mismatched better models.

**Licensing:** the Runtime Fee was cancelled outright in September 2024 and the model returned to per-seat
subscriptions. Personal is free below $200,000 in revenue and funding (doubled from $100,000) and the splash
screen is optional in Unity 6. Pro and Enterprise rose 5% in January 2026. The trust damage is real and is a
fair reason to prefer Godot — but note that the split already neutralises the risk: the part of the codebase
you could never afford to rewrite is a plain .NET library Godot can consume just as happily.

### The rendering rule everything depends on

The view layer must be a **pure function of simulation state plus an interpolation alpha.** No view-side
accumulators, no effects that advance themselves, no animation storing authoritative progress. Break this and
replay scrubbing, double speed, and instant-resolve all break with it — and you need instant-resolve, because
"run my five matches now" and the server's parity check are the same code path with the renderer detached.

A fixed three-quarter camera with no free rotation is worth committing to for the reason LTD2 does: it is a
budget decision as much as a style one. You never render what is behind anything, LODs become trivial, and
silhouettes can be tuned against a single known viewing angle.

---

## 5. Services — the smallest server that does the job

Part II's own arithmetic (100k defenses under 100 MB) is a licence to build almost nothing. The entire ghost
pool fits in the page cache of the cheapest managed database instance.

| Layer | Choice | Why |
|---|---|---|
| Runtime | ASP.NET Core minimal API | References the identical sim assembly the client ships. Re-validation is a function call, not a service. |
| Database | PostgreSQL — one `ghosts` table, records as `bytea` | Index on `(sim_version, stage, rating)`. **Stage first, deliberately** — matching on progression before rating is Part II's fix for the Super Auto Pets complaint. |
| Job queue | A Postgres table with `SELECT … FOR UPDATE SKIP LOCKED` | Five re-sims per submission at ~10 ms each. One worker absorbs enormous volume. Redis and Kafka answer a problem you do not have. |
| Auth | Steam session tickets | Do not build accounts, passwords, or email verification. |
| Transport | HTTPS, request/response | WebSockets appear exactly once, at build-order step 7, for private friend lobbies. |
| Rating | Glicko-2 | ~200 lines. The deviation term that widens with inactivity is the point — Part II's fix for stale ghosts holding inflated ratings. |
| Hosting | One container + managed Postgres | Fly, Railway, or a Hetzner box. ~$20–50/month at launch. Avoid serverless: you want long-lived re-simulation workers. |
| Metric to watch | **Ghost pool density per (stage, rating) bucket** | Not uptime, not latency. This is what silently breaks matching, and what tells you when authored ghosts must backfill. |

### Serialization is a trap and deserves its own decision

Do **not** use `System.Text.Json`, Json.NET, MessagePack or protobuf reflection for ghost records. A reflection
serializer's output is a function of *your type definitions* — rename a field, reorder an enum, upgrade the
library, and stored records silently change meaning. Part II names precisely this as fatal: "a newer compiler
on identical source can produce a binary that no longer reproduces old recordings."

Hand-write a forward-only little-endian binary reader and writer with explicit field order, ~150 lines, leading
with `u32 sim_version` and `u16 format_version`. **Never delete a reader branch.** Old readers live forever, or
the ghost is retired explicitly and loudly. That is the difference between a pool migration and a correctness
bug nobody notices.

---

## 6. The determinism harness (week one)

Part II's step one is "a test that runs the same layout plus wave ten thousand times, across debug and release
builds and across platforms, asserting bit-identical results." Concretely — and GitHub Actions supplies the
cross-platform matrix for free, which is itself a reason the repo lives there:

- **State hash.** FNV-1a or xxHash over the entity arrays every 32 ticks, ordered iteration only. Cheap enough
  to leave on permanently.
- **Golden fixtures.** Commit `(seed, layout, wave) → hash trace` files. A diff is a determinism regression,
  and it names the exact tick where reality forked.
- **The matrix.** `windows-latest`, `ubuntu-latest`, `macos-latest` (arm64), each in Debug and Release. Six
  runs, all must equal the committed golden. Catches the architecture and configuration divergence Fiedler
  catalogues, on every push.

  > ✅ **Built 6 Aug 2026** as the `determinism` job in `.github/workflows/build-gate.yml`, closing
  > [#63](https://github.com/ssalter21/tower-defense-game/issues/63). Six rows, `fail-fast` off, each playing
  > the committed bundle and comparing the trace, the landmark table and every historical golden result byte
  > for byte. **The Debug and Release axis needed a reading this bullet does not supply:** the simulation is
  > consumed everywhere as the *committed* `Sim.dll`, which is a Debug build and is required to stay one, so
  > `--configuration Release` would have built the test assembly in Release and gone on playing the same Debug
  > simulation. Debug is therefore the committed image and Release is a fresh optimised build of `sim/`,
  > selected by `tools/run-headless-match.ps1 -Simulation`, and the row asserts which image it actually loaded
  > rather than trusting the property meant to point it there.
- **Nightly fuzz.** 10,000 random layout/wave pairs asserting self-consistency across configurations. This is
  where sort-stability and iteration-order bugs surface, because they need unusual inputs to bite.
- **One CLI, shared.** `simcli play --seed X --defense a.ghost --wave b.wave --hash-trace`. The same binary the
  server calls, so a production desync is reproducible on your laptop from two records and a version tag.

### Repository layout — the shape is the architecture

```
sim/                    # netstandard2.1 · no engine ref · no floats · analyzer-enforced
  Fix64.cs              # Q32.32 fixed point
  Rng.cs                # PCG32, seeded from the record
  World.cs              # struct-of-arrays entity state
  Tick.cs               # the entire simulation step
  Records/
    GhostRecord.cs      # sim_version, seed, author, rating, stage, layout, economy
    AttackRecord.cs     # wave: kind, count, lane, timing
    BinaryFormat.cs     # hand-rolled, versioned, forward-only
sim.tests/              # golden hashes · fuzz · cross-config assertions
simcli/                 # headless runner · balance sweeps · desync repro
client/                 # Unity 6 project — references sim/, owns everything visual
server/                 # ASP.NET Core — references sim/, owns pool and ladder
content/                # balance tables as data, pinned to a sim version
.github/workflows/      # determinism.yml — the six-way matrix
```

**Keep tuning out of code.** Unit statistics, costs and wave compositions belong in `content/` as data compiled
against a sim version — not as constants in the tick loop. You will change these thousands of times, and every
change is a pool migration, so it needs to be a versioned data artefact rather than a rebuild. This also means
balance iteration never blocks on a compile, which matters more than it sounds when the sweep harness can
evaluate a change in ninety seconds.

---

## 7. Seven things not to build

A stack assessment that only adds things is half an assessment. Each of these is a default reflex for this
genre, and each is wrong here for a specific reason.

1. **Realtime netcode, rollback, or lockstep.** Not until step 7's friend lobbies, and even then it reuses the
   deterministic sim rather than introducing a new subsystem. The async design bought you this; do not spend it
   back.
2. **Engine physics in the simulation.** TD collision is circle-vs-circle range checks on a grid. ~200 lines of
   integer math, fully under your control, and the only version that can ever be deterministic.
3. **An ECS framework in the simulation.** At 200 entities a struct-of-arrays tick loop is faster to write,
   faster to debug, and free of the iteration-order risk a framework's storage introduces.
4. **A reflection serializer for records.** Quietly fatal because it fails years later, on content you can no
   longer regenerate.
5. **Redis, Kafka, or microservices.** One process and one Postgres will carry you past any population this
   genre has reached. Element TD 2 peaked at 783.
6. **An account system.** Steam tickets. Every hour on password reset is an hour not spent on the step-3
   question.
7. **Art, before step 3 passes.** The design's own gate — *is composing a wave against a fixed, non-reacting
   defense fun?* — is answerable with capsules, debug text and a top-down camera.

---

## 8. The risk this assessment is most confident about

**Art is the long pole, not code.** Legion TD 2 took four people through four years of early access to reach
the look you are aiming at. The engineering plan above is perhaps three months of careful work for one
competent developer; reaching that visual bar is longer and harder, and it is the estimate most likely to be
wrong by a factor of three.

This is an argument for the build order Part II already proposes rather than against the goal — prove the loop
ugly, then buy the look incrementally, and let the fixed camera and a coherent asset pack do more work than a
bespoke pipeline would.

---

## Sources

1. Part II, *Async Ghost Round-Robin: A Design & Feasibility Deep Dive* — determinism as the whole build risk,
   the fixed-point recommendation, ghost record format, storage arithmetic, and the seven-step build order this
   assessment is sequenced against.
2. Glenn Fiedler (Gaffer On Games), *Floating Point Determinism* and *Deterministic Lockstep* — via Part II.
3. [MMOs.com](https://mmos.com/review/legion-td-2), [EverybodyWiki](https://en.everybodywiki.com/Legion_TD_2),
   [PCGamingWiki](https://www.pcgamingwiki.com/wiki/Legion_TD_2) — Legion TD 2 built independently in Unity by
   AutoAttack Games; WC3 mod lineage; Kickstarter 2016, EA Nov 2017, 1.0 Oct 2021.
   [Ninja Kiwi's acquisition of AutoAttack Games](https://blog.gamerae.com/news/gaming-news/industry-news/ninja-kiwi-acquires-legion-td-2-developer-autoattack-games/).
4. [Unity — "Unity is Canceling the Runtime Fee"](https://unity.com/blog/unity-is-canceling-the-runtime-fee)
   (Sept 2024), [CG Channel](https://www.cgchannel.com/2024/09/unity-scraps-controversial-runtime-fee-but-raises-prices/),
   [CG Channel on the Nov 2025 price changes](https://www.cgchannel.com/2025/11/price-of-paid-unity-subscriptions-to-rise-but-free-subs-extended/).
5. [Ziva — "Can Godot Handle 3D Games?"](https://ziva.sh/blogs/godot-3d),
   [GameGen on Godot 4 in 2026](https://www.gamegen.com/godot-4-for-beginners-why-it-s-the-best-first-game-engine-in-2026-and-how-to-get-started),
   [the godot-rust book](https://godot-rust.github.io/book/toolchain/compatibility.html).
6. [Zack Sinisi — "Deterministic Lockstep Networking Demystified"](https://zacksinisi.com/deterministic-lockstep-networking-demystified/),
   [UnityLockstep](https://github.com/proepkes/UnityLockstep),
   [Unity fixed-point library discussion](https://discussions.unity.com/t/fixed-point-number-library-for-c/736773).
7. [lib.rs/games](https://lib.rs/games) and [crates.io fixed-point](https://crates.io/keywords/fixed-point) —
   the `fixed` crate and `rand_pcg`.
8. C# language specification on unchecked integer overflow as the default in all build configurations; .NET
   documentation on `Dictionary` iteration order being unspecified and `Array.Sort` being unstable.
