# Playing a run from a shell

**A specification** · 10 August 2026 · written against `main` at `b5cf627`

> What `simcli` has to grow to become the first thing in this repository that a person **plays** rather than
> reads. It is written to be lifted into a wayfinder map, so §7 is a ticket list and §8 is the set of
> questions a map would grill before any of them are cut.
>
> Not a standing document and not indexed in [the docs README](README.md): it describes work that does not
> exist yet, so what is decided in it belongs in [the vision](vision.md), what is sequenced in it belongs in
> [the build order](build-order.md), and it is superseded by the map that carries it.

---

## Bottom line

**One new verb, `play`, which is `play-run` with the decisions taken at a prompt instead of read from a
file — and which writes the command script it just played.**

Everything under it already exists. `Run.Advance` takes a round's decision and gives back what the round came
to. `BuildPhase.Resolve` checks a decision against the offering, the unlocks, the slot width, the map, the
board and the purse, **and it is pure** — it returns a `Build` and moves nothing — so the same call that
validates a stored command file can price a half-composed decision at a prompt and be thrown away. `Offering`
draws the menu, `CostTable` prices everything including the things that are not units, `Board.ToReportText`
prints the position and `RoundReport.ToString` prints the round line the committed outcome file already uses.

**So the new code is a loop, a parser for what somebody types, and a way to draw a map in text.** No new
simulation surface, no format change, no engine, no licence. The estimate is one file of loop, two of
rendering, one verb branch, and a test file.

**This is not the vision's step 5 and does not replace it.** Step 5 is the client, and what an engine answers —
does it read, does it feel — this cannot. What this answers is the question sitting in front of it:
*is the build phase a decision worth making?* Ten waves is about five minutes at a prompt, so that question
gets asked tonight rather than after a medium-sized engine effort.

---

## 1. The verb

```
play       --seed <number> --out <file>
           --content <directory>, or each file named outright
           [--waves <number>] [--field-size <number>] [--no-death]
           [--transcript <file>]

       Plays a run one round at a time, taking each build phase from the
       terminal, and writes the decisions to --out as a command script.
       The script it writes is the one record-run compiles, so a run
       somebody played can be replayed, committed and diffed.

       --transcript reads the decisions from a file instead of the
       terminal, which is what a test does and what re-playing a session
       needs. The same words either way.
```

**A new verb rather than a flag on `play-run`**, because `play-run` is the verb a build gate calls and a mode
flag on it is a branch a gate can take by accident. The two verbs share every argument reader they already
have — `ContentOf`, `ShapeOf`, `RunVerb` — and share the report at the end.

**`--out` is required, not optional.** A run played at a prompt and not written down is an experiment nobody
can repeat, which is the one thing this repository does not do. It costs a path.

---

## 2. The screen

One frame per round, printed before the prompt. Every number on it is read off the run — the frame below is
wave 4 of the committed run, `content/run.commands` on seed 20260807, drawn from the real map and the real
offering. It is `RoundFrame.ToText`'s output character for character, pinned by `RoundFrameTests`.

```
wave 4 of 10        health 1245 of 1500        gold 545        3 slots

      0  1  2  3  4  5  6  7  8  9 10 11 12 13 14
 0    .  .  .  .  .  .  .  .  .  .  .  .  .  .  .
 1      .  S  #  #  #  #  #  #  #  #  #  #  .  .  .        standing
 2    .  .  .  .  .  .  a  .  .  .  .  .  #  .  .          1  a  archer   6,2
 3      .  .  #  #  #  #  #  #  #  #  #  #  .  .  .        2  a  archer   7,4
 4    .  .  #  .  .  .  .  a  .  .  .  .  .  .  .          3  a  archer   7,6
 5      .  .  #  #  #  #  #  #  #  #  #  #  #  .  .
 6    .  .  .  .  .  .  .  a  .  .  .  .  .  #  .        you may build
 7      .  E  #  #  #  #  #  #  #  #  #  #  #  .  .       11  soldier   30
 8    .  .  .  .  .  .  .  .  .  .  .  .  .  .  .          3  archer    40
                                                          14  ranger    40
                                                           4  mage      92

this wave's menu                           what you may send
  ordinary   1  minion            type 1     1  minion            10 each
  ordinary  13  skeleton-warrior  type 13    2  skeleton-scout     9 each
  ordinary  12  skeleton          type 12

nothing taken, nothing built, no slot filled.
>
```

Three claims about that frame:

