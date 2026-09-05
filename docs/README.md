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
| [The roster expansion proposal](roster-expansion-proposal.md) | Nine tower lines and seventeen creeps drawn from every KayKit character, for review — placeholders until the roster signs them |
| [The sit-down](sit-down.md) | Twelve things to look at in the build, once, each naming the exact tick |
| [`adr/`](adr/) | Why the code is shaped the way it is — 57 records. Source comments say *what*; these say *why* |
| [`research/`](research/) | Evidence notes. Each answers one question and cites primary sources |
| [`archive/`](archive/README.md) | The five deep dives the vision was built on, the row-by-row account of what it overturned in them, and the specifications whose implementation is gone |
| [`frames/`](frames/) | Rendered match frames — documentation, not an oracle |

## Research notes

In [`research/`](research/). They are **evidence, not design documents**: each resolves one question, cites
primary sources, and decides nothing.

**Nine remain of twenty-four.** [Fifteen were
retired](decision-log.md#5-september-2026-later--fifteen-research-notes-are-retired) on 5 September 2026, and
the test each one was held to was not its age but whether anything still needed it: a note stays if code, a
content file or an ADR cites it, or if it holds a measurement that costs real time to take again. A survey
whose verdict has been read and written into the vision, an ADR or [open
questions](open-questions.md#what-the-design-research-found) is finished work, and keeping the working beside
the answer only gives a reader two places to look and one of them stale.

**Simulation research**, measured in this repository rather than commissioned. Each of these is cited from the
code or the content file whose number it explains:

| Note | The question it answers |
|---|---|
| [Cost is not a balance lever under a one-for-one leak](research/cost-is-not-a-balance-lever-under-a-one-for-one-leak.md) | If a leak charges what the creep cost, what does the cost column actually control? |
| [A canned field of one collapses the bands](research/a-canned-field-of-one-collapses-the-bands.md) | Four authored performance bands behave as two. Is the mechanism wrong, or the stand-in field? |
| [A wall kills a count, not a share](research/a-wall-kills-a-count-not-a-share.md) | What does an opponent who buys its column again every round cost a run, and can a run build its way out? |
| [A purse in one column beats the same purse in four](research/a-purse-in-one-column-beats-the-same-purse-in-four.md) | The sweep says taking more ingredients makes you worse. Why does spreading one purse across more columns lose? |
| [Why the golden trace moved when the balance did not](research/the-tenfold-rescale-and-the-dice.md) | Multiplying every damage and health number by ten moved every generated artefact. Is that the rescale working, or a desync? |

**Asset research**, on the art that is actually on the machine:

| Note | The question it answers |
|---|---|
| [What is actually inside The Complete KayKit Collection v6.1](research/kaykit-collection-inventory.md) | What does the bundle really contain — packs, rigs, clips, characters, triangle counts — read from the archive rather than from a listing? |

**Build research**, on the tools rather than the game:

| Note | The question it answers |
|---|---|
| [A player build measures no text without a PanelSettings asset](research/a-player-build-measures-no-text-without-a-panelsettings-asset.md) | A build drew none of its HUD while the editor drew all of it. What is different about a player? |
| [How long Unity takes to notice a rebuilt plug-in](research/unity-hot-reload-timing.md) | Does an agent working while nobody is at the keyboard get stuck waiting for a reimport? |
| [What agents can build unattended](research/what-agents-can-build-unattended.md) | Which seams can an `/afk` run take to green, what proves each, and what must a person hand over first? |

> **One caveat the hot-reload note carries.** `unity.com` returns 403 to automated fetching, so every licence
> and pricing claim written against it was read via a browser user-agent as extracted text. A human should
> confirm those in a real browser before relying on them commercially.

**Where the retired notes went.** The nine design surveys are summarised, verdict by verdict, in [open
questions](open-questions.md#what-the-design-research-found), which also carries the one thing in them that had
not been written down anywhere else: the fourteen uses a 2.75 ms match could be put to. The three Unity
build notes had all been adopted and then enforced by something that cannot go stale — `AGENTS.md` rule 3
forbids the editor bridge one of them recommended, and `tools/check-project-settings.ps1` asserts the settings
another chose. The two KayKit listings were superseded by the inventory above and by
`client/Assets/Art/Kaykit/`, which is the collection itself.

## The archive

The five deep dives written before the vision live in [`archive/`](archive/README.md), which is also where the
row-by-row account of what the vision overturned in them lives, and where a specification goes once the thing
it specified is deleted. **They are an input to the vision, not the current state of anything.**
