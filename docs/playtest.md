# The playtest protocol

What six people are asked to look at and say, once a round and once a run, and where it is written down.
This is the instrument for the people playtest that [the build order](build-order.md#the-sequence)'s step 7
builds toward; [the sit-down](sit-down.md) was the walking skeleton's and has run.

**A playtest is evidence, not a vibe.** The sit-down's rule holds: no row is allowed to be an opinion about
whether the game is good. Every question below asks for something a person can point at, and the answers are
recorded beside the rounds they were given about, so a reading can be checked against the match.

## Before you start

- A build from `tools/build-player.ps1`, the same build on every machine, named by its commit.
- One lobby folder every machine can reach, empty. Each player's client writes its rounds there and reads the
  others'. The client waits at the barrier until the folder holds a round at the stage from everyone present.
- Each player's name in their settings, so a round says whose it is.
- One person keeping the record: a GitHub issue for the sitting, labelled `type:grilling` and the effort's
  `effort:` label, opened before the first round.

## Each round, after the result screen

Three questions, asked of everyone, answered in a sentence each. They are asked at the screen, not
afterwards.

| # | Ask | What it is evidence about |
|---|---|---|
| 1 | Which bar did you look at first, and did it say what you expected? | Which metrics carry information, and whether a position on the curve reads |
| 2 | What did you want to do this round that the screen did not let you do, or did not tell you? | The wheel, the rail and the header: what is missing from the planning surface |
| 3 | Name the moment in the wave you were watching for, and say whether you saw it | The watching surface, the animation score and the projectiles: what reads at the speed it plays |

## After the run

Four questions, asked once, answered in a sentence each.

| # | Ask | What it is evidence about |
|---|---|---|
| 4 | Which round did you know you had lost or won, and what told you? | Whether the score's curve gives a run a shape, and where the tension is |
| 5 | What would you build differently next run, in one sentence? | Whether the build space is a decision rather than a menu; the roster's depth |
| 6 | Which tower or creep did you never consider, and why? | Which rows are invisible, mispriced or unreadable, for the sweep to be pointed at |
| 7 | Where on the board could you not tell which level a cell was on? | The smoothing candidate against the legibility veto |

## What is recorded, and where

- **The lobby folder is kept whole** and attached to the sitting's issue, since a round is hundreds of bytes.
  It is the first population of real rounds, and [the sweep specification](specs/sweep-harness.md) names it
  as the field the stand-in gives way to.
- **Every answer goes on the issue** in the round it was given about, in the player's own words, and never
  paraphrased into a verdict. A verdict is the decision log's, on the day Sam takes it.
- **The build's commit and every machine's settings** go at the top of the issue, so a reading can be
  reproduced.

## What is not being assessed

Not whether it looks good, and not whether it is fun. Question 5 is the nearest this protocol comes to the
second, and it asks for a plan rather than a feeling. A row above is failed when the thing it names was not
seen, not when the thing it names was disliked.
