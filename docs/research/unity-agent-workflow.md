# Claude Code Inside a Unity 6 Project

**Research note** · 30 July 2026 · resolves [#4](https://github.com/ssalter21/tower-defense-game/issues/4)

**Question:** inside a Unity 6 project, what can a terminal-only coding agent actually do, and what must a human
do by hand in the editor?

---

## Verdict

**Unity is less agent-hostile than the map assumed, and for a non-obvious reason: almost nothing that matters is
hand-authored YAML.** The naive mental model — "scenes are text, so the agent edits the text" — is wrong, and
Unity says so in as many words: *"You cannot externally produce or edit UnityYAML files"*
([UnityYAML](https://docs.unity3d.com/6000.0/Documentation/Manual/UnityYAML.html)). The correct model is that
an agent writes **editor C#** and then triggers it, either headlessly via `-batchmode -executeMethod` or live
against a running Editor via Unity's first-party MCP bridge. Everything a human would click — creating scenes,
saving prefabs, setting import settings, building Animator Controllers, changing player settings, running tests,
producing builds — has a documented `UnityEditor` API, and that API is reachable without a display.

The genuinely mouse-only residue is **small, front-loaded, and mostly one-time**: install the Hub, activate a
Personal licence (the one hard GUI gate, [documented as Hub-only](https://docs.unity3d.com/6000.0/Documentation/Manual/LicenseActivationMethods.html)),
approve the MCP client once, then a couple of hours of visual judgement — camera framing, asset scale, rig
sanity, lighting — that no agent could do for you in *any* engine because the deliverable is an opinion about
how something looks.

**One caveat that changes which bridge you use.** Unity ships a first-party MCP server that names Claude Code as
a client and exposes exactly the right tools — but Unity staff have confirmed *"as of right now, a subscription
is required for an MCP connection"*, and the Personal column of the pricing comparison reads **"Monthly
subscription required"** against *Unity AI Concurrent MCP Connections*. On this project's stated $0 budget the
first-party bridge is **out**, and the free paths are the MIT community bridge or plain
`-batchmode -executeMethod`. See §6 — it is the one place where Unity's answer got worse on inspection, not
better.

**The load-bearing finding for the spec:** the architecture Part III already chose for determinism reasons —
sim as a library, *"the view layer must be a pure function of simulation state"* — independently makes this
project agent-tractable. Unity's agent-hostility is concentrated almost entirely in scene YAML, prefab wiring
and inspector-serialized state. A project whose view is built from sim state at runtime has very little of any
of those. **Keep the scene almost empty and Unity's agent problem mostly evaporates.** Design against that
explicitly rather than discovering it.

---

## 1. The three-way division

### An agent can do this — unattended, no GUI

| Task | Mechanism |
|---|---|
| Create the project | `-createProject <path> -batchmode -quit` ([CLI ref](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html)) |
| Add/remove packages | Edit `Packages/manifest.json` — plain JSON, documented format ([project manifest](https://docs.unity3d.com/Manual/upm-manifestPrj.html)) |
| Write all runtime and editor C# | Ordinary files under `Assets/`; editor-only code goes in an `Editor/` folder |
| Define assemblies | `.asmdef` is *"a JSON object"* with a documented field list ([format ref](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-file-format.html)) |
| Author UI | UXML/USS are text and hand-authorable ([Structure UI with UXML](https://docs.unity3d.com/6000.4/Documentation/Manual/UIE-UXML.html)) |
| Create and populate scenes | `EditorSceneManager` + `-executeMethod` |
| Create prefabs | [`PrefabUtility.SaveAsPrefabAsset`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PrefabUtility.SaveAsPrefabAsset.html) |
| Create `.asset` data | `AssetDatabase.CreateAsset` on a `ScriptableObject` |
| Set import settings | [`ModelImporter`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ModelImporter.html) — *"lets you modify model import settings from editor scripts"* — plus `AssetPostprocessor` for do-it-on-every-import |
| Build Animator Controllers | [`UnityEditor.Animations.AnimatorController`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animations.AnimatorController.html) — `CreateAnimatorControllerAtPath`, `AddLayer`, `AddMotion`, `AddParameter` |
| Change project settings | `PlayerSettings` / `EditorSettings` / `GraphicsSettings` APIs — **not** by editing `ProjectSettings/*.asset` |
| Run tests headless | `-runTests -testPlatform EditMode|PlayMode -testResults <path>` ([UTF CLI ref](https://docs.unity3d.com/6000.2/Documentation/Manual/test-framework/reference-command-line.html)) |
| Produce builds | `-build <path>` from the active build profile, or `BuildPipeline.BuildPlayer` via `-executeMethod` |
| Invoke any existing menu command | `EditorApplication.ExecuteMenuItem("Path/To/Item")` |
| Review its own work | Every artifact above is text under Force Text serialization, so `git diff` is a real review surface |
| Git hygiene | `.gitignore`, LFS config, branch/commit/push |

### You must do this by hand

| Task | Why it can't be automated | Frequency |
|---|---|---|
| Install Unity Hub, sign in, activate a **Personal** licence | *"For Unity Personal, the Unity Hub is the only method for activating and returning licences"* ([activation methods](https://docs.unity3d.com/6000.0/Documentation/Manual/LicenseActivationMethods.html)); the `-serial`/`-username` CLI path *"[doesn't] apply to Unity Personal"* ([manage licence via CLI](https://docs.unity3d.com/6000.0/Documentation/Manual/ManagingYourUnityLicense.html)) | Once per machine |
| Install an MCP bridge and approve the client connection | For the first-party server: *"an external MCP client... Unity shows a Pending Connection message... you must approve it before the client can invoke tools"* ([get started](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.16/manual/integration/unity-mcp-get-started.html)) — but see §6, it is paid on Personal | Once per client |
| **Look at it.** Camera framing, asset scale, silhouette read, material and lighting judgement, "does this animation retarget look like a person" | The deliverable is an aesthetic opinion | Continuous, and correctly so |
| **Play it.** Timing feel, readability under load | Same | Continuous |
| Avatar *Configure* when humanoid auto-mapping fails | `ModelImporter.humanDescription` exists, but bone-by-bone remapping through the API is strictly worse than the mapping window | Per broken rig |
| Shader Graph / VFX Graph authoring | Node graphs; not hand-authorable assets and no meaningful construction API | Only if used — **avoid for the skeleton** |
| Arbitrate a `.unity` / `.prefab` merge conflict | Text ≠ mergeable; see §2 | Structurally avoidable |
| Bake global illumination | *"-nographics does not allow you to bake GI"* ([CLI ref](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html)) | Avoid; use realtime lighting |
| Close the Editor before a batchmode run | The project lock — see §5 | Every headless run, unless using MCP |

### Nobody should do this by hand

- **Hand-edit `.unity`, `.prefab` or `.asset` YAML.** Unity's own manual: *"You cannot externally produce or
  edit UnityYAML files."* The library is a custom subset — no comments, no tags, no multiple documents, no
  complex keys ([UnityYAML](https://docs.unity3d.com/6000.0/Documentation/Manual/UnityYAML.html)) — and the
  content is `!u!` class IDs, `&` fileIDs, cross-file GUID references, and floats written as hex with the
  decimal *"in parentheses for debugging purposes, but only the hex is actually parsed"*
  ([format of text serialized files](https://docs.unity3d.com/Manual/FormatDescription.html)). It is text you
  can *read* in a diff. It is not text you should *write*.
- **Hand-edit `ProjectSettings/*.asset`.** Same YAML, same reasons. Use the settings APIs.
- **Hand-edit `.meta` GUIDs**, or let an asset and its `.meta` drift apart. See §3.
- **Commit `Library/`**, or gitignore `*.meta`.
- **Drive package installs through the Package Manager window** when `manifest.json` is the reviewable,
  diffable source of truth.
- **Click through any setup a `[MenuItem]` could do once.** This is the whole game: the correct response to a
  repetitive editor chore is an editor script, which the agent writes, and which is then also reproducible,
  reviewable and re-runnable in CI.

---

## 2. What is text, and whether authoring it is *supported*

Unity 6 defaults to text serialization. Editor settings → Asset Serialization → Mode: *Force Text* — *"Convert
all assets to Text mode, including new assets. This is the default option"* — with a companion *Reduce version
control noise* toggle that *"[forces] the Editor to write references and similar YAML structures on one line"*
([Editor settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-EditorManager.html)). Verify both
on day one; do not assume.

| Artifact | Text? | Hand-authoring |
|---|---|---|
| `.unity` scene | UnityYAML | **Possible, not supported.** Read-only in practice |
| `.prefab` | UnityYAML | **Possible, not supported.** Same |
| `.asset` (ScriptableObject, settings) | UnityYAML | Same — create via `AssetDatabase.CreateAsset` |
| `ProjectSettings/*.asset` | UnityYAML | Same — change via `PlayerSettings`/`EditorSettings` API |
| `.controller` (Animator) | UnityYAML | Same — build via `AnimatorController` API |
| `.meta` | YAML | Never touch by hand except to move it with its asset |
| `.asmdef` | **JSON, documented** | **Supported.** Write freely |
| `Packages/manifest.json` | **JSON, documented** | **Supported.** Write freely |
| `.uxml` / `.uss` | **XML / CSS-like** | **Supported.** Write freely |
| `.cs` | C# | Obviously |
| `.shader` (hand-written HLSL) | Text | Supported, if you are writing shaders by hand |

The distinction the ticket asked for is real and it splits cleanly: **the four formats Unity documents as
formats — asmdef, manifest, UXML, USS — are agent territory. Everything serialized through UnityYAML is
agent-*readable* and agent-*generated-via-API*, never agent-typed.**

A practical corollary: text serialization buys you *review*, not *merge*. Two people editing one scene still
produces a conflict that is not sanely resolvable by hand, because object identity lives in fileIDs. For a solo
developer this mostly does not bite — but it is another argument for keeping the scene nearly empty.

---

## 3. `.meta` files and git hygiene

Unity creates a `.meta` file *"for each folder and file in your project's `Assets` folder"* as part of import,
containing *"the unique ID assigned to the asset, and values for all the asset's import settings"*
([asset metadata](https://docs.unity3d.com/6000.2/Documentation/Manual/AssetMetadata.html)). They are hidden in
the Project window and typically hidden by the OS file browser.

**What breaks when a file appears from outside the Editor:** nothing, if you let Unity import it. The Editor
*"automatically detects changes to assets on disk and imports them"*, and `AssetDatabase.Refresh` exists for the
case where Auto Refresh is off or *"you have made changes to assets on disk from outside the editor"*
([refreshing the Asset Database](https://docs.unity3d.com/Manual/AssetDatabaseRefreshing.html)). So an agent
that writes `Assets/Scripts/Foo.cs` and then runs *anything* in batchmode gets a `.meta` generated for it, and
must then commit that `.meta`.

**What actually breaks:** separating an asset from its `.meta`. *"If an asset loses its `.meta` file, any
reference to that asset is broken in your project"* — Unity treats it as brand new, and every scene, prefab and
material referring to it by GUID now points at nothing. *"If you move or rename an asset outside of Unity, you
must move or rename the `.meta` file to match."* This makes `git mv` on an asset a **two-file operation**, and
it is the single most likely way an agent silently destroys a Unity project. It belongs in `CLAUDE.md`.

**`.gitignore`.** Unity's own directory reference is the authority
([default project directories](https://docs.unity3d.com/6000.0/Documentation/Manual/default-directories.html)):

- **Commit:** `Assets/` (including every `.meta`), `Packages/` (`manifest.json` *and* `packages-lock.json`),
  `ProjectSettings/`.
- **Ignore:** `Library/` — *"exclude... because it's unique to your computer and is a working directory"*;
  `Temp/` — *"gets cleared every time you close Unity. Exclude this folder from version control"*;
  `UserSettings/` — *"exclude... to avoid overwriting your teammates' personal Unity preferences"*; `Logs/`,
  `obj/`, build output.
- **Start from** [github/gitignore's `Unity.gitignore`](https://github.com/github/gitignore/blob/main/Unity.gitignore),
  which is the de-facto standard and also ignores the generated `*.csproj`/`*.sln` (correct — Unity regenerates
  them).
- **Two traps in that template for this repo's layout:** it ignores `/[Bb]uild/` and `/[Bb]uilds/` **anchored to
  the file's directory**, so placing it at repo root while the Unity project lives in `client/` changes what
  those anchors mean. Put the Unity `.gitignore` inside `client/`, not at the repo root, or de-anchor
  deliberately. It also ignores `*.pdb` — harmless for Unity, but it would swallow debug symbols from `sim/`
  if the file is hoisted to the root.
- **Do not** ignore `*.meta`, `ProjectSettings/`, or `Packages/packages-lock.json`.
- **LFS:** not needed for this slice. CC0 low-poly FBX and small textures are kilobytes-to-low-megabytes. Add
  LFS when a binary asset class actually appears, not speculatively — LFS is easy to add and annoying to remove.

Version control mode should stay on the default *Visible Meta Files*, which is the setting for *"a version
control system that Unity doesn't support"* — i.e. git
([version control settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-VersionControlSettings.html)).

---

## 4. Editor scripting: the actual automation surface

Yes, and this is the answer to the ticket's central question.

A script in an `Editor/` folder may use the `UnityEditor` namespace. `[MenuItem]` *"allows you to add menu items
to the main menu"*; *"only static functions can use the MenuItem attribute"*
([MenuItem](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MenuItem.html)). The same static
method is directly invocable from the command line: `-executeMethod` will *"execute the static method as soon as
Unity opens the project"*, and the method *"must be placed in an Editor folder and defined as static"*
([CLI ref](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html)).

So the loop is:

```
agent writes  Assets/Editor/Setup.cs   (ordinary C#, fully reviewable)
        ↓
agent runs    Unity -batchmode -quit -projectPath client \
                    -executeMethod Setup.BuildEverything -logFile -
        ↓
agent reads   the log, the git diff of the YAML that got written, the test results
```

Every clicking task in this project has an API on the far side of that loop: scene creation and save
(`EditorSceneManager`), prefab creation (`PrefabUtility.SaveAsPrefabAsset`), asset creation and import
(`AssetDatabase`), model import settings including `animationType` / `avatarSetup` / `sourceAvatar` /
`clipAnimations` (`ModelImporter`), animator state machines (`AnimatorController`), player and graphics settings,
build invocation, and — as an escape hatch — `EditorApplication.ExecuteMenuItem` for anything exposed only as a
menu command.

**The limits, honestly:**

1. **No compile feedback without running Unity.** The agent cannot know whether a `MonoBehaviour` compiles
   without either a batchmode run (which compiles on open) or an MCP `ValidateScript` call. `dotnet build` on
   Unity's generated `.csproj` is unsupported and unreliable. Budget one Unity round-trip per compile check;
   this is the main per-iteration tax.
2. **`-executeMethod` failure signalling is crude.** Errors surface as an exception in the log or a non-zero
   `EditorApplication.Exit`. Write the setup scripts to be idempotent and to exit non-zero loudly, or CI will
   go green on a project that did not get built.
3. **Editor API surface is not a supported public contract in the way runtime API is.** Bits of it move between
   versions. Pin the Editor version.
4. **Anything whose output is a node graph** (Shader Graph, VFX Graph) has no construction API worth using.
5. **Nothing replaces looking at the result.** An editor script that places a camera cannot tell you the framing
   is bad.

---

## 5. Batch mode and the CLI: what genuinely runs without a display

`-batchmode` *"runs command line arguments without the need for human interaction"*; `-nographics` means
*"Unity doesn't initialize the graphics device"*, and note that *"output logs are disabled unless you specify
-logFile"* ([CLI ref](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html)).

**Works headless:** project creation (`-createProject`), package resolution, asset import, `-executeMethod`,
player builds (`-build`, or `BuildPipeline.BuildPlayer` via `-executeMethod`), `-importPackage`, and the test
runner. Practically all of the walking skeleton's mechanical work.

**Needs a graphics device:** GI baking — *"-nographics does not allow you to bake GI"* — plus, by extension,
anything that reads back rendered pixels. For a fixed-camera stylized project using realtime lighting this is a
non-issue; treat "we bake lightmaps" as a decision that costs you headless CI and decide it deliberately.

**Tests.** `-runTests -testPlatform EditMode|PlayMode -testResults results.xml`, with `-testFilter`,
`-testCategory`, `-assemblyNames`, `-runSynchronously` and retry/repeat flags
([UTF CLI ref](https://docs.unity3d.com/6000.2/Documentation/Manual/test-framework/reference-command-line.html)).
**One gotcha worth internalising: *"The Editor's regular `-quit` command-line argument is not supported while
tests are running."*** `-batchmode -quit -runTests` is the wrong incantation and will bite; use `-batchmode
-runTests` and let the runner exit.

**The project lock — the operational constraint nobody mentions.** A Unity project open in the Editor holds a
`UnityLockfile`, and a second process is refused: the error *"occurs when a Unity process runs in the background
or a UnityLockfile remains in the project folder"*
([Unity support](https://support.unity.com/hc/en-us/articles/40828087523092-Resolving-the-The-project-is-currently-open-in-the-Unity-Editor-Please-close-it-in-the-Editor-to-proceed-with-this-operation-Error)).

This produces a genuinely awkward property: **the two automation paths are mutually exclusive at any given
instant.** Batchmode requires the Editor *closed*; the MCP bridge (§6) requires it *open*. Pick one per session
rather than trying to run both. Practically: MCP while iterating with the Editor up, batchmode in CI and for
unattended runs.

**Licensing implications of batch mode.** Two separate facts, both from Unity:

1. The Editor needs an activated licence to run at all, in batch mode included. Command-line activation
   (`-serial -username -password`) exists and is explicitly aimed at *"headless mode (without a GUI) for
   automated tasks, such as builds and tests"* — but *"the following procedures don't apply to Unity Personal"*
   ([licence via CLI](https://docs.unity3d.com/6000.0/Documentation/Manual/ManagingYourUnityLicense.html)), and
   *"for Unity Personal, the Unity Hub is the only method for activating and returning licences"*
   ([activation methods](https://docs.unity3d.com/6000.0/Documentation/Manual/LicenseActivationMethods.html)).
   **On the free tier there is no headless activation path.** The workaround is fine for a solo dev — activate
   once through the Hub on the machine, after which local batchmode runs use the activated licence — but it
   means hosted CI running the *Unity* half of the build is not free-tier-friendly. It costs nothing for the
   determinism matrix Part III cares about, which runs `dotnet test` on `sim/` and never touches Unity.
2. The manual attaches a terms note directly to the batch mode section: *"Unless subject to separate Commercial
   Terms with Unity, using Unity in batch mode is subject to Unity's Terms of Service, including applicable
   Additional Terms."* Read it before wiring Unity into a build farm; irrelevant at one developer on one
   machine.

---

## 6. Agent bridges: Unity MCP exists, and it is first-party

This is the finding that most changes the answer, and it is recent enough that the map's framing ("Claude Code
cannot drive Unity's editor GUI") is now only half true.

### Unity's own MCP server — first-party, pre-release

Shipped inside the `com.unity.ai.assistant` package: *"Connect external AI clients to the Unity Editor so they
can call Unity tools through Model Context Protocol (MCP)"*, naming **Claude Code** explicitly among supported
clients ([Unity MCP overview](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.16/manual/integration/unity-mcp-overview.html)).

- **Architecture:** the Editor runs an MCP Bridge over local IPC (named pipes on Windows); a relay binary
  installed to `~/.unity/relay/` is what the AI client launches, with `--mcp`. *"When Unity starts, the MCP
  bridge launches automatically and opens a local IPC channel."*
- **Requirements:** *"Unity 6 (6000.0) or later with the `com.unity.ai.assistant` package installed"*
  ([get started](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.16/manual/integration/unity-mcp-get-started.html)).
  The package manifest itself states Unity 6.0.60f1 or 6.3+.
- **Setup:** Edit → Project Settings → AI → Unity MCP; the Integrations panel will auto-configure Claude Code.
  First connection lands in *Pending Connections* and must be approved by a human, once.
- **Tools exposed** (from the [`Unity.AI.MCP.Editor.Tools` API reference](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0/api/Unity.AI.MCP.Editor.Tools.html)):
  `ManageScene` (*"loading, saving, creating, and querying hierarchy"*), `ManageGameObject` (*"CRUD, find,
  components"*), `ManageAsset`, `ManageEditor` (*"controlling and querying the Unity Editor state, including
  managing Tags and Layers"*), `ManageMenuItem` (*"execute, list, exists"*), `ManageScript` / `CreateScript` /
  `ApplyTextEdits` / `ValidateScript`, `ManageShader`, `ReadConsole` (*"reading and clearing Unity Editor console
  log entries"*), `ImportExternalModel` (*"importing assets from outside the Unity project. Creates GameObject
  in the scene and creates a prefab for reuse"*), and `RunCommand` (*"compilation and execution of C# scripts in
  the Unity environment"*).

`ReadConsole` and `ValidateScript` alone remove the worst of the per-iteration tax identified in §4 — the agent
gets compile errors and runtime log output without a batchmode round trip. `RunCommand` means arbitrary editor
C# without even authoring a `[MenuItem]`.

**Credibility:** first-party, documented on docs.unity3d.com, and iterating hard — the package has gone from
`2.0.0-pre.1` to at least `2.16.0-pre.1` with docs published for each. But it is **pre-release**: `-pre.N`
throughout, no stable release, API surface visibly churning (the manual restructured `unity-mcp-*.html` into an
`integration/` folder between 2.0 and 2.7).

### Cost: the first-party MCP server is **paywalled on Personal**

This is the part that decides whether it belongs in the spec, and the answer is no.

Unity's own pricing page contradicts itself, so both readings are recorded here. The Unity Personal card's
*"What's included"* list contains a checked bullet reading **"Unity's MCP access"** (alongside *"Command Line
Interface access"*). The **plan comparison table on the same page**, Personal column, says:

> **Unity AI Concurrent MCP Connections — "Monthly subscription required"**

…sitting in a block where *Unity AI*, *Unity AI Gateway* and *Unity AI Credits* all carry the identical string.
Pro shows `3` concurrent MCP connections, Enterprise and Industry `5`, and the standalone **Unity AI
Subscription at $10/month** lists *"One concurrent MCP worker connection"* among its inclusions.
(Read from the page source of [unity.com/products](https://unity.com/products) on 30 July 2026 — unity.com
returns 403 to automated fetching, so this came from the embedded page data with a browser user-agent.
**A human should re-read this in a browser before it is treated as settled**, because the marketing bullet and
the feature table disagree.)

The tiebreak is a Unity staff answer, which is unambiguous. On a Unity Discussions thread titled
[*"Request for official clarification: Unity MCP access, subscription requirements, and future policy"*](https://discussions.unity.com/t/request-for-official-clarification-unity-mcp-access-subscription-requirements-and-future-policy/1720323)
(opened 19 May 2026, asking specifically whether *local* MCP — which consumes no Unity-hosted inference — needs
a subscription), Unity's AuroreUnity replied on **20 May 2026**:

> **"As of right now, a subscription is required for an MCP connection."**

She noted she had no information on future policy and was forwarding the feedback internally.

So the accurate statement is: **the first-party bridge costs $10/month on the free tier, for one concurrent
connection, and Unity has explicitly declined to separate local protocol access from the AI entitlement.** Note
what is *not* being charged for here — the model is Claude Code on your own subscription, and MCP tool calls do
not appear among the credit-consuming features in the
[credit reference](https://docs.unity.com/en-us/ai/credits/credits-about). The gate is on the connection, not
on usage.

**Consequence for this project, which is explicitly $0:** the first-party server is out of the default plan.
The free path is (a) the community bridge below, or (b) plain `-batchmode -executeMethod`, which is included on
Personal — *"Command Line Interface access"* is a listed Personal inclusion, and nothing in the CLI or licensing
docs gates it. Keep every automation expressible as an `-executeMethod` entry point regardless; that costs
nothing, because the MCP tools and the batchmode path call the same editor APIs. **$10/month is a defensible
purchase if the compile round-trip proves to be the real bottleneck — but it is a decision, not a default.**

### MCP for Unity (CoplayDev) — community, mature

[CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp), MIT, **12,995 stars / 1,380 forks / 85 open
issues, created March 2025, last pushed 28 July 2026** (GitHub API, this session). Stewardship was taken over
by Coplay, a Unity-AI startup, and it is
[explicitly not affiliated with Unity Technologies](https://www.pocketgamer.biz/coplay-takes-over-unity-mcp-as-it-reaches-key-milestones-with-public-beta-launch/)
(**community source, flagged**). Peers: [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) (3,748
stars, active) and [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) (1,851 stars, active).

By the usual solo-project test — is it maintained, is it licensed permissively, will it be there in six months —
CoplayDev passes comfortably; a 13k-star MIT project pushed today is about as safe as community tooling gets.
Its tool naming (`manage_scene`, `manage_gameobject`, `manage_asset`, `read_console`) is near-identical to
Unity's first-party set, so switching between them is a configuration change rather than a rewrite.

**Given the paywall above, this is the default choice for a $0 project, not the fallback.** It has no
entitlement gate, no connection cap and no monthly fee. The trade is the usual one: no vendor support, and a
first-party competitor now exists that could either commoditise it or starve it. Neither risk is expensive here
— if it breaks, the batchmode path still works and nothing in the project depends on the bridge.

---

## 7. Wiring `sim/` into `client/` — the specific shape of this project

Unity only compiles code under `Assets/` or `Packages/`, so Part III's repo layout (`sim/` beside `client/`)
needs one deliberate decision. Three options, all agent-executable:

1. **Local UPM package (recommended).** Give `sim/` a `package.json` and reference it from
   `client/Packages/manifest.json` as `"com.<you>.sim": "file:../../sim"` — relative paths *"offer better
   portability... when tracking a project and packages in the same repository"*
   ([local folder paths](https://docs.unity3d.com/Manual/upm-localpath.html)). Unity compiles the sim from
   source; `dotnet` compiles the same source for `simcli`/`server`. One source of truth, no build step, no DLL
   to keep in sync, and the whole wiring is a JSON edit an agent makes and you review.
2. **Compiled DLL as a managed plug-in.** `dotnet build sim/` → drop the `.dll` into `client/Assets/Plugins/`.
   Works ([managed plug-ins](https://docs.unity3d.com/6000.0/Documentation/Manual/plug-ins-managed.html)), but
   commits a binary, adds a sync step, and loses step-through debugging by default.
3. **Sim source inside `Assets/`.** Rejected: it inverts Part III's dependency direction and makes the sim's
   canonical home the Unity project.

Either way, put an `.asmdef` on the sim assembly with **`"noEngineReferences": true`**
([asmdef format](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-file-format.html)).
That reproduces inside Unity exactly the enforcement Part III §3 relies on outside it — `UnityEngine.*` becomes
unresolvable, and `Time.deltaTime` in the tick loop becomes a compile error rather than a code review.

Compatibility is not a problem: Unity 6's default API Compatibility Level is **.NET Standard 2.1** and the
compiler is Roslyn at **C# 9.0**
([.NET profiles](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html),
[C# compiler](https://docs.unity3d.com/6000.0/Documentation/Manual/csharp-compiler.html)) — exactly what Part III
specified. Note C# 9 `record` needs an `IsExternalInit` shim in Unity; trivial, but a real papercut if `sim/`
targets a newer language version. **Pin `sim/` to C# 9 and netstandard2.1 or accept divergence.**

**Assets.** Unity reads `.fbx`, `.dae`, `.3ds`, `.dxf`, `.obj` natively and *"internally uses the .fbx file
format as its importing chain"*; `.blend` imports only if Blender is installed
([model file formats](https://docs.unity3d.com/Manual/3D-formats.html)). `.gltf`/`.glb` need the
`com.unity.cloud.gltfast` package, which registers itself as the default importer for those extensions — a
`manifest.json` edit, therefore agent-doable. **Prefer FBX from the CC0 packs** and skip the extra dependency
for the skeleton.

---

## 8. Minimum hands-on-mouse list for a project of this shape

Fixed camera, stylized 3D, imported CC0 characters, external sim library, view built at runtime. Ordered, with
honest time estimates for someone who has never opened Unity.

| # | Hands-on step | Once or ongoing | Est. |
|---|---|---|---|
| 1 | Install Unity Hub; sign in; install Editor **6.3 LTS** (`6000.3`); activate Personal | Once per machine | 30–60 min |
| 2 | Open the agent-created project once so the Editor generates `Library/` and the first `.meta` sweep | Once | 5 min |
| 3 | Verify Asset Serialization = Force Text, Version Control = Visible Meta Files, Reduce version control noise on | Once | 2 min |
| 4 | Install an MCP bridge and approve the Claude Code connection — **the MIT community one, not `com.unity.ai.assistant`, which needs a $10/mo subscription** (§6) | Once (optional) | 10–20 min |
| 5 | Download the CC0 packs from itch.io and drop them in `Assets/` (browser + click-through licence pages) | Per pack | 10 min |
| 6 | Eyeball the first import of each pack: scale, forward axis, materials, whether the rig auto-mapped to Humanoid | Per pack | 15 min |
| 7 | Frame the fixed camera and commit the transform into a setup script so it never moves again | Once | 20 min |
| 8 | Judge lighting once — a directional light and ambient you can stand to look at | Once | 20 min |
| 9 | **Press Play. Watch. Repeat.** | Every session | ∞ — and this is the point |

**Total unavoidable mouse time before first pixel: roughly two hours, most of it installation.** Everything else
on the list is visual judgement, which is not friction — it is the job.

Two things keep it there, and both should be spec decisions rather than accidents:

- **One scene, near-empty.** A single bootstrap GameObject with a single `MonoBehaviour` that builds the view
  from sim state on `Start`. No inspector-wired references, no prefab-per-unit hierarchy, no scene state to
  merge. This falls straight out of Part III's *"the view layer must be a pure function of simulation state"* —
  the determinism rule and the agent-tractability rule are the same rule.
- **No inspector-configured tuning.** Part III already puts tuning in `content/` as versioned data. Enforce it
  and the inspector stops being a place where state hides from git.

---

## 9. Is Unity agent-hostile? — for the pending engine verdict

**Stated plainly: Unity is moderately agent-hostile, the hostility is bounded and front-loaded, and Unity is
actively fixing it.** The specific charges, weighed:

| Charge | Real? | Weight |
|---|---|---|
| Scene/prefab authoring needs the GUI | **No** — everything has an editor API, invocable headless | Low, once you know |
| Scene/prefab YAML is un-hand-editable | **Yes**, by Unity's own statement | Low — you should never want to |
| Scene merges are not resolvable | **Yes** | Low solo, high on a team; avoidable by keeping the scene thin |
| `.meta` files are a footgun for file-moving agents | **Yes** | Medium — one `CLAUDE.md` rule fixes it |
| No compile feedback without a Unity round-trip | **Yes** | **Medium-high** — the largest per-iteration tax; MCP mostly removes it |
| Free tier cannot activate headlessly | **Yes** | Medium — blocks hosted Unity CI, not local automation |
| Batchmode conflicts with an open Editor | **Yes** | Medium — a real workflow annoyance every single day |
| No agent bridge exists | **No longer true** — first-party MCP exists, plus mature community ones | Was the biggest charge; now the weakest |
| The *good* agent bridge is behind a paywall | **Yes** — $10/mo on Personal, per Unity staff | Low-medium — free alternatives exist and work |
| Node-graph tools are unautomatable | **Yes** | Zero here, if the skeleton avoids Shader Graph |

**The fairness point the ticket asked for.** Against the alternative this developer already ships with — Odin +
raylib — Unity loses the agent-tractability contest outright and it is not close: raylib has no GUI, no licence
gate, no `.meta`, no YAML, no project lock, and a text-only pipeline. But that comparison flatters raylib by
counting only the code. What Unity sells is the *asset* pipeline: skinned mesh import, humanoid retargeting,
animator state machines, material/renderer plumbing. In raylib you would build that yourself — and building it
is exactly the kind of work an agent *can* do, so the comparison is closer than it looks, but the result is that
you spend agent-months rebuilding a pipeline whose finished form is the thing the ticket is trying to learn.

The honest framing: **Unity's mouse cost is concentrated in the tasks where a human was needed anyway (looking
at things), plus about two hours of one-time setup. Unity's agent cost is concentrated in the compile round-trip
and the merge risk, both of which the chosen architecture already suppresses.** That is not a disqualifying
verdict for a slice whose stated purpose is *learning the Unity dev environment*.

**One caveat for the eventual verdict, flagged and not researched here:** Godot's `.tscn` is a documented,
hand-authorable, merge-friendly text format, its editor is scriptable from `--headless --script`, and it has no
licence activation gate at all. If pure agent-tractability were the deciding criterion, Godot is the obvious
suspect and this ticket did not examine it. Whoever writes the Unity-vs-alternatives verdict should.

---

## 10. Confidence and gaps

- **High confidence** — everything cited to `docs.unity3d.com`: serialization modes, UnityYAML limits, `.meta`
  semantics, directory/VCS guidance, CLI arguments, `-nographics` GI restriction, test runner flags and the
  `-quit` incompatibility, Personal-licence activation being Hub-only, the editor scripting APIs, asmdef/manifest
  formats, .NET/C# levels, model format support, and the Unity MCP architecture and tool list.
- **Medium-high confidence, with a caveat worth honouring** — that the first-party MCP server requires a paid
  subscription on Personal. The strongest evidence is a **Unity staff reply** on Unity Discussions (*"as of
  right now, a subscription is required for an MCP connection"*, 20 May 2026) corroborated by the Personal
  column of the unity.com/products comparison table. Against it: a checked *"Unity's MCP access"* bullet in the
  Personal card on the same page. **unity.com 403s automated fetching, so the pricing data was read from the
  page's embedded JSON with a browser user-agent — a human should confirm it in a real browser**, and it is a
  live policy question Unity said it was reviewing, so it may have moved since May.
- **Community sources, flagged** — the project-lock behaviour comes from a Unity *support KB* article rather
  than the manual; the Coplay stewardship claim comes from PocketGamer.biz. The GitHub repository metrics are
  from the GitHub API directly and are primary for what they measure. The staff reply above is a forum post —
  authoritative as to Unity's position, but not a document Unity is bound by.
- **Not verified** — that PlayMode tests run cleanly under `-nographics`. The docs only state that `-nographics`
  blocks GI baking. Assume `-batchmode` alone for PlayMode tests until measured.
- **Not researched** — Godot's comparable surface (§9), and whether Unity 6.3's Build Profiles change the
  `-build` CLI contract versus 6.0.

---

## Recommendations to ticket

Concrete enough to become spec lines or new tickets:

1. **Pin Unity 6.3 LTS (`6000.3`)** — current LTS, supported to December 2027.
2. **Unity project at `client/`, `sim/` consumed as a local UPM package** via a relative `file:` path, with
   `"noEngineReferences": true` on the sim asmdef.
3. **One near-empty scene.** Bootstrap GameObject → one `MonoBehaviour` → view built from sim state at runtime.
   No prefab-per-unit wiring, no inspector-serialized tuning.
4. **`Assets/Editor/` is a first-class source directory.** Every setup act gets an idempotent static method,
   `[MenuItem]`-decorated for humans and `-executeMethod`-invocable for the agent and CI.
5. **`.gitignore` lives in `client/`**, derived from github/gitignore's Unity template, with the `Build`
   anchoring reviewed. No LFS yet.
6. **A `CLAUDE.md` rule:** never move, rename or delete an asset without its `.meta`; never hand-edit
   `.unity`/`.prefab`/`.asset`/`ProjectSettings` YAML; never gitignore `*.meta`.
7. **Use the MIT community MCP bridge, not the first-party one** — the latter costs $10/month on Personal
   (§6). **Let nothing depend on either:** every automation must also have an `-executeMethod` entry point.
   Revisit the $10 if the compile round-trip turns out to be the real bottleneck.
8. **Assume no hosted Unity CI on the free tier.** `sim/` gets the GitHub Actions determinism matrix; Unity
   builds and tests run locally.
9. **No GI baking, no Shader Graph, no VFX Graph in the skeleton.** Each converts an automatable step into a
   mouse-only one; none earns its place at this stage.

---

## Sources

All Unity documentation is the Unity 6 manual (`6000.0`–`6000.4` as linked; the manual is versioned per release
and paths shift between them).

1. [UnityYAML](https://docs.unity3d.com/6000.0/Documentation/Manual/UnityYAML.html) — supported/unsupported YAML
   subset; *"You cannot externally produce or edit UnityYAML files."*
2. [Format of text serialized files](https://docs.unity3d.com/Manual/FormatDescription.html) — `!u!` class IDs,
   fileIDs, hex float encoding.
3. [Editor settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-EditorManager.html) — Asset
   Serialization Mode; Force Text as default; Reduce version control noise.
4. [Asset metadata](https://docs.unity3d.com/6000.2/Documentation/Manual/AssetMetadata.html) — `.meta` creation,
   contents, and what breaks when they are separated from their asset.
5. [Refreshing the Asset Database](https://docs.unity3d.com/Manual/AssetDatabaseRefreshing.html) — auto-detection
   of on-disk changes; when `AssetDatabase.Refresh` is required.
6. [Default project directories](https://docs.unity3d.com/6000.0/Documentation/Manual/default-directories.html) —
   which folders to commit and which to exclude.
7. [Version control settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-VersionControlSettings.html)
   — Visible Meta Files as the git-appropriate mode.
8. [github/gitignore — `Unity.gitignore`](https://github.com/github/gitignore/blob/main/Unity.gitignore) — the
   de-facto template (community-maintained, GitHub-published).
9. [Unity Editor command line arguments](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html)
   — `-batchmode`, `-nographics`, `-quit`, `-executeMethod`, `-createProject`, `-build`, `-buildTarget`,
   `-importPackage`, `-logFile`; the GI restriction; the batch mode terms-of-service note.
10. [Unity Test Framework — command line reference](https://docs.unity3d.com/6000.2/Documentation/Manual/test-framework/reference-command-line.html)
    — `-runTests` and friends; *"the Editor's regular `-quit` command-line argument is not supported while tests
    are running."*
11. [Manage your licence through the command line](https://docs.unity3d.com/6000.0/Documentation/Manual/ManagingYourUnityLicense.html)
    and [Licence activation methods](https://docs.unity3d.com/6000.0/Documentation/Manual/LicenseActivationMethods.html)
    — headless activation, and its explicit unavailability for Unity Personal.
12. [MenuItem](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MenuItem.html),
    [PrefabUtility.SaveAsPrefabAsset](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PrefabUtility.SaveAsPrefabAsset.html),
    [ModelImporter](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ModelImporter.html),
    [UnityEditor.Animations.AnimatorController](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animations.AnimatorController.html)
    — the editor scripting surface.
13. [Assembly definition file format](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-file-format.html)
    — asmdef as documented JSON, including `noEngineReferences`.
14. [Project manifest](https://docs.unity3d.com/Manual/upm-manifestPrj.html) and
    [local folder or tarball paths](https://docs.unity3d.com/Manual/upm-localpath.html) — `manifest.json` format
    and `file:` dependencies.
15. [Managed plug-ins](https://docs.unity3d.com/6000.0/Documentation/Manual/plug-ins-managed.html) — the DLL route.
16. [API compatibility levels for .NET](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html)
    and [C# compiler and language version](https://docs.unity3d.com/6000.0/Documentation/Manual/csharp-compiler.html)
    — .NET Standard 2.1 default, C# 9.0.
17. [Model file formats](https://docs.unity3d.com/Manual/3D-formats.html) and
    [Unity glTFast — editor import](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.0/manual/ImportEditor.html)
    — FBX/OBJ/DAE native; glTF via package.
18. [Structure UI with UXML](https://docs.unity3d.com/6000.4/Documentation/Manual/UIE-UXML.html) — UXML/USS as
    hand-authorable text.
19. Unity MCP (first-party, pre-release):
    [overview](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.16/manual/integration/unity-mcp-overview.html),
    [get started](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.16/manual/integration/unity-mcp-get-started.html),
    [`Unity.AI.MCP.Editor.Tools` API](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0/api/Unity.AI.MCP.Editor.Tools.html),
    [Assistant package overview](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0/manual/index.html),
    [About Unity Credits](https://docs.unity.com/en-us/ai/credits/credits-about).
19b. MCP entitlement: [unity.com/products](https://unity.com/products) plan comparison — *Unity AI Concurrent
    MCP Connections*, Personal = "Monthly subscription required", Pro = 3, Enterprise/Industry = 5, Unity AI
    Subscription ($10/mo) = one concurrent MCP worker connection (read from embedded page data, 30 Jul 2026;
    **needs a human to confirm in a browser**); and
    [Unity Discussions — "Request for official clarification: Unity MCP access, subscription requirements, and future policy"](https://discussions.unity.com/t/request-for-official-clarification-unity-mcp-access-subscription-requirements-and-future-policy/1720323),
    Unity staff reply 20 May 2026: *"As of right now, a subscription is required for an MCP connection."*
20. **Community sources, flagged as such:**
    [Unity support KB on the project lock file](https://support.unity.com/hc/en-us/articles/40828087523092-Resolving-the-The-project-is-currently-open-in-the-Unity-Editor-Please-close-it-in-the-Editor-to-proceed-with-this-operation-Error)
    (Unity-published, but a support article rather than the manual);
    [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp),
    [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP),
    [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) — repository metrics read from the
    GitHub API on 30 July 2026;
    [PocketGamer.biz on Coplay's stewardship of MCP for Unity](https://www.pocketgamer.biz/coplay-takes-over-unity-mcp-as-it-reaches-key-milestones-with-public-beta-launch/).
21. [Unity 6.3 LTS is now available](https://unity.com/blog/unity-6-3-lts-is-now-available) — current LTS,
    supported to December 2027.