- **The map is the whole map, in the coordinates a command uses.** Column across the top, row down the side,
  odd rows indented half a cell exactly as `content/map.txt` writes them, so counting characters gives the pair
  a `place` names. A built tower is a letter — lower case for a root of the ladder, upper case for anything
  upgraded — and the legend beside it carries the placement id, that letter, the name and the cell. **The
  prices are the `you may build` panel's and not the legend's**: what a tower costs is a fact about the roster
  and belongs beside the roster, and repeating it against every placement would price four archers four times.
- **The menu is spelled in the words a command script uses.** `ordinary 12` at the prompt, `ordinary 12` in the
  file. Same for `changer`. This is `Offerings.ToText`'s existing rule and the reason it can stay one
  vocabulary.
- **Nothing on it is forecast.** No predicted damage, no "this will hold". Vision §12 settled that outcome is
  not computed in any mode, and a prompt is exactly where the temptation to helpfully compute one shows up.
  Mechanism — what a tower costs, what it reaches, what the wave slots are — is free and total, and that is the
  whole of it.

---

## 3. What you type

Ten words. The four that are decisions carry the keywords `CommandScript` uses and the operands a script row
carries, **minus the wave** — at a prompt the round you are in is not something you should have to type.

| Word | What it does |
|---|---|
| `take ordinary <id>` / `take changer <id>` | The round's one take. Typing a second replaces the first — nothing has moved yet. One case refuses: a wave already fielding a creep only the first take unlocked, where the replacement leaves a decision that does not resolve. Emptying the slot on the player's behalf would be a silent drop, so the sentence names the slot and `undo` is the way past it |
| `place <type-id> <column> <row>` | Adds a placement to the phase being composed |
| `upgrade <type-id> <column> <row>` | Adds an upgrade, naming its target by the hex |
| `send <type-id> <count>` | Fills the next wave slot, in the order the sends were typed. Filled slots ascend strictly by type id, so a creep at or below the last one sent is refused rather than quietly reordered — sorting would rewrite the decision on its author's behalf. `send` with no room is a refusal, not a silent drop |
| `undo` | Drops the last thing added. Free, because a phase is composed in a local and the run has not seen it |
| `map` / `menu` / `costs` | Reprints a panel, and changes nothing |
| — | A label may be typed where an id is expected — `place archer 4 4` — because the roster carries labels already. **The written script always carries the id and the wave**, so what is typed is a convenience and what is stored is the record's own spelling |
| `done` | Commits the phase: `run.Advance(phase)`, print the round line, next wave |
| `quit` | Ends the run early, writes what was played, and says so |

**After every word that changes the phase, the loop re-runs `Resolve` and reprints the two numbers it
returns** — gold left and towers standing — plus any refusal. That is the whole feel of the thing: you type
`place archer 4 4`, and the frame comes back with 279 gold and a fourth letter on the map, or with the
sentence saying why not.

**A refusal is caught and reprinted, never thrown out of the loop.** `Resolve` raises
`SimulationException` with a sentence that already names the round, the verb and the cell — those sentences
were written for a script author and read perfectly at a prompt. The composed phase keeps whatever was legal;
the refused word is simply not added.

**`done` on a phase that cannot afford its wave is the same refusal**, and it is the one place the loop must be
careful: `Resolve` walks take → actions → slots and refuses the *whole* phase if the wave is unaffordable, so
the reprint after each `send` is what stops that arriving as a surprise at commit.

---

## 4. What it writes, and the claim it makes

At the end — the tenth wave, death, or `quit` — the verb does four things in this order:

1. Prints the run's outcome and the ending board, using `RunSummary.Outcome` — the fold the committed
   outcome file's own summary line is written by — and `Board.ToReportText`.
2. Compiles the decisions it collected into a command script, in the `content/commands.txt` grammar.
3. **Plays that script into a fresh run on the same seed and shape**, via `CommandStream.Recorded`, and
   compares every round report and the final outcome against what the player was shown.
4. Writes the script to `--out` only if step 3 agreed.

Step 3 is why this verb is worth building to this repository's standard rather than as a throwaway. It makes
**every play session a determinism test**: the interactive path and the recorded path have to produce the same
run, or nothing is written and the disagreement is printed. That is the same discipline the engine will need at
step 5 — the vision names it as *the view emits a command, the command goes into the record, the record is what
the match consumes* — and this pays for the first half of it in a place with no editor in the loop.

A session that ends in a disagreement exits non-zero. That failure is a bug in this verb, and it is better
found by a person playing than by nobody.

---

## 5. How it is tested with nobody at the keyboard

The rule in `AGENTS.md` is that everything runs from a cold shell. An interactive verb has to be tested
without a terminal, and `--transcript` is how: the loop reads lines from a `TextReader` and writes to a
`TextWriter`, both handed in at the top of the verb, `Console.In` and `Console.Out` in the ordinary case.

