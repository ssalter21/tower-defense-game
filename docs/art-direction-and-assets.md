# Art Direction & Asset Pack Strategy

**Part IV of IV** · 30 July 2026

**Subject:** Async ghost round-robin tower defense
**Aesthetic north stars under debate:** Legion TD 2 (stylized 3D) vs. Noita (pixel)
**Input:** [Technology Stack Assessment](tech-stack-assessment.md) (Part III), which chose the stack *assuming* the
Legion TD 2 look and explicitly ruled out "2D-first engines" on that basis. This document re-opens that
assumption and then closes it.

---

## Recommendation

**Stay 3D. Buy [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete) for $150 today. Do not buy
Synty yet.**

The single most important fact in this entire document, and the one that inverts the obvious answer:
**Synty POLYGON packs do not include animations.** Synty says so itself — "most Synty art asset packs (i.e.
Simple and POLYGON) do not include animations" ([Synty FAQ](https://syntystore.com/community/faq)) — and every
individual fantasy pack listing repeats it verbatim: "Characters set up with Mecanim (no animations included)"
([Fantasy Kingdom](https://syntystore.com/products/polygon-fantasy-kingdom),
[Dungeon Pack](https://syntystore.com/products/polygon-dungeon-pack),
[Fantasy Rivals](https://syntystore.com/products/polygon-fantasy-rivals-pack),
[Dark Fortress](https://syntystore.com/products/polygon-dark-fortress)).

For most genres that is a footnote. For a tower defense it is the whole problem. A TD unit is *nothing but*
animation — idle, walk, attack, hit-react, die, on loop, forty times over, watched from a fixed camera for the
entire match. Buying Synty first converts an art problem you can solve with money into a Blender problem you
solve with months. Mixamo covers the humanoids; nothing covers a troll, a rock golem, a medusa or a behemoth.

KayKit is the only shortlisted option that satisfies all four requirements at once — **one artist's coherent
style, animations included, a roster large enough for a whole TD, and raw engine-agnostic formats** — and it
costs $150 once, forever, including every future pack, under CC0.

| Decision | Verdict |
|---|---|
| **Art direction** | Stylized low-poly 3D, flat/gradient-atlas textures, fixed three-quarter camera. Not pixel art. |
| **Day-one purchase** | [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete) — **$150**, CC0, ~57 rigged and animated characters across 5 character packs, plus dungeon and hex environment kits, plus `.blend` sources, plus all future packs. |
| **Day-one free supplements** | [Kenney](https://kenney.nl/assets) (CC0) for prototype UI and blockout; [Quaternius Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html) (CC0, 50 animated monsters) as an explicitly-temporary stand-in for non-humanoid creeps. **$0.** |
| **Deferred purchase (only after Part II step 3 passes)** | Synty, for the things that do not move — environment, towers, UI. [POLYGON Dungeon Pack](https://syntystore.com/products/polygon-dungeon-pack) $149.99 + [INTERFACE Fantasy Menus](https://syntystore.com/products/interface-fantasy-menus) $79.99, or a Humble bundle at ~$30 if one recurs. |
| **Total to a playable, coherent, non-embarrassing build** | **$150.** Total to a vertical slice: **~$400.** |
| **Does the Part III stack verdict survive?** | **Yes — but one of its two supporting arguments has expired.** See §2. |

The 2D question deserves a plain answer rather than a hedge: **2D is not faster here, it is roughly eight times
slower**, and the reason is arithmetic rather than taste. See [§7](#7-why-3d-wins-and-its-not-about-taste).

---

## 1. What Part III assumed, and what has to be re-examined

Part III's requirements table contains this row:

> | Legion TD 2 aesthetic | Stylized 3D, ~40–60 skinned units, ability VFX, dense UI | Out: 2D-first engines, thin-3D frameworks |

That is a conditional. It rules out 2D engines *because the art was assumed to be 3D*. Relaxing the art
direction ought therefore to reopen the engine question, and it half does. Three things need re-testing:

1. **Is the Legion TD 2 look actually expensive?** (§3.1 — no, less than expected, but not for the reason
   people assume.)
2. **Is the Noita look actually cheap?** (§3.2 — no. The aesthetic is buyable; the thing that makes Noita
   *look like Noita* is not an aesthetic, it is a physics engine.)
3. **Does the pack ecosystem still justify Unity specifically?** (§2 — much less than it did in Part III.)

---

## 2. The Part III verdict survives, but the Unity argument has weakened

Part III justified Unity honestly and narrowly: *"Not because it is the best engine — because of the asset
ecosystem, which is the actual constraint on a small team reaching this look."* That argument is now
substantially weaker than it was, and intellectual honesty requires saying so:

- **Synty ships Godot projects.** The current [Fantasy Kingdom](https://syntystore.com/products/polygon-fantasy-kingdom)
  and [Dungeon Pack](https://syntystore.com/products/polygon-dungeon-pack) listings both include a "Godot 4.5.1
  project" alongside the Unity package, the Unreal project and raw FBX — though the
  [FAQ](https://syntystore.com/community/faq) still says official support is Unity and Unreal only, and Godot
  is merely "technically possible."
- **Synty's own licence is engine-neutral in writing.** The
  [One-Time Purchase Licence](https://syntystore.com/pages/one-time-purchase-licence) states the licence is
  "not limited by game engine, OS, platform or device."
- **Unity's own support desk says Asset Store assets work in other engines.** Unity's article
  [*Can I use assets from the Asset Store with other engines?*](https://support.unity.com/hc/en-us/articles/34387186019988-Can-I-use-assets-from-the-Asset-Store-with-other-engines)
  answers yes, subject to the ordinary EULA restrictions — no redistribution as standalone items, no product
  designed to let end users extract them.
- **Fab is explicitly engine-agnostic.** Epic's unified marketplace launched with a Standard Licence usable
  "in any game engine or tool," and content categorised for Unity, Unreal, Godot, Blender and others
  ([Epic's launch announcement](https://www.unrealengine.com/en-US/blog/fab-epics-new-unified-content-marketplace-launches-today),
  [Fab licensing docs](https://dev.epicgames.com/documentation/en-us/fab/licenses-and-pricing-in-fab)).
- **The recommended pack is CC0 FBX/GLTF.** KayKit and Quaternius ship raw formats explicitly listed as
  compatible with "Unity, Godot, Unreal Engine, Roblox and other engines"
  ([KayKit Adventurers](https://kaylousberg.itch.io/kaykit-adventurers)).

So "you must use Unity to get the art" is no longer true. What survives is narrower and still decisive:

| Argument for Unity 6 | Status after this research |
|---|---|
| Asset ecosystem lock-in | **Expired.** Assets are portable; licences permit it; publishers ship multi-engine. |
| Mecanim humanoid retargeting | **Holds, and is the load-bearing one.** Every animation shortcut in §5 — Mixamo, Synty ANIMATION, Sidekick, Quaternius' Universal Animation Library — targets a humanoid avatar and depends on retargeting being free and reliable. Synty ANIMATION packs are documented as "designed to work with the Mecanim humanoid avatar" ([Base Locomotion](https://syntystore.com/products/animation-base-locomotion)). |
| Dense UI | **Holds.** Synty's [INTERFACE Fantasy Menus](https://syntystore.com/products/interface-fantasy-menus) is explicitly Unity-only — "Unreal Engine: not supported… Godot: not supported." |
| Same language as the sim | **Holds, unchanged.** |

**Verdict: keep Unity 6.** But amend Part III's table row. The correct statement is not "2D-first engines are
ruled out by the Legion TD 2 aesthetic." It is:

> **2D-first engines are ruled out by unit-count × facings arithmetic, independent of art direction.**

That version is stronger, because it survives a change of taste. See §7.

---

## 3. Grounding the two north stars

### 3.1 Legion TD 2 — the bar is lower than it looks, in exactly one dimension

| Fact | Source |
|---|---|
| Built in Unity by AutoAttack Games; Early Access 20 Nov 2017, 1.0 on 1 Oct 2021 | [Steam store page](https://store.steampowered.com/app/469600/Legion_TD_2__Multiplayer_Tower_Defense/) |
| "Over 100 unique fighter units across 8 distinct legions"; 1–8 players | [Steam store page](https://store.steampowered.com/app/469600/Legion_TD_2__Multiplayer_Tower_Defense/) |
| Team of ~6, of whom **one** is Lead Artist; the previous Art Director held the role 2018–2022 | [Official team page](https://beta.legiontd2.com/team/) |
| Pipeline: ZBrush → 3D Coat / Photoshop hand-painted texturing → Maya animation → PopcornFX VFX | [Sketchfab studio spotlight, 20 Aug 2018](https://sketchfab.com/blogs/community/game-studio-spotlight-autoattack-games) |
| The Art Director worked as concept *and* 3D artist simultaneously, to skip the concept-handoff wait | same |
| Animation was **outsourced**, with the studio supplying "assets, animation descriptions, and references… as detailed as possible about each animation's timing, feel, and use case" | same |
| 144 unit models published publicly | [Sketchfab profile](https://sketchfab.com/autoattackgames) |
| A representative unit — "Nekomata" — is **1,800 triangles / 916 vertices** | [Sketchfab model page](https://sketchfab.com/3d-models/legion-td-2-nekomata-06bb70ef39bb4751bb84bb9808b8fece) |
| Other units on the same profile are listed at 950–3,000 triangles (Antler 950, Harbinger 1.5k, Samurai 2k, Janus 3k) | [Sketchfab profile listing](https://sketchfab.com/autoattackgames/models) |
| A **13-step** per-character pipeline, beginning at "Gameplay ideation — what gameplay purpose will the character serve?" and running through concept art, orthographic turnarounds, high-poly sculpt, low-poly, UV, texturing, animation, VFX, balance and finally skins | [Character Design & "Making of a King"](https://beta.legiontd2.com/updates/character-design-making-of-a-king/) |
| Concept artists are **named, recruited individuals** — e.g. Oğuzalp "Drakhas" Döndüren of Istanbul, introduced in a dedicated dev post showing his original Forsaken designs (Butcher, Fire Archer, Imp, Undead Dragon, Lord of Death) | [Introducing Our New Concept Artist](https://beta.legiontd2.com/updates/introducing-our-new-concept-artist/) |
| Characters are commissioned from concept art turnaround sheets, in a "hand-painted style," by contract 3D character artists hired through industry job boards | [polycount job posting](https://polycount.com/discussion/228439/3d-character-artist-for-legion-td-2-played-by-220-000-players-on-steam) *(403 to fetch — see §12)* |

### Does Legion TD 2 itself use asset packs?

**No. Not one.** This is worth stating outright, because it is the natural follow-up to a document recommending
that you do.

Every published trace of their production points the same way. The pipeline starts at *gameplay ideation* and
reaches a model only at step 5 — the unit exists because the design needs it, which is the exact inverse of
buying a pack and casting your roster from what is in it (§9, failure mode 5). They introduce concept artists
by name in dev posts. They ship a Kickstarter artbook of "never-before-seen sketches of 50 characters." They
post job listings for contract character artists working from their own turnaround sheets.

What they *did* outsource is instructive, because it is the opposite of what an asset pack gives you.
The studio's lead artist Jean Go handled concept, character art, illustration and marketing art, and as
production scaled, **modelling and texturing were progressively contracted out while animation was outsourced
from early on** — to SuperSpline Studios, with the studio supplying "assets, animation descriptions, and
references… as detailed as possible about each animation's timing, feel, and use case"
([Sketchfab studio spotlight](https://sketchfab.com/blogs/community/game-studio-spotlight-autoattack-games)).
They bought *labour*, applied to their own designs. An asset pack sells you *designs*, with the labour already
spent on someone else's.

**So the honest framing of this document's recommendation is not "do what Legion TD 2 did."** It is: Legion TD 2
spent eight years and a dedicated art department building 100+ bespoke units, you are one person, and the pack
is how you get a playable game in front of players in the meantime. Treat the north star as a description of
the *feel to aim at*, not a production plan to copy. The bespoke path is available later, unit by unit, funded
by a game that already exists — which is precisely the order AutoAttack could not use, because in 2016 there
was no game yet either.

**The useful conclusion on budget.** Legion TD 2's units are *smaller* than the assets in most of the packs below.
Nothing about the geometry budget is out of reach — a KayKit or Synty character is in the same order of
magnitude, and a Meshtint "Mega Toon" creature is 505 triangles
([Phantom](https://www.meshtint.com/products/phantom-mega-toon-series)). The polygon count is not the gap.

The gap is two things you cannot buy: **hand-painted texture treatment**, and **100+ bespoke silhouettes
produced over eight years by a dedicated art director**. Part III already flagged this as the risk it is most
confident about, and this research confirms it: keep the polycount low, keep the silhouette unique, and express
detail through texture rather than geometry.

What you *can* buy is a coherent 50-unit roster that reads at TD scale from a fixed camera. That is most of the
felt effect and none of the eight years. **Judgement, not fact:** the specific hand-painted look is worth
deferring indefinitely — it is a shader-and-texture pass over a roster you already have, not a prerequisite for
one.

### 3.2 Noita — the aesthetic is buyable, the thing that matters is not

| Fact | Source |
|---|---|
| Custom in-house engine: the **Falling Everything Engine**, by Nolla Games (Helsinki) | [Noita presskit](https://noitagame.com/press/), [Nolla Games](https://nollagames.com/fallingeverything/) |
| "Every pixel in the world is simulated. Burn, explode or melt anything." | [Noita presskit](https://noitagame.com/press/) |
| Three developers: Petri Purho, Olli Harjola, Arvi Teikari; company formed 2016 | [80.lv interview, 5 Apr 2019](https://80.lv/articles/noita-a-game-based-on-falling-sand-simulation) |
| Early Access 24 Sep 2019; 1.0 on 15 Oct 2020 | [Noita presskit](https://noitagame.com/press/) |
| The simulation is "complex cellular automata"; the world is divided into **64×64 chunks with dirty rects**, updated in **four passes** picking "every other 64×64 chunk," with pixels allowed to move within the chunk plus 32 pixels in each direction | [80.lv interview](https://80.lv/articles/noita-a-game-based-on-falling-sand-simulation) |
| GDC 2019: *Exploring the Tech and Design of 'Noita'*, Petri Purho | [GDC Vault](https://www.gdcvault.com/play/1025695/Exploring-the-Tech-and-Design) |
| Daily Runs share one seed worldwide per 24-hour window, so identical worlds are reproduced from a seed | [Noita Wiki: Daily Run](https://noita.wiki.gg/wiki/Daily_Run) |

**Separate the aesthetic from the tech, because they are not separable in Noita and that is the point.**

Noita's *aesthetic* — low-resolution pixels, murky palette, heavy glow and lighting — is entirely purchasable
as sprite packs. Noita's *appeal* is not that. It is that the visual interest is generated by materials
interacting: oil pooling, catching, spreading up a rope, burning through a wooden platform that then collapses.
Take away the sim and you have a competent, unremarkable pixel roguelite. **You cannot buy the interesting
half.**

**Now the genuinely interesting question this raises for Part II's architecture.** Would falling sand fight the
deterministic-integer sim, or suit it?

*Architecturally, it suits it beautifully.* A falling-sand world is cellular automata over integer cell states
with a fixed update order — no floats, no transcendentals, no physics solver, no `Dictionary` iteration order.
It is, structurally, more deterministic than anything Part III's banned-API list is trying to protect you from.
Noita's shared-seed Daily Runs are indirect evidence that reproducibility from a seed is achievable in that
model. The one caveat is Noita's own multithreading: the four-pass checkerboard chunk update is deterministic
only if the partition and the pass order are fixed — and Part III bans `Task`/`Parallel` inside the sim
outright, so a single-threaded implementation would sidestep the hazard entirely.

*Economically, it is fatal.* Part III's second-biggest payoff, after anti-cheat, is that "a headless match at
~10 ms means 100,000 AI matchups in minutes." A TD sim is ~200 entities. A modest 512×512 falling-sand grid is
262,144 cells you must step every tick. That is three orders of magnitude more work per tick, and it lands
directly on the balance-sweep harness that makes the whole design tractable. You would trade the ability to
evaluate a balance change in ninety seconds for a visual effect.

**Cut it.** Not because it breaks determinism — it doesn't — but because it costs you the one computational
superpower the architecture was built to give you.

---

## 4. The marketplaces, judged on their licences

Read the licence before the screenshots. Every row below was checked against the licensor's own page.

| Source | Licence | Commercial use | Engine-restricted? | The catch |
|---|---|---|---|---|
| [Unity Asset Store](https://assetstore.unity.com/browse/eula-faq) | Standard Unity Asset Store EULA; Single Entity (individuals/small business) or Multi Entity; assets tagged "Extension Asset" or "Restricted Asset" | Yes — assets must be "embedded and integrated into your game… with a substantial amount of original creative work" | **No.** Unity's [support article](https://support.unity.com/hc/en-us/articles/34387186019988-Can-I-use-assets-from-the-Asset-Store-with-other-engines) confirms other engines are permitted | No redistribution as standalone items; nothing that lets end users "extract or download assets separately." **Restricted Assets** carry extra terms (usually open-source components) and must be read individually. |
| [Fab](https://dev.epicgames.com/documentation/en-us/fab/licenses-and-pricing-in-fab) (Epic) | Standard Licence at Personal and Professional tiers, plus Creative Commons options on free content | Yes | **No** — explicitly "any game engine or tool" | Two tiers means you must check which one a listing sells you. |
| [Synty Store](https://syntystore.com/pages/one-time-purchase-licence) | One-time purchase: perpetual, royalty-free, **5 seats** | Yes | **No** — "not limited by game engine, OS, platform or device" | No NFT/blockchain; no "Metaverse-related and/or game creation software"; **no inclusion in datasets used by Generative AI Programs.** Humble Bundle copies are **1 seat**, not 5 ([FAQ](https://syntystore.com/community/faq)). |
| [SyntyPass](https://syntystore.com/products/syntypass) | Subscription: $40/mo (3-month minimum) or **$30/mo billed annually**; 130+ packs; 5 seats | Yes, while active | No | **You are licensed "while your subscription plan is active"** ([licence overview](https://syntystore.com/pages/licences-overview)). This is a rental. See the warning below. |
| [itch.io](https://itch.io/blog/929708/general-paid-asset-license) | **No platform-wide licence.** Each seller picks their own, or none | Varies | Varies | itch.io's own forums note many free assets carry **no licence at all**, in which case you need explicit permission ([thread](https://itch.io/t/402794/warning-for-authors-of-unlicensed-assets)). Read every pack's "More information" block. |
| [Kenney](https://kenney.nl/assets) | **CC0** across the whole library | Yes | No | Attribution optional. Almost nothing is animated. |
| [KayKit](https://kaylousberg.itch.io/kaykit-complete) | **CC0 (Creative Commons Zero v1.0 Universal)**, with a request — "please don't resell unmodified copies or claim them as your own" | Yes | No | The request is a request; CC0 does not legally enforce it. Honour it anyway. |
| [Quaternius](https://quaternius.com/packs/ultimatemonsters.html) | **CC0** | Yes | No | Pay-what-you-want / Patreon-funded. Some packs are years old (Ultimate Monsters: Oct 2022). |
| [CraftPix](https://craftpix.net/file-licenses/) | Royalty-free, unlimited free and commercial projects; **"By the end of your subscription, you can continue to use all the downloaded game assets"** | Yes | No | Cannot resell source files or "slightly modified" versions. **No AI training.** |
| [GameDev Market](https://www.gamedevmarket.net/) | Pro Licence — commercial, editable, no engine limits, no credit required | Yes | No | Direct redistribution prohibited. |
| [OpenGameArt](https://opengameart.org/content/faq) | Mixed: CC0, CC-BY, **CC-BY-SA**, **GPL**, OGA-BY | Yes, with conditions | No | **This is the trap row.** CC-BY-SA forces derivatives under the same licence; GPL may pull your game's code with it. Filter to CC0 only or don't use it. |
| [Poly Haven](https://polyhaven.com/license) | **CC0** | Yes | No | HDRIs, PBR textures, props. **No characters.** Right source for your ground materials and sky, wrong source for creeps. |
| [Mixamo](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html) | Free with an Adobe account; no royalty; ship inside a finished product, don't repackage the raw files | Yes | No | **Bipedal humanoids only**, and effectively unmaintained by Adobe. See §12 — I could not fetch this page directly. |

> ### ⚠ The SyntyPass trap, stated plainly
>
> SyntyPass at $30/month annually looks like the obvious play: 130+ packs, everything you could want, less than
> one POLYGON pack per year. But the [licence overview](https://syntystore.com/pages/licences-overview) draws a
> hard line — one-time purchases give "perpetual rights to use the specific pack(s) you have purchased," while
> the subscription licenses you "for development using any assets available to you **while your subscription
> plan is active**."
>
> Part II's design ships a game whose ghost pool and replays are expected to live for **years**. A licence that
> terminates when you stop paying is structurally mismatched to a product with a long tail, and you would be
> making the payment decision under duress every month for the life of the game. **Use SyntyPass to evaluate;
> buy outright anything that ships.** And see §12 — Synty's public pages do not state what happens to an
> already-shipped title after cancellation, and that is a question to put to them in writing before you rely on
> either answer.

---

## 5. Track A — stylized 3D. The shortlist.

The criterion is not "best art." It is **coherence from one hand, at sufficient roster size, with animations
already in the box.** Mixing character publishers is the classic failure; a KayKit skeleton beside a Synty
skeleton beside a Meshtint slime is how a game reads as assembled rather than made.

| Pack | Publisher | Price (USD) | Characters | Animated? | Rig | Formats | Verified |
|---|---|---:|---:|---|---|---|---|
| **[The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete)** ★ | Kay Lousberg | **$150** (or more) | ~57 across 5 character packs, + 15 environment/prop packs | **Yes — rigged, textured and animated** | Own rig; `.blend` sources included | FBX, GLTF, OBJ, DAE, .blend | 20 packs listed; CC0; all future packs included at no extra cost |
| ↳ [Character Pack: Adventurers](https://kaylousberg.itch.io/kaykit-adventurers) | Kay Lousberg | Free / $7.95 / $11.95 | 8 (Knight, Barbarian, Rogue, Mage, Ranger + Engineer, Druid, Barbarian_Large) | Yes | — | FBX, GLTF | CC0; updated within the last fortnight |
| ↳ [Character Pack: Skeletons](https://kaylousberg.itch.io/kaykit-skeletons) | Kay Lousberg | Free / $7.95 / $11.95 | 6 (Warrior, Rogue, Mage, Minion + Skeleton Golem, Necromancer) | Yes | — | FBX, GLTF | 1024×1024 gradient atlas, downsamples to 128×128 |
| ↳ [Mystery Monthly Series 6](https://kaylousberg.itch.io/kaykit-series-6) | Kay Lousberg | $19.99 | 14 (incl. Orc Brute, The Monstrosity, Plant Warrior, Avian Swordsman) | Yes | — | FBX, GLTF, .blend | Series 4 (15) and 5 (14) are the same deal |
| [Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html) | Quaternius | **Free** (CC0) | 50 monsters | **Yes** — attack, death, run, walk and more | Own | FBX, OBJ, Blend, glTF | Dated Oct 2022 |
| [Ultimate Animated Characters](https://quaternius.com/packs/ultimatedanimatedcharacter.html) | Quaternius | **Free** (CC0) | 52 | Yes | Own | FBX, OBJ, Blend | Dated Nov 2019 |
| [Universal Animation Library 2](https://quaternius.com/packs/universalanimationlibrary2.html) | Quaternius | **Free** (CC0) | — | **130+ animations**: melee/armed combos, parkour, zombie locomotion | "Universal humanoid rig… for retargeting" | FBX, GLB, Blend | Dated Jan 2026 |
| [POLYGON Dungeon Pack](https://syntystore.com/products/polygon-dungeon-pack) | Synty | $149.99 | 16 (Goblin ×6, Skeleton ×4, Ghost ×2, Rock Golem, Tormented Soul, Hero ×2) + 770 assets, 73 weapons | **No** | Mecanim humanoid | Unity 2022.3, Unreal 5.3, **Godot 4.5.1**, FBX | v1.10.1; 4 colourways + 3 skin tones per character |
| [POLYGON Fantasy Rivals](https://syntystore.com/products/polygon-fantasy-rivals-pack) | Synty | $99.99 | 20 (Troll, Big Ork, Medusa, three Golems, Red Demon, Barbarian Giant, Evil God…) | **No** | Mecanim | Unity 2023.3, Unreal 4.25, FBX | Listing notes "a new big rig for massive monsters" |
| [POLYGON Fantasy Kingdom](https://syntystore.com/products/polygon-fantasy-kingdom) | Synty | $349.99 | 22 + 2,100 prefabs (the towers/castle/village content) | **No** | Mecanim | Unity 2022.3, Unreal 5.3, **Godot 4.5.1**, FBX | v1.12.4 |
| [POLYGON MINI Fantasy Characters](https://syntystore.com/products/polygon-mini-fantasy-characters-pack) | Synty | **$39.99** | **60** across fantasy, dungeon, pirate, samurai, viking, western | **No** | Mecanim | Unity 2022.3, Unreal 4.25, FBX | v1.7.0. Chibi proportions. The best characters-per-dollar in the Synty catalogue. |
| [POLYGON Dark Fortress](https://syntystore.com/products/polygon-dark-fortress) | Synty | $249.99 | 13 (behemoth, colossus, plague lord, wraith, undeads…) + 850 prefabs | **No** | Mecanim | Unity 2022.3, Unreal 5.3, FBX | v1.0.5 |
| [POLYGON Goblin War Camp](https://syntystore.com/products/polygon-goblin-war-camp) | Synty | $179.99 | 15 + 600 prefabs | **No** | Mecanim / UE4 skeleton | Unity 2022.3, Unreal 5.3, FBX | v1.1.0 |
| [ANIMATION Collection Bundle](https://syntystore.com/collections/animation) | Synty | **$204.99** (from $409.99) | 6 packs: Base Locomotion, Sword Combat, Bow Combat, Idles, Emotes & Taunts, Goblin Locomotion | — | **Mecanim humanoid avatar only** | Unity 2022.3 + FBX zip | [Base Locomotion](https://syntystore.com/products/animation-base-locomotion) alone is 247 clips, $69.99 |
| [Sidekick Starter Pack](https://syntystore.com/products/sidekick-modular-characters-starter-pack) | Synty | **Free** | 57 modular parts + 91 human base parts + the Character Creator tool | No | Unity Humanoid + UE Mannequin | Unity 2021.3+, Unreal 5.3–5.8 | Full packs (e.g. [Fantasy Skeletons](https://syntystore.com/products/fantasy-skeletons-sidekick-modular-characters)) are $199.99 each |
| [Lowpoly Complete Bundle — Medieval Fantasy](https://assetstore.unity.com/packages/3d/characters/lowpoly-complete-bundle-medieval-fantasy-series-315750) | Polytope Studio | $344.99 | Modular NPCs, armours, village, props | Partly | Humanoid | Unity | Coherent single-publisher series; skews human NPCs over monsters |
| [Monsters Ultimate Pack 01 — Cute Series](https://assetstore.unity.com/packages/3d/characters/creatures/monsters-ultimate-pack-01-cute-series-167028) | Meshtint | €64.40 observed (at 50% off) | Not stated on the listing | **Yes** — e.g. [Phantom](https://www.meshtint.com/products/phantom-mega-toon-series) ships 13 clips (spawn, idle, fly, turn L/R, projectile, slash L/R, stab, cast, hit, die) at 505 tris | Generic Mecanim | FBX, PSD, unitypackage | Updated 31 Jan 2025. Style is "cute," not LTD2. |
| [RPG Monster BUNDLE Polyart](https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-bundle-polyart-261480) | Dungeon Mason | €55.20 observed | Not stated on the listing | Not stated on the listing | — | Unity 2021.3+ | v1.0, 23 Jul 2023 |

★ = top pick.

### Why KayKit and not Synty

This is the opposite of the received wisdom — Part III itself named Synty first — so the reasoning should be
explicit.

| | KayKit Complete | Synty (equivalent fantasy set) |
|---|---|---|
| Cost | **$150** once, forever, all future packs | ~$400–600 one-time (Dungeon + Rivals + ANIMATION bundle), or $360/yr rental |
| Animated out of the box | **Yes** | **No** — animations sold separately, humanoid only |
| Non-humanoid creature animation | Included | **You animate it yourself.** Nothing in the Synty ANIMATION line covers a golem, troll or medusa. |
| Roster size | ~57 characters | Larger, if you buy several packs |
| Source files | `.blend` included at the $150 tier | FBX only; editing permitted, reselling edits prohibited |
| Licence | CC0 — no seats, no expiry, no AI-dataset clause | 5 seats, perpetual only if bought outright |
| Environment / towers | Dungeon + Medieval Hexagon kits, free | **Far better and far deeper.** This is Synty's real advantage. |
| Art direction fit to LTD2 | Chunky, flat, gradient-atlas | Flat-shaded low-poly, larger prop vocabulary |

**Neither matches Legion TD 2's hand-painted look.** That is worth saying flatly rather than pretending
otherwise — LTD2's texture treatment is bespoke and is the thing an asset pack cannot give you. Both packs land
in the same *family* (readable low-poly silhouettes at small screen size), and at TD scale that family is what
players actually perceive.

The split that follows is the recommendation: **KayKit for everything that moves, Synty for everything that
doesn't.** Character-to-character coherence is what the eye grades, because units are the thing crowded
together in a lane at identical scale. An environment from a different publisher is a much weaker mismatch
signal — and it is fixable with one shared shader and one post stack (§10). This is a judgement call, and it
has a cheap test: put a KayKit skeleton on a Synty dungeon tile, screenshot it at your actual camera distance,
and look. Do that before spending the $150 + $150.

### The animation problem, priced

If you *do* go Synty-first, here is what you are actually signing up for. It is the honest number nobody quotes:

- **Humanoid creeps (goblins, skeletons, knights, peasants):** solved. Mecanim humanoid retargeting plus
  Mixamo (free) or the Synty ANIMATION bundle ($204.99). Cheap.
- **Non-humanoid creeps (rock golem, troll, medusa, big demon, behemoth, ghosts, kaiju):** unsolved. Each needs
  a rig and four to six clips authored in Blender by you. Judgement: **one to three days per creature for a
  competent non-animator, and a week for the first one.** Twenty non-humanoid creeps is a quarter of a year
  before a single one of them is *interesting*.

That is the calculation that moves KayKit above Synty for a solo developer optimising for speed to a playable
build. It is not a statement about the art.

---

## 6. Track B — 2D / pixel art. The shortlist, offered honestly.

Presented in full because the question was asked in good faith, and because if the design turns out to be a
**side-on lane** game rather than a top-down grid, most of §7's objection collapses and this track becomes
genuinely competitive.

| Pack | Publisher | Price (USD) | Contents | Animated? | Perspective | Verified |
|---|---|---:|---|---|---|---|
| **[Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords)** ★ | Pixel Frog | **Free** (name your price) + **$15** for the Enemy Pack | Free: 4 unit types (Warrior, Lancer, Archer, Monk), 8 buildings incl. **defense towers** and castles, terrain, decorations, UI, in 5 faction colours. Paid: 18 enemies, "expanding to 30 with weekly updates" | **Yes** — animated at 10 fps / 100 ms | Top-down-ish, 64×64 tile grid | PNG + Aseprite sources. Personal and commercial use, modification allowed, redistribution prohibited. Last devlog ~72 days ago. |
| [Tower Defense (Top-Down)](https://kenney.nl/assets/tower-defense-top-down) | Kenney | **Free** (CC0) | 300 assets | Not indicated | Top-down | The single best free starting point for a 2D TD blockout. Static. |
| [Tower Defense Top-Down Pixel Art Collection](https://craftpix.net/sets/tower-defense-top-down-pixel-art/) | CraftPix | [Membership](https://craftpix.net/membership/): **$15/mo** or **$4/mo billed annually (~$48/yr)** | 18+ character/sprite packs (Guardian, Catapult, Mage and Archer towers; medieval, undead, swamp, graveyard, field and village enemy sets) + 7 tilesets | Yes ("great motion animations"), but per-pack details are thin | **Purpose-built top-down TD** | PSD + PNG. **Downloads remain usable after cancellation** ([licence](https://craftpix.net/file-licenses/)). Individual packs are small — e.g. [Top-Down Pixel Monster Sprites](https://craftpix.net/product/top-down-pixel-monster-sprites-for-tower-defense/) is 3 characters. |
| [Fantasy Monsters Animated \[Megapack\]](https://assetstore.unity.com/packages/2d/characters/fantasy-monsters-animated-megapack-159572) | Hippo | €36.71 observed | Roster count not stated on the listing | Yes | **Side-scroller framing** | v3.1, 7 Jul 2025 |
| [Monsters Creatures Fantasy](https://luizmelo.itch.io/monsters-creatures-fantasy) | LuizMelo | Free (CC0) | Fantasy monsters | Yes | **Side-scroller framing** | CC0 is genuinely permissive; perspective is the problem |
| [Pixel Art Tower Defence](https://www.gamedevmarket.net/asset/pixel-art-tower-defence) | GameDev Market | — | TD kit | — | Top-down | Pro Licence: commercial, no engine limits |

★ = top pick. **Track B total cost: ~$63** (Tiny Swords Enemy Pack $15 + a CraftPix annual membership $48),
or **$15** if Tiny Swords alone carries it.

That is a genuinely cheaper number than Track A, and it is why the intuition "2D will be faster" feels right.
It is wrong for a reason that has nothing to do with money.

---

## 7. Why 3D wins, and it's not about taste

Four arguments, in descending order of force.

### 7.1 The facings multiplier

A top-down or three-quarter tower defense needs units that face the direction they are walking. In 3D that is
free — the GPU rotates the mesh. In 2D it is a separate sprite sequence per facing.

| | 3D | 2D top-down (8 facings) |
|---|---:|---:|
| 40 units × 4 clips (idle/walk/attack/die) | **160 clips** | 160 × 8 = **1,280 sprite sequences** |
| Adding one unit | 1 model + retarget the shared clip library | 32 new sequences, hand-drawn |
| Adding one animation to every unit | 40 clips, or **1** if it retargets | 320 sequences |

Every asset pack in Track B is drawn for one to four facings, because they are made for platformers and
top-down RPGs with modest rosters. **A 2D TD with a 40–60 unit roster is not a purchasing problem, it is a
commissioning problem**, and no pack on the market solves it. That is the whole argument, and it holds
regardless of whether you prefer pixels.

*Caveat that matters:* if the game is **side-on lane** — the original Legion TD framing, units marching left
to right — facings collapse to two (or one plus a horizontal flip) and this multiplier evaporates. See §11.

### 7.2 Depth sorting in a crowded lane

Part III's target is 40–60 simultaneous units. In 3D, overlapping units resolve themselves via the depth
buffer. In 2D you own a per-frame sort, and with 60 sprites clustered at a choke point it is both a
correctness problem (flicker on ties — and note Part III already bans unstable sorts inside the sim) and a
readability problem.

### 7.3 The fixed camera pays twice in 3D, once in 2D

Part III recommends committing to a fixed three-quarter camera "as much a budget decision as a style one." In
3D that buys you free back-face culling, trivial LODs and silhouettes tuned to a single known angle. In 2D the
camera is already fixed, so you get none of that as a saving — you have simply pre-paid it in sprite count.

### 7.4 The market is thinner where you need it

Track A has multiple single-publisher rosters of 50+ animated fantasy creatures (KayKit ~57, Quaternius 50
monsters + 52 characters, Synty MINI 60 static). Track B has nothing comparable in top-down framing. CraftPix
gets closest and does it by assembling many three-and-four-character packs. That difference is not an accident
— 3D character packs amortise across every camera angle, so publishers invest there.

> **The one thing that would flip this.** Pre-rendering. Render your 3D roster to sprite sheets from the fixed
> camera at the eight facings you need and ship a 2D game with 3D-derived art — the Diablo/StarCraft method.
> This is a real option and it preserves the pixel aesthetic if you post-process aggressively. But note what it
> requires: **a 3D roster first.** It is a rendering decision downstream of Track A, not an alternative to it.

---

## 8. The decoupling argument — what is sticky and what is cheap

The user's real question underneath the asset question is: *if I commit now, how expensive is it to be wrong?*
Part III's answer — the sim is an engine-free library — is stronger here than it looks, but it does not make
everything free. Precisely:

### Cheap. Change these on any given Tuesday.

| Decision | Why it's cheap |
|---|---|
| **2D vs 3D rendering** | The sim emits fixed-point positions and states. A 2D renderer reads the same `World` struct-of-arrays a 3D renderer does. Neither is referenced by `sim/`. |
| **Which asset pack** | Prefab references and a mapping table from unit id → visual. Content, not code. |
| **Art style, models, textures, VFX** | Entirely view-layer. |
| **Camera perspective** | *Provided gameplay never depends on it* — no fog of war revealed by camera, no rotation that changes what is knowable. |
| **Animation clip lengths** | See below. This is cheap **only if** you enforce one rule. |

### Sticky. These are sim data and stored ghost format; changing them retires the pool.

| Decision | Why it's sticky |
|---|---|
| **Grid vs free placement** | Part II's `GhostRecord` stores `layout []Tower` with a `cell` field. Grid-to-free is a format change that invalidates every stored defense. Decide before step 2. |
| **Simultaneous unit count** | Drives the entity arrays, the tick cost, the balance-sweep throughput, *and* the readability budget of the art. It is simultaneously the most art-facing and most sim-facing number in the project. |
| **Lane topology and pathing model** | Sim. |
| **Tick rate** | Sim, and baked into every stored record. |
| **Projectile travel time vs. hitscan** | Sim. Also the single most art-visible sim decision, because it determines whether you need projectile VFX at all. |

### The half-sticky one, which is where projects actually die

**Animation-driven timing.** The natural instinct with a bought pack is to fire damage on an animation event —
the frame the sword lands. Do that and the simulation now depends on the renderer, and Part III's central rule
("the view layer must be a pure function of simulation state plus an interpolation alpha") is broken, along
with replay scrubbing, double speed, instant-resolve, and server re-validation.

> **The rule that makes asset packs safe.** The **sim** owns windup and backswing as integer tick counts, per
> unit, in `content/`. The **view** receives them and scales animation playback speed to fit. A pack whose
> attack clip is 0.8 s is perfectly usable when your sim says 0.6 s — you play it at 1.33×.
>
> Two consequences worth internalising. First, **animation length never constrains balance**, so a balance
> sweep can retune every attack speed in the game without touching art. Second, **swapping packs cannot desync
> anything**, because no stored ghost ever encoded a frame number. This one rule is what converts "centre the
> game on an asset pack" from a lock-in into a genuinely reversible decision.

---

## 9. Does centring on one pack actually accelerate a solo dev?

Yes, and the primary evidence is unusually direct — developers of shipped, commercially successful games
saying so in their own words.

**Soulstone Survivors** (Game Smithing Limited) is the closest analogue available: a horde game rendering large
numbers of animated units at once, from a small team, built on Synty. It sits at **26,675 Steam reviews across
all languages, 12,229 in English at 91% positive**, Early Access Nov 2022, 1.0 Jun 2025
([Steam](https://store.steampowered.com/app/2066020/Soulstone_Survivors/)). Founder Allan, in Synty's own
case study ([6 Aug 2024](https://syntystore.com/blogs/blog/made-with-synty-soulstone-survivors)):

> "We wanted to prototype ideas and make playable games as fast as possible to be able to test concepts out
> with players. Using Synty assets was literally the first thing we did when we hit 'create new project'!"

> "To this day we still use a ton of Synty products, and I can safely say that if it were not for these assets,
> there would be no Game Smithing today."

They report functional prototypes with moving characters, enemies and spell effects **within one to two weeks**.
That is the acceleration claim, made concretely, by someone who shipped. Synty's case-study series carries
several more — [No Plan B](https://syntystore.com/blogs/blog/made-with-synty-no-plan-b) (released April 2024,
Very Positive a year later), [SurrounDead](https://syntystore.com/blogs/blog/made-with-synty-surroundead),
[It's Only Money](https://syntystore.com/blogs/blog/made-with-synty-its-only-money). These are the publisher's
own marketing and should be read as such, but the Steam numbers behind them are independently checkable.

### The five ways it goes wrong

1. **The roster runs out.** You need ~50 readable units; most packs have 12–20. Count the roster against your
   design's unit count *before* buying, not after. This is the check that eliminated most of the Track A table.
2. **Animations aren't included.** The Synty trap. Assume nothing; the listing says it explicitly if it's true.
3. **Rig fragmentation.** Humanoid Mecanim characters share one animation library for free. Every non-humanoid
   creature is a separate rig with a separate library and no retargeting. A roster that is 80% humanoid is
   dramatically cheaper than one that is 40% humanoid, and *that is an art-direction decision you make now*.
4. **Proportion and scale drift.** Chibi next to heroic next to realistic. Fatal, and only visible once units
   are crowded together — which is exactly the TD failure case.
5. **The pack starts designing the game.** The insidious one. A unit exists because the pack has it, and now an
   artist you have never met is making your balance decisions. Counter: write the roster from the design first,
   *then* map it onto the pack, and accept substitutions only where the silhouette still communicates the role.

---

## 10. Avoiding the asset-flip read

### First, the definition — because it does not say what people think it says

An asset flip is "a type of shovelware in which a video game developer purchases pre-made assets" to create
"numerous permutations of generic games to sell at low prices" — the term was coined by games journalist
James Stephanie Sterling around 2015 ([Wikipedia: Asset flip](https://en.wikipedia.org/wiki/Asset_flip)).

Read the load-bearing words: *shovelware*, *numerous permutations*, *generic*, *low prices*. The definition
indicts **absence of original work, not presence of bought work.** A single game with real systems behind it
does not meet it no matter how much of the art was purchased.

### Do people look down on it? Yes — loudly, in public, and it does not appear to matter

Both halves of that sentence are true and the gap between them is the actionable part.

**The criticism is real and specific.** Players have learned to recognise Synty on sight. There is a thread on
Erenshor's Steam forum titled *"these damn synty unity assets again"*
([Steam](https://steamcommunity.com/app/2382520/discussions/0/506200271931576244/)), naming Stolen Realm and
Soulstone Survivors in the same breath:

> "These games all have the same spell effects, models, art, it's so boring… I'd much prefer bad graphics and
> low res textures over these cringe overused assets."

That is the risk, stated by an actual customer, and it is the strongest argument in this document for §10's
coherence work below. Note the precise complaint, though: not *bought*, but **recognisable and repeated**.

**The community answer, in the same thread, ran overwhelmingly the other way** — and no developer had to show
up to make the case:

> "These assets allow solo devs to be able to make games on their own that normally would take a studio of
> multiple programmers and designers to make."

> "You clearly don't know how hard it is to make even 'bad assets' for a full game. I'd rather have a game with
> store assets than no game at all."

**And the scoreboard is unambiguous.** Every game named in that complaint thread is a commercial and critical
success:

| Game | Steam reviews | Team |
|---|---|---|
| [Erenshor](https://store.steampowered.com/app/2382520/Erenshor/) — the game the thread was posted on | **94% positive, 1,951 reviews** (Very Positive) | Burgee Media |
| [Soulstone Survivors](https://store.steampowered.com/app/2066020/Soulstone_Survivors/) | **91% positive**, 12,229 English of 26,675 total | Game Smithing |
| [Stolen Realm](https://store.steampowered.com/app/1330000/Stolen_Realm/) | **84% positive, 2,490 reviews** (Very Positive) | Burst2Flame, three friends |

A game can be the literal subject of a "these damn Synty assets" thread and sit at 94% positive. The complaint
is loud; it is also a minority position that gets argued down by other players, and it does not show up in the
numbers.

**The precedent goes considerably higher than indie.** Bennett Foddy — who has more standing here than anyone,
having shipped *Getting Over It* on "nearly all free assets, a handful of paid assets, one or two custom
things" — has argued the label is applied incoherently, noting that games built from bought or free art can
still contain "a huge amount of work in them when it comes to code and design and ideas," and naming
*Flappy Bird*, *PUBG* and *Banished* as games arguably caught by the same definition
([Wikipedia](https://en.wikipedia.org/wiki/Asset_flip),
[GamesBeat](https://venturebeat.com/pc-gaming/in-defense-of-asset-flips-on-steam/) *(429 on fetch — see §12)*).
PUBG shipped on pre-made assets and became one of the best-selling games ever made.

**The conclusion for this project.** The reputational risk of buying art is real but small, well-understood by
players, and almost entirely mitigable by the six items below. The risk of *not* buying art — a solo developer
spending a year on models for a game whose core loop has not been validated — is far larger and is the one
Part II's build order exists to prevent. Ship the game; the art is a surface you can replace unit by unit
afterwards, and §8's animation-timing rule is what keeps that true.

One genuine caveat, and it points at Synty rather than at packs generally: **the recognisability risk scales
with the pack's popularity.** Synty is the most-used low-poly art in indie games, which is exactly why players
can name it. KayKit is far less saturated — a real, if minor, secondary advantage of the §5 recommendation
that has nothing to do with animations.

### What to do about it — in rough order of leverage per hour

1. **Custom UI.** The highest-leverage original art in the project, and the one nobody does. Players stare at
   your HUD for the entire match; it is the surface that says "someone made this." Part III already flags dense
   UI as a requirement — treat it as art, not plumbing. Synty's [INTERFACE Fantasy
   Menus](https://syntystore.com/products/interface-fantasy-menus) ($79.99, 200+ animated prefabs, 700+ sprites)
   is a strong base *to modify*, not to ship untouched.
2. **One shader for everything.** A single custom lit/ramp shader with a rim light, applied uniformly across
   every pack you use, is the cheapest possible coherence fix and it is what makes a mixed-publisher game read
   as one. This is also the answer to the KayKit-characters-on-Synty-tiles concern in §5.
3. **One post-processing stack.** Colour grading, bloom, vignette, applied globally. Two packs graded to the
   same LUT look far more related than they are.
4. **Recolour aggressively.** Synty ships 4–5 alternative colourways and 3 skin tones per character
   *specifically for this*; KayKit ships alternative textures at the Extra tier. Team-colour tinting for the
   attacker/defender split is free differentiation your design needs anyway.
5. **Custom VFX.** Abilities are where a TD's personality lives, and VFX is authored, not bought — or bought and
   heavily retinted. Legion TD 2 used PopcornFX; the modern equivalent is Unity VFX Graph or Shuriken plus a
   stylized pack you restyle.
6. **Signature silhouettes for signature units.** You do not need 50 bespoke models. You need the four or five
   units that appear in every screenshot to be yours. Budget custom art there and nowhere else.

---

## 11. The one input I do not have

**Can you model and rig in Blender at even a competent hobbyist level?**

The entire recommendation pivots on this one fact, and it flips cleanly:

- **If no** (the assumption here): KayKit at $150, because animations-in-the-box is worth more than roster
  ceiling or art quality, and the non-humanoid animation gap in the Synty line is a wall you cannot climb.
- **If yes**: invert it. Buy [POLYGON Dungeon Pack](https://syntystore.com/products/polygon-dungeon-pack)
  ($149.99) + [Fantasy Rivals](https://syntystore.com/products/polygon-fantasy-rivals-pack) ($99.99) + the
  [ANIMATION Collection](https://syntystore.com/collections/animation) ($204.99) for ~$455, use Mecanim
  retargeting for the 36 humanoids, and author four clips each for the dozen non-humanoids yourself. The art
  ceiling, the prop vocabulary and the environment coverage are all meaningfully higher, and Synty's FBX-only
  delivery (no `.blend` sources) stops mattering once you can rig.

**A second, smaller unknown that would change §6 and §7 substantially:** is the game **top-down grid** or
**side-on lane**? A side-on lane game collapses the facings multiplier from 8 to 1, removes the depth-sorting
problem, and makes Track B's pixel packs — which are overwhelmingly drawn side-on — suddenly appropriate rather
than mismatched. Legion TD 2 is top-down three-quarter and Part III assumed the same. If that is still open,
resolve it before either purchase, because it is worth more than the $150.

---

## 12. What I could not verify

Stated plainly, because a research document that only reports successes is not a research document.

- **The Unity Asset Store EULA text itself.** `unity.com/legal/as-terms` and its sub-pages returned **HTTP 403**
  to automated fetching on every attempt. Everything in §4's Unity row comes from two other Unity-operated
  pages — the [Asset Store Terms & EULA FAQ](https://assetstore.unity.com/browse/eula-faq) and Unity's
  [official support article on other engines](https://support.unity.com/hc/en-us/articles/34387186019988-Can-I-use-assets-from-the-Asset-Store-with-other-engines).
  Both are authoritative, neither is the contract. **Read the EULA in a browser before you rely on the
  non-Unity-engine conclusion.**
- **The Mixamo FAQ.** [helpx.adobe.com/creative-cloud/faq/mixamo-faq.html](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html)
  timed out. The characterisation in §4 — free, royalty-free, humanoid-only, unmaintained since Adobe's
  acquisition, with a multi-day outage in June 2025 — comes from secondary summaries of that page, not from the
  page. Treat Mixamo as a useful legacy tool with a non-zero chance of vanishing, and download what you need
  rather than depending on the service.
- **SyntyPass post-cancellation rights for an already-shipped game.** Synty's public pages state the
  subscription licenses you "while your subscription plan is active" but do not say what happens to a title
  already on sale. **Get this in writing from Synty before subscribing**, or buy one-time for anything shipping.
- **Unity Asset Store prices are shown in EUR** by the store's geo-detection, so the Meshtint (€64.40 at 50%
  off), Dungeon Mason (€55.20) and Hippo (€36.71) figures are observed in euros, not dollars. Do not quote the
  dollar conversions as prices.
- **Roster counts and animation lists for several Unity Asset Store packs** are simply not on the listing page —
  including for both Dungeon Mason bundles and Meshtint's Monsters Ultimate Pack 01. That absence is itself a
  reason to prefer publishers who state it (KayKit, Quaternius, Synty all do).
- **KayKit's total character count.** The [Complete KayKit devlog](https://kaylousberg.itch.io/kaykit-complete/devlog/1571041/the-complete-kaykit-v6)
  does not state a total. The ~57 figure is my own sum from the individual pack pages: Adventurers 8 +
  Skeletons 6 + Mystery Monthly Series 4 (15) + Series 5 (14) + Series 6 (14).
- **Legion TD 2's camera angle and on-screen unit scale.** No primary source states them. Part III's "fixed
  three-quarter camera" is an observation from screenshots, and I am carrying it forward as **judgement, not
  fact**.
- **Polycount.com job postings** describing LTD2's hand-painted texture approach and polygon philosophy returned
  **HTTP 403**. Those descriptions appear in search summaries and are consistent with the Sketchfab spotlight,
  but I have not read the postings, so §3.1's characterisation of the texture philosophy is **judgement, not a
  verified quote**.
- **The asset-flip etymology and the Foddy quotes in §10** come from
  [Wikipedia's *Asset flip* article](https://en.wikipedia.org/wiki/Asset_flip), not from Sterling's or Foddy's
  own material. The [GamesBeat piece](https://venturebeat.com/pc-gaming/in-defense-of-asset-flips-on-steam/)
  that carries Foddy's argument in full returned **HTTP 429** on every fetch. The definition is
  uncontroversial and the Steam review numbers in §10 are independently checkable; the Foddy attribution is one
  step removed from primary.
- **Legion TD 2's outsourcing arrangement** (§3.1) — that modelling and texturing were progressively contracted
  out while Jean Go moved to other work — comes from search-result summaries of the artist's ArtStation
  project pages, which returned **HTTP 403** to direct fetching. The animation-outsourcing claim *is* primary,
  from the [Sketchfab studio spotlight](https://sketchfab.com/blogs/community/game-studio-spotlight-autoattack-games).
  The load-bearing claim — that Legion TD 2 uses no purchased asset packs — rests on the absence of any
  evidence for them across every source checked, plus a documented pipeline that starts at gameplay ideation.
  **Absence of evidence is weaker than a denial**, and no source states outright "we made everything ourselves."
- **Noita's internal render resolution.** Not stated in the presskit or the 80.lv interview; the GDC talk may
  cover it and I could not extract its contents.
- **Whether any recurring Synty Humble Bundle is live now.** The most recent I could verify is
  [Best of Synty #5](https://syntystore.com/blogs/blog/humble-bundle-best-of-synty-5) — "up to 16 asset packs
  worth over $1,000 USD… for just $30 USD" — which ran 24 Sep to 12 Oct 2025. These recur; bundle copies carry
  **1 seat, not 5**.

---

## 13. Monday morning

Ordered so that nothing you buy can be wasted by a later decision.

1. **Spend $0 and answer the perspective question first.** Top-down grid or side-on lane? It is worth more than
   any purchase on this page and it is a fifteen-minute paper decision. (§11)
2. **Download the free tier of everything before buying anything.** [KayKit Adventurers and
   Skeletons](https://kaylousberg.itch.io/) (free, 14 animated characters between them),
   [Quaternius Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html) (free, 50 animated
   monsters), [Kenney Tower Defense (Top-Down)](https://kenney.nl/assets/tower-defense-top-down) (free, 300
   assets), [Synty Sidekick Starter Pack](https://syntystore.com/products/sidekick-modular-characters-starter-pack)
   (free). **Total: $0, and it is already enough art for build-order steps 1 through 3.**
3. **Run the screenshot test.** Drop a KayKit skeleton, a Quaternius monster and a Synty Sidekick character into
   one scene at your intended camera distance and unit scale. Screenshot. This is the only reliable way to judge
   coherence, and it costs nothing.
4. **Buy [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete) — $150.** One transaction, CC0,
   ~57 animated characters, `.blend` sources, every future pack included. This is the whole Track A purchase.
5. **Write the animation-timing rule into `sim/` before you import a single model.** Windup and backswing are
   integer ticks in `content/`; the view scales playback to fit. Enforce it the same way Part III enforces the
   banned-API list — as a build error or a code review you never skip. (§8)
6. **Write the unit roster from the design, not the pack.** 40–60 rows: name, role, silhouette description,
   humanoid or not. Then map it onto KayKit and mark the gaps. The count of non-humanoid gaps is your real art
   budget. (§9)
7. **Do not buy Synty yet.** Revisit at the vertical-slice stage, after Part II's step 3 gate passes, and buy
   one-time rather than SyntyPass — starting with [POLYGON Dungeon
   Pack](https://syntystore.com/products/polygon-dungeon-pack) ($149.99) for environment and towers, and
   [INTERFACE Fantasy Menus](https://syntystore.com/products/interface-fantasy-menus) ($79.99) as a UI base to
   modify. Watch for a recurring Humble bundle at ~$30 first; note it carries 1 seat.
8. **Read the Unity Asset Store EULA in a browser** before you buy anything there, and ask Synty in writing what
   happens to a shipped game after a SyntyPass cancellation. (§12)
9. **Ignore all of the above until the determinism harness is green.** Part III's seventh thing-not-to-build is
   "art, before step 3 passes," and this document does not repeal it. Steps 1–3 are answerable with capsules.
   The $150 buys you the *option* to make it look like a game the week the gate opens — not permission to start
   now.

---

## Sources

Every claim above links to the page it came from. Grouped here for the record.

**Licences (primary documents)**
1. [Synty One-Time Purchase Licence & EULA](https://syntystore.com/pages/one-time-purchase-licence);
   [Synty Licences Overview](https://syntystore.com/pages/licences-overview); [Synty FAQ](https://syntystore.com/community/faq).
2. [Unity Asset Store Terms of Service and EULA FAQ](https://assetstore.unity.com/browse/eula-faq);
   [Unity Support — *Can I use assets from the Asset Store with other engines?*](https://support.unity.com/hc/en-us/articles/34387186019988-Can-I-use-assets-from-the-Asset-Store-with-other-engines).
3. [Fab — Licenses and Pricing](https://dev.epicgames.com/documentation/en-us/fab/licenses-and-pricing-in-fab);
   [Epic — Fab launch announcement](https://www.unrealengine.com/en-US/blog/fab-epics-new-unified-content-marketplace-launches-today).
4. [CraftPix File Licenses](https://craftpix.net/file-licenses/); [CraftPix Membership](https://craftpix.net/membership/).
5. [itch.io — General Paid Asset License, 21 Apr 2025](https://itch.io/blog/929708/general-paid-asset-license);
   [itch.io forum — warning for authors of unlicensed assets](https://itch.io/t/402794/warning-for-authors-of-unlicensed-assets).
6. [Poly Haven License](https://polyhaven.com/license); [OpenGameArt FAQ](https://opengameart.org/content/faq);
   [Adobe Mixamo FAQ](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html) *(fetch failed — see §12)*.

**Track A packs**
7. [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete) and the individual packs:
   [Adventurers](https://kaylousberg.itch.io/kaykit-adventurers), [Skeletons](https://kaylousberg.itch.io/kaykit-skeletons),
   [Mystery Monthly Series 6](https://kaylousberg.itch.io/kaykit-series-6),
   [Dungeon Remastered](https://kaylousberg.itch.io/kaykit-dungeon-remastered),
   [Character Animations (legacy)](https://kaylousberg.itch.io/kaykit-animations),
   [Kay Lousberg's itch.io profile](https://kaylousberg.itch.io/).
8. Quaternius: [Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html),
   [Ultimate Animated Characters](https://quaternius.com/packs/ultimatedanimatedcharacter.html),
   [Universal Animation Library 2](https://quaternius.com/packs/universalanimationlibrary2.html).
9. Synty: [Fantasy Kingdom](https://syntystore.com/products/polygon-fantasy-kingdom),
   [Dungeon Pack](https://syntystore.com/products/polygon-dungeon-pack),
   [Dungeon Realms](https://syntystore.com/products/polygon-dungeon-realms),
   [Fantasy Rivals](https://syntystore.com/products/polygon-fantasy-rivals-pack),
   [Dark Fortress](https://syntystore.com/products/polygon-dark-fortress),
   [Goblin War Camp](https://syntystore.com/products/polygon-goblin-war-camp),
   [MINI Fantasy Characters](https://syntystore.com/products/polygon-mini-fantasy-characters-pack),
   [Fantasy Characters](https://syntystore.com/products/polygon-fantasy-characters-pack),
   [POLYGON collection](https://syntystore.com/collections/polygon),
   [ANIMATION collection](https://syntystore.com/collections/animation),
   [ANIMATION Base Locomotion](https://syntystore.com/products/animation-base-locomotion),
   [INTERFACE Fantasy Menus](https://syntystore.com/products/interface-fantasy-menus),
   [Sidekick Starter Pack](https://syntystore.com/products/sidekick-modular-characters-starter-pack),
   [Sidekick Fantasy Skeletons](https://syntystore.com/products/fantasy-skeletons-sidekick-modular-characters),
   [SyntyPass](https://syntystore.com/products/syntypass).
10. Unity Asset Store listings: [RPG Monster BUNDLE Polyart](https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-bundle-polyart-261480),
    [RPG Monster BUNDLE PBR](https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-bundle-pbr-260493),
    [Monsters Ultimate Pack 01 Cute Series](https://assetstore.unity.com/packages/3d/characters/creatures/monsters-ultimate-pack-01-cute-series-167028),
    [Lowpoly Complete Bundle — Medieval Fantasy](https://assetstore.unity.com/packages/3d/characters/lowpoly-complete-bundle-medieval-fantasy-series-315750);
    [Meshtint Phantom Mega Toon](https://www.meshtint.com/products/phantom-mega-toon-series),
    [Meshtint series list](https://www.meshtint.com/pages/all-series-list-on-unity-asset-store).

**Track B packs**
11. [Tiny Swords by Pixel Frog](https://pixelfrog-assets.itch.io/tiny-swords);
    [Kenney Tower Defense (Top-Down)](https://kenney.nl/assets/tower-defense-top-down) and
    [Kenney's asset index](https://kenney.nl/assets);
    [CraftPix Tower Defense Top-Down Pixel Art Collection](https://craftpix.net/sets/tower-defense-top-down-pixel-art/) and
    [Top-Down Pixel Monster Sprites](https://craftpix.net/product/top-down-pixel-monster-sprites-for-tower-defense/);
    [Fantasy Monsters Animated Megapack](https://assetstore.unity.com/packages/2d/characters/fantasy-monsters-animated-megapack-159572);
    [LuizMelo Monsters Creatures Fantasy](https://luizmelo.itch.io/monsters-creatures-fantasy).

**Aesthetic grounding**
12. Legion TD 2: [Steam store page](https://store.steampowered.com/app/469600/Legion_TD_2__Multiplayer_Tower_Defense/),
    [official team page](https://beta.legiontd2.com/team/),
    [Sketchfab Game Studio Spotlight, 20 Aug 2018](https://sketchfab.com/blogs/community/game-studio-spotlight-autoattack-games),
    [Sketchfab model profile](https://sketchfab.com/autoattackgames),
    [Nekomata model, 1.8k tris](https://sketchfab.com/3d-models/legion-td-2-nekomata-06bb70ef39bb4751bb84bb9808b8fece),
    [dev post introducing their concept artist, 1 Aug 2016](https://beta.legiontd2.com/updates/introducing-our-new-concept-artist/).
13. Noita: [official presskit](https://noitagame.com/press/),
    [Nolla Games — Falling Everything](https://nollagames.com/fallingeverything/),
    [GDC Vault — *Exploring the Tech and Design of 'Noita'*, Petri Purho, GDC 2019](https://www.gdcvault.com/play/1025695/Exploring-the-Tech-and-Design),
    [80.lv interview with Petri Purho, 5 Apr 2019](https://80.lv/articles/noita-a-game-based-on-falling-sand-simulation),
    [Noita Wiki — Daily Run](https://noita.wiki.gg/wiki/Daily_Run),
    [Wikipedia — Noita](https://en.wikipedia.org/wiki/Noita_(video_game)).

**Asset-pack strategy evidence**
14. [Wikipedia — *Asset flip*](https://en.wikipedia.org/wiki/Asset_flip) (definition, Sterling attribution,
    Foddy's counter-argument, PUBG);
    [GamesBeat — *In defense of asset flips on Steam*](https://venturebeat.com/pc-gaming/in-defense-of-asset-flips-on-steam/)
    *(429 on fetch — see §12)*;
    [Erenshor Steam forum — "these damn synty unity assets again"](https://steamcommunity.com/app/2382520/discussions/0/506200271931576244/);
    [Erenshor on Steam](https://store.steampowered.com/app/2382520/Erenshor/) (94%, 1,951 reviews);
    [Stolen Realm on Steam](https://store.steampowered.com/app/1330000/Stolen_Realm/) (84%, 2,490 reviews).
15. [Made with Synty: Soulstone Survivors, 6 Aug 2024](https://syntystore.com/blogs/blog/made-with-synty-soulstone-survivors)
    and [Soulstone Survivors on Steam](https://store.steampowered.com/app/2066020/Soulstone_Survivors/) (26,675
    reviews all languages; 12,229 English at 91% positive);
    [Made with Synty: No Plan B](https://syntystore.com/blogs/blog/made-with-synty-no-plan-b);
    [Made with Synty: SurrounDead](https://syntystore.com/blogs/blog/made-with-synty-surroundead);
    [Made with Synty: It's Only Money](https://syntystore.com/blogs/blog/made-with-synty-its-only-money);
    [Humble Bundle: Best of Synty #5](https://syntystore.com/blogs/blog/humble-bundle-best-of-synty-5).
16. Part III, *Technology Stack Assessment* — the engine-free sim library, the rendering rule, the fixed-camera
    budget argument, and §8's "art is the long pole, not code."
17. Part II, *Async Ghost Round-Robin* — the ghost record format that makes grid-vs-free placement sticky, and
    the build order this document is sequenced against.
