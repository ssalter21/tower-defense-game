# How the Unity project consumes the plain C# sim library

**Research note** · 30 July 2026 · resolves [#5](https://github.com/ssalter21/tower-defense-game/issues/5)

**Question:** how should a Unity 6 project consume a separately compiled .NET simulation library, and what does each
mechanism cost?
**Input:** [Part III — Technology Stack Assessment](../tech-stack-assessment.md) §2, §3, §6.

---

## Recommendation

**Build the sim with `dotnet build` outside Unity and consume the compiled `netstandard2.1` DLL as a managed plug-in,
placed in a local UPM package under `client/Packages/com.ssalter.sim/Runtime/` (or, if that proves fussy in week one,
`client/Assets/Plugins/Sim/`). Commit the DLL and its portable PDB.**

The competing option — putting the sim's `.cs` files inside Unity behind an `.asmdef` with `noEngineReferences: true` —
is more pleasant to iterate on and is the one everybody reaches for first. Reject it, for a reason that has nothing to do
with `UnityEngine` and everything to do with Part III §6:

> **If Unity compiles the sim, the client runs a different binary from the one the determinism matrix hashed.**

Unity does not use MSBuild or your `.csproj`; it compiles with its own bundled Roslyn (C# 9, `Microsoft.CodeAnalysis.CSharp`
4.3) into `Library/ScriptAssemblies` [[6]](#s6)[[15]](#s15), and the generated `.sln`/`.csproj` exist only so IDEs can
provide IntelliSense — Unity's own docs acknowledge "differences in the way Unity and Visual Studio compiles user code"
[[26]](#s26). So a source-in-Unity layout produces **two IL images of the simulation from one source tree**, built by two
different compilers with two different language versions and two different analyzer sets. The headless CLI and the golden
hashes exercise one; the client ships the other. Integer semantics say they should agree. "Should agree" is precisely the
standard Part II refuses to accept for this subsystem.

The precompiled DLL produces exactly one artefact, byte-identical across client, server and CLI. That is not a
convenience — it is the thing the determinism harness is measuring.

Three secondary findings push the same way, and one of them is a correctness bug in Part III as written. They are in
§5 and §6. Read §6 first if you read nothing else.

---

## 1. What the seam has to hold

Part III §3 states the structural claim precisely: a .NET class library that does not reference `UnityEngine` *cannot*
call into the engine, "not by convention, but because the types are unresolvable at compile time." Grade every mechanism
against four questions, in this order:

1. **Is `UnityEngine` unresolvable, or merely unreferenced?** A checkbox that someone can uncheck is not a compile-time
   guarantee, it is a convention with better ergonomics.
2. **Is the sim one artefact or several?** Part III §6's six-way debug/release × OS matrix is meaningless if the client
   ships a build the matrix never saw.
3. **What does changing one line cost?** Measured, not guessed.
4. **Does the banned-API enforcement actually run?**

---

## 2. The mechanisms

Unity 6 offers exactly three ways to get code into a compiled assembly the project can call, plus two ways to package
each of them. Everything below is a combination of those.

### A. Precompiled DLL in `Assets/Plugins/`

**Setup.** Build `sim/Sim.csproj` (`<TargetFramework>netstandard2.1</TargetFramework>`) with `dotnet build`. Copy
`Sim.dll` and `Sim.pdb` into `client/Assets/Plugins/Sim/`. Unity imports the DLL as a managed plug-in; managed plug-ins
are ".NET assemblies you create and compile outside of Unity" and the documented workflow is literally to copy the DLL out
of `bin/Debug/` into `Assets` [[4]](#s4). `Plugins` remains a reserved folder in Unity 6 [[23]](#s23), though for a
managed assembly any folder under `Assets` works; `Plugins` buys you convention and platform-path handling.

Open the Plugin Inspector and turn **Auto Reference** off. Its documented behaviour is: "When you enable Auto Reference,
all predefined assemblies and assembly definitions automatically reference the plug-in file" [[3]](#s3). Off, plus an
explicit reference from the view asmdef (`"overrideReferences": true`, `"precompiledReferences": ["Sim.dll"]`
[[2]](#s2)), makes the dependency edge visible in a JSON file that shows up in a diff.

Automate the copy rather than doing it by hand — an MSBuild `Copy` target in `Sim.csproj`, or just
`dotnet build sim -o client/Assets/Plugins/Sim`. Copy the PDB in the same step; §4 explains why.

**`UnityEngine` unresolvable?** **Yes, absolutely and unconditionally.** The sim is compiled by the .NET SDK against
`netstandard2.1` reference assemblies. `UnityEngine` is not "unreferenced"; it does not exist in the compilation, cannot
be added by an inspector checkbox, and cannot be added by anyone who does not edit `Sim.csproj`. It is the same
guarantee the server and CLI get, because it is the same compilation.

**One artefact?** Yes. This is the only family of options where the bytes the client loads are the bytes CI hashed.

**Iteration loop.** Edit `.cs` → `dotnet build` → Unity reimports the plug-in on focus → domain reload. Measured on this
machine (§3): **~0.8 s** for an incremental one-file rebuild. Unity's asset **Auto Refresh** preference governs when the
reimport happens — `Enabled`, `Enabled Outside Playmode`, or `Disabled` [[8]](#s8) — and the reimport implies a domain
reload, since "Unity also performs domain reload as part of an asset database refresh when it detects changes to
scripts" [[7]](#s7). So: **no manual copy** (if automated), **an external build**, **a domain reload**, **no editor
restart** in the normal path.

**The one real hazard, and it is the thing to test first.** Unity holds the loaded managed assembly open. Rebuilding
over a DLL that the running domain has mapped is the classic "access denied" failure, and Unity's own issue tracker
carries a case for `Plugins` DLLs in use at runtime [[27]](#s27). The working rule is *stop Play mode before rebuilding*.
Whether Unity 6 ever requires a full editor restart for a managed plug-in swap is not stated in the documentation either
way, and it is the single highest-value thing for the walking skeleton to establish in its first hour — it is the
difference between a 3-second loop and a 40-second one.

### B. Source files plus an `.asmdef` inside `Assets/`

**Setup.** Put the sim's `.cs` files under `client/Assets/Sim/` with a `Sim.asmdef` alongside, and tick **No Engine
References**. Unity compiles it as its own assembly.

**`UnityEngine` unresolvable?** **Yes inside Unity, by a genuine compile-time mechanism — but it is one boolean away
from not being.** The documented behaviour of No Engine References is exact: "When enabled, Unity does not add references
to `UnityEditor` or `UnityEngine` assemblies" [[1]](#s1). The `.asmdef` is JSON with `"noEngineReferences": true`
[[2]](#s2), and it **defaults to `false`** — every asmdef Unity's *Create → Assembly Definition* menu produces starts
engine-referenced. So the guarantee survives exactly as long as nobody regenerates the file, and it is reviewable only if
someone notices a one-word JSON diff. Contrast with option A, where the guarantee is a property of the toolchain rather
than of a setting.

Worse for this project specifically: the guarantee is enforced **twice, by two unrelated mechanisms**. Unity enforces it
via `noEngineReferences`; the server and CLI enforce it by `Sim.csproj` simply not referencing anything Unity-shaped.
Two enforcement points that must independently stay correct is worse than one that cannot be wrong.

**One artefact?** **No.** See the Recommendation. This is the disqualifying property.

**Iteration loop.** The best of any option: edit `.cs` → alt-tab → Unity recompiles the one changed assembly → domain
reload. No copy, no external build, no restart. If you also disable domain reload in **Enter Play Mode Settings**
[[7]](#s7), the Edit↔Play cycle gets faster still, at the cost of static state persisting between sessions — which for a
sim whose whole contract is "state comes from the record, not from statics" is actually low-risk, but relies on you
never introducing a mutable static.

**Cost nobody mentions.** Unity writes a `.meta` file for every file and folder it imports [[25]](#s25). Sim source
under `Assets/` means a `.meta` next to every `Fix64.cs`, `Rng.cs`, `Tick.cs` — inside the directory the server and CLI
also compile from, carrying GUIDs that must be committed. Part III §6's repo layout puts `sim/` at the top level
precisely so it is not a Unity thing; this option makes it one.

### C. Local UPM package via `file:` dependency (source form)

**Setup.** Give `sim/` a `package.json` with `name` (reverse-domain: `com.ssalter.sim`) and `version` — the only two
required fields [[22]](#s22). Move sources to `sim/Runtime/` with `Runtime/com.ssalter.sim.asmdef`; package code **must**
be in an asmdef ("You must associate scripts inside a package to an assembly definition file (`.asmdef`)" [[21]](#s21)).
Then in `client/Packages/manifest.json`:

```json
"com.ssalter.sim": "file:../../sim"
```

Relative paths resolve against the `Packages` folder: "a path preceded with two dots (`..`) refers to the root of the
project path, so that `../another_folder` is a sibling of the `Packages` folder" [[17]](#s17). With Part III §6's layout
(`sim/` and `client/` as siblings at the repo root) that is `file:../../sim`.

The package is **not copied** — Package Manager keeps a reference to the folder's location on disk [[18]](#s18) — and it
is **mutable**: "You can permanently change content only from Local and Embedded package sources" [[19]](#s19).

**`UnityEngine` unresolvable?** Same as B: yes, via `noEngineReferences` on the package's asmdef, with the same
one-boolean fragility.

**One artefact?** No — Unity compiles the sources. Same disqualifier as B.

**Iteration loop.** Effectively identical to B: edit in place, Unity recompiles the package assembly, domain reload. The
sources stay outside the Unity project on disk, which is the one genuine advantage over B — `sim/` keeps its shape from
Part III §6 and the `.meta` files at least land in a package rather than in `Assets`. They still land, though; package
validation expects meta files for package contents [[25]](#s25).

### D. Embedded package under `Packages/`

**Setup.** Same package layout, but physically inside `client/Packages/com.ssalter.sim/`. "Any package that appears
under your project's `Packages` folder is embedded in that project" [[20]](#s20), and embedded packages are mutable
[[19]](#s19). No `manifest.json` edit needed.

This is C with the folder moved back inside the Unity project. It buys nothing over C for this repo and costs the
top-level `sim/` directory the architecture diagram depends on. Include for completeness; do not choose it.

### E. Local/embedded UPM package containing the **precompiled DLL** — the recommended shape

**Setup.** A package whose `Runtime/` folder contains `Sim.dll` + `Sim.pdb` rather than sources. `dotnet build` targets
that folder. Consumers reference `Sim.dll` through their asmdef's `precompiledReferences` [[2]](#s2).

This is option A's compilation model with option C's packaging, and it inherits A's guarantees exactly: one artefact,
`UnityEngine` unresolvable by construction, analyzers running in MSBuild. What it adds over dropping the DLL in
`Assets/Plugins/` is a **version number** — `package.json`'s `version` field becomes the sim version the client is
pinned to, which is the thing Part III §2 means by "separately versioned artefact" and which the ghost record's
`u32 sim_version` has to agree with. That is a real benefit and it costs one JSON file.

If UPM adds friction in week one — it is another unfamiliar subsystem on a cold start — fall back to `Assets/Plugins/`
and add the package wrapper later. Nothing about the sim changes; only where the DLL lands.

### F. Things that are not options

- **Symlink or NTFS junction from `Assets/` to `sim/`.** Unity's position: "Using symlinks in Unity projects may cause
  your project to become corrupted if you create multiple references to the same asset, use recursive symlinks or use
  symlinks to share assets between projects used with different versions of Unity" [[24]](#s24). Asset Store validation
  errors on any symlink in a package. Don't.
- **NuGet `<PackageReference>` reaching Unity's compile.** It cannot. Unity has no NuGet integration and does not read
  the `.csproj`; `NuGetForUnity` is a third-party tool that works by downloading the nupkg and copying its DLLs into
  `Assets`, i.e. by turning it back into option A. This matters more than it sounds — see §5.
- **Referencing the sim's `.csproj` from Unity's generated `.csproj`.** Unity regenerates those files. Any edit is lost.

---

## 3. The .NET target — confirmed, and confirmed buildable here

**Unity 6 accepts `netstandard2.1`, and it is the default.** Unity 6's API Compatibility Level offers exactly two
values: ".NET Standard 2.1", described as "the default API compatibility Level", and ".NET Framework" (4.8 plus
additional APIs from .NET Standard 2.1) [[5]](#s5). Unity's own documentation recommends ".NET Standard over .NET
Framework for all new projects." Microsoft's compatibility table names Unity 2021.2 as the Unity version implementing
.NET Standard 2.1 [[28]](#s28), so Unity 6 is comfortably past it.

This holds for **both** scripting backends. Mono (JIT) and IL2CPP (AOT) differ on dynamic code generation, not on API
surface [[5]](#s5); a pure-integer sim with no reflection-based serialization (Part III §5 already bans that) has
nothing IL2CPP will strip or reject.

**`netstandard2.1` is not deprecated.** "No new versions of .NET Standard will be released, but .NET 5 and all later
versions will continue to support .NET Standard 2.1 and earlier." Microsoft's own guidance for a *new* library is to
skip it and target `net10.0` — but that guidance is written for NuGet authors chasing reach, and it explicitly names the
case that applies here: "Use `netstandard2.1` to share code between Mono and .NET Core 3.x" [[28]](#s28). Unity's Mono is
that Mono. Keep Part III's choice.

**Can a machine with only the .NET 10 SDK produce it? Yes. Verified, not inferred.**

On this machine — SDK 10.0.101, MSBuild 18.0.6, no other SDK installed, no workloads — a `netstandard2.1` class library
with a `BannedApiAnalyzers` `PackageReference` restored and built clean. The reason is that the .NET SDK ships the
targeting pack: `NETStandard.Library.Ref` is present under `C:\Program Files\dotnet\packs` alongside
`Microsoft.NETCore.App.Ref`. **Nothing else needs installing** — not Visual Studio, not an older SDK, not Mono, not
Unity itself. (Microsoft's "install Visual Studio 2019 or later to build .NET Standard libraries" note [[28]](#s28)
predates SDK-delivered targeting packs; the CLI alone is sufficient.)

Two knock-on facts worth pinning down now:

- **Default `LangVersion` for `netstandard2.1` is C# 8.0**, because the SDK derives the language version from the TFM
  [[29]](#s29). Unity 6 compiles at **C# 9.0** [[6]](#s6). If you go the source-in-Unity route (B/C/D), the sim compiles
  at C# 8 in MSBuild and C# 9 in Unity — a third way for the two builds to diverge, and one that will bite the day
  someone uses a record type. With the precompiled DLL the question disappears: only MSBuild ever compiles the sim, and
  you pin `<LangVersion>` explicitly rather than inheriting it.
- **`<DebugType>portable</DebugType>`** is the SDK default and is what Unity's managed debugging wants. Say it out loud
  in `Sim.csproj` anyway.

---

## 4. Debugging across the boundary

Managed code debugging in Unity 6 works with both Mono and IL2CPP on every platform except Web; the debugger attaches
over TCP from Visual Studio, VS Code or Rider; for the Editor you toggle Debug Mode in the status bar, for a Player you
need Development Build + Script Debugging [[10]](#s10).

The whole boundary question reduces to one documented sentence:

> "Unity cannot generate debugging information for managed plug-ins in your project. You can only debug code from
> managed plug-ins if the associated `.pdb` files are next to the managed plug-ins in the Unity project on disk."
> [[10]](#s10)[[11]](#s11)

So:

| Mechanism | Step from Unity code into sim code? | Setup |
|---|---|---|
| A / E (precompiled DLL) | **Yes** | Copy `Sim.pdb` next to `Sim.dll`. That is the entire requirement. Portable PDB, `DebugType=portable`. Source paths in the PDB point at `sim/` on disk, so breakpoints resolve in the real source files, not a decompilation. |
| B / C / D (source in Unity) | **Yes**, with zero setup | Unity generates the PDB into `Library/ScriptAssemblies` itself. |

There is no meaningful debugging penalty for the precompiled DLL. It costs one extra file in the copy step. If you have
ever wondered why a Unity plug-in "can't be debugged", it is almost always that the PDB was left in `bin/Debug`.

Note the corollary: **do not ship a Release build of the sim into the Unity project during development.** Build the sim
Debug for the editor loop and Release for the determinism matrix and shipping builds — C# has no debug/release overflow
asymmetry (Part III §3), so the two are semantically identical and swapping is safe.

---

## 5. Analyzers: how they wire up in Unity, and why MSBuild is the better host

Unity's analyzer support is real but is a parallel, hand-wired universe. Analyzers and source generators "are imported as
managed plugins to your Unity project" [[16]](#s16). The full wiring, from Unity's own instructions:

1. Extract the analyzer DLLs from the nupkg — the `analyzers/dotnet/cs/` folder — and copy them into `Assets`
   [[12]](#s12). (`Microsoft.CodeAnalysis.BannedApiAnalyzers` ships exactly that folder layout, so this step is clean.)
2. In the Plugin Inspector, disable **Any Platform**, then disable **Editor** and **Standalone** under Include Platforms
   [[12]](#s12)[[15]](#s15).
3. Add the asset label **`RoslynAnalyzer`** — "this label must match exactly and is case sensitive" [[15]](#s15).
4. Scope is positional, not declarative: "If an analyzer is in a folder that contains an assembly definition file, or
   one of its subfolders, the analyzer only applies to that assembly, and to any other assembly that references it"
   [[13]](#s13). An analyzer at the `Assets` root applies to everything.
5. Severity comes from a `.ruleset` — `Default.ruleset` in `Assets` for project-wide rules, or a ruleset beside a
   specific `.asmdef`; "the `Default.ruleset` is the only single rule set file that can apply to more than one assembly"
   [[13]](#s13). `.editorconfig` (`dotnet_diagnostic.<ID>.severity = error`) is the documented alternative.
6. `AdditionalFiles` use a Unity-specific naming convention: "files must be named according to the format
   `Filename.[Analyzer Name].additionalfile`" [[14]](#s14).

**Version compatibility is a live constraint.** Unity 6 requires analyzers built against `Microsoft.CodeAnalysis.CSharp`
**4.3**, targeting `netstandard2.0` [[15]](#s15). Analyzers compiled against a newer Roslyn than the host will not load.
Inspecting the shipped assemblies:

| Package | Analyzer TFM | `Microsoft.CodeAnalysis` ref | `System.Collections.Immutable` ref |
|---|---|---|---|
| `BannedApiAnalyzers` 4.14.0 | netstandard2.0 | 3.11.0.0 | **9.0.0.0** |
| `BannedApiAnalyzers` 3.3.4 | netstandard2.0 | 2.9.0.0 | 1.2.3.0 |

Both reference a Roslyn *older* than Unity's 4.3, so both should load — but 4.14.0's `System.Collections.Immutable`
9.0.0.0 reference is a plausible load failure against Unity's bundled compiler, which ships its own version-locked copy.
If you do host analyzers in Unity, start at **3.3.4** and only move up if it works. Under MSBuild none of this exists:
the .NET 10 SDK's Roslyn is newer than everything, and `<PackageReference>` handles it.

**Where each mechanism leaves you:**

| Mechanism | Do analyzers run on sim code? |
|---|---|
| A / E (precompiled DLL) | **Yes — in `dotnet build`, via ordinary `<PackageReference>` + `<AdditionalFiles>`.** Verified working on this machine. Zero Unity involvement, zero Roslyn-version risk, and enforcement sits on the one compilation that produces the shipped bytes. Also runs unchanged in CI, where Unity is not installed. |
| B / C / D (source in Unity) | **In MSBuild, yes** (the IDE and the CLI/server builds see the analyzer). **In Unity's compile, only if you hand-wire all six steps above** — and see §6, because for this particular analyzer that wiring appears not to be possible. |

---

## 6. `BannedApiAnalyzers` does less than Part III assumes — two findings

This is the most consequential thing in this note, and it is independent of which integration you pick.

### 6a. It does not catch `float` or `double` as declarations, parameters, returns, or arithmetic

Part III §3 lists `float`, `double`, `decimal` first in the banned table and calls the analyzer the enforcement
mechanism. It is not, for that row.

`SymbolIsBannedAnalyzerBase` registers on these operation kinds only — `ObjectCreation`, `Invocation`, `EventReference`,
`FieldReference`, `MethodReference`, `PropertyReference`, `ArrayCreation`, `AddressOf`, `Conversion`, `UnaryOperator`,
`BinaryOperator`, `Increment`, `Decrement`, `TypeOf` — plus syntax actions for XML `cref`s and base-type declarations
[[31]](#s31). Types used in *declarations* are never visited.

Measured, with `T:System.Single` and `T:System.Double` banned and `dotnet_diagnostic.RS0030.severity = error`:

| Code | RS0030? |
|---|---|
| `public static float F;` | **no** |
| `public static double Bad(double d) => d * 2.0;` | **no** (built-in operators have no operator method to inspect) |
| `public static long Trunc(double d) => (long)d;` | **no** (built-in conversion, likewise) |
| `float.Parse(s)` | yes |
| `f.ToString()` where `f` is `float` | yes |
| `System.Math.Sqrt(d)` (`T:System.Math`) | yes |
| `new System.Random()` (`T:System.Random`) | yes |
| `new Dictionary<int,int>()` (`T:System.Collections.Generic.Dictionary`2`) | yes |
| `System.Array.Sort(a)` (`M:System.Array.Sort``1(``0[])`) | yes |

So the rows Part III cares about most — `Math.*`, `Dictionary`, `System.Random`, `Array.Sort` — are enforced exactly as
promised, and severity escalation to a build error works via `.editorconfig`. But **`float x = a * b;` sails straight
through.** A leak of exactly the kind Part III warns about ("one leak loses determinism silently and you find out months
later") is not caught.

Note also that `Array.Sort` needs the method-level documentation-comment ID with its overload signature
(``M:System.Array.Sort``1(``0[])``); banning `T:System.Array` would be too broad and banning `Array.Sort` by name does
nothing. Each overload you care about needs its own line.

**Fix — pick one, in the sim's own build, not Unity's:**

- **A ~40-line custom `DiagnosticAnalyzer`** registering a `SymbolAction` on `Field`/`Property`/`Parameter`/`Method`
  and flagging any `SpecialType.System_Single`/`System_Double`/`System_Decimal` in the signature, plus a
  `SyntaxNodeAction` on real literals. Cheap, exact, and it composes with `BannedApiAnalyzers` for the rest of the list.
- **Or a post-build metadata assertion in `sim.tests`** — walk `Sim.dll` with `System.Reflection.Metadata` and fail if
  any signature mentions `Single`/`Double`/`Decimal` or any method body contains `ldc.r4`/`ldc.r8`. Airtight, catches
  locals and constants the analyzer cannot see, and it validates *the artefact* rather than the source — which is the
  right level for this project. Slightly more work; strictly stronger.

Either one wants the sim to be a normal MSBuild project. Neither is convenient to run inside Unity's compile.

### 6b. Unity's `AdditionalFiles` convention and the analyzer's filename matcher are incompatible

`BannedApiAnalyzers` finds its banned list by filename. The matcher is literal [[32]](#s32):

```csharp
let fileName = Path.GetFileName(additionalFile.Path)
where fileName != null && fileName.StartsWith("BannedSymbols.", StringComparison.Ordinal)
                       && fileName.EndsWith(".txt", StringComparison.Ordinal)
```

The documented required names are `BannedSymbols.txt` or `BannedSymbols.*.txt`, added as `<AdditionalFiles>`
[[30]](#s30). Unity's mechanism for getting additional files into a compilation requires the name
`Filename.[Analyzer Name].additionalfile` [[14]](#s14) — which ends in `.additionalfile`, not `.txt`, and therefore
**cannot satisfy that matcher**.

The failure mode is the bad one: the analyzer loads, finds no banned-symbols file, reports nothing, and the build stays
green. You would believe the rule is enforced and it would not be. There is no error, no warning, no signal.

An untested escape hatch exists — `Assets/csc.rsp` (or a `.rsp` named after the asmdef) passes raw arguments to Unity's
csc [[33]](#s33), and `/additionalfile:` is a valid csc switch — but that is undocumented for this purpose and would
itself need a test that proves a banned symbol fails the build.

**This is the decisive practical argument.** Part III's enforcement mechanism works perfectly under `dotnet build` and,
on the evidence, does not work at all under Unity's compiler without a workaround nobody has validated. Compile the sim
outside Unity and the whole problem is out of scope.

---

## 7. Assembly definition settings that bear on this

| Field (JSON) | Inspector | Default | Relevance |
|---|---|---|---|
| `noEngineReferences` | No Engine References | `false` | "When enabled, Unity does not add references to `UnityEditor` or `UnityEngine` assemblies" [[1]](#s1). The whole B/C/D guarantee rests on this one boolean, which is off by default in every newly created asmdef. |
| `autoReferenced` | Auto Referenced | `true` | Whether Unity's predefined assemblies (`Assembly-CSharp`) auto-reference this assembly [[1]](#s1). Set `false` on the sim so a stray `MonoBehaviour` cannot reach it without an explicit edge. |
| `overrideReferences` + `precompiledReferences` | Override References + Assembly References | `false` / `[]` | "Enable this option to manually specify which precompiled assemblies this assembly depends upon" [[1]](#s1)[[2]](#s2). This is how the view asmdef declares its dependency on `Sim.dll` under option A/E. |
| `allowUnsafeCode` | Allow 'unsafe' Code | `false` | Irrelevant to the sim (Part III's struct-of-arrays needs no pointers), and there is a separate Player setting for DLLs that do [[4]](#s4). |
| `defineConstraints` / `versionDefines` | — | `[]` | Not needed here. |
| Plugin Inspector **Auto Reference** | — | on | Per-DLL analogue of `autoReferenced` [[3]](#s3). Turn it off for `Sim.dll`. |
| Plugin Inspector **Validate References** | — | on | "Unity can check that your plug-in's references are available in the project" [[3]](#s3). Leave on; it is the thing that will tell you loudly if `Sim.dll` accidentally starts targeting `net10.0`. |

---

## 8. Comparison

| | A. DLL in Plugins | B. Source + asmdef | C. Local pkg (source) | D. Embedded pkg (source) | **E. Local pkg + DLL** |
|---|---|---|---|---|---|
| `UnityEngine` unresolvable | **By construction** | By checkbox | By checkbox | By checkbox | **By construction** |
| One artefact across client/server/CLI | **Yes** | No | No | No | **Yes** |
| Sim version pinned & visible | Weakly | No | Via `package.json` | Via `package.json` | **Via `package.json`** |
| Change one line | build (~0.8 s) + reimport + reload | reimport + reload | reimport + reload | reimport + reload | build + reimport + reload |
| Manual copy step | avoidable via build output path | none | none | none | avoidable |
| Editor restart | not in the normal path; **test this** | never | never | never | **test this** |
| Step into sim from Unity | yes, PDB beside DLL | yes, free | yes, free | yes, free | yes, PDB beside DLL |
| `BannedApiAnalyzers` runs | **Yes, in MSBuild** | Unity path likely broken (§6b) | likely broken | likely broken | **Yes, in MSBuild** |
| `.meta` files in sim source tree | one, for the DLL | one per file | one per file | one per file | one, for the DLL |
| `sim/` keeps Part III §6 shape | yes | **no** | yes | no | yes |

---

## 9. Recommendation, and what would change it

**Take E, with A as the week-one fallback.** Concretely:

```
sim/
  Sim.csproj                     # netstandard2.1, LangVersion pinned, DebugType portable
  BannedSymbols.txt              # <AdditionalFiles>, plus the custom float analyzer from §6a
  package.json                   # com.ssalter.sim, version == the sim_version in the record
  Runtime/
    Sim.dll  Sim.dll.meta        # dotnet build output; committed
    Sim.pdb                      # committed, so breakpoints work
    com.ssalter.sim.asmdef       # optional wrapper; or reference the DLL directly
client/
  Packages/manifest.json         # "com.ssalter.sim": "file:../../sim"
  Assets/View/View.asmdef        # overrideReferences: true, precompiledReferences: ["Sim.dll"]
```

`dotnet build sim -c Debug` during development, `-c Release` for the determinism matrix and shipping builds. Commit the
DLL and PDB: they are small, they make the client project openable without a build step, they carry the plug-in import
settings in a `.meta` that would otherwise regenerate per machine, and — the real reason — a client commit then names
exactly one sim binary, which is what "separately versioned artefact" has to mean if ghost records are going to replay
for years.

The argument in one line: **the mechanism that makes `UnityEngine` genuinely unresolvable and the mechanism that makes
the client run the binary CI hashed are the same mechanism**, and it also happens to be the only one where Part III's
banned-API enforcement provably works. The ~0.8-second build in the loop is the price, and it is trivial next to a
domain reload you were going to pay anyway.

**What would change my mind:**

1. **A managed plug-in swap turns out to need an editor restart in Unity 6.** This is the one thing the documentation
   does not settle [[27]](#s27). If rebuilding `Sim.dll` with the editor open reliably fails, the loop goes from
   "3 seconds" to "40 seconds and a lost scene state", and that is bad enough to reconsider. **Test it in the first
   hour of the skeleton**: open the editor, change a constant in the sim, rebuild, and time the round trip. Report the
   number; it is the single most decision-relevant measurement in this whole area.
2. **Someone demonstrates `Assets/csc.rsp` with `/additionalfile:` feeding `BannedSymbols.txt` to Unity's compiler**,
   and a banned symbol actually fails a Unity build. That removes §6b, which is the sharpest edge of the argument. It
   does *not* remove the two-binaries problem in §Recommendation, so it would soften the case rather than reverse it.
3. **The sim stops being pure.** If the sim ever needs to be edited from inside Unity — a Unity-authored tuning window
   writing into sim types, an inspector-driven balance workflow — the source-in-package option (C) becomes worth its
   costs. Part III §6 explicitly routes around this by putting tuning in `content/` as data, so it should not happen;
   if it does, that is a design change to argue about, not an integration choice.
4. **Unity 6 drops or deprecates .NET Standard 2.1 as an API compatibility level.** It has not [[5]](#s5), and .NET
   Standard 2.1 is frozen rather than deprecated [[28]](#s28), but it is the assumption with the longest tail. The
   mitigation is cheap: multi-target `netstandard2.1;net10.0` in `Sim.csproj` and ship the `netstandard2.1` output to
   Unity while the server and CLI take `net10.0`. That is one line and it is worth adding on day one — it also lets the
   server and CLI compile at a modern `LangVersion` while the Unity-facing artefact stays conservative.

---

## Sources

<a id="s1"></a>1. Unity 6 — [Assembly Definition properties reference](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AssemblyDefinitionImporter.html). "No Engine References: When enabled, Unity does not add references to `UnityEditor` or `UnityEngine` assemblies." Also Auto Referenced, Override References, Allow 'unsafe' Code.
<a id="s2"></a>2. Unity 6 — [Assembly definition file format](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-file-format.html). `noEngineReferences` (default `false`), `autoReferenced` (default `true`), `overrideReferences`, `precompiledReferences`, `defineConstraints`, `versionDefines`.
<a id="s3"></a>3. Unity 6 — [Import and configure plug-ins (Plugin Inspector)](https://docs.unity3d.com/6000.0/Documentation/Manual/plug-in-inspector.html). Auto Reference and Validate References descriptions; managed plug-in placement.
<a id="s4"></a>4. Unity 6 — [Managed plug-ins](https://docs.unity3d.com/6000.0/Documentation/Manual/plug-ins-managed.html). ".NET assemblies you create and compile outside of Unity"; copy the DLL from `bin/Debug/` into `Assets`; Allow Unsafe Code player setting.
<a id="s5"></a>5. Unity 6 — [API compatibility levels for .NET](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html). ".NET Standard 2.1" is "the default API compatibility Level"; .NET Framework 4.8 alternative; Mono JIT vs IL2CPP AOT.
<a id="s6"></a>6. Unity 6 — [C# compiler](https://docs.unity3d.com/6000.0/Documentation/Manual/csharp-compiler.html). "C# compiler: Roslyn", "C# language version: C# 9.0".
<a id="s7"></a>7. Unity 6 — [Domain reloading](https://docs.unity3d.com/6000.0/Documentation/Manual/domain-reloading.html). "Unity also performs domain reload as part of an asset database refresh when it detects changes to scripts"; Enter Play Mode Settings; the iteration-time cost and the consequences of disabling it.
<a id="s8"></a>8. Unity 6 — [Asset Pipeline preferences](https://docs.unity3d.com/6000.0/Documentation/Manual/preferences-asset-pipeline.html). Auto Refresh (Disabled / Enabled / Enabled Outside Playmode); Directory Monitoring (Windows).
<a id="s9"></a>9. Unity 6 — [General preferences](https://docs.unity3d.com/6000.0/Documentation/Manual/preferences-general.html). Script Changes While Playing: Recompile And Continue Playing (default) / Recompile After Finished Playing / Stop Playing And Recompile.
<a id="s10"></a>10. Unity 6 — [Debug C# code in Unity](https://docs.unity3d.com/6000.0/Documentation/Manual/managed-code-debugging.html). Mono and IL2CPP, all platforms except Web; Debug Mode / Development Build + Script Debugging; "Managed code debugging information is stored in files named .pdb, next to the managed assembly (.dll file) on the disk."
<a id="s11"></a>11. Unity 6 — [Troubleshooting debugging](https://docs.unity3d.com/6000.3/Documentation/Manual/managed-debugging-troubleshooting.html). "Unity cannot generate debugging information for managed plug-ins in your project. You can only debug code from managed plug-ins if the associated .pdb files are next to the managed plug-ins in the Unity project on disk."
<a id="s12"></a>12. Unity 6 — [Install and use an existing analyzer or source generator](https://docs.unity3d.com/6000.1/Documentation/Manual/install-existing-analyzer.html). Extract `analyzers/dotnet/cs`, copy into `Assets`, disable Any Platform / Editor / Standalone, apply the `RoslynAnalyzer` label.
<a id="s13"></a>13. Unity 6 — [Analyzer scope and diagnostics](https://docs.unity3d.com/6000.0/Documentation/Manual/analyzer-scope-and-diagnostics.html). Scope by asmdef folder; `Default.ruleset` "is the only single rule set file that can apply to more than one assembly"; `.editorconfig` alternative.
<a id="s14"></a>14. Unity 6 — [Additional files for Roslyn analyzers and source generators](https://docs.unity3d.com/6000.0/Documentation/Manual/roslyn-analyzers-additional-files.html). "files must be named according to the format `Filename.[Analyzer Name].additionalfile`"; analyzer name is case sensitive; `Filename` cannot contain periods.
<a id="s15"></a>15. Unity 6 — [Create and use a source generator](https://docs.unity3d.com/6000.0/Documentation/Manual/create-source-generator.html). "create a C# class library project that targets .NET Standard 2.0"; "Your source generator must use Microsoft.CodeAnalysis.Csharp 4.3 to work with Unity"; import steps and the case-sensitive `RoslynAnalyzer` label.
<a id="s16"></a>16. Unity 6 — [Code analysis and source generation](https://docs.unity3d.com/6000.0/Documentation/Manual/roslyn-analyzers.html). Analyzers "are imported as managed plugins to your Unity project"; "Roslyn analyzers are only compatible with the IDEs that Unity supports."
<a id="s17"></a>17. Unity 6 — [Local folder or tarball paths](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-localpath.html). `file:` prefix; "a path preceded with two dots (`..`) refers to the root of the project path, so that `../another_folder` is a sibling of the `Packages` folder."
<a id="s18"></a>18. Unity 6 — [Install a UPM package from a local folder](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-local.html). The package is not copied; Package Manager references its location. Cannot live in `Assets`, `Library`, `ProjectSettings`; inside `Packages` it becomes embedded.
<a id="s19"></a>19. Unity 6 — [Package Manager concepts](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-concepts.html). "You can permanently change content only from Local and Embedded package sources."
<a id="s20"></a>20. Unity 6 — [Embedded dependencies](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-embed.html). "Any package that appears under your project's `Packages` folder is embedded in that project."
<a id="s21"></a>21. Unity 6 — [Assembly definition files in packages](https://docs.unity3d.com/6000.0/Documentation/Manual/cus-asmdef.html). "You must associate scripts inside a package to an assembly definition file (`.asmdef`)"; `Runtime/<company>.<package>.asmdef` naming.
<a id="s22"></a>22. Unity 6 — [Package manifest](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-manifestPkg.html). `name` (reverse-domain) and `version` are the only required fields.
<a id="s23"></a>23. Unity 6 — [Special folder names](https://docs.unity3d.com/6000.0/Documentation/Manual/SpecialFolders.html). `Plugins` is "Reserved for third-party plugins."
<a id="s24"></a>24. Unity — [No Symlinks Validation, Asset Store Validation package](https://docs.unity3d.com/Packages/com.unity.asset-store-validation@0.5/manual/no_symlinks_validation.html). "Using symlinks in Unity projects may cause your project to become corrupted…"
<a id="s25"></a>25. Unity 6 — [Asset metadata](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetMetadata.html) ("Unity creates `.meta` files for each folder and file in your project's `Assets` folder") and [Meta Files Validation](https://docs.unity3d.com/Packages/com.unity.asset-store-validation@0.5/manual/meta_files_validation.html) (packages must carry `.meta` files for their contents; duplicate-GUID checks).
<a id="s26"></a>26. Unity 6 — [IDE support](https://docs.unity3d.com/6000.0/Documentation/Manual/scripting-ide-support.html). "Unity automatically creates and maintains a Visual Studio `.sln` and `.csproj` file"; notes "differences in the way Unity and Visual Studio compiles user code" requiring extra analyzer configuration.
<a id="s27"></a>27. Unity Issue Tracker — ["Deleted DLL in use from Plugins during runtime only removes the meta file, forcing DLL to be imported every time it is deleted"](https://issuetracker.unity3d.com/issues/deleted-dll-in-use-from-plugins-during-runtime-only-removes-the-meta-file-forcing-dll-to-be-imported-every-time-it-is-deleted). Evidence that loaded `Plugins` assemblies are held open at runtime. Not a Unity 6 statement; treat as the hazard to test, not as a settled fact.
<a id="s28"></a>28. Microsoft Learn — [.NET Standard](https://learn.microsoft.com/en-us/dotnet/standard/net-standard). "No new versions of .NET Standard will be released, but .NET 5 and all later versions will continue to support .NET Standard 2.1 and earlier"; Unity 2021.2 implements .NET Standard 2.1; ".NET Standard not deprecated — use `netstandard2.1` to share code between Mono and .NET Core 3.x."
<a id="s29"></a>29. Microsoft Learn — [Configure the language version](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version). "the default version aligns with the project's target framework (`TFM`)"; `<LangVersion>` override; warning against `latest`.
<a id="s30"></a>30. dotnet/roslyn — [`BannedApiAnalyzers.Help.md`](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md). Required file names `BannedSymbols.txt` / `BannedSymbols.*.txt` added as `<AdditionalFiles>`; entry format `{Documentation Comment ID};[Description]`; `T:`/`M:`/`F:`/`P:`/`E:`/`N:` prefixes.
<a id="s31"></a>31. dotnet/roslyn — [`SymbolIsBannedAnalyzerBase.cs`](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/Core/SymbolIsBannedAnalyzerBase.cs). Registered operation kinds (ObjectCreation, Invocation, EventReference, FieldReference, MethodReference, PropertyReference, ArrayCreation, AddressOf, Conversion, UnaryOperator, BinaryOperator, Increment, Decrement, TypeOf) and syntax actions (XML cref, base types). No symbol action over declarations.
<a id="s32"></a>32. dotnet/roslyn — [`SymbolIsBannedAnalyzer.cs`](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/Core/SymbolIsBannedAnalyzer.cs). Additional-file selection: `fileName.StartsWith("BannedSymbols.", StringComparison.Ordinal) && fileName.EndsWith(".txt", StringComparison.Ordinal)`.
<a id="s33"></a>33. Unity 6 — [Add class library references / response files](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-assemblies.html) and [Conditional compilation in Unity](https://docs.unity3d.com/Manual/platform-dependent-compilation.html). `Assets/csc.rsp` passes raw arguments to Unity's C# compiler; changes require a recompile.
<a id="s34"></a>34. **Measured locally, 30 July 2026.** Windows 11, .NET SDK 10.0.101 (sole SDK), MSBuild 18.0.6, no workloads. (a) A `netstandard2.1` class library with `Microsoft.CodeAnalysis.BannedApiAnalyzers` 4.14.0 restored and built clean with no additional installs; `NETStandard.Library.Ref` ships in `C:\Program Files\dotnet\packs`. (b) RS0030 fire/no-fire matrix as tabulated in §6a; `.editorconfig` `dotnet_diagnostic.RS0030.severity = error` promotes them to build errors. (c) Analyzer assembly references read from package metadata: 4.14.0 → `Microsoft.CodeAnalysis` 3.11.0.0, `System.Collections.Immutable` 9.0.0.0; 3.3.4 → 2.9.0.0, 1.2.3.0; both `netstandard2.0`. (d) Incremental `dotnet build` of a one-file `netstandard2.1` library: 797 / 806 / 793 ms.
