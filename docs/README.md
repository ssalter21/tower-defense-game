# Documents

An index. **Every verdict lives in the document that holds it** — this file says only what each one is for, so
there is nowhere for a claim to sit and go quietly out of date.

## Start here

**[The Vision](vision.md)** is the standing document — what this game is and what it is not. Where anything
else in this directory disagrees with it, the vision is current.

In one line: *a technically excellent tower defense, built for the pleasure of building it, whose multiplayer
is real — and every mode of it is the same machine at a different latency.*

| | |
|---|---|
| [The Vision](vision.md) | The destination and the pillars — what is decided, and nothing else |
| [The build order](build-order.md) | The seven-step sequence, and the nine seams it serves |
| [Open questions](open-questions.md) | In scope, undecided, and what each one is waiting on |
| [The decision log](decision-log.md) | Every time the vision changed its own mind, and why |
| [The roster](roster.md) | Every unit that exists or is proposed — what it is for, what it looks like, and what about it is still unsigned |
| [The sit-down](sit-down.md) | Twelve things to look at in the build, once, each naming the exact tick |
| [`adr/`](adr/) | Why the code is shaped the way it is — 51 records. Source comments say *what*; these say *why* |
| [`research/`](research/) | Evidence notes. Each answers one question and cites primary sources |
| [`archive/`](archive/README.md) | The five deep dives the vision was built on, the row-by-row account of what it overturned in them, and the specifications whose implementation is gone |
| [`frames/`](frames/) | Rendered match frames — documentation, not an oracle |

## Research notes

In [`research/`](research/). They are **evidence, not design documents**: each resolves one question, cites
primary sources, and decides nothing. Where the vision has since moved past one, the note says so in a banner
at its top rather than being rewritten.

**Design research**, commissioned against the vision's depth direction and open questions:

| Note | The question it answers |
|---|---|
| [Build depth in tower defense](research/build-depth-in-tower-defense.md) | How do TD games produce extreme, combinatorial build depth, and which mechanisms survive this project's constraints? |
| [The attacking half](research/attack-composition-and-sending.md) | How is sending made deep — and has anyone gated the attacking options on the player's defensive build? |
| [Creep wave variety and creep upgrade systems](research/creep-wave-variety-and-creep-upgrade-systems.md) | Which games went deep on creep variety, and does anything let you upgrade creeps the way you upgrade towers? |
| [Element TD's ancestry](research/element-td-ancestry-and-wc3-tower-mechanics.md) | Which WC3 map inspired Element TD, and what were the original's tower mechanics? |
| [Upgrade graphs in shipped tower defenses](research/upgrade-graph-representation-in-shipped-tower-defenses.md) | How do shipped games represent a tower's upgrade ladder, and what survives an upgrade? |
| [Towers, or placed squads?](research/towers-versus-placed-squads.md) | Does the defending side have to be towers, or could placements be flanking walls with archer squads? |
| [Why tower defense is fun, and where the skill is](research/fun-and-skill-expression.html) *(HTML)* | Why is the genre fun, and where does its skill expression actually live? |
| [Making the plan the game](research/planning-phase-and-simulated-stats.html) *(HTML)* | How do you make a build phase carry a whole game, and what can a 2.75 ms sim be spent on as design material? |
| [Generated maps, and how often they turn over](research/generated-maps-and-rotation.html) *(HTML)* | How do you generate maps worth playing, seed them cheaply, and pick a rotation cadence? |
| [The character roster: KayKit and Quaternius](research/kaykit-character-roster.md) | What 102 character models do the two packs actually contain, and which read as towers and which as creeps? |

**Simulation research**, measured in this repository rather than commissioned:

| Note | The question it answers |
|---|---|
| [Why the golden trace moved when the balance did not](research/the-tenfold-rescale-and-the-dice.md) | Multiplying every damage and health number by ten moved every generated artefact. Is that the rescale working, or a desync? |
| [A purse in one column beats the same purse in four](research/a-purse-in-one-column-beats-the-same-purse-in-four.md) | The sweep says taking more ingredients makes you worse. Why does spreading one purse across more columns lose? |
| [Cost is not a balance lever under a one-for-one leak](research/cost-is-not-a-balance-lever-under-a-one-for-one-leak.md) | If a leak charges what the creep cost, what does the cost column actually control? |
| [A canned field of one collapses the bands](research/a-canned-field-of-one-collapses-the-bands.md) | Four authored performance bands behave as two. Is the mechanism wrong, or the stand-in field? |

**Asset research**, on the art that is actually on the machine:

| Note | The question it answers |
|---|---|
| [What is actually inside The Complete KayKit Collection v6.1](research/kaykit-collection-inventory.md) | What does the downloaded bundle really contain — packs, rigs, clips, characters, triangle counts — read from the archive rather than from a listing? |
| [The KayKit model index](research/kaykit-model-index.md) | Does KayKit have a *thing*, and what is the file called? All 2,252 distinct model names |

**Build research**, on the tools rather than the game:

| Note | The question it answers |
|---|---|
| [Claude Code inside a Unity 6 project](research/unity-agent-workflow.md) | What can a terminal-only agent do inside Unity, and what must be done by hand? |
| [Unity 6 project-creation settings](research/unity-project-settings.md) | Which settings are expensive to change later? |
| [How the Unity project consumes the sim library](research/unity-sim-library-integration.md) | Precompiled DLL, or sources inside Unity? Carries [amendments](research/unity-sim-library-integration.md#amendments) |
| [How long Unity takes to notice a rebuilt plug-in](research/unity-hot-reload-timing.md) | Does an agent working while nobody is at the keyboard get stuck waiting for a reimport? |
| [A player build measures no text without a PanelSettings asset](research/a-player-build-measures-no-text-without-a-panelsettings-asset.md) | A build drew none of its HUD while the editor drew all of it. What is different about a player? |
| [The software factory, assessed against this repository](research/the-software-factory.html) *(HTML)* | What is the software-factory approach, which of it applies here, and what should change as a result? |
| [What agents can build unattended](research/what-agents-can-build-unattended.md) | With the tracker empty and step 5 half-built, which seams can an `/afk` run take to green, what proves each, and what must a person hand over first? |

> **One caveat the three Unity notes carry.** `unity.com` returns 403 to automated fetching, so every licence
> and pricing claim in them was read via a browser user-agent as extracted text. A human should confirm those
> in a real browser before relying on them commercially.

## The archive

The five deep dives written before the vision live in [`archive/`](archive/README.md), which is also where the
row-by-row account of what the vision overturned in them lives, and where a specification goes once the thing
it specified is deleted. **They are an input to the vision, not the current state of anything.**