| Test | What it holds |
|---|---|
| A canned transcript plays ten rounds and the outcome matches `content/run-outcome.txt` | The interactive path and the recorded path are the same run — the claim of §4, asserted rather than asserted about |
| A transcript with a misspelled word, an unaffordable placement and a cell in the corridor | Each refuses, reprints, and the run continues on the next line |
| A transcript that ends early | `quit` writes a short script that `record-run` compiles and `play-run` plays |
| The map render, against the committed map | A pure string function, tested on the text it produces |

The first of those is the valuable one: it makes `content/commands.txt` playable as a transcript, so the
committed run becomes an input to this verb and not just to `play-run`.

**A `tools/play-run-interactive.ps1`** — one more static entry point, per rule 3 — supplies `--content content`,
a seed and an `--out`, so playing is one command from a cold clone. §1 makes the path required, so the script
has to default one; it is scratch space and not `content/`, because a session is an experiment and `content/`
holds the run this project committed.

---

## 6. What this deliberately does not do

- **No scouting.** It is priced — `snapshot 10 25` in `content/ruleset.txt`, `Purchase.Snapshot` in the cost
  table — and nothing in the simulation reads it. It cannot be added here: buying a snapshot spends gold, the
  command stream carries a take, actions and slots and **has no row for a purchase**, and a spend the record
  does not carry replays into a different purse. Scouting is format version 2 and its own effort. Naming that
  here is half the value of writing this down.
- **No saving mid-run.** The command script *is* the save file, and it is written at the end. A run is ten
  waves and about five minutes.
- **No selling and no cap on actions.** Both settled by [#142](https://github.com/ssalter21/tower-defense-game/issues/142) at charting; this verb is not where they get re-opened.
- **No colour, no cursor addressing, no live redraw.** A frame per round and a reprint per word, scrolling like
  every other verb here. A terminal UI library is a dependency, and this program has none.
- **No watching a wave.** A round resolves and prints a line. Watching is the engine's job and it is step 5.

---

## 7. The tickets

Eight tiny commits and an integrate, in dependency order, on `effort/played-from-a-shell`.

| # | Commit | Why it is separate |
|---|---|---|
| 1 | **A map draws itself in text** — `simcli/BoardMap.cs`, the grid, the legend, odd-row indentation | A pure string function with a test, and the only piece with a layout to argue about |
| 2 | **A round draws itself** — the header, the menu panel, the sendable panel | Same, and it composes with 1 |
| 3 | **A typed line becomes a build action** — the ten words, parsed, refusing what it does not know | Parser and vocabulary, no run in sight |
| 4 | **A phase is composed and priced without being played** — the compose-and-`Resolve` loop over one round | The heart of it. One round, no lifecycle |
| 5 | **A run is played round by round** — the verb loop, commit, the round line, the end | Adds the lifecycle to 4 |
| 6 | **The decisions come back as a script** — the writer, in the `commands.txt` grammar | Text out, no verb wiring |
| 7 | **A session is proved against a fresh run** — the §4 comparison, and the refusal to write | The claim, isolated so it can be watched failing |
| 8 | **The verb, the usage block and the shell script** — `play` in `Dispatch`, `Usage`, `tools/` | Wiring last, when everything it wires exists |
| 9 | Integrate: full suite, `/code-review` against the merge-base, one PR | The repo's review boundary |

---

## 8. What a map would grill first

Four questions this specification has answered by choosing, where choosing differently is cheap now:

1. **Should the flat offense be fixed first?** [#163](https://github.com/ssalter21/tower-defense-game/issues/163) left `win_rate_bp` at 0 on all 22 sweep rows — a bought wave gets nothing past the field's six towers. Play this verb today and *sending is pointless* is what you will feel, and it is a numbers problem wearing a design problem's clothes. **The recommendation is to move the field or the income first, in a commit, and then play.**
2. **Is the map render worth the two tickets?** The alternative is a list of placements. On a one-wide corridor with 47 route hexes, a list is arguably enough — and [#142](https://github.com/ssalter21/tower-defense-game/issues/142) already warns that placement is thin until the maze lands at seam 9. The grid is specified here because *where* is the only spatial decision the game currently has, and reading it as text is what makes the thinness visible rather than theoretical.
3. **Should `quit` write, or discard?** Specified as write, because a run abandoned at wave 6 because it was boring is the most informative artefact this verb can produce.
4. **One take, no skip — does that read at a prompt?** The rule is settled (unlocking is free, declining is rewarded by nothing), but at a prompt it means the loop cannot proceed until a take is named. Specified as: `done` before a take refuses and says why.
