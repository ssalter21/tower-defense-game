---
name: prototype
description: Render candidate art, poses and framings through the game's own views so a human can pick one by looking. Use when a ticket asks for candidates, a look, a clip, a prop turn or a camera angle to be put up for sign-off.
---

# Prototype a look

In this repo a prototype is almost always **a rendered candidate set** — pictures a human decides from.
AGENTS.md rule 6 makes that the only way art, names and numbers a player sees can move: an agent renders the
alternatives and **stops**.

This shadows the general `prototype` skill, whose two branches assume a web app. Reach for those instead when
the question is a `sim/` state model (`~/.claude/skills/prototype/LOGIC.md`) or a screen layout
(`~/.claude/skills/prototype/UI.md`); neither describes putting a model up for approval.

## 1. Pick the instrument by the question

| The question | The instrument |
|---|---|
| Which model, prop, atlas or prop turn? | `capture-armed-roster.ps1 -SetFile` — one still per candidate, plus a contact sheet |
| **Which animation?** | the same, **`-Strip 12`** — see §3, a still cannot answer this |
| How does it read on the board, or with an effect firing? | `capture-match-frames.ps1 -Ticks` |
| What does a camera angle cost? | `capture-match-frames.ps1 -Yaw / -Pitch / -Distance` |

All of them are `-batchmode`, so **the editor must be closed**. Never edit a file while a run is going: it
forces a synchronous recompile and the run dies with exit 3 and no results.

## 2. Write a set file, one per question

`docs/roster-expansion-candidates.txt` documents the line format in full and is the file to copy. Six to eight
whitespace-separated fields, every path relative to `client/Assets/Art/`:

```
side  name  model  right-hand  left-hand  clip  [texture [beside]]
```

- `-` is an empty field. A held prop takes a turn as `path@x,y,z`; a beside prop takes a size as `path*0.55`.
- A model may drop mesh children with `path!Node_A,Node_B` — for kit carried in the body rather than a hand.
- **Qualify every clip name with its bank** (`Rig_Medium_General/Idle_A`). Unqualified names search
  `Rig_Medium_*` only. `Idle_A`, `Walking_A` and `Death_A` exist in both rigs, and a Large body posed by a
  Medium clip draws without complaining — issue #258 found that the hard way.
- Name the file `docs/roster-<question>.txt`, and add it to `StandingSets` in
  `client/Assets/Tests/EditMode/CandidateSetTests.cs`. Nothing else checks those paths: a model moved out of
  `Assets/Art/Kaykit` otherwise surfaces as a three-minute batchmode run that throws.

## 3. A still cannot answer "which animation"

Posed at the strike, a chop and a diagonal slice are two pictures of a body holding a hammer. What tells them
apart is the path the hammer took, and that is in no single frame. So a clip question takes `-Strip 12`, which
draws twelve frames across the clip into one row and writes `strip-index.txt` giving each clip's length.

Whatever displays them plays the frames back — a CSS sprite animation over the strip, stepped 12, run at the
length from the index. **That is the clip's authored speed, not the tower's**: a swing in the match is stretched
to the row's windup and backswing budget, and `docs/roster.md` carries `_` for both on any row whose clip is
still unsigned. Say so wherever the strips are shown.

## 4. Render at the framing the game ships, not only a flattering one

Every set goes out twice. The default `-Width 700` is the magnification. Then **`-Width 28`**, which puts a body
at about the 24 pixels it gets at 1600×900 in the built player — measured off
`docs/frames/played-run/a-slow-nobody-can-see.png`. Look at those at 100%. A difference that disappears there is
not a difference a player has, which is the finding issue #270 already made about a slowed body.

## 5. Read the render back before handing it over

**A run that exits 0 and writes differing PNGs can still answer nothing.** Every one of these shipped once and
had to be redone:

- **Something else was in frame.** A `beside` prop halved the character; the single-still capture's stand was
  still parented to the host when the strip drew, so a motionless body sat superimposed on the moving one. Two
  bodies at one position do not read as two bodies — they read as a smeared model. `ArmedRosterCapture.Alone`
  now refuses the second case; nothing guards the first.
- **Only the prop moved.** Posing an already-built view moves the bones — so a prop parented to a hand follows —
  while the skinned mesh goes on drawing the pose it was built in. Build each frame fresh, the way the
  single-still path does. It costs a minute for all forty-seven candidates; the clever version cost a round trip
  with Sam.
- **The difference was invisible at the size it matters.** See §4.

So: open the sheet, and **say out loud what changes between tiles or frames**. If you cannot name it, the render
is not ready. `cmp` two files that ought to differ — it caught that the Grave Robber's backpack hides the sword
in every frame, which retired a whole question. Crop two frames out of a strip side by side at 2× to check a
body is really moving.

## 6. Decide nothing

Where the vocabulary is enumerable, put **all** of it up — every clip a one-handed hammer could swing, every
quarter turn, with and without. An exhaustive sheet is not a choice; a shortlist is. Leaving an odd-looking
candidate off is the file deciding the question it was written to ask.

State what could not be rendered and why, rather than quietly narrowing. Two examples worth copying: the pack
ships no hatless Mage, so that candidate does not exist; and a different effect anchor could not be drawn
without rebinding `MatchArt.asset`, which the ticket forbade — so it became its own ticket.

## 7. Keep one sheet, regenerate the rest

`docs/frames/.gitignore` excludes everything the captures write and names the exceptions one at a time. Commit
**one sheet per question** under a name nothing else writes — never the tool's own `candidates-sheet.png`, which
every run overwrites — plus a short `.txt` beside it saying what was rendered, what question it answers, and the
exact command to redraw it. Add the negation to that `.gitignore` in the same commit.

Everything else — the per-candidate PNGs, the strips, `candidates-manifest.txt`, `strip-index.txt` — is
regenerated by whoever wants to look and must stay ignored, by construction and not by memory (AGENTS.md rule 4).
Check `git status` before committing: `git add -A` swept six generated indexes into a commit once.

## 8. Report

Post the sheet, the set file and the command on the ticket, close it, and leave the decision to the sitting.
Findings the render made on its own — a pair of tiles that came out identical, a direction a sweep ruled out —
go in the report; they are usually worth more than the pictures.
