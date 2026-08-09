# Unity 6 Project-Creation Settings: What Is Expensive To Change Later

**Research note** · 30 July 2026 · resolves [#6](https://github.com/ssalter21/tower-defense-game/issues/6)

**Question:** which Unity 6 project-creation settings are expensive to change later, and what should this
project pick?
**Inputs:** [Part III §4](../archive/tech-stack-assessment.md#4-client-engine) (Unity 6 client, the rendering rule),
Part IV §5 and §8 (stylized low-poly 3D, CC0 packs, fixed three-quarter camera, Shuriken particles).

---

## Recommendation

**Unity 6.3 LTS · Universal 3D template · Linear · Input System package · Personal.** Then five minutes in
`ProjectSettings` before the first asset is imported.

The reframe that makes this a short answer: **the New Project dialog only has two fields that are expensive to
get wrong** — the Editor version and the template. Everything else the ticket asks about lives in
`ProjectSettings` after the project exists, and almost all of it is free or merely annoying to change. The
dialog's own fields are Editor version, template, project name, location, and optional Unity Cloud / version
control connection [[13](#s13)]; the last three are free.

| The dialog | Pick | Reversibility |
|---|---|---|
| Editor version | **Unity 6.3 LTS**, latest patch (6000.3.21f1 as of 29 Jul 2026) | **Effectively permanent** downward — forward upgrades are routine, downgrades are not supported |
| Template | **Universal 3D** (not "Universal 3D sample" — it ships example content) | **Effectively permanent** in practice: this is the render-pipeline choice |
| Project name | `TowerDefense` or similar | Free (but see Product Name, below) |
| Location | wherever Part III's `client/` lands | Free — Unity projects are relocatable |
| Connect to Unity Cloud / Unity Version Control | **off** | Free |

| First pass through `ProjectSettings` | Set to | Reversibility |
|---|---|---|
| Player → Other → Rendering → **Color Space** | **Linear** (confirm; the URP template should already have set it) | Annoying — the dropdown is free, the re-tuning of everything you judged by eye is not |
| Player → Other → Configuration → **Active Input Handling** | **Input System Package (New)** | **Free** — there is literally a `Both` option |
| Player → Other → Configuration → **Api Compatibility Level** | **.NET Standard** (the default) | Annoying, one-way in practice |
| Player → Other → Configuration → **Scripting Backend** | **Mono** for the skeleton | Free — per-platform build setting |
| Player → **Splash Image** → Show Splash Screen | your call; it is genuinely unlocked on Personal now | Free |
| Editor → **Asset Serialization** | **Force Text** (already the default) | Annoying — flipping later re-serializes the whole project |
| Version Control → **Mode** | **Visible Meta Files** (already the default) | Annoying — same reason |
| Player → **Company Name / Product Name** | set once, deliberately | Annoying — they are baked into `persistentDataPath` |

---

## The reversibility ladder

The ticket's actual demand. Three tiers, and only the top one deserves an hour of reading.

**Effectively permanent.** Editor version (downward). Render pipeline, in the honest sense: reversible on
paper, and nobody does it.

**Annoying.** Colour space. API compatibility level. Asset serialization and version-control mode. Company /
Product name. Each of these is one control; the expense is entirely in the work already built on top of the old
value.

**Free.** Input system. Scripting backend. Texture compression. Project name, location, cloud connection.
Graphics API. Quality levels. Physics and timestep — and for this project those are not merely free but
irrelevant, because Part III forbids the simulation from touching engine physics or `deltaTime` at all.

---

## 1. Render pipeline — the only genuinely costly field in the dialog

### Unity's own guidance, verbatim

> "It's important to choose the right Unity render pipeline for your project when you're early in
> development. … It can be very time-consuming to switch a project from one render pipeline to another,
> especially if the project is far along in development. Different render pipelines use different shaders and
> might not have the same features." [[1](#s1)]

Also, plainly: "You can't use the Universal Render Pipeline and the High Definition Render Pipeline (HDRP) at
the same time." [[1](#s1)]

### The 2026 decision has already been made for you

URP is the conventional answer for stylized low-poly with a fixed camera. It is now more than conventional —
Unity has published a render-pipeline strategy that removes the choice [[2](#s2)]:

- **URP is where all the investment goes.** "URP is the render pipeline used for the vast majority of Unity
  games released in the past three years."
- **The Built-In Render Pipeline is being deprecated,** starting in Unity 6.5: "we strictly do not recommend it
  for any new titles." It is guaranteed available through Unity 6.7 LTS (due end of 2026), removal date
  undecided. Unity is also "converting all education and Asset Store content to be compatible by default with
  URP."
- **HDRP is in maintenance.** "While no new features are planned for HDRP," the only active work is Nintendo
  Switch 2 support and stability.

HDRP was never a candidate — it targets "photorealism and high-fidelity rendering on high-end platforms"
[[1](#s1)] and it is the wrong economics for flat gradient-atlas art. Built-In was the only real alternative and
Unity has now told new projects not to use it. **URP. Not a judgement call any more.**

### What the CC0 packs actually ship — the material-conversion risk is zero here

This is the part worth checking rather than assuming, because the standard warning about pipeline choice is
"your bought materials will render magenta." That warning does not apply to the packs Part IV names, because
none of them ship Unity materials at all:

| Pack | What it ships | Pipeline-bound? |
|---|---|---|
| KayKit (Adventurers, Skeletons) | `.FBX` and `.GLTF`, one 1024×1024 gradient atlas, CC0. "The files are compatible with pretty much any 3D game engine on the market (including Unity, Godot, Unreal Engine, Roblox, and more)." [[3](#s3)] | **No** |
| Quaternius Ultimate Monsters | FBX, OBJ, Blend, glTF, CC0, 50 animated monsters [[4](#s4)] | **No** |
| Kenney Particle Pack | 80 files, CC0, sprites/shaders [[5](#s5)] | **No** |

A raw mesh file has no `.mat` asset in it. Unity generates materials on import — and when a model arrives with
no material assigned, "it uses the Unity diffuse material," with Material Creation Mode defaulting to `Standard
(Legacy)` [[6](#s6)]. Whatever the active pipeline is, the importer produces materials for *that* pipeline. The
conversion liability people warn about belongs to Asset Store packages authored against Built-In shaders —
Synty, which Part IV defers past the step-3 gate anyway.

**So the honest cost of getting this wrong is lower than the folklore suggests, but it is not zero:** it is
every material you hand-authored, every Shader Graph, every post-processing volume, every light's intensity, and
every camera setup. The `Window > Rendering > Render Pipeline Converter` handles read-only material references,
prebuilt shaders and quality settings, and explicitly **"doesn't support converting custom shaders"** [[7](#s7)].
Unity's own warning on the conversion: *"The following task overwrites several files in your project folder.
These can't be restored after Unity overwrites them"* [[7](#s7)], and on material upgrade specifically,
*"Back up your Built-in Render Pipeline material assets before proceeding. This conversion modifies materials
and cannot be easily undone"* [[8](#s8)]. Unmigrated materials render bright pink [[8](#s8)].

There is a converter Built-In → URP. There is no converter URP → Built-In. **Verdict: effectively permanent.**

### Shuriken under URP — Part IV's dependency holds

Part IV §8 commits to the built-in Particle System over VFX Graph, and flagged that it could not verify the
requirement because Unity's pages returned 403/404. Confirmed now from Unity's own feature-comparison table
[[9](#s9)]:

| | Built-In | URP | HDRP |
|---|---|---|---|
| **CPU Particles (Shuriken)** | Yes | **Yes** | Yes (GPU instancing unsupported) |
| GPU Particles (VFX Graph) | **No** | Yes — requires compute-capable hardware, no OpenGL ES | Yes — requires compute |
| Linear colour space | Yes (needs OpenGL ES 3.0) | Yes | Yes |
| Gamma colour space | Yes | Yes | **No** |

Shuriken is fully supported under URP and gets dedicated shaders — **Particles Lit**, **Particles Simple Lit**
and **Particles Unlit** [[10](#s10)]. For flat gradient-atlas VFX re-tinted from one palette (Part IV's
"cheapest coherence win"), **Particles Unlit** is the right default: "for particles that don't need lighting …
optimal for lower-end hardware because there are no time-consuming lighting calculations or lookups"
[[10](#s10)].

Note the row that quietly kills the alternative: VFX Graph is **not** supported under Built-In at all. Even if
you wanted the GPU path later, URP is the pipeline that keeps it open.

### Universal 3D, not Universal 2D

The Hub offers **"Universal 2D"** ("URP is pre-configured with 2D renderer"), **"Universal 3D"** ("URP is
pre-configured with 3D renderer") and a **"Universal 3D sample"** that ships demo content [[11](#s11)]. Take the
blank **Universal 3D**.

The map still has "top-down grid vs side-on lane" open. That question does not touch this one: Part IV §7 already
decided 3D over 2D on the facings multiplier and depth sorting, and a side-on lane in 3D is a camera transform,
not a different renderer. 3D covers both perspectives; the 2D renderer covers only one.

---

## 2. Colour space — annoying, and only if you switch late

Set at **Player → Other Settings → Rendering → Color Space**, options Linear and Gamma [[12](#s12)].

Unity's position is not neutral: "Working in linear color space gives more accurate rendering than working in
gamma color space" [[14](#s14)], and gamma exists because "on some platforms the hardware only supports the
gamma format" [[15](#s15)]. Desktop is not one of those platforms. URP supports both; HDRP supports only linear
[[9](#s9)].

**The cost of a late switch.** The setting is one dropdown, and Unity's scripting reference notes only that
"changing the project color space may cause a reimport of some assets" [[16](#s16)]. That understates it for a
project with art in it. Under linear, Unity assumes textures were authored in gamma space and uses the GPU's
sRGB sampler to convert them, shader maths happens in linear, and gamma correction is reapplied at output
[[14](#s14)]. Under gamma, none of that happens: textures stay in gamma, calculations happen in gamma, and the
framebuffer does not re-correct [[15](#s15)]. Every blend, every falloff, every light intensity and every
particle alpha ramp lands somewhere different. Baked lightmaps must be re-baked.

Nothing *breaks*. What breaks is every judgement you made by eye — which, for a project whose whole art
direction is flat colour from a gradient atlas, is the entire art direction.

**Pick Linear at creation and never think about it again.** The Universal 3D template is expected to have set
it already; this is a 10-second confirmation on first launch, not a task. (See *What I could not verify*.)

---

## 3. Input system — free, genuinely

The most over-worried decision in the dialog. It isn't even in the dialog.

**Player → Other Settings → Configuration → Active Input Handling** takes three values: `Input Manager (Old)`,
`Input System Package (New)`, and **`Both`** [[17](#s17)]. Changing it requires an Editor restart, and with
`Both` selected the C# defines `ENABLE_INPUT_SYSTEM=1` and `ENABLE_LEGACY_INPUT_MANAGER=1` are simultaneously
active [[17](#s17)]. A project that has chosen wrong flips a dropdown and restarts. The real cost is rewriting
whatever input code already exists — which, for this game, is close to nothing.

**Take the Input System package.** Three reasons, in order of weight:

1. **The interaction surface is grid placement plus replay transport** — a pointer position, a click, and a
   handful of transport commands (play/pause, scrub, 2×/4×, instant-resolve). Under the Input System those are
   an `InputActionAsset`: data, versioned in the repo, rebindable without touching code. That is the same
   instinct Part III applies to tuning ("tuning lives as versioned data rather than constants"), applied to
   input.
2. **Unity 6 templates ship the package** in the manifest already, so "installing it" is not work.
3. Gamepad or rebinding, if either ever matters, is free rather than a rewrite.

The honest counter-argument: `Input.GetMouseButtonDown(0)` plus a raycast is three lines and the new system has
a real learning curve for someone who has never opened Unity. If that curve costs a session, set `Both` and move
on — the escape hatch is free by construction.

**The rule that matters more than the choice.** Neither input system may be read by the simulation. Input is
captured in the view layer, converted to a sim command stamped with a tick, and fed in. Part III's rendering
rule and Part II's determinism requirement both die the moment a raycast result reaches sim code.

---

## 4. Version and release channel — install 6.3 LTS

Unity 6 abolished the Tech Stream. There are now two channels [[18](#s18)]:

- **LTS** — "Released once a year. Supported for two years. Additional year of support for Unity Enterprise and
  Unity Industry users."
- **Supported / Update releases** — "Multiple Update releases a year. **Supported until the next release is
  published.** Same level of support as LTS with weekly patches. Fully production-ready with same QA testing as
  LTS." Unity is explicit that these are not the old Tech Streams: "All Update releases undergo the same
  rigorous quality assurance and stability testing as our LTS releases … unlike previous Tech Stream releases,
  which were primarily for early testing of new features."

The state of the family as of 30 July 2026 [[18](#s18)][[19](#s19)]:

| Release | Version | Released | Supported until |
|---|---|---|---|
| Unity 6.0 **LTS** | 6000.0.80f1 | Oct 2024 | **16 Oct 2026** — 10 weeks away |
| Unity 6.1 | 6000.1.17f1 | Apr 2025 | superseded Aug 2025 |
| Unity 6.2 | 6000.2.15f1 | Aug 2025 | superseded Dec 2025 |
| Unity 6.3 **LTS** | **6000.3.21f1** | 4 Dec 2025 | **4 Dec 2027** (2028 for Enterprise/Industry) |
| Unity 6.4 | 6000.4.12f1 | 18 Mar 2026 | superseded 17 Jun 2026 |
| Unity 6.5 | 6000.5.6f1 | 15 Jun 2026 | until 6.6 publishes |
| Unity 6.7 **LTS** | — | "due at the end of the year" [[2](#s2)] | — |

**Install Unity 6.3 LTS at the latest patch.** Reasoning:

- 6.0 LTS is the version most tutorials target and it dies in ten weeks. Do not start there.
- Update releases lapse the moment the next one ships — 6.4 was supported for three months. This project will
  sit unattended between sessions; a channel that expires on someone else's schedule is the wrong channel for
  it, whatever Unity's QA claims.
- 6.3 LTS buys 17 months of support with no action required, and 6.7 LTS arrives around December 2026 as an
  in-family upgrade Unity says it deliberately made cheap: "We have prioritized easier upgrades between Unity 6
  releases" [[18](#s18)].

One footnote, and it is why the version choice is on the "permanent" tier: **upgrades are one-way.** Unity's
upgrade guidance is a backup warning — "it's crucial that you back up your project files … A backup ensures that
you can revert to the previous version of your project if you have any issues" — with the recommended practice
being sequential upgrades, one major version at a time [[20](#s20)]. There is no downgrade path documented,
because there isn't one; the recorded project version and the asset re-serialization that comes with an upgrade
are not undone by pointing an older Editor at the folder. Git is the revert mechanism. This is a strong argument
for having the repo and `.gitignore` correct *before* the first Editor launch, not after.

---

## 5. Licence — all three of Part III's assertions still hold

Part III asserted, as of mid-2026: Personal free below $200,000 in revenue *and* funding; splash screen optional
in Unity 6; Runtime Fee cancelled. Verified against Unity's own live pages:

**Runtime Fee: cancelled, and stayed cancelled.** Unity's announcement is still published and unamended: "we've
made the decision to cancel the Runtime Fee for our games customers, effective immediately … we're reverting to
our existing seat-based subscription model for all gaming customers, including those who adopt Unity 6"
(Matt Bromberg, 12 Sep 2024) [[21](#s21)]. No successor scheme appears anywhere on the current pricing pages
[[22](#s22)]. ✔ Confirmed.

**Personal: free, $200K ceiling on revenue *and* funding.** From the Personal product page: "Unity Personal is
for individuals and small organizations with less than $200K USD of revenue and funds raised in the last 12
months" [[23](#s23)]. The pricing FAQ gives the precise form: small businesses qualify "if their aggregate gross
revenue and funding are less than $200K USD"; individuals and hobbyists qualify "if the amount generated in
connection with their use of Unity is less than $200K USD"; and if you are providing services to clients, it is
*your clients'* aggregate revenue that is tested [[22](#s22)]. Pro is required above $200K, Enterprise above
$25M [[22](#s22)]. ✔ Confirmed — and note the conjunction is real: funding counts even at zero revenue.

**Splash screen: optional on Personal.** Three independent confirmations, since this is the one people get
wrong. (a) The cancellation post promised it: "The Made with Unity splash screen will become optional for Unity
Personal games made with Unity 6" [[21](#s21)]. (b) The current plan-comparison table lists **"Splash screen
customization"** with a check mark in the **Unity Personal** column [[22](#s22)]. (c) The Personal product page
sells it as a Personal feature: "Customize the splash screen — You're in control" [[23](#s23)]. And the Unity 6.3
Player-settings page for **Show Splash Screen** now documents it as plain "Enable or disable the splash screen"
with **no licence caveat** [[24](#s24)] — the Personal restriction that older versions of that page carried is
gone. ✔ Confirmed.

**What has changed since Part III was written.** Pro is now **$210/month, from $2,310.00/yr** [[22](#s22)],
up from the $2,200 named in the cancellation post [[21](#s21)] — consistent with the ~5% January 2026 rise Part
III recorded. Irrelevant to this project, which is on Personal, but it means the Part III figure is stale.

**A lead worth its own ticket, not a claim.** The Personal tier's "What's included" list now names **"Unity's
MCP access"** and **"Command Line Interface access"** [[22](#s22)]. Given the map's binding constraint — Claude
Code cannot drive Unity's editor GUI, so scene authoring and prefab wiring are mouse acts — a first-party MCP
server for the Editor would change what "hands-on" means in the spec. Do not act on this yet: the same page
attaches "Monthly subscription required" to the adjacent Unity AI rows, and the "Unity AI Concurrent MCP
Connections" row shows counts only for the paid tiers. Whether Personal's MCP access is real, free, and useful
for authoring is exactly the question [#10](https://github.com/ssalter21/tower-defense-game/issues/10) should
answer with a browser.

---

## 6. The rest of the first ten minutes

Everything below is in `ProjectSettings`, not the dialog. Ordered by how much it would hurt to change at
step 4.

**Api Compatibility Level — annoying, one-way in practice.** Player → Other → Configuration. Default is **.NET
Standard**, and Unity recommends it for new projects: "a smaller API surface that reduces the size of your final
executable file, and it has better cross-platform support so your code is more likely to work across all
platforms" [[25](#s25)]. The .NET Framework option is .NET Framework 4.8 plus additions [[25](#s25)]. **Keep the
default.** This is load-bearing for Part III's architecture: the sim library must compile once and be consumed
unchanged by the Unity client, an ASP.NET service and a headless CLI, which means targeting `netstandard2.1`.
The reason it is one-way is social, not technical — widening to .NET Framework is a dropdown, but the code
written against the wider surface will not compile back down.

**Scripting Backend — free.** Mono is "a stable, mature .NET runtime that provides a managed environment for
the just-in-time (JIT) compilation of your C# code"; IL2CPP is "Unity's ahead-of-time (AOT) pipeline that
converts C# intermediate language (IL) to C++, then compiles to native code. It's required on several platforms
where Mono and JIT are not supported" [[26](#s26)]. It is a per-platform build setting; flipping it costs a
rebuild. **Mono for the skeleton** — faster iteration, and the walking skeleton's deliverable includes a
double-clickable Windows build, which Mono produces fine. Worth knowing for later: IL2CPP's AOT constraints bite
reflection-based serialization, which Part III has already banned.

**Asset Serialization = Force Text — annoying.** Editor settings. Already the default: Force Text "Convert all
assets to Text mode, including new assets. This is the default option" [[27](#s27)]. Leave it. Flipping it later
re-serializes every asset in the project — one commit with a diff nobody can read.

**Version Control Mode = Visible Meta Files — annoying.** Already the default [[28](#s28)]. `.meta` files carry
the GUIDs that connect every reference between assets; they must be committed. Same re-write cost if changed
late. This is the setting the map's open "Unity + git hygiene" thread depends on, and the good news is that
Unity 6's defaults are already the git-correct ones.

**Company Name / Product Name — annoying, and nobody expects it.** Player settings. `Application.persistentDataPath`
is literally `%userprofile%\AppData\LocalLow\<companyname>\<productname>` on Windows and the equivalent
elsewhere [[29](#s29)]. Change either later and every saved file, PlayerPrefs entry and — relevant here — every
locally stored ghost record moves to a new folder and appears to have vanished. Set them once at creation.

**Texture compression defaults — free.** A per-platform build setting with per-texture overrides; changing it
re-compresses on reimport and costs only time. Ignore it for a desktop skeleton.

**Graphics API, quality levels, URP renderer (Forward / Forward+ / Deferred) — free.** All are asset or
per-platform settings; the URP Asset can be edited or replaced at any point.

**Physics and Time settings — free, and irrelevant.** Not a Unity question for this project. Part III forbids
engine physics, engine transforms and `deltaTime` inside the simulation; the sim owns its own integer tick, and
`Time.fixedDeltaTime` must never become the thing that advances it.

---

## What I could not verify

- **That the Universal 3D template sets Color Space = Linear.** Unity does not document per-template default
  settings anywhere I could reach, and the template package is not published as a readable page. Every
  secondary source says Unity 6's URP templates are Linear, and it would be surprising otherwise given HDRP
  cannot even run in gamma [[9](#s9)]. **Confirm it in Player settings on first launch** — it is a ten-second
  check and it is the one "annoying"-tier setting that a wrong template default would silently hand you.
- **That the Universal 3D template pre-installs the Input System package.** Same problem: widely stated,
  including for the 6.3 template manifest, but not on a first-party page I could fetch. It changes nothing —
  if the package is absent, Package Manager installs it and offers the restart [[17](#s17)].
- **`unity.com` blocks automated fetching** (HTTP 403 to a plain request), as Part IV §12 also found. All
  `unity.com` citations here were retrieved with a browser user-agent and read as extracted text, and the
  plan-comparison check marks were read out of the page's raw markup rather than rendered. The claims are
  Unity's own words from Unity's own live pages, but a human should sanity-check the licence section in a
  browser before relying on it commercially.
- **The Unity Asset Store EULA** — still 403, still unread, still irrelevant to this ticket because the three
  packs Part IV names are CC0 from itch.io, quaternius.com and kenney.nl, none of which route through the Asset
  Store.
- **The Unity Hub "New project" dialog's exact current layout.** The Hub documentation page
  (`docs.unity.com/en-us/hub/project-create`) is real and describes the fields, but the Hub ships independently
  of the Editor and its UI moves. Treat the dialog table above as "these fields exist," not "in this order."
- **Whether Unity 6.7 LTS's exact date is fixed.** Unity says "due at the end of the year" [[2](#s2)] and gives
  no date. The 6.3 → 6.7 upgrade is a plan, not a commitment.

---

## Sources

Retrieved 30 July 2026.

1. <a id="s1"></a>[Unity Manual 6.3 — Choose a render pipeline](https://docs.unity3d.com/6000.3/Documentation/Manual/choose-a-render-pipeline.html)
2. <a id="s2"></a>[Unity — Render Pipelines strategy for 2026](https://unity.com/topics/render-pipelines-strategy-for-2026)
3. <a id="s3"></a>[KayKit Character Pack: Adventurers](https://kaylousberg.itch.io/kaykit-adventurers) — formats, gradient atlas, CC0
4. <a id="s4"></a>[Quaternius — Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html)
5. <a id="s5"></a>[Kenney — Particle Pack](https://kenney.nl/assets/particle-pack)
6. <a id="s6"></a>[Unity Manual 6.3 — Model Importer, Materials tab](https://docs.unity3d.com/6000.3/Documentation/Manual/FBXImporter-Materials.html)
7. <a id="s7"></a>[Unity Manual 6.4 — Convert assets and quality levels from the Built-In Render Pipeline to URP](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/convert-assets-to-urp.html) and [Render Pipeline Converter window reference](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/features/rp-converter.html)
8. <a id="s8"></a>[Unity Manual 6.3 — Upgrade material assets to a Scriptable Render Pipeline](https://docs.unity3d.com/6000.3/Documentation/Manual/upgrade-material.html)
9. <a id="s9"></a>[Unity Manual 6.3 — Render pipeline feature comparison](https://docs.unity3d.com/6000.3/Documentation/Manual/render-pipelines-feature-comparison.html)
10. <a id="s10"></a>[Unity Manual 6.3 — Particles Unlit shader for URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/particles-unlit-shader.html), [Particles Simple Lit](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/particles-simple-lit-shader.html), [Choose a prebuilt shader in URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/shaders-in-universalrp-choose.html)
11. <a id="s11"></a>[Unity Manual 6.3 — Create a new project that uses URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/creating-a-new-project-with-urp.html)
12. <a id="s12"></a>[Unity Manual 6.3 — Set a project's color space](https://docs.unity3d.com/6000.3/Documentation/Manual/set-project-color-space.html)
13. <a id="s13"></a>[Unity Hub docs — Create a new project](https://docs.unity.com/en-us/hub/project-create)
14. <a id="s14"></a>[Unity Manual 6.3 — Introduction to linear color space](https://docs.unity3d.com/6000.3/Documentation/Manual/linear-color-space.html)
15. <a id="s15"></a>[Unity Manual 6.3 — Gamma color space](https://docs.unity3d.com/6000.3/Documentation/Manual/gamma-color-space.html)
16. <a id="s16"></a>[Unity Scripting API 6.3 — PlayerSettings.colorSpace](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerSettings-colorSpace.html)
17. <a id="s17"></a>[Input System package — Installation guide](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/Installation.html)
18. <a id="s18"></a>[Unity — Unity 6 release support](https://unity.com/releases/unity-6/support) and [Unity 6](https://unity.com/releases/unity-6)
19. <a id="s19"></a>[endoflife.date — Unity](https://endoflife.date/unity) — patch numbers and exact dates. Aggregator, not first-party; the support-window claims it repeats are independently confirmed by [[18](#s18)].
20. <a id="s20"></a>[Unity Manual 6.3 — Upgrade your Unity project](https://docs.unity3d.com/6000.3/Documentation/Manual/upgrade-project.html)
21. <a id="s21"></a>[Unity blog — "Unity is canceling the Runtime Fee"](https://unity.com/blog/unity-is-canceling-the-runtime-fee), Matt Bromberg, 12 Sep 2024
22. <a id="s22"></a>[Unity — Compare plans and pricing](https://unity.com/products/compare-plans)
23. <a id="s23"></a>[Unity — Unity Personal](https://unity.com/products/unity-personal)
24. <a id="s24"></a>[Unity Manual 6.3 — Splash Image Player settings](https://docs.unity3d.com/6000.3/Documentation/Manual/class-PlayerSettingsSplashScreen.html)
25. <a id="s25"></a>[Unity Manual — API compatibility levels for .NET](https://docs.unity3d.com/6000.5/Documentation/Manual/dotnet-profile-support.html)
26. <a id="s26"></a>[Unity Manual 6.3 — Scripting backends](https://docs.unity3d.com/6000.3/Documentation/Manual/scripting-backends.html)
27. <a id="s27"></a>[Unity Manual 6.3 — Editor settings](https://docs.unity3d.com/6000.3/Documentation/Manual/class-EditorManager.html)
28. <a id="s28"></a>[Unity Manual — Version Control settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-VersionControlSettings.html)
29. <a id="s29"></a>[Unity Scripting API 6.3 — Application.persistentDataPath](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html)
