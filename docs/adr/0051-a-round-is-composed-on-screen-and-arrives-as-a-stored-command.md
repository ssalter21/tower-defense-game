# 0051 — A round is composed on screen, and arrives as a stored command

The client holds a `Run`, composes a round's `BuildPhase` **in a local** as the player clicks, prices every
change by calling the pure `BuildPhase.Resolve` and throwing the `Build` away, and hands the run nothing until
the player says they are done. The wave it then watches is fetched back out of the run by a read-only call,
not kept from the resolving.

This is [ADR-0050](0050-a-decision-is-composed-in-a-local-and-proved-before-it-is-written.md)'s shape at the
other end of the project. What changes is not the path but the surface: a prompt refuses a typed word, and a
screen never offers one.

## What was decided

**The client holds the run, and the view still holds nothing.**
[ADR-0007](0007-snapshot-is-the-only-view-input.md) says the snapshot is the only thing that may draw game
state, and that stands untouched — `MatchView` reads snapshots and nothing else. The `Run` lives one layer
out, in the scene root, beside the mode the player is in. What the build phase draws from is not game state
being simulated; it is a decision being composed, which no tick has yet seen.

**Illegality is prevented, never refused.** The prompt could afford to refuse a word, because a person who
types something is already looking at the line they typed. A player who has spent a minute arranging a wave
and is told at the end that it does not resolve has been allowed to waste the minute. So every affordance on
screen offers only what `BuildPhase.Resolve` would accept: a hex that cannot take a tower does not light, a
creep that cannot go in a box is not in the box's list, and the take — which is **mandatory and easily
forgotten** — has a permanent place in the header that reads as unfinished until it is taken.

The consequence is that the commit is not a gate. By the time it is pressed the phase already resolves,
because a phase that did not was never composable. This is [ADR-0050](0050-a-decision-is-composed-in-a-local-and-proved-before-it-is-written.md)'s
invariant reached from the other side: there, the composed phase always resolves because a candidate that did
not was never kept; here, because a candidate that did not was never offered.

**Prevention covers legality and stops there.** What is prevented is what the rules refuse. What is *not*
prevented — and must not be — is a decision that is legal and bad. [The vision](../vision.md) settles that
outcome is not computed in any mode, and a screen that greyed out a placement because it would not hold is a
forecast wearing the clothes of a rule. Mechanism is free and total; consequence is the player's to find out.

**The watched match is asked for, not kept.** `Run.Advance` resolves the wave against every member of the
field in locals and lets them go, which is right — a run that held ten matches per round would hold a hundred
by the end of one. The client needs exactly one of them to look at, so `Run` gains a read-only
`MatchAt(round, opponent)` that builds that match without advancing anything. The seed is derived, so the
match it builds is the match that was resolved; determinism is what makes asking twice cheaper than
remembering.

> **Amended 14 August 2026.** The call is `MatchAt(round, opponent, attacking)`, and the screen it feeds is the
> Offence and Defence Results Screen: a pairing is resolved in both directions, so a committed round can be
> watched as this round's towers against an opponent's wave or as this round's wave against an opponent's
> towers. It opens on the defence, which is the loop the game is about.
> [#206](https://github.com/ssalter21/tower-defense-game/issues/206) is why — the call was pinned to the
> offence, and what a player saw after pressing Done was their own wave walking into a stranger's defense.
> **The paragraph above is otherwise unchanged and is what it is for**: both matches were resolved by the
> round, switching between them rebuilds rather than re-simulates anything new, and no forecast is computed in
> either view. The build phase stays one joint screen.

**The record is still the only route in.** [ADR-0039](0039-the-command-stream-is-the-only-route-into-a-run.md)
holds: a decision reaches the simulation through a stored command and by no other route. Nothing a player
clicks touches a tick. What was `simcli`'s alone — compiling the played phases into a script, replaying it
into a fresh run on the same seed, and holding every round report and the folded outcome against what the
player was shown — moves down into `sim`, so the client proves its session the same way the prompt did. A
playtest is a determinism test in Unity for the same reason it was one in a terminal.

**A wave slot's position is its release order.** Vision §"You choose the order they come out in" says a wave
is a sequence and not a bag, and `content/wave.txt` has always been an ordered list of `(tick, type, count)`.
`BuildPhase.Resolve` did not honour that: it gave every slot the same release tick, so a wave's columns all
started together and position meant nothing — and the rule that filled slots must ascend strictly by type id
existed to canonicalise the arrangement precisely because the arrangement was not a decision. Position now
sets the release offset, the ascending rule comes out, and the bar the player drags is the order the creeps
arrive in. See [the decision log](../decision-log.md) for what that cost.

## What it costs

**Two ways to be told no, and only one of them is visible.** The refusal sentences `Resolve` writes are the
clearest statement of the rules in the codebase, and a screen that prevents illegality never shows one. They
stay reachable — the client asserts on a refusal rather than displaying it, because a refusal arriving at
commit means an affordance offered something it should not have. That assertion is the test that prevention is
actually total, and it is the reason prevention is safe to rely on.

**`Run` gains a public surface that ADR-0039 froze deliberately.** `MatchAt` is a second construction path for
a match, beside the one inside `Advance`, and two paths that must agree are two paths that can drift. They are
kept in step by sharing the assembly, not by being read side by side.

**The client can compose a phase the record cannot spell.** Nothing prevents a view from building a
`BuildPhase` whose fields no `CommandScript` row could carry. What catches it is the proving step at the end
of the run, which is late — a session discovers on its way out that it was not writable. That is accepted for
the same reason [ADR-0050](0050-a-decision-is-composed-in-a-local-and-proved-before-it-is-written.md) accepted
it: the alternative is a second implementation of the grammar, in the view, free to disagree.

## What was rejected

**Refusing at commit and showing the sentence.** The smallest thing to build, and `Resolve` already writes the
words. It fails the case that matters: a deep build phase is a place a player spends real time, and being told
at the end that the last four minutes do not resolve is the experience this effort exists to avoid.

**Greying the commit button until the phase is legal.** Prevention without explanation. As the game gains
levers the number of reasons the button might be grey grows, and the player learns none of them — a dead end
that gets worse with every feature rather than better.

**Keeping the resolved match from `Advance` and drawing that.** It would save a construction and it widens the
one call ADR-0039 pinned, so that the return type of advancing a run depends on whether anybody intends to
watch it. A run that is swept a hundred thousand times overnight should not carry a match it will never draw.

**Letting the view hold the run's state itself** — a purse, a board and an unlock set on the client side,
synchronised with the run. Two representations of one thing, free to disagree, in the place where disagreeing
is invisible. `Run` is already the single write point and `Board` already survives upgrades with its ordinals
intact; the client's job is to ask it questions.

**Sorting the wave bar at record time so free arrangement costs nothing.** It keeps the format version and
lets the player drag, and the drag changes nothing — an affordance that looks like a decision and is not. The
vision asked for a sequence; this would have delivered a bag with handles on it.
